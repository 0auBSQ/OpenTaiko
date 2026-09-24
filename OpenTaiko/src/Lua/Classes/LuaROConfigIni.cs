namespace OpenTaiko {
	/// <summary>
	/// Read-only base of <see cref="LuaConfigIniFunc"/> for use in <see cref="LuaROActivityWrapper"/> scripts.
	/// Any attempt to call a setter or mutating method logs an error and does nothing.
	/// </summary>
	public class LuaROConfigIniFunc {
		private static void BlockWrite(string member) {
			LogNotification.PopError($"[ROActivity] 'CONFIG.{member}' is a write operation and is not allowed in a read-only module.");
		}

		public LuaROConfigIniFunc AsReadOnly() => new();

		public bool ConfigIsNew {
			get => OpenTaiko.ConfigIsNew;
		}

		#region [General variables]

		// No setter for the Language for now, no reason to use it outside the first boot screen and the settings for the moment
		public string Language {
			get => OpenTaiko.ConfigIni.sLang;
		}

		public virtual int PlayerCount {
			get => OpenTaiko.ConfigIni.nPlayerCount;
			set => BlockWrite(nameof(PlayerCount));
		}

		public virtual bool IsAIBattleMode {
			get => OpenTaiko.ConfigIni.bAIBattleMode;
			set => BlockWrite(nameof(IsAIBattleMode));
		}

		public virtual int AILevel {
			get => OpenTaiko.ConfigIni.nAILevel;
			set => BlockWrite(nameof(AILevel));
		}

		public virtual bool IsTrainingMode {
			get => OpenTaiko.ConfigIni.bTokkunMode;
			set => BlockWrite(nameof(IsTrainingMode));
		}

		public virtual bool UseModernScoringMethod {
			get => OpenTaiko.ConfigIni.ShinuchiMode;
			set => BlockWrite(nameof(UseModernScoringMethod));
		}

		public class LegacyScoringMethod {
			public const int Gen1Oni = 0;
			public const int Gen1_2 = 1;
			public const int Gen2 = Gen1_2;
			public const int Gen3 = 2;
		}
		public readonly LegacyScoringMethod LEGACY_SCORING = new();
		public virtual int UsedLegacyScoringMethod {
			get => OpenTaiko.ConfigIni.nScoreMode;
			set => BlockWrite(nameof(UsedLegacyScoringMethod));
		}

		public class GameType {
			public const int Taiko = (int)EGameType.Taiko;
			public const int Konga = (int)EGameType.Konga;
		}
		public readonly GameType GAMETYPE = new();
		public int GetGameType(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return (int)EGameType.Taiko;
			return (int)OpenTaiko.ConfigIni.nGameType[player];
		}
		public virtual void SetGameType(int player, int gt) => BlockWrite(nameof(SetGameType));

		public class DefaultCourse {
			public const int Easy = (int)Difficulty.Easy;
			public const int Normal = (int)Difficulty.Normal;
			public const int Hard = (int)Difficulty.Hard;
			public const int Oni = (int)Difficulty.Oni;
			public const int Edit = (int)Difficulty.Edit;
		}
		public readonly DefaultCourse DEFAULT_COURSE = new();
		public int GetDefaultCourse(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return (int)Difficulty.Normal;
			return OpenTaiko.ConfigIni.nDefaultCourse;
		}
		public virtual void SetDefaultCourse(int player, int diff) => BlockWrite(nameof(SetDefaultCourse));

		// There might be some funny usages of this
		public bool AreSongUnlockablesDisabled {
			get => OpenTaiko.ConfigIni.bIgnoreSongUnlockables;
		}

		// Whether the player allowed online connections in the settings; there is no setter on purpose
		public bool AreLuaNetworkingConnectionsAllowed {
			get => OpenTaiko.ConfigIni.bAllowLuaNetworkingConnections;
		}
		#endregion

		#region [Gameplay mods]
		public interface ISpeedFunc {
			static abstract int Normal { get; }
			static abstract double ScaleFromActual { get; }
			double ToActual(double speed);
			double dbFromActual(double speed);
			int FromActual(double speed);
		}

		public class SongSpeedFunc : ISpeedFunc {
			public static int Normal => CConfigIni.SongSpeedNormal;
			public static double ScaleFromActual => CConfigIni.SongSpeedScaleFromActual;
			public double ToActual(double speed) => CConfigIni.SongSpeedToActual(speed);
			public double dbFromActual(double actual) => CConfigIni.SongSpeedFromActual(actual);
			public int FromActual(double speed) => (int)Math.Round(dbFromActual(speed));
		}
		public readonly SongSpeedFunc SONGSPEED = new();
		public virtual int SongSpeed {
			get => OpenTaiko.ConfigIni.nSongSpeed;
			set => BlockWrite(nameof(SongSpeed));
		}

		public class ScrollSpeedFunc : ISpeedFunc {
			public static int Normal => CConfigIni.ScrollSpeedNormal;
			public static double ScaleFromActual => CConfigIni.ScrollSpeedScaleFromActual;
			public double ToActual(double speed) => CConfigIni.ScrollSpeedToActual(speed);
			public double dbFromActual(double actual) => CConfigIni.ScrollSpeedFromActual(actual);
			public int FromActual(double speed) => (int)Math.Round(dbFromActual(speed));
		}
		public readonly ScrollSpeedFunc SCROLLSPEED = new();
		public int GetScrollSpeed(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return 9;
			return OpenTaiko.ConfigIni.nScrollSpeed[player];
		}
		public virtual void SetScrollSpeed(int player, int speed) => BlockWrite(nameof(SetScrollSpeed));

		public class TimingZone {
			public const int Loose = -2;
			public const int Lenient = -1;
			public const int Normal = 0;
			public const int Strict = 1;
			public const int Rigorous = 2;
		}
		public readonly TimingZone TIMINGZONE = new();
		public int GetTimingZone(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return 2;
			return OpenTaiko.ConfigIni.nTimingZones[player];
		}
		public virtual void SetTimingZone(int player, int zone) => BlockWrite(nameof(SetTimingZone));

		public bool GetAutoStatus(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return false;
			// replay playback shows the auto modicon too (even though it isn't auto-judging)
			return OpenTaiko.ConfigIni.bAutoPlay[player] || OpenTaiko.bReplayMode[player];
		}
		public virtual void SetAutoStatus(int player, bool isAuto) => BlockWrite(nameof(SetAutoStatus));

		public class RandomMod {
			public const int Off = (int)ERandomMode.Off;
			public const int Random = (int)ERandomMode.Random;
			public const int Mirror = (int)ERandomMode.Mirror;
			public const int SuperRandom = (int)ERandomMode.SuperRandom;
			public const int MirrorRandom = (int)ERandomMode.MirrorRandom;
		}
		public readonly RandomMod RANDOM = new();
		public int GetRandomMod(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return (int)ERandomMode.Off;
			return (int)OpenTaiko.ConfigIni.eRandom[player];
		}
		public virtual void SetRandomMod(int player, int mode) => BlockWrite(nameof(SetRandomMod));

		public class FunMod {
			public const int None = (int)EFunMods.None;
			public const int Avalanche = (int)EFunMods.Avalanche;
			public const int Minesweeper = (int)EFunMods.Minesweeper;
			public const int DynamicBeat = (int)EFunMods.DynamicBeat;
			public const int Total = (int)EFunMods.Total;
		}
		public readonly FunMod FUN = new();
		public int GetFunMod(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return (int)EFunMods.None;
			return (int)OpenTaiko.ConfigIni.nFunMods[player];
		}
		public virtual void SetFunMod(int player, int mod) => BlockWrite(nameof(SetFunMod));

		public class StealthMod {
			public const int Off = (int)EStealthMode.Off;
			public const int Doron = (int)EStealthMode.Doron;
			public const int Stealth = (int)EStealthMode.Stealth;
			public const int Hidden = (int)EStealthMode.Hidden;
			public const int Flashlight = (int)EStealthMode.Flashlight;
		}
		public readonly StealthMod STEALTH = new();
		public int GetStealthMod(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return (int)EStealthMode.Off;
			return (int)OpenTaiko.ConfigIni.eSTEALTH[player];
		}
		public virtual void SetStealthMod(int player, int mode) => BlockWrite(nameof(SetStealthMod));

		public class JusticeMod {
			public const int None = 0;
			public const int Just = 1;
			public const int Safe = 2;
		}
		public readonly JusticeMod JUSTICE = new();
		public int GetJusticeMod(int player) {
			if (player < 0 || player >= OpenTaiko.MAX_PLAYERS) return 0;
			return OpenTaiko.ConfigIni.bJust[player];
		}
		public virtual void SetJusticeMod(int player, int mode) => BlockWrite(nameof(SetJusticeMod));

		public class ModState {
			private readonly byte[] states = new byte[8];

			public long Flags {
				get => BitConverter.ToInt64(states);
				set => Array.Copy(BitConverter.GetBytes(value), states, 8);
			}

			public byte ScrollSpeed { get => states[0]; set => states[0] = value; }
			public byte StealthMod { get => states[1]; set => states[1] = value; }
			public byte RandomMod { get => states[2]; set => states[2] = value; }
			public byte SongSpeed { get => states[3]; set => states[3] = value; }
			public byte TimingZone { get => states[4]; set => states[4] = value; }
			public byte JusticeMod { get => states[5]; set => states[5] = value; }
			[NLua.LuaHide] public byte _unused6 { get => states[6]; set => states[6] = value; }
			public byte FunMod { get => states[7]; set => states[7] = value; }
		}
		public ModState ModFlagsToState(long flags) => new() { Flags = flags };
		public long ModFlagsFromState(ModState state) => state.Flags;
		public long GetModFlags(int player)
			=> new ModState() {
				ScrollSpeed = (byte)Math.Min(255, GetScrollSpeed(player)),
				StealthMod = (byte)GetStealthMod(player),
				RandomMod = (byte)GetRandomMod(player),
				SongSpeed = (byte)Math.Min(255, SongSpeed),
				TimingZone = (byte)GetTimingZone(player),
				JusticeMod = (byte)GetJusticeMod(player),
				FunMod = (byte)GetFunMod(player),
			}.Flags;
		public virtual void SetModFlags(int player, long flags) => BlockWrite(nameof(SetModFlags));
		#endregion

		#region [Volume]

		public virtual int MasterVolume {
			get => OpenTaiko.ConfigIni.MasterLevel;
			set => BlockWrite(nameof(MasterVolume));
		}

		public virtual int SoundEffectVolume {
			get => OpenTaiko.ConfigIni.SoundEffectLevel;
			set => BlockWrite(nameof(SoundEffectVolume));
		}

		public virtual int VoiceVolume {
			get => OpenTaiko.ConfigIni.VoiceLevel;
			set => BlockWrite(nameof(VoiceVolume));
		}

		public virtual int SongVolume {
			get => OpenTaiko.ConfigIni.SongPlaybackLevel;
			set => BlockWrite(nameof(SongVolume));
		}

		public virtual int PreviewVolume {
			get => OpenTaiko.ConfigIni.SongPreviewLevel;
			set => BlockWrite(nameof(PreviewVolume));
		}

		#endregion
	}
}
