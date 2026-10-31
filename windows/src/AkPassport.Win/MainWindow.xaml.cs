using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using AkPassport.Win.Services;
using AkPassport.Win.Views;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using Windows.Graphics;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace AkPassport.Win;

public sealed partial class MainWindow : Window
{
	private const string Host = "app.local";
	private const string HomeRoute = "pages/desktop_home/desktop_home";
	private const string AeRoot = "package_ArknightEnds";
	private const string AeHomeRoute = "package_ArknightEnds/index/index";
	private const string AppDisplayName = "方舟通行证谷子查询工具";
	private const string AeDisplayName = "终末地工具箱";

	/// <summary>侧边栏的一项。两套工具包（方舟 / 终末地）共用同一个 NavigationView。</summary>
	private sealed record NavEntry(string Text, string Tag, Symbol Icon);

	private static readonly NavEntry[] AkPrimary =
	{
		new("首页", "pages/desktop_home/desktop_home", Symbol.Home),
		new("通行证查询", "pages/Search/Search", Symbol.Find),
		new("通行证列表", "pages/list/list", Symbol.List),
		new("模拟抽卡", "packageB/pages/gacha_simulated/gacha_simulated", Symbol.Play),
		new("我的收藏", "packageB/pages/mystarbox/mystarbox", Symbol.Favorite),
	};

	private static readonly NavEntry[] AkSecondary =
	{
		new("通行证展柜", "pages/card_share/card_share", Symbol.Pictures),
		new("通行证日历", "packageA/calendar/calendar", Symbol.Calendar),
		new("今日宜挂", "pages/daily_recommend/daily_recommend", Symbol.Clock),
		// 与 macOS 版 Sidebar.swift 逐字一致：这两个页面之前只有手机首页能进，
		// 桌面侧边栏没有项，等于对 PC 用户不可用。
		// PRTS 页面的 onLoad 只认 from=index，缺这个参数会白屏，所以必须带上。
		new("通行证游乐场", "packageB/pages/passport_game/passport_game", Symbol.Emoji),
		new("PRTS 帮查", "packageA/PRTS_Search/PRTS_Search?from=index", Symbol.Message),
		new("数据反馈", "pages/box_feedback_new/box_feedback_new", Symbol.Comment),
	};

	private static readonly NavEntry[] AePrimary =
	{
		new("终末地首页", AeHomeRoute, Symbol.Home),
		new("凭证查询", "package_ArknightEnds/Search/Search", Symbol.Find),
		new("凭证列表", "package_ArknightEnds/list/list", Symbol.List),
		new("模拟抽卡", "package_ArknightEnds/gacha_simulated/gacha_simulated", Symbol.Play),
	};

	private static readonly NavEntry[] AeSecondary =
	{
		new("终末地日历", "package_ArknightEnds/calendar/calendar", Symbol.Calendar),
		new("自定义搜索词", "package_ArknightEnds/custom-words/custom-words", Symbol.Edit),
		new("公告详情", "package_ArknightEnds/more-notice/more-notice", Symbol.Comment),
	};

	/// <summary>
	/// index.html 带 ?v=构建时间戳。
	/// WebView2 会缓存 https://app.local/index.html，而它引用的资源文件名是带哈希的：
	/// 缓存了旧 index.html 就等于整套旧前端 —— 更新后用户看到的还是老界面。
	/// </summary>
	private readonly string _baseUrl;
	private readonly string _homeUrl;

	private readonly LocalProxy _proxy = new();
	private readonly ImageProxy _imageProxy = new();
	private readonly RemoteProxy _remoteProxy = new();
	private readonly AppPreferences _prefs = AppPreferences.Load();
	private readonly DispatcherQueueTimer _saveTimer;
	private readonly DispatcherQueueTimer _settingsReloadTimer;

	private TrayService? _tray;
	private BattleServer? _battle;
	private DispatcherQueueTimer? _battleIdleTimer;
	private DateTime _battleActiveAt = DateTime.UtcNow;
	private HotkeyService? _hotkey;
	private CoreWebView2? _core;
	private readonly string _startUrl;
	private bool _startHidden;
	private bool _reallyExit;
	private bool _syncingNav;
	private string? _appliedTheme;
	private bool? _lastNarrow;
	private bool _wideLayout = true;
	private bool _startInSettings;
	private bool _settingsVisible;
	private bool _aeHomeVisible;
	private bool _webReloadPending;
	private bool _suppressSettingsRender;
	private bool _restoringFromSettings;
	private bool _endfield;
	private string? _pendingSelectionTag;
	private string? _lastWebUrl;
	private string? _lastWebRoute;
	private AboutWindow? _aboutWindow;
	private ShellTheme _shellTheme = ShellTheme.For("liquid");
	private (PointInt32 Pos, SizeInt32 Size)? _lastNormalRect;

	private static readonly JsonSerializerOptions JsonRead = new() { PropertyNameCaseInsensitive = true };

	public MainWindow(bool startHidden, string? route = null)
	{
		InitializeComponent();
		_startHidden = startHidden;

		AppPaths.EnsureCreated();
		// 联机服务器不在这里开：等前端真的点「创建房间」再监听（见 StartBattleServer）
		// 必须先算好入口地址，再拼 _startUrl —— 顺序反了会拿到 null，WebView 停在 about:blank。
		_baseUrl = BuildVersionedUrl();
		_homeUrl = _baseUrl + "#/" + HomeRoute;

		var target = string.IsNullOrWhiteSpace(route) ? null : route.Trim().TrimStart('/');
		// --route=settings 用来看原生设置页（settings 不是 H5 路由，不参与 WebView 导航）
		_startInSettings = string.Equals(target, "settings", StringComparison.OrdinalIgnoreCase);
		if (_startInSettings) target = null;

		// 上次停在终末地就还回终末地；显式给了路由则以路由为准
		_endfield = IsEndfield(target) || (target is null && IsEndfield(_prefs.LastMode));
		BuildNav(_endfield);
		UpdateModeChrome();

		var landing = target ?? (_endfield ? AeHomeRoute : HomeRoute);
		_startUrl = target is null ? $"{_baseUrl}#/{landing}" : $"{_baseUrl}#/{target}";
		_wideLayout = IsWideRoute(landing);

		ApplyBackdrop();
		SetupTitleBar();
		ApplyUiFont();
		ApplyShellTheme("liquid", reload: false);   // Windows 版默认液态玻璃
		RestoreWindowState();
		ApplyMinWindowSize();
		WireSettingsPage();

		// 终末地包的首页是原生页：开机就盖上，避免先闪一下 H5 首页再被覆盖。
		// （--route=settings 的情况留给 OnNavigationCompleted 里的 _startInSettings 分支）
		if (_endfield && !_startInSettings)
		{
			_aeHomeVisible = true;
			AeHomePage.Visibility = Visibility.Visible;
		}

		_saveTimer = DispatcherQueue.CreateTimer();
		_saveTimer.Interval = TimeSpan.FromMilliseconds(400);
		_saveTimer.IsRepeating = false;
		_saveTimer.Tick += (_, _) => SaveWindowState();

		// 设置改完不等到离开设置页才刷新：800ms 防抖后直接重载 WebView，
		// 用户点回任何页面看到的都是新配置（旧实现只在切换路由时 reload，容易「改了没反应」）。
		_settingsReloadTimer = DispatcherQueue.CreateTimer();
		_settingsReloadTimer.Interval = TimeSpan.FromMilliseconds(800);
		_settingsReloadTimer.IsRepeating = false;
		_settingsReloadTimer.Tick += (_, _) => ReloadFromSettings();

		AppWindow.Changed += OnAppWindowChanged;
		AppWindow.Closing += OnClosing;
		Activated += OnActivated;

		// 首次运行向导：等 XamlRoot 就绪再弹（ContentDialog 必须要 XamlRoot）
		Root.Loaded += OnRootLoaded;

		ApplyContentWidth(AppWindow.Size.Width);

		_ = InitWebViewAsync();
		_ = CheckUpdateOnStartupAsync();
	}

	// ── 工具包（方舟 / 终末地）──────────────────────────────────────────────

	private static bool IsEndfield(string? value)
		=> !string.IsNullOrWhiteSpace(value)
		   && (value.StartsWith(AeRoot, StringComparison.OrdinalIgnoreCase)
		       || string.Equals(value, "ae", StringComparison.OrdinalIgnoreCase));

	private static bool IsWideRoute(string? path)
		=> !string.IsNullOrEmpty(path)
		   && (path.StartsWith(HomeRoute, StringComparison.OrdinalIgnoreCase)
		       || path.StartsWith(AeHomeRoute, StringComparison.OrdinalIgnoreCase));

	/// <summary>按当前工具包重建侧边栏菜单（设置项在 Footer，固定不动）。</summary>
	private void BuildNav(bool endfield)
	{
		_syncingNav = true;
		try
		{
			Nav.MenuItems.Clear();
			foreach (var entry in endfield ? AePrimary : AkPrimary) Nav.MenuItems.Add(CreateNavItem(entry));
			Nav.MenuItems.Add(new NavigationViewItemHeader { Content = "更多" });
			foreach (var entry in endfield ? AeSecondary : AkSecondary) Nav.MenuItems.Add(CreateNavItem(entry));
		}
		finally
		{
			_syncingNav = false;
		}
	}

	private static NavigationViewItem CreateNavItem(NavEntry entry) => new()
	{
		Content = entry.Text,
		Tag = entry.Tag,
		Icon = new SymbolIcon(entry.Icon),
	};

	/// <summary>标题栏、窗口标题、切换按钮文案都跟着工具包走。</summary>
	private void UpdateModeChrome()
	{
		var name = _endfield ? AeDisplayName : AppDisplayName;
		Title = name;
		AppWindow.Title = name;
		TitleText.Text = name;
		AeSwitchText.Text = _endfield ? "明日方舟" : "终末地";
		// 图标跟「点下去要去的地方」走：在终末地里显示罗德岛，在方舟里显示终末地
		// 两张图资源都在 XAML 里绑好，这里只切显隐；不要用代码 new BitmapImage(ms-appx:///...)，
		// 免打包应用里会在构造阶段直接崩，catch 也拦不住。
		AeSwitchIcon.Visibility = _endfield ? Visibility.Collapsed : Visibility.Visible;
		AkSwitchIcon.Visibility = _endfield ? Visibility.Visible : Visibility.Collapsed;
	}

