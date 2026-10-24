using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AkPassport.Win.Services;

/// <summary>GitHub Releases 上的一条发布。</summary>
public sealed record ReleaseInfo(
	string Tag,
	string Name,
	string Notes,
	string PageUrl,
	string? InstallerUrl,
	string? PortableUrl,
	string? PortableName,
	long PortableSize,
	string? PortableSha256,
	DateTimeOffset? PublishedAt);

/// <summary>下载进度（已收字节 / 总字节 / 当前线路名）。</summary>
public readonly record struct DownloadProgress(long Received, long Total, string Source);

/// <summary>下载线路。</summary>
public enum DownloadRoute
{
	/// <summary>先直连 GitHub，失败再走加速线路。</summary>
	Auto = 0,

	/// <summary>只用 GitHub 官方直链。</summary>
	GitHubOnly = 1,

	/// <summary>只用加速线路（国内更快，但属于第三方转发）。</summary>
	CdnOnly = 2,
}

public sealed record DownloadResult(string Path, string SourceLabel);

/// <summary>
/// 检查更新 + 下载更新包。
///
/// 设计取向（对齐 MAA / BetterGI，但只用开源直链，不接任何付费 CDN）：
///   · 检查只读 GitHub Releases 元数据，不改本机设置、不执行任何东西；
///   · 下载走两套线路：GitHub 官方直链，以及可选的第三方加速前缀（仅拼接原 URL）；
///   · 装包交给同目录的 AkPassport.Updater.exe —— 主程序没法覆盖正在运行的自己。
/// </summary>
public static class UpdateService
{
	// 开源仓库：正式版会发在这里的 Releases
	public const string Owner = "awadwd";
	public const string Repo = "Arknights-Authorization-Tool";
	public const string RepoUrl = "https://github.com/awadwd/Arknights-Authorization-Tool";
	public const string ReleasesUrl = RepoUrl + "/releases";

	/// <summary>
	/// 加速线路：只是给原始 GitHub 地址加前缀，不改内容、不经过任何账号体系。
	/// 都属于第三方反代，可用性会变，所以放在直链之后兜底。
	/// </summary>
	private static readonly string[] CdnPrefixes =
	{
		"https://ghfast.top/",
		"https://gh-proxy.com/",
	};

	private const string UpdaterName = "AkPassport.Updater.exe";
	private const string MainExeName = "AkPassport.Win.exe";

	/// <summary>当前程序版本（去掉了 +commit 后缀）。</summary>
	public static string CurrentVersion { get; } = ReadCurrentVersion();

	private static readonly HttpClient Http = CreateClient();

	private static HttpClient CreateClient()
	{
		var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
		// GitHub API 强制要求 User-Agent，缺了直接 403
		client.DefaultRequestHeaders.UserAgent.ParseAdd("AkPassport-Win/" + CurrentVersion);
		client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
		return client;
	}

	private static string ReadCurrentVersion()
	{
		var asm = Assembly.GetExecutingAssembly();

		// AssemblyVersion 只能是 1.0.0.0，真正的「1.0.0-beta.4」在 InformationalVersion 里；
		// 带源码链接时会附一个 +<commit hash>，要截掉。
		var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		if (!string.IsNullOrWhiteSpace(info))
		{
			var plus = info.IndexOf('+');
			return (plus > 0 ? info[..plus] : info).Trim();
		}

		return asm.GetName().Version?.ToString(3) ?? "1.0.0";
	}

	// ── 检查 ────────────────────────────────────────────────────────────────

