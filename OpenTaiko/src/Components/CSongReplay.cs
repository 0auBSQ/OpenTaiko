namespace OpenTaiko;

class CSongReplay {
	/* Game version used for the replay
	 * 521 = 0.5.2.1
	 * 530 = 0.5.3
	 * 531 = 0.5.3.1
	 * 540 = 0.5.4
	 * 600 = 0.6.0
	 * 601 = 0.6.1 (adds the note-shuffle RandomSeed at the end of the file)
	 * 700 = 0.7.0
	 * 1000 = 1.0.0
	 */
	public const int STORED_GAME_VERSION = 601;
	public string REPLAY_FOLDER_NAME = "Replay";

	/* Mod Flags
	 * Bit Offsets (Values) :
	 * - 0 (1) : Mirror
	 * - 1 (2) : Random (Kimagure)
	 * - 2 (4) : Super Random (Detarame)
	 * - 3 (8) : Invisible (Doron)
	 * - 4 (16) : Perfect memory (Stealth)
	 * - 5 (32) : Avalanche
	 * - 6 (64) : Minesweeper
	 * - 7 (128) : Just (Ok => Bad)
	 * - 8 (256) : Safe (Bad => Ok)
	 */
	[Flags]
	public enum EModFlag {
		None = 0,
		Mirror = 1 << 0,
		Random = 1 << 1,
		SuperRandom = 1 << 2,
		Invisible = 1 << 3,
		PerfectMemory = 1 << 4,
		Avalanche = 1 << 5,
		Minesweeper = 1 << 6,
		Just = 1 << 7,
		Safe = 1 << 8,
		DynamicBeat = 1 << 9
	}

	public CSongReplay() {
		replayFolder = "";
		storedPlayer = 0;
	}

	public CSongReplay(string ChartPath, int player) {
		string _chartFolder = Path.GetDirectoryName(ChartPath);
		replayFolder = Path.Combine(_chartFolder, REPLAY_FOLDER_NAME);

		try {
			Directory.CreateDirectory(replayFolder);

			Console.WriteLine("Folder Path: " + replayFolder);
		} catch (Exception ex) {
			Console.WriteLine("An error occurred: " + ex.Message);
		}

		storedPlayer = player;
		chartPath = ChartPath;
		ChartChecksum = tComputeChartMd5(ChartPath);
	}

	// md5 of the chart's .tja file (hex), or "" if it can't be read
	private static string tComputeChartMd5(string path) {
		try {
			using var stream = File.OpenRead(path);
			using var md5 = System.Security.Cryptography.MD5.Create();
			return Convert.ToHexString(md5.ComputeHash(stream));
		} catch {
			return "";
		}
	}

	// The best-plays prefetch hashes the same chart file once per difficulty — cache by write time.
	private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (long ticks, string md5)> _md5Cache = new();
	private static string tComputeChartMd5Cached(string path) {
		try {
			long t = File.GetLastWriteTimeUtc(path).Ticks;
			if (_md5Cache.TryGetValue(path, out var e) && e.ticks == t) return e.md5;
			string m = tComputeChartMd5(path);
			_md5Cache[path] = (t, m);
			return m;
		} catch {
			return "";
		}
	}

	public void tRegisterInput(double timestamp, byte keypress) {
		allInputs.Add(Tuple.Create(timestamp, keypress));
	}

	#region [Dan methods]

	public void tDanRegisterSongCount(int songCount) {
		DanSongCount = songCount;
		IndividualGoodCount = new int[songCount];
		IndividualOkCount = new int[songCount];
		IndividualBadCount = new int[songCount];
		IndividualRollCount = new int[songCount];
		IndividualMaxCombo = new int[songCount];
		IndividualBoomCount = new int[songCount];
		IndividualADLibCount = new int[songCount];
		IndividualScore = new int[songCount];
	}

	public void tDanInputSongResults(int songNo) {
		if (songNo >= DanSongCount) return;
		if (songNo < 0) return;
		IndividualGoodCount[songNo] = OpenTaiko.stageGameScreen.DanSongScore[songNo].nGreat;
		IndividualOkCount[songNo] = OpenTaiko.stageGameScreen.DanSongScore[songNo].nGood;
		IndividualBadCount[songNo] = OpenTaiko.stageGameScreen.DanSongScore[songNo].nMiss;
		IndividualRollCount[songNo] = OpenTaiko.stageGameScreen.DanSongScore[songNo].nRoll;
		IndividualMaxCombo[songNo] = OpenTaiko.stageGameScreen.DanSongScore[songNo].nHighestCombo;
		IndividualBoomCount[songNo] = OpenTaiko.stageGameScreen.DanSongScore[songNo].nMine;
		IndividualADLibCount[songNo] = OpenTaiko.stageGameScreen.DanSongScore[songNo].nADLIB;
		danAccumulatedScore = 0;
		for (int acc = 0; acc < songNo; acc++) danAccumulatedScore += IndividualScore[acc];
		IndividualScore[songNo] = (int)OpenTaiko.stageGameScreen.actScore.Get(0) - danAccumulatedScore;
	}