	/// <summary>切换工具包；navigate=true 时把 WebView 带到对应首页。</summary>
	private void SetMode(bool endfield, bool navigate)
	{
		if (_endfield != endfield)
		{
			_endfield = endfield;
			BuildNav(endfield);
			UpdateModeChrome();
			ApplyContentWidth(AppWindow.Size.Width);
			_prefs.LastMode = endfield ? "ae" : "ak";
			_prefs.Save();
			Diag.Log("mode: " + (endfield ? "endfield" : "arknights"));
		}

		if (!navigate || _core is null) return;
		HideSettings();
		if (endfield)
		{
			// 先盖原生首页再导航，H5 首页在下面慢慢加载（拉数据），用户看不到闪烁
			ShowAeHome();
			NavigateWeb(AeHomeRoute);
		}
		else
		{
			HideAeHome();
			NavigateWeb(HomeRoute);
		}
	}

	private void NavigateWeb(string route)
	{
		_wideLayout = IsWideRoute(route);
		ApplyContentWidth(AppWindow.Size.Width);
		try { _core?.Navigate($"{_baseUrl}#/{route}"); }
		catch (Exception ex) { Diag.Log("navigate failed: " + ex.Message); }
	}

	private void OnAeSwitchClick(object sender, RoutedEventArgs e) => SetMode(!_endfield, navigate: true);

	/// <summary>没装苹方就用系统默认，不做任何覆盖。</summary>
	private void ApplyUiFont()
	{
		if (FontService.Current is not { } family) return;
		var font = new FontFamily(family);
		Nav.FontFamily = font;
		TitleText.FontFamily = font;
		AeSwitchText.FontFamily = font;
	}

	private async void OnRootLoaded(object sender, RoutedEventArgs e)
	{
		Root.Loaded -= OnRootLoaded;
		if (_prefs.SetupCompleted || _startHidden) return;   // --tray 静默启动不打扰
		await RunSetupAsync();
	}

	/// <summary>
	/// 首次设置向导。设置页的「重新运行首次设置向导」也走这里。
	/// 勾选项立刻生效，不是纯 UI 装饰。
	/// </summary>
	private async Task RunSetupAsync()
	{
		try
		{
			var root = Root.XamlRoot;
			if (root is null) return;

			// 开始菜单是「安装动作」，不进勾选项、也不询问：
			// 非打包应用没有注册表安装记录，这是用户在「所有应用」里唯一的发现入口。
			if (!ShortcutService.ExistsInStartMenu(AppDisplayName))
				ShortcutService.CreateStartMenu(AppDisplayName);

			var choice = await SetupWizard.ShowAsync(root, _shellTheme.Dark, _prefs);
			if (choice is null) return;

			if (choice.Accepted)
			{
				if (choice.CreateShortcut) ShortcutService.Create(AppDisplayName);

				if (choice.StartWithWindows != StartupService.IsEnabled())
					StartupService.SetEnabled(choice.StartWithWindows);

				_prefs.StartWithWindows = choice.StartWithWindows;
				_prefs.CloseToTray = choice.CloseToTray;
				_prefs.TrayEnabled = choice.TrayIcon;
				_prefs.HotkeyEnabled = choice.Hotkey;
			}

			_prefs.SetupCompleted = true;
			_prefs.Save();

			OnSettingsAutoUpdateChanged(choice.AutoUpdate);
			RebuildTrayAndHotkey();
			Diag.Log($"setup wizard: shortcut={choice.CreateShortcut} autoUpdate={choice.AutoUpdate} startup={choice.StartWithWindows} tray={choice.TrayIcon} hotkey={choice.Hotkey}");
		}
		catch (Exception ex)
		{
			Diag.Log("setup wizard failed: " + ex.Message);
		}
	}

	// ── 启动 ────────────────────────────────────────────────────────────────

	/// <summary>
	/// 自绘标题栏：把系统标题栏去掉，用侧边栏同色的条作为拖动区，
	/// 只保留右上角三个系统按钮（颜色随主题变）。
	/// </summary>
	private void SetupTitleBar()
	{
		ExtendsContentIntoTitleBar = true;
		SetTitleBar(AppTitleBar);

		var tb = AppWindow.TitleBar;
		tb.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
		tb.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
	}

	private async Task InitWebViewAsync()
	{
		try
		{
			// 固定用户数据目录：升级/换目录后 localStorage（收藏、设置、抽卡记录）才不会丢。
			// WebView2 官方支持用这个环境变量指定目录，必须在初始化之前设置。
			Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", AppPaths.WebViewData);
#if DEBUG
			Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--remote-debugging-port=9222 --remote-allow-origins=*");
#endif
			await Web.EnsureCoreWebView2Async();
			_core = Web.CoreWebView2;

			_core.Settings.AreDefaultContextMenusEnabled = false;
			_core.Settings.IsStatusBarEnabled = false;
#if DEBUG
			_core.Settings.AreDevToolsEnabled = true;
#else
			_core.Settings.AreDevToolsEnabled = false;          // 正式包关掉 F12，避免误操作改坏页面
#endif
			_core.Settings.IsSwipeNavigationEnabled = false;
			_core.Settings.AreBrowserAcceleratorKeysEnabled = false;
			// 桌面端固定版式：禁掉 Ctrl+滚轮 / 双指捏合缩放
			_core.Settings.IsZoomControlEnabled = false;
			_core.Settings.IsPinchZoomEnabled = false;

			_core.SetVirtualHostNameToFolderMapping(Host, AppPaths.WebRoot,
				CoreWebView2HostResourceAccessKind.Allow);

			// 前端 H5 分支把 raw.gitcode.com 换成了 /gitcode，这里接管这一段
			_core.AddWebResourceRequestedFilter("https://" + Host + "/gitcode/*",
				CoreWebView2WebResourceContext.All);
			// B 站图床防盗链：直接请求会 403，改由壳带合法 Referer 取回
			_core.AddWebResourceRequestedFilter("https://*.hdslb.com/*",
				CoreWebView2WebResourceContext.Image);
			_core.AddWebResourceRequestedFilter("https://*.biliimg.com/*",
				CoreWebView2WebResourceContext.Image);
			_core.AddWebResourceRequestedFilter("https://*.sinaimg.cn/*",
				CoreWebView2WebResourceContext.Image);

			// 数据源直连：页面在 https://app.local 下用 uni.request 打这些域名属于跨域，
			// 对方不返回 Access-Control-Allow-Origin → 终末地包整页「网络错误」。
			// 交给壳带 UA 取回再补 CORS 头（RemoteProxy）。
			_core.AddWebResourceRequestedFilter("https://raw.gitcode.com/*",
				CoreWebView2WebResourceContext.All);
			_core.AddWebResourceRequestedFilter("https://cdn.jsdelivr.net/*",
				CoreWebView2WebResourceContext.All);
			_core.AddWebResourceRequestedFilter("https://fastly.jsdelivr.net/*",
				CoreWebView2WebResourceContext.All);
			_core.AddWebResourceRequestedFilter("https://raw.githubusercontent.com/*",
				CoreWebView2WebResourceContext.All);
			// 知晓云（数据版本 / 公告）：租户域名每次可能不同，按通配拦
			_core.AddWebResourceRequestedFilter("https://*.myminapp.com/*",
				CoreWebView2WebResourceContext.All);
			_core.WebResourceRequested += OnWebResourceRequested;

			_core.NewWindowRequested += OnNewWindowRequested;
			_core.NavigationStarting += OnNavigationStarting;
			_core.DownloadStarting += OnDownloadStarting;
			_core.NavigationCompleted += OnNavigationCompleted;
			_core.SourceChanged += (s, _) => SyncNavFromUrl(s.Source);
			_core.ProcessFailed += OnProcessFailed;

			// 桌面端字号修正：必须在加载页面前注册
			await _core.AddScriptToExecuteOnDocumentCreatedAsync(DesktopPatches.ScaleScript);
			// 禁止页面缩放（Ctrl+滚轮 / 捏合 / Ctrl +/-）
			await _core.AddScriptToExecuteOnDocumentCreatedAsync(DesktopPatches.ZoomGuardScript);
			// Windows 版没有「简约」主题：首屏前把 themeMode 兜底成 liquid
			await _core.AddScriptToExecuteOnDocumentCreatedAsync(DesktopPatches.ThemeDefaultScript);
			// 顶层页隐藏 uni 自带标题栏（侧边栏已经承担导航）
			await _core.AddScriptToExecuteOnDocumentCreatedAsync(DesktopPatches.ChromeScript);
			// 主题桥：前端主题要同步到 WinUI 外壳
			await _core.AddScriptToExecuteOnDocumentCreatedAsync(DesktopPatches.ThemeBridgeScript);
			// 「去设置」桥：把 H5 里的 packageA/Setting/Setting 跳转拦下来，改成打开原生设置页
			await _core.AddScriptToExecuteOnDocumentCreatedAsync(DesktopPatches.SettingsBridgeScript);

			// 必须在 uni / 页面脚本发请求之前挂上，否则首屏那次下载还是会打到 /gitcode
			await _core.AddScriptToExecuteOnDocumentCreatedAsync(DesktopPatches.RemoteRewriteScript);
			_core.WebMessageReceived += OnWebMessageReceived;

			Web.Source = new Uri(_startUrl);

			SetUpTrayAndHotkey();
			if (_startHidden) AppWindow.Hide();
		}
		catch (Exception ex)
		{
			Diag.Log("init failed: " + ex);
			ShowError(DescribeInitFailure(ex));
		}
	}

	private void SetUpTrayAndHotkey() => RebuildTrayAndHotkey();

	/// <summary>
	/// 联机服务器：按需启动。只有前端点了「创建房间」才开监听，离开就关。
	/// 另外留一层空闲兜底 —— 页面刷新 / 前端异常没发 stop 时，15 分钟没人也自动关。
	/// </summary>
	/// <summary>
	/// 启动后台检查一次更新。有新版弹一次对话框；点「去更新」直接进设置页，
	/// 并把这个 ReleaseInfo 塞给设置页，省得它再查一遍。
	/// 只查不装 —— 装包必须用户点，且由独立更新器完成。
	/// </summary>
	private async Task CheckUpdateOnStartupAsync()
	{
		if (!_prefs.AutoCheckUpdate) return;

		// 避开启动高峰（WebView 初始化、首页数据），12 秒后再查
		await Task.Delay(TimeSpan.FromSeconds(12));

		try
		{
			var rel = await UpdateService.GetLatestAsync();
			if (rel is null) return;
			if (!UpdateService.IsNewer(rel.Tag, UpdateService.CurrentVersion)) return;
			if (string.Equals(_prefs.SkippedVersion, rel.Tag, StringComparison.OrdinalIgnoreCase)) return;

			var dialog = new ContentDialog
			{
				XamlRoot = Root.XamlRoot,
				Title = "发现新版本 " + rel.Tag,
				Content = $"当前版本 {UpdateService.CurrentVersion}。\n"
				          + (string.IsNullOrWhiteSpace(rel.Name) ? "" : rel.Name + "\n\n")
				          + "要现在去设置页更新吗？更新完成后程序会自动重启。",
				PrimaryButtonText = "去更新",
				CloseButtonText = "稍后",
				DefaultButton = ContentDialogButton.Primary,
			};

			if (await dialog.ShowAsync() == ContentDialogResult.Primary)
			{
				OpenSettingsFromWeb(restore: false);
				SettingsPage.OfferUpdate(rel);
			}
		}
		catch (Exception ex)
		{
			// 检查失败不打扰用户：可能是没网、GitHub 被墙，或仓库还没有 Windows 版发布
			Diag.Log("startup update check failed: " + ex.Message);
		}
	}

