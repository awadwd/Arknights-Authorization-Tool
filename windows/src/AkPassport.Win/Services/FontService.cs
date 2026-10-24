using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace AkPassport.Win.Services;

/// <summary>
/// 界面字体：Windows 不自带苹方，但只要用户装过（装到
/// %LOCALAPPDATA%\Microsoft\Windows\Fonts 的「用户字体」也算），就自动换上去，
/// 让原生外壳和别的 WinUI3 桌面程序一样是苹方观感。
///
/// ⚠️ 为什么不用 GDI 探测：用户字体不进 GDI 的字体映射表。
/// 实测 <c>CreateFontW("PingFang SC")</c> + <c>GetTextFace</c> 拿到的是「宋体」，
/// 但 DirectWrite / XAML 完全能用 —— 拿 GDI 判断会一律误判为「没装」。
/// 所以这里直接翻字体目录、解析 TTF/OTF 的 name 表读真实字族名。
/// </summary>
internal static class FontService
{
	/// <summary>候选字族，按偏好排序（简体优先）。</summary>
	private static readonly string[] Candidates = { "PingFang SC", "苹方", "PingFang TC", "PingFang HK" };

	/// <summary>已启用的字族（null = 用系统默认）。窗口创建后按它同步控件级字体。</summary>
	public static string? Current { get; private set; }

	/// <summary>应用启动时调用一次：检测 → 写入应用级资源。</summary>
	public static void Initialize()
	{
		var family = Detect();
		if (family is null)
		{
			Diag.Log("font: no PingFang installed, keep system default");
			return;
		}

		Current = family;
		Apply(family);
	}

	/// <summary>
	/// 把字体应用到整个外壳：应用级资源（对话框、模板里的 TextBlock 都吃这一层）
	/// ＋ 明确的控件级设置（导航栏、设置页）。
	/// </summary>
	public static void Apply(string family)
	{
		var font = new FontFamily(family);
		var res = Application.Current.Resources;

		// WinUI 里控件的默认字体都从这里取，ContentDialog / Flyout 这类独立视觉树也认它
		res["ContentControlThemeFontFamily"] = font;

		// 自绘文字（标题栏、设置页里的裸 TextBlock）走隐式样式；
		// 必须在任何窗口创建之前换上，之后新建的元素才会套用。
		var textStyle = new Style(typeof(TextBlock));
		textStyle.Setters.Add(new Setter(TextBlock.FontFamilyProperty, font));
		res[typeof(TextBlock)] = textStyle;

		Diag.Log("font: using " + family);
	}

