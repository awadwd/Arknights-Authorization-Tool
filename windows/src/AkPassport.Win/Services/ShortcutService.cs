using System;
using System.IO;

namespace AkPassport.Win.Services;

/// <summary>
/// 桌面快捷方式（.lnk）。
/// 用 WScript.Shell 的 COM 接口生成，不依赖 PowerShell（本机 PowerShell 有执行策略与沙箱限制）。
/// 非打包应用（WindowsPackageType=None）没有 MSIX 的图标注册，快捷方式是唯一像样的入口。
/// </summary>
public static class ShortcutService
{
	public static string DesktopPath => ResolveDesktop();

	/// <summary>
	/// 桌面路径。`SpecialFolder.DesktopDirectory` 在某些环境（OneDrive 重定向、
	/// 精简过的系统镜像）会返回空串，Path.Combine 之后就变成相对路径，
	/// WScript.Shell 会把 .lnk 建到开始菜单里 —— 用户根本找不到。
	/// </summary>
	private static string ResolveDesktop()
	{
		var d = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
		if (!string.IsNullOrWhiteSpace(d) && Directory.Exists(d)) return d;

		var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var fallback = Path.Combine(profile, "Desktop");
		if (!string.IsNullOrWhiteSpace(profile) && Directory.Exists(fallback)) return fallback;

		return AppContext.BaseDirectory;
	}

	public static string PathFor(string displayName) =>
		Path.Combine(DesktopPath, displayName + ".lnk");

	public static bool Exists(string displayName) => File.Exists(PathFor(displayName));

	/// <summary>开始菜单 «所有应用» 里的入口。与桌面快捷方式分开：这个是「安装动作」，默认必建。</summary>
	public static string StartMenuPathFor(string displayName) =>
		Path.Combine(ProgramsPath(), displayName + ".lnk");

	public static bool ExistsInStartMenu(string displayName) => File.Exists(StartMenuPathFor(displayName));

	private static string ProgramsPath()
	{
		var p = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
		if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p)) return p;

		var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		return Path.Combine(appData, @"Microsoft\Windows\Start Menu\Programs");
	}

	/// <summary>创建/覆盖开始菜单快捷方式。返回是否成功。</summary>
	public static bool CreateStartMenu(string displayName) =>
		CreateAt(StartMenuPathFor(displayName), displayName, "startmenu");

	/// <summary>创建/覆盖桌面快捷方式；失败只写日志，不打断向导。</summary>
	public static bool Create(string displayName) => CreateAt(PathFor(displayName), displayName, "desktop");

	private static bool CreateAt(string path, string displayName, string tag)
	{
		try
		{
			var exe = Environment.ProcessPath;
			if (string.IsNullOrEmpty(exe)) return false;

			var type = Type.GetTypeFromProgID("WScript.Shell");
			if (type is null) return false;

			dynamic shell = Activator.CreateInstance(type)!;
			dynamic link = shell.CreateShortcut(path);
			link.TargetPath = exe;
			link.WorkingDirectory = AppContext.BaseDirectory;
			link.Description = displayName;
			var ico = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
			if (File.Exists(ico)) link.IconLocation = ico + ",0";
			link.Save();

			var ok = File.Exists(path);
			Diag.Log($"shortcut({tag}): path={path} ok={ok}");
			return ok;
		}
		catch (Exception ex)
		{
			Diag.Log($"create shortcut({tag}) failed: " + ex.Message);
			return false;
		}
	}
}