	#endregion

	#region [Load methods]

	private List<Tuple<double, byte>> ConvertByteArrayToTupleList(byte[] byteArray) {
		List<Tuple<double, byte>> tupleList = new List<Tuple<double, byte>>();

		for (int i = 0; i < byteArray.Length; i += sizeof(double) + sizeof(byte)) {
			double doubleValue = BitConverter.ToDouble(byteArray, i);
			byte byteValue = byteArray[i + sizeof(double)];
			tupleList.Add(Tuple.Create(doubleValue, byteValue));
		}

		return tupleList;
	}

	public void tLoadReplayFile(string optkrFilePath) {
		try {
			using (FileStream fileStream = new FileStream(optkrFilePath, FileMode.Open)) {
				using (BinaryReader reader = new BinaryReader(fileStream)) {
					GameMode = reader.ReadByte();
					GameVersion = reader.ReadInt32();
					ChartChecksum = reader.ReadString();
					PlayerName = reader.ReadString();
					GoodCount = reader.ReadInt32();
					OkCount = reader.ReadInt32();
					BadCount = reader.ReadInt32();
					RollCount = reader.ReadInt32();
					MaxCombo = reader.ReadInt32();
					BoomCount = reader.ReadInt32();
					ADLibCount = reader.ReadInt32();
					Score = reader.ReadInt32();
					CoinValue = reader.ReadInt16();
					ReachedFloor = reader.ReadInt32();
					RemainingLives = reader.ReadInt32();
					DanSongCount = reader.ReadInt32();
					for (int i = 0; i < DanSongCount; i++) {
						IndividualGoodCount[i] = reader.ReadInt32();
						IndividualOkCount[i] = reader.ReadInt32();
						IndividualBadCount[i] = reader.ReadInt32();
						IndividualRollCount[i] = reader.ReadInt32();
						IndividualMaxCombo[i] = reader.ReadInt32();
						IndividualBoomCount[i] = reader.ReadInt32();
						IndividualADLibCount[i] = reader.ReadInt32();
						IndividualScore[i] = reader.ReadInt32();
					}
					ClearStatus = reader.ReadByte();
					ScoreRank = reader.ReadByte();
					ScrollSpeedValue = reader.ReadInt32();
					SongSpeedValue = reader.ReadInt32();
					JudgeStrictnessAdjust = reader.ReadInt32();
					ModFlags = reader.ReadInt32();
					GaugeType = reader.ReadByte();
					GaugeFill = reader.ReadSingle();
					Timestamp = reader.ReadInt64();
					CompressedInputsSize = reader.ReadInt32();
					CompressedInputs = reader.ReadBytes(CompressedInputsSize);
					var uncomp = SevenZip.Compression.LZMA.SevenZipHelper.Decompress(CompressedInputs);
					allInputs = ConvertByteArrayToTupleList(uncomp);
					ChartUniqueID = reader.ReadString();
					ChartDifficulty = reader.ReadByte();
					ChartLevel = reader.ReadByte();
					OnlineScoreID = reader.ReadInt64();
					// note-shuffle seed (added in 601); older replays have none → -1 (not replayable if RNG mods were used)
					RandomSeed = (GameVersion >= 601) ? reader.ReadInt32() : -1;
				}
			}
		} catch (Exception ex) {

		}
	}

