using Newtonsoft.Json;

namespace OpenTaiko;

[Serializable]
public class CLocalizationData {
	// Public so System.Text.Json includes this field in the JSON song cache.
	[JsonProperty("strings")]
	[System.Text.Json.Serialization.JsonPropertyName("strings")]
	public Dictionary<string, string> Strings = new Dictionary<string, string>();

	public CLocalizationData() {
		Strings = new Dictionary<string, string>();
	}

	public CLocalizationData(Dictionary<string, string> strings) { Strings = strings; }

	public string[] GetAllStrings() {
		return Strings.Values.ToArray();
	}

	public string GetString(string defaultsDefault) {
		string _lang = CLangManager.fetchLang();
		if (Strings.ContainsKey(_lang))
			return Strings[_lang];
		else if (Strings.ContainsKey("default"))
			return Strings["default"];
		return defaultsDefault;
	}

	public void SetString(string langcode, string str) {
		Strings[langcode] = str;
	}
}