	private void StartBattleServer()
	{
		if (_battle != null) return;
		try
		{
			_battle = new BattleServer();
			_battle.Start();
			_battleActiveAt = DateTime.UtcNow;
			EnsureBattleIdleTimer();
			Diag.Log("battle server started on demand");
		}
		catch (Exception ex) { Diag.Log("battle server start failed: " + ex.Message); }
	}

	private void StopBattleServer()
	{
		if (_battle is null) return;
		try { _battle.Dispose(); } catch { }
		_battle = null;
		Diag.Log("battle server stopped");
	}

	private void EnsureBattleIdleTimer()
	{
		if (_battleIdleTimer != null) return;

		_battleIdleTimer = DispatcherQueue.CreateTimer();
		_battleIdleTimer.Interval = TimeSpan.FromMinutes(2);
		_battleIdleTimer.IsRepeating = true;
		_battleIdleTimer.Tick += (_, _) =>
		{
			if (_battle is null) return;
			if (_battle.HasClients)
			{
				_battleActiveAt = DateTime.UtcNow;
				return;
			}
			if (DateTime.UtcNow - _battleActiveAt >= TimeSpan.FromMinutes(15))
				StopBattleServer();
		};
		_battleIdleTimer.Start();
	}

	private void RebuildTrayAndHotkey()
	{
		_tray?.Dispose();
		_tray = null;
		_hotkey?.Dispose();
		_hotkey = null;

		if (_prefs.TrayEnabled)
		{
			try { _tray = new TrayService(ShowFromTray, ExitApp); }
			catch (Exception ex) { Diag.Log("tray init failed: " + ex.Message); }
		}

		if (_prefs.HotkeyEnabled)
		{
			try
			{
				var hwnd = WindowNative.GetWindowHandle(this);
				_hotkey = new HotkeyService(hwnd, ShowFromTray);
			}
			catch (Exception ex) { Diag.Log("hotkey init failed: " + ex.Message); }
		}
	}

	private static string BuildVersionedUrl()
	{
		try
		{
			var index = Path.Combine(AppPaths.WebRoot, "index.html");
			var stamp = File.Exists(index)
				? File.GetLastWriteTimeUtc(index).Ticks.ToString()
				: "0";
			return $"https://{Host}/index.html?v={stamp}";
		}
		catch
		{
			return $"https://{Host}/index.html";
		}
	}

	private static string DescribeInitFailure(Exception ex)
	{
		if (ex is FileNotFoundException || ex.Message.Contains("0x80070002", StringComparison.OrdinalIgnoreCase))
		{
			return "没有找到 WebView2 运行时。\n请安装 Microsoft Edge WebView2 Runtime 后重试。";
		}
		return "启动失败：" + ex.Message;
	}

	// ── 页面事件 ────────────────────────────────────────────────────────────

	/// <summary>接收前端发来的主题名（simple / ak / ark / jieyuan / liquid / liquidDark）与「打开设置」请求。</summary>
	private void OnWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
	{
		try
		{
			var raw = args.TryGetWebMessageAsString();
			if (string.IsNullOrWhiteSpace(raw)) return;

			using var doc = System.Text.Json.JsonDocument.Parse(raw);
			if (!doc.RootElement.TryGetProperty("type", out var type)) return;

			switch (type.GetString())
			{
				case "theme":
					if (!doc.RootElement.TryGetProperty("value", out var val)) return;
					// 前端自己改的主题：外壳跟色即可。
					// 这里不能重载页面 —— 重载会和 uni 的路由同步抢时序，导致页面类名还是旧主题。
					// 真正的重载只发生在「离开原生设置页」时（_webReloadPending）。
					ApplyShellTheme(val.GetString(), reload: false);
					break;

				case "openSettings":
					// H5 里的「去设置」按钮：不跳 Vue 设置页，直接开原生这一份
					DispatcherQueue.TryEnqueue(() => OpenSettingsFromWeb(restore: false));
					break;

				case "battle":
					// 前端点「创建房间 / 离开」时才开关监听端口。
					// 之前是进程启动就监听 0.0.0.0:23515，装完程序就常驻一个
					// 面向上帝端口的无鉴权服务 —— 既容易被安全软件标红，
					// 也确实给了别人试探的机会。
					var action = doc.RootElement.TryGetProperty("action", out var act)
						? act.GetString()
						: null;
					if (string.Equals(action, "start", StringComparison.OrdinalIgnoreCase))
						DispatcherQueue.TryEnqueue(StartBattleServer);
					else if (string.Equals(action, "stop", StringComparison.OrdinalIgnoreCase))
						DispatcherQueue.TryEnqueue(StopBattleServer);
					break;
			}
		}
		catch (Exception ex)
		{
			Diag.Log("theme message failed: " + ex.Message);
		}
	}

	/// <summary>把前端主题映射到 WinUI 外壳：侧边栏、窗口底、内容卡片一起换色。</summary>
	private void ApplyShellTheme(string? name, bool reload = true)
	{
		var t = ShellTheme.For(name);
		_shellTheme = t;

		Root.RequestedTheme = t.Dark ? ElementTheme.Dark : ElementTheme.Light;
		Root.Background = new SolidColorBrush(t.Canvas);
		ContentArea.Background = new SolidColorBrush(t.Canvas);
		ContentCard.Background = new SolidColorBrush(
			(_settingsVisible || _aeHomeVisible) ? t.Canvas : t.Panel);
		ContentCard.BorderBrush = new SolidColorBrush(t.Border);
		Nav.Background = new SolidColorBrush(t.Pane);

		// 标题栏跟随主题
		AppTitleBar.Background = new SolidColorBrush(t.Pane);
		TitleText.Foreground = new SolidColorBrush(t.Text);
		AeSwitchText.Foreground = new SolidColorBrush(t.Text);

		var tb = AppWindow.TitleBar;
		tb.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
		tb.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
		tb.ButtonForegroundColor = t.Text;
		tb.ButtonInactiveForegroundColor = t.Dark
			? Windows.UI.Color.FromArgb(0xFF, 0x7C, 0x80, 0x7E)
			: Windows.UI.Color.FromArgb(0xFF, 0x9A, 0xA0, 0xA6);
		tb.ButtonHoverBackgroundColor = t.Dark
			? Windows.UI.Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)
			: Windows.UI.Color.FromArgb(0x14, 0x00, 0x00, 0x00);

		var changed = _appliedTheme is not null && !string.Equals(_appliedTheme, name, StringComparison.OrdinalIgnoreCase);
		_appliedTheme = name;
		Diag.Log($"shell theme: {name ?? "liquid"} (dark={t.Dark})");

		SettingsPage.ApplyTheme(t);
		_aboutWindow?.ApplyTheme(t);

		if (!changed) return;