	// Lightweight metadata read by the best-plays list. Field order is coupled to tSaveReplayFile/tLoadReplayFile.
	public sealed class ReplayHeader {
		public byte GameMode;
		public int GameVersion;
		public string PlayerName = "";
		public int Good, Ok, Bad, Roll, MaxCombo, Boom, ADLib;   // judge counts (for the best-plays card)
		public int Score;
		public byte ClearStatus;
		public byte ScoreRank;
		public int ModFlags;
		public int ScrollSpeed, SongSpeed, JudgeStrictness;   // mod-icon values (scroll/song-speed/timing zone)
		public long Timestamp;
		public string ChartUniqueID = "";
		public byte ChartDifficulty;
		public int RandomSeed = -1;
		public string FilePath = "";
		public string ChartChecksum = "";
		public bool Watchable;
		public string UnwatchableReason = "";   // tooltip text when Watchable is false
		// warnings surfaced on the best-plays card (tooltip + badge)
		public bool OldVersion;         // recorded by an older game version (calculations may differ)
		public bool ChecksumMismatch;   // chart md5 no longer matches (chart edited since the play)
		// human-readable play date for the list (Timestamp is DateTime.Now.Ticks)
		public string Date {
			get { try { return new DateTime(Timestamp).ToString("yyyy-MM-dd HH:mm"); } catch { return ""; } }
		}
	}

	// RNG mods reproducible via the stored note-shuffle seed (only meaningful once a seed exists)
	private const int RNG_SEEDABLE_MODS = (int)(EModFlag.Random | EModFlag.SuperRandom);
	// RNG mods that aren't seeded, so their replays can never be reproduced (for now)
	private const int RNG_UNSEEDED_MODS = (int)(EModFlag.Avalanche | EModFlag.Minesweeper);

	// true if the replay can be played back faithfully (no unreproducible RNG)
	public static bool tIsReplayWatchable(int modFlags, int randomSeed, int gameVersion)
		=> tUnwatchableReason(modFlags, randomSeed, gameVersion) == null;

	// null when watchable, else the reason shown on the best-plays card's error tooltip.
	// Dynamic Beat is deterministic given the replayed inputs (the warp factor is re-derived from the same
	// judgements), but only while the evaluation logic matches the recorder's — so it needs the same version.
	public static string? tUnwatchableReason(int modFlags, int randomSeed, int gameVersion) {
		if ((modFlags & RNG_UNSEEDED_MODS) != 0) return "Uses random mods that cannot be replayed";
		if ((modFlags & (int)EModFlag.DynamicBeat) != 0 && gameVersion != STORED_GAME_VERSION)
			return "Dynamic Beat replay from a different game version";
		if ((modFlags & RNG_SEEDABLE_MODS) != 0 && randomSeed < 0)
			return "Recorded before the note shuffle was seeded";
		return null;
	}

	// Reads a replay's metadata without decompressing the input log (seeks past it). Returns null on failure.
	public static ReplayHeader tParseHeader(string optkrFilePath) {
		try {
			using var fs = new FileStream(optkrFilePath, FileMode.Open, FileAccess.Read);
			using var r = new BinaryReader(fs);
			var h = new ReplayHeader { FilePath = optkrFilePath };
			h.GameMode = r.ReadByte();
			h.GameVersion = r.ReadInt32();
			h.ChartChecksum = r.ReadString();
			h.PlayerName = r.ReadString();
			h.Good = r.ReadInt32(); h.Ok = r.ReadInt32(); h.Bad = r.ReadInt32(); h.Roll = r.ReadInt32();
			h.MaxCombo = r.ReadInt32(); h.Boom = r.ReadInt32(); h.ADLib = r.ReadInt32();
			h.Score = r.ReadInt32();
			r.ReadInt16();                               // CoinValue
			r.ReadInt32(); r.ReadInt32();                // ReachedFloor, RemainingLives
			int danCount = r.ReadInt32();
			for (int i = 0; i < danCount * 8; i++) r.ReadInt32();   // per-dan-song stats (8 ints each)
			h.ClearStatus = r.ReadByte();
			h.ScoreRank = r.ReadByte();
			h.ScrollSpeed = r.ReadInt32(); h.SongSpeed = r.ReadInt32(); h.JudgeStrictness = r.ReadInt32();
			h.ModFlags = r.ReadInt32();
			r.ReadByte();                                // GaugeType
			r.ReadSingle();                              // GaugeFill
			h.Timestamp = r.ReadInt64();
			int compSize = r.ReadInt32();
			fs.Seek(compSize, SeekOrigin.Current);       // skip the compressed input log
			h.ChartUniqueID = r.ReadString();
			h.ChartDifficulty = r.ReadByte();
			r.ReadByte();                                // ChartLevel
			r.ReadInt64();                               // OnlineScoreID
			h.RandomSeed = (h.GameVersion >= 601) ? r.ReadInt32() : -1;
			h.UnwatchableReason = tUnwatchableReason(h.ModFlags, h.RandomSeed, h.GameVersion) ?? "";
			h.Watchable = h.UnwatchableReason.Length == 0;
			h.OldVersion = h.GameVersion < STORED_GAME_VERSION;
			return h;
		} catch {
			return null;
		}
	}

