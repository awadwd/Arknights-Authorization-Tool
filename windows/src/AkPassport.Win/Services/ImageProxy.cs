using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AkPassport.Win.Services;

/// <summary>
/// 热门图床（B 站图床 i0.hdslb.com 等）对 Referer 做了防盗链：
/// 页面在 https://app.local 下请求图片时会带上 Referer: https://app.local/，服务端直接返回 403，
/// 表现就是「盒号大图一片空白、点开也是空白」。
/// 这里把这些域名的图片请求接管下来，用合法 Referer 由壳自己下载，再回填给页面。
/// </summary>
public sealed class ImageProxy
{
	// 需要改写 Referer 的图床域名（后缀匹配）→ 各自合法的来源页
	private static readonly (string Host, string Referer)[] HotlinkHosts =
	{
		("hdslb.com", "https://www.bilibili.com/"),
		("biliimg.com", "https://www.bilibili.com/"),
		("sinaimg.cn", "https://weibo.com/"),
	};

	private const int MaxCachedItems = 120;
	private const long MaxCachedBytes = 64L * 1024 * 1024;

	private readonly HttpClient _http = new(new SocketsHttpHandler
	{
		AutomaticDecompression = DecompressionMethods.All,
		PooledConnectionLifetime = TimeSpan.FromMinutes(5),
	})
	{
		Timeout = TimeSpan.FromSeconds(20),
	};

	private readonly ConcurrentDictionary<string, byte[]> _cache = new(StringComparer.Ordinal);
	private readonly LinkedList<string> _order = new();
	private readonly object _lock = new();
	private long _cachedBytes;

	public static bool Handles(string uri) => RefererFor(uri) is not null;

	/// <summary>命中图床则返回应使用的 Referer，否则返回 null。</summary>
	public static string? RefererFor(string uri)
	{
		if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed)) return null;
		if (parsed.Scheme != Uri.UriSchemeHttps) return null;

		foreach (var (host, referer) in HotlinkHosts)
		{
			if (parsed.Host.Equals(host, StringComparison.OrdinalIgnoreCase) ||
				parsed.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase))
			{
				return referer;
			}
		}
		return null;
	}

	public async Task<ProxyResult> GetAsync(string uri)
	{
		if (_cache.TryGetValue(uri, out var cached))
		{
			return new ProxyResult(200, GuessContentType(uri), cached);
		}

		using var request = new HttpRequestMessage(HttpMethod.Get, uri);
		// 关键：换成来源站点的 Referer，去掉 app.local
		var referer = RefererFor(uri) ?? "https://www.bilibili.com/";
		request.Headers.TryAddWithoutValidation("Referer", referer);
		request.Headers.TryAddWithoutValidation("User-Agent",
			"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0 Safari/537.36");

		using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
		var body = await response.Content.ReadAsByteArrayAsync();
		var type = response.Content.Headers.ContentType?.MediaType ?? GuessContentType(uri);

		if (response.IsSuccessStatusCode && body.Length > 0)
		{
			Remember(uri, body);
		}

		return new ProxyResult((int)response.StatusCode, type, body);
	}

	private void Remember(string uri, byte[] body)
	{
		lock (_lock)
		{
			if (_cache.ContainsKey(uri)) return;

			_cache[uri] = body;
			_order.AddLast(uri);
			_cachedBytes += body.Length;

			while (_order.Count > MaxCachedItems || _cachedBytes > MaxCachedBytes)
			{
				var first = _order.First;
				if (first is null) break;
				_order.RemoveFirst();
				if (_cache.TryRemove(first.Value, out var removed))
				{
					_cachedBytes -= removed.Length;
				}
			}
		}
	}

	private static string GuessContentType(string uri)
	{
		var path = uri.Split('?')[0].ToLowerInvariant();
		if (path.EndsWith(".png")) return "image/png";
		if (path.EndsWith(".webp")) return "image/webp";
		if (path.EndsWith(".gif")) return "image/gif";
		if (path.EndsWith(".bmp")) return "image/bmp";
		return "image/jpeg";
	}
}
