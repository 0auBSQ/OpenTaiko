using System.Text.Json.Nodes;
using Newtonsoft.Json;

namespace OpenTaiko {
	public class LuaI18NFunc {

		private Dictionary<string, string> _ConvertToStringDict(Dictionary<string, object> dict) {
			return dict.ToDictionary(
				kvp => kvp.Key,
				kvp => kvp.Value?.ToString() ?? string.Empty
			);
		}

		public string GetInternalTranslatedString(string key) {
			return CLangManager.LangInstance.GetString(key);
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