	// Regular (GameMode 0) replays for a chart+difficulty, ranked by score (highest first), capped to topN.
	// When chartPath is given, each header's ChecksumMismatch is set by comparing its stored chart md5 against
	// the chart file's current md5 (a mismatch means the chart was edited after the play was recorded).
	public static List<ReplayHeader> tListReplays(string songFolder, string uniqueId, int difficulty, int topN, string chartPath = null) {
		var result = new List<ReplayHeader>();
		try {
			string dir = Path.Combine(songFolder, "Replay");
			if (!Directory.Exists(dir)) return result;
			string currentMd5 = string.IsNullOrEmpty(chartPath) ? "" : tComputeChartMd5Cached(chartPath);
			foreach (var file in Directory.EnumerateFiles(dir, "Replay_*.optkr")) {
				var h = tParseHeader(file);
				if (h == null || h.GameMode != 0 || h.ChartUniqueID != uniqueId || h.ChartDifficulty != difficulty) continue;
				if (h.Score <= 0) continue;   // skip empty/incomplete plays (score 0)
				h.ChecksumMismatch = currentMd5 != "" && h.ChartChecksum != "" && !string.Equals(h.ChartChecksum, currentMd5, StringComparison.OrdinalIgnoreCase);
				result.Add(h);
			}
			result.Sort((a, b) => b.Score.CompareTo(a.Score));
			if (topN > 0 && result.Count > topN) result.RemoveRange(topN, result.Count - topN);
		} catch { }
		return result;
	}

	#endregion

	#region [Save methods]

	private byte[] ConvertTupleListToByteArray(List<Tuple<double, byte>> tupleList) {
		List<byte> byteArray = new List<byte>();

		foreach (var tuple in tupleList) {
			byte[] doubleBytes = BitConverter.GetBytes(tuple.Item1);
			byteArray.AddRange(doubleBytes);
			byteArray.Add(tuple.Item2);
		}

		return byteArray.ToArray();
	}

	public void tSaveReplayFile() {
		string _path = replayFolder + @"/Replay_" + ChartUniqueID + @"_" + PlayerName + @"_" + Timestamp.ToString() + @".optkr";

		try {
			using (FileStream fileStream = new FileStream(_path, FileMode.Create)) {
				using (BinaryWriter writer = new BinaryWriter(fileStream)) {
					writer.Write(GameMode);
					writer.Write(GameVersion);
					writer.Write(ChartChecksum);
					writer.Write(PlayerName);
					writer.Write(GoodCount);
					writer.Write(OkCount);
					writer.Write(BadCount);
					writer.Write(RollCount);
					writer.Write(MaxCombo);
					writer.Write(BoomCount);
					writer.Write(ADLibCount);
					writer.Write(Score);
					writer.Write(CoinValue);
					writer.Write(ReachedFloor);
					writer.Write(RemainingLives);
					writer.Write(DanSongCount);
					for (int i = 0; i < DanSongCount; i++) {
						writer.Write(IndividualGoodCount[i]);
						writer.Write(IndividualOkCount[i]);
						writer.Write(IndividualBadCount[i]);
						writer.Write(IndividualRollCount[i]);
						writer.Write(IndividualMaxCombo[i]);
						writer.Write(IndividualBoomCount[i]);
						writer.Write(IndividualADLibCount[i]);
						writer.Write(IndividualScore[i]);
					}
					writer.Write(ClearStatus);
					writer.Write(ScoreRank);
					writer.Write(ScrollSpeedValue);
					writer.Write(SongSpeedValue);
					writer.Write(JudgeStrictnessAdjust);
					writer.Write(ModFlags);
					writer.Write(GaugeType);
					writer.Write(GaugeFill);
					writer.Write(Timestamp);
					writer.Write(CompressedInputsSize);
					writer.Write(CompressedInputs);
					writer.Write(ChartUniqueID);
					writer.Write(ChartDifficulty);
					writer.Write(ChartLevel);
					writer.Write(OnlineScoreID);
					writer.Write(RandomSeed);
				}
			}
		} catch (Exception ex) {

		}
	}

