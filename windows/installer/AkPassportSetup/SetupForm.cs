using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AkPassport.Setup;

/// <summary>安装向导窗口。就一页：选目录 → 勾选项 → 安装。</summary>
internal sealed class SetupForm : Form
{
	private readonly TextBox _pathBox;
	private readonly CheckBox _chkDesktop;
	private readonly CheckBox _chkStartMenu;
	private readonly CheckBox _chkStartup;
	private readonly CheckBox _chkLaunch;
	private readonly Button _installBtn;
	private readonly Button _uninstallBtn;
	private readonly Button _closeBtn;
	private readonly ProgressBar _bar;
	private readonly Label _status;
	private readonly Label _spaceLabel;

	private bool _busy;
	private readonly string? _installedDir;

	private static readonly Color HintColor = Color.FromArgb(0x60, 0x60, 0x60);
	private static readonly Color ErrorColor = Color.FromArgb(0xC0, 0x39, 0x2B);

	/// <summary>Windows 文件名里不允许出现的字符（Path.GetInvalidPathChars 在 .NET Core 上几乎是空的，靠不住）。</summary>
	private static readonly char[] BadChars = { '"', '<', '>', '|', '\0', '\t', '\r', '\n' };

	public SetupForm()
	{
		var ver = Program.FullVersion;   // 含 beta 段

		Text = $"{Program.AppName} {ver} 安装";
		StartPosition = FormStartPosition.CenterScreen;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		AutoScaleMode = AutoScaleMode.Dpi;
		Font = new Font("Microsoft YaHei UI", 9f);
		ClientSize = new Size(560, 430);
		Icon = LoadOwnIcon();

		// ---- 标题区 ----
		var title = new Label
		{
			Text = Program.AppName,
			Font = new Font("Microsoft YaHei UI", 14f, FontStyle.Bold),
			Location = new Point(24, 20),
			AutoSize = true,
		};
		var sub = new Label
		{
			Text = $"版本 {ver} · Windows 桌面版（WebView2 内核）",
			Location = new Point(26, 50),
			AutoSize = true,
			ForeColor = HintColor,
		};
		Controls.Add(title);
		Controls.Add(sub);

		// ---- 安装目录 ----
		// ★ 以前这里是 ReadOnly，用户没法手动输入/粘贴路径，只能一层层点「浏览」。
		//   现在可编辑，并且接受粘贴「复制为路径」带引号的形式、拖文件夹进来、%ENV% 变量。
		var pathLabel = new Label
		{
			Text = "安装位置（可直接输入或粘贴路径）：",
			Location = new Point(26, 88),
			AutoSize = true,
		};
		_pathBox = new TextBox
		{
			Location = new Point(26, 108),
			Size = new Size(390, 26),
			ReadOnly = false,
			AllowDrop = true,
		};
		var browseBtn = new Button { Text = "浏览…", Location = new Point(424, 106), Size = new Size(80, 30) };
		browseBtn.Click += OnBrowse;
		_spaceLabel = new Label
		{
			Location = new Point(26, 138),
			Size = new Size(508, 20),
			AutoSize = false,
			AutoEllipsis = true,
			ForeColor = HintColor,
		};
		Controls.Add(pathLabel);
		Controls.Add(_pathBox);
		Controls.Add(browseBtn);
		Controls.Add(_spaceLabel);

		_pathBox.TextChanged += (_, _) => RefreshSpace();
		_pathBox.Leave += (_, _) => _pathBox.Text = NormalizePath(_pathBox.Text);
		_pathBox.DragEnter += OnPathDragEnter;
		_pathBox.DragDrop += OnPathDrop;

		// ---- 选项 ----
		var optLabel = new Label { Text = "安装选项：", Location = new Point(26, 168), AutoSize = true };
		_chkDesktop = NewCheck("创建桌面快捷方式", 190, true);
		_chkStartMenu = NewCheck("创建「开始」菜单快捷方式", 216, true);
		_chkStartup = NewCheck("开机自动启动（静默驻留托盘）", 242, false);
		_chkLaunch = NewCheck("安装完成后立即运行", 268, true);
		Controls.Add(optLabel);
		Controls.Add(_chkDesktop);
		Controls.Add(_chkStartMenu);
		Controls.Add(_chkStartup);
		Controls.Add(_chkLaunch);

		// ---- 提示 ----
		// 单行：原来写了两行、宽度又不够，第一行折行后把第二行顶出可视区，
		// 界面上只剩半句「保存在 %LOCALAPPDATA%\\AkPassport」加一个孤零零的逗号。
		var tip = new Label
		{
			Text = "数据存在 %LOCALAPPDATA%\\AkPassport，覆盖安装不会丢。",
			Location = new Point(26, 302),
			Size = new Size(508, 22),
			ForeColor = HintColor,
		};
		Controls.Add(tip);

		// ---- 进度 ----
		_bar = new ProgressBar { Location = new Point(26, 348), Size = new Size(508, 18), Visible = false };
		_status = new Label { Location = new Point(26, 372), Size = new Size(508, 20), Text = "" };
		Controls.Add(_bar);
		Controls.Add(_status);

		// ---- 按钮 ----
		_installBtn = new Button { Text = "安装", Location = new Point(300, 398), Size = new Size(110, 34) };
		_installBtn.Click += async (_, _) => await OnInstallAsync();
		_uninstallBtn = new Button { Text = "卸载", Location = new Point(180, 398), Size = new Size(110, 34), Enabled = false };
		_uninstallBtn.Click += OnUninstallClick;
		_closeBtn = new Button { Text = "取消", Location = new Point(424, 398), Size = new Size(110, 34) };
		_closeBtn.Click += (_, _) => Close();

		AcceptButton = _installBtn;
		CancelButton = _closeBtn;
		Controls.Add(_installBtn);
		Controls.Add(_uninstallBtn);
		Controls.Add(_closeBtn);

		// ---- 初始化 ----
		_installedDir = Program.GetInstalledDir();
		_pathBox.Text = _installedDir ?? Program.DefaultInstallDir;
		_pathBox.SelectionStart = 0;   // 否则默认滚到末尾，左边看不全

		if (_installedDir is not null)
		{
			_installBtn.Text = "覆盖安装";
			_uninstallBtn.Enabled = true;
			_chkStartup.Checked = Program.IsStartupEnabled();
			_status.Text = "检测到已安装，覆盖安装会保留个人数据。";
		}

		RefreshSpace();
		CheckPayload();
	}

