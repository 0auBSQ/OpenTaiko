using FDK;

namespace OpenTaiko {
	/// <summary>
	/// Writable derivation of <see cref="LuaROConfigIniFunc"/>.
	/// </summary>
	public class LuaConfigIniFunc : LuaROConfigIniFunc {
		#region [General variables]

		public override int PlayerCount {
			get => base.PlayerCount;
			set {
				if (value > 0 && value <= OpenTaiko.MAX_PLAYERS) {
					OpenTaiko.ConfigIni.nPlayerCount = value;
				}
			}
		}

		public override bool IsAIBattleMode {
			get => base.IsAIBattleMode;
			set {
				OpenTaiko.ConfigIni.bAIBattleMode = value;
			}
		}

		public override int AILevel {
			get => base.AILevel;
			set {
				OpenTaiko.ConfigIni.nAILevel = Math.Clamp(value, 1, 10);
			}
		}

		public override bool IsTrainingMode {
			get => base.IsTrainingMode;
			set {
				OpenTaiko.ConfigIni.bTokkunMode = value;
			}
		}

		public override bool UseModernScoringMethod {
			get => base.UseModernScoringMethod;
			set {
				OpenTaiko.ConfigIni.ShinuchiMode = value;
			}
		}

		public override int UsedLegacyScoringMethod {
			get => base.UsedLegacyScoringMethod;
			set {
				OpenTaiko.ConfigIni.nScoreMode = Math.Clamp(value, 0, 3);
			}
		}

		public override void SetGameType(int player, int gt) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(EGameType), (EGameType)gt)) OpenTaiko.ConfigIni.nGameType[player] = (EGameType)gt;
		}

		public override void SetDefaultCourse(int player, int diff) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			// Difficulty.Edit + 1 is "Ex+ExEx" mode, displaying the highest of both difficulties
			OpenTaiko.ConfigIni.nDefaultCourse = Math.Clamp(diff, (int)Difficulty.Easy, (int)Difficulty.Edit + 1);
		}

		#endregion

		#region [Gameplay mods]

		public override int SongSpeed {
			get => base.SongSpeed;
			set {
				// Set between 2 (0.1x) and 200 (10x) when saved at exit
				OpenTaiko.ConfigIni.nSongSpeed = Math.Clamp(value, CConfigIni.MinimumSongSpeed, CConfigIni.MaximumSongSpeed);
			}
		}

		public override void SetScrollSpeed(int player, int speed) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			// 0 => x0.1, +0.1 per unit
			OpenTaiko.ConfigIni.nScrollSpeed[player] = Math.Clamp(speed, CConfigIni.MinimumScrollSpeed, CConfigIni.MaximumScrollSpeed);
		}

		public override void SetTimingZone(int player, int zone) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			// 0 => Loose, 1 => Lenient, 2 => Normal, 3 => Strict, 4 => Rigorous
			OpenTaiko.ConfigIni.nTimingZones[player] = Math.Clamp(zone, 0, 4);
		}

		public override void SetAutoStatus(int player, bool isAuto) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			OpenTaiko.ConfigIni.bAutoPlay[player] = isAuto;
		}

		public override void SetSpecialMod(int player, int mod) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(ESpecialMod), (ESpecialMod)mod)) OpenTaiko.ConfigIni.SetSpecialMod(player, (ESpecialMod)mod);
		}

		public override void SetRandomMod(int player, int mode) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(ERandomMode), (ERandomMode)mode)) OpenTaiko.ConfigIni.eRandom[player] = (ERandomMode)mode;
		}

		public override void SetFunMod(int player, int mod) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(EFunMods), (EFunMods)mod)) OpenTaiko.ConfigIni.nFunMods[player] = (EFunMods)mod;
		}

		public override void SetStealthMod(int player, int mode) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			if (Enum.IsDefined(typeof(EStealthMode), (EStealthMode)mode)) OpenTaiko.ConfigIni.eSTEALTH[player] = (EStealthMode)mode;
		}

		public override void SetJusticeMod(int player, int mode) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return;
			// 0: Off, 1: Just (Ok => Bad), 2: Safe (Bad => Ok)
			OpenTaiko.ConfigIni.bJust[player] = Math.Clamp(mode, 0, 2);
		}

		public override void SetModFlags(int player, long flags) {
			ModState state = new() { Flags = flags };

			SetScrollSpeed(player, state.ScrollSpeed);
			SetStealthMod(player, state.StealthMod);
			SetRandomMod(player, state.RandomMod);
			SongSpeed = state.SongSpeed;
			SetTimingZone(player, state.TimingZone);
			SetJusticeMod(player, state.JusticeMod);
			SetFunMod(player, state.FunMod);
		}

		#endregion

		#region Volume
		public override int MasterVolume {
			get => base.MasterVolume;
			set { OpenTaiko.ConfigIni.MasterLevel = Math.Clamp(value, CSound.MinimumGroupLevel, CSound.MaximumGroupLevel); }
		}
		public override int SoundEffectVolume {
			get => base.SoundEffectVolume;
			set { OpenTaiko.ConfigIni.SoundEffectLevel = Math.Clamp(value, CSound.MinimumGroupLevel, CSound.MaximumGroupLevel); }
		}
		public override int VoiceVolume {
			get => base.VoiceVolume;
			set { OpenTaiko.ConfigIni.VoiceLevel = Math.Clamp(value, CSound.MinimumGroupLevel, CSound.MaximumGroupLevel); }
		}
		public override int SongVolume {
			get => base.SongVolume;
			set { OpenTaiko.ConfigIni.SongPlaybackLevel = Math.Clamp(value, CSound.MinimumGroupLevel, CSound.MaximumGroupLevel); }
		}
		public override int PreviewVolume {
			get => base.PreviewVolume;
			set { OpenTaiko.ConfigIni.SongPreviewLevel = Math.Clamp(value, CSound.MinimumGroupLevel, CSound.MaximumGroupLevel); }
		}
		#endregion
	}
}
