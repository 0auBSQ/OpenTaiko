namespace TJAPlayer3 {
	internal class CLuaInfo {
		public int playerCount => TJAPlayer3.ConfigIni.nPlayerCount;
		public string lang => TJAPlayer3.ConfigIni.sLang;
		public bool simplemode => TJAPlayer3.ConfigIni.SimpleMode;
		public bool p1IsBlue => TJAPlayer3.P1IsBlue();
		public bool online => TJAPlayer3.app.bネットワークに接続中;

		public string dir { get; init; }

		public CLuaInfo(string dir) {
			this.dir = dir;
		}
	}
}