	public void tResultsRegisterReplayInformations(int Coins, int Clear, int SRank) {
		// Actual player (Used for saved informations)
		int actualPlayer = storedPlayer;

		// Game mode
		switch (OpenTaiko.SongMount.nChoosenSongDifficulty[0]) {
			case (int)Difficulty.Dan:
				GameMode = 1;
				break;
			case (int)Difficulty.Tower:
				GameMode = 2;
				break;
			default:
				GameMode = 0;
				break;
		}
		// Game version
		GameVersion = STORED_GAME_VERSION;
		// Chart checksum (md5 of the .tja file) is set in the constructor; don't overwrite it here
		// Player Name
		PlayerName = OpenTaiko.SaveFileInstances[actualPlayer].data.Name;
		// Performance informations
		GoodCount = OpenTaiko.stageGameScreen.CChartScore[storedPlayer].nGreat;
		OkCount = OpenTaiko.stageGameScreen.CChartScore[storedPlayer].nGood;
		BadCount = OpenTaiko.stageGameScreen.CChartScore[storedPlayer].nMiss;
		RollCount = OpenTaiko.stageGameScreen.CChartScore[storedPlayer].nRoll;
		MaxCombo = OpenTaiko.stageGameScreen.actCombo.nCurrentCombo.MaxValue[storedPlayer];
		BoomCount = OpenTaiko.stageGameScreen.CChartScore[storedPlayer].nMine;
		ADLibCount = OpenTaiko.stageGameScreen.CChartScore[storedPlayer].nADLIB;
		Score = (int)OpenTaiko.stageGameScreen.actScore.Get(storedPlayer);   // match the result-screen score (actScore, incl. the deferred +10000 combo bonus) not CChartScore.nScore
		CoinValue = (short)Coins;
		// Tower parameters
		if (GameMode == 2) {
			ReachedFloor = OpenTaiko.stageGameScreen.FloorManagement.LastRegisteredFloor;
			RemainingLives = OpenTaiko.stageGameScreen.FloorManagement.CurrentNumberOfLives;
		}
		// Clear status
		ClearStatus = (byte)Clear;
		// Score rank
		ScoreRank = (byte)SRank;
		// Scroll speed value (as on ConfigIni, 9 is x1)
		ScrollSpeedValue = OpenTaiko.ConfigIni.nScrollSpeed[actualPlayer];
		// Song speed value (as on ConfigIni, 20 is x1)
		SongSpeedValue = OpenTaiko.ConfigIni.nSongSpeed;
		// Just strictess adjust mod value (as on ConfigIni, between -2 for lenient and 2 for rigorous)
		JudgeStrictnessAdjust = OpenTaiko.ConfigIni.nTimingZones[actualPlayer];

		/* Mod Flags
		 * Bit Offsets (Values) :
		 * - 0 (1) : Mirror
		 * - 1 (2) : Random (Kimagure)
		 * - 2 (4) : Super Random (Detarame)
		 * - 3 (8) : Invisible (Doron)
		 * - 4 (16) : Perfect memory (Stealth)
		 * - 5 (32) : Avalanche
		 * - 6 (64) : Minesweeper
		 * - 7 (128) : Just (Ok => Bad)
		 * - 8 (256) : Safe (Bad => Ok)
		 */
		ModFlags = (int)EModFlag.None;
		if (OpenTaiko.ConfigIni.eRandom[actualPlayer] == ERandomMode.Mirror) ModFlags |= (int)EModFlag.Mirror;
		if (OpenTaiko.ConfigIni.eRandom[actualPlayer] == ERandomMode.Random) ModFlags |= (int)EModFlag.Random;
		if (OpenTaiko.ConfigIni.eRandom[actualPlayer] == ERandomMode.SuperRandom) ModFlags |= (int)EModFlag.SuperRandom;
		if (OpenTaiko.ConfigIni.eRandom[actualPlayer] == ERandomMode.MirrorRandom) ModFlags |= ((int)EModFlag.Random | (int)EModFlag.Mirror);
		if (OpenTaiko.ConfigIni.eSTEALTH[actualPlayer] == EStealthMode.Doron) ModFlags |= (int)EModFlag.Invisible;
		if (OpenTaiko.ConfigIni.eSTEALTH[actualPlayer] == EStealthMode.Stealth) ModFlags |= (int)EModFlag.PerfectMemory;
		if (OpenTaiko.ConfigIni.nFunMods[actualPlayer] == EFunMods.Avalanche) ModFlags |= (int)EModFlag.Avalanche;
		if (OpenTaiko.ConfigIni.nFunMods[actualPlayer] == EFunMods.Minesweeper) ModFlags |= (int)EModFlag.Minesweeper;
		if (OpenTaiko.ConfigIni.nFunMods[actualPlayer] == EFunMods.DynamicBeat) ModFlags |= (int)EModFlag.DynamicBeat;
		if (OpenTaiko.ConfigIni.bJust[actualPlayer] == 1) ModFlags |= (int)EModFlag.Just;
		if (OpenTaiko.ConfigIni.bJust[actualPlayer] == 2) ModFlags |= (int)EModFlag.Safe;
		/* Gauge type
		 * - 0 : Normal
		 * - 1 : Hard
		 * - 2 : Extreme
		 */
		GaugeType = (byte)HGaugeMethods.tGetGaugeTypeEnum(storedPlayer);
		// Gauge fill value
		GaugeFill = (float)OpenTaiko.stageGameScreen.actGauge.dbCurrentGaugeValue[storedPlayer];
		// Generation timestamp (in ticks)
		Timestamp = DateTime.Now.Ticks;
		// Compressed inputs and size
		byte[] barr = ConvertTupleListToByteArray(allInputs);
		CompressedInputs = SevenZip.Compression.LZMA.SevenZipHelper.Compress(barr);
		CompressedInputsSize = CompressedInputs.Length;
		// Chart metadata
		// DanBuilder charts have no persistent uniqueId; skip replay recording for them.
		if (OpenTaiko.SongMount.rChoosenSong?.uniqueId == null) return;
		ChartUniqueID = OpenTaiko.SongMount.rChoosenSong.uniqueId.data.id;
		ChartDifficulty = (byte)OpenTaiko.SongMount.nChoosenSongDifficulty[storedPlayer];
		ChartLevel = (byte)Math.Min(255, OpenTaiko.SongMount.rChoosenSong.score[ChartDifficulty].ChartInfo.nLevel[ChartDifficulty]);
		// Online score ID used for online leaderboards linking, given by the server (Defaulted to 0 for now)
		OnlineScoreID = 0;
		// Note-shuffle seed used this play (so Random/Super-Random replays can be reproduced)
		RandomSeed = OpenTaiko.ReplaySeed[storedPlayer];
		// Replay Checksum (Calculate at the end)
		ReplayChecksum = "";
	}

