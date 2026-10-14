namespace OpenTaiko;

internal interface ILang {
	string GetString(int idx);
}

static internal class CLangManager {
	// Cheap factory-like design pattern

	// Loads the languages once. The lock and the single dictionary assignment let the song
	// enumeration read localized strings from several threads.
	private static void EnsureLangs() {
		lock (_langsLock) {
			if (_langs.Count > 0) return;
			var langs = new Dictionary<string, CLang>();
			foreach (string path in OpenTaiko.GetMergedDirectories(Path.Combine(OpenTaiko.strEXEFolder, "Lang"), "*")) {
				// Key by the folder name (the language code). On iOS GetMergedDirectories returns paths under
				// the app bundle, not under strEXEFolder/Lang, so Path.GetRelativePath would not yield the code.
				string id = new DirectoryInfo(path).Name;
				langs.Add(id, CLang.GetCLang(id));
			}
			_langs = langs;
		}
	}
	public static CLang LangInstance {
		get {
			EnsureLangs();
			return _langs[_langId];
		}
	}
	public static void langAttach(string lang) {
		_langId = lang;
		CLuaScript.tReloadLanguage(lang);
		OpenTaiko.NamePlate?.RefreshSkin();   // the equipped titles follow the language
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
			EnsureLangs();
			return _langs.Values.Select(lang => new KeyValuePair<string, string>(lang.Id, lang.Language)).ToDictionary();
		}
	}

	public static CLocalizationData GetAllStringsAsLocalizationData(string key) {
		if (_cachedLocs.ContainsKey(key)) return _cachedLocs[key];

		CLocalizationData loc = new CLocalizationData();
		loc.SetString("default", "?");

		EnsureLangs();
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

		EnsureLangs();
		foreach (var lang in _langs) {
			loc.SetString(lang.Key, lang.Value.GetString(key, values));
		}

		_cachedLocs[key + keySalt] = loc;
		return loc;
	}

	private static readonly object _langsLock = new();
	private static Dictionary<string, CLang> _langs = [];
	private static string _langId = "en";

	private static Dictionary<string, CLocalizationData> _cachedLocs = new Dictionary<string, CLocalizationData>();
}
