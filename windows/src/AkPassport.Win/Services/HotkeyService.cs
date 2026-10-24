using System;
using System.Runtime.InteropServices;

namespace AkPassport.Win.Services;

/// <summary>
/// 全局快捷键。WinUI 3 没有 WndProc，所以给窗口挂一个 subclass 来收 WM_HOTKEY。
/// </summary>
public sealed class HotkeyService : IDisposable
{
	private const int WM_HOTKEY = 0x0312;
	private const int HotkeyId = 0xA11;
	private const uint MOD_ALT = 0x0001;
	private const uint MOD_CONTROL = 0x0002;
	private const uint MOD_NOREPEAT = 0x4000;

	private readonly IntPtr _hwnd;
	private readonly Action _action;
	private readonly SUBCLASSPROC _proc;   // 委托必须持有，否则被 GC 后回调会踩空
	private bool _registered;

	public bool IsRegistered => _registered;

	/// <summary>默认 Ctrl + Alt + A。</summary>
	public HotkeyService(IntPtr hwnd, Action action)
	{
		_hwnd = hwnd;
		_action = action;
		_proc = WndProc;

		_registered = RegisterHotKey(_hwnd, HotkeyId, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, 0x41);
		if (_registered)
		{
			SetWindowSubclass(_hwnd, _proc, (UIntPtr)HotkeyId, UIntPtr.Zero);
		}
	}

	private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, UIntPtr id, UIntPtr data)
	{
		if (msg == WM_HOTKEY && wParam.ToInt64() == HotkeyId)
		{
			_action();
			return IntPtr.Zero;
		}
		return DefSubclassProc(hWnd, msg, wParam, lParam);
	}

	public void Dispose()
	{
		if (!_registered) return;
		UnregisterHotKey(_hwnd, HotkeyId);
		RemoveWindowSubclass(_hwnd, _proc, (UIntPtr)HotkeyId);
		_registered = false;
	}

	private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, UIntPtr dwRefData);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

	[DllImport("comctl32.dll", SetLastError = true)]
	private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, UIntPtr uIdSubclass, UIntPtr dwRefData);

	[DllImport("comctl32.dll", SetLastError = true)]
	private static extern bool RemoveWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, UIntPtr uIdSubclass);

	[DllImport("comctl32.dll")]
	private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
}