	#endregion

	#region [Virtual mods for replay playback]

	// warnings for the watched replay (in-game "(Invalid replay file)" line); never written to disk
	public bool WarnOldVersion;
	public bool WarnChecksumMismatch;

	// set the playback warnings by comparing this loaded replay against the chart it is about to be played on
	public void tEvaluateWarnings(string currentChartPath) {
		WarnOldVersion = GameVersion < STORED_GAME_VERSION;
		string currentMd5 = string.IsNullOrEmpty(currentChartPath) ? "" : tComputeChartMd5(currentChartPath);
		WarnChecksumMismatch = currentMd5 != "" && ChartChecksum != "" && !string.Equals(ChartChecksum, currentMd5, StringComparison.OrdinalIgnoreCase);
	}

	// snapshot of player-0 mod state, so a watched replay applies its own mods in memory and restores them after
	private static bool _modsSnapped = false;
	private static bool _snapAuto;
	private static ERandomMode _snapRandom;
	private static EStealthMode _snapStealth;
	private static EFunMods _snapFunMods;
	private static int _snapJust, _snapScroll, _snapTimingZones, _snapSongSpeed, _snapSeed;
	private static string _snapName;

	// apply this replay's recorded mods to player 0 IN MEMORY (never exported to Config.ini), snapshotting first
	public void tApplyVirtualMods() {
		var cfg = OpenTaiko.ConfigIni;
		if (!_modsSnapped) {
			_snapAuto = cfg.bAutoPlay[0]; _snapRandom = cfg.eRandom[0]; _snapStealth = cfg.eSTEALTH[0];
			_snapFunMods = cfg.nFunMods[0]; _snapJust = cfg.bJust[0]; _snapScroll = cfg.nScrollSpeed[0];
			_snapTimingZones = cfg.nTimingZones[0]; _snapSongSpeed = cfg.nSongSpeed; _snapSeed = OpenTaiko.ReplaySeed[0];
			_snapName = OpenTaiko.SaveFileInstances[0].data.Name;
			_modsSnapped = true;
		}
		// show the replay's recorded player name on the 1P nameplate for this play (in-memory only: nothing
		// writes data.Name to disk during a play, and tRestoreVirtualMods puts the real name back)
		if (!string.IsNullOrEmpty(PlayerName)) {
			OpenTaiko.SaveFileInstances[0].data.Name = PlayerName;
			OpenTaiko.NamePlate?.tNamePlateRefreshTitles(0);
		}
		if ((ModFlags & (int)EModFlag.Random) != 0 && (ModFlags & (int)EModFlag.Mirror) != 0) cfg.eRandom[0] = ERandomMode.MirrorRandom;
		else if ((ModFlags & (int)EModFlag.SuperRandom) != 0) cfg.eRandom[0] = ERandomMode.SuperRandom;
		else if ((ModFlags & (int)EModFlag.Random) != 0) cfg.eRandom[0] = ERandomMode.Random;
		else if ((ModFlags & (int)EModFlag.Mirror) != 0) cfg.eRandom[0] = ERandomMode.Mirror;
		else cfg.eRandom[0] = ERandomMode.Off;

		if ((ModFlags & (int)EModFlag.Invisible) != 0) cfg.eSTEALTH[0] = EStealthMode.Doron;
		else if ((ModFlags & (int)EModFlag.PerfectMemory) != 0) cfg.eSTEALTH[0] = EStealthMode.Stealth;
		else cfg.eSTEALTH[0] = EStealthMode.Off;

		if ((ModFlags & (int)EModFlag.Avalanche) != 0) cfg.nFunMods[0] = EFunMods.Avalanche;
		else if ((ModFlags & (int)EModFlag.Minesweeper) != 0) cfg.nFunMods[0] = EFunMods.Minesweeper;
		else if ((ModFlags & (int)EModFlag.DynamicBeat) != 0) cfg.nFunMods[0] = EFunMods.DynamicBeat;
		else cfg.nFunMods[0] = EFunMods.None;

		cfg.bJust[0] = (ModFlags & (int)EModFlag.Just) != 0 ? 1 : ((ModFlags & (int)EModFlag.Safe) != 0 ? 2 : 0);
		cfg.nScrollSpeed[0] = ScrollSpeedValue;
		cfg.nSongSpeed = SongSpeedValue;
		cfg.nTimingZones[0] = JudgeStrictnessAdjust;
		cfg.bAutoPlay[0] = false;   // the recorded inputs do the judging, not auto (the auto icon shows via bReplayMode)
		OpenTaiko.ReplaySeed[0] = RandomSeed;
	}

