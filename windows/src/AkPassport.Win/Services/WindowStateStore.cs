using System;
using System.IO;
using System.Text.Json;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace AkPassport.Win.Services;

public sealed record WindowStateData(int X, int Y, int Width, int Height, bool Maximized);

/// <summary>窗口位置与尺寸记忆，多显示器拔插后做一次可见性校验。</summary>
public static class WindowStateStore
{
	private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

	public static WindowStateData? Load()
	{
		try
		{
			if (!File.Exists(AppPaths.WindowState)) return null;
			var data = JsonSerializer.Deserialize<WindowStateData>(File.ReadAllText(AppPaths.WindowState));
			if (data is null || data.Width < 300 || data.Height < 400) return null;
			return data;
		}
		catch
		{
			return null;
		}
	}

	public static void Save(WindowStateData data)
	{
		try
		{
			AppPaths.EnsureCreated();
			File.WriteAllText(AppPaths.WindowState, JsonSerializer.Serialize(data, Json));
		}
		catch
		{
			// 记忆窗口位置失败不该影响使用
		}
	}

	/// <summary>判断记录的矩形是否还落在某块屏幕里（防止改分辨率后窗口跑到屏幕外）。</summary>
	public static bool IsVisible(WindowStateData data)
	{
		try
		{
			var area = DisplayArea.GetFromPoint(
				new PointInt32(data.X + 40, data.Y + 40), DisplayAreaFallback.None);
			return area is not null;
		}
		catch
		{
			return false;
		}
	}
}
