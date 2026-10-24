using System;
using Microsoft.Win32;

namespace AkPassport.Win.Services;

/// <summary>开机自启：写 HKCU 的 Run 键，不写计划任务，避免要管理员权限。</summary>
public static class StartupService
{
	private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
	private const string ValueName = "AkPassportWin";

	public static bool IsEnabled()
	{
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(RunKey);
			return key?.GetValue(ValueName) is string;
		}
		catch
		{
			return false;
		}
	}

	public static void SetEnabled(bool enabled)
	{
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
			if (key is null) return;

			if (enabled)
			{
				var exe = Environment.ProcessPath;
				if (!string.IsNullOrEmpty(exe))
				{
					// --tray 表示静默启动到托盘，不闪主窗口
					key.SetValue(ValueName, $"\"{exe}\" --tray");
				}
			}
			else
			{
				key.DeleteValue(ValueName, throwOnMissingValue: false);
			}
		}
		catch
		{
		}
	}
}
