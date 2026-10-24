using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AkPassport.Win.Services;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;

namespace AkPassport.Win.Views;

/// <summary>
/// Windows 版设置页（原生 WinUI）。
///
/// 为什么不在 H5 里改：packageA/Setting/Setting.vue 有 3 万行，里面大半是
/// 小程序 / App 专属逻辑（版本更新、桌面小组件、自定义数据上传……），
/// 在桌面上既跑不通也不该出现。这里只挑「在 Windows 上真正有效」的项，
/// 值仍然写回 uni 的 localStorage，底层逻辑和手机端完全共用 —— 包括
/// 「干员头像个性化」那份 avatarPrefSettings，格式与 utils/avatarPref.js 完全一致。
/// </summary>
public sealed partial class SettingsView : UserControl
{
	public sealed record WebSettings(
		string Theme,
		bool ShowMarketPrice,
		bool EnableEnglishSearch,
		bool EnableJapaneseSearch,
		bool EnableNicknameSearch,
		bool EnableGuessData,
		bool UpdateNotify,
		bool AutoUpdate,
		string DataSource,
		string DataVersion,
		string LastUpdate);

	private sealed record ThemeOption(string Value, string Label, string Desc, Color Pane, Color Panel, Color Accent);

	/// <summary>
	/// Windows 版可选的 5 套主题（「简约 simple」已从 Windows 版移除，默认就是液态玻璃）。
	/// 预览色取自 common/ak-theme.css 与 liquid-theme.css。
	/// </summary>
	private static readonly ThemeOption[] Themes =
	{
		new("liquid", "液态玻璃", "默认 · iOS 26 冷白", C(0xEC, 0xF1, 0xF7), C(0xFA, 0xFC, 0xFF), C(0x0A, 0x84, 0xFF)),
		new("liquidDark", "液态·深色", "深底玻璃控件", C(0x0F, 0x11, 0x14), C(0x17, 0x1A, 0x1E), C(0x0A, 0x84, 0xFF)),
		new("ak", "工业", "ak-ui 纸面工业", C(0xEF, 0xEF, 0xEC), C(0xFD, 0xFD, 0xFB), C(0xC2, 0x41, 0x0C)),
		new("ark", "深色", "石墨底 + 信号色", C(0x13, 0x14, 0x15), C(0x1B, 0x1D, 0x1E), C(0xF5, 0xC8, 0x6B)),
		new("jieyuan", "界园", "米纸底 · 暖褐", C(0xFA, 0xF3, 0xE0), C(0xFF, 0xFD, 0xF7), C(0x8C, 0x5A, 0x2B)),
	};

	private static Color C(byte r, byte g, byte b) => Color.FromArgb(0xFF, r, g, b);
	private static Color C(byte a, byte r, byte g, byte b) => Color.FromArgb(a, r, g, b);

	// ── 外部事件 ────────────────────────────────────────────────────────────
	public event Action<string>? ThemePicked;
	public event Action<string, bool>? ToggleChanged;   // storage key, value
	public event Action<bool>? AutoUpdateChanged;        // autoUpdateSetting.enabled
	public event Action<AppPreferences>? ShellPreferenceChanged;
	public event Action? OpenRepoRequested;
	public event Action? AboutRequested;          // 打开「关于我们」竖屏窗口
	public event Action? SetupRequested;          // 重新运行首次设置向导
	public event Action<AvatarPref>? AvatarPrefChanged;   // 头像个性化整份设置
	public event Action<string>? PageRequested;           // 跳到某个 H5 页面（自定义搜索词等）

	private readonly List<(string Value, Button Button)> _themeButtons = new();
	private readonly List<Border> _cards = new();
	private readonly List<Border> _separators = new();

	private AppPreferences _prefs = new();
	private AvatarPref _avatar = AvatarPref.CreateDefault();
	private string _theme = "liquid";
	private bool _suspend;
	private bool _suspendAvatar;

	// 更新检查：进程内只自动查一次，避免每次进设置都打 GitHub 接口（未登录限 60 次/小时）
	private static bool _autoUpdateChecked;
	private bool _checkingUpdate;
	private string? _latestUrl;
	private ReleaseInfo? _pendingRelease;
	private bool _downloading;
	private CancellationTokenSource? _downloadCts;

	private SolidColorBrush _cardBrush = new(C(0xFF, 0xFF, 0xFF));
	private SolidColorBrush _strokeBrush = new(C(0xE4, 0xE6, 0xEB));
	private SolidColorBrush _sepBrush = new(C(0xEA, 0xEC, 0xEF));

	public SettingsView()
	{
		InitializeComponent();
		_cards.AddRange(new[] { CardTheme, CardSearch, CardAvatar, CardData, CardWindow, CardAbout });
		BuildThemeCards();
		ApplyTheme(ShellTheme.For("liquid"));

		FontText.Text = FontService.Current is null
			? "界面字体：系统默认"
			: "界面字体：" + FontService.Current;
	}

	// ── 对外入口 ────────────────────────────────────────────────────────────

