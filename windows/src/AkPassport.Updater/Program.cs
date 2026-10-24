using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace AkPassport.Updater;

/// <summary>
/// 自动更新器：等主程序退出 -> 解压新版本 -> 备份 -> 覆盖 -> 重启。
///
/// 参数（全部由主程序传入，不接受任何网络输入）：
///   --pid &lt;进程号&gt;       要等待退出的主进程
///   --zip &lt;包路径&gt;       已下载并校验过的 zip
///   --dir &lt;安装目录&gt;     要覆盖到的目录
///   --exe &lt;主程序名&gt;     更新完成后要启动的 exe（相对 --dir）
///   --log &lt;日志路径&gt;     可选
///   --wait &lt;秒&gt;          可选，等主进程退出的上限，默认 60
/// </summary>
internal static class Program
{
	/// <summary>覆盖时要跳过的文件名：自己正在运行，写不进去。</summary>
	private const string SelfName = "AkPassport.Updater.exe";

	private static readonly string UpdateDirName = ".update";

	private static async Task<int> Main(string[] args)
	{
		var opt = Options.Parse(args);
		var log = new Log(opt.LogPath);

		if (opt.ShowHelp)
		{
			Console.WriteLine(Options.HelpText);
			return 0;
		}

		if (opt.Error is not null)
		{
			log.Write("参数错误: " + opt.Error);
			Console.Error.WriteLine(opt.Error);
			return 2;
		}

		log.Write($"updater 启动 pid={Environment.ProcessId} parent={opt.Pid} zip={opt.ZipPath} dir={opt.Dir}");

		try
		{
			await WaitForParentExitAsync(opt.Pid, opt.WaitSeconds, log);

			var updateRoot = Path.Combine(opt.Dir, UpdateDirName);
			var staging = Path.Combine(updateRoot, "staging");
			var backup = Path.Combine(updateRoot, "backup");

			PrepareEmptyDir(staging);
			PrepareEmptyDir(backup);

			log.Write("解压 -> " + staging);
			ZipFile.ExtractToDirectory(opt.ZipPath, staging, overwriteFiles: true);

			// 防呆：确认解出来的确实是这个程序，避免拿错包把安装目录洗掉
			var probe = Path.Combine(staging, opt.ExeName);
			if (!File.Exists(probe))
			{
				log.Write("包内容不对：缺少 " + opt.ExeName);
				Cleanup(staging);
				return 3;
			}

			var copied = ApplyUpdate(staging, opt.Dir, backup, log);
			log.Write($"覆盖完成，共 {copied} 个文件");

			Cleanup(staging);

			var exePath = Path.Combine(opt.Dir, opt.ExeName);
			log.Write("启动新版本: " + exePath);
			Process.Start(new ProcessStartInfo
			{
				FileName = exePath,
				WorkingDirectory = opt.Dir,
				UseShellExecute = true,
			});

			SelfDelete(log);
			return 0;
		}
		catch (Exception ex)
		{
			log.Write("失败: " + ex);

			// 覆盖到一半失败：备份里有原文件就放回去，尽量让用户还能打开旧版本
			try
			{
				var backup = Path.Combine(opt.Dir, UpdateDirName, "backup");
				if (Directory.Exists(backup))
				{
					RestoreBackup(backup, opt.Dir, log);
					log.Write("已回滚到更新前");
				}
			}
			catch (Exception rex)
			{
				log.Write("回滚失败: " + rex.Message);
			}

			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = Path.Combine(opt.Dir, opt.ExeName),
					WorkingDirectory = opt.Dir,
					UseShellExecute = true,
				});
			}
			catch { }

			SelfDelete(log);
			return 1;
		}
	}

	private static async Task WaitForParentExitAsync(int pid, int seconds, Log log)
	{
		if (pid <= 0) return;

		Process p;
		try
		{
			p = Process.GetProcessById(pid);
		}
		catch (ArgumentException)
		{
			log.Write("主进程已经退出");
			return;
		}

		log.Write($"等待主进程 {pid} 退出（最多 {seconds}s）");
		var done = await Task.Run(() => p.WaitForExit(seconds * 1000));
		log.Write(done ? "主进程已退出" : "等待超时，继续尝试覆盖");

		// 再给系统一点时间释放文件句柄（WebView2 相关 dll 尤其慢）
		if (done) await Task.Delay(700);
	}

	private static int ApplyUpdate(string staging, string targetDir, string backupDir, Log log)
	{
		var count = 0;

		foreach (var src in Directory.EnumerateFiles(staging, "*", SearchOption.AllDirectories))
		{
			var rel = Path.GetRelativePath(staging, src);

			// 自己不覆盖自己：进程还在跑，会抛占用错误
			if (string.Equals(Path.GetFileName(rel), SelfName, StringComparison.OrdinalIgnoreCase))
				continue;

			var dst = Path.Combine(targetDir, rel);

			if (File.Exists(dst))
			{
				var bak = Path.Combine(backupDir, rel);
				Directory.CreateDirectory(Path.GetDirectoryName(bak)!);
				File.Copy(dst, bak, overwrite: true);
				File.SetAttributes(dst, FileAttributes.Normal);
			}

			Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
			File.Copy(src, dst, overwrite: true);
			count++;
		}

		return count;
	}

	private static void RestoreBackup(string backupDir, string targetDir, Log log)
	{
		foreach (var src in Directory.EnumerateFiles(backupDir, "*", SearchOption.AllDirectories))
		{
			var rel = Path.GetRelativePath(backupDir, src);
			var dst = Path.Combine(targetDir, rel);

			Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
			File.Copy(src, dst, overwrite: true);
		}
		log.Write("回滚完成");
	}

	private static void PrepareEmptyDir(string dir)
	{
		try
		{
			if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
		}
		catch
		{
			// 删不掉就换个新名字，避免和上一轮残留混在一起
			dir += "-" + DateTime.Now.ToString("HHmmss");
		}
		Directory.CreateDirectory(dir);
	}

	private static void Cleanup(string staging)
	{
		try { Directory.Delete(staging, recursive: true); } catch { }
	}

	/// <summary>更新器自己的 exe 要在退出后才能删，交给 cmd 延迟执行。</summary>
	private static void SelfDelete(Log log)
	{
		try
		{
			var self = Environment.ProcessPath;
			if (string.IsNullOrEmpty(self)) return;

			Process.Start(new ProcessStartInfo
			{
				FileName = "cmd.exe",
				Arguments = $"/c ping -n 3 127.0.0.1 >nul & del /f /q \"{self}\"",
				CreateNoWindow = true,
				UseShellExecute = false,
			});
			log.Write("已安排自删: " + self);
		}
		catch (Exception ex)
		{
			log.Write("自删安排失败: " + ex.Message);
		}
	}
}

