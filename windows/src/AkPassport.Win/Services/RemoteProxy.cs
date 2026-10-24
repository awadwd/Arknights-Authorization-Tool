using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AkPassport.Win.Services;

/// <summary>
/// 直连代理：把页面里对数据源域名的请求转给外壳去发，再回填 CORS 头。
///
/// 为什么需要：uni-app H5 跑在 https://app.local 这个虚拟域上，
/// 页面用 uni.request / fetch 直接打 raw.gitcode.com、知晓云（*.myminapp.com）这些域名时是跨域请求，
/// 对方不返回 Access-Control-Allow-Origin → WebView2 直接判失败（XHR status=0）。
/// 手机浏览器/小程序各有自己的放行策略，所以只有 Windows 壳会「全端提示网络错误」。
/// 这里在 WebView2 层把这些域名接管掉，前端代码一行都不用改。
///
/// 注意两点：
/// 1. 知晓云要求带 X-Hydrogen-Client-ID，头必须原样转发；
/// 2. 带自定义头的 GET 会先发 OPTIONS 预检，预检也要给回 CORS 头，否则真正请求根本不会发出。
/// </summary>
public sealed class RemoteProxy
{
	private static readonly string[] Hosts =
	{
		"https://raw.gitcode.com/",
		"https://cdn.jsdelivr.net/",
		"https://fastly.jsdelivr.net/",
		"https://raw.githubusercontent.com/",
	};

	/// <summary>需要转发给上游的请求头白名单（其余如 Host / Origin / Cookie 一律不带）。</summary>
	private static readonly string[] ForwardHeaders =
	{
		"Accept", "Accept-Language", "Cache-Control", "Content-Type",
		"Authorization", "X-Requested-With", "X-Hydrogen-Client-ID", "X-Hydrogen-Environment",
	};

	private const string CorsHeaders =
		"Access-Control-Allow-Origin: *\r\n" +
		"Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
		"Access-Control-Allow-Headers: *\r\n" +
		"Access-Control-Max-Age: 86400\r\n";

	private readonly HttpClient _http = new(new SocketsHttpHandler
	{
		AutomaticDecompression = DecompressionMethods.All,
		PooledConnectionLifetime = TimeSpan.FromMinutes(5),
	})
	{
		Timeout = TimeSpan.FromSeconds(25),
	};

	public static bool Handles(string uri)
	{
		if (Hosts.Any(h => uri.StartsWith(h, StringComparison.OrdinalIgnoreCase))) return true;

		// 知晓云（数据版本 / 公告都从这里取），租户域名形如 <clientId>.myminapp.com
		if (Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
		{
			return parsed.Scheme == Uri.UriSchemeHttps
			       && parsed.Host.EndsWith(".myminapp.com", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	/// <summary>预检请求：不访问上游，直接回一套放行头。</summary>
	public static string PreflightHeaders => CorsHeaders;

	public async Task<ProxyResult> GetAsync(string uri, IEnumerable<KeyValuePair<string, string>>? headers = null)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, uri);
		request.Headers.TryAddWithoutValidation("User-Agent", "AkPassportWin/1.0");

		if (headers is not null)
		{
			foreach (var (name, value) in headers)
			{
				if (name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase)) continue;
				if (!ForwardHeaders.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
				request.Headers.TryAddWithoutValidation(name, value);
			}
		}

		using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
		var body = await response.Content.ReadAsByteArrayAsync();

		var type = response.Content.Headers.ContentType?.ToString();
		// gitcode 给的是 application/octet-stream，uni.request 会当字符串处理，没问题；
		// 但 json 资源明确标成 json 更稳（部分解析分支看 content-type）。
		if (string.IsNullOrWhiteSpace(type))
		{
			type = uri.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
				? "application/json; charset=utf-8"
				: "application/octet-stream";
		}

		return new ProxyResult((int)response.StatusCode, type, body);
	}
}
