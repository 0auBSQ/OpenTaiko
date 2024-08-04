namespace OpenTaiko {
	internal class CLuaInfo {
		public int playerCount => OpenTaiko.ConfigIni.nPlayerCount;
		public string lang => OpenTaiko.ConfigIni.sLang;
		public bool simplemode => OpenTaiko.ConfigIni.SimpleMode;
		public bool p1IsBlue => OpenTaiko.P1IsBlue();
		public bool online => OpenTaiko.app.bネットワークに接続中;

		public string dir { get; init; }

		public CLuaInfo(string dir) {
			this.dir = dir;
		}
	}
}