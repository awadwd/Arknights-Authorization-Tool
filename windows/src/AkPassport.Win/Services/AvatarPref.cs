using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AkPassport.Win.Services;

/// <summary>
/// 「干员头像个性化」设置的模型层。
///
/// 存的是和手机端 <c>utils/avatarPref.js</c> 完全一样的结构：
/// <c>localStorage.avatarPrefSettings</c> = JSON 字符串
/// <c>{ v:1, def:0|2, rules:[{id,level,cond:{...}}], white:[], black:[] }</c>。
///
/// 判定优先级（高 → 低）：黑名单(强制精一) &gt; 白名单(强制精二) &gt; 规则(首条命中) &gt; 默认档位。
/// 页面侧读到这份 JSON 后会自己 invalidate 缓存，所以这里只管写对格式。
/// </summary>
public sealed class AvatarPref
{
	public const int LevelE1 = 0;
	public const int LevelE2 = 2;

	[JsonPropertyName("v")] public int V { get; set; } = 1;
	[JsonPropertyName("def")] public int Def { get; set; }
	[JsonPropertyName("rules")] public List<AvatarRule> Rules { get; set; } = new();
	[JsonPropertyName("white")] public List<string> White { get; set; } = new();
	[JsonPropertyName("black")] public List<string> Black { get; set; } = new();

	public static AvatarPref CreateDefault() => new();

	/// <summary>清洗成手机端 normalizeSettings 认可的形状（脏数据不会让页面崩）。</summary>
	public AvatarPref Normalize()
	{
		var clean = new AvatarPref
		{
			V = 1,
			Def = Def == LevelE2 ? LevelE2 : LevelE1,
			White = Clean(White),
			Black = Clean(Black),
			Rules = new List<AvatarRule>(),
		};

		foreach (var rule in Rules ?? new List<AvatarRule>())
		{
			if (rule?.Cond is null) continue;
			var cond = rule.Cond.Normalize();
			if (!cond.HasAny()) continue;          // 空条件等价于默认档位，手机端也会丢掉
			clean.Rules.Add(new AvatarRule
			{
				Id = string.IsNullOrWhiteSpace(rule.Id) ? NewId() : rule.Id,
				Level = rule.Level == LevelE2 ? LevelE2 : LevelE1,
				Cond = cond,
			});
		}
		return clean;
	}

	public string ToJson()
	{
		var clean = Normalize();
		return JsonSerializer.Serialize(clean, JsonOptions);
	}

	public static AvatarPref FromJson(string? json)
	{
		if (string.IsNullOrWhiteSpace(json)) return CreateDefault();
		try
		{
			var parsed = JsonSerializer.Deserialize<AvatarPref>(json, JsonOptions);
			return parsed?.Normalize() ?? CreateDefault();
		}
		catch
		{
			return CreateDefault();
		}
	}

	public static string NewId() => "r" + Guid.NewGuid().ToString("N").Substring(0, 8);

	private static List<string> Clean(List<string>? src)
		=> (src ?? new List<string>())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x.Trim())
			.Distinct()
			.ToList();

	internal static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = false,
		Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
	};
}

public sealed class AvatarRule
{
	[JsonPropertyName("id")] public string Id { get; set; } = AvatarPref.NewId();
	[JsonPropertyName("level")] public int Level { get; set; }
	[JsonPropertyName("cond")] public AvatarCond Cond { get; set; } = new();

	public bool IsElite2 => Level == AvatarPref.LevelE2;

	public string LevelLabel => IsElite2 ? "精二" : "精一";

	/// <summary>如「职业：术师、狙击 · 稀有度：6★」。</summary>
	public string Summary => Cond.Summary();

	public AvatarRule Clone() => new()
	{
		Id = Id,
		Level = Level,
		Cond = Cond.Clone(),
	};
}

/// <summary>
/// 一条规则的条件。不同分组之间「且」，同组多选「或」，空分组不限。
/// 字段名必须和手机端一致（eng 小写 key）。
/// </summary>
public sealed class AvatarCond
{
	[JsonPropertyName("prof")] public List<string> Prof { get; set; } = new();
	[JsonPropertyName("sub")] public List<string> Sub { get; set; } = new();
	[JsonPropertyName("rar")] public List<string> Rar { get; set; } = new();
	[JsonPropertyName("pos")] public List<string> Pos { get; set; } = new();
	[JsonPropertyName("sex")] public List<string> Sex { get; set; } = new();
	[JsonPropertyName("obt")] public List<string> Obt { get; set; } = new();
	[JsonPropertyName("tag")] public List<string> Tag { get; set; } = new();
	[JsonPropertyName("boxType")] public List<string> BoxType { get; set; } = new();
	[JsonPropertyName("sale")] public List<string> Sale { get; set; } = new();
	[JsonPropertyName("rep")] public List<string> Rep { get; set; } = new();
	[JsonPropertyName("year")] public List<string> Year { get; set; } = new();

	public List<string> Get(string key) => key switch
	{
		"prof" => Prof,
		"sub" => Sub,
		"rar" => Rar,
		"pos" => Pos,
		"sex" => Sex,
		"obt" => Obt,
		"tag" => Tag,
		"boxType" => BoxType,
		"sale" => Sale,
		"rep" => Rep,
		"year" => Year,
		_ => new List<string>(),
	};

	public void Set(string key, List<string> values)
	{
		switch (key)
		{
			case "prof": Prof = values; break;
			case "sub": Sub = values; break;
			case "rar": Rar = values; break;
			case "pos": Pos = values; break;
			case "sex": Sex = values; break;
			case "obt": Obt = values; break;
			case "tag": Tag = values; break;
			case "boxType": BoxType = values; break;
			case "sale": Sale = values; break;
			case "rep": Rep = values; break;
			case "year": Year = values; break;
		}
	}

	public bool HasAny()
	{
		foreach (var (key, _) in CharRoster.Groups) if (Get(key).Count > 0) return true;
		return Year.Count > 0;
	}

	public AvatarCond Normalize()
	{
		var copy = new AvatarCond();
		foreach (var (key, _) in CharRoster.Groups) copy.Set(key, Clean(Get(key)));
		copy.Year = Clean(Year);
		return copy;
	}

	public AvatarCond Clone() => Normalize();

	public bool IsPicked(string key, string value) => Get(key).Contains(value);

	public void Toggle(string key, string value)
	{
		var list = new List<string>(Get(key));
		if (list.Contains(value)) list.Remove(value);
		else list.Add(value);
		Set(key, list);

		// 分支随职业联动：职业改动后剔除不再适用的分支（与手机端一致）
		if (key == "prof")
		{
			var allowed = CharRoster.OptionsOf("sub", Prof);
			Sub = Sub.Where(allowed.Contains).ToList();
		}
	}

	public void Clear(string key) => Set(key, new List<string>());

	/// <summary>「职业：术师、狙击 · 稀有度：6★」，空条件返回「不限」。</summary>
	public string Summary()
	{
		var parts = new List<string>();
		foreach (var (key, label) in CharRoster.Groups)
		{
			var values = Get(key);
			if (values.Count == 0) continue;
			parts.Add(label + "：" + string.Join("、", values.Select(v => CharRoster.LabelOf(key, v))));
		}
		if (Year.Count > 0) parts.Add("发布年份：" + string.Join("、", Year.Select(y => y + "年")));
		return parts.Count == 0 ? "不限" : string.Join(" · ", parts);
	}

	private static List<string> Clean(List<string>? src)
		=> (src ?? new List<string>())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Select(x => x.Trim())
			.Distinct()
			.ToList();
}