	private CheckBox NewCheck(string text, int y, bool checked_)
	{
		var c = new CheckBox
		{
			Text = text,
			Location = new Point(30, y),
			AutoSize = true,
			Checked = checked_,
		};
		return c;
	}

	private Icon? LoadOwnIcon()
	{
		try
		{
			var exe = Environment.ProcessPath;
			if (!string.IsNullOrEmpty(exe)) return Icon.ExtractAssociatedIcon(exe);
		}
		catch
		{
		}
		return null;
	}

	private void CheckPayload()
	{
		using var payload = Program.OpenPayload();
		if (payload is null)
		{
			_installBtn.Enabled = false;
			_status.Text = "安装包不完整：找不到内嵌的 payload.zip。";
			_status.ForeColor = ErrorColor;
		}
	}

	// ---------------------------------------------------------------- 路径处理

	/// <summary>去掉首尾引号/空白、展开 %ENV%、转绝对路径、去掉尾部反斜杠（盘符根除外）。</summary>
	internal static string NormalizePath(string? raw)
	{
		var s = (raw ?? "").Trim();

		// 右键「复制为路径」给的是带引号的
		if (s.Length >= 2 && ((s[0] == '"' && s[^1] == '"') || (s[0] == '\'' && s[^1] == '\'')))
			s = s[1..^1].Trim();

		if (s.Length == 0) return s;

		try
		{
			s = Environment.ExpandEnvironmentVariables(s);
			s = Path.GetFullPath(s);
		}
		catch
		{
			return s;   // 非法路径交给 Validate 报错，别在这里吞掉
		}

		var root = Path.GetPathRoot(s);
		if (!string.IsNullOrEmpty(root) && s.Length > root.Length)
			s = s.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

		return s;
	}

