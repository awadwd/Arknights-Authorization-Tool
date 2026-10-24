using System.Text.Json;

namespace AkPassport.Win.Services;

/// <summary>
/// 注入到 H5 的桌面端补丁（只在 Windows 壳里生效，不影响小程序 / App / 手机 H5）。
///
/// 为什么要强制根字号：uni-app H5 的 rpx 按窗口宽度换算 rem，
/// 换算时把 750rpx 当「设计稿宽度」，窗口越宽整页等比放大。
/// 实测根字号 = 宽度 / 750 * 32，直到宽度超过 960 才回落到 16px。
/// 桌面内容区通常 800 左右 → 根字号 34px，字号放大 2 倍多，页面自然又大又糙。
/// 这里用 !important 覆盖 uni 写在 html 上的内联 font-size，让整套界面回到设计稿比例。
/// </summary>
internal static class DesktopPatches
{
	/// <summary>桌面端根字号（16px = 移动端 375 设计稿原尺寸，18px 略放大更适合桌面阅读）。</summary>
	private const double RootFontPx = 18;

	/// <summary>页面一创建就注入，避免首屏用放大的字号闪一下。</summary>
	public static string ScaleScript { get; } = BuildScaleScript();

	/// <summary>
	/// 主题桥：轮询 localStorage.themeMode，变化时报给 WinUI 外壳，
	/// 让侧边栏 / 内容卡片跟着前端的 6 套主题一起换色。
	/// 用轮询而不是改前端代码，是为了不动那 21 个页面的主题逻辑。
	/// </summary>
	public static string ThemeBridgeScript { get; } = BuildThemeBridge();

	/// <summary>
	/// Windows 版不提供「简约（simple）」主题：默认就是液态玻璃（liquid）。
	/// 老用户 localStorage 里可能还是 simple 或空，在页面脚本执行前改写掉，
	/// 这样首屏不会先闪一下简约风格。
	/// </summary>
	/// <summary>
	/// 桌面端「干净窗口」：顶层页面由侧边栏导航，不需要 uni-app H5 自带的
	/// 那条 44px 标题栏（它还会在滚动时盖住页面工具栏）。
	/// 只对侧边栏里的 9 个顶层路由生效，二级页（盒号详情等）仍然保留返回按钮。
	/// </summary>
	public static string ChromeScript { get; } = BuildChromeScript();

	public static string ThemeDefaultScript { get; } =
		"(function(){try{var k='themeMode',v=localStorage.getItem(k);"
		+ "if(!v||v==='simple'){localStorage.setItem(k,'liquid');}}catch(e){}})();";

	/// <summary>
	/// 「去设置」桥：H5 里所有「去设置」最终都是
	/// <c>uni.navigateTo({url:'/packageA/Setting/Setting'})</c>（终末地包是 package_ArknightEnds）。
	/// Windows 版有自己的一份原生设置页，这里把三个导航 API 包一层：
	/// 命中设置路由就只发消息给外壳，不真的跳页 —— 用户看不到 Vue 设置页闪一下，
	/// 页面栈也不会被顶掉。location.hash 被直接改的情况由外壳在 SourceChanged 里兜底。
	/// </summary>
	public static string SettingsBridgeScript { get; } = BuildSettingsBridge();

	/// <summary>
	/// 数据源重写：H5 构建里把 <c>https://raw.gitcode.com</c> 写成了 <c>/gitcode</c>
	/// （原本是 Vite dev server 的代理路径）。静态站点/WebView 里这个路径根本不存在，
	/// 请求会直接以 status 0 失败 —— 表现就是「下载数据失败」。
	///
	/// 这里在 XHR / fetch 层把它改回真实域名，交给 RemoteProxy（带 UA + CORS 头 + 预检）去取。
	/// 比在 WebView 里注册 /gitcode 过滤器可靠：虚拟主机映射会先把这个路径吃掉。
	/// </summary>
	public static string RemoteRewriteScript { get; } = BuildRemoteRewrite();

