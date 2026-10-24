using System;
using System.IO;
using System.Windows.Input;
using H.NotifyIcon;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AkPassport.Win.Services;

/// <summary>托盘图标：左键唤出窗口，右键菜单提供显示与退出。</summary>
public sealed class TrayService : IDisposable
{
	private readonly TaskbarIcon _icon;
	private readonly System.Drawing.Icon? _hIcon;

	public TrayService(Action show, Action exit)
	{
		_icon = new TaskbarIcon
		{
			ToolTipText = "方舟通行证谷子查询工具",
			LeftClickCommand = new RelayCommand(show),
			NoLeftClickDelay = true,
		};

		// 托盘要的是 HICON，不是 XAML 的 ImageSource。
		// 之前用 BitmapImage 赋给 IconSource：它是**异步**解码的，而下面紧跟着 ForceCreate()，
		// 创建托盘项时位图还没解码完 → 系统拿到空图标 → 托盘区只剩一个看不见的占位。
		// 直接从 .ico 同步构造 Icon，创建时就有真实句柄。
		var ico = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
		if (File.Exists(ico))
		{
			try
			{
				_hIcon = new System.Drawing.Icon(ico);
				_icon.Icon = _hIcon;
			}
			catch (Exception ex)
			{
				Diag.Log("tray: 载入 app.ico 失败 " + ex.Message);
			}
		}

		// 兜底：没有 .ico 时才退回 PNG（同样可能异步，仅保证不崩）
		if (_icon.Icon is null)
		{
			var png = Path.Combine(AppContext.BaseDirectory, "Assets", "tray.png");
			if (File.Exists(png))
			{
				_icon.IconSource = new BitmapImage(new Uri("file:///" + png.Replace('\\', '/')));
			}
		}

		var menu = new MenuFlyout();
		var open = new MenuFlyoutItem { Text = "显示主窗口" };
		open.Click += (_, _) => show();
		var quit = new MenuFlyoutItem { Text = "退出" };
		quit.Click += (_, _) => exit();
		menu.Items.Add(open);
		menu.Items.Add(new MenuFlyoutSeparator());
		menu.Items.Add(quit);

		_icon.ContextFlyout = menu;
		_icon.ForceCreate();
	}

	public void Dispose()
	{
		try
		{
			_icon.Dispose();
		}
		catch
		{
		}

		_hIcon?.Dispose();
	}
}

internal sealed class RelayCommand(Action execute) : ICommand
{
	public event EventHandler? CanExecuteChanged;

	public bool CanExecute(object? parameter) => true;

	public void Execute(object? parameter) => execute();
}
