namespace OpenTaiko;

internal class CLuaInfo {
	public int playerCount => OpenTaiko.ConfigIni.nPlayerCount;
	public string lang => OpenTaiko.ConfigIni.sLang;
	public bool simplemode => OpenTaiko.ConfigIni.SimpleMode;
	public bool p1IsBlue => OpenTaiko.P1IsBlue();
	// the game's version, four numbers such as "0.7.0.0" (the project's Version)
	public string gameVersion => OpenTaiko.VERSION;
	// the current skin's Name=, Version= and Creator= from its SkinConfig.ini ("Unknown" when missing)
	public string skinName => OpenTaiko.Skin.Skin_Name;
	public string skinVersion => OpenTaiko.Skin.Skin_Version;
	public string skinCreator => OpenTaiko.Skin.Skin_Creator;
	// Lazy, on-demand only (the periodic ping/connectivity check + the connection icon were removed). No
	// cost unless a script actually reads it; reports whether a usable network interface is up.
	public bool online => System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

	public string dir { get; init; }

	public CLuaInfo(string dir) {
		this.dir = dir;
	}
}
