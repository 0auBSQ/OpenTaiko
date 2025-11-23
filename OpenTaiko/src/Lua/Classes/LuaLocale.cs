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
	}
}
