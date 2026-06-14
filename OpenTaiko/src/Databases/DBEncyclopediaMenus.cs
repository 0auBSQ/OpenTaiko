using Newtonsoft.Json;

namespace OpenTaiko;

class DBEncyclopediaMenus : CSavableT<DBEncyclopediaMenus.EncyclopediaMenu> {
	public DBEncyclopediaMenus() {
		_fn = @$"{OpenTaiko.strEXEFolder}Encyclopedia{Path.DirectorySeparatorChar}Menus.json";
		base.tDBInitSavable();
	}

	#region [Auxiliary classes]
	public class EncyclopediaMenu {
		[JsonProperty("menus")]
		public KeyValuePair<int, EncyclopediaMenu>[] Menus;

		[JsonProperty("pages")]
		public int[] Pages;
	}

	#endregion
}
