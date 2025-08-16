using System.Text.Json.Nodes;
using Newtonsoft.Json;

namespace OpenTaiko {
	public class LuaI18NFunc {

		public string GetInternalTranslatedString(string key) {
			return CLangManager.LangInstance.GetString(key);
		}

		public CLocalizationData AsLocalizationData(JsonNode obj) {
			string _str = System.Text.Json.JsonSerializer.Serialize(obj);
			return JsonConvert.DeserializeObject<CLocalizationData>(_str) ?? new CLocalizationData();
		}
	}
}
