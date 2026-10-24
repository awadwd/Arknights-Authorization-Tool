using System;
using System.Threading.Tasks;
using AkPassport.Win.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AkPassport.Win.Views;

/// <summary>
/// 首次运行设置向导。
///
/// 用 ContentDialog 而不是独立窗口：主窗口已经按主题渲染好了，这里直接继承
/// 同一套 WinUI 控件与配色，不用再手写一遍浅色/深色。
/// 勾选项写的是「真开关」，不是装饰：桌面快捷方式会立刻创建，开机自启写注册表，
/// 自动更新写前端的 localStorage（autoUpdateSetting）。
/// </summary>
internal static class SetupWizard
{
	internal sealed record Choice(
		bool Accepted,
		bool CreateShortcut,
		bool AutoUpdate,
		bool StartWithWindows,
		bool CloseToTray,
		bool TrayIcon,
		bool Hotkey);

	public static async Task<Choice?> ShowAsync(XamlRoot root, bool dark, AppPreferences prefs)
	{
		var shortcut = Item("创建桌面快捷方式", "在桌面放一个启动图标，不用每次去文件夹里找", true);
		var autoUpdate = Item("启动时自动更新数据", "每 24 小时静默检查盒号 / 干员数据，发现新版自动拉取", true);
		var startup = Item("开机自动启动", "登录 Windows 后直接在托盘运行，不弹主窗口", false);
		var tray = Item("关闭窗口时收进托盘", "点关闭不退出程序，从右下角托盘图标恢复", prefs.CloseToTray);
		var hotkey = Item("全局快捷键 Ctrl + Alt + A", "任何界面下按快捷键都能唤出窗口", prefs.HotkeyEnabled);

		var panel = new StackPanel { Spacing = 6, MinWidth = 380 };
		panel.Children.Add(new TextBlock
		{
			Text = "方舟通行证谷子查询工具",
			FontSize = 18,
			FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
		});
		panel.Children.Add(new TextBlock
		{
			Text = "花 30 秒把常用开关定下来，之后随时能在「设置」里改。",
			FontSize = 12,
			Opacity = 0.6,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0, 0, 0, 8),
		});

		foreach (var row in new[] { shortcut, autoUpdate, startup, tray, hotkey })
		{
			panel.Children.Add(row);
		}

		panel.Children.Add(new Border
		{
			Height = 1,
			Margin = new Thickness(0, 8, 0, 8),
			Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
			Opacity = 0.35,
		});
		panel.Children.Add(new TextBlock
		{
			Text = "开始菜单快捷方式会自动创建（在「所有应用」里搜程序名即可）。勾选项随时可改；「关闭窗口时收进托盘」开启时会同时保留托盘图标，否则关掉窗口就找不回来了。",
			FontSize = 11,
			Opacity = 0.55,
			TextWrapping = TextWrapping.Wrap,
		});

		var dialog = new ContentDialog
		{
			XamlRoot = root,
			RequestedTheme = dark ? ElementTheme.Dark : ElementTheme.Light,
			Title = "欢迎使用",
			Content = new ScrollViewer { Content = panel, MaxHeight = 460, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
			PrimaryButtonText = "开始使用",
			CloseButtonText = "稍后再说",
			DefaultButton = ContentDialogButton.Primary,
		};

		var result = await dialog.ShowAsync();
		if (result != ContentDialogResult.Primary)
		{
			// 点「稍后再说」也算走完，不再每次启动纠缠
			return new Choice(false, false, false, false, prefs.CloseToTray, prefs.TrayEnabled, prefs.HotkeyEnabled);
		}

		var closeToTray = tray.IsChecked == true;
		return new Choice(
			Accepted: true,
			CreateShortcut: shortcut.IsChecked == true,
			AutoUpdate: autoUpdate.IsChecked == true,
			StartWithWindows: startup.IsChecked == true,
			CloseToTray: closeToTray,
			// 收进托盘必须同时有托盘图标，否则窗口会「消失且无法找回」
			TrayIcon: closeToTray || prefs.TrayEnabled || tray.IsChecked == true,
			Hotkey: hotkey.IsChecked == true);
	}

	private static CheckBox Item(string title, string desc, bool isOn)
	{
		var text = new StackPanel { Spacing = 1 };
		text.Children.Add(new TextBlock { Text = title, FontSize = 13.5 });
		text.Children.Add(new TextBlock
		{
			Text = desc,
			FontSize = 11.5,
			Opacity = 0.58,
			TextWrapping = TextWrapping.Wrap,
		});

		return new CheckBox
		{
			Content = text,
			IsChecked = isOn,
			Margin = new Thickness(0, 2, 0, 2),
		};
	}
}
