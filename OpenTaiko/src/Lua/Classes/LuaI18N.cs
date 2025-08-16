namespace OpenTaiko {
	public class LuaI18NFunc {

		public string GetInternalTranslatedString(string key) {
			return CLangManager.LangInstance.GetString(key);
		}

		// If necessary add functions handling CLocalizationData here
	}
}