	/// <summary>取仓库里最新一条「带 Windows 包」的发布。没有则返回 null。</summary>
	public static async Task<ReleaseInfo?> GetLatestAsync(CancellationToken ct = default)
	{
		// 不用 /releases/latest —— 它只返回非 prerelease，beta 阶段永远查不到东西。
		var url = $"https://api.github.com/repos/{Owner}/{Repo}/releases?per_page=10";

		using var resp = await Http.GetAsync(url, ct).ConfigureAwait(false);

		if (resp.StatusCode == HttpStatusCode.NotFound) return null;
		if (resp.StatusCode == HttpStatusCode.Forbidden)
			throw new InvalidOperationException("GitHub 接口访问过于频繁，请稍后再试");

		resp.EnsureSuccessStatusCode();

		await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
		using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

		if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

		foreach (var rel in doc.RootElement.EnumerateArray())
		{
			if (rel.TryGetProperty("draft", out var draft) && draft.GetBoolean()) continue;

			var tag = Str(rel, "tag_name");
			if (string.IsNullOrWhiteSpace(tag)) continue;

			string? installer = null, portable = null, portableName = null, portableSha = null;
			long portableSize = 0;

			if (rel.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
			{
				foreach (var a in assets.EnumerateArray())
				{
					var name = Str(a, "name") ?? "";
					var dl = Str(a, "browser_download_url");
					if (string.IsNullOrWhiteSpace(dl)) continue;

					if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
					{
						installer ??= dl;
					}
					else if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
					         && name.Contains("Win", StringComparison.OrdinalIgnoreCase))
					{
						if (portable is not null) continue;
						portable = dl;
						portableName = name;
						portableSha = Str(a, "digest");            // 形如 sha256:abc…
						if (a.TryGetProperty("size", out var sz) && sz.TryGetInt64(out var n)) portableSize = n;
					}
				}
			}

			// 只认「带了 Windows 安装包」的发布。
			// 同一个仓库里还有安卓版的发布（v1.50.43 之类，挂的是 apk），
			// 版本号体系和 Windows 版（1.0.0-beta.x）完全不同，混在一起比
			// 会永远显示「有新版本」，还会把人引到一个装不上的 apk 上。
			if (installer is null && portable is null) continue;

			DateTimeOffset? published = null;
			if (DateTimeOffset.TryParse(Str(rel, "published_at"), out var p)) published = p;

			var relName = Str(rel, "name");
			return new ReleaseInfo(
				Tag: tag.Trim(),
				Name: string.IsNullOrWhiteSpace(relName) ? tag.Trim() : relName!.Trim(),
				Notes: Str(rel, "body") ?? "",
				PageUrl: Str(rel, "html_url") ?? ReleasesUrl,
				InstallerUrl: installer,
				PortableUrl: portable,
				PortableName: portableName,
				PortableSize: portableSize,
				PortableSha256: portableSha,
				PublishedAt: published);
		}

		return null;
	}

	private static string? Str(JsonElement e, string key)
		=> e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

	// ── 下载 ────────────────────────────────────────────────────────────────

	/// <summary>
	/// 下载更新包到 <c>%LOCALAPPDATA%\AkPassport\update\</c>，返回本地路径。
	/// 有 sha256 就校验；某个源校验不过会自动换下一个。
	/// </summary>
	public static async Task<DownloadResult> DownloadAsync(
		ReleaseInfo rel,
		DownloadRoute route,
		IProgress<DownloadProgress>? progress = null,
		CancellationToken ct = default)
	{
		// 覆盖式更新用 zip（绿色包）最合适：不管是安装版还是绿色版，
		// 解压覆盖都能落在同一个目录里。
		var url = rel.PortableUrl ?? rel.InstallerUrl
		          ?? throw new InvalidOperationException("这条发布没有可下载的 Windows 安装包");

		var fileName = rel.PortableName ?? Path.GetFileName(new Uri(url).AbsolutePath);

		AppPaths.EnsureUpdateDir();
		var target = Path.Combine(AppPaths.UpdateDir, Sanitize(fileName));

		var errors = new List<string>();

		foreach (var (sourceUrl, label) in BuildSources(url, route))
		{
			try
			{
				await DownloadOneAsync(sourceUrl, target, label, rel.PortableSize, progress, ct)
					.ConfigureAwait(false);

				if (!string.IsNullOrWhiteSpace(rel.PortableSha256)
				    && !await VerifySha256Async(target, rel.PortableSha256!, ct).ConfigureAwait(false))
				{
					TryDelete(target);
					errors.Add($"{label}：sha256 校验不通过");
					continue;
				}

				return new DownloadResult(target, label);
			}
			catch (OperationCanceledException)
			{
				TryDelete(target);
				throw;
			}
			catch (Exception ex)
			{
				TryDelete(target);
				errors.Add($"{label}：{Describe(ex)}");
			}
		}

		throw new InvalidOperationException("所有下载线路都失败了 —— " + string.Join("；", errors));
	}

	private static IEnumerable<(string Url, string Label)> BuildSources(
		string directUrl, DownloadRoute route)
	{
		if (route != DownloadRoute.CdnOnly)
			yield return (directUrl, "GitHub 直链");

		if (route == DownloadRoute.GitHubOnly) yield break;

		foreach (var prefix in CdnPrefixes)
		{
			string host;
			try { host = new Uri(prefix).Host; } catch { host = prefix; }
			yield return (prefix + directUrl, "加速线路 " + host);
		}
	}

	private static async Task DownloadOneAsync(
		string url, string target, string label, long knownSize,
		IProgress<DownloadProgress>? progress, CancellationToken ct)
	{
		using var req = new HttpRequestMessage(HttpMethod.Get, url);
		using var resp = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
			.ConfigureAwait(false);
		resp.EnsureSuccessStatusCode();

		var total = resp.Content.Headers.ContentLength ?? knownSize;
		progress?.Report(new DownloadProgress(0, total, label));

		await using var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
		await using var dst = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None,
			81920, useAsync: true);