	public void Render(WebSettings s, AvatarPref avatar, AppPreferences prefs)
	{
		_prefs = prefs;
		_avatar = avatar.Normalize();
		_theme = Themes.Any(t => t.Value == s.Theme) ? s.Theme : "liquid";
		SetSelectedTheme(_theme);

		_suspend = true;
		try
		{
			SearchRows.Children.Clear();
			DataRows.Children.Clear();
			WindowRows.Children.Clear();

			AddToggle(SearchRows, "显示市场价", "在列表与详情里显示干员通行证的市场参考价",
				s.ShowMarketPrice, v => ToggleChanged?.Invoke("showMarketPrice", v));

			AddToggle(SearchRows, "英文名搜索", "支持用英文名 / 英文别名检索干员",
				s.EnableEnglishSearch, v => ToggleChanged?.Invoke("enableEnglishSearch", v));

			AddToggle(SearchRows, "日文名搜索", "支持用日文名检索干员",
				s.EnableJapaneseSearch, v => ToggleChanged?.Invoke("enableJapaneseSearch", v));

			AddToggle(SearchRows, "外号搜索", "支持社区常用外号（仅部分干员）",
				s.EnableNicknameSearch, v => ToggleChanged?.Invoke("enableNicknameSearch", v));

			AddToggle(SearchRows, "预测通行证", "显示官方未公布、由社区预测的新盒号数据",
				s.EnableGuessData, v => ToggleChanged?.Invoke("enableGuessData", v), last: true);

			AddToggle(DataRows, "自动更新数据", "启动时静默检查并更新本地盒号数据",
				s.AutoUpdate, v => AutoUpdateChanged?.Invoke(v));

			AddToggle(DataRows, "数据更新提示", "发现新版本数据时弹窗提醒，避免静默错过更新",
				s.UpdateNotify, v => ToggleChanged?.Invoke("updateNotifyEnabled", v), last: true);

			DataSourceText.Text = DescribeSource(s.DataSource) + " · 数据 " + Fallback(s.DataVersion)
				+ " · 更新于 " + Fallback(s.LastUpdate);

			AddToggle(WindowRows, "自动检查更新", "启动后在后台查一次 GitHub Releases，有新版本会提示；不会自动安装",
				_prefs.AutoCheckUpdate, v => Prefs(p => p.AutoCheckUpdate = v));

			AddToggle(WindowRows, "开机自动启动", "登录 Windows 后直接在托盘运行",
				StartupService.IsEnabled(), v =>
				{
					StartupService.SetEnabled(v);
					Prefs(p => p.StartWithWindows = v);
				});

			AddToggle(WindowRows, "关闭窗口时收进托盘", "点关闭不退出程序，从托盘图标恢复",
				_prefs.CloseToTray, v => Prefs(p => p.CloseToTray = v));

			AddToggle(WindowRows, "显示托盘图标", "任务栏通知区域显示本程序图标",
				_prefs.TrayEnabled, v => Prefs(p => p.TrayEnabled = v));

			AddToggle(WindowRows, "全局快捷键 Ctrl+Alt+A", "任何界面下按快捷键呼出窗口",
				_prefs.HotkeyEnabled, v => Prefs(p => p.HotkeyEnabled = v), last: true);

			RenderAvatar();
		}
		finally
		{
			_suspend = false;
		}

		// 优先用 InformationalVersion：AssemblyVersion 只能是 1.0.0.0，
		// 真正的「1.0.0-beta.1」在 InformationalVersion 里（含 +commit 后缀时截断）
		var info = Assembly.GetExecutingAssembly()
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
		var plus = info?.IndexOf('+') ?? -1;
		if (plus > 0) info = info!.Substring(0, plus);
		var ver = Assembly.GetExecutingAssembly().GetName().Version;
		VersionText.Text = $"版本 {info ?? ver?.ToString(3) ?? "1.0.0"} · WebView2 内核";
		Scroller.ChangeView(null, 0, null, true);

		// 进设置页时静默查一次：有新版本就把按钮文案标出来，不弹窗打扰
		if (_prefs.AutoCheckUpdate && !_autoUpdateChecked)
		{
			_autoUpdateChecked = true;
			_ = CheckUpdateAsync(interactive: false);
		}
	}

	// ── 检查更新 / 下载安装 ─────────────────────────────────────────────────

	private async void OnCheckUpdateClick(object sender, RoutedEventArgs e)
	{
		if (_downloading) return;

		// 已经查到新版本了：这一步是「下载并安装」
		if (_pendingRelease is not null)
		{
			await StartDownloadAsync(_pendingRelease, ask: true);
			return;
		}

		await CheckUpdateAsync(interactive: true);
	}

