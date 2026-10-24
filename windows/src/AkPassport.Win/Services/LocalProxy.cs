using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace AkPassport.Win.Services;

public readonly record struct ProxyResult(int Status, string ContentType, byte[] Body);

/// <summary>
/// 把 https://app.local/gitcode/** 转发到 raw.gitcode.com。
/// 前端在 H5 分支里已经把 https://raw.gitcode.com 换成了 /gitcode（原本是 Vite dev server 的代理），
/// 所以壳里补上这一段，前端一行都不用改。
/// </summary>
public sealed class LocalProxy
{
	private const string Prefix = "/gitcode";
	private const string Upstream = "https://raw.gitcode.com";

	private readonly HttpClient _http = new(new SocketsHttpHandler
	{
		AutomaticDecompression = DecompressionMethods.All,
		PooledConnectionLifetime = TimeSpan.FromMinutes(5),
	})
	{
		Timeout = TimeSpan.FromSeconds(25),
	};

	public static bool Handles(string uri) =>
		uri.StartsWith("https://app.local" + Prefix + "/", StringComparison.OrdinalIgnoreCase);

	public async Task<ProxyResult> GetAsync(string uri)
	{
		var tail = uri[("https://app.local".Length + Prefix.Length)..];
		using var request = new HttpRequestMessage(HttpMethod.Get, Upstream + tail);
		request.Headers.TryAddWithoutValidation("User-Agent", "AkPassportWin/1.0");

		using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead);
		var body = await response.Content.ReadAsByteArrayAsync();
		var type = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
		return new ProxyResult((int)response.StatusCode, type, body);
	}
}
