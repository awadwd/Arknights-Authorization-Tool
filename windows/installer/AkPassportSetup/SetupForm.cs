using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

	public SetupForm()
	{
		var ver = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

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
			ForeColor = Color.FromArgb(0x60, 0x60, 0x60),
		};
		Controls.Add(title);
		Controls.Add(sub);

		// ---- 安装目录 ----
		var pathLabel = new Label { Text = "安装位置：", Location = new Point(26, 88), AutoSize = true };
		_pathBox = new TextBox
		{
			Location = new Point(26, 108),
			Size = new Size(390, 26),
			ReadOnly = true,
			BackColor = SystemColors.Window,
		};
		var browseBtn = new Button { Text = "浏览…", Location = new Point(424, 106), Size = new Size(80, 30) };
		browseBtn.Click += OnBrowse;
		_spaceLabel = new Label
		{
			Location = new Point(26, 138),
			AutoSize = true,
			ForeColor = Color.FromArgb(0x60, 0x60, 0x60),
		};
		Controls.Add(pathLabel);
		Controls.Add(_pathBox);
		Controls.Add(browseBtn);
		Controls.Add(_spaceLabel);

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
			ForeColor = Color.FromArgb(0x60, 0x60, 0x60),
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
			_status.ForeColor = Color.FromArgb(0xC0, 0x39, 0x2B);
		}
	}

	private void RefreshSpace()
	{
		var bytes = Program.PayloadBytes();
		_spaceLabel.Text = bytes > 0
			? $"安装包 {bytes / 1048576.0:F0} MB，安装后约 221 MB"
			: "需要约 250 MB 磁盘空间";
	}

	private void OnBrowse(object? sender, EventArgs e)
	{
		using var dlg = new FolderBrowserDialog
		{
			Description = "选择安装位置",
			SelectedPath = _pathBox.Text,
			ShowNewFolderButton = true,
		};
		if (dlg.ShowDialog(this) == DialogResult.OK)
			_pathBox.Text = Path.Combine(dlg.SelectedPath, Program.AppName);
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

		var dir = _pathBox.Text.Trim();
		if (string.IsNullOrWhiteSpace(dir))
		{
			MessageBox.Show(this, "请选择安装位置。", "安装", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		_busy = true;
		_installBtn.Enabled = false;
		_uninstallBtn.Enabled = false;
		_closeBtn.Enabled = false;
		_bar.Visible = true;
		_bar.Value = 0;
		_status.ForeColor = SystemColors.ControlText;

		var wantDesktop = _chkDesktop.Checked;
		var wantStartMenu = _chkStartMenu.Checked;
		var wantStartup = _chkStartup.Checked;
		var wantLaunch = _chkLaunch.Checked;

		try
		{
			_status.Text = "正在准备…";
			await Task.Run(() => Program.KillRunningApp());
			_bar.Value = 3;

			_status.Text = "正在写入文件…";
			var progress = new Progress<int>(v => _bar.Value = Math.Min(97, 3 + v * 94 / 100));
			int files = 0;
			await Task.Run(() =>
			{
				Program.ClearDirectory(dir, Program.UninstallerName);
				Program.Extract(dir, progress, out files);
			});

			_bar.Value = 97;
			_status.Text = "正在创建快捷方式…";
			await Task.Run(() =>
			{
				var exe = Path.Combine(dir, Program.ExeName);
				if (wantDesktop) Program.CreateShortcut(Path.Combine(Program.DesktopPath(), Program.AppName + ".lnk"), exe, dir, Program.AppName);
				if (wantStartMenu) Program.CreateShortcut(Path.Combine(Program.StartMenuPath(), Program.AppName + ".lnk"), exe, dir, Program.AppName);
			});

			_bar.Value = 98;
			_status.Text = "正在写入卸载信息…";
			await Task.Run(() =>
			{
				var self = Environment.ProcessPath;
				if (!string.IsNullOrEmpty(self))
				{
					try
					{
						File.Copy(self, Path.Combine(dir, Program.UninstallerName), overwrite: true);
					}
					catch
					{
					}
				}
				Program.RegisterUninstall(dir);
				Program.SetStartup(wantStartup, dir);
			});

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

			if (wantLaunch)
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
		catch (Exception ex)
		{
			_status.Text = "安装失败：" + ex.Message;
			_status.ForeColor = Color.FromArgb(0xC0, 0x39, 0x2B);
			MessageBox.Show(this, "安装失败：\n" + ex.Message, Program.AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			_installBtn.Enabled = true;
			_uninstallBtn.Enabled = _installedDir is not null;
			_closeBtn.Enabled = true;
			_busy = false;
		}
	}
}