	// restore the player-0 mod state captured by tApplyVirtualMods (idempotent)
	public static void tRestoreVirtualMods() {
		if (!_modsSnapped) return;
		var cfg = OpenTaiko.ConfigIni;
		cfg.bAutoPlay[0] = _snapAuto; cfg.eRandom[0] = _snapRandom; cfg.eSTEALTH[0] = _snapStealth;
		cfg.nFunMods[0] = _snapFunMods; cfg.bJust[0] = _snapJust; cfg.nScrollSpeed[0] = _snapScroll;
		cfg.nTimingZones[0] = _snapTimingZones; cfg.nSongSpeed = _snapSongSpeed; OpenTaiko.ReplaySeed[0] = _snapSeed;
		if (_snapName != null) {
			// defensive: this also runs from the game-exit path, where nameplate/save structures may be tearing down
			try {
				OpenTaiko.SaveFileInstances[0].data.Name = _snapName;
				OpenTaiko.NamePlate?.tNamePlateRefreshTitles(0);
			} catch { }
			_snapName = null;
		}
		_modsSnapped = false;
	}

	#endregion

	#region [Helper variables]

	private string chartPath;
	private string replayFolder;
	private int storedPlayer;
	private int danAccumulatedScore = 0;

	private List<Tuple<double, byte>> allInputs = new List<Tuple<double, byte>>();

	#endregion

	#region [Replay file variables]

