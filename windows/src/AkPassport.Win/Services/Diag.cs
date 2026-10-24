using System;
using System.IO;

namespace AkPassport.Win.Services;

/// <summary>轻量诊断日志，出问题时可查 %LOCALAPPDATA%\AkPassport\shell.log。</summary>
public static class Diag
{
	private static readonly object Gate = new();

	public static void Log(string message)
	{
		try
		{
			AppPaths.EnsureCreated();
			var line = $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}";
			lock (Gate)
			{
				File.AppendAllText(Path.Combine(AppPaths.DataRoot, "shell.log"), line);
			}
		}
		catch
		{
			// 日志失败不影响主流程
		}
	}
}
