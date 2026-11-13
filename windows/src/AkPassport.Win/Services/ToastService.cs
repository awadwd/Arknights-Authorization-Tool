using System;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace AkPassport.Win.Services;

/// <summary>
/// Windows 系统通知（Toast）。
///
/// 为什么不在页面里用 Web Notification：页面跑在 WebView2 里，Notification
/// 权限请求没有可用的宿主 UI，Chromium 会直接判 denied，new Notification() 静默失败。
/// 真正的系统级弹窗只能由外壳出，所以前端用 postMessage 发过来、这里再交给
/// Windows App SDK 的 AppNotificationManager。
///
/// 本工程是 unpackaged（WindowsPackageType=None）。AppNotificationManager.Register()
/// 在 WinAppSDK 1.2+ 下会自动登记 AUMID，但要求安装过开始菜单快捷方式（安装器会建），
/// 所以这里全部 try/catch：注册失败就当没这功能，不能把主窗口带崩。
/// </summary>
public static class ToastService
{
	private static bool _registered;
	private static readonly object Gate = new();

	public static bool EnsureRegistered()
	{
		if (_registered) return true;
		lock (Gate)
		{
			if (_registered) return true;
			try
			{
				AppNotificationManager.Default.Register();
				_registered = true;
				Diag.Log("toast: registered");
				return true;
			}
			catch (Exception ex)
			{
				Diag.Log("toast register failed: " + ex.Message);
				return false;
			}
		}
	}

	public static bool Show(string title, string body)
	{
		try
		{
			if (!EnsureRegistered()) return false;

			var notification = new AppNotificationBuilder()
				.AddText(string.IsNullOrWhiteSpace(title) ? "方舟通行证谷子查询工具" : title)
				.AddText(body ?? "")
				.BuildNotification();

			AppNotificationManager.Default.Show(notification);
			Diag.Log("toast shown: " + title);
			return true;
		}
		catch (Exception ex)
		{
			Diag.Log("toast show failed: " + ex.Message);
			return false;
		}
	}
}
