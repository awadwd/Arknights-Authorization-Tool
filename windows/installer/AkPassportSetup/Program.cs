using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AkPassport.Setup;

/// <summary>安装 / 卸载器。</summary>
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

		var opt = SetupOptions.Parse(args);

		if (opt.Uninstall) return (int)Uninstall(opt);

		// 静默安装：无窗口、无弹框、只靠退出码和日志表达结果。
		// 给批量部署和自动化测试用，也是首轮验收脚本能跑起来的前提。
		if (opt.Silent) return (int)InstallSilent(opt);

		Application.Run(new SetupForm());
		return (int)SetupExit.Ok;
	}

	// ---------------------------------------------------------------- 版本

	/// <summary>
	/// 完整版本号，含 prerelease 段（如 1.2.3-beta.4；正式版就是 1.2.3）。
	/// ★ 不能用 AssemblyName.Version —— 它只有四段数字，beta.4 会被抹成 1.0.0，
	///   结果「设置 → 应用」里装着 beta.4 却显示 1.0.0，排查版本问题时会直接误导。
	/// </summary>
	internal static string FullVersion
	{
		get
		{
			try
			{
				var info = typeof(Program).Assembly
					.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
				if (!string.IsNullOrWhiteSpace(info))
					return info!.Split('+')[0].Trim();

				var v = typeof(Program).Assembly.GetName().Version;
				return v is null ? "1.0.0" : v.ToString(3);
			}
			catch
			{
				return "1.0.0";
			}
		}
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

	/// <summary>可被 /datadir 覆盖，测试时用它把数据目录关进沙箱。</summary>
	internal static string ResolveDataDir(string? overrideDir) =>
		string.IsNullOrWhiteSpace(overrideDir) ? UserDataDir : Path.GetFullPath(overrideDir!);

	// ---------------------------------------------------------------- payload

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

	/// <summary>解压后的总字节数，用来做磁盘空间预检。读不到返回 -1（跳过预检）。</summary>
	internal static long PayloadUncompressedBytes()
	{
		try
		{
			using var s = OpenPayload();
			if (s is null) return -1;
			using var zip = new ZipArchive(s, ZipArchiveMode.Read);
			return zip.Entries.Sum(e => e.Length);
		}
		catch
		{
			return -1;
		}
	}

	// ---------------------------------------------------------------- 安装

	internal sealed class InstallOptions
	{
		public bool Desktop = true;
		public bool StartMenu = true;
		public bool Startup;
		public bool Launch;
	}

	/// <summary>
	/// 真正的安装动作。GUI 和 /silent 都走这里，保证两条路径行为完全一致
	/// （以前只有 GUI 一条路径，静默安装根本不存在，自动化测试无从谈起）。
	/// </summary>
	internal static SetupExit Install(
		string dir,
		InstallOptions opt,
		IProgress<int>? progress,
		IProgress<string>? phase,
		out int files,
		out string error)
	{
		files = 0;
		error = "";

		if (string.IsNullOrWhiteSpace(dir))
		{
			error = "没有指定安装位置。";
			return SetupExit.NotWritable;
		}

		dir = Path.GetFullPath(dir.Trim());

		// 目标路径被一个同名文件占着 —— CreateDirectory / 解压都会炸得莫名其妙，先讲清楚。
		if (File.Exists(dir) && !Directory.Exists(dir))
		{
			error = $"安装位置是一个文件，不是文件夹：{dir}";
			return SetupExit.NotWritable;
		}

		if (OpenPayload() is null)
		{
			error = "安装包不完整：找不到内嵌的 payload.zip。请重新下载安装程序。";
			return SetupExit.PayloadMissing;
		}

		// ---- 磁盘空间预检 ----
		// 不预检的话，解压到一半才失败，用户拿到的是一个半装的目录，
		// 而且错误信息是 IOException 的 "磁盘空间不足"，看不懂。
		var needed = PayloadUncompressedBytes();
		if (needed > 0 && !HasEnoughSpace(dir, needed, out var free, out var probe))
		{
			error = $"磁盘空间不足：需要 {needed / 1048576} MB，{probe} 只剩 {free / 1048576} MB。";
			return SetupExit.DiskFull;
		}

		phase?.Report("正在准备…");
		KillRunningApp();
		progress?.Report(2);

		// ---- 建目录 ----
		try
		{
			Directory.CreateDirectory(dir);
		}
		catch (Exception ex)
		{
			error = $"无法创建安装目录（{ex.GetType().Name}）：{ex.Message}";
			return SetupExit.NotWritable;
		}

		// ---- 清旧文件 ----
		phase?.Report("正在清理旧版本…");
		var failedDeletes = ClearDirectoryEx(dir, UninstallerName);
		if (failedDeletes.Count > 0)
		{
			foreach (var f in failedDeletes.Take(5)) SetupLog.Write("清理失败 " + f);
			SetupLog.Write($"清理阶段有 {failedDeletes.Count} 项删不掉，继续尝试覆盖");
		}
		progress?.Report(5);

		// ---- 解压 ----
		phase?.Report("正在写入文件…");
		try
		{
			var p = progress is null
				? null
				: new Progress<int>(v => progress.Report(5 + (int)(v * 0.88)));
			Extract(dir, p, out files);
		}
		catch (UnauthorizedAccessException ex)
		{
			error = $"没有写入权限：{ex.Message}\n如果程序正在运行，请先退出再装。";
			return SetupExit.NotWritable;
		}
		catch (IOException ex)
		{
			error = $"写入文件失败（可能被占用或磁盘已满）：{ex.Message}";
			return SetupExit.NotWritable;
		}
		catch (Exception ex)
		{
			error = $"解压失败（{ex.GetType().Name}）：{ex.Message}";
			return SetupExit.Failed;
		}

		progress?.Report(94);

		// ---- 快捷方式 ----
		phase?.Report("正在创建快捷方式…");
		try
		{
			var exe = Path.Combine(dir, ExeName);
			if (opt.Desktop)
				CreateShortcut(Path.Combine(DesktopPath(), AppName + ".lnk"), exe, dir, AppName);
			if (opt.StartMenu)
				CreateShortcut(Path.Combine(StartMenuPath(), AppName + ".lnk"), exe, dir, AppName);
		}
		catch (Exception ex)
		{
			// 快捷方式失败不该让整个安装失败，记日志就够
			SetupLog.Write("创建快捷方式失败：" + ex.Message);
		}

		// ---- 卸载器 / 注册表 / 自启 ----
		phase?.Report("正在写入卸载信息…");
		var self = Environment.ProcessPath;
		if (!string.IsNullOrEmpty(self))
		{
			try
			{
				var target = Path.Combine(dir, UninstallerName);
				if (!string.Equals(Path.GetFullPath(self!), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
					File.Copy(self!, target, overwrite: true);
			}
			catch (Exception ex)
			{
				SetupLog.Write("复制卸载器失败：" + ex.Message);
			}
		}
		RegisterUninstall(dir);
		SetStartup(opt.Startup, dir);

		progress?.Report(100);
		phase?.Report($"安装完成（{files} 个文件）");
		return SetupExit.Ok;
	}

	private static bool HasEnoughSpace(string dir, long needed, out long free, out string probe)
	{
		free = 0;
		probe = "";
		try
		{
			var existing = NearestExisting(dir);
			if (existing is null) return true;   // 盘符都不存在，交给后续步骤报错
			var root = Path.GetPathRoot(existing);
			if (string.IsNullOrEmpty(root)) return true;

			var drive = new DriveInfo(root);
			if (!drive.IsReady) return true;

			probe = drive.Name;
			free = drive.AvailableFreeSpace;
			// 留 32 MB 余量：exe 本身、快捷方式、日志都要占一点
			return free >= needed + 32L * 1024 * 1024;
		}
		catch
		{
			return true;   // 探测失败不阻断安装
		}
	}

	internal static string? NearestExisting(string path)
	{
		try
		{
			var p = Path.GetFullPath(path);
			while (!string.IsNullOrEmpty(p))
			{
				if (Directory.Exists(p)) return p;
				var parent = Path.GetDirectoryName(p);
				if (string.IsNullOrEmpty(parent) || string.Equals(parent, p, StringComparison.OrdinalIgnoreCase))
					break;
				p = parent;
			}
		}
		catch
		{
		}
		return null;
	}

	/// <summary>静默安装：无界面，结果全靠退出码 + 日志。</summary>
	private static SetupExit InstallSilent(SetupOptions opt)
	{
		var dir = string.IsNullOrWhiteSpace(opt.Dir) ? DefaultInstallDir : opt.Dir!;
		SetupLog.Write($"静默安装开始 dir={dir} desktop={opt.Desktop} startmenu={opt.StartMenu} startup={opt.Startup} launch={opt.Launch} ver={FullVersion}");

		var io = new InstallOptions
		{
			Desktop = opt.Desktop,
			StartMenu = opt.StartMenu,
			Startup = opt.Startup,
			Launch = opt.Launch,
		};

		var rc = Install(dir, io, null, null, out var files, out var error);

		if (rc == SetupExit.Ok)
		{
			SetupLog.Write($"静默安装成功 files={files}");
			if (opt.Launch)
			{
				try
				{
					Process.Start(new ProcessStartInfo(Path.Combine(dir, ExeName))
					{
						UseShellExecute = true,
						WorkingDirectory = dir,
					});
				}
				catch (Exception ex)
				{
					SetupLog.Write("启动失败：" + ex.Message);
				}
			}
		}
		else
		{
			SetupLog.Write($"静默安装失败 rc={(int)rc} {error}");
		}

		return rc;
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
					// ★ 不能只发 WM_CLOSE。程序默认「关闭时最小化到托盘」，
					//   CloseMainWindow() 会返回 true，但进程只是把窗口藏起来，
					//   文件锁一个都没松，覆盖安装照样失败（rc=5）。
					//   所以先礼貌关，关不掉就强制结束。
					p.CloseMainWindow();
					if (!p.WaitForExit(1200))
					{
						SetupLog.Write($"进程 {p.Id} 未响应关闭，强制结束");
						try
						{
							p.Kill(entireProcessTree: true);
						}
						catch
						{
							try { p.Kill(); } catch { }
						}
						p.WaitForExit(4000);
					}

					if (!p.HasExited)
						SetupLog.Write($"进程 {p.Id} 仍未能结束，接下来的写入可能失败");
				}
				catch (Exception ex)
				{
					SetupLog.Write("结束进程失败：" + ex.Message);
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
		var failed = ClearDirectoryEx(dir, keepFileName);
		foreach (var f in failed) SetupLog.Write("清理失败 " + f);
	}

	/// <summary>
	/// 清空目录并返回删不掉的项。
	/// ★ 以前把异常全吞掉、直接继续，结果是「文件没删掉 → 覆盖时又撞上占用 →
	///   用户只看到一句解压失败」。现在至少把失败项记下来，能定位到具体文件。
	/// </summary>
	internal static List<string> ClearDirectoryEx(string dir, string? keepFileName = null)
	{
		var failures = new List<string>();
		if (!Directory.Exists(dir)) return failures;

		try
		{
			foreach (var f in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
			{
				if (keepFileName is not null &&
					string.Equals(Path.GetFileName(f), keepFileName, StringComparison.OrdinalIgnoreCase))
					continue;
				try
				{
					File.SetAttributes(f, FileAttributes.Normal);
					File.Delete(f);
				}
				catch (Exception ex)
				{
					failures.Add($"{f} ({ex.GetType().Name})");
				}
			}
		}
		catch (Exception ex)
		{
			failures.Add($"{dir} 枚举失败 ({ex.GetType().Name})");
		}

		try
		{
			foreach (var d in Directory.EnumerateDirectories(dir).ToList())
			{
				try
				{
					Directory.Delete(d, recursive: true);
				}
				catch (Exception ex)
				{
					failures.Add($"{d} ({ex.GetType().Name})");
				}
			}
		}
		catch (Exception ex)
		{
			failures.Add($"{dir} 子目录枚举失败 ({ex.GetType().Name})");
		}

		return failures;
	}

	internal static void Extract(string dir, IProgress<int>? progress, out int fileCount)
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

			// ★ 防目录穿越。必须补尾部分隔符再比较：
			//   只比 "C:\Apps\App" 的话，"C:\Apps\AppEvil\x" 也会被判为在目录内。
			var root = Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			{
				SetupLog.Write("跳过越界条目：" + e.FullName);
				continue;
			}

			if (e.FullName.EndsWith("/", StringComparison.Ordinal))
			{
				Directory.CreateDirectory(full);
				continue;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(full)!);
			e.ExtractToFile(full, overwrite: true);
			fileCount++;
			done++;
			progress?.Report(done * 100 / total);
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
			var size = Directory.Exists(dir)
				? Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories).Sum(SafeLength) / 1024
				: 0;

			key.SetValue("DisplayName", AppName);
			key.SetValue("DisplayVersion", FullVersion);   // 含 beta 段，别再被抹成 1.0.0
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
		catch (Exception ex)
		{
			SetupLog.Write("写卸载注册表失败：" + ex.Message);
		}
	}

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

	private static SetupExit Uninstall(SetupOptions opt)
	{
		var silent = opt.Silent;
		var dir = opt.Dir ?? GetInstalledDir();

		if (dir is null)
		{
			// 没装过就明确返回 2，而不是「清空默认目录再报成功」——
			// 自动化脚本靠退出码判断，返回 0 会让「卸载成功」变成假象。
			SetupLog.Write("卸载：未检测到安装");
			if (!silent)
				MessageBox.Show($"没有检测到「{AppName}」的安装。", "卸载 - " + AppName,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			return SetupExit.NotInstalled;
		}

		// 显式指定了 /dir 但那个目录根本不存在 —— 同样是「未安装」，
		// 不能因为「删除一个不存在的目录也算成功」就返回 0。
		if (opt.Dir is not null && !Directory.Exists(dir))
		{
			SetupLog.Write($"卸载：指定的目录不存在 {dir}");
			if (!silent)
				MessageBox.Show($"没有检测到「{AppName}」的安装：\n{dir}", "卸载 - " + AppName,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			return SetupExit.NotInstalled;
		}

		if (!silent)
		{
			var r = MessageBox.Show(
				$"确定要卸载「{AppName}」吗？\n\n安装目录：{dir}",
				"卸载 - " + AppName,
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question,
				MessageBoxDefaultButton.Button2);
			if (r != DialogResult.Yes) return SetupExit.Cancelled;
		}

		KillRunningApp();
		SetStartup(false, dir);

		var dataDir = ResolveDataDir(opt.DataDir);
		var keepData = opt.PurgeData != true;
		if (!silent && opt.PurgeData is null && Directory.Exists(dataDir))
		{
			var r = MessageBox.Show(
				$"是否保留个人数据（收藏、设置、抽卡记录）？\n\n位置：{dataDir}\n\n选「是」保留，下次重装还在；选「否」一并删除。",
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
			// ★ /purge 是删除动作，必须能证明目标确实是数据目录。
			//   否则 /purge /datadir=C:\ 就是一颗地雷（路径写错就清盘）。
			if (Directory.Exists(dataDir) && !LooksLikeDataDir(dataDir))
			{
				SetupLog.Write($"拒绝删除 {dataDir}：不像本程序的数据目录");
				if (!silent)
					MessageBox.Show($"为安全起见已跳过删除：\n{dataDir}\n\n这个目录看起来不是本程序的数据目录。",
						"卸载 - " + AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			else
			{
				try
				{
					if (Directory.Exists(dataDir)) Directory.Delete(dataDir, recursive: true);
				}
				catch (Exception ex)
				{
					SetupLog.Write($"删除数据目录失败 {dataDir}: {ex.Message}");
					if (!silent)
						MessageBox.Show("部分数据文件删不掉（可能正在被占用）：\n" + ex.Message,
							"卸载 - " + AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}

		SetupLog.Write($"卸载完成 dir={dir} keepData={keepData} dataDir={dataDir}");

		if (!silent)
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
			catch (Exception ex)
			{
				SetupLog.Write("启动延迟删除失败：" + ex.Message);
			}
		}

		return SetupExit.Ok;
	}

	/// <summary>目标是否像本程序的数据目录（防止 /purge 打错路径删到别处）。</summary>
	private static bool LooksLikeDataDir(string dir)
	{
		var name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		if (!string.IsNullOrEmpty(name) &&
			name.StartsWith("AkPassport", StringComparison.OrdinalIgnoreCase))
			return true;

		return File.Exists(Path.Combine(dir, "preferences.json"))
			|| File.Exists(Path.Combine(dir, "shell.log"))
			|| Directory.Exists(Path.Combine(dir, "WebView2"));
	}
}

/// <summary>安装器退出码。自动化脚本靠它判断结果，不要随意改动已有数值。</summary>
internal enum SetupExit
{
	Ok = 0,
	Cancelled = 1,
	NotInstalled = 2,
	PayloadMissing = 3,
	DiskFull = 4,
	NotWritable = 5,
	Failed = 6,
}

/// <summary>命令行参数。可用写法：/silent、-silent、/quiet 都等价。</summary>
internal sealed class SetupOptions
{
	public bool Silent;
	public bool Uninstall;
	public string? Dir;
	public string? DataDir;
	public bool Desktop;
	public bool StartMenu;
	public bool Startup;
	public bool Launch;
	public bool? PurgeData;

	public static SetupOptions Parse(string[] args)
	{
		// 静默模式下默认不建快捷方式、不自启、不自动运行 —— 最保守，
		// 批量部署想要快捷方式就显式加 /desktop /startmenu。
		var o = new SetupOptions();

		for (var i = 0; i < args.Length; i++)
		{
			var raw = args[i].Trim();
			if (raw.Length == 0) continue;

			var body = raw.TrimStart('/', '-');
			string key;
			string? value = null;

			var eq = body.IndexOf('=');
			if (eq >= 0)
			{
				key = body[..eq].Trim().ToLowerInvariant();
				value = Unquote(body[(eq + 1)..].Trim());
			}
			else
			{
				key = body.ToLowerInvariant();
			}

			// 支持 "/dir C:\Path" 这种用空格分隔的写法
			if (value is null && (key == "dir" || key == "datadir") && i + 1 < args.Length)
			{
				var next = args[i + 1].Trim();
				if (next.Length > 0 && next[0] != '/' && next[0] != '-')
				{
					value = Unquote(next);
					i++;
				}
			}

			switch (key)
			{
				case "silent":
				case "s":
				case "quiet":
					o.Silent = true;
					break;
				case "uninstall":
					o.Uninstall = true;
					break;
				case "dir":
					o.Dir = value;
					break;
				case "datadir":
					o.DataDir = value;
					break;
				case "desktop":
					o.Desktop = true;
					break;
				case "startmenu":
				case "start":
					o.StartMenu = true;
					break;
				case "nodekstop":
					o.Desktop = false;
					break;
				case "nostartmenu":
					o.StartMenu = false;
					break;
				case "startup":
				case "autostart":
					o.Startup = true;
					break;
				case "launch":
					o.Launch = true;
					break;
				case "purge":
					o.PurgeData = true;
					break;
				case "keepdata":
					o.PurgeData = false;
					break;
			}
		}

		return o;
	}

	private static string Unquote(string s)
	{
		if (s.Length >= 2 && s[0] == '"' && s[^1] == '"') return s[1..^1];
		if (s.Length >= 2 && s[0] == '\'' && s[^1] == '\'') return s[1..^1];
		return s;
	}
}

/// <summary>
/// 安装器的日志。放在数据目录之外 —— 卸载（含 /purge）后仍然存在，
/// 装失败时用户能把这一份直接发过来。
/// </summary>
internal static class SetupLog
{
	private static readonly object Gate = new();

	internal static string FilePath =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"AkPassport-setup.log");

	internal static void Write(string message)
	{
		try
		{
			lock (Gate)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
				File.AppendAllText(FilePath,
					$"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
			}
		}
		catch
		{
			// 日志失败绝不能影响安装本身
		}
	}
}
