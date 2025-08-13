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

		public EGameType GetGameType(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return EGameType.Taiko;
			return OpenTaiko.ConfigIni.nGameType[player];
		}

		public void SetGameType(int player, EGameType gt) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(EGameType), gt)) OpenTaiko.ConfigIni.nGameType[player] = gt;
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

		public ERandomMode GetRandomMode(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return ERandomMode.Off;
			return OpenTaiko.ConfigIni.eRandom[player];
		}

		public void SetRandomMode(int player, ERandomMode mode) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(ERandomMode), mode)) OpenTaiko.ConfigIni.eRandom[player] = mode;
		}

		public EFunMods GetFunMod(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return EFunMods.None;
			return OpenTaiko.ConfigIni.nFunMods[player];
		}

		public void SetFunMod(int player, EFunMods mod) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(EFunMods), mod)) OpenTaiko.ConfigIni.nFunMods[player] = mod;
		}

		public EStealthMode GetStealthMode(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return EStealthMode.Off;
			return OpenTaiko.ConfigIni.eSTEALTH[player];
		}

		public void SetStealthMode(int player, EStealthMode mode) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(EStealthMode), mode)) OpenTaiko.ConfigIni.eSTEALTH[player] = mode;
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
	}

	#endregion
}