		// 页面主题是在 onLoad 里从 storage 读的，不重新加载不会重绘。
		// 在原生设置页里改主题时先攒着，等离开设置页再统一刷新，避免来回切主题时反复重载。
		if (reload && !_settingsVisible) _core?.Reload();
		else ScheduleSettingsReload();
	}

	private void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
	{
		if (args.IsSuccess)
		{
			Splash.Visibility = Visibility.Collapsed;
			ErrorLayer.Visibility = Visibility.Collapsed;
			SyncNavFromUrl(sender.Source);

			_restoringFromSettings = false;

			// 设置改动触发的自动重载只刷新 H5，不重排设置页 —— 否则滚动位置和展开的弹窗都会被打断
			if (_aeHomeVisible) _ = LoadAeHomeDataAsync();

			if (_settingsVisible)
			{
				if (_suppressSettingsRender) _suppressSettingsRender = false;
				else if (_endfield) _ = LoadAeSettingsIntoViewAsync();
				else _ = LoadSettingsIntoViewAsync();
			}

			if (_startInSettings)
			{
				_startInSettings = false;
				var item = FindNavItem("settings");
				if (item is not null)
				{
					SelectNavItem(item);
				}
				ShowSettings();
			}
		}
		else
		{
			Diag.Log("nav failed: " + args.WebErrorStatus + " src=" + sender.Source);
			ShowError("页面加载失败：" + args.WebErrorStatus);
		}
	}

	// 进程异常退出的恢复：只对「浏览器进程 / 渲染进程」这类真正致命的失败重载。
	//
	// GpuProcessExited、UtilityProcessExited（Storage Service / Network Service）等辅助进程退出后
	// 由 WebView2 内部自动拉起，页面照常运行。以前这里不区分 kind、一律 Retry()，
	// 一次启动里六七个辅助进程退出事件会连锁触发重载风暴，页面永远停在「正在重新加载…」。
	private int _procFailRetries;
	private DateTime _procFailWindow = DateTime.MinValue;

	private void OnProcessFailed(CoreWebView2 sender, CoreWebView2ProcessFailedEventArgs args)
	{
		Diag.Log("webview process failed: " + args.ProcessFailedKind + " desc=" + args.ProcessDescription);

		switch (args.ProcessFailedKind)
		{
			case CoreWebView2ProcessFailedKind.BrowserProcessExited:
			case CoreWebView2ProcessFailedKind.RenderProcessExited:
			case CoreWebView2ProcessFailedKind.RenderProcessUnresponsive:
			case CoreWebView2ProcessFailedKind.FrameRenderProcessExited:
				break;
			default:
				// 辅助进程（GPU / Utility / 沙盒）退出不影响页面，交给 WebView2 自己恢复
				return;
		}

		var now = DateTime.UtcNow;
		if ((now - _procFailWindow).TotalSeconds > 60)
		{
			_procFailWindow = now;
			_procFailRetries = 0;
		}
		if (++_procFailRetries > 3)
		{
			ShowError("页面内核反复退出（" + args.ProcessFailedKind + "）。请点「重试」；若仍不行，关闭程序后删除缓存目录再启动。");
			return;
		}

		ShowError("页面进程异常退出（" + args.ProcessFailedKind + "），正在重新加载…");
		DispatcherQueue.TryEnqueue(async () =>
		{
			await Task.Delay(600);
			Retry();
		});
	}

	private async void OnWebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
	{
		var isLocal = LocalProxy.Handles(args.Request.Uri);
		var isHotlink = !isLocal && ImageProxy.Handles(args.Request.Uri);
		var isRemote = !isLocal && !isHotlink && RemoteProxy.Handles(args.Request.Uri);
		if (!isLocal && !isHotlink && !isRemote) return;

		var deferral = args.GetDeferral();
		try
		{
			// 带自定义头的跨域 GET 会先发 OPTIONS 预检；预检不走上游，直接放行。
			if (!isLocal && !isHotlink
			    && string.Equals(args.Request.Method, "OPTIONS", StringComparison.OrdinalIgnoreCase))
			{
				args.Response = sender.Environment.CreateWebResourceResponse(
					null, 200, "OK", RemoteProxy.PreflightHeaders + "Content-Length: 0\r\n");
				Diag.Log("proxy preflight " + args.Request.Uri);
				return;
			}

			var result = isLocal
				? await _proxy.GetAsync(args.Request.Uri)
				: isHotlink
					? await _imageProxy.GetAsync(args.Request.Uri)
					: await _remoteProxy.GetAsync(args.Request.Uri, RequestHeaders(args.Request.Headers));

			var stream = new InMemoryRandomAccessStream();
			using (var writer = new DataWriter(stream))
			{
				writer.WriteBytes(result.Body);
				await writer.StoreAsync();
				writer.DetachStream();
			}
			stream.Seek(0);

			var headers =
				$"Content-Type: {result.ContentType}\r\n" +
				"Access-Control-Allow-Origin: *\r\n" +
				"Access-Control-Allow-Headers: *\r\n" +
				"Cache-Control: max-age=300\r\n";

			Diag.Log($"proxy {(isLocal ? "gitcode" : isHotlink ? "image" : "remote")} {args.Request.Uri} -> {result.Status} ({result.Body.Length}B)");

			args.Response = sender.Environment.CreateWebResourceResponse(
				stream, result.Status, "OK", headers);
		}
		catch (Exception ex)
		{
			args.Response = sender.Environment.CreateWebResourceResponse(
				null, 502, "Proxy Failed", "Content-Type: text/plain\r\nAccess-Control-Allow-Origin: *\r\n");
			System.Diagnostics.Debug.WriteLine("[proxy] " + ex.Message);
		}
		finally
		{
			deferral.Complete();
		}
	}

	/// <summary>只把上游真正需要的头带过去（Origin / Host / Cookie 不能带，否则上游会按别的主机处理）。</summary>
	private static System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>
		RequestHeaders(CoreWebView2HttpRequestHeaders headers)
	{
		var names = new[]
		{
			"Accept", "Accept-Language", "Cache-Control", "Content-Type",
			"Authorization", "X-Requested-With", "X-Hydrogen-Client-ID", "X-Hydrogen-Environment",
		};

		foreach (var name in names)
		{
			if (!headers.Contains(name)) continue;
			var value = headers.GetHeader(name);
			if (!string.IsNullOrEmpty(value))
				yield return new System.Collections.Generic.KeyValuePair<string, string>(name, value);
		}
	}

	/// <summary>
	/// 页面只能待在壳自己的虚拟域里，站外链接一律交给系统浏览器。
	/// 这样「本程序会不会加载任意网页、往里注入脚本」这类疑问就不存在了，
	/// 顺便也防止 H5 里的外链把用户带进一个没有侧边栏、退不回来的页面。
	/// </summary>
	private async void OnNavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
	{
		if (IsInternalUrl(args.Uri)) return;

		args.Cancel = true;
		try
		{
			if (Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri)
			    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
			{
				await Windows.System.Launcher.LaunchUriAsync(uri);
			}
			else
			{
				Diag.Log("navigation blocked: " + args.Uri);
			}
		}
		catch (Exception ex)
		{
			Diag.Log("nav redirect failed: " + ex.Message);
		}
	}

	private static bool IsInternalUrl(string? uri)
	{
		if (string.IsNullOrWhiteSpace(uri)) return false;
		return uri.StartsWith("https://app.local", StringComparison.OrdinalIgnoreCase)
		       || uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase)
		       || uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
		       || uri.StartsWith("blob:", StringComparison.OrdinalIgnoreCase);
	}

	private async void OnNewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)	{
		args.Handled = true;
		if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var uri)) return;

		if (uri.Scheme is "http" or "https")
		{
			if (string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase))
			{
				sender.Navigate(uri.ToString());   // 站内链接留在应用里
			}
			else
			{
				await Windows.System.Launcher.LaunchUriAsync(uri);   // 站外走系统浏览器
			}
		}
	}

	private async void OnDownloadStarting(CoreWebView2 sender, CoreWebView2DownloadStartingEventArgs args)
	{
		try
		{
			var picker = new FileSavePicker
			{
				SuggestedFileName = Path.GetFileName(args.ResultFilePath),
			};
			picker.FileTypeChoices.Add("文件", new[] { Path.GetExtension(args.ResultFilePath) });

			InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
			var file = await picker.PickSaveFileAsync();

			if (file is null)
			{
				args.Cancel = true;
				return;
			}

			args.ResultFilePath = file.Path;
			args.Handled = true;
		}
		catch
		{
			// 交给 WebView2 默认行为
		}
	}

	private void OnRetryClick(object sender, RoutedEventArgs e) => Retry();

	private void Retry()
	{
		ErrorLayer.Visibility = Visibility.Collapsed;
		Splash.Visibility = Visibility.Visible;
		SplashText.Text = "正在重新加载…";
		try
		{
			_core?.Navigate(_homeUrl);
		}
		catch
		{
			_ = InitWebViewAsync();
		}
	}

	private void ShowError(string message)
	{
		Splash.Visibility = Visibility.Collapsed;
		ErrorText.Text = message;
		ErrorLayer.Visibility = Visibility.Visible;
	}

	// ── 侧边栏导航 ──────────────────────────────────────────────────────────

	private async void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
	{
		if (_syncingNav) return;
		if (args.SelectedItemContainer?.Tag is not string route) return;

		// SelectionChanged 是异步派发的：代码里刚设完 SelectedItem 就复位 _syncingNav，
		// 等事件真的到达时标志已经没了，会被当成用户点击 —— 曾经把刚打开的设置页顶掉。
		// 这里用「预期选中项」再挡一次。
		if (_pendingSelectionTag is not null)
		{
			var same = string.Equals(_pendingSelectionTag, route, StringComparison.OrdinalIgnoreCase);
			_pendingSelectionTag = null;
			if (same) return;
		}

		// 设置页是 WinUI 原生的，不经过 WebView
		if (route == "settings")
		{
			ShowSettings();
			return;
		}

		// 终末地首页也是原生的：只盖一层，不动下面的 WebView（它可能正停在某个工具页上）
		if (_endfield && string.Equals(route, AeHomeRoute, StringComparison.OrdinalIgnoreCase))
		{
			ShowAeHome();
			return;
		}

		HideSettings();
		HideAeHome();
		if (_core is null) return;

		Diag.Log("nav click: " + route);

		_wideLayout = IsWideRoute(route);
		ApplyContentWidth(AppWindow.Size.Width);

		// 设置改过（主题/开关）→ 直接带着目标路由重新加载，避免读到旧配置
		if (_webReloadPending)
		{
			_webReloadPending = false;
			_settingsReloadTimer.Stop();
			var target = $"{_baseUrl}#/{route}";
			// ⚠️ 目标 URL 与当前完全相同时，Navigate 只会被 WebView2 当成「无变化」忽略，
			// 页面不会重载 → 主题/开关还是旧的。这种情况必须显式 Reload。
			if (string.Equals(_core.Source, target, StringComparison.OrdinalIgnoreCase))
				_core.Reload();
			else
				_core.Navigate(target);
			return;
		}

		// uni-app H5 的 uni 挂在全局；若不可用就直接改 hash，路由一样会跟随。
		// 两条路都写在一个表达式里，返回值可直接判断走的是哪条。
		var script =
			"(function(){try{if(typeof uni!=='undefined'&&uni.reLaunch){uni.reLaunch({url:'/" + route + "'});return 'reLaunch';}}" +
			"catch(e){}try{location.hash='#/" + route + "';return 'hash';}catch(e2){return 'fail:'+e2.message;}})()";

		try
		{
			var result = await _core.ExecuteScriptAsync(script);
			Diag.Log("nav result: " + result);
		}
		catch (Exception ex)
		{
			Diag.Log("nav error: " + ex.Message);
		}
	}

	/// <summary>H5 内部跳转后，反向把侧边栏选中项对齐；顺带处理「去设置」与两套工具包的切换。</summary>
	private void SyncNavFromUrl(string url)
	{
		try
		{
			if (string.IsNullOrEmpty(url)) return;
			var fragment = new Uri(url).Fragment;          // 形如 "#/pages/list/list"
			if (string.IsNullOrEmpty(fragment)) return;

			var path = fragment.TrimStart('#').TrimStart('/').Split('?')[0];
			if (path.Length == 0) return;

			// 手机端的「去设置」指向 packageA(或终末地包)/Setting/Setting：
			// 在 Windows 上直接换成原生设置页，并把 WebView 退回刚才那一页，
			// 免得 Vue 设置页占着路由（用户改完设置再返回会看到两套设置）。
			if (IsVueSettingsPath(path))
			{
				OpenSettingsFromWeb(restore: true);
				return;
			}

			// 终末地首页里的「明日方舟」按钮指向旧版手机首页，桌面端应该落到 Dashboard
			if (string.Equals(path, "pages/index/index", StringComparison.OrdinalIgnoreCase))
			{
				SetMode(false, navigate: false);
				NavigateWeb(HomeRoute);
				return;
			}

			_lastWebRoute = path;
			_lastWebUrl = url;

			var endfield = path.StartsWith(AeRoot, StringComparison.OrdinalIgnoreCase);
			if (endfield != _endfield) SetMode(endfield, navigate: false);

			// 终末地首页 → 换成原生页。正在看设置时不打扰（刷新数据会导航到首页，
			// 那一下不该把设置页顶掉）。
			if (_endfield && !_settingsVisible
			    && string.Equals(path, AeHomeRoute, StringComparison.OrdinalIgnoreCase))
			{
				ShowAeHome();
				var homeItem = FindNavItem(path);
				if (homeItem is not null) SelectNavItem(homeItem);
				return;
			}

			var wide = IsWideRoute(path);
			if (wide != _wideLayout)
			{
				_wideLayout = wide;
				ApplyContentWidth(AppWindow.Size.Width);
			}

			var item = FindNavItem(path);
			if (item is null) return;

			SelectNavItem(item);
		}
		catch
		{
			_syncingNav = false;
		}
	}

	private static bool IsVueSettingsPath(string path)
		=> path.EndsWith("Setting/Setting", StringComparison.OrdinalIgnoreCase);

	/// <summary>从 H5 触发（或兜底拦到 Vue 设置页）时，切到原生设置页。</summary>
	private void OpenSettingsFromWeb(bool restore)
	{
		if (!_settingsVisible)
		{
			var item = FindNavItem("settings");
			if (item is not null)
			{
				SelectNavItem(item);
			}
			ShowSettings();
		}

		// JS 侧已经拦掉了 uni.navigateTo，这里是 location.hash 被直接改的兜底：
		// 把 WebView 拉回上一个真实页面（设置页是不透明覆盖层，看不见这次导航）。
		if (!restore || _restoringFromSettings || _core is null || _lastWebUrl is null) return;

		_restoringFromSettings = true;
		try { _core.Navigate(_lastWebUrl); }
		catch (Exception ex) { Diag.Log("restore from settings failed: " + ex.Message); }
	}

	/// <summary>
	/// 程序化选中侧边栏。记下「预期选中项」，让随后异步到达的 SelectionChanged
	/// 不会把它当成用户点击（否则会顺手 HideSettings + 再导航一次）。
	/// </summary>
	private void SelectNavItem(NavigationViewItem? item)
	{
		if (item?.Tag is not string tag) return;
		_pendingSelectionTag = tag;
		_syncingNav = true;
		Nav.SelectedItem = item;
		_syncingNav = false;
	}

	private NavigationViewItem? FindNavItem(string path)
	{
		foreach (var entry in Nav.MenuItems.Concat(Nav.FooterMenuItems))
		{
			if (entry is NavigationViewItem item
				&& item.Tag is string tag
				&& path.StartsWith(tag, StringComparison.OrdinalIgnoreCase))
			{
				return item;
			}
		}
		return null;
	}

	/// <summary>窗口变窄时收起侧边栏、变宽时展开；只在跨阈值时动作，避免和用户手动折叠打架。</summary>
	private void ApplyResponsivePane(int width)
	{
		var narrow = width < 700;
		if (_lastNarrow == narrow) return;
		_lastNarrow = narrow;
		Nav.IsPaneOpen = !narrow;
	}

	/// <summary>
	/// 内容画布宽度（单位 DIP）。
	/// 首页是真正的宽屏 Dashboard，用满可用宽度。
	/// 工具页沿用移动端布局，但画布必须 &gt; 750 DIP：uni-app H5 的根字号在 750 以上会锁定为
	/// 16px（即 375 设计宽度的原始比例），低于 750 则按宽度等比放大 —— 640 时字号会放大 1.7 倍，
	/// 这才是「所有工具页都显大、显丑」的根因。
	/// </summary>
	private void ApplyContentWidth(int windowWidth)
	{
		var dipWidth = windowWidth / GetScale();
		var pane = Nav.IsPaneOpen ? 212 : 52;
		var available = dipWidth - pane - 48;

		double width;
		if (_aeHomeVisible)
			width = Math.Clamp(available, 620, 1180);
		else if (_settingsVisible)
			width = Math.Clamp(available, 560, 920);
		else
			width = _wideLayout
				? Math.Clamp(available, 560, 1180)
				: Math.Clamp(available, 380, 1060);

		ContentHost.Width = width;

		SettingsPage.Width = width;
		AeSettingsPage.Width = width;
		AeHomePage.Width = width;
	}

	/// <summary>窗口 DPI 缩放（1.0 = 96 DPI）。AppWindow 的尺寸是物理像素，XAML 的 Width 是 DIP。</summary>
	private double GetScale()
	{
		try
		{
			var dpi = GetDpiForWindow(WindowNative.GetWindowHandle(this));
			return dpi <= 0 ? 1.0 : dpi / 96.0;
		}
		catch
		{
			return 1.0;
		}
	}

	[DllImport("user32.dll")]
	private static extern uint GetDpiForWindow(IntPtr hwnd);

	// ── 原生设置页（WinUI） ─────────────────────────────────────────────────

	private void WireSettingsPage()
	{
		SettingsPage.ThemePicked += OnSettingsThemePicked;
		SettingsPage.ToggleChanged += OnSettingsToggleChanged;
		SettingsPage.AutoUpdateChanged += OnSettingsAutoUpdateChanged;
		SettingsPage.AvatarPrefChanged += OnAvatarPrefChanged;
		SettingsPage.ShellPreferenceChanged += OnSettingsPreferenceChanged;
		SettingsPage.OpenRepoRequested += OnSettingsOpenRepo;
		SettingsPage.AboutRequested += OnAboutRequested;
		SettingsPage.SetupRequested += () => _ = RunSetupAsync();
		SettingsPage.PageRequested += OnSettingsPageRequested;
		WireAePages();
	}

	private void WireAePages()
	{
		AeHomePage.ToolRequested += route =>
		{
			HideAeHome();
			var item = FindNavItem(route);
			if (item is not null) SelectNavItem(item);
			NavigateWeb(route);
		};
		AeHomePage.RefreshRequested += OnAeRefreshData;
		AeHomePage.SettingsRequested += ShowSettings;
		AeHomePage.AboutRequested += OnAboutRequested;

		AeSettingsPage.ToggleChanged += OnAeToggleChanged;
		AeSettingsPage.AutoUpdateChanged += OnAeAutoUpdateChanged;
		AeSettingsPage.AutoUpdateHoursChanged += OnAeAutoUpdateHoursChanged;
		AeSettingsPage.RefreshRequested += OnAeRefreshData;
		AeSettingsPage.ClearRequested += OnAeClearData;
		AeSettingsPage.PageRequested += OnSettingsPageRequested;
		AeSettingsPage.AboutRequested += OnAboutRequested;
		AeSettingsPage.OpenRepoRequested += OnSettingsOpenRepo;
	}

	private void ShowSettings()
	{
		// 从首页/工具卡片直接打开设置时，侧边栏也要跟着选中「设置」，
		// 否则高亮还停在上一页，看起来像没切过去。
		if (Nav.SelectedItem is not NavigationViewItem { Tag: "settings" })
		{
			var settingsItem = FindNavItem("settings");
			if (settingsItem is not null) SelectNavItem(settingsItem);
		}

		_settingsVisible = true;
		_aeHomeVisible = false;
		AeHomePage.Visibility = Visibility.Collapsed;
		ApplyContentWidth(AppWindow.Size.Width);

		// 注意：不能折叠 ContentHost。WebView2 一旦不可见会被挂起，
		// ExecuteScriptAsync 就读不到 uni 的存储，开关也写不进去。
		// 改成用不透明的设置页盖在它上面。
		ContentCard.Background = new SolidColorBrush(_shellTheme.Canvas);
		SettingsPage.ApplyTheme(_shellTheme);

		if (_endfield)
		{
			SettingsPage.Visibility = Visibility.Collapsed;
			AeSettingsPage.Visibility = Visibility.Visible;
			_ = LoadAeSettingsIntoViewAsync();
		}
		else
		{
			AeSettingsPage.Visibility = Visibility.Collapsed;
			SettingsPage.Visibility = Visibility.Visible;
			_ = LoadSettingsIntoViewAsync();
		}
	}

	/// <summary>
	/// 读设置并渲染。uni 的运行时是异步起来的，刚跳完路由时 uni 还可能不存在，
	/// 直接读会拿到 null → 主题选中项错位，所以这里要重试几次。
	/// </summary>
	private async Task LoadSettingsIntoViewAsync()
	{
		SettingsView.WebSettings? data = null;
		for (var i = 0; i < 10 && data is null; i++)
		{
			if (!_settingsVisible) return;
			data = await ReadWebSettingsAsync();
			if (data is null) await Task.Delay(250);
		}

		if (!_settingsVisible) return;

		var avatar = await ReadAvatarPrefAsync() ?? AvatarPref.CreateDefault();
		SettingsPage.Render(data ?? DefaultWebSettings(), avatar, _prefs);
	}

	private void HideSettings()
	{
		if (!_settingsVisible) return;
		_settingsVisible = false;
		SettingsPage.Visibility = Visibility.Collapsed;
		AeSettingsPage.Visibility = Visibility.Collapsed;
		ContentCard.Background = new SolidColorBrush(_shellTheme.Panel);
		ApplyContentWidth(AppWindow.Size.Width);
	}

	// ── 终末地原生页（首页 / 设置） ─────────────────────────────────────────

	/// <summary>终末地本地数据状态。一次读全，首页和设置页各取所需的字段。</summary>
	private sealed record AeState(
		string DataSource,
		string DataVersion,
		string LastUpdate,
		long Chars,
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

	private void ShowAeHome()
	{
		if (!_endfield) return;
		HideSettings();
		_aeHomeVisible = true;
		AeHomePage.Visibility = Visibility.Visible;
		ContentCard.Background = new SolidColorBrush(_shellTheme.Canvas);
		ApplyContentWidth(AppWindow.Size.Width);
		_ = LoadAeHomeDataAsync();
	}

	private void HideAeHome()
	{
		if (!_aeHomeVisible) return;
		_aeHomeVisible = false;
		AeHomePage.Visibility = Visibility.Collapsed;
		if (!_settingsVisible)
		{
			ContentCard.Background = new SolidColorBrush(_shellTheme.Panel);
		}
		ApplyContentWidth(AppWindow.Size.Width);
	}

	private async Task LoadAeHomeDataAsync()
	{
		AeState? state = null;
		for (var i = 0; i < 12 && state is null; i++)
		{
			if (!_aeHomeVisible) return;
			state = await ReadAeStateAsync();
			if (state is null) await Task.Delay(300);
		}

		if (!_aeHomeVisible) return;
		AeHomePage.Render(ToHomeData(state));
	}

	private async Task LoadAeSettingsIntoViewAsync()
	{
		AeState? state = null;
		for (var i = 0; i < 12 && state is null; i++)
		{
			if (!_settingsVisible) return;
			state = await ReadAeStateAsync();
			if (state is null) await Task.Delay(300);
		}

		if (!_settingsVisible) return;
		AeSettingsPage.Render(ToAeSettings(state));
	}

	private static AeHomeView.AeHomeData ToHomeData(AeState? s)
	{
		if (s is null)
			return new AeHomeView.AeHomeData("未知", "—", "—", "—", false,
				"读取本地数据失败。若页面还在加载，稍等片刻；否则点「刷新数据」重试。");

		var has = s.BoxCount > 0;
		return new AeHomeView.AeHomeData(
			DescribeAeSource(s.DataSource),
			Blank(s.DataVersion),
			has ? s.BoxCount.ToString() : "0",
			Blank(s.LastUpdate),
			has,
			has ? "" : "本机还没有盒号数据。第一次使用需要联网拉取一次（约 1 MB），点右侧「去下载」即可。");
	}

	private static AeSettingsView.AeSettings ToAeSettings(AeState? s)
	{
		s ??= new AeState("", "", "", 0, 0, 0, 0, 0, false, false, false, false, false, false, "", 24);
		return new AeSettingsView.AeSettings(
			s.DataSource, s.DataVersion, s.LastUpdate, FormatChars(s.Chars),
			s.BoxCount, s.GuessCount, s.SearchWordCount, s.CustomWordCount,
			s.EnableEnglish, s.EnableJapanese, s.EnableNickname,
			s.EnableGuess, s.ShowMarketPrice, s.AutoUpdate,
			s.AutoUpdateLast, s.AutoUpdateHours);
	}

	private static string FormatChars(long chars)
	{
		if (chars <= 0) return "";
		var kb = chars / 1024.0;
		return kb < 1024 ? $"约 {kb:0} KB" : $"约 {kb / 1024:0.0} MB";
	}

	private static string DescribeAeSource(string value) => value switch
	{
		"domestic" => "国内源",
		"local" => "本地数据",
		"custom" => "自定义",
		"github" => "GitHub 源",
		_ => "未设置",
	};

	private static string Blank(string value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

	private async Task<AeState?> ReadAeStateAsync()
	{
		if (_core is null) return null;

		var script = "(function(){try{" + StorageHelper +
			"var n=function(v){try{return Array.isArray(v)?v.length:(v&&typeof v==='object'?Object.keys(v).length:0);}catch(e){return 0;}};" +
			"var d=g('ae_arknightsData');if(n(d)===0)d=g('ae_currentData');" +
			"var au=g('ae_autoUpdateSetting');if(!au||typeof au!=='object')au={};" +
			"var raw='';try{raw=localStorage.getItem('ae_arknightsData')||'';}catch(e){}" +
			"return {dataSource:g('ae_dataSource')||''," +
			"dataVersion:g('ae_dataVersion')||''," +
			"lastUpdate:g('ae_localUpdateTime')||''," +
			"chars:raw.length," +
			"boxCount:n(d)," +
			"guessCount:n(g('ae_guessData'))," +
			"searchWordCount:n(g('ae_searchWords'))," +
			"customWordCount:n(g('ae_customSearchWords'))," +
			// 终末地包的开关全部按字符串 'true'/'false' 存
			"enableEnglish:b('ae_enableEnglishSearch')," +
			"enableJapanese:b('ae_enableJapaneseSearch')," +
			"enableNickname:b('ae_enableNicknameSearch')," +
			"enableGuess:b('ae_enableGuessData')," +
			"showMarketPrice:b('ae_showMarketPrice')," +
			"autoUpdate:!!au.enabled," +
			"autoUpdateLast:String(au.lastCheckTime||'')," +
			"autoUpdateHours:Math.round((Number(au.checkInterval)||86400000)/3600000)};" +
			"}catch(e){return null;}})()";

		try
		{
			var raw = await _core.ExecuteScriptAsync(script);
			if (string.IsNullOrWhiteSpace(raw) || raw == "null")
			{
				Diag.Log("ae state null; src=" + _core.Source);
				return null;
			}
			var st = JsonSerializer.Deserialize<AeState>(raw, JsonRead);
			return st;
		}
		catch (Exception ex)
		{
			Diag.Log("read ae state failed: " + ex.Message + " src=" + _core.Source);
			return null;
		}
	}

	private async void OnAeToggleChanged(string key, bool value)
	{
		if (_core is null) return;

		// 终末地包的存储键统一带 ae_ 前缀（与主包的 enableGuessData 等区分开，
		// 少了前缀就会写到主包的命名空间里 —— 两套工具包会互相串味）。
		var fullKey = key.StartsWith("ae_", StringComparison.Ordinal) ? key : "ae_" + key;

		var script = "(function(){try{" + StorageHelper +
			"s(" + JsonSerializer.Serialize(fullKey) + "," + (value ? "'true'" : "'false'") + ");" +
			"return true;}catch(e){return false;}})()";

		try
		{
			await _core.ExecuteScriptAsync(script);
			ScheduleSettingsReload();
		}
		catch (Exception ex) { Diag.Log("ae write failed: " + ex.Message); }
	}

	/// <summary>ae_autoUpdateSetting 是对象（enabled / lastCheckTime / checkInterval…），改了要整份写回。</summary>
	private async void OnAeAutoUpdateChanged(bool value)
	{
		if (_core is null) return;

		var script = "(function(){try{" + StorageHelper +
			"var v=g('ae_autoUpdateSetting');if(!v||typeof v!=='object')v={};" +
			// 首次开启时补齐手机端的默认字段，省得前端读到 undefined 再自己兜底
			"if(typeof v.wifiOnly!=='boolean')v.wifiOnly=true;" +
			"if(typeof v.autoDownload!=='boolean')v.autoDownload=true;" +
			"if(!v.checkFrequency)v.checkFrequency=24;" +
			"if(!v.checkInterval)v.checkInterval=86400000;" +
			"if(typeof v.lastCheckTime!=='string')v.lastCheckTime='';" +
			"v.enabled=" + (value ? "true" : "false") + ";s('ae_autoUpdateSetting',v);" +
			"return true;}catch(e){return false;}})()";

		try
		{
			await _core.ExecuteScriptAsync(script);
			ScheduleSettingsReload();
		}
		catch (Exception ex) { Diag.Log("ae auto update write failed: " + ex.Message); }
	}

	private async void OnAeAutoUpdateHoursChanged(int hours)
	{
		if (_core is null) return;
		var ms = Math.Max(1, hours) * 3600L * 1000L;

		var script = "(function(){try{" + StorageHelper +
			"var v=g('ae_autoUpdateSetting');if(!v||typeof v!=='object')v={};" +
			"v.checkFrequency=" + hours + ";v.checkInterval=" + ms + ";s('ae_autoUpdateSetting',v);" +
			"return true;}catch(e){return false;}})()";

		try
		{
			await _core.ExecuteScriptAsync(script);
			ScheduleSettingsReload();
		}
		catch (Exception ex) { Diag.Log("ae frequency write failed: " + ex.Message); }
	}

	/// <summary>
	/// 清空已下载的数据。注意：手机端 clearAllDownloadedFiles() 里列的键**没有 ae_ 前缀**
	/// （应为笔误，那些键在终末地包里根本不存在），这里按实际使用的键来删。
	/// 只删数据，不动用户的开关偏好。
	/// </summary>
	private async void OnAeClearData()
	{
		if (_core is null) return;

		var keys = new[]
		{
			"ae_arknightsData", "ae_arknightsDataString", "ae_currentData", "ae_currentDataVersion",
			"ae_currentUpdateTime", "ae_dataVersion", "ae_localUpdateTime", "ae_cloudUpdateTime",
			"ae_guessData", "ae_searchWords", "ae_githubCharacters", "ae_githubDataVersion",
			"ae_customData", "ae_customDataInfo", "ae_dataUrl", "ae_searchDataUrl",
		};

		var script = "(function(){try{" + StorageHelper +
			"var ks=" + JsonSerializer.Serialize(keys) + ";" +
			"for(var i=0;i<ks.length;i++){try{localStorage.removeItem(ks[i]);}catch(e){}}" +
			"s('ae_dataSource','domestic');return true;}catch(e){return false;}})()";

		try
		{
			await _core.ExecuteScriptAsync(script);
			Diag.Log("ae data cleared");
			await LoadAeSettingsIntoViewAsync();
		}
		catch (Exception ex) { Diag.Log("ae clear failed: " + ex.Message); }
	}

	/// <summary>
	/// 终末地数据源（写在 package_ArknightEnds 前端里的公开常量，不是密钥）。
	/// 桌面端自己走一遍下载，是因为 H5 那条链路有两处坑：
	/// ① 它把 raw.gitcode.com 改成了 /gitcode（Vite dev 代理路径，静态站点不存在）；
	/// ② 它的报错分支会退回一个**写死的旧 URL**，那个文件里只有 1 个盒号 ——
	///    于是「下载成功」但列表里只有 1 个盒子，看起来像数据错了。
	/// </summary>
	// 知晓云租户 ID：开源版统一用占位符（与前端仓库的 YOUR_KNOW_CLOUD_CLIENT_ID 保持一致）。
	// 想接自己的知晓云，把下面这行换成真实 clientId 即可；保持占位符也能正常用 ——
	// 只是读不到「云端最新数据版本」，会直接走下面的公开镜像兜底。
	private const string AeKnowCloudClientId = "YOUR_KNOW_CLOUD_CLIENT_ID";
	private const string AeKnowCloudBase = "https://" + AeKnowCloudClientId + ".myminapp.com/hserve/v2.2";
	private static bool IsKnowCloudConfigured => AeKnowCloudClientId != "YOUR_KNOW_CLOUD_CLIENT_ID";
	private const string AeFallbackDataUrl =
		"https://raw.gitcode.com/huangjinzhou1/ArknightsAuthorization_Series_End/raw/main/Box_Id.json";

	private sealed record AeCloudVersion(string Url, string Version, string CloudTime);

	private async Task<AeCloudVersion> ReadAeCloudVersionAsync()
	{
		// 未配置知晓云租户：不拼无效域名去请求，直接用公开镜像
		if (!IsKnowCloudConfigured) return new AeCloudVersion(AeFallbackDataUrl, "", "");

		var headers = new[]
		{
			new System.Collections.Generic.KeyValuePair<string, string>("X-Hydrogen-Client-ID", AeKnowCloudClientId),
		};

		var res = await _remoteProxy.GetAsync(
			AeKnowCloudBase + "/table/Version/record/?limit=1&offset=0", headers);
		if (res.Status != 200) throw new Exception("version http " + res.Status);

		using var doc = JsonDocument.Parse(res.Body);
		var root = doc.RootElement;
		if (!root.TryGetProperty("objects", out var list) || list.GetArrayLength() == 0)
			throw new Exception("version: no objects");

		var obj = list[0];
		string Str(string name) =>
			obj.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? v.ToString() : "";

		var url = Str("url");
		var cloud = Str("updated_at");
		if (cloud.Length == 0) cloud = Str("created_at");
		return new AeCloudVersion(url, Str("Version"), cloud);
	}

	/// <summary>
	/// 重新拉数据：桌面端自己下载并写回 uni 的存储（与 H5 的 downloadData 等价）。
	///
	/// 写入格式必须和 uni-h5 一致：字符串直接存，对象存 {"type":"object","data":...}。
	/// 字段清洗也照抄前端：Box_id 与其它字段转字符串，character* 原样保留
	/// （否则列表页按字符串比较数字会出问题）。
	/// </summary>
	private async void OnAeRefreshData()
	{
		if (_core is null) return;

		AeHomePage.SetBusy(true);
		AeSettingsPage.SetBusy(true);

		var before = await ReadAeStateAsync();
		string? error = null;
		var count = -1;
		string version = "", dataUrl = "", cloud = "";

		try
		{
			var info = await ReadAeCloudVersionAsync();
			version = info.Version;
			cloud = info.CloudTime;
			dataUrl = string.IsNullOrWhiteSpace(info.Url) ? AeFallbackDataUrl : info.Url;

			var download = await _remoteProxy.GetAsync(dataUrl);
			if (download.Status != 200) throw new Exception("data http " + download.Status);

			var json = System.Text.Encoding.UTF8.GetString(download.Body);
			var localTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

			var script = "(function(){try{var raw=" + json + ";"
				+ "if(!Array.isArray(raw))return 'notarray';"
				+ "var cleaned=raw.map(function(box){var o={};for(var k in box){"
				+ "if(k==='Box_id'){o[k]=String(box[k]||'');}"
				+ "else if(k.indexOf('character')===0){o[k]=box[k];}"
				+ "else{o[k]=String(box[k]||'');}}return o;});"
				+ "localStorage.setItem('ae_arknightsData',JSON.stringify({type:'object',data:cleaned}));"
				+ "localStorage.setItem('ae_localUpdateTime'," + JsonSerializer.Serialize(localTime) + ");"
				+ "localStorage.setItem('ae_dataVersion'," + JsonSerializer.Serialize(version) + ");"
				+ "localStorage.setItem('ae_cloudUpdateTime'," + JsonSerializer.Serialize(cloud) + ");"
				+ "localStorage.setItem('ae_dataUrl'," + JsonSerializer.Serialize(dataUrl) + ");"
				+ "localStorage.setItem('ae_dataSource','domestic');"
				+ "return 'ok:'+cleaned.length;}catch(e){return 'err:'+e.message;}})()";

			var result = await _core.ExecuteScriptAsync(script);
			Diag.Log("ae download result: " + result);

			if (result is not null && result.Contains("ok:"))
			{
				var tail = result.Substring(result.IndexOf("ok:", StringComparison.Ordinal) + 3).Trim('"');
				_ = int.TryParse(tail, out count);
			}
			else
			{
				error = "写入本地数据失败：" + (result ?? "null");
			}
		}
		catch (Exception ex)
		{
			error = ex.Message;
			Diag.Log("ae download failed: " + ex.Message);
		}

		var after = await ReadAeStateAsync();
		AeHomePage.SetBusy(false);
		AeSettingsPage.SetBusy(false);

		if (_aeHomeVisible)
		{
			AeHomePage.Render(error is null
				? ToHomeData(after)
				: new AeHomeView.AeHomeData(
					DescribeAeSource(after?.DataSource ?? ""),
					Blank(after?.DataVersion ?? ""),
					after?.BoxCount.ToString() ?? "0",
					Blank(after?.LastUpdate ?? ""),
					(after?.BoxCount ?? 0) > 0,
					"下载失败：" + error + "。请检查网络后重试。"));
		}

		if (_settingsVisible && _endfield) AeSettingsPage.Render(ToAeSettings(after));
		Diag.Log($"ae data refreshed (boxes={count}, before={before?.BoxCount ?? 0})");
	}

	private static SettingsView.WebSettings DefaultWebSettings() => new(
		"liquid", false, false, false, false, false, true, false, "domestic", "", "");

	/// <summary>
	/// 设置改完后统一走这里：先写 localStorage，再防抖重载 WebView。
	/// 重载时不重排设置页（_suppressSettingsRender），滚动位置不跳。
	/// </summary>
	private void ScheduleSettingsReload()
	{
		_webReloadPending = true;
		if (_settingsReloadTimer is null) return;   // 构造期（主题初始化）还没建定时器
		_settingsReloadTimer.Stop();
		_settingsReloadTimer.Start();
	}

	private void ReloadFromSettings()
	{
		_webReloadPending = false;
		if (_core is null || !_settingsVisible) return;

		// 终末地首页下面压着的就是首页本体，而首页不消费这些开关（它已经被原生页替代了）。
		// 这时候重载只会白白重拉一遍数据，跳过。
		if (_endfield && string.Equals(_lastWebRoute, AeHomeRoute, StringComparison.OrdinalIgnoreCase))
		{
			Diag.Log("settings changed → skip reload (ae home underneath)");
			return;
		}
		_suppressSettingsRender = true;
		try
		{
			// 不能只 Reload()：WebView2 的 Reload 会回到「最初加载的那个地址」，
			// 页面里的 hash 路由（用户正在看列表/收藏）会一起丢掉，等于把人踢回首页。
			// 这里显式回到最近一次的真实路由。
			var target = _lastWebUrl ?? $"{_baseUrl}#/{(_endfield ? AeHomeRoute : HomeRoute)}";
			_restoringFromSettings = true;
			_core.Navigate(target);
			Diag.Log("settings changed → refresh h5: " + target);
		}
		catch (Exception ex)
		{
			Diag.Log("settings reload failed: " + ex.Message);
		}
	}

	/// <summary>原生设置页里的「管理自定义搜索词」等按钮：关掉设置页再跳 H5。</summary>
	private void OnSettingsPageRequested(string route)
	{
		HideSettings();
		var item = FindNavItem(route);
		if (item is not null)
		{
			SelectNavItem(item);
		}
		NavigateWeb(route);
	}

	/// <summary>
	/// uni 的存储编码（uni-h5 源码）：字符串直接存；其它类型存 {"type":...,"data":...}。
	/// 读的时候反向解析。必须按这套来写，前端 getStorageSync 才认。
	///
	/// ⚠️ 不能用 window.uni：App 内嵌的 H5 里 uni 是个空对象（uni 只在模块作用域里），
	/// 之前调 uni.getStorageSync 全部静默失败 —— 主题只改了外壳，存储没变，
	/// 1 秒后主题桥又把外壳拉回旧主题。
	/// </summary>
	private const string StorageHelper =
		"var g=function(k){try{var v=localStorage.getItem(k);if(v===null)return null;" +
		"try{var o=JSON.parse(v);if(o&&typeof o==='object'&&'type' in o&&'data' in o)return o.data;}catch(e){}" +
		"return v;}catch(e){return null;}};"
		+ "var s=function(k,v){var t=typeof v;localStorage.setItem(k,t==='string'?v:JSON.stringify({type:t,data:v}));};"
		+ "var b=function(k){var v=g(k);return v===true||v==='true';};";

	private async Task<SettingsView.WebSettings?> ReadWebSettingsAsync()
	{
		if (_core is null) return null;

		var script =
			"(function(){try{" + StorageHelper +
			"var au=g('autoUpdateSetting');if(typeof au!=='object'||au===null)au={};" +
			"var un=g('updateNotifyEnabled');" +
			"return {theme:g('themeMode')||'liquid'," +
			"showMarketPrice:b('showMarketPrice')," +
			"enableEnglishSearch:b('enableEnglishSearch')," +
			"enableJapaneseSearch:b('enableJapaneseSearch')," +
			"enableNicknameSearch:b('enableNicknameSearch')," +
			"enableGuessData:b('enableGuessData')," +
			// updateNotifyEnabled 手机端存的是布尔，缺省视为开启
			"updateNotify:(un===null||un==='')?true:(un===true||un==='true')," +
			"autoUpdate:!!au.enabled," +
			"dataSource:g('dataSource')||''," +
			"dataVersion:g('dataVersion')||''," +
			"lastUpdate:g('localUpdateTime')||''};" +
			"}catch(e){return null;}})()";

		try
		{
			var raw = await _core.ExecuteScriptAsync(script);
			if (string.IsNullOrWhiteSpace(raw) || raw == "null") return null;
			return JsonSerializer.Deserialize<SettingsView.WebSettings>(raw, JsonRead);
		}
		catch (Exception ex)
		{
			Diag.Log("read settings failed: " + ex.Message);
			return null;
		}
	}

	/// <summary>
	/// 写开关。showMarketPrice 在手机端按字符串 'true'/'false' 存（页面里是 === 'true' 判断），
	/// 其余开关按布尔存。
	/// </summary>
	/// <summary>读「干员头像个性化」整份设置（存的是 JSON 字符串，需要二次解析）。</summary>
	private async Task<AvatarPref?> ReadAvatarPrefAsync()
	{
		if (_core is null) return null;

		var script = "(function(){try{" + StorageHelper +
			"var v=g('avatarPrefSettings');" +
			"if(typeof v==='string'){try{v=JSON.parse(v);}catch(e){v=null;}}" +
			"return JSON.stringify(v||null);}catch(e){return 'null';}})()";

		try
		{
			var raw = await _core.ExecuteScriptAsync(script);
			if (string.IsNullOrWhiteSpace(raw) || raw == "null") return null;

			// ExecuteScriptAsync 返回的是 JS 值的 JSON 文本：先还原成字符串，再解析设置本身
			var json = JsonSerializer.Deserialize<string>(raw);
			return string.IsNullOrWhiteSpace(json) ? null : AvatarPref.FromJson(json);
		}
		catch (Exception ex)
		{
			Diag.Log("read avatar pref failed: " + ex.Message);
			return null;
		}
	}

	/// <summary>
	/// 这些开关在手机端是按字符串 'true' / 'false' 存的（页面里普遍写 === 'true'），
	/// 写成布尔会「看着改了、实际没生效」—— 必须按各页面的读法写。
	/// </summary>
	private static readonly System.Collections.Generic.HashSet<string> StringBoolKeys = new(StringComparer.Ordinal)
	{
		"showMarketPrice", "enableEnglishSearch", "enableJapaneseSearch",
		"enableNicknameSearch", "enableGuessData",
	};

	private async void OnSettingsToggleChanged(string key, bool value)
	{
		if (_core is null) return;

		var literal = StringBoolKeys.Contains(key)
			? (value ? "'true'" : "'false'")
			: (value ? "true" : "false");

		var script = "(function(){try{" + StorageHelper +
			"s(" + JsonSerializer.Serialize(key) + "," + literal + ");return true;}catch(e){return false;}})()";

		try
		{
			await _core.ExecuteScriptAsync(script);
			ScheduleSettingsReload();
		}
		catch (Exception ex) { Diag.Log("write setting failed: " + ex.Message); }
	}

	/// <summary>autoUpdateSetting 是对象，读出来改 enabled 再写回，不能整个覆盖。</summary>
	private async void OnSettingsAutoUpdateChanged(bool value)
	{
		if (_core is null) return;

		var script = "(function(){try{" + StorageHelper +
			"var v=g('autoUpdateSetting');if(!v||typeof v!=='object')v={};v.enabled=" +
			(value ? "true" : "false") + ";s('autoUpdateSetting',v);return true;}catch(e){return false;}})()";

		try
		{
			await _core.ExecuteScriptAsync(script);
			ScheduleSettingsReload();
		}
		catch (Exception ex) { Diag.Log("write autoUpdate failed: " + ex.Message); }
	}

	/// <summary>
	/// 头像个性化：整份 JSON 写回 avatarPrefSettings。
	/// 手机端 saveSettings() 存的也是 JSON 字符串，页面 getSettings() 会自己 parse。
	/// </summary>
	private async void OnAvatarPrefChanged(AvatarPref pref)
	{
		if (_core is null) return;

		var json = pref.ToJson();
		var script = "(function(){try{" + StorageHelper +
			"s('avatarPrefSettings'," + JsonSerializer.Serialize(json) + ");return true;}catch(e){return false;}})()";

		try
		{
			await _core.ExecuteScriptAsync(script);
			ScheduleSettingsReload();
		}
		catch (Exception ex) { Diag.Log("write avatar pref failed: " + ex.Message); }
	}

	private async void OnSettingsThemePicked(string name)
	{
		ApplyShellTheme(name);
		if (_core is null) return;

		var script = "(function(){try{" + StorageHelper +
			"s('themeMode'," + JsonSerializer.Serialize(name) + ");return true;}catch(e){return false;}})()";
		try { await _core.ExecuteScriptAsync(script); }
		catch (Exception ex) { Diag.Log("write theme failed: " + ex.Message); }
	}

	private void OnSettingsPreferenceChanged(AppPreferences prefs)
	{
		prefs.Save();
		RebuildTrayAndHotkey();
	}

	/// <summary>「关于我们」：独立竖屏小窗，复用 packageA/aboutus 的 Vue 页面。</summary>
	private void OnAboutRequested()
	{
		// 已开着就拉到前面，不再开第二个
		if (_aboutWindow is not null)
		{
			_aboutWindow.Activate();
			return;
		}

		var window = new AboutWindow(this, _shellTheme);
		window.Closed += (_, _) => _aboutWindow = null;
		_aboutWindow = window;
		window.Activate();
	}

	private async void OnSettingsOpenRepo()
	{
		try
		{
			await Windows.System.Launcher.LaunchUriAsync(
				new Uri("https://github.com/awadwd/Arknights-Authorization-Tool"));
		}
		catch (Exception ex)
		{
			Diag.Log("open repo failed: " + ex.Message);
		}
	}

	// ── 窗口 ────────────────────────────────────────────────────────────────

	private void ApplyBackdrop()
	{
		try
		{
			if (MicaController.IsSupported())
			{
				SystemBackdrop = new MicaBackdrop();
			}
		}
		catch
		{
			// Win10 上没有 Mica，用默认背景即可
		}
	}

	private void RestoreWindowState()
	{
		var saved = WindowStateStore.Load();

		if (saved is not null && WindowStateStore.IsVisible(saved))
		{
			// 旧版本可能存过小到布局会破的尺寸：先按最小尺寸夹一下再恢复
			var (minW, minH) = MinWindowPixelSize(WindowNative.GetWindowHandle(this));
			var restoredW = Math.Max(saved.Width, minW);
			var restoredH = Math.Max(saved.Height, minH);
			AppWindow.MoveAndResize(new RectInt32(saved.X, saved.Y, restoredW, restoredH));
			if (saved.Maximized && AppWindow.Presenter is OverlappedPresenter p)
			{
				p.Maximize();
			}
			_lastNormalRect = (new PointInt32(saved.X, saved.Y), new SizeInt32(restoredW, restoredH));
			return;
		}

		// 首次启动：桌面横屏比例，屏幕居中
		var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
		var w = Math.Min(1440, (int)(area.WorkArea.Width * 0.76));
		var h = Math.Min(940, (int)(area.WorkArea.Height * 0.88));
		var x = area.WorkArea.X + (area.WorkArea.Width - w) / 2;
		var y = area.WorkArea.Y + (area.WorkArea.Height - h) / 2;
		AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
		_lastNormalRect = (new PointInt32(x, y), new SizeInt32(w, h));
	}

	private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
	{
		if (args.DidSizeChange)
		{
			ApplyResponsivePane(sender.Size.Width);
			ApplyContentWidth(sender.Size.Width);
		}

		if (sender.Presenter is OverlappedPresenter p && p.State == OverlappedPresenterState.Maximized) return;
		if (!args.DidSizeChange && !args.DidPositionChange) return;

		_lastNormalRect = (sender.Position, sender.Size);
		_saveTimer.Stop();
		_saveTimer.Start();
	}

	private void SaveWindowState()
	{
		var rect = _lastNormalRect;
		if (rect is null) return;

		var maximized = AppWindow.Presenter is OverlappedPresenter p
			&& p.State == OverlappedPresenterState.Maximized;

		WindowStateStore.Save(new WindowStateData(
			rect.Value.Pos.X, rect.Value.Pos.Y,
			rect.Value.Size.Width, rect.Value.Size.Height,
			maximized));
	}

	private void OnActivated(object sender, WindowActivatedEventArgs args)
	{
		if (args.WindowActivationState != WindowActivationState.Deactivated)
		{
			_saveTimer.Stop();
		}
	}

	private void OnClosing(AppWindow sender, AppWindowClosingEventArgs args)
	{
		SaveWindowState();

		if (_reallyExit || !_prefs.CloseToTray || _tray is null)
		{
			_aboutWindow?.Close();
			return;
		}

		// 关窗口不退出，收进托盘；「关于我们」小窗一并藏起来，免得只剩它孤零零挂在桌面上
		args.Cancel = true;
		_aboutWindow?.AppWindow.Hide();
		AppWindow.Hide();
	}

	public void ShowFromTray()
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			AppWindow.Show();
			Activate();

			var hwnd = WindowNative.GetWindowHandle(this);
			if (IsIconic(hwnd))
			{
				ShowWindow(hwnd, SW_RESTORE);
			}
			SetForegroundWindow(hwnd);
		});
	}

	private void ExitApp()
	{
		_reallyExit = true;
		SaveWindowState();
		_battle?.Dispose();
		_battle = null;
		_tray?.Dispose();
		_tray = null;
		_hotkey?.Dispose();
		_hotkey = null;
		Close();
	}

	// ── 最小窗口尺寸 ────────────────────────────────────────────────────────
	// WinUI 3 的 AppWindow 没有 MinSize API（OverlappedPresenter 只管最大化/最小化），
	// 只能在 WM_GETMINMAXINFO 里改 ptMinTrackSize。窗口过程被 WinUI 托管，
	// 所以用 comctl32 的 SetWindowSubclass 挂一层，处理完再交给 DefSubclassProc。
	//
	// 数值按 DIP 定义，运行时乘窗口 DPI 换算成物理像素 —— 这样 100% / 125% / 150%
	// 缩放下，用户看到的内容一样大。1040×578 DIP 对应 125% 下的 1301×722 物理像素，
	// 也就是侧栏展开 + 首页 dashboard 刚好排得下、再窄就要换行/溢出的那个临界点。
	private const double MinWindowWidthDip = 1040;
	private const double MinWindowHeightDip = 578;
	private const int WM_GETMINMAXINFO = 0x0024;
	private const uint MinSizeSubclassId = 0x4B41;   // 'KA'

	[StructLayout(LayoutKind.Sequential)]
	private struct NativePoint
	{
		public int X;
		public int Y;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct MinMaxInfo
	{
		public NativePoint Reserved;
		public NativePoint MaxSize;
		public NativePoint MaxPosition;
		public NativePoint MinTrackSize;
		public NativePoint MaxTrackSize;
	}

	private delegate IntPtr SubclassProc(
		IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data);

	// 委托必须常驻：被 GC 回收后窗口消息再进来就是访问已释放内存，直接崩
	private static readonly SubclassProc _minSizeProc = OnMinSizeMessage;

	[DllImport("comctl32.dll", SetLastError = true)]
	private static extern bool SetWindowSubclass(
		IntPtr hWnd, SubclassProc proc, UIntPtr id, IntPtr data);

	[DllImport("comctl32.dll")]
	private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	private static (int W, int H) MinWindowPixelSize(IntPtr hwnd)
	{
		var dpi = GetDpiForWindow(hwnd);
		var scale = (dpi == 0 ? 96u : dpi) / 96.0;
		return ((int)Math.Round(MinWindowWidthDip * scale),
		        (int)Math.Round(MinWindowHeightDip * scale));
	}

	private static IntPtr OnMinSizeMessage(
		IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr id, IntPtr data)
	{
		if (msg == WM_GETMINMAXINFO && lParam != IntPtr.Zero)
		{
			try
			{
				var info = Marshal.PtrToStructure<MinMaxInfo>(lParam);
				var (w, h) = MinWindowPixelSize(hWnd);
				info.MinTrackSize.X = w;
				info.MinTrackSize.Y = h;
				Marshal.StructureToPtr(info, lParam, false);
			}
			catch
			{
				// 结构体读写失败就交回系统默认值，别把窗口拖不动了
			}
		}
		return DefSubclassProc(hWnd, msg, wParam, lParam);
	}

	private void ApplyMinWindowSize()
	{
		try
		{
			var hwnd = WindowNative.GetWindowHandle(this);
			if (!SetWindowSubclass(hwnd, _minSizeProc, (UIntPtr)MinSizeSubclassId, IntPtr.Zero))
				Diag.Log("min size hook failed: err=" + Marshal.GetLastWin32Error());
		}
		catch (Exception ex)
		{
			Diag.Log("min size hook threw: " + ex.Message);
		}
	}

	private const int SW_RESTORE = 9;

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(IntPtr hWnd);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern bool IsIconic(IntPtr hWnd);
}
