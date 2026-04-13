namespace OpenTaiko {
	/// <summary>
	/// Read-only view of <see cref="LuaConfigIniFunc"/> for use in <see cref="LuaROActivityWrapper"/> scripts.
	/// Any attempt to call a setter or mutating method logs an error and does nothing.
	/// </summary>
	public class LuaROConfigIniFunc : LuaConfigIniFunc {
		private static void BlockWrite(string member) {
			LogNotification.PopError($"[ROActivity] '{member}' is a write operation and is not allowed in a read-only module.");
		}

		#region [General variables]

		public new int PlayerCount {
			get => base.PlayerCount;
			set => BlockWrite("PlayerCount");
		}

		public new bool IsAIBattleMode {
			get => base.IsAIBattleMode;
			set => BlockWrite("IsAIBattleMode");
		}

		public new int AILevel {
			get => base.AILevel;
			set => BlockWrite("AILevel");
		}

		public new bool IsTrainingMode {
			get => base.IsTrainingMode;
			set => BlockWrite("IsTrainingMode");
		}

		public new bool UseModernScoringMethod {
			get => base.UseModernScoringMethod;
			set => BlockWrite("UseModernScoringMethod");
		}

		public new int UsedLegacyScoringMethod {
			get => base.UsedLegacyScoringMethod;
			set => BlockWrite("UsedLegacyScoringMethod");
		}

		public new void SetGameType(int player, int gt) => BlockWrite("SetGameType");
		public new void SetDefaultCourse(int player, int diff) => BlockWrite("SetDefaultCourse");

		#endregion

		#region [Gameplay mods]

		public new int SongSpeed {
			get => base.SongSpeed;
			set => BlockWrite("SongSpeed");
		}

		public new void SetScrollSpeed(int player, int speed) => BlockWrite("SetScrollSpeed");
		public new void SetTimingZone(int player, int zone) => BlockWrite("SetTimingZone");
		public new void SetAutoStatus(int player, bool isAuto) => BlockWrite("SetAutoStatus");
		public new void SetRandomMod(int player, int mode) => BlockWrite("SetRandomMod");
		public new void SetFunMod(int player, int mod) => BlockWrite("SetFunMod");
		public new void SetStealthMod(int player, int mode) => BlockWrite("SetStealthMod");
		public new void SetJusticeMod(int player, int mode) => BlockWrite("SetJusticeMod");
		public new void SetModFlags(int player, long flags) => BlockWrite("SetModFlags");

		#endregion

		#region [Volume]

		public new int MasterVolume {
			get => base.MasterVolume;
			set => BlockWrite("MasterVolume");
		}

		public new int SoundEffectVolume {
			get => base.SoundEffectVolume;
			set => BlockWrite("SoundEffectVolume");
		}

		public new int VoiceVolume {
			get => base.VoiceVolume;
			set => BlockWrite("VoiceVolume");
		}

		public new int SongVolume {
			get => base.SongVolume;
			set => BlockWrite("SongVolume");
		}

		public new int PreviewVolume {
			get => base.PreviewVolume;
			set => BlockWrite("PreviewVolume");
		}

		#endregion
	}
}