	private async Task CheckUpdateAsync(bool interactive)
	{
		if (_checkingUpdate || _downloading) return;
		_checkingUpdate = true;

		if (interactive)
		{
			CheckUpdateButton.IsEnabled = false;
			CheckUpdateButton.Content = "检查中…";
			UpdatePanel.Visibility = Visibility.Visible;
			UpdateText.Opacity = 0.6;
			UpdateText.Text = "正在连接 GitHub 检查更新…";
			UpdateLink.Visibility = Visibility.Collapsed;
			UpdateProgress.Visibility = Visibility.Collapsed;
			_latestUrl = null;
		}

		try
		{
			var rel = await UpdateService.GetLatestAsync();

			if (rel is null)
			{
				_pendingRelease = null;
				if (interactive)
				{
					UpdateText.Text = "暂无 Windows 版发布记录，当前使用的已是最新版本。";
					CheckUpdateButton.Content = "检查更新";
				}
				return;
			}

			if (UpdateService.IsNewer(rel.Tag, UpdateService.CurrentVersion))
			{
				OfferUpdate(rel);

				if (!interactive && _prefs.AutoDownloadUpdate)
					await StartDownloadAsync(rel, ask: false);
			}
			else
			{
				_pendingRelease = null;
				_latestUrl = rel.PageUrl;

				if (interactive)
				{
					UpdateText.Opacity = 0.6;
					UpdateText.Text = $"已是最新版本（{UpdateService.CurrentVersion}）。";
					CheckUpdateButton.Content = "检查更新";
				}
			}
		}
		catch (Exception ex)
		{
			Diag.Log("check update failed: " + ex.Message);
			if (interactive)
			{
				UpdateText.Opacity = 0.65;
				UpdateText.Text = "检查失败：" + FriendlyUpdateError(ex)
				                  + "。可以点「项目主页」到 Releases 页面手动查看。";
				CheckUpdateButton.Content = "重试";
			}
		}
		finally
		{
			_checkingUpdate = false;
			if (interactive) CheckUpdateButton.IsEnabled = true;
		}
	}

	/// <summary>把「有新版本」落成界面状态。主窗口启动检查后也会调这里。</summary>
	internal void OfferUpdate(ReleaseInfo rel)
	{
		_pendingRelease = rel;
		_latestUrl = rel.PageUrl;

		CheckUpdateButton.IsEnabled = true;
		CheckUpdateButton.Content = rel.PortableUrl is null ? "前往下载" : "下载并安装";

		UpdatePanel.Visibility = Visibility.Visible;
		UpdateProgress.Visibility = Visibility.Collapsed;
		UpdateLink.Visibility = rel.PortableUrl is null ? Visibility.Visible : Visibility.Collapsed;
		UpdateText.Opacity = 1;
		UpdateText.Text = $"发现新版本 {rel.Tag}（当前 {UpdateService.CurrentVersion}）"
		                  + (string.IsNullOrWhiteSpace(rel.Name) ? "" : "：" + rel.Name)
		                  + (rel.PortableUrl is null ? "。这条发布没有绿色包，请用浏览器下载安装程序。" : "。");
	}

	/// <summary>
	/// 下载并交给更新器。ask=true 时先弹确认框（更新会重启程序）。
	/// 下载完不会自己替换 —— 交给同目录的 AkPassport.Updater.exe，我们退出。
	/// </summary>
	private async Task StartDownloadAsync(ReleaseInfo rel, bool ask)
	{
		if (_downloading) return;

		// 只有安装程序（没有绿色 zip）：直接交给浏览器，不走自动更新
		if (rel.PortableUrl is null)
		{
			await OpenUrlAsync(rel.InstallerUrl ?? rel.PageUrl);
			return;
		}

		if (ask)
		{
			var dialog = new ContentDialog
			{
				XamlRoot = XamlRoot,
				Title = "更新到 " + rel.Tag,
				Content = $"将从 GitHub Releases 下载安装包（约 {Mb(rel.PortableSize)}）。\n" +
				          "下载完成后程序会关闭，由更新器覆盖文件并自动重启。",
				PrimaryButtonText = "下载并安装",
				CloseButtonText = "取消",
				DefaultButton = ContentDialogButton.Primary,
			};
			if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
		}

		_downloading = true;
		_downloadCts = new CancellationTokenSource();

		CheckUpdateButton.IsEnabled = false;
		CheckUpdateButton.Content = "下载中…";
		UpdatePanel.Visibility = Visibility.Visible;
		UpdateProgress.Visibility = Visibility.Visible;
		UpdateProgress.Value = 0;
		UpdateCancelLink.Visibility = Visibility.Visible;
		UpdateLink.Visibility = Visibility.Collapsed;
		UpdateText.Opacity = 1;
		UpdateText.Text = "准备下载…";

		var progress = new Progress<DownloadProgress>(p =>
		{
			var pct = p.Total > 0 ? p.Received * 100.0 / p.Total : 0;
			UpdateProgress.Value = pct;
			UpdateText.Text = p.Total > 0
				? $"{p.Source} · {pct:0}%（{Mb(p.Received)} / {Mb(p.Total)}）"
				: $"{p.Source} · {Mb(p.Received)}";
		});

		try
		{
			var result = await UpdateService.DownloadAsync(
				rel, ParseRoute(_prefs.UpdateRoute), progress, _downloadCts.Token);

			UpdateProgress.Value = 100;
			UpdateText.Text = $"下载完成（{result.SourceLabel}），正在重启安装…";

			if (!UpdateService.LaunchUpdater(result.Path, out var error))
			{
				UpdateText.Text = "启动更新程序失败：" + error;
				UpdateLink.Visibility = Visibility.Visible;
				return;
			}

			// 交给更新器：它等我们退出后才会覆盖文件，所以这里必须真的退出
			await Task.Delay(300);
			Application.Current.Exit();
		}
		catch (OperationCanceledException)
		{
			UpdateText.Text = "已取消下载。";
			UpdateProgress.Visibility = Visibility.Collapsed;
		}
		catch (Exception ex)
		{
			Diag.Log("download update failed: " + ex.Message);
			UpdateText.Text = "下载失败：" + ex.Message + "。可以点「前往下载」用浏览器手动下载。";
			UpdateLink.Visibility = Visibility.Visible;
			UpdateProgress.Visibility = Visibility.Collapsed;
		}
		finally
		{
			_downloading = false;
			try { _downloadCts?.Dispose(); } catch { }
			_downloadCts = null;
			UpdateCancelLink.Visibility = Visibility.Collapsed;
			CheckUpdateButton.IsEnabled = true;
			if (_pendingRelease is not null) CheckUpdateButton.Content = "重试下载";
		}
	}

