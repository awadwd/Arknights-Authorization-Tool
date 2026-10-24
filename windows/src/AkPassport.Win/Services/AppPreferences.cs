using System;
using System.IO;
using System.Text.Json;

namespace AkPassport.Win.Services;

public sealed class AppPreferences
{
	public bool CloseToTray { get; set; } = true;
	public bool StartWithWindows { get; set; }
	public bool TrayEnabled { get; set; } = true;
	public bool HotkeyEnabled { get; set; } = true;

	/// <summary>是否走完过「首次设置向导」。走完（或点过「稍后再说」）就不再自动弹。</summary>
	public bool SetupCompleted { get; set; }

	/// <summary>启动后是否自动检查程序更新（只查，不问就下载）。</summary>
	public bool AutoCheckUpdate { get; set; } = true;

	/// <summary>发现新版本后是否直接开始下载（默认关：让用户自己点）。</summary>
	public bool AutoDownloadUpdate { get; set; }

	/// <summary>下载线路：auto / github / cdn。</summary>
	public string UpdateRoute { get; set; } = "auto";

	/// <summary>用户点过「跳过此版本」的 tag，不再提示。</summary>
	public string SkippedVersion { get; set; } = "";

	/// <summary>上次用的工具包：<c>ak</c> = 方舟通行证，<c>ae</c> = 终末地。启动时按它落到对应首页。</summary>
	public string LastMode { get; set; } = "ak";

	private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

	public static AppPreferences Load()
	{
		try
		{
			if (File.Exists(AppPaths.Preferences))
			{
				return JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(AppPaths.Preferences))
					?? new AppPreferences();
			}
		}
		catch
		{
			// 配置损坏就回到默认值，不要让应用起不来
		}
		return new AppPreferences();
	}

	public void Save()
	{
		try
		{
			AppPaths.EnsureCreated();
			File.WriteAllText(AppPaths.Preferences, JsonSerializer.Serialize(this, Json));
		}
		catch
		{
		}
	}
}
