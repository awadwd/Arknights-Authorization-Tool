using System;
using System.Linq;
using AkPassport.Win.Services;
using Microsoft.UI.Xaml;

namespace AkPassport.Win;

public partial class App : Application
{
	private SingleInstance? _single;
	private MainWindow? _window;

	public App()
	{
		InitializeComponent();
	}

	protected override void OnLaunched(LaunchActivatedEventArgs args)
	{
		// 字体要在任何窗口/XAML 元素创建之前定下来（隐式 TextBlock 样式只影响之后创建的元素）
		FontService.Initialize();

		var commandLine = Environment.GetCommandLineArgs();

		var startHidden = commandLine.Any(a => string.Equals(a, "--tray", StringComparison.OrdinalIgnoreCase));

		// --route=pages/list/list 可直接落到指定页面，便于调试与后续协议唤起
		var routeArg = commandLine.FirstOrDefault(a =>
			a.StartsWith("--route=", StringComparison.OrdinalIgnoreCase));
		var route = routeArg is null ? null : routeArg["--route=".Length..];

		_single = new SingleInstance(() => _window?.ShowFromTray());

		if (!_single.IsFirstInstance)
		{
			// 第二个实例：已经把激活信号发给第一个进程，自己直接退出
			Exit();
			return;
		}

		_window = new MainWindow(startHidden, route);
		_window.Activate();
	}
}