internal sealed class Options
{
	public int Pid { get; private set; }
	public string ZipPath { get; private set; } = "";
	public string Dir { get; private set; } = "";
	public string ExeName { get; private set; } = "AkPassport.Win.exe";
	public string? LogPath { get; private set; }
	public int WaitSeconds { get; private set; } = 60;
	public bool ShowHelp { get; private set; }
	public string? Error { get; private set; }

	public const string HelpText =
		"AkPassport.Updater --pid <进程号> --zip <包路径> --dir <安装目录> " +
		"[--exe <主程序名>] [--log <日志路径>] [--wait <秒>]";

	public static Options Parse(string[] args)
	{
		var o = new Options();

		for (var i = 0; i < args.Length; i++)
		{
			var key = args[i];
			if (key is "--help" or "-h" or "/?")
			{
				o.ShowHelp = true;
				return o;
			}

			if (i + 1 >= args.Length)
			{
				o.Error = "参数缺少值: " + key;
				return o;
			}

			var val = args[++i];
			switch (key)
			{
				case "--pid":
					if (!int.TryParse(val, out var pid)) { o.Error = "--pid 不是数字"; return o; }
					o.Pid = pid;
					break;

				case "--zip": o.ZipPath = val; break;
				case "--dir": o.Dir = val; break;
				case "--exe": o.ExeName = val; break;
				case "--log": o.LogPath = val; break;

				case "--wait":
					if (!int.TryParse(val, out var s)) { o.Error = "--wait 不是数字"; return o; }
					o.WaitSeconds = Math.Clamp(s, 5, 300);
					break;

				default:
					o.Error = "未知参数: " + key;
					return o;
			}
		}

		if (string.IsNullOrWhiteSpace(o.ZipPath) || !File.Exists(o.ZipPath))
			o.Error = "找不到更新包: " + o.ZipPath;
		else if (string.IsNullOrWhiteSpace(o.Dir) || !Directory.Exists(o.Dir))
			o.Error = "找不到安装目录: " + o.Dir;

		return o;
	}
}

internal sealed class Log
{
	private readonly string? _path;
	private readonly object _gate = new();

	public Log(string? path) => _path = path;

	public void Write(string message)
	{
		var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}  {message}";
		Console.WriteLine(line);

		if (string.IsNullOrEmpty(_path)) return;

		try
		{
			lock (_gate)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
				File.AppendAllText(_path, line + Environment.NewLine, Encoding.UTF8);
			}
		}
		catch
		{
			// 日志写不进去不影响更新本身
		}
	}
}
