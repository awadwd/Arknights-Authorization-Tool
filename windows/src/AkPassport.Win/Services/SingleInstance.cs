using System;
using System.Threading;

namespace AkPassport.Win.Services;

/// <summary>
/// 单实例：第二个进程不会开新窗口，而是通知第一个进程把自己唤到前台。
/// </summary>
public sealed class SingleInstance : IDisposable
{
	private const string MutexName = @"Local\AkPassport.Win.SingleInstance";
	private const string EventName = @"Local\AkPassport.Win.Activate";

	private readonly Mutex _mutex;
	private readonly EventWaitHandle _activateEvent;
	private readonly Thread? _listener;
	private volatile bool _running = true;

	public bool IsFirstInstance { get; }

	public SingleInstance(Action onActivate)
	{
		_mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
		IsFirstInstance = createdNew;
		_activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);

		if (!IsFirstInstance)
		{
			// 已有实例在跑：敲一下门然后退出
			_activateEvent.Set();
			return;
		}

		_listener = new Thread(() =>
		{
			while (_running)
			{
				if (_activateEvent.WaitOne(TimeSpan.FromMilliseconds(500)))
				{
					onActivate();
				}
			}
		})
		{
			IsBackground = true,
			Name = "single-instance-listener",
		};
		_listener.Start();
	}

	public void Dispose()
	{
		_running = false;
		try { _activateEvent.Set(); } catch { }
		try { _mutex.ReleaseMutex(); } catch { }
		_mutex.Dispose();
		_activateEvent.Dispose();
	}
}