	private void OnCancelDownloadClick(object sender, RoutedEventArgs e)
	{
		try { _downloadCts?.Cancel(); } catch { }
	}

	private static DownloadRoute ParseRoute(string? value) => (value ?? "").ToLowerInvariant() switch
	{
		"github" => DownloadRoute.GitHubOnly,
		"cdn" => DownloadRoute.CdnOnly,
		_ => DownloadRoute.Auto,
	};

	private static string Mb(long bytes)
	{
		if (bytes <= 0) return "未知大小";
		return bytes >= 1048576
			? (bytes / 1048576.0).ToString("0.0") + " MB"
			: (bytes / 1024.0).ToString("0") + " KB";
	}

	private static string FriendlyUpdateError(Exception ex) => ex switch
	{
		HttpRequestException => "连不上 GitHub（网络不通或被拦截）",
		TaskCanceledException => "请求超时，GitHub 响应太慢",
		_ => ex.Message,
	};

	private static async Task OpenUrlAsync(string? url)
	{
		if (string.IsNullOrWhiteSpace(url)) return;
		try { await Windows.System.Launcher.LaunchUriAsync(new Uri(url)); }
		catch (Exception ex) { Diag.Log("open url failed: " + ex.Message); }
	}

	private async void OnDownloadUpdateClick(object sender, RoutedEventArgs e)
		=> await OpenUrlAsync(_latestUrl);

	/// <summary>外壳主题变化时同步本页配色（卡片 / 分割线 / 选中描边）。</summary>
	internal void ApplyTheme(ShellTheme t)
	{
		_cardBrush = new SolidColorBrush(t.Dark ? Blend(t.Panel, Colors.White, 0.04) : t.Panel);
		_strokeBrush = new SolidColorBrush(t.Border);
		_sepBrush = new SolidColorBrush(Blend(t.Border, t.Panel, 0.35));

		// 设置页盖在 WebView 上面：必须是不透明底，否则会透出下面的页面
		Scroller.Background = new SolidColorBrush(t.Canvas);

		foreach (var card in _cards)
		{
			card.Background = _cardBrush;
			card.BorderBrush = _strokeBrush;
		}
		foreach (var sep in _separators) sep.Background = _sepBrush;

		SetSelectedTheme(_theme);
	}

	private void Prefs(Action<AppPreferences> change)
	{
		if (_suspend) return;
		change(_prefs);
		_prefs.Save();
		ShellPreferenceChanged?.Invoke(_prefs);
	}

	// ── 主题卡片 ────────────────────────────────────────────────────────────

