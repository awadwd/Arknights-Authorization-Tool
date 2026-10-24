using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AkPassport.Setup;

/// <summary>
/// 安装 / 卸载器。
///
/// 设计取舍：
/// - 免管理员：装到 %LOCALAPPDATA%\Programs\，不需要 UAC，也不会在 Program Files 下踩权限坑。
/// - 卸载入口写 HKCU 的 Uninstall 键，「设置 → 应用」里能看到，卸载本身就是这个 exe 加 /uninstall。
/// - payload.zip 内嵌为资源（release.sh 打包时放进来），所以整个安装包是单个 exe。
/// - 用户数据（收藏 / 设置 / 抽卡记录）在 %LOCALAPPDATA%\AkPassport，和安装目录分开，
///   升级覆盖安装不会丢；只有卸载时才会问是否一起删。
/// </summary>
internal static class Program
{
	internal const string AppName = "方舟通行证谷子查询工具";
	internal const string ExeName = "AkPassport.Win.exe";
	internal const string UninstallerName = "卸载.exe";
	internal const string Publisher = "AkPassport";

	private const string RegUninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\AkPassportWin";
	private const string RegRunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
	private const string RunValueName = "AkPassportWin";
	private const string PayloadName = "payload.zip";

	[STAThread]
	private static int Main(string[] args)
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.SetHighDpiMode(HighDpiMode.SystemAware);

