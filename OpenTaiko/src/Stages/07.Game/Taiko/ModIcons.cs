namespace OpenTaiko {
	class ModIcons {
		static Dictionary<int, Action<int, int, int, int>> __methods = new Dictionary<int, Action<int, int, int, int>>()
		{
			{0, (x, y, a, p) => tDisplayHSIcon(x, y, a) },
			{1, (x, y, a, p) => tDisplayDoronIcon(x, y, a) },
			{2, (x, y, a, p) => tDisplayRandomIcon(x, y, a) },
			{3, (x, y, a, p) => tDisplayFunModIcon(x, y, a) },
			{4, (x, y, a, p) => tDisplayJustIcon(x, y, a) },
			{5, (x, y, a, p) => tDisplayTimingIcon(x, y, a) },
			{6, (x, y, a, p) => tDisplaySongSpeedIcon(x, y, p) },
			{7, (x, y, a, p) => tDisplayAutoIcon(x, y, p) },
		};

		static public void tDisplayMods(int x, int y, int player) {
			// +30 x/y
			int actual = OpenTaiko.GetActualPlayer(player);

			for (int i = 0; i < 8; i++) {
				__methods[i](x + OpenTaiko.Skin.ModIcons_OffsetX[i], y + OpenTaiko.Skin.ModIcons_OffsetY[i], actual, player);
			}
		}

		static public void tDisplayModsMenu(int x, int y, int player) {
			if (OpenTaiko.Tx.Mod_None != null)
				OpenTaiko.Tx.Mod_None.Opacity = 0;

			int actual = OpenTaiko.GetActualPlayer(player);

			for (int i = 0; i < 8; i++) {
				__methods[i](x + OpenTaiko.Skin.ModIcons_OffsetX_Menu[i], y + OpenTaiko.Skin.ModIcons_OffsetY_Menu[i], actual, player);
			}

			if (OpenTaiko.Tx.Mod_None != null)
				OpenTaiko.Tx.Mod_None.Opacity = 255;
		}

		#region [Displayables]

		static private void tDisplayHSIcon(int x, int y, int player) {
			// TO DO : Add HS x0.5 icon (_vals == 4)
			var _vals = new int[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 24, 29, 34, 39 };
			int _i = -1;

			for (int j = 0; j < _vals.Length; j++) {
				if (OpenTaiko.ConfigIni.nScrollSpeed[player] >= _vals[j] && j < OpenTaiko.Tx.HiSp.Length)
					_i = j;
				else
					break;
			}

			if (_i >= 0)
				OpenTaiko.Tx.HiSp[_i]?.t2D描画(x, y);
			else
				OpenTaiko.Tx.Mod_None?.t2D描画(x, y);
		}

		static private void tDisplayAutoIcon(int x, int y, int player) {
			bool _displayed = false;

			if (OpenTaiko.ConfigIni.bAutoPlay[player])
				_displayed = true;

			if (_displayed == true)
				OpenTaiko.Tx.Mod_Auto?.t2D描画(x, y);
			else
				OpenTaiko.Tx.Mod_None?.t2D描画(x, y);
		}

		static private void tDisplayDoronIcon(int x, int y, int player) {
			var conf_ = OpenTaiko.ConfigIni.eSTEALTH[player];

			if (conf_ == EStealthMode.DORON)
				OpenTaiko.Tx.Mod_Doron?.t2D描画(x, y);
			else if (conf_ == EStealthMode.STEALTH)
				OpenTaiko.Tx.Mod_Stealth?.t2D描画(x, y);
			else
				OpenTaiko.Tx.Mod_None?.t2D描画(x, y);
		}

		static private void tDisplayJustIcon(int x, int y, int player) {
			var conf_ = OpenTaiko.ConfigIni.bJust[player];

			if (conf_ == 1)
				OpenTaiko.Tx.Mod_Just?.t2D描画(x, y);
			else if (conf_ == 2)
				OpenTaiko.Tx.Mod_Safe?.t2D描画(x, y);
			else
				OpenTaiko.Tx.Mod_None?.t2D描画(x, y);
		}

		static private void tDisplayRandomIcon(int x, int y, int player) {
			var rand_ = OpenTaiko.ConfigIni.eRandom[player];

			if (rand_ == ERandomMode.MIRROR)
				OpenTaiko.Tx.Mod_Mirror?.t2D描画(x, y);
			else if (rand_ == ERandomMode.RANDOM)
				OpenTaiko.Tx.Mod_Random?.t2D描画(x, y);
			else if (rand_ == ERandomMode.SUPERRANDOM)
				OpenTaiko.Tx.Mod_Super?.t2D描画(x, y);
			else if (rand_ == ERandomMode.MIRRORRANDOM)
				OpenTaiko.Tx.Mod_Hyper?.t2D描画(x, y);
			else
				OpenTaiko.Tx.Mod_None?.t2D描画(x, y);
		}

		static private void tDisplaySongSpeedIcon(int x, int y, int player) {
			if (OpenTaiko.ConfigIni.nSongSpeed > 20)
				OpenTaiko.Tx.Mod_SongSpeed[1]?.t2D描画(x, y);
			else if (OpenTaiko.ConfigIni.nSongSpeed < 20)
				OpenTaiko.Tx.Mod_SongSpeed[0]?.t2D描画(x, y);
			else
				OpenTaiko.Tx.Mod_None?.t2D描画(x, y);
		}

		static private void tDisplayFunModIcon(int x, int y, int player) {
			int nFun = (int)OpenTaiko.ConfigIni.nFunMods[player];

			if (nFun > 0)
				OpenTaiko.Tx.Mod_Fun[nFun]?.t2D描画(x, y);
			else
				OpenTaiko.Tx.Mod_None?.t2D描画(x, y);
		}

		static private void tDisplayTimingIcon(int x, int y, int player) {
			int zones = OpenTaiko.ConfigIni.nTimingZones[player];

			if (zones != 2)
				OpenTaiko.Tx.Mod_Timing[zones]?.t2D描画(x, y);
			else
				OpenTaiko.Tx.Mod_None?.t2D描画(x, y);
		}

		static private void PLACEHOLDER_tDisplayNoneIcon(int x, int y, int player) {
			OpenTaiko.Tx.Mod_None?.t2D描画(x, y);
		}

		#endregion

		#region [Mod flags]

		static public bool tPlayIsStock(int player) {
			int actual = OpenTaiko.GetActualPlayer(player);

			if (OpenTaiko.ConfigIni.nFunMods[actual] != EFunMods.NONE) return false;
			if (OpenTaiko.ConfigIni.bJust[actual] != 0) return false;
			if (OpenTaiko.ConfigIni.nTimingZones[actual] != 2) return false;
			if (OpenTaiko.ConfigIni.nSongSpeed != 20) return false;
			if (OpenTaiko.ConfigIni.eRandom[actual] != ERandomMode.OFF) return false;
			if (OpenTaiko.ConfigIni.eSTEALTH[actual] != EStealthMode.OFF) return false;
			if (OpenTaiko.ConfigIni.nScrollSpeed[actual] != 9) return false;

			return true;
		}
		static public Int64 tModsToPlayModsFlags(int player) {
			byte[] _flags = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
			int actual = OpenTaiko.GetActualPlayer(player);

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
}