	/* Game mode of the replay
	 * 0 = Regular
	 * 1 = Dan
	 * 2 = Tower
	 */
	public byte GameMode = 0;
	// Game version used for the replay
	public int GameVersion;
	// MD5 checksum of the chart
	public string ChartChecksum;
	// Player name
	public string PlayerName;
	// Replay hash
	public string ReplayChecksum;
	/* Performance informations
	 * - Good count (Int)
	 * - Ok count (Int)
	 * - Bad count (Int)
	 * - Roll count (Int)
	 * - Max combo (Int)
	 * - Boom count (Int)
	 * - ADLib count (Int)
	 * - Score (Int)
	 * - Coin value of the play (Short)
	 */
	public int GoodCount;
	public int OkCount;
	public int BadCount;
	public int RollCount;
	public int MaxCombo;
	public int BoomCount;
	public int ADLibCount;
	public int Score;
	public short CoinValue;
	/* Performance informations (Tower only)
	 * - Reached floor (Int)
	 * - Remaining lives (Int)
	 */
	public int ReachedFloor = 0;
	public int RemainingLives = 0;
	// Individual performance informations (Dan only)
	public int DanSongCount = 0;
	public int[] IndividualGoodCount;
	public int[] IndividualOkCount;
	public int[] IndividualBadCount;
	public int[] IndividualRollCount;
	public int[] IndividualMaxCombo;
	public int[] IndividualBoomCount;
	public int[] IndividualADLibCount;
	public int[] IndividualScore;
	/* Clear status
	 * - Regular :
	 *  > 0 : Failed (None)
	 *  > 1 : Assisted clear (Bronze)
	 *  > 2 : Clear (Silver)
	 *  > 3 : Full combo (Gold)
	 *  > 4 : Perfect (Platinum / Rainbow)
	 * - Tower :
	 *  > 0 : None
	 *  > 1 : 10% Mark (初)
	 *  > 2 : 25% Mark (低)
	 *  > 3 : 50% Mark (中)
	 *  > 4 : 75% Mark (高)
	 *  > 5 : Assisted clear (Bronze 可)
	 *  > 6 : Clear (Silver 良)
	 *  > 7 : Full combo (Gold 優)
	 *  > 8 : Perfect (Platinum / Rainbow 秀)
	 *  - Dan :
	 *  > 0 : Failed - No dan title
	 *  > 1 : Assisted Red clear - No dan title
	 *  > 2 : Assisted Gold clear - No dan title
	 *  > 3 : Red clear - Dan title
	 *  > 4 : Gold clear - Dan title
	 *  > 5 : Red full combo - Dan title
	 *  > 6 : Gold full combo - Dan title
	 *  > 7 : Red perfect - Dan title
	 *  > 8 : Gold perfect - Dan title
	 */
	public byte ClearStatus;
	/* Score Rank (Regular only)
	 * - 0 : F (Under 500k, Press F for respects)
	 * - 1 : E (500k ~ Under 600k, Ew...)
	 * - 2 : D (600k ~ Under 700k, Disappointing)
	 * - 3 : C (700k ~ Under 800k, Correct)
	 * - 4 : B (800k ~ Under 900k, Brillant!)
	 * - 5 : A (900k ~ Under 950k, Amazing!)
	 * - 6 : S (950k and more, Splendiferous!!)
	 * - 7 : Ω ((Around) 1M and more, Ωut-of-this-world!!!)
	 */
	public byte ScoreRank;
	// Scroll speed value (as on ConfigIni, 9 is x1)
	public int ScrollSpeedValue;
	// Song speed value (as on ConfigIni, 20 is x1)
	public int SongSpeedValue;
	// Just strictess adjust mod value (as on ConfigIni, between -2 for lenient and 2 for rigorous)
	public int JudgeStrictnessAdjust;
	/* Mod Flags
	 * Bit Offsets (Values) :
	 * - 0 (1) : Mirror
	 * - 1 (2) : Random (Kimagure)
	 * - 2 (4) : Super Random (Detarame)
	 * - 3 (8) : Invisible (Doron)
	 * - 4 (16) : Perfect memory (Stealth)
	 * - 5 (32) : Avalanche
	 * - 6 (64) : Minesweeper
	 * - 7 (128) : Just (Ok => Bad)
	 * - 8 (256) : Safe (Bad => Ok)
	 */
	public int ModFlags;
	/* Gauge type
	 * - 0 : Normal
	 * - 1 : Hard
	 * - 2 : Extreme
	 */
	public byte GaugeType;
	// Gauge fill value
	public float GaugeFill;
	// Generation timestamp (in ticks)
	public long Timestamp;
	// Size in bytes of the compressed inputs (replay data) array
	public int CompressedInputsSize;
	// Compressed inputs (replay data)
	public byte[] CompressedInputs;
	/* Chart metadata
	 * - Chart unique ID : String
	 * - Chart difficulty : Byte (Between 0 and 6)
	 * - Chart level : Byte (Rounded to 255, usually between 0 and 13)
	 */
	public string ChartUniqueID;
	public byte ChartDifficulty;
	public byte ChartLevel;
	// Online score ID used for online leaderboards linking, given by the server
	public long OnlineScoreID;
	// note-shuffle seed (added in version 601); -1 = none recorded (RNG-mod replays from before this are not replayable)
	public int RandomSeed = -1;

	#endregion

	// the recorded inputs (tja time, pad) read by tLoadReplayFile; used to drive replay playback
	public IReadOnlyList<Tuple<double, byte>> Inputs => allInputs;
}