		var buffer = new byte[81920];
		long received = 0;
		var lastReport = 0L;
		int read;

		while ((read = await src.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
		{
			await dst.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
			received += read;

			// 每 ~256 KB 报一次，避免进度回调把 UI 线程打满
			if (received - lastReport >= 262144 || (total > 0 && received >= total))
			{
				lastReport = received;
				progress?.Report(new DownloadProgress(received, total, label));
			}
		}

		if (total > 0 && received < total)
			throw new IOException($"下载不完整（{received}/{total} 字节）");
	}

	private static async Task<bool> VerifySha256Async(string path, string expected, CancellationToken ct)
	{
		var want = expected.Contains(':') ? expected[(expected.IndexOf(':') + 1)..] : expected;
		want = want.Trim().ToLowerInvariant();
		if (want.Length != 64) return true;   // 不是标准 sha256，跳过校验

		await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
			81920, useAsync: true);
		var hash = await SHA256.HashDataAsync(fs, ct).ConfigureAwait(false);
		var got = Convert.ToHexString(hash).ToLowerInvariant();
		return got == want;
	}

	// ── 安装 ────────────────────────────────────────────────────────────────

	/// <summary>
	/// 把已下载的包交给独立更新器，然后调用方应当立即退出程序。
	/// 更新器会等本进程退出、解压覆盖、再拉起新版本。
	/// </summary>
	public static bool LaunchUpdater(string zipPath, out string error)
	{
		var dir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		var updater = Path.Combine(dir, UpdaterName);

		if (!File.Exists(updater))
		{
			error = "更新程序缺失（" + UpdaterName + "），请重新下载完整安装包";
			return false;
		}

		try
		{
			AppPaths.EnsureUpdateDir();
			Diag.Log("launch updater: " + zipPath);

			Process.Start(new ProcessStartInfo
			{
				FileName = updater,
				Arguments = $"--pid {Environment.ProcessId} --zip \"{zipPath}\" --dir \"{dir}\" " +
				            $"--exe \"{MainExeName}\" --log \"{AppPaths.UpdateLog}\"",
				WorkingDirectory = dir,
				UseShellExecute = false,
				CreateNoWindow = true,
			});

			error = "";
			return true;
		}
		catch (Exception ex)
		{
			Diag.Log("launch updater failed: " + ex.Message);
			error = ex.Message;
			return false;
		}
	}

	private static string Sanitize(string name)
	{
		foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
		return name;
	}

	private static void TryDelete(string path)
	{
		try { if (File.Exists(path)) File.Delete(path); } catch { }
	}

	private static string Describe(Exception ex) => ex switch
	{
		HttpRequestException => "连接失败",
		TaskCanceledException => "超时",
		IOException => ex.Message,
		_ => ex.Message,
	};

	// ── 版本比较 ────────────────────────────────────────────────────────────

	/// <summary>candidate 是否比 current 新。支持 v 前缀与 -beta.4 这类预发布后缀。</summary>
	public static bool IsNewer(string candidate, string current)
	{
		var (cCore, cPre) = Split(candidate);
		var (nCore, nPre) = Split(current);

		if (cCore != nCore) return cCore > nCore;

		// 主版本相同时：正式版 > 预发布版
		if (cPre.Length == 0) return nPre.Length > 0;
		if (nPre.Length == 0) return false;

		return ComparePre(cPre, nPre) > 0;
	}

	private static (Version Core, string Pre) Split(string? raw)
	{
		var s = (raw ?? "").Trim();
		if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase)) s = s[1..];

		var dash = s.IndexOf('-');
		var corePart = dash < 0 ? s : s[..dash];
		var pre = dash < 0 ? "" : s[(dash + 1)..];

		if (!Version.TryParse(corePart, out var v))
			v = Version.TryParse(corePart + ".0", out var v2) ? v2 : new Version(0, 0);

		return (new Version(v.Major, v.Minor, Math.Max(v.Build, 0)), pre);
	}

	/// <summary>比较预发布后缀（beta.3 / rc.1）：数字段按数值，字母段按序数。</summary>
	private static int ComparePre(string a, string b)
	{
		var pa = a.Split('.');
		var pb = b.Split('.');

		for (var i = 0; i < Math.Max(pa.Length, pb.Length); i++)
		{
			if (i >= pa.Length) return -1;
			if (i >= pb.Length) return 1;

			var na = int.TryParse(pa[i], out var x);
			var nb = int.TryParse(pb[i], out var y);

			int cmp;
			if (na && nb) cmp = x.CompareTo(y);
			else if (na) cmp = -1;
			else if (nb) cmp = 1;
			else cmp = string.Compare(pa[i], pb[i], StringComparison.OrdinalIgnoreCase);

			if (cmp != 0) return cmp > 0 ? 1 : -1;
		}

		return 0;
	}
}
