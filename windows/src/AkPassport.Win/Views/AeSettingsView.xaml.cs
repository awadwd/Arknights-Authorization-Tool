using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AkPassport.Win.Services;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace AkPassport.Win.Views;

/// <summary>
/// 终末地设置页（原生）。
///
/// 对应 package_ArknightEnds/Setting/Setting.vue（4200 行）。那份 Vue 里一大半是手机专属逻辑：
/// 数据源切换、自定义 JSON 上传/校验、GitHub 大文件下载进度、扫码、Wi-Fi 判断……
/// 这些在桌面端要么跑不通、要么没有意义（本机就是宽带有线），所以只保留真正生效的四块：
/// 数据状态与清理、搜索开关、预测/市价/自动更新、关于。
///
/// 值仍然写回 uni 的 localStorage（ae_ 前缀），手机端与 Windows 共用同一套数据。
/// 注意终末地包里的开关是按字符串 'true' / 'false' 存的（页面里普遍写 === 'true'），
/// 写成布尔会「看着改了、实际没生效」。
/// </summary>
public sealed partial class AeSettingsView : UserControl
{
	public sealed record AeSettings(
		string DataSource,
		string DataVersion,
		string LastUpdate,
		string Size,
		int BoxCount,
		int GuessCount,
		int SearchWordCount,
		int CustomWordCount,
		bool EnableEnglish,
		bool EnableJapanese,
		bool EnableNickname,
		bool EnableGuess,
		bool ShowMarketPrice,
		bool AutoUpdate,
		string AutoUpdateLast,
		int AutoUpdateHours);

	// ── 对外事件 ────────────────────────────────────────────────────────────
	public event Action<string, bool>? ToggleChanged;      // 存储键, 值
	public event Action<bool>? AutoUpdateChanged;          // ae_autoUpdateSetting.enabled
	public event Action<int>? AutoUpdateHoursChanged;      // ae_autoUpdateSetting.checkInterval（小时）
	public event Action? RefreshRequested;                 // 重新下载数据
	public event Action? ClearRequested;                   // 清空本地数据
	public event Action<string>? PageRequested;            // 跳到 H5 页面（自定义搜索词）
	public event Action? AboutRequested;
	public event Action? OpenRepoRequested;

	private static readonly (int Hours, string Label)[] Frequencies =
	{
		(6, "6 小时"), (12, "12 小时"), (24, "24 小时"), (48, "48 小时"), (168, "7 天"),
	};

	private bool _suspend;
	private ToggleSwitch? _marketSwitch;

	public AeSettingsView()
	{
		InitializeComponent();
		Surface_Theme();
		BuildFrequencyBox();

		FontText.Text = FontService.Current is null
			? "界面字体：系统默认"
			: "界面字体：" + FontService.Current;

		var info = Assembly.GetExecutingAssembly()
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		var plus = info?.IndexOf('+') ?? -1;
		if (plus > 0) info = info!.Substring(0, plus);
		VersionText.Text = $"版本 {info ?? "1.0.0"} · WebView2 内核";
	}

