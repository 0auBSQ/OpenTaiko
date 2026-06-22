namespace OpenTaiko;

internal class CLuaInfo {
	public int playerCount => OpenTaiko.ConfigIni.nPlayerCount;
	public string lang => OpenTaiko.ConfigIni.sLang;
	public bool simplemode => OpenTaiko.ConfigIni.SimpleMode;
	public bool p1IsBlue => OpenTaiko.P1IsBlue();
	// Lazy, on-demand only (the periodic ping/connectivity check + the connection icon were removed). No
	// cost unless a script actually reads it; reports whether a usable network interface is up.
	public bool online => System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();

	public string dir { get; init; }

	public CLuaInfo(string dir) {
		this.dir = dir;
	}
}
