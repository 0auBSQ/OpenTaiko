namespace OpenTaiko {
	internal class CMainMenuSettings : CSavableT<List<CMainMenuSettings.MenuSettings>> {

		public CMainMenuSettings() {

		}

		public void tReloadMenus() {
			_fn = CSkin.Path($"Config/MainMenuSettings.json");
			base.tDBInitSavable();
		}

		public class MenuSettings {
			public CLocalizationData StageName;
			public CLocalizationData StageDescription;
			public string StageType;
			public string LuaStageName;
			public bool Restricted1P;
			public string LEGACY_MenuBoxColor;
		}
	}
}
