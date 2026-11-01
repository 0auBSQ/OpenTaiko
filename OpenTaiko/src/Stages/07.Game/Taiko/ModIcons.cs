namespace OpenTaiko;

class ModIcons {
	#region [Mod flags]

	static public bool tPlayIsStock(int player) {
		int actual = player;

		if (OpenTaiko.ConfigIni.nFunMods[actual] != EFunMods.None) return false;
		if (OpenTaiko.ConfigIni.bJust[actual] != 0) return false;
		if (OpenTaiko.ConfigIni.nTimingZones[actual] != 2) return false;
		if (OpenTaiko.ConfigIni.nSongSpeed != 20) return false;
		if (OpenTaiko.ConfigIni.eRandom[actual] != ERandomMode.Off) return false;
		if (OpenTaiko.ConfigIni.eSTEALTH[actual] != EStealthMode.Off) return false;
		if (OpenTaiko.ConfigIni.nScrollSpeed[actual] != 9) return false;

		return true;
	}

	static public Int64 tModsToPlayModsFlags(int player) {
		byte[] _flags = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
		int actual = player;

		_flags[0] = (byte)Math.Min(255, OpenTaiko.ConfigIni.nScrollSpeed[actual]);
		_flags[1] = (byte)OpenTaiko.ConfigIni.eSTEALTH[actual];
		_flags[2] = (byte)OpenTaiko.ConfigIni.eRandom[actual];
		_flags[3] = (byte)Math.Min(255, OpenTaiko.ConfigIni.nSongSpeed);
		_flags[4] = (byte)OpenTaiko.ConfigIni.nTimingZones[actual];
		_flags[5] = (byte)OpenTaiko.ConfigIni.bJust[actual];
		_flags[7] = (byte)OpenTaiko.ConfigIni.nFunMods[actual];

		return BitConverter.ToInt64(_flags, 0);
	}

	#endregion
}