	private void Surface_Theme()
	{
		RequestedTheme = ElementTheme.Light;   // 终末地包只有浅色主题，图标也是黑色 PNG
		Scroller.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xFA, 0xFA, 0xFA));
	}

	// ── 渲染 ────────────────────────────────────────────────────────────────

	public void Render(AeSettings s)
	{
		_suspend = true;
		try
		{
			SourceValue.Text = DescribeSource(s.DataSource);
			VersionValue.Text = Fallback(s.DataVersion);
			UpdateValue.Text = Fallback(s.LastUpdate);
			SizeValue.Text = string.IsNullOrEmpty(s.Size) ? "—" : s.Size;
			BoxValue.Text = s.BoxCount > 0 ? s.BoxCount + " 个" : "—";
			GuessValue.Text = s.GuessCount > 0 ? s.GuessCount + " 个" : "—";
			SearchWordValue.Text = s.SearchWordCount > 0 ? s.SearchWordCount + " 个干员" : "—";
			CustomWordValue.Text = s.CustomWordCount > 0 ? s.CustomWordCount + " 条" : "—";
			CustomWordsHint.Text = s.CustomWordCount > 0
				? $"已添加 {s.CustomWordCount} 个自定义搜索词，点击「管理」增删"
				: "给常用外号建别名，查询时一并命中";

			SearchRows.Children.Clear();
			FuncRows.Children.Clear();

			AddToggle(SearchRows, "英文名搜索", "支持用英文名 / 英文别名检索干员",
				s.EnableEnglish, v => ToggleChanged?.Invoke("enableEnglishSearch", v));

			AddToggle(SearchRows, "日文名搜索", "支持用日文名检索干员",
				s.EnableJapanese, v => ToggleChanged?.Invoke("enableJapaneseSearch", v));

			AddToggle(SearchRows, "外号搜索", "支持社区常用外号（依赖已下载的搜索词数据）",
				s.EnableNickname, v => ToggleChanged?.Invoke("enableNicknameSearch", v), last: true);

			AddToggle(FuncRows, "启用预测通行证", "显示官方尚未公布、由社区预测的新盒号",
				s.EnableGuess, v => ToggleChanged?.Invoke("enableGuessData", v),
				note: "请以官方最终发布为准 · 改动后重新进入页面即生效");

			_marketSwitch = AddToggle(FuncRows, "开启市价展示", "在干员列表里显示第三方平台（千岛）的参考价",
				s.ShowMarketPrice, OnMarketPriceToggle,
				note: "价格仅供参考，本程序不对准确性负责");

			AddToggle(FuncRows, "干员数据自动更新", "检测到新版本数据时自动下载",
				s.AutoUpdate,
				v =>
				{
					// 频率选择器跟着开关可用性走（不重绘整页，避免滚动位置跳回顶部）
					FrequencyBox.IsEnabled = v;
					AutoUpdateChanged?.Invoke(v);
				},
				note: s.AutoUpdate && !string.IsNullOrEmpty(s.AutoUpdateLast)
					? "上次检查：" + s.AutoUpdateLast
					: null,
				last: true);

			var hours = Frequencies.Any(f => f.Hours == s.AutoUpdateHours) ? s.AutoUpdateHours : 24;
			FrequencyBox.SelectedIndex = Array.FindIndex(Frequencies, f => f.Hours == hours);
			FrequencyBox.IsEnabled = s.AutoUpdate;
		}
		finally
		{
			_suspend = false;
		}

		Scroller.ChangeView(null, 0, null, true);
	}

	/// <summary>刷新期间锁住按钮，避免连点。</summary>
	public void SetBusy(bool busy)
	{
		Page.IsHitTestVisible = !busy;
		Page.Opacity = busy ? 0.6 : 1;
	}

	private static string DescribeSource(string value) => value switch
	{
		"domestic" => "国内源（推荐）",
		"local" => "本地数据",
		"custom" => "自定义数据",
		"github" => "GitHub 源（仅基础数据）",
		_ => "未设置 · 默认国内源",
	};

	private static string Fallback(string value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

	// ── 通用行 ──────────────────────────────────────────────────────────────

	private ToggleSwitch AddToggle(Panel host, string title, string desc, bool isOn,
		Action<bool> changed, string? note = null, bool last = false)
	{		var grid = new Grid { Padding = new Thickness(0, 11, 0, last ? 4 : 11), ColumnSpacing = 14 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var text = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
		text.Children.Add(new TextBlock { Text = title, FontSize = 13.5 });
		if (!string.IsNullOrEmpty(desc))
			text.Children.Add(new TextBlock
			{
				Text = desc,
				FontSize = 11.5,
				Opacity = 0.58,
				TextWrapping = TextWrapping.Wrap,
			});
		if (!string.IsNullOrEmpty(note))
			text.Children.Add(new TextBlock
			{
				Text = note,
				FontSize = 11,
				Opacity = 0.45,
				TextWrapping = TextWrapping.Wrap,
			});
		Grid.SetColumn(text, 0);
		grid.Children.Add(text);

		var sw = new ToggleSwitch
		{
			IsOn = isOn,
			OnContent = "",
			OffContent = "",
			MinWidth = 0,
			VerticalAlignment = VerticalAlignment.Center,
		};
		sw.Toggled += (_, _) =>
		{
			if (_suspend) return;
			changed(sw.IsOn);
		};
		Grid.SetColumn(sw, 1);
		grid.Children.Add(sw);

		host.Children.Add(grid);
		if (!last) host.Children.Add(new Border { Height = 1, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0xFF, 0xED, 0xED, 0xED)) });
		return sw;
	}

	// ── 市价免责声明 ────────────────────────────────────────────────────────

	private async void OnMarketPriceToggle(bool value)
	{
		if (!value)
		{
			ToggleChanged?.Invoke("showMarketPrice", false);
			return;
		}

		// 先按「关闭」写回，用户同意后再打开 —— 否则弹窗期间页面已经按开启渲染了
		ToggleChanged?.Invoke("showMarketPrice", false);

		var dialog = new ContentDialog
		{
			Title = "免责声明",
			XamlRoot = XamlRoot,
			PrimaryButtonText = "同意并开启",
			CloseButtonText = "取消",
			DefaultButton = ContentDialogButton.Close,
			Content = new TextBlock
			{
				TextWrapping = TextWrapping.Wrap,
				FontSize = 13,
				Text = "市价信息来源于第三方平台（千岛 APP），仅供参考。\n\n" +
				       "1. 不保证价格的真实性与准确性；\n" +
				       "2. 价格可能随市场波动实时变化；\n" +
				       "3. 请以实际成交价为准；\n" +
				       "4. 因依赖此信息造成的损失，本程序不承担责任。",
			},
		};

		var result = await dialog.ShowAsync();
		if (result == ContentDialogResult.Primary)
		{
			ToggleChanged?.Invoke("showMarketPrice", true);
		}
		else
		{
			// 取消 → 只把这个开关拨回去。整页重绘会把别的开关也刷成「上次渲染时」的旧状态。
			_suspend = true;
			if (_marketSwitch is not null) _marketSwitch.IsOn = false;
			_suspend = false;
			ToggleChanged?.Invoke("showMarketPrice", false);
		}
	}

	// ── 清空数据 ────────────────────────────────────────────────────────────

	private async void OnClearClick(object sender, RoutedEventArgs e)
	{
		var dialog = new ContentDialog
		{
			Title = "清空本地数据？",
			XamlRoot = XamlRoot,
			PrimaryButtonText = "清空",
			CloseButtonText = "取消",
			DefaultButton = ContentDialogButton.Close,
			Content = new TextBlock
			{
				TextWrapping = TextWrapping.Wrap,
				FontSize = 13,
				Text = "会删除已下载的盒号、预测盒号与搜索词数据，并切回国内源。\n" +
				       "清空后请回到首页重新下载，否则查询页会没有数据。",
			},
		};

		if (await dialog.ShowAsync() == ContentDialogResult.Primary)
		{
			ClearRequested?.Invoke();
		}
	}

	private void BuildFrequencyBox()
	{
		// 构造期设默认项会触发 SelectionChanged；此时事件还没接线，
		// 但仍用 _suspend 挡一下，避免以后有人在构造后立刻订阅时收到假事件。
		_suspend = true;
		try
		{
			foreach (var (_, label) in Frequencies) FrequencyBox.Items.Add(label);
			FrequencyBox.SelectedIndex = 2;   // 24 小时
		}
		finally
		{
			_suspend = false;
		}
	}

	private void OnFrequencyChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_suspend) return;
		var index = FrequencyBox.SelectedIndex;
		if (index < 0 || index >= Frequencies.Length) return;
		AutoUpdateHoursChanged?.Invoke(Frequencies[index].Hours);
	}

	private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshRequested?.Invoke();

	private void OnCustomWordsClick(object sender, RoutedEventArgs e)
		=> PageRequested?.Invoke("package_ArknightEnds/custom-words/custom-words");

	private void OnAboutClick(object sender, RoutedEventArgs e) => AboutRequested?.Invoke();

	private void OnRepoClick(object sender, RoutedEventArgs e) => OpenRepoRequested?.Invoke();
}