		var quiet = args.Any(a => a.Equals("/quiet", StringComparison.OrdinalIgnoreCase));
		if (args.Any(a => a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase)))
			return Uninstall(quiet);

		Application.Run(new SetupForm());
		return 0;
	}

	// ---------------------------------------------------------------- 路径

	/// <summary>默认安装目录（每用户，不需要管理员）。</summary>
	internal static string DefaultInstallDir =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Programs", AppName);

	/// <summary>已安装时的目录；没装过返回 null。</summary>
	internal static string? GetInstalledDir()
	{
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(RegUninstallKey);
			var dir = key?.GetValue("InstallLocation") as string;
			if (!string.IsNullOrWhiteSpace(dir) && File.Exists(Path.Combine(dir!, ExeName)))
				return dir;
		}
		catch
		{
		}

		// 注册表丢了但程序还在（比如手动拷过目录）——按默认目录兜底找一次
		if (File.Exists(Path.Combine(DefaultInstallDir, ExeName))) return DefaultInstallDir;
		return null;
	}

	internal static string DesktopPath()
	{
		var d = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
		if (!string.IsNullOrWhiteSpace(d) && Directory.Exists(d)) return d;

		var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var fallback = Path.Combine(profile, "Desktop");
		return !string.IsNullOrWhiteSpace(profile) && Directory.Exists(fallback) ? fallback : d;
	}

	internal static string StartMenuPath()
	{
		var p = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
		if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p)) return p;

		var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		return Path.Combine(appData, @"Microsoft\Windows\Start Menu\Programs");
	}

	internal static string UserDataDir =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AkPassport");

	// ---------------------------------------------------------------- 安装

	internal static Stream? OpenPayload()
	{
		var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadName);
		if (s is not null) return s;

		var side = Path.Combine(AppContext.BaseDirectory, PayloadName);
		return File.Exists(side) ? File.OpenRead(side) : null;
	}

	internal static long PayloadBytes()
	{
		var side = Path.Combine(AppContext.BaseDirectory, PayloadName);
		if (File.Exists(side)) return new FileInfo(side).Length;

		// 内嵌资源模式下没有旁边的 payload.zip，得从资源流里取长度，
		// 否则界面上会退化成写死的「约 250 MB」。
		try
		{
			using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadName);
			return s?.Length ?? -1;
		}
		catch
		{
			return -1;
		}
	}

	/// <summary>关掉正在跑的程序，否则文件被占用会安装失败。</summary>
	internal static void KillRunningApp()
	{
		try
		{
			foreach (var p in Process.GetProcessesByName("AkPassport.Win"))
			{
				try
				{
					if (!p.CloseMainWindow()) p.Kill();
					p.WaitForExit(3000);
				}
				catch
				{
				}
				finally
				{
					p.Dispose();
				}
			}
		}
		catch
		{
		}
	}

	/// <summary>清空安装目录（保留卸载器自身，稍后会被覆盖）。</summary>
	internal static void ClearDirectory(string dir, string? keepFileName = null)
	{
		if (!Directory.Exists(dir)) return;

		foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
		{
			if (keepFileName is not null && string.Equals(Path.GetFileName(f), keepFileName, StringComparison.OrdinalIgnoreCase))
				continue;
			try
			{
				File.SetAttributes(f, FileAttributes.Normal);
				File.Delete(f);
			}
			catch
			{
			}
		}

		foreach (var d in Directory.EnumerateDirectories(dir).ToList())
		{
			try
			{
				Directory.Delete(d, recursive: true);
			}
			catch
			{
			}
		}
	}

	internal static void Extract(string dir, IProgress<int> progress, out int fileCount)
	{
		Directory.CreateDirectory(dir);
		using var stream = OpenPayload() ?? throw new InvalidOperationException("安装包里没有 payload.zip，文件不完整。");
		using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

		var entries = zip.Entries.Where(e => !string.IsNullOrEmpty(e.FullName)).ToList();
		var total = Math.Max(entries.Count, 1);
		var done = 0;
		fileCount = 0;

		foreach (var e in entries)
		{
			var target = Path.Combine(dir, e.FullName.Replace('/', Path.DirectorySeparatorChar));
			var full = Path.GetFullPath(target);
			if (!full.StartsWith(Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase))
				continue; // 防目录穿越

			if (e.FullName.EndsWith("/", StringComparison.Ordinal))
			{
				Directory.CreateDirectory(full);
				continue;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(full)!);
			e.ExtractToFile(full, overwrite: true);
			fileCount++;
			done++;
			progress.Report(done * 100 / total);
		}
	}

	// ---------------------------------------------------------------- 快捷方式 / 注册表

	internal static bool CreateShortcut(string lnkPath, string target, string workingDir, string description)
	{
		try
		{
			var type = Type.GetTypeFromProgID("WScript.Shell");
			if (type is null) return false;

			dynamic shell = Activator.CreateInstance(type)!;
			dynamic link = shell.CreateShortcut(lnkPath);
			link.TargetPath = target;
			link.WorkingDirectory = workingDir;
			link.Description = description;
			var ico = Path.Combine(workingDir, "Assets", "app.ico");
			if (File.Exists(ico)) link.IconLocation = ico + ",0";
			link.Save();
			return File.Exists(lnkPath);
		}
		catch
		{
			return false;
		}
	}

	internal static void RemoveShortcuts()
	{
		foreach (var p in new[]
		{
			Path.Combine(DesktopPath(), AppName + ".lnk"),
			Path.Combine(StartMenuPath(), AppName + ".lnk"),
		})
		{
			try
			{
				if (File.Exists(p)) File.Delete(p);
			}
			catch
			{
			}
		}
	}

	internal static void RegisterUninstall(string dir)
	{
		try
		{
			using var key = Registry.CurrentUser.CreateSubKey(RegUninstallKey, true);
			var ver = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
			size = Directory.Exists(dir)
				? Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Sum(f => SafeLength(f)) / 1024
				: 0;

			key.SetValue("DisplayName", AppName);
			key.SetValue("DisplayVersion", ver);
			key.SetValue("Publisher", Publisher);
			key.SetValue("InstallLocation", dir);
			key.SetValue("DisplayIcon", Path.Combine(dir, "Assets", "app.ico"));
			key.SetValue("UninstallString", $"\"{Path.Combine(dir, UninstallerName)}\" /uninstall");
			key.SetValue("QuietUninstallString", $"\"{Path.Combine(dir, UninstallerName)}\" /uninstall /quiet");
			key.SetValue("NoModify", 1, RegistryValueKind.DWord);
			key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
			key.SetValue("EstimatedSize", (int)Math.Min(size, int.MaxValue), RegistryValueKind.DWord);
			key.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
		}
		catch
		{
		}
	}

	private static long size;

	private static long SafeLength(string path)
	{
		try
		{
			return new FileInfo(path).Length;
		}
		catch
		{
			return 0;
		}
	}

	internal static void UnregisterUninstall()
	{
		try
		{
			Registry.CurrentUser.DeleteSubKeyTree(RegUninstallKey, throwOnMissingSubKey: false);
		}
		catch
		{
		}
	}

	internal static void SetStartup(bool enabled, string dir)
	{
		try
		{
			using var key = Registry.CurrentUser.CreateSubKey(RegRunKey, true);
			if (enabled)
			{
				var exe = Path.Combine(dir, ExeName);
				if (File.Exists(exe)) key.SetValue(RunValueName, $"\"{exe}\" --tray");
			}
			else
			{
				key.DeleteValue(RunValueName, throwOnMissingValue: false);
			}
		}
		catch
		{
		}
	}

	internal static bool IsStartupEnabled()
	{
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(RegRunKey);
			return key?.GetValue(RunValueName) is string;
		}
		catch
		{
			return false;
		}
	}

	/// <summary>系统里有没有 WebView2 运行时（Win11 自带；Win10 可能没有）。</summary>
	internal static bool HasWebView2()
	{
		const string id = @"{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";
		foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
		{
			foreach (var sub in new[]
			{
				$@"SOFTWARE\Microsoft\EdgeUpdate\Clients\{id}",
				$@"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{id}",
			})
			{
				try
				{
					using var k = root.OpenSubKey(sub);
					if (k?.GetValue("pv") is string pv && pv != "0.0.0.0") return true;
				}
				catch
				{
				}
			}
		}

		return false;
	}

	// ---------------------------------------------------------------- 卸载

	private static int Uninstall(bool quiet)
	{
		var dir = GetInstalledDir() ?? DefaultInstallDir;

		if (!quiet)
		{
			var r = MessageBox.Show(
				$"确定要卸载「{AppName}」吗？\n\n安装目录：{dir}",
				"卸载 - " + AppName,
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question,
				MessageBoxDefaultButton.Button2);
			if (r != DialogResult.Yes) return 1;
		}

        KillRunningApp();
		SetStartup(false, dir);

		var keepData = true;
		if (!quiet && Directory.Exists(UserDataDir))
		{
			var r = MessageBox.Show(
				$"是否保留个人数据（收藏、设置、抽卡记录）？\n\n位置：{UserDataDir}\n\n选「是」保留，下次重装还在；选「否」一并删除。",
				"卸载 - " + AppName,
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question,
				MessageBoxDefaultButton.Button1);
			keepData = r == DialogResult.Yes;
		}

		RemoveShortcuts();
		UnregisterUninstall();

		// 卸载器自己就在安装目录里，删不掉自己 —— 清完其它文件后用 cmd 延迟删整个目录
		ClearDirectory(dir, UninstallerName);
		if (!keepData)
		{
			try
			{
				Directory.Delete(UserDataDir, recursive: true);
			}
			catch
			{
			}
		}

		if (!quiet)
			MessageBox.Show("已卸载。", "卸载 - " + AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);

		// ★ 必须在提示框关掉之后再排延迟删除：
		//   卸载器进程本身就在这个目录里，进程还活着时 rmdir 会因「文件被占用」失败，
		//   结果目录里只剩一个 卸载.exe 删不掉。
		if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, UninstallerName)))
		{
			try
			{
				// -n 6 ≈ 等 5 秒：给本进程退出、exe 句柄释放留时间；
				// WorkingDirectory 必须挪出安装目录 —— 若 cmd 的当前目录就是待删目录，
				// rmdir 会因为「目录正在被使用」失败，最后只剩一个 卸载.exe。
				Process.Start(new ProcessStartInfo("cmd.exe")
				{
					Arguments = $"/c ping 127.0.0.1 -n 6 >nul & rmdir /s /q \"{dir}\"",
					UseShellExecute = true,
					WindowStyle = ProcessWindowStyle.Hidden,
					WorkingDirectory = Path.GetTempPath(),
				});
			}
			catch
			{
			}
		}

		return 0;
	}
}