	private void BuildThemeCards()
	{
		for (var i = 0; i < Themes.Length; i++)
		{
			var opt = Themes[i];
			var btn = new Button
			{
				Style = (Style)Resources["ThemeCard"],
				Tag = opt.Value,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				Margin = new Thickness(0),
			};

			var box = new StackPanel { Spacing = 7 };

			// 三色预览条：左 = 侧边栏底色，中 = 面板，右 = 强调色
			var preview = new Grid { Height = 34, CornerRadius = new CornerRadius(5) };
			preview.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30, GridUnitType.Star) });
			preview.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45, GridUnitType.Star) });
			preview.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(25, GridUnitType.Star) });

			var b1 = new Border { Background = new SolidColorBrush(opt.Pane), CornerRadius = new CornerRadius(5, 0, 0, 5) };
			var b2 = new Border { Background = new SolidColorBrush(opt.Panel) };
			var b3 = new Border { Background = new SolidColorBrush(opt.Accent), CornerRadius = new CornerRadius(0, 5, 5, 0) };
			Grid.SetColumn(b1, 0); Grid.SetColumn(b2, 1); Grid.SetColumn(b3, 2);
			preview.Children.Add(b1); preview.Children.Add(b2); preview.Children.Add(b3);

			var frame = new Border
			{
				BorderBrush = new SolidColorBrush(C(0x33, 0x00, 0x00, 0x00)),
				BorderThickness = new Thickness(1),
				CornerRadius = new CornerRadius(5),
				Child = preview,
			};
			box.Children.Add(frame);
			box.Children.Add(new TextBlock { Text = opt.Label, FontSize = 12.5, FontWeight = FontWeights.SemiBold });
			box.Children.Add(new TextBlock { Text = opt.Desc, FontSize = 10.5, Opacity = 0.55, TextWrapping = TextWrapping.Wrap });

			btn.Content = box;
			// 卡片内容是嵌套面板，UIA 拿不到名字 —— 显式补上，读屏和自动化都靠它
			AutomationProperties.SetName(btn, opt.Label);
			AutomationProperties.SetAutomationId(btn, "theme-" + opt.Value);
			btn.Click += (s, _) =>
			{
				if (_suspend) return;
				var value = (string)((Button)s).Tag;
				_theme = value;
				SetSelectedTheme(value);
				ThemePicked?.Invoke(value);
			};

			Grid.SetRow(btn, i / 3);
			Grid.SetColumn(btn, i % 3);
			ThemeGrid.Children.Add(btn);
			_themeButtons.Add((opt.Value, btn));
		}
	}

	private void SetSelectedTheme(string name)
	{
		foreach (var (value, btn) in _themeButtons)
		{
			var selected = string.Equals(value, name, StringComparison.OrdinalIgnoreCase);
			var accent = Array.Find(Themes, t => t.Value == value)?.Accent ?? Colors.Gray;
			btn.BorderBrush = new SolidColorBrush(selected ? accent : C(0x33, 0x00, 0x00, 0x00));
			btn.BorderThickness = new Thickness(selected ? 2 : 1);
			btn.Background = new SolidColorBrush(selected ? C(0x10, 0x00, 0x00, 0x00) : Colors.Transparent);
			// 无障碍 / 自动化测试都能读到「当前选中」状态
			AutomationProperties.SetHelpText(btn, selected ? "selected" : "");
		}
	}

	// ── 通用行 ──────────────────────────────────────────────────────────────

	private void AddToggle(Panel host, string title, string desc, bool isOn,
		Action<bool> changed, bool last = false)
	{
		var grid = new Grid { Padding = new Thickness(0, 11, 0, 11), ColumnSpacing = 14 };
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
		sw.Toggled += (s, _) =>
		{
			if (_suspend) return;
			changed(((ToggleSwitch)s).IsOn);
		};
		Grid.SetColumn(sw, 1);
		grid.Children.Add(sw);

		host.Children.Add(grid);

		if (!last)
		{
			var sep = new Border { Height = 1, Background = _sepBrush, Margin = new Thickness(0) };
			host.Children.Add(sep);
			_separators.Add(sep);
		}
	}

	// ── 干员头像个性化 ──────────────────────────────────────────────────────

	private void RenderAvatar()
	{
		_suspendAvatar = true;
		try
		{
			AvatarE1.IsChecked = _avatar.Def != AvatarPref.LevelE2;
			AvatarE2.IsChecked = _avatar.Def == AvatarPref.LevelE2;

			AvatarRules.Children.Clear();
			for (var i = 0; i < _avatar.Rules.Count; i++)
				AvatarRules.Children.Add(BuildRuleRow(_avatar.Rules[i], i));
			AvatarRulesEmpty.Visibility = _avatar.Rules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

			RenderChips(AvatarWhite, _avatar.White, n => RemoveName(_avatar.White, n));
			AvatarWhiteEmpty.Visibility = _avatar.White.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

			RenderChips(AvatarBlack, _avatar.Black, n => RemoveName(_avatar.Black, n));
			AvatarBlackEmpty.Visibility = _avatar.Black.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}
		finally
		{
			_suspendAvatar = false;
		}
	}

	private void OnAvatarLevelClick(object sender, RoutedEventArgs e)
	{
		if (_suspendAvatar) return;
		if (sender is not ToggleButton btn) return;
		var level = (string)btn.Tag == "2" ? AvatarPref.LevelE2 : AvatarPref.LevelE1;
		_avatar.Def = level;
		CommitAvatar();
	}

	private UIElement BuildRuleRow(AvatarRule rule, int index)
	{
		var grid = new Grid { Padding = new Thickness(0, 6, 0, 6), ColumnSpacing = 10 };
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

		var badge = new TextBlock
		{
			Text = rule.LevelLabel,
			FontSize = 11.5,
			FontWeight = FontWeights.SemiBold,
			Foreground = new SolidColorBrush(rule.IsElite2 ? C(0xD9, 0x7A, 0x06) : C(0x25, 0x63, 0xEB)),
			VerticalAlignment = VerticalAlignment.Center,
			MinWidth = 28,
		};
		Grid.SetColumn(badge, 0);
		grid.Children.Add(badge);

		var summary = new TextBlock
		{
			Text = (index + 1) + ". " + rule.Summary,
			FontSize = 12.5,
			TextWrapping = TextWrapping.Wrap,
			VerticalAlignment = VerticalAlignment.Center,
		};
		Grid.SetColumn(summary, 1);
		grid.Children.Add(summary);

		var ops = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
		ops.Children.Add(MiniButton("编辑", () => _ = EditRuleAsync(rule)));
		if (index > 0) ops.Children.Add(MiniButton("上移", () => MoveRule(index, -1)));
		if (index < _avatar.Rules.Count - 1) ops.Children.Add(MiniButton("下移", () => MoveRule(index, 1)));
		ops.Children.Add(MiniButton("删除", () => RemoveRule(index)));
		Grid.SetColumn(ops, 2);
		grid.Children.Add(ops);

		return grid;
	}

	private Button MiniButton(string text, Action click)
	{
		var btn = new Button
		{
			Content = text,
			Style = (Style)Resources["MiniButton"],
			VerticalAlignment = VerticalAlignment.Center,
		};
		btn.Click += (_, _) => click();
		return btn;
	}

	/// <summary>名单胶囊：每行最多 4 个，点一下即移除（与手机端一致）。</summary>
	private void RenderChips(Panel host, IReadOnlyList<string> names, Action<string> remove)
	{
		host.Children.Clear();
		if (names.Count == 0) return;

		StackPanel? row = null;
		for (var i = 0; i < names.Count; i++)
		{
			if (i % 4 == 0)
			{
				row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
				host.Children.Add(row);
			}

			var name = names[i];
			var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			content.Children.Add(new TextBlock { Text = name, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
			content.Children.Add(new TextBlock
			{
				Text = "✕",
				FontSize = 10,
				Opacity = 0.45,
				VerticalAlignment = VerticalAlignment.Center,
			});

			var chip = new Button
			{
				Content = content,
				Style = (Style)Resources["ChipButton"],
				BorderBrush = _strokeBrush,
				Margin = new Thickness(0, 0, 6, 0),
			};
			AutomationProperties.SetName(chip, name);
			chip.Click += (_, _) =>
			{
				if (_suspendAvatar) return;
				remove(name);
			};
			row!.Children.Add(chip);
		}
	}

	private void RemoveName(List<string> list, string name)
	{
		list.Remove(name);
		CommitAvatar();
	}

	private void MoveRule(int index, int delta)
	{
		var target = index + delta;
		if (target < 0 || target >= _avatar.Rules.Count) return;
		(_avatar.Rules[index], _avatar.Rules[target]) = (_avatar.Rules[target], _avatar.Rules[index]);
		CommitAvatar();
	}

	private void RemoveRule(int index)
	{
		if (index < 0 || index >= _avatar.Rules.Count) return;
		_avatar.Rules.RemoveAt(index);
		CommitAvatar();
	}

	private void CommitAvatar()
	{
		_avatar = _avatar.Normalize();
		RenderAvatar();
		AvatarPrefChanged?.Invoke(_avatar);
	}

	private async void OnAddRuleClick(object sender, RoutedEventArgs e) => await EditRuleAsync(null);

	private async Task EditRuleAsync(AvatarRule? existing)
	{
		if (XamlRoot is null) return;

		var draft = existing?.Clone() ?? new AvatarRule
		{
			Id = AvatarPref.NewId(),
			Level = _avatar.Def,
			Cond = new AvatarCond(),
		};

		var groupHosts = new Dictionary<string, StackPanel>();
		var body = new StackPanel { Spacing = 12, MinWidth = 640 };

		var levelRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
		levelRow.Children.Add(new TextBlock
		{
			Text = "展示档位",
			FontSize = 12.5,
			VerticalAlignment = VerticalAlignment.Center,
			Opacity = 0.7,
		});
		var lv1 = new ToggleButton { Content = "精一", Tag = "0", MinWidth = 68 };
		var lv2 = new ToggleButton { Content = "精二", Tag = "2", MinWidth = 68 };
		levelRow.Children.Add(lv1);
		levelRow.Children.Add(lv2);
		body.Children.Add(levelRow);

		var error = new TextBlock
		{
			Text = "至少选择一个条件",
			FontSize = 11.5,
			Foreground = new SolidColorBrush(C(0xC0, 0x39, 0x2B)),
			Visibility = Visibility.Collapsed,
		};

		void SyncLevel()
		{
			lv1.IsChecked = draft.Level != AvatarPref.LevelE2;
			lv2.IsChecked = draft.Level == AvatarPref.LevelE2;
		}
		SyncLevel();
		lv1.Click += (_, _) => { draft.Level = AvatarPref.LevelE1; SyncLevel(); };
		lv2.Click += (_, _) => { draft.Level = AvatarPref.LevelE2; SyncLevel(); };

		foreach (var (key, label) in CharRoster.Groups)
		{
			var host = new StackPanel { Spacing = 4 };
			groupHosts[key] = host;

			var head = new Grid();
			head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			head.Children.Add(new TextBlock { Text = label, FontSize = 12, FontWeight = FontWeights.SemiBold, Opacity = 0.85 });

			var clear = new Button
			{
				Content = "清除",
				Style = (Style)Resources["MiniButton"],
				HorizontalAlignment = HorizontalAlignment.Right,
			};
			clear.Click += (_, _) =>
			{
				draft.Cond.Clear(key);
				RefreshGroup(key);
				if (key == "prof") RefreshGroup("sub");
			};
			Grid.SetColumn(clear, 1);
			head.Children.Add(clear);
			host.Children.Add(head);
			host.Children.Add(new StackPanel { Spacing = 6 });   // 占位，RefreshGroup 填这里

			body.Children.Add(host);
		}
		body.Children.Add(error);

		void RefreshGroup(string key)
		{
			var host = groupHosts[key];
			var chips = (StackPanel)host.Children[1];
			chips.Children.Clear();

			var options = CharRoster.OptionsOf(key, draft.Cond.Prof);
			StackPanel? row = null;
			var used = 0.0;
			foreach (var option in options)
			{
				var width = 30 + option.Length * 13.0;
				if (row is null || used + width > 700)
				{
					row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
					chips.Children.Add(row);
					used = 0;
				}
				used += width;

				var chip = new ToggleButton
				{
					Content = CharRoster.LabelOf(key, option),
					IsChecked = draft.Cond.IsPicked(key, option),
					MinWidth = 0,
					Padding = new Thickness(10, 2, 10, 3),
					CornerRadius = new CornerRadius(13),
					FontSize = 12,
				};
				var capturedKey = key;
				var capturedValue = option;
				chip.Click += (_, _) =>
				{
					draft.Cond.Toggle(capturedKey, capturedValue);
					RefreshGroup(capturedKey);
					if (capturedKey == "prof") RefreshGroup("sub");
				};
				row.Children.Add(chip);
			}
		}

		foreach (var (key, _) in CharRoster.Groups) RefreshGroup(key);

		var scroller = new ScrollViewer
		{
			Content = body,
			MaxHeight = 430,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			HorizontalScrollMode = ScrollMode.Disabled,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
		};

		var dialog = new ContentDialog
		{
			Title = existing is null ? "新增头像规则" : "编辑头像规则",
			Content = scroller,
			PrimaryButtonText = "保存",
			CloseButtonText = "取消",
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = XamlRoot,
		};

		while (true)
		{
			var result = await dialog.ShowAsync();
			if (result != ContentDialogResult.Primary) return;

			if (!draft.Cond.HasAny())
			{
				error.Visibility = Visibility.Visible;
				continue;      // 条件为空时不让关，等价于手机端的「至少选择一个条件」
			}

			if (existing is null) _avatar.Rules.Add(draft);
			else
			{
				var index = _avatar.Rules.FindIndex(r => r.Id == existing.Id);
				if (index >= 0) _avatar.Rules[index] = draft;
				else _avatar.Rules.Add(draft);
			}
			CommitAvatar();
			return;
		}
	}

	private async void OnAddWhiteClick(object sender, RoutedEventArgs e) => await AddNamesAsync(_avatar.White, "精二");
	private async void OnAddBlackClick(object sender, RoutedEventArgs e) => await AddNamesAsync(_avatar.Black, "精一");

	private async Task AddNamesAsync(List<string> target, string levelLabel)
	{
		var picked = await PickCharactersAsync($"添加到{levelLabel}名单", target);
		if (picked is null) return;

		target.Clear();
		target.AddRange(picked);
		CommitAvatar();
	}

	private async Task<IReadOnlyList<string>?> PickCharactersAsync(string title, IReadOnlyList<string> existing)
	{
		if (XamlRoot is null) return null;

		var picked = new HashSet<string>(existing);
		var selecting = false;

		var list = new ListView
		{
			SelectionMode = ListViewSelectionMode.Multiple,
			Height = 320,
			MinWidth = 420,
		};

		list.SelectionChanged += (_, _) =>
		{
			if (selecting) return;
			picked.Clear();
			foreach (var item in list.SelectedItems) picked.Add((string)item);
		};

		var count = new TextBlock { FontSize = 11.5, Opacity = 0.55, Margin = new Thickness(0, 8, 0, 0) };
		count.Text = $"已选 {picked.Count} 位";
		list.SelectionChanged += (_, _) => count.Text = $"已选 {picked.Count} 位";

		// 搜索时重建 ItemsSource 会把选中态清掉，这里按 picked 回填
		void ApplyFilter(string keyword)
		{
			var kw = (keyword ?? "").Trim();
			var items = kw.Length == 0
				? CharRoster.All
				: CharRoster.All.Where(n => n.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();

			selecting = true;
			list.ItemsSource = items;
			foreach (var name in items)
				if (picked.Contains(name)) list.SelectedItems.Add(name);
			selecting = false;
			count.Text = $"已选 {picked.Count} 位";
		}

		var search = new TextBox { PlaceholderText = "搜索干员名", Margin = new Thickness(0, 0, 0, 8) };
		search.TextChanged += (_, _) => ApplyFilter(search.Text);
		ApplyFilter("");

		var body = new StackPanel { Spacing = 4, MinWidth = 420 };
		body.Children.Add(search);
		body.Children.Add(list);
		body.Children.Add(count);

		var dialog = new ContentDialog
		{
			Title = title,
			Content = body,
			PrimaryButtonText = "确定",
			CloseButtonText = "取消",
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = XamlRoot,
		};

		var result = await dialog.ShowAsync();
		return result == ContentDialogResult.Primary ? picked.ToList() : null;
	}

	private async void OnExportAvatarClick(object sender, RoutedEventArgs e)
	{
		if (XamlRoot is null) return;

		var box = new TextBox
		{
			Text = _avatar.ToJson(),
			IsReadOnly = true,
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			Height = 220,
			FontSize = 11.5,
			MinWidth = 560,
		};

		var dialog = new ContentDialog
		{
			Title = "导出头像个性化设置",
			Content = new StackPanel
			{
				Spacing = 8,
				Children =
				{
					new TextBlock { Text = "复制下面这段 JSON，换设备后粘贴到「导入」。", FontSize = 11.5, Opacity = 0.6 },
					box,
				},
			},
			PrimaryButtonText = "复制",
			CloseButtonText = "关闭",
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = XamlRoot,
		};

		var result = await dialog.ShowAsync();
		if (result != ContentDialogResult.Primary) return;

		var package = new DataPackage();
		package.SetText(box.Text);
		Clipboard.SetContent(package);
	}

	private async void OnImportAvatarClick(object sender, RoutedEventArgs e)
	{
		if (XamlRoot is null) return;

		var box = new TextBox
		{
			PlaceholderText = "粘贴之前导出的 JSON 文本",
			AcceptsReturn = true,
			TextWrapping = TextWrapping.Wrap,
			Height = 220,
			FontSize = 11.5,
			MinWidth = 560,
		};

		var error = new TextBlock
		{
			FontSize = 11.5,
			Foreground = new SolidColorBrush(C(0xC0, 0x39, 0x2B)),
			Visibility = Visibility.Collapsed,
			TextWrapping = TextWrapping.Wrap,
		};

		var dialog = new ContentDialog
		{
			Title = "导入头像个性化设置",
			Content = new StackPanel
			{
				Spacing = 8,
				Children =
				{
					new TextBlock { Text = "导入会覆盖当前这份设置（规则 / 名单 / 默认档位）。", FontSize = 11.5, Opacity = 0.6 },
					box,
					error,
				},
			},
			PrimaryButtonText = "导入",
			CloseButtonText = "取消",
			DefaultButton = ContentDialogButton.Primary,
			XamlRoot = XamlRoot,
		};

		while (true)
		{
			var result = await dialog.ShowAsync();
			if (result != ContentDialogResult.Primary) return;

			var text = (box.Text ?? "").Trim();
			if (text.Length == 0)
			{
				error.Text = "先粘贴一段 JSON 文本";
				error.Visibility = Visibility.Visible;
				continue;
			}

			try
			{
				var parsed = JsonSerializer.Deserialize<AvatarPref>(text, AvatarPref.JsonOptions);
				if (parsed is null) throw new JsonException("空内容");

				_avatar = parsed.Normalize();
				RenderAvatar();
				AvatarPrefChanged?.Invoke(_avatar);
				return;
			}
			catch (Exception ex)
			{
				error.Text = "JSON 解析失败：" + ex.Message;
				error.Visibility = Visibility.Visible;
			}
		}
	}

	private async void OnResetAvatarClick(object sender, RoutedEventArgs e)
	{
		if (XamlRoot is null) return;

		var dialog = new ContentDialog
		{
			Title = "恢复默认头像设置？",
			Content = new TextBlock
			{
				Text = "默认档位回到「优先精一」，条件规则与两份名单都会清空。",
				FontSize = 12.5,
				TextWrapping = TextWrapping.Wrap,
			},
			PrimaryButtonText = "恢复默认",
			CloseButtonText = "取消",
			DefaultButton = ContentDialogButton.Close,
			XamlRoot = XamlRoot,
		};

		if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

		_avatar = AvatarPref.CreateDefault();
		RenderAvatar();
		AvatarPrefChanged?.Invoke(_avatar);
	}

	// ── 其它按钮 ────────────────────────────────────────────────────────────

	private void OnCustomWordsClick(object sender, RoutedEventArgs e)
		=> PageRequested?.Invoke("packageA/custom-words/custom-words");

	private void OnOpenRepoClick(object sender, RoutedEventArgs e) => OpenRepoRequested?.Invoke();

	private void OnAboutClick(object sender, RoutedEventArgs e) => AboutRequested?.Invoke();

	private void OnRunSetupClick(object sender, RoutedEventArgs e) => SetupRequested?.Invoke();

	// ── 小工具 ──────────────────────────────────────────────────────────────

	private static string DescribeSource(string key) => key switch
	{
		"knowCloud" => "知晓云源",
		"domestic" => "国内源（GitCode）",
		"github" => "GitHub 源",
		"cdn" => "CDN 源",
		"local" => "本地数据",
		"custom" => "自定义数据",
		_ => "默认源",
	};

	private static string Fallback(string v) => string.IsNullOrWhiteSpace(v) ? "未知" : v;

	/// <summary>把 a 按 pct% 的权重叠到 b 上（只用于生成一点点明暗差）。</summary>
	private static Color Blend(Color a, Color b, double t)
		=> Color.FromArgb(0xFF,
			(byte)(a.R + (b.R - a.R) * t),
			(byte)(a.G + (b.G - a.G) * t),
			(byte)(a.B + (b.B - a.B) * t));
}
