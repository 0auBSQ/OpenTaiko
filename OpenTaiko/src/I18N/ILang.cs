namespace OpenTaiko;

internal interface ILang {
	string GetString(int idx);
}

static internal class CLangManager {
	// Cheap factory-like design pattern

	public static (string, int) DefaultLanguage = ("ja", 0);
	public static CLang LangInstance { get; private set; } = new CLang(Langcodes.FirstOrDefault("ja"));
	public static void langAttach(string lang) {
		LangInstance = CLang.GetCLang(lang);
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

	// temporary garbage code
	public static string[] Langcodes {
		get {
			if (_langCodes == null)
				_langCodes = Directory.GetDirectories(Path.Combine(OpenTaiko.strEXEのあるフォルダ, "Lang"), "*", SearchOption.TopDirectoryOnly)
					.Select(result => Path.GetRelativePath(Path.Combine(OpenTaiko.strEXEのあるフォルダ, "Lang"), result))
					.ToArray();

			return _langCodes;
		}
	}
	public static string[] Languages {
		get {
			if (_languages == null)
				_languages = Langcodes.Select(result => CLang.GetLanguage(result)).ToArray();

			return _languages;
		}
	}

	public static CLocalizationData GetAllStringsAsLocalizationData(string key) {
		if (_cachedLocs.ContainsKey(key)) return _cachedLocs[key];

		CLocalizationData loc = new CLocalizationData();
		loc.SetString("default", "?");

		foreach (string lang in Langcodes) {
			CLang _inst = CLang.GetCLang(lang);

			loc.SetString(lang, _inst.GetString(key));
		}

		_cachedLocs[key] = loc;
		return loc;
	}

	public static CLocalizationData GetAllStringsAsLocalizationDataWithArgs(string key, string keySalt, params object?[] values) {
		if (_cachedLocs.ContainsKey(key + keySalt)) return _cachedLocs[key + keySalt];

		CLocalizationData loc = new CLocalizationData();
		loc.SetString("default", "?");

		foreach (string lang in Langcodes) {
			CLang _inst = CLang.GetCLang(lang);

			loc.SetString(lang, _inst.GetString(key, values));
		}

		_cachedLocs[key + keySalt] = loc;
		return loc;
	}

	private static string[] _langCodes;
	private static string[] _languages;

	private static Dictionary<string, CLocalizationData> _cachedLocs = new Dictionary<string, CLocalizationData>();
}