	/// <summary>返回错误文案；合法返回 null。</summary>
	internal static string? ValidatePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path)) return "请填写安装位置。";
		if (path.IndexOfAny(BadChars) >= 0) return "路径里有非法字符（\" < > | 等）。";

		// 每一段都不能是 Windows 保留名
		var reserved = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "LPT1", "LPT2" };
		foreach (var seg in path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
		{
			var name = seg.TrimEnd('.');
			if (reserved.Any(r => string.Equals(r, name, StringComparison.OrdinalIgnoreCase)))
				return $"「{seg}」是 Windows 保留名称，换个文件夹。";
		}

		if (File.Exists(path) && !Directory.Exists(path)) return "这个位置是一个文件，请换一个文件夹。";

		var root = Path.GetPathRoot(path);
		if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return $"盘符 {root} 不存在。";

		try
		{
			if (!Path.IsPathFullyQualified(path)) return "请填写完整路径（例如 D:\\Games\\App）。";
		}
		catch
		{
			return "路径格式不对。";
		}

		return null;
	}

	private void RefreshSpace()
	{
		var packed = Program.PayloadBytes();
		var unpacked = Program.PayloadUncompressedBytes();

		var size = unpacked > 0
			? $"安装包 {packed / 1048576.0:F0} MB · 安装后约 {unpacked / 1048576.0:F0} MB"
			: $"安装包 {packed / 1048576.0:F0} MB";

		var dir = NormalizePath(_pathBox.Text);
		var err = ValidatePath(dir);

		if (err is not null)
		{
			_spaceLabel.ForeColor = ErrorColor;
			_spaceLabel.Text = err;
			return;
		}

		var disk = "";
		try
		{
			var probe = Program.NearestExisting(dir);
			if (probe is not null)
			{
				var di = new DriveInfo(Path.GetPathRoot(probe)!);
				if (di.IsReady)
				{
					var free = di.AvailableFreeSpace;
					disk = $" · {di.Name.TrimEnd('\\')} 剩余 {free / 1073741824.0:F1} GB";

					if (unpacked > 0 && free < unpacked + 32L * 1024 * 1024)
					{
						_spaceLabel.ForeColor = ErrorColor;
						_spaceLabel.Text = $"磁盘空间不足：需要约 {unpacked / 1048576} MB，只剩 {free / 1048576} MB";
									return;
					}
				}
			}
		}
		catch
		{
		}

		_spaceLabel.ForeColor = HintColor;
		_spaceLabel.Text = size + disk;
	}

	private void OnPathDragEnter(object? sender, DragEventArgs e)
	{
		if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
			e.Effect = DragDropEffects.Copy;
	}

	private void OnPathDrop(object? sender, DragEventArgs e)
	{
		if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths)
			_pathBox.Text = paths[0];
	}

	private void OnBrowse(object? sender, EventArgs e)
	{
		using var dlg = new FolderBrowserDialog
		{
			Description = "选择安装位置",
			SelectedPath = NormalizePath(_pathBox.Text),
			ShowNewFolderButton = true,
		};
		if (dlg.ShowDialog(this) != DialogResult.OK) return;

		// 用户直接选中了本程序的目录就不再套一层，否则会变成 …\App\App
		var picked = dlg.SelectedPath;
		var name = Path.GetFileName(picked.TrimEnd(Path.DirectorySeparatorChar));
		_pathBox.Text = string.Equals(name, Program.AppName, StringComparison.OrdinalIgnoreCase)
			? picked
			: Path.Combine(picked, Program.AppName);
	}

	private void OnUninstallClick(object? sender, EventArgs e)
	{
		// 交给带 /uninstall 的自己去做，弹窗和清理由那边统一处理
		var self = Environment.ProcessPath;
		if (string.IsNullOrEmpty(self)) return;
		try
		{
			Process.Start(new ProcessStartInfo(self) { Arguments = "/uninstall", UseShellExecute = true });
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, "打不开卸载器：" + ex.Message, "卸载", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private async Task OnInstallAsync()
	{
		if (_busy) return;

		// 先归一化再校验：用户可能粘了带引号的路径、或者末尾带反斜杠
		var dir = NormalizePath(_pathBox.Text);
		_pathBox.Text = dir;

		var bad = ValidatePath(dir);
		if (bad is not null)
		{
			MessageBox.Show(this, bad, "安装", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			_pathBox.Focus();
			_pathBox.SelectAll();
			return;
		}

		_busy = true;
		_installBtn.Enabled = false;
		_uninstallBtn.Enabled = false;
		_closeBtn.Enabled = false;
		_bar.Visible = true;
		_bar.Value = 0;
		_status.ForeColor = SystemColors.ControlText;

		var io = new Program.InstallOptions
		{
			Desktop = _chkDesktop.Checked,
			StartMenu = _chkStartMenu.Checked,
			Startup = _chkStartup.Checked,
			Launch = _chkLaunch.Checked,
		};

		// 进度和阶段文案都由 Program.Install 上报，GUI 只是显示器。
		// Progress<T> 在这里（UI 线程）创建，回调会自动回到 UI 线程。
		var progress = new Progress<int>(v => _bar.Value = Math.Clamp(v, 0, 100));
		var phase = new Progress<string>(s => _status.Text = s);

		int files = 0;
		string error = "";
		SetupExit rc = SetupExit.Failed;

		try
		{
			await Task.Run(() => { rc = Program.Install(dir, io, progress, phase, out files, out error); });
		}
		catch (Exception ex)
		{
			rc = SetupExit.Failed;
			error = ex.Message;
		}

		if (rc != SetupExit.Ok)
		{
			_bar.Value = 0;
			_status.Text = "安装失败：" + error;
			_status.ForeColor = ErrorColor;
			MessageBox.Show(this,
				"安装失败：\n" + error + "\n\n详细记录：" + SetupLog.FilePath,
				Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			_installBtn.Enabled = true;
			_uninstallBtn.Enabled = _installedDir is not null;
			_closeBtn.Enabled = true;
			_busy = false;
			return;
		}

		_bar.Value = 100;
		_status.Text = $"安装完成（{files} 个文件）";

		if (!Program.HasWebView2())
		{
			MessageBox.Show(this,
				"没有检测到 WebView2 运行时。\n\n" +
				"Win11 一般自带；Win10 需要先装 Evergreen Runtime，否则打开是白屏。\n" +
				"下载地址：https://developer.microsoft.com/microsoft-edge/webview2/",
				"提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		if (io.Launch)
		{
			try
			{
				Process.Start(new ProcessStartInfo(Path.Combine(dir, Program.ExeName))
				{
					UseShellExecute = true,
					WorkingDirectory = dir,
				});
			}
			catch
			{
			}
			Close();
			return;
		}

		MessageBox.Show(this, "安装完成。", Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
		Close();
	}
}
