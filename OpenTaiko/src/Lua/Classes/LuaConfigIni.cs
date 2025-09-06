namespace OpenTaiko {
	public class LuaConfigIniFunc {

		#region [General variables]

		// No setter for the Language for now, no reason to use it outside the first boot screen and the settings for the moment
		public string Language {
			get {
				return OpenTaiko.ConfigIni.sLang;
			}
		}

		public int PlayerCount {
			get {
				return OpenTaiko.ConfigIni.nPlayerCount;
			}
			set {
				if (value >= 0 && value < OpenTaiko.MAX_PLAYERS) {
					OpenTaiko.ConfigIni.nPlayerCount = value;
				}
			}
		}

		public bool IsAIBattleMode {
			get {
				return OpenTaiko.ConfigIni.bAIBattleMode;
			}
			set {
				OpenTaiko.ConfigIni.bAIBattleMode = value;
			}
		}

		public int AILevel {
			get {
				return OpenTaiko.ConfigIni.nAILevel;
			}
			set {
				OpenTaiko.ConfigIni.nAILevel = Math.Clamp(value, 1, 10);
			}
		}

		public bool IsTrainingMode {
			get {
				return OpenTaiko.ConfigIni.bTokkunMode;
			}
			set {
				OpenTaiko.ConfigIni.bTokkunMode = value;
			}
		}

		public bool UseModernScoringMethod {
			get {
				return OpenTaiko.ConfigIni.ShinuchiMode;
			}
			set {
				OpenTaiko.ConfigIni.ShinuchiMode = value;
			}
		}

		public int UsedLegacyScoringMethod {
			get {
				return OpenTaiko.ConfigIni.nScoreMode;
			}
			set {
				OpenTaiko.ConfigIni.nScoreMode = Math.Clamp(value, 0, 3);
			}
		}

		public int GetGameType(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return (int)EGameType.Taiko;
			return (int)OpenTaiko.ConfigIni.nGameType[player];
		}

		public void SetGameType(int player, int gt) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(EGameType), (EGameType)gt)) OpenTaiko.ConfigIni.nGameType[player] = (EGameType)gt;
		}

		public int GetDefaultCourse(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return (int)Difficulty.Normal;
			return OpenTaiko.ConfigIni.nDefaultCourse;
		}

		public void SetDefaultCourse(int player, int diff) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			// Difficulty.Edit + 1 is "Ex+ExEx" mode, displaying the highest of both difficulties
			OpenTaiko.ConfigIni.nDefaultCourse = Math.Clamp(diff, (int)Difficulty.Easy, (int)Difficulty.Edit + 1);
		}

		// There might be some funny usages of this
		public bool AreSongUnlockablesDisabled {
			get {
				return OpenTaiko.ConfigIni.bIgnoreSongUnlockables;
			}
		}

		#endregion

		#region [Gameplay mods]

		public int SongSpeed {
			get {
				return OpenTaiko.ConfigIni.nSongSpeed;
			}
			set {
				// Set between 5 (0.25x) and 400 (20x) when saved at exit
				OpenTaiko.ConfigIni.nSongSpeed = Math.Max(1, value);
			}
		}

		public int GetScrollSpeed(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return 9;
			return OpenTaiko.ConfigIni.nScrollSpeed[player];
		}

		public void SetScrollSpeed(int player, int speed) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			// 0 => x0.1, +0.1 per unit
			OpenTaiko.ConfigIni.nScrollSpeed[player] = Math.Max(0, speed);
		}

		public int GetTimingZone(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return 2;
			return OpenTaiko.ConfigIni.nTimingZones[player];
		}

		public void SetTimingZone(int player, int zone) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			// 0 => Loose, 1 => Lenient, 2 => Normal, 3 => Strict, 4 => Rigorous
			OpenTaiko.ConfigIni.nTimingZones[player] = Math.Clamp(zone, 0, 4);
		}

		public bool GetAutoStatus(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return false;
			return OpenTaiko.ConfigIni.bAutoPlay[player];
		}

		public void SetAutoStatus(int player, bool isAuto) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			OpenTaiko.ConfigIni.bAutoPlay[player] = isAuto;
		}

		public int GetRandomMode(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return (int)ERandomMode.Off;
			return (int)OpenTaiko.ConfigIni.eRandom[player];
		}

		public void SetRandomMode(int player, int mode) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(ERandomMode), (ERandomMode)mode)) OpenTaiko.ConfigIni.eRandom[player] = (ERandomMode)mode;
		}

		public int GetFunMod(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return (int)EFunMods.None;
			return (int)OpenTaiko.ConfigIni.nFunMods[player];
		}

		public void SetFunMod(int player, int mod) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(EFunMods), (EFunMods)mod)) OpenTaiko.ConfigIni.nFunMods[player] = (EFunMods)mod;
		}

		public int GetStealthMode(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return (int)EStealthMode.Off;
			return (int)OpenTaiko.ConfigIni.eSTEALTH[player];
		}

		public void SetStealthMode(int player, int mode) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(EStealthMode), (EStealthMode)mode)) OpenTaiko.ConfigIni.eSTEALTH[player] = (EStealthMode)mode;
		}

		public int GetJusticeMode(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return 0;
			return OpenTaiko.ConfigIni.bJust[player];
		}

		public void SetJusticeMode(int player, int mode) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			// 0: Off, 1: Just (Ok => Bad), 2: Safe (Bad => Ok)
			OpenTaiko.ConfigIni.bJust[player] = Math.Clamp(mode, 0, 2);
		}

		public Int64 GetModsFlags(int player) {
			byte[] _flags = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

			_flags[0] = (byte)Math.Min(255, GetScrollSpeed(player));
			_flags[1] = (byte)GetStealthMode(player);
			_flags[2] = (byte)GetRandomMode(player);
			_flags[3] = (byte)Math.Min(255, SongSpeed);
			_flags[4] = (byte)GetTimingZone(player);
			_flags[5] = (byte)GetJusticeMode(player);
			_flags[7] = (byte)GetFunMod(player);

			return BitConverter.ToInt64(_flags, 0);
		}

		public void SetModsFlags(int player, Int64 flags) {
			byte[] _flags = BitConverter.GetBytes(flags);

			SetScrollSpeed(player, _flags[0]);
			SetStealthMode(player, _flags[1]);
			SetRandomMode(player, _flags[2]);
			SongSpeed = _flags[3];
			SetTimingZone(player, _flags[4]);
			SetJusticeMode(player, _flags[5]);
			SetFunMod(player, _flags[7]);
		}
	}

	#endregion
}
