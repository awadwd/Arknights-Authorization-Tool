using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AkPassport.Win.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Windows.Graphics;

namespace AkPassport.Win.Views;

/// <summary>
/// 「关于我们」独立窗口。
///
/// 为什么不复用主窗口里的 WebView：关于页是竖屏排版的 H5 页面（一堆纵向卡片、
/// GitHub 统计、"服务支持"入口），塞进 1000px 宽的桌面内容区会拉成一条很尴尬的长条。
/// 这里开一个 420×880 的小窗，直接复用 packageA/aboutus 那份 Vue 页面 ——
/// 内容只有一份，改关于页不用同步两个地方。
///
/// 窗口是不可调整大小、不可最大化，位置居中于主窗口。
/// </summary>
public sealed partial class AboutWindow : Window
{
	private const string Host = "app.local";
	private const string Route = "packageA/aboutus/aboutus";

	/// <summary>按逻辑像素设计，实际尺寸再乘 DPI 缩放。</summary>
	private const int LogicalWidth = 420;
	private const int LogicalHeight = 880;

	private bool _themeApplied;
	private bool _dark;

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(IntPtr hwnd);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);

	[StructLayout(LayoutKind.Sequential)]
	private struct Rect
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}

	internal AboutWindow(Window owner, ShellTheme theme)
	{
		InitializeComponent();
		ApplyTheme(theme);
		SetupWindow(owner);
		_ = InitWebViewAsync();
	}

	/// <summary>窗口开着时用户又改了主题：窗口配色跟着换，页面内容也要重载才会重读 themeMode。</summary>
	internal void ApplyTheme(ShellTheme theme)
	{
		var flip = _themeApplied && _dark != theme.Dark;
		_themeApplied = true;
		_dark = theme.Dark;

		Root.RequestedTheme = theme.Dark ? ElementTheme.Dark : ElementTheme.Light;
		Root.Background = new SolidColorBrush(theme.Canvas);
		Splash.Background = new SolidColorBrush(theme.Canvas);

		try
		{
			var tb = AppWindow.TitleBar;
			tb.ButtonBackgroundColor = Colors.Transparent;
			tb.ButtonInactiveBackgroundColor = Colors.Transparent;
			tb.ButtonForegroundColor = theme.Text;
		}
		catch
		{
			// 个别系统版本上标题栏还不可用，忽略即可
		}

		if (flip)
		{
			try { Web.CoreWebView2?.Reload(); } catch { }
		}
	}

	private void SetupWindow(Window owner)
	{
		var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
		var dpi = GetDpiForWindow(hwnd);
		var scale = dpi > 0 ? dpi / 96.0 : 1.0;

		var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
		var width = (int)(LogicalWidth * scale);
		var height = Math.Min((int)(LogicalHeight * scale), (int)(area.Height * 0.94));
		AppWindow.Resize(new SizeInt32(width, height));

		var ownerHwnd = WinRT.Interop.WindowNative.GetWindowHandle(owner);
		if (GetWindowRect(ownerHwnd, out var r))
		{
			var ox = r.Left + (r.Right - r.Left - width) / 2;
			var oy = r.Top + (r.Bottom - r.Top - height) / 2;
			AppWindow.Move(new PointInt32(
				Math.Clamp(ox, area.X, Math.Max(area.X, area.X + area.Width - width)),
				Math.Clamp(oy, area.Y, Math.Max(area.Y, area.Y + area.Height - height))));
		}

		if (AppWindow.Presenter is OverlappedPresenter presenter)
		{
			presenter.IsResizable = false;
			presenter.IsMaximizable = false;
		}
	}

	private async Task InitWebViewAsync()
	{
		try
		{
			await Web.EnsureCoreWebView2Async();
			var core = Web.CoreWebView2;

			core.Settings.AreDefaultContextMenusEnabled = false;
			core.Settings.IsStatusBarEnabled = false;
			core.Settings.IsZoomControlEnabled = false;
			core.Settings.IsPinchZoomEnabled = false;
			core.Settings.AreDevToolsEnabled = false;

			core.SetVirtualHostNameToFolderMapping(Host, AppPaths.WebRoot,
				CoreWebView2HostResourceAccessKind.Allow);

			// 页面里的外链（GitHub / 问卷）交给系统浏览器，不在这个小窗里打开
			// 注意：lambda 参数不能叫 _，否则会和里面的弃元 _ = 抢名字（编译报 CoreWebView2 / IAsyncOperation 不匹配）
			core.NewWindowRequested += (sender, e) =>
			{
				e.Handled = true;
				try { _ = Windows.System.Launcher.LaunchUriAsync(new Uri(e.Uri)); } catch { }
			};
			core.NavigationCompleted += (_, e) =>
			{
				if (e.IsSuccess) Splash.Visibility = Visibility.Collapsed;
			};

			// 420px 宽 ≈ 手机竖屏：根字号 18px 正好让 750rpx 铺满窗口
			await core.AddScriptToExecuteOnDocumentCreatedAsync(DesktopPatches.ScaleScript);
			await core.AddScriptToExecuteOnDocumentCreatedAsync(DesktopPatches.ZoomGuardScript);
			// 独立小窗只有这一页，uni 自带的 44px 标题栏（带返回箭头）没有意义
			await core.AddScriptToExecuteOnDocumentCreatedAsync(HidePageHeadScript);

			Web.Source = new Uri(BuildUrl());
		}
		catch (Exception ex)
		{
			Diag.Log("about window init failed: " + ex);
			Splash.Visibility = Visibility.Collapsed;
		}
	}

	/// <summary>小窗里不需要 uni 的页面标题栏：窗口标题已经写了「关于我们」。</summary>
	private const string HidePageHeadScript =
		"(function(){var css='uni-page-head{display:none!important}uni-page-body{top:0!important}';" +
		"function apply(){try{if(document.getElementById('ak-about-flat'))return;" +
		"var s=document.createElement('style');s.id='ak-about-flat';s.textContent=css;" +
		"var host=document.head||document.documentElement;if(host)host.appendChild(s);}catch(e){}}" +
		"document.addEventListener('DOMContentLoaded',apply);" +
		"window.addEventListener('load',apply);apply();})();";

	private static string BuildUrl()
	{
		var stamp = "0";
		try
		{
			var index = Path.Combine(AppPaths.WebRoot, "index.html");
			if (File.Exists(index))
				stamp = File.GetLastWriteTimeUtc(index).Ticks.ToString();
		}
		catch
		{
			// 取不到时间戳就不带版本号
		}
		return $"https://{Host}/index.html?v={stamp}#/{Route}";
	}
}
