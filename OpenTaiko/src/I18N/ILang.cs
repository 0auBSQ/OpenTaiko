namespace OpenTaiko;

internal interface ILang {
	string GetString(int idx);
}

static internal class CLangManager {
	// Cheap factory-like design pattern

	private static void InitializeLangs() {
		foreach (string path in Directory.GetDirectories(Path.Combine(OpenTaiko.strEXEのあるフォルダ, "Lang"), "*", SearchOption.TopDirectoryOnly)) {
			string id = Path.GetRelativePath(Path.Combine(OpenTaiko.strEXEのあるフォルダ, "Lang"), path);
			_langs.Add(id, CLang.GetCLang(id));
		}
	}
	public static CLang LangInstance {
		get {
			if (_langs.Count == 0) InitializeLangs();
			return _langs[_langId];
		}
	}
	public static void langAttach(string lang) {
		_langId = lang;
		CLuaScript.tReloadLanguage(lang);
	}

	public static int langToInt(string lang) {
		return Array.IndexOf(Langcodes, lang);
	}

	public static string fetchLang() {
		return LangInstance.Id;
	}

	public static string intToLang(int idx) {
		return Langcodes[idx];
	}

	public static string[] Langcodes {
		get {
			return LanguageDict.Keys.ToArray();
		}
	}
	public static string[] Languages {
		get {
			return LanguageDict.Values.ToArray();
		}
	}
	public static Dictionary<string, string> LanguageDict {
		get {
			if (_langs.Count == 0) InitializeLangs();
			return _langs.Values.Select(lang => new KeyValuePair<string, string>(lang.Id, lang.Language)).ToDictionary();
		}
	}

	public static CLocalizationData GetAllStringsAsLocalizationData(string key) {
		if (_cachedLocs.ContainsKey(key)) return _cachedLocs[key];

		CLocalizationData loc = new CLocalizationData();
		loc.SetString("default", "?");

		if (_langs.Count == 0) InitializeLangs();
		foreach (var lang in _langs) {
			loc.SetString(lang.Key, lang.Value.GetString(key));
		}

		_cachedLocs[key] = loc;
		return loc;
	}

	public static CLocalizationData GetAllStringsAsLocalizationDataWithArgs(string key, string keySalt, params object?[] values) {
		if (_cachedLocs.ContainsKey(key + keySalt)) return _cachedLocs[key + keySalt];

		CLocalizationData loc = new CLocalizationData();
		loc.SetString("default", "?");

		if (_langs.Count == 0) InitializeLangs();
		foreach (var lang in _langs) {
			loc.SetString(lang.Key, lang.Value.GetString(key, values));
		}

		_cachedLocs[key + keySalt] = loc;
		return loc;
	}

	private static Dictionary<string, CLang> _langs = [];
	private static string _langId = "en";

	private static Dictionary<string, CLocalizationData> _cachedLocs = new Dictionary<string, CLocalizationData>();
}
