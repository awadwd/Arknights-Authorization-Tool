using System;
using System.IO;

namespace AkPassport.Win.Services;

/// <summary>集中管理所有落盘路径，避免散落各处的字符串。</summary>
public static class AppPaths
{
	/// <summary>%LOCALAPPDATA%\AkPassport</summary>
	public static string DataRoot { get; } = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"AkPassport");

	/// <summary>H5 产物目录（随程序一起分发）。</summary>
	public static string WebRoot { get; } = Path.Combine(AppContext.BaseDirectory, "wwwroot");

	/// <summary>WebView2 用户数据目录。必须固定，否则升级后 localStorage 会丢。</summary>
	public static string WebViewData { get; } = Path.Combine(DataRoot, "WebView2");

	/// <summary>窗口位置/尺寸。</summary>
	public static string WindowState { get; } = Path.Combine(DataRoot, "window.json");

	/// <summary>桌面偏好（关闭到托盘、开机自启等）。</summary>
	public static string Preferences { get; } = Path.Combine(DataRoot, "preferences.json");

	/// <summary>更新包下载目录（zip 落在这里再交给更新器）。</summary>
	public static string UpdateDir { get; } = Path.Combine(DataRoot, "update");

	/// <summary>更新器日志。更新过程主程序已经退出，只能靠它留痕。</summary>
	public static string UpdateLog { get; } = Path.Combine(DataRoot, "update.log");

	public static void EnsureCreated()
	{
		Directory.CreateDirectory(DataRoot);
		Directory.CreateDirectory(WebViewData);
	}

	public static void EnsureUpdateDir()
	{
		Directory.CreateDirectory(UpdateDir);
	}
}