	private static string BuildRemoteRewrite()
	{
		return "(function(){if(window.__akRemoteRewrite)return;window.__akRemoteRewrite=true;"
			+ "function fix(u){try{if(typeof u!=='string')return u;var i=u.indexOf('/gitcode/');"
			+ "if(i<0)return u;return 'https://raw.gitcode.com/'+u.slice(i+9);}catch(e){return u;}}"
			+ "try{var open=XMLHttpRequest.prototype.open;XMLHttpRequest.prototype.open=function(m,u){"
			// ⚠️ 不能写成 open.call(this,m,fix(u),arguments[2],...)：
			// 少参数时 async 会变成 undefined → 被当成同步请求，跨域直接抛错。
			// 这里保留原始实参表，只替换第 2 个（url）。
			+ "var a=[].slice.call(arguments);if(a.length>1)a[1]=fix(a[1]);return open.apply(this,a);};}catch(e){}"
			+ "try{var of=window.fetch;if(of){window.fetch=function(input,init){"
			+ "try{if(typeof input==='string'){input=fix(input);}"
			+ "else if(input&&input.url){var nu=fix(input.url);if(nu!==input.url)input=new Request(nu,input);}}catch(e){}"
			+ "return of.call(this,input,init);};}}catch(e){}"
			+ "})();";
	}

	/// <summary>
	/// 禁止页面缩放。
	///
	/// 桌面端不该跟着缩放：Ctrl+滚轮、触控板双指捏合（浏览器把它翻译成 ctrl+wheel）、
	/// Ctrl +/- 都会让整个 uni-app 页面忽大忽小，破坏按 18px 根字号排好的版式。
	///
	/// 两层一起上：
	/// ① WebView2 设置 IsZoomControlEnabled / IsPinchZoomEnabled = false（见 MainWindow）；
	/// ② 这里拦页面能收到的事件 —— 设置只管浏览器默认行为，页面自己监听的事件照样会触发。
	/// </summary>
	public static string ZoomGuardScript { get; } = BuildZoomGuard();

	private static string BuildZoomGuard()
	{
		return "(function(){if(window.__akZoomGuard)return;window.__akZoomGuard=true;"
			// 触控板捏合 / Ctrl+滚轮
			+ "window.addEventListener('wheel',function(e){"
			+ "if(e.ctrlKey||e.metaKey){e.preventDefault();}},{passive:false,capture:true});"
			// WebKit 手势事件（内嵌 WebView 偶尔会转发过来）
			+ "['gesturestart','gesturechange','gestureend'].forEach(function(t){"
			+ "window.addEventListener(t,function(e){e.preventDefault();},{passive:false,capture:true});});"
			// Ctrl +/-/0
			+ "window.addEventListener('keydown',function(e){"
			+ "if(!(e.ctrlKey||e.metaKey))return;var k=e.key;"
			+ "if(k==='+'||k==='-'||k==='='||k==='_'||k==='0'){e.preventDefault();e.stopPropagation();}},true);"
			+ "})();";
	}

