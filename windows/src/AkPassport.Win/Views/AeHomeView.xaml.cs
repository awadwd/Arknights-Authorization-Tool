using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace AkPassport.Win.Views;

/// <summary>
/// 终末地首页（原生）。
///
/// 手机端 index.vue 里的公告轮播、工具卡片排序（长按编辑 / ae_toolsConfig）在桌面上意义不大：
/// 桌面侧边栏已经承担了导航，长按排序也没有鼠标手势。这里保留真正的入口 + 数据状态，
/// 数据仍然由下面那层 WebView 里的 H5 首页负责拉取（拉完写进 ae_* localStorage），
/// 本页只读状态，不重复实现一套下载逻辑。
///
/// 配色固定浅色：终末地包本身只有浅色主题（simple / ark / jieyuan 都是浅底），
/// 而且工具图标是黑色 PNG —— 跟着外壳变深色会让图标直接消失。
/// </summary>
public sealed partial class AeHomeView : UserControl
{
	/// <summary>概览区要显示的四项。值已经格式化成可直接上屏的文本。</summary>
	public sealed record AeHomeData(
		string Source,
		string Version,
		string BoxCount,
		string Updated,
		bool HasData,
		string Hint);

	public event Action<string>? ToolRequested;   // 目标路由
	public event Action? RefreshRequested;        // 重新拉数据
	public event Action? SettingsRequested;
	public event Action? AboutRequested;

	public AeHomeView()
	{
		InitializeComponent();
		Surface.RequestedTheme = ElementTheme.Light;
		Render(new AeHomeData("—", "—", "—", "—", false, "正在读取本地数据…"));
	}

	public void Render(AeHomeData d)
	{
		SourceText.Text = d.Source;
		VersionText.Text = d.Version;
		BoxCountText.Text = d.BoxCount;
		UpdateText.Text = d.Updated;

		DataHintBar.Visibility = d.HasData ? Visibility.Collapsed : Visibility.Visible;
		DataHintText.Text = d.Hint;
	}

	/// <summary>刷新期间把按钮锁住，避免连点重复触发整页重载。</summary>
	public void SetBusy(bool busy)
	{
		RefreshButton.IsEnabled = !busy;
		RefreshButton.Content = busy ? "刷新中…" : "刷新数据";
	}

	private void OnToolClick(object sender, RoutedEventArgs e)
	{
		if (sender is Button { Tag: string route } && route.Length > 0)
		{
			ToolRequested?.Invoke(route);
		}
	}

	private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshRequested?.Invoke();

	private void OnSettingsClick(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke();

	private void OnAboutClick(object sender, RoutedEventArgs e) => AboutRequested?.Invoke();
}
