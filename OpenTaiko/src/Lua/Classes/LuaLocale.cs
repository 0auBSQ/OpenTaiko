using System.Text.Json.Nodes;
using Newtonsoft.Json;
using NLua;

namespace OpenTaiko {
	public class LuaLangFunc {
		public string GetString(string key, params object?[] args) {
			return CLangManager.LangInstance.GetString(key, args);
		}

		public bool ChangeLanguage(string id) {
			if (CLangManager.Langcodes.Contains(id) && CLangManager.fetchLang() != id) {
				CLangManager.langAttach(id);
				return true;
			}
			return false;
		}

		public string[] GetLanguageIds() {
			return CLangManager.Langcodes;
		}
		public string[] GetLanguageNames() {
			return CLangManager.Languages;
		}
		public Dictionary<string, string> GetAvailableLanguages() {
			return CLangManager.LanguageDict;
		}

		// I18N

		private Dictionary<string, string> _ConvertToStringDict(Dictionary<string, object> dict) {
			return dict.ToDictionary(
				kvp => kvp.Key,
				kvp => kvp.Value?.ToString() ?? string.Empty
			);
		}

		public CLocalizationData AsLocalizationData(JsonNode obj) {
			string _str = System.Text.Json.JsonSerializer.Serialize(obj);
			return JsonConvert.DeserializeObject<CLocalizationData>(_str) ?? new CLocalizationData();
		}

		public CLocalizationData FromDict(Dictionary<string, object> dict) {
			var _loc = new CLocalizationData(_ConvertToStringDict(dict));
			return _loc;
		}

		public CLocalizationData FromString(string _str) {
			var _strdct = JsonConvert.DeserializeObject<Dictionary<string, string>>(_str) ?? null;
			if (_strdct != null) {
				return new CLocalizationData(_strdct);
			}
			return new CLocalizationData();
		}
	}
}