	private static string BuildChromeScript()
	{
		const string tops =
			"['pages/desktop_home/desktop_home','pages/Search/Search','pages/list/list',"
			+ "'packageB/pages/gacha_simulated/gacha_simulated','packageB/pages/mystarbox/mystarbox',"
			+ "'pages/card_share/card_share','packageA/calendar/calendar',"
			+ "'pages/daily_recommend/daily_recommend','pages/box_feedback_new/box_feedback_new',"
			// 终末地包：同样是侧边栏顶层页面，别再叠一条 uni 自带标题栏
			+ "'package_ArknightEnds/index/index','package_ArknightEnds/Search/Search',"
			+ "'package_ArknightEnds/list/list','package_ArknightEnds/calendar/calendar',"
			+ "'package_ArknightEnds/gacha_simulated/gacha_simulated',"
			+ "'package_ArknightEnds/custom-words/custom-words',"
			+ "'package_ArknightEnds/more-notice/more-notice']";

		const string css =
			"html.ak-flat-head uni-page-head{display:none!important}" +
			"html.ak-flat-head uni-page-body{top:0!important}";

		return "(function(){var TOPS=" + tops + ";"
			+ "function applyCss(){try{if(document.getElementById('ak-flat-head-css'))return;"
			+ "var s=document.createElement('style');s.id='ak-flat-head-css';"
			+ "s.textContent=" + JsonSerializer.Serialize(css) + ";"
			+ "var host=document.head||document.documentElement;if(host)host.appendChild(s);}catch(e){}}"
			+ "function check(){try{var h=(location.hash||'').split('#/')[1]||'';h=h.split('?')[0];"
			+ "var flat=TOPS.indexOf(h)>-1;"
			+ "document.documentElement.classList.toggle('ak-flat-head',flat);}catch(e){}}"
			+ "document.addEventListener('DOMContentLoaded',function(){applyCss();check();});"
			+ "window.addEventListener('hashchange',check);"
			+ "setInterval(check,700);applyCss();check();})();";
	}

	private static string BuildThemeBridge()
	{
		return "(function(){if(window.__akThemeBridge)return;window.__akThemeBridge=true;var last=null;"
			+ "function push(){try{var m=localStorage.getItem('themeMode')||'simple';"
			+ "if(m!==last){last=m;window.chrome.webview.postMessage(JSON.stringify({type:'theme',value:m}));}}catch(e){}}"
			+ "push();setInterval(push,1000);})();";
	}

	private static string BuildSettingsBridge()
	{
		return "(function(){if(window.__akSettingsBridge)return;window.__akSettingsBridge=true;"
			+ "var RE=/(?:^|\\/)(?:packageA|package_ArknightEnds)\\/Setting\\/Setting/;"
			+ "function isSettings(u){try{return RE.test(String(u||'').split('?')[0]);}catch(e){return false;}}"
			+ "function ask(){try{window.chrome.webview.postMessage(JSON.stringify({type:'openSettings'}));return true;}catch(e){return false;}}"
			+ "function patch(){try{if(typeof uni==='undefined'||!uni)return false;"
			+ "['navigateTo','redirectTo','reLaunch','switchTab'].forEach(function(m){"
			+ "var fn=uni[m];if(typeof fn!=='function'||fn.__akShellWrapped)return;"
			+ "var wrapped=function(opts){try{if(opts&&isSettings(opts.url)){ask();return;}}catch(e){}return fn.apply(uni,arguments);};"
			+ "wrapped.__akShellWrapped=true;uni[m]=wrapped;});return true;}catch(e){return false;}}"
			// uni 的运行时可能在 document 创建之后才挂上 window，这里轮询几十秒即可
			+ "var n=0;var t=setInterval(function(){patch();if(++n>120)clearInterval(t);},250);"
			+ "document.addEventListener('DOMContentLoaded',patch);"
			+ "window.addEventListener('pageshow',patch);"
			// 兜底：uni 不可用时（壳内 window.uni 可能是空对象）页面会直接改 hash
			+ "window.addEventListener('hashchange',function(){try{if(isSettings(decodeURIComponent(location.hash)))ask();}catch(e){}});"
			+ "})();";
	}

	private static string BuildScaleScript()
	{
		var css = $"html{{font-size:{RootFontPx}px!important}}";
		// 注意：脚本在 document 刚创建时执行，此时 documentElement 可能还不存在，
		// apply() 必须自带 try/catch，否则异常会让后面的监听器注册不上。
		return "(function(){var CSS='" + css + "';"
			+ "function apply(){try{if(document.getElementById('ak-desktop-scale'))return;"
			+ "var s=document.createElement('style');s.id='ak-desktop-scale';s.textContent=CSS;"
			+ "var host=document.head||document.documentElement;if(host)host.appendChild(s);}catch(e){}}"
			+ "document.addEventListener('DOMContentLoaded',apply);"
			+ "window.addEventListener('load',apply);apply();})();";
	}
}