	/// <summary>在用户字体目录 / 系统字体目录里找苹方，返回能直接喂给 XAML 的字族名。</summary>
	public static string? Detect()
	{
		try
		{
			foreach (var family in Candidates)
			{
				foreach (var file in FontFiles())
				{
					if (!MatchesFileName(file)) continue;
					var names = ReadFamilyNames(file);
					foreach (var name in names)
					{
						if (!SameFamily(name, family)) continue;
						Diag.Log($"font probe: {Path.GetFileName(file)} -> '{name}'");
						return family;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Diag.Log("font detect failed: " + ex.Message);
		}
		return null;
	}

	private static IEnumerable<string> FontFiles()
	{
		// 用户字体（右键「安装」默认落这里）、系统字体、Office 字体
		var dirs = new[]
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"Microsoft", "Windows", "Fonts"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				"Microsoft", "Windows", "Fonts"),
		};

		foreach (var dir in dirs)
		{
			string[] files;
			try
			{
				if (!Directory.Exists(dir)) continue;
				files = Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
			}
			catch
			{
				continue;
			}

			foreach (var file in files)
			{
				var ext = Path.GetExtension(file).ToLowerInvariant();
				if (ext is ".ttf" or ".otf" or ".ttc" or ".otc") yield return file;
			}
		}
	}

	/// <summary>文件名预筛，避免启动时解析几百个字体文件。</summary>
	private static bool MatchesFileName(string path)
	{
		var name = Path.GetFileNameWithoutExtension(path);
		return name.Contains("pingfang", StringComparison.OrdinalIgnoreCase)
		       || name.Contains("苹方", StringComparison.OrdinalIgnoreCase);
	}

	private static bool SameFamily(string a, string b)
	{
		var x = Squeeze(a);
		var y = Squeeze(b);
		if (x.Length == 0 || y.Length == 0) return false;
		return string.Equals(x, y, StringComparison.OrdinalIgnoreCase)
		       || x.StartsWith(y, StringComparison.OrdinalIgnoreCase);
	}

	private static string Squeeze(string s) => s.Replace(" ", "").Replace("\u3000", "").Trim();

	/// <summary>
	/// 读 sfnt 的 name 表，取 family 名（nameID 1 / 16）。
	/// 字体文件都是大端，BinaryReader 是小端，所以表头按字节读。
	/// </summary>
	private static List<string> ReadFamilyNames(string path)
	{
		var result = new List<string>();
		try
		{
			using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			long tableOffset = 0;
			if (fs.Length < 12) return result;
			var tag = ReadU32(fs);
			if (tag == 0x74746366)          // 'ttcf'：字体集合，取第一个字体
			{
				ReadU32(fs);                // version
				var count = ReadU32(fs);
				if (count == 0) return result;
				tableOffset = ReadU32(fs);
			}

			fs.Position = tableOffset;
			ReadU32(fs);                    // sfnt version
			var numTables = ReadU16(fs);
			fs.Position += 6;               // searchRange / entrySelector / rangeShift

			long nameOffset = -1;
			for (var i = 0; i < numTables; i++)
			{
				var tagBytes = ReadBytes(fs, 4);
				if (tagBytes.Length < 4) break;
				ReadU32(fs);                // checksum
				var offset = ReadU32(fs);
				var length = ReadU32(fs);
				if (tagBytes[0] == 'n' && tagBytes[1] == 'a' && tagBytes[2] == 'm' && tagBytes[3] == 'e')
				{
					nameOffset = offset;
					if (length == 0) return result;
					break;
				}
			}
			if (nameOffset < 0) return result;

			fs.Position = nameOffset;
			ReadU16(fs);                    // format
			var countRecords = ReadU16(fs);
			var stringOffset = ReadU16(fs);

			for (var i = 0; i < countRecords; i++)
			{
				var platformId = ReadU16(fs);
				ReadU16(fs);                // encodingId
				ReadU16(fs);                // languageId
				var nameId = ReadU16(fs);
				var length = ReadU16(fs);
				var offset = ReadU16(fs);

				if (nameId != 1 && nameId != 16) continue;
				if (length == 0 || length > 512) continue;

				var here = fs.Position;
				fs.Position = nameOffset + stringOffset + offset;
				var bytes = ReadBytes(fs, length);
				fs.Position = here;
				if (bytes.Length < length) continue;

				var text = platformId == 3
					? Encoding.BigEndianUnicode.GetString(bytes)
					: Encoding.Latin1.GetString(bytes);
				text = text.Replace("\0", "").Trim();
				if (text.Length > 0 && !result.Contains(text)) result.Add(text);
			}
		}
		catch (Exception ex)
		{
			Diag.Log($"font parse failed ({Path.GetFileName(path)}): {ex.Message}");
		}
		return result;
	}

	private static byte[] ReadBytes(FileStream fs, int count)
	{
		var buffer = new byte[count];
		var read = 0;
		while (read < count)
		{
			var n = fs.Read(buffer, read, count - read);
			if (n <= 0) break;
			read += n;
		}
		if (read == count) return buffer;
		var partial = new byte[read];
		Array.Copy(buffer, partial, read);
		return partial;
	}

	private static ushort ReadU16(FileStream fs)
		=> (ushort)((fs.ReadByte() << 8) | fs.ReadByte());

	private static uint ReadU32(FileStream fs)
		=> ((uint)fs.ReadByte() << 24) | ((uint)fs.ReadByte() << 16) | ((uint)fs.ReadByte() << 8) | (uint)fs.ReadByte();
}
