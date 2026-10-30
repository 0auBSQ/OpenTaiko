using System.Diagnostics;
using System.Numerics;
using FDK;

namespace OpenTaiko;

class TextureLoader {
	public static string BASE = @$"Graphics{Path.DirectorySeparatorChar}";
	public static string GLOBAL = @$"Global{Path.DirectorySeparatorChar}";

	// Global assets
	public static string PUCHICHARA = @$"PuchiChara{Path.DirectorySeparatorChar}";
	public static string CHARACTERS = @$"Characters{Path.DirectorySeparatorChar}";

	// Stage
	public static string STARTUP = @$"0_Startup{Path.DirectorySeparatorChar}";
	public static string TITLE = @$"1_Title{Path.DirectorySeparatorChar}";
	public static string CONFIG = @$"2_Config{Path.DirectorySeparatorChar}";
	public static string SONGSELECT = @$"3_SongSelect{Path.DirectorySeparatorChar}";
	public static string DANISELECT = @$"3_DaniSelect{Path.DirectorySeparatorChar}";
	public static string GAME = @$"5_Game{Path.DirectorySeparatorChar}";
	public static string RESULT = @$"6_Result{Path.DirectorySeparatorChar}";
	public static string DANRESULT = @$"7_DanResult{Path.DirectorySeparatorChar}";
	public static string TOWERRESULT = @$"8_TowerResult{Path.DirectorySeparatorChar}";

	public static string MODALS = @$"11_Modals{Path.DirectorySeparatorChar}";
	public static string ONLINELOUNGE = @$"12_OnlineLounge{Path.DirectorySeparatorChar}";

	// InGame
	public static string DANCER = @$"2_Dancer{Path.DirectorySeparatorChar}";
	public static string MOB = @$"3_Mob{Path.DirectorySeparatorChar}";
	public static string COURSESYMBOL = @$"4_CourseSymbol{Path.DirectorySeparatorChar}";
	public static string BACKGROUND = @$"5_Background{Path.DirectorySeparatorChar}";
	public static string TAIKO = @$"6_Taiko{Path.DirectorySeparatorChar}";
	public static string GAUGE = @$"7_Gauge{Path.DirectorySeparatorChar}";
	public static string FOOTER = @$"8_Footer{Path.DirectorySeparatorChar}";
	public static string END = @$"9_End{Path.DirectorySeparatorChar}";
	public static string EFFECTS = @$"10_Effects{Path.DirectorySeparatorChar}";
	public static string BALLOON = @$"11_Balloon{Path.DirectorySeparatorChar}";
	public static string LANE = @$"12_Lane{Path.DirectorySeparatorChar}";
	public static string GENRE = @$"13_GENRE{Path.DirectorySeparatorChar}";
	public static string GAMEMODE = @$"14_GameMode{Path.DirectorySeparatorChar}";
	public static string FAILED = @$"15_Failed{Path.DirectorySeparatorChar}";
	public static string RUNNER = @$"16_Runner{Path.DirectorySeparatorChar}";
	public static string TRAINING = @$"19_Training{Path.DirectorySeparatorChar}";
	public static string DANC = @$"17_DanC{Path.DirectorySeparatorChar}";
	public static string TOWER = @$"20_Tower{Path.DirectorySeparatorChar}";
	public static string MODICONS = @$"21_ModIcons{Path.DirectorySeparatorChar}";
	public static string AIBATTLE = @$"22_AIBattle{Path.DirectorySeparatorChar}";

	// Special balloons
	public static string KUSUDAMA = @$"Kusudama{Path.DirectorySeparatorChar}";
	public static string FUSE = @$"Fuseroll{Path.DirectorySeparatorChar}";

	// InGame_Effects
	public static string FIRE = @$"Fire{Path.DirectorySeparatorChar}";
	public static string HIT = @$"Hit{Path.DirectorySeparatorChar}";
	public static string ROLL = @$"Roll{Path.DirectorySeparatorChar}";
	public static string SPLASH = @$"Splash{Path.DirectorySeparatorChar}";

	public Dictionary<string, CTexture> trackedTextures = new Dictionary<string, CTexture>();

	// Gameplay-screen textures (the "5_演奏画面" region below), collected so they can be warmed
	// (decoded + uploaded) during the song-load screen instead of lazily on first draw during play,
	// which otherwise causes an in-frame decode hitch the first time each effect appears.
	public readonly List<CTexture?> gameplayTextures = new();
	private bool _collectGameplay;
	private int _warmIndex;

	// What a gameplay texture needs to be drawn at all; the warm pass skips it when the coming play cannot draw it.
	// A skipped texture that is drawn anyway still loads on its first draw.
	[Flags]
	internal enum WarmNeed {
		Always = 0,
		Dan = 1 << 0,
		Tower = 1 << 1,
		Training = 1 << 2,
		AI = 1 << 3,
		Konga = 1 << 4,       // a Konga player, or a chart note switched to Konga
		Mine = 1 << 5,        // a mine or fuse-roll note in the chart (a failed fuse roll draws the bomb effect)
		TwoPlus = 1 << 6,     // a second player (or the AI)
		ThreePlus = 1 << 7,   // three players or more
		Partner = 1 << 8,     // a partner note in the chart
		Giant = 1 << 9,       // a giant note in the chart
	}
	private readonly Dictionary<CTexture, WarmNeed> _warmNeeds = new();
	private WarmNeed _playNeeds;

	private CTexture? TxCFor(WarmNeed need, string FileName) {
		var tex = TxC(FileName);
		if (tex != null && need != WarmNeed.Always) _warmNeeds[tex] = need;
		return tex;
	}

	private CTextureAf? TxCAfFor(WarmNeed need, string FileName) {
		var tex = TxCAf(FileName);
		if (tex != null && need != WarmNeed.Always) _warmNeeds[tex] = need;
		return tex;
	}

	// what the coming play can draw: its mode, its players and its charts (loaded by now)
	private static WarmNeed tPlayNeeds() {
		var cfg = OpenTaiko.ConfigIni;
		var needs = WarmNeed.Always;
		var difficulty = (Difficulty)OpenTaiko.SongMount.nChoosenSongDifficulty[0];
		if (difficulty == Difficulty.Dan) needs |= WarmNeed.Dan;
		if (difficulty == Difficulty.Tower) needs |= WarmNeed.Tower;
		if (cfg.bTokkunMode) needs |= WarmNeed.Training;
		if (cfg.bAIBattleMode) needs |= WarmNeed.AI | WarmNeed.TwoPlus;
		if (cfg.nPlayerCount >= 2) needs |= WarmNeed.TwoPlus;
		if (cfg.nPlayerCount >= 3) needs |= WarmNeed.ThreePlus;
		for (int p = 0; p < cfg.nPlayerCount; p++) {
			if (cfg.nGameType[p] == EGameType.Konga) needs |= WarmNeed.Konga;
			if (cfg.nFunMods[p] == EFunMods.Minesweeper) needs |= WarmNeed.Mine;
			var tja = OpenTaiko.GetTJA(p);
			if (tja == null) continue;
			if (tja.PlayerSideMetadata.GameType == EGameType.Konga) needs |= WarmNeed.Konga;
			foreach (var chip in tja.listChip) {
				if (chip.eGameType == EGameType.Konga) needs |= WarmNeed.Konga;
				if (NotesManager.IsMine(chip) || NotesManager.IsFuzeRoll(chip)) needs |= WarmNeed.Mine;
				if (chip.IsPartnerNote) needs |= WarmNeed.Partner;
				if (chip.IsGiantNote) needs |= WarmNeed.Giant;
			}
		}
		return needs;
	}

	public TextureLoader() {
		// Constructor
	}

	internal CTexture? TxC(string FileName, bool localize = true) {
		tTickTextureProgress();
		var texpath = (localize) ? HLocalizedPath.GetAvailableLocalizedPath(CSkin.Path(BASE + FileName)) : CSkin.Path(BASE + FileName);
		var tex = OpenTaiko.tTextureCreate(texpath, false);

		listTexture.Add(tex);
		if (_collectGameplay) gameplayTextures.Add(tex);
		return tex;
	}

	internal CTexture? TxCGlobal(string FileName) {
		tTickTextureProgress();
		var tex = OpenTaiko.tTextureCreate(OpenTaiko.strEXEFolder + GLOBAL + FileName, false);
		listTexture.Add(tex);
		return tex;
	}

	internal CTexture? TxCAbsolute(string FileName) {
		tTickTextureProgress();
		var tex = OpenTaiko.tTextureCreate(FileName, false);
		listTexture.Add(tex);
		return tex;
	}

	internal CTextureAf? TxCAf(string FileName) {
		tTickTextureProgress();
		var tex = OpenTaiko.tTextureCreateAf(CSkin.Path(BASE + FileName));
		listTexture.Add(tex);
		if (_collectGameplay) gameplayTextures.Add(tex);
		return tex;
	}

	/// <summary>Reset the gameplay-texture warm cursor. Call at the start of each song load.</summary>
	public void BeginWarmGameplay() {
		_warmIndex = 0;
		_playNeeds = tPlayNeeds();
	}

	/// <summary>Force-realize deferred gameplay textures (decode + GL upload) within a time budget so
	/// their first in-game draw hits a resident texture instead of an inline decode hitch. Returns true
	/// when the whole set has been warmed. Render thread; call each frame during the song-load screen.</summary>
	public bool WarmGameplayBatch(double budgetMs) {
		long start = System.Diagnostics.Stopwatch.GetTimestamp();
		while (_warmIndex < gameplayTextures.Count) {
			var tex = gameplayTextures[_warmIndex++];
			if (tex == null) continue;
			if (_warmNeeds.TryGetValue(tex, out var need) && (need & _playNeeds) == 0) continue;
			tex.RealizeIfDeferred();
			if (System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds >= budgetMs)
				break;
		}
		return _warmIndex >= gameplayTextures.Count;
	}
	internal CTexture? TxCGen(string FileName) {
		tTickTextureProgress();
		return OpenTaiko.tTextureCreate(CSkin.Path(BASE + GAME + GENRE + FileName + ".png"), false);
	}

	internal CTexture? TxCSong(string path) {
		return TxCUntrackedSong(path);
	}

	private CTexture?[] TxCSong(int count, string format, int start = 0) {
		return TxCSong(format, Enumerable.Range(start, count).Select(o => o.ToString()).ToArray());
	}

	private CTexture?[] TxCSong(string format, params string[] parts) {
		return parts.Select(o => TxCSong(string.Format(format, o))).ToArray();
	}

	public CTexture?[]? TxCSongFolder(string folder) {
		// Match .png case-insensitively so mixed-case frame names work on case-sensitive filesystems (iOS).
		if (!Directory.Exists(folder)) return null;
		var byIndex = new Dictionary<int, string>();
		foreach (var file in Directory.EnumerateFiles(folder, "*.png", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive })) {
			if (int.TryParse(Path.GetFileNameWithoutExtension(file), out int n))
				byIndex[n] = file;
		}
		var textures = new List<CTexture?>();
		for (int i = 0; byIndex.TryGetValue(i, out string? path); i++)
			textures.Add(TxCSong(path));
		return textures.Count == 0 ? null : textures.ToArray();
	}

	internal CTexture? TxCUntrackedSong(string path) {
		tTickTextureProgress();
		// Chart-object textures (#ADDOBJECT / #CHANGETEXTURE) are created during the OFF-thread chart parse. Load
		// them async so the GL upload lands on the render thread (Game.AsyncActions) — doing GL on the parse thread
		// is unreliable (blank images). The CTexture is returned immediately (blank, correct count) + fills in
		// before gameplay; t2DDraw no-ops until then.
		bool prev = CTexture.AsyncLoad;
		CTexture.AsyncLoad = true;
		try { return OpenTaiko.tTextureCreate(path, false); }
		finally { CTexture.AsyncLoad = prev; }
	}

	// ── Boot loading-bar progress ──────────────────────────────────────────────────────────────────
	// LoadTexture() loads hundreds of textures as a flat sequence (no loop to count), so we count calls
	// through the TxC* family while it runs and map them onto the 20%..100% range of the boot bar (the
	// first 20% is the system-sound preload). The total isn't known statically, so we use an estimate
	// that is persisted after each run (exact from the second boot onward) and let the startup stage snap
	// the bar to 100% when LoadTexture returns.
	private static readonly string strTextureCountCache = OpenTaiko.strEXEFolder + "startup_textures.count";
	private bool bReportTextureProgress = false;
	private int nTexturesLoaded = 0;
	private int nTextureEstimate = 320;   // first-run fallback; refined from strTextureCountCache afterwards
	private long _bootTexturesStart;
	// Boot-screen status of LoadTexture: how many textures so far (of the estimate), and the sub-phase running
	// when it is not plain textures (the character sets)
	public int BootTexturesLoaded => nTexturesLoaded;
	public int BootTexturesEstimate => nTextureEstimate;
	public volatile string BootLabel = "";
	private void tTickTextureProgress() {
		if (!bReportTextureProgress) return;
		nTexturesLoaded++;
		// Boot loading bar: textures are the final 66-100% (modules 0-60%, system sounds 60-66%).
		CLoadingProgress.ReportSegment(0.66f, 1.0f, nTexturesLoaded, nTextureEstimate);
	}

	public void LoadTexture() {
		#region [ Boot loading-bar bracket: start ]
		nTexturesLoaded = 0;
		try {
			if (File.Exists(strTextureCountCache)
				&& int.TryParse(File.ReadAllText(strTextureCountCache).Trim(), out int est) && est > 0)
				nTextureEstimate = est;
		} catch { /* estimate stays at the fallback */ }
		bReportTextureProgress = true;
		_bootTexturesStart = System.Diagnostics.Stopwatch.GetTimestamp();
		#endregion

		CalibrateFG = TxC(CONFIG + $@"Calibration{Path.DirectorySeparatorChar}FG.png");
		CalibrateBG = TxC(CONFIG + $@"Calibration{Path.DirectorySeparatorChar}BG.png");

		#region 共通
		Tile_Black = TxC(@$"Tile_Black.png");
		Menu_Title = TxC(@$"Menu_Title.png");
		Menu_Highlight = TxC(@$"Menu_Highlight.png");
		Loading = TxC(@$"Loading.png");
		Scanning_Loudness = TxC(@$"Scanning_Loudness.png");
		Overlay = TxC(@$"Overlay.png");
		NamePlateBase = TxC(@$"NamePlate.png");

		#endregion

		#region 2_コンフィグ画面
		// The legacy C# config screen's rendering was replaced by the Lua config_ui ROActivity, so its UI
		// textures (Cursor / ItemBox / Arrow / KeyAssign / Font / Font_Bold) are never drawn any more and are
		// no longer loaded. The "Enumerating songs…" icon likewise moved to the Lua song_enum ROActivity, so
		// Enum_Song / Config_Enum_Song are no longer loaded here either.
		#endregion

		#region 3_段位選択画面 (textures still used by result/gameplay stages)

		Dani_Difficulty_Cymbol = TxC(DANISELECT + "Difficulty_Cymbol.png");
		Dani_Level_Number = TxC(DANISELECT + "Level_Number.png");
		Dani_DanIcon = TxC(DANISELECT + "DanIcon.png");
		Dani_DanIcon_Fade = TxC(DANISELECT + "DanIcon_Fade.png");

		#endregion

		#region 5_演奏画面
		_collectGameplay = true;

		#region General

		// the notes, rolls, balloons, hit effects and judges come from the selected packs: LoadNotesSkinTextures
		Note_Mine = TxCFor(WarmNeed.Mine, GAME + @$"Mine.png");

		SENotes = new CTexture[2];
		SENotes[0] = TxC(GAME + @$"SENotes.png");
		SENotes[1] = TxCFor(WarmNeed.Konga, GAME + @$"SENotes_Konga.png");

		SENotesExtension = TxC(GAME + @$"SENotes_Extension.png");

		Notes_Arm = TxC(GAME + @$"Notes_Arm.png");
		ScoreRank = TxC(GAME + @$"ScoreRank.png");

		Judge_Meter = TxC(GAME + @$"Judge_Meter.png");
		Bar = TxC(GAME + @$"Bar.png");
		Bar_Branch = TxC(GAME + @$"Bar_Branch.png");

		Vector2 judgeDiff = new(OpenTaiko.Skin.Game_Judge_X[1] - OpenTaiko.Skin.Game_Judge_X[0], OpenTaiko.Skin.Game_Judge_Y[1] - OpenTaiko.Skin.Game_Judge_Y[0]);
		OpenTaiko.Skin.Init_Game_Notes_Arm_Configs(CSkin.ToVector2(Notes_Arm!.szTextureSize), CSkin.ToVector2(OpenTaiko.Skin.Game_Notes_Size), judgeDiff,
			(float)OpenTaiko.Skin.ScaleY * new Vector2(35, 0), (float)OpenTaiko.Skin.ScaleY * 8);

		var _presetsDefs = CSkin.Path(BASE + GAME + BACKGROUND + @$"Presets.json");
		if (File.Exists(_presetsDefs))
			OpenTaiko.Skin.Game_SkinScenes = ConfigManager.GetConfig<DBSkinPreset.SkinPreset>(_presetsDefs);
		else
			OpenTaiko.Skin.Game_SkinScenes = new DBSkinPreset.SkinPreset();

		#endregion

		#region Taiko

		Taiko_Background = new CTexture[12];
		Taiko_Background[0] = TxC(GAME + TAIKO + @$"1P_Background.png");
		Taiko_Background[1] = TxCFor(WarmNeed.TwoPlus, GAME + TAIKO + @$"2P_Background.png");
		Taiko_Background[2] = TxCFor(WarmNeed.Dan, GAME + TAIKO + @$"Dan_Background.png");
		Taiko_Background[3] = TxCFor(WarmNeed.Tower, GAME + TAIKO + @$"Tower_Background.png");
		Taiko_Background[4] = TxC(GAME + TAIKO + @$"1P_Background_Right.png");
		Taiko_Background[5] = TxCFor(WarmNeed.Training, GAME + TAIKO + @$"1P_Background_Tokkun.png");
		Taiko_Background[6] = TxCFor(WarmNeed.Training, GAME + TAIKO + @$"2P_Background_Tokkun.png");
		Taiko_Background[7] = TxCFor(WarmNeed.ThreePlus, GAME + TAIKO + @$"3P_Background.png");
		Taiko_Background[8] = TxCFor(WarmNeed.ThreePlus, GAME + TAIKO + @$"4P_Background.png");
		Taiko_Background[9] = TxCFor(WarmNeed.AI, GAME + TAIKO + @$"AI_Background.png");
		Taiko_Background[10] = TxC(GAME + TAIKO + @$"Boss_Background.png");
		Taiko_Background[11] = TxCFor(WarmNeed.ThreePlus, GAME + TAIKO + @$"5P_Background.png");

		Taiko_Frame = new CTexture[7];
		Taiko_Frame[0] = TxC(GAME + TAIKO + @$"1P_Frame.png");
		Taiko_Frame[1] = TxCFor(WarmNeed.TwoPlus, GAME + TAIKO + @$"2P_Frame.png");
		Taiko_Frame[2] = TxCFor(WarmNeed.Tower, GAME + TAIKO + @$"Tower_Frame.png");
		Taiko_Frame[3] = TxCFor(WarmNeed.Training, GAME + TAIKO + @$"Tokkun_Frame.png");
		Taiko_Frame[4] = TxCFor(WarmNeed.AI, GAME + TAIKO + @$"2P_None_Frame.png");
		Taiko_Frame[5] = TxCFor(WarmNeed.AI, GAME + TAIKO + @$"AI_Frame.png");
		Taiko_Frame[6] = TxCFor(WarmNeed.ThreePlus, GAME + TAIKO + @$"4PPlay_Frame.png");

		Taiko_PlayerNumber = new CTexture[5];
		Taiko_PlayerNumber[0] = TxC(GAME + TAIKO + @$"1P_PlayerNumber.png");
		Taiko_PlayerNumber[1] = TxCFor(WarmNeed.TwoPlus, GAME + TAIKO + @$"2P_PlayerNumber.png");
		Taiko_PlayerNumber[2] = TxCFor(WarmNeed.ThreePlus, GAME + TAIKO + @$"3P_PlayerNumber.png");
		Taiko_PlayerNumber[3] = TxCFor(WarmNeed.ThreePlus, GAME + TAIKO + @$"4P_PlayerNumber.png");
		Taiko_PlayerNumber[4] = TxCFor(WarmNeed.ThreePlus, GAME + TAIKO + @$"5P_PlayerNumber.png");


		Taiko_Base = new CTexture[2];
		Taiko_Base[0] = TxC(GAME + TAIKO + @$"Base.png");
		Taiko_Base[1] = TxCFor(WarmNeed.Konga, GAME + TAIKO + @$"Base_Konga.png");

		Taiko_Don_Left = TxC(GAME + TAIKO + @$"Don.png");
		Taiko_Don_Right = TxC(GAME + TAIKO + @$"Don.png");
		Taiko_Ka_Left = TxC(GAME + TAIKO + @$"Ka.png");
		Taiko_Ka_Right = TxC(GAME + TAIKO + @$"Ka.png");

		Taiko_Konga_Don = TxCFor(WarmNeed.Konga, GAME + TAIKO + @$"Don_Konga.png");
		Taiko_Konga_Ka = TxCFor(WarmNeed.Konga, GAME + TAIKO + @$"Ka_Konga.png");
		Taiko_Konga_Clap = TxCFor(WarmNeed.Konga, GAME + TAIKO + @$"Clap.png");

		Taiko_LevelUp = TxC(GAME + TAIKO + @$"LevelUp.png");
		Taiko_LevelDown = TxC(GAME + TAIKO + @$"LevelDown.png");
		Couse_Symbol = new CTexture[(int)Difficulty.Total + 1]; // +1は真打ちモードの分
		Couse_Symbol_Back = new CTexture[(int)Difficulty.Total + 1]; // +1は真打ちモードの分
		Couse_Symbol_Back_Flash = new CTexture[(int)Difficulty.Total + 1]; // +1は真打ちモードの分
		string[] Couse_Symbols = new string[(int)Difficulty.Total + 1] { "Easy", "Normal", "Hard", "Oni", "Edit", "Tower", "Dan", "Shin" };
		for (int i = 0; i < (int)Difficulty.Total + 1; i++) {
			Couse_Symbol[i] = TxC(GAME + COURSESYMBOL + Couse_Symbols[i] + ".png");
			Couse_Symbol_Back[i] = TxC(GAME + COURSESYMBOL + Couse_Symbols[i] + "_Back.png");
			Couse_Symbol_Back_Flash[i] = TxC(GAME + COURSESYMBOL + Couse_Symbols[i] + "_Back_Flash.png");
		}

		Taiko_Score = new CTexture[6];
		Taiko_Score[0] = TxC(GAME + TAIKO + @$"Score.png");
		Taiko_Score[1] = TxC(GAME + TAIKO + @$"Score_1P.png");
		Taiko_Score[2] = TxC(GAME + TAIKO + @$"Score_2P.png");
		Taiko_Score[3] = TxC(GAME + TAIKO + @$"Score_3P.png");
		Taiko_Score[4] = TxC(GAME + TAIKO + @$"Score_4P.png");
		Taiko_Score[5] = TxC(GAME + TAIKO + @$"Score_5P.png");
		Taiko_Combo = new CTexture[4];
		Taiko_Combo[0] = TxC(GAME + TAIKO + @$"Combo.png");
		Taiko_Combo[1] = TxC(GAME + TAIKO + @$"Combo_Big.png");
		Taiko_Combo[2] = TxC(GAME + TAIKO + @$"Combo_Midium.png");
		Taiko_Combo[3] = TxC(GAME + TAIKO + @$"Combo_Huge.png");
		Taiko_Combo_Effect = TxC(GAME + TAIKO + @$"Combo_Effect.png");
		Taiko_Combo_Text = TxC(GAME + TAIKO + @$"Combo_Text.png");

		Taiko_Combo_Guide = new CTexture[3];
		for (int i = 0; i < Taiko_Combo_Guide.Length; i++) {
			Taiko_Combo_Guide[i] = TxC(GAME + TAIKO + @$"Combo_Guide" + i.ToString() + ".png");
		}

		#endregion

		#region Gauge

		Gauge = new CTexture[8];
		Gauge[0] = TxC(GAME + GAUGE + @$"1P.png");
		Gauge[1] = TxCFor(WarmNeed.TwoPlus, GAME + GAUGE + @$"2P.png");
		Gauge[2] = TxC(GAME + GAUGE + @$"1P_Right.png");
		Gauge[3] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"1P_4PGauge.png");
		Gauge[4] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"2P_4PGauge.png");
		Gauge[5] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"3P_4PGauge.png");
		Gauge[6] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"4P_4PGauge.png");
		Gauge[7] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"5P_4PGauge.png");

		Gauge_Base = new CTexture[8];
		Gauge_Base[0] = TxC(GAME + GAUGE + @$"1P_Base.png");
		Gauge_Base[1] = TxCFor(WarmNeed.TwoPlus, GAME + GAUGE + @$"2P_Base.png");
		Gauge_Base[2] = TxC(GAME + GAUGE + @$"1P_Base_Right.png");
		Gauge_Base[3] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"1P_Base_4PGauge.png");
		Gauge_Base[4] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"2P_Base_4PGauge.png");
		Gauge_Base[5] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"3P_Base_4PGauge.png");
		Gauge_Base[6] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"4P_Base_4PGauge.png");
		Gauge_Base[7] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"5P_Base_4PGauge.png");

		Gauge_Line = new CTexture[2];
		Gauge_Line[0] = TxC(GAME + GAUGE + @$"1P_Line.png");
		Gauge_Line[1] = TxC(GAME + GAUGE + @$"2P_Line.png");

		Gauge_Clear = new CTexture[3];
		Gauge_Clear[0] = TxC(GAME + GAUGE + @$"Clear.png");
		Gauge_Clear[1] = TxCFor(WarmNeed.TwoPlus, GAME + GAUGE + @$"Clear_2PGauge.png");
		Gauge_Clear[2] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"Clear_4PGauge.png");

		Gauge_Base_Norma = new CTexture[3];
		Gauge_Base_Norma[0] = TxC(GAME + GAUGE + @$"Norma_Base.png");
		Gauge_Base_Norma[1] = TxCFor(WarmNeed.TwoPlus, GAME + GAUGE + @$"Norma_Base_2PGauge.png");
		Gauge_Base_Norma[2] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"Norma_Base_4PGauge.png");

		Gauge_Killzone = new CTexture[3];
		Gauge_Killzone[0] = TxC(GAME + GAUGE + @$"Killzone.png");
		Gauge_Killzone[1] = TxCFor(WarmNeed.TwoPlus, GAME + GAUGE + @$"Killzone_2PGauge.png");
		Gauge_Killzone[2] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"Killzone_4PGauge.png");

		OpenTaiko.Skin.Game_Gauge_Rainbow_Ptn = OpenTaiko.tSequenceImageSheetCountCount(CSkin.Path(BASE + GAME + GAUGE + @$"Rainbow{Path.DirectorySeparatorChar}"));
		if (OpenTaiko.Skin.Game_Gauge_Rainbow_Ptn != 0) {
			Gauge_Rainbow = new CTexture[OpenTaiko.Skin.Game_Gauge_Rainbow_Ptn];
			for (int i = 0; i < OpenTaiko.Skin.Game_Gauge_Rainbow_Ptn; i++) {
				Gauge_Rainbow[i] = TxC(GAME + GAUGE + @$"Rainbow{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			}
		}

		OpenTaiko.Skin.Game_Gauge_Rainbow_Flat_Ptn = OpenTaiko.tSequenceImageSheetCountCount(CSkin.Path(BASE + GAME + GAUGE + @$"Rainbow_Flat{Path.DirectorySeparatorChar}"));
		if (OpenTaiko.Skin.Game_Gauge_Rainbow_Flat_Ptn != 0) {
			Gauge_Rainbow_Flat = new CTexture[OpenTaiko.Skin.Game_Gauge_Rainbow_Flat_Ptn];
			for (int i = 0; i < OpenTaiko.Skin.Game_Gauge_Rainbow_Flat_Ptn; i++) {
				Gauge_Rainbow_Flat[i] = TxCFor(WarmNeed.ThreePlus, GAME + GAUGE + @$"Rainbow_Flat{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			}
		}

		OpenTaiko.Skin.Game_Gauge_Rainbow_2PGauge_Ptn = OpenTaiko.tSequenceImageSheetCountCount(CSkin.Path(BASE + GAME + GAUGE + @$"Rainbow_2PGauge{Path.DirectorySeparatorChar}"));
		if (OpenTaiko.Skin.Game_Gauge_Rainbow_2PGauge_Ptn != 0) {
			Gauge_Rainbow_2PGauge = new CTexture[OpenTaiko.Skin.Game_Gauge_Rainbow_2PGauge_Ptn];
			for (int i = 0; i < OpenTaiko.Skin.Game_Gauge_Rainbow_2PGauge_Ptn; i++) {
				Gauge_Rainbow_2PGauge[i] = TxCFor(WarmNeed.TwoPlus, GAME + GAUGE + @$"Rainbow_2PGauge{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			}
		}

		// Dan

		OpenTaiko.Skin.Game_Gauge_Dan_Rainbow_Ptn = OpenTaiko.tSequenceImageSheetCountCount(CSkin.Path(BASE + GAME + DANC + @$"Rainbow{Path.DirectorySeparatorChar}"));
		if (OpenTaiko.Skin.Game_Gauge_Dan_Rainbow_Ptn != 0) {
			Gauge_Dan_Rainbow = new CTexture[OpenTaiko.Skin.Game_Gauge_Dan_Rainbow_Ptn];
			for (int i = 0; i < OpenTaiko.Skin.Game_Gauge_Dan_Rainbow_Ptn; i++) {
				Gauge_Dan_Rainbow[i] = TxCFor(WarmNeed.Dan, GAME + DANC + @$"Rainbow{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			}
		}

		Gauge_Dan = new CTexture[6];

		Gauge_Dan[0] = TxCFor(WarmNeed.Dan, GAME + GAUGE + @$"1P_Dan_Base.png");
		Gauge_Dan[1] = TxCFor(WarmNeed.Dan, GAME + GAUGE + @$"1P_Dan.png");
		Gauge_Dan[2] = TxCFor(WarmNeed.Dan, GAME + GAUGE + @$"1P_Dan_Clear_Base.png");
		Gauge_Dan[3] = TxCFor(WarmNeed.Dan, GAME + GAUGE + @$"1P_Dan_Clear.png");
		Gauge_Dan[4] = TxCFor(WarmNeed.Dan, GAME + GAUGE + @$"1P_Dan_Base_Right.png");
		Gauge_Dan[5] = TxCFor(WarmNeed.Dan, GAME + GAUGE + @$"1P_Dan_Right.png");

		Gauge_Soul = TxC(GAME + GAUGE + @$"Soul.png");
		Gauge_Soul_Fire = TxC(GAME + GAUGE + @$"Fire.png");
		Gauge_Soul_Explosion = new CTexture[2];
		Gauge_Soul_Explosion[0] = TxC(GAME + GAUGE + @$"1P_Explosion.png");
		Gauge_Soul_Explosion[1] = TxC(GAME + GAUGE + @$"2P_Explosion.png");

		#endregion

		#region Balloon

		Balloon_Combo = new CTexture[2];
		Balloon_Combo[0] = TxC(GAME + BALLOON + @$"Combo_1P.png");
		Balloon_Combo[1] = TxC(GAME + BALLOON + @$"Combo_2P.png");
		Balloon_Number_Roll = TxC(GAME + BALLOON + @$"Number_Roll.png");
		Balloon_Number_Combo = TxC(GAME + BALLOON + @$"Number_Combo.png");

		#endregion

		#region Effects

		Effects_Fire = TxC(GAME + EFFECTS + @$"Fire.png");
		if (Effects_Fire != null) Effects_Fire.bAddBlend = OpenTaiko.Skin.Game_Effect_Fire_AddBlend;

		Effects_Rainbow = TxC(GAME + EFFECTS + @$"Rainbow.png");

		Effects_GoGoSplash = TxC(GAME + EFFECTS + @$"GoGoSplash.png");
		if (Effects_GoGoSplash != null) Effects_GoGoSplash.bAddBlend = OpenTaiko.Skin.Game_Effect_GoGoSplash_AddBlend;
		OpenTaiko.Skin.Game_Effect_Roll_Ptn = OpenTaiko.tSequenceImageSheetCountCount(CSkin.Path(BASE + GAME + EFFECTS + @$"Roll{Path.DirectorySeparatorChar}"));
		Effects_Roll = new CTexture[OpenTaiko.Skin.Game_Effect_Roll_Ptn];
		for (int i = 0; i < OpenTaiko.Skin.Game_Effect_Roll_Ptn; i++) {
			Effects_Roll[i] = TxC(GAME + EFFECTS + @$"Roll{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
		}

		#endregion

		#region Lane

		Lane_Base = new CTexture[3];
		Lane_Text = new CTexture[3];
		string[] Lanes = new string[3] { "Normal", "Expert", "Master" };
		for (int i = 0; i < 3; i++) {
			Lane_Base[i] = TxC(GAME + LANE + "Base_" + Lanes[i] + ".png");
			Lane_Text[i] = TxC(GAME + LANE + "Text_" + Lanes[i] + ".png");
		}

		Lane_Red = new CTexture[2];
		Lane_Blue = new CTexture[2];
		Lane_Clap = new CTexture[2];

		var _suffixes = new string[] { "", "_Konga" };

		for (int i = 0; i < Lane_Red.Length; i++) {
			var need = i == 1 ? WarmNeed.Konga : WarmNeed.Always;
			Lane_Red[i] = TxCFor(need, GAME + LANE + @$"Red" + _suffixes[i] + @$".png");
			Lane_Blue[i] = TxCFor(need, GAME + LANE + @$"Blue" + _suffixes[i] + @$".png");
			Lane_Clap[i] = TxCFor(need, GAME + LANE + @$"Clap" + _suffixes[i] + @$".png");
		}


		Lane_Yellow = TxC(GAME + LANE + @$"Yellow.png");
		Lane_Background_Main = TxC(GAME + LANE + @$"Background_Main.png");
		Lane_Background_AI = TxCFor(WarmNeed.AI, GAME + LANE + @$"Background_AI.png");
		Lane_Background_Sub = TxC(GAME + LANE + @$"Background_Sub.png");
		Lane_Background_GoGo = TxC(GAME + LANE + @$"Background_GoGo.png");

		#endregion

		#region GameMode

		GameMode_Timer_Frame = TxC(GAME + GAMEMODE + @$"Timer_Frame.png");

		#endregion

		#region DanC

		DanC_Background = TxCFor(WarmNeed.Dan, GAME + DANC + @$"Background.png");
		DanC_Gauge = new CTexture[4];
		var type = new string[] { "Normal", "Reach", "Clear", "Flush" };
		for (int i = 0; i < 4; i++) {
			DanC_Gauge[i] = TxCFor(WarmNeed.Dan, GAME + DANC + @$"Gauge_" + type[i] + ".png");
		}
		DanC_Base = TxCFor(WarmNeed.Dan, GAME + DANC + @$"Base.png");
		DanC_Base_Small = TxCFor(WarmNeed.Dan, GAME + DANC + @$"Base_Small.png");

		DanC_Gauge_Base = TxCFor(WarmNeed.Dan, GAME + DANC + @$"Gauge_Base.png");
		DanC_Failed = TxCFor(WarmNeed.Dan, GAME + DANC + @$"Failed.png");
		DanC_Number = TxCFor(WarmNeed.Dan, GAME + DANC + @$"Number.png");
		DanC_Small_Number = TxCFor(WarmNeed.Dan, GAME + DANC + @$"Small_Number.png");
		DanC_ExamType = TxCFor(WarmNeed.Dan, GAME + DANC + @$"ExamType.png");
		DanC_ExamRange = TxCFor(WarmNeed.Dan, GAME + DANC + @$"ExamRange.png");
		DanC_Screen = TxCFor(WarmNeed.Dan, GAME + DANC + @$"Screen.png");
		DanC_SmallBase = TxCFor(WarmNeed.Dan, GAME + DANC + @$"SmallBase.png");
		DanC_Small_ExamCymbol = TxCFor(WarmNeed.Dan, GAME + DANC + @$"Small_ExamCymbol.png");
		DanC_ExamCymbol = TxCFor(WarmNeed.Dan, GAME + DANC + @$"ExamCymbol.png");
		DanC_MiniNumber = TxCFor(WarmNeed.Dan, GAME + DANC + @$"MiniNumber.png");

		#endregion

		#region PuchiChara

		var puchicharaDirs = OpenTaiko.GetMergedDirectories(OpenTaiko.strEXEFolder + GLOBAL + PUCHICHARA);
		OpenTaiko.Skin.Puchichara_Ptn = puchicharaDirs.Length;

		Puchichara = new CPuchichara[OpenTaiko.Skin.Puchichara_Ptn];
		OpenTaiko.Skin.Puchicharas_Name = new string[OpenTaiko.Skin.Puchichara_Ptn];

		for (int i = 0; i < OpenTaiko.Skin.Puchichara_Ptn; i++) {
			Puchichara[i] = new CPuchichara(puchicharaDirs[i]);

			OpenTaiko.Skin.Puchicharas_Name[i] = System.IO.Path.GetFileName(puchicharaDirs[i]);
		}
		OpenTaiko.Skin.Puchicharas_NameToIndex = OpenTaiko.Skin.Puchicharas_Name.Select((val, idx) => (val, idx)).ToDictionary();
		LuaPuchicharaDb?.Dispose();
		LuaPuchicharaDb = new LuaPuchicharaDatabase(Puchichara);

		///TJAPlayer3.Skin.Puchichara_Ptn = 5 * Math.Max(1, (PuchiChara.szテクスチャサイズ.Height / 256));


		#endregion

		#region Training

		Tokkun_DownBG = TxCFor(WarmNeed.Training, GAME + TRAINING + @$"Down.png");
		Tokkun_BigTaiko = TxCFor(WarmNeed.Training, GAME + TRAINING + @$"BigTaiko.png");
		Tokkun_ProgressBar = TxCFor(WarmNeed.Training, GAME + TRAINING + @$"ProgressBar_Red.png");
		Tokkun_ProgressBarWhite = TxCFor(WarmNeed.Training, GAME + TRAINING + @$"ProgressBar_White.png");
		Tokkun_GoGoPoint = TxCFor(WarmNeed.Training, GAME + TRAINING + @$"GoGoPoint.png");
		Tokkun_JumpPoint = TxCFor(WarmNeed.Training, GAME + TRAINING + @$"JumpPoint.png");
		Tokkun_Background_Up = TxCFor(WarmNeed.Training, GAME + TRAINING + @$"Background_Up.png");
		Tokkun_BigNumber = TxCFor(WarmNeed.Training, GAME + TRAINING + @$"BigNumber.png");
		Tokkun_SmallNumber = TxCFor(WarmNeed.Training, GAME + TRAINING + @$"SmallNumber.png");
		Tokkun_Speed_Measure = TxCFor(WarmNeed.Training, GAME + TRAINING + @$"Speed_Measure.png");

		#endregion

		#region [20_Tower]

		// The tower itself (its floors, roof and sky) is a tower look, a Down background folder under
		// 5_Background/Tower/Down/, drawn by its own script and composed by Lua elsewhere
		Tower_Miss = TxCFor(WarmNeed.Tower, GAME + TOWER + @$"Miss.png");

		#endregion

		#region [22_AIBattle]

		AIBattle_SectionTime_Panel = TxCFor(WarmNeed.AI, GAME + AIBATTLE + @$"SectionTime_Panel.png");

		AIBattle_SectionTime_Bar_Base = TxCFor(WarmNeed.AI, GAME + AIBATTLE + @$"SectionTime_Bar_Base.png");
		AIBattle_SectionTime_Bar_Finish = TxCFor(WarmNeed.AI, GAME + AIBATTLE + @$"SectionTime_Bar_Finish.png");
		AIBattle_SectionTime_Bar_Normal = TxCFor(WarmNeed.AI, GAME + AIBATTLE + @$"SectionTime_Bar_Normal.png");

		AIBattle_Batch_Base = TxCFor(WarmNeed.AI, GAME + AIBATTLE + @$"Batch_Base.png");
		AIBattle_Batch = TxCFor(WarmNeed.AI, GAME + AIBATTLE + @$"Batch.png");

		AIBattle_Judge_Meter[0] = TxCFor(WarmNeed.AI, GAME + AIBATTLE + @$"Judge_Meter.png");
		AIBattle_Judge_Meter[1] = TxCFor(WarmNeed.AI, GAME + AIBATTLE + @$"Judge_Meter_AI.png");

		AIBattle_Judge_Number = TxCFor(WarmNeed.AI, GAME + AIBATTLE + @$"Judge_Number.png");

		#endregion

		_collectGameplay = false;
		#endregion

		#region 6_結果発表

		// the result screens are the skin's results_* ROActivities; the play's judge meter draws with these digits
		Result_Number = TxC(RESULT + @$"Number.png");

		#endregion

		#region [11_Characters]

		string[] charaDirs = OpenTaiko.GetMergedDirectories(OpenTaiko.strEXEFolder + GLOBAL + CHARACTERS);

		Characters = new CCharacterLuaSet[charaDirs.Length];

		string[] charaDirNames = new string[charaDirs.Length];
		for (int i = 0; i < charaDirs.Length; i++) {
			BootLabel = $"CHARACTERS ... {Path.GetFileName(charaDirs[i])} ({i + 1}/{charaDirs.Length})";
			Characters[i] = new(charaDirs[i], i);
			charaDirNames[i] = Characters[i].dirName;
		}

		// before the player loads below, so CHARACTERLIST exists even if one of them fails
		LuaCharacterDb?.Dispose();
		LuaCharacterDb = new LuaCharacterDatabase(Characters);

		for (int i = 0; i < 5; i++) {
			BootLabel = $"CHARACTERS ... player {i + 1} ({OpenTaiko.SaveFileInstances[i].data.Character})";
			OpenTaiko.SaveFileInstances[i].tReindexCharacter(charaDirNames);
			if (Characters.Length > 0)
				this.ReloadCharacter(-1, OpenTaiko.SaveFileInstances[i].data.Character, i, true);
			PaletteManager.RestoreFromSave(i);
			PlayerCharacters[i] = new LuaCharacter(i);

			// If the saved puchichara folder no longer exists, fall back to index 0.
			string puchi = OpenTaiko.SaveFileInstances[i].data.PuchiChara;
			if (OpenTaiko.Skin.Puchicharas_Name.Length > 0 && !OpenTaiko.Skin.Puchicharas_NameToIndex.ContainsKey(puchi))
				OpenTaiko.SaveFileInstances[i].data.PuchiChara = OpenTaiko.Skin.Puchicharas_Name[0];
		}

		OpenTaiko.Databases?.LoadThemeSettings();
		CNotesSkins.Scan();
		LoadNotesSkinTextures();
		CVirtualSlotManager.Initialize();
		BootLabel = "";

		#endregion

		#region [12_OnlineLounge]

		//OnlineLounge_Background = TxC(ONLINELOUNGE + @"Background.png");
		OnlineLounge_Box = TxC(ONLINELOUNGE + @"Box.png");
		OnlineLounge_Side_Menu = TxC(ONLINELOUNGE + @"Side_Menu.png");
		OnlineLounge_Context = TxC(ONLINELOUNGE + @"Context.png");
		OnlineLounge_Song_Box = TxC(ONLINELOUNGE + @"Song_Box.png");
		OnlineLounge_Return_Box = TxC(ONLINELOUNGE + @"Return_Box.png");

		#endregion

		if (OpenTaiko.ConfigIni.PreAssetsLoading) {
			foreach (var act in OpenTaiko.app.listTopLevelActivities) {
				act.CreateManagedResource();
				act.CreateUnmanagedResource();
			}
		}

		#region [ Boot loading-bar bracket: end ]
#if DEBUG
		Trace.TraceInformation($"[BOOT] textures: {nTexturesLoaded} in {System.Diagnostics.Stopwatch.GetElapsedTime(_bootTexturesStart).TotalMilliseconds:F0} ms");
#endif
		bReportTextureProgress = false;
		try {
			File.WriteAllText(strTextureCountCache, nTexturesLoaded.ToString());
		} catch { /* non-fatal: just means next boot reuses the previous estimate */ }
		#endregion
	}

	// the textures of the selected notes and judges packs, replaced when another pack is selected
	private readonly List<CTexture> _notesSkinTextures = new();

	/// <summary>(Re)loads the textures of the selected notes and judges packs (<see cref="CNotesSkins"/>); a skin
	/// without packs loads its former files.</summary>
	public void LoadNotesSkinTextures() {
		foreach (var tex in _notesSkinTextures) {
			if (tex == null) continue;
			listTexture.Remove(tex);
			gameplayTextures.Remove(tex);
			_warmNeeds.Remove(tex);
			tex.Dispose();
		}
		_notesSkinTextures.Clear();
		bool collect = _collectGameplay;
		_collectGameplay = true;
		try {
			LoadNotesSkinTexturesCore();
		} finally {
			_collectGameplay = collect;
		}
	}

	private T? Pack<T>(T? tex) where T : CTexture {
		if (tex != null) _notesSkinTextures.Add(tex);
		return tex;
	}

	private CTexture? PackTx(WarmNeed need, string? rel) => rel == null ? null : Pack(TxCFor(need, rel));
	private CTexture? PackTxAf(WarmNeed need, string? rel) => rel == null ? null : Pack<CTexture>(TxCAfFor(need, rel));

	private void LoadNotesSkinTexturesCore() {
		const CNotesSkins.EKind TAIKO = CNotesSkins.EKind.Taiko, BONGO = CNotesSkins.EKind.Bongo, JUDGES = CNotesSkins.EKind.Judges;
		char sep = Path.DirectorySeparatorChar;
		bool taikoPacks = CNotesSkins.HasPacks(TAIKO), bongoPacks = CNotesSkins.HasPacks(BONGO);
		// a Taiko pack's file, or the former file when the skin has no Taiko packs
		string? T(string rel, string legacy, bool selectedOnly = false)
			=> taikoPacks ? CNotesSkins.File_(TAIKO, rel, selectedOnly) : legacy;
		string? B(string rel, string legacy, bool selectedOnly = false)
			=> bongoPacks ? CNotesSkins.File_(BONGO, rel, selectedOnly) : legacy;

		Notes = new CTexture[2];
		Rolls = new CTexture[2];
		HitCircle = new CTexture[2];
		ChipEffect = new CTexture[2];
		Notes_Partner = new CTexture[2];
		Notes_Giant = new CTexture[2];

		Notes[0] = PackTx(WarmNeed.Always, T("Notes.png", GAME + "Notes.png"));
		Notes[1] = PackTx(WarmNeed.Konga, B("Notes.png", GAME + "Notes_Konga.png"));
		Rolls[0] = taikoPacks ? PackTx(WarmNeed.Always, T("Rolls.png", "")) : null;
		Rolls[1] = bongoPacks ? PackTx(WarmNeed.Konga, B("Rolls.png", "")) : null;
		HitCircle[0] = PackTx(WarmNeed.Always, T("HitCircle.png", GAME + "Notes.png"));
		HitCircle[1] = bongoPacks ? PackTx(WarmNeed.Konga, CNotesSkins.File_(BONGO, "HitCircle.png", true)) : null;
		ChipEffect[0] = PackTx(WarmNeed.Always, T("ChipEffect.png", GAME + "ChipEffect.png", true));
		ChipEffect[1] = bongoPacks ? PackTx(WarmNeed.Konga, CNotesSkins.File_(BONGO, "ChipEffect.png", true)) : null;
		Notes_Partner[0] = taikoPacks ? PackTx(WarmNeed.Partner, CNotesSkins.File_(TAIKO, "Notes_Partner.png", true)) : null;
		Notes_Partner[1] = bongoPacks ? PackTx(WarmNeed.Partner, CNotesSkins.File_(BONGO, "Notes_Partner.png", true)) : null;
		Notes_Giant[0] = taikoPacks ? PackTx(WarmNeed.Giant, CNotesSkins.File_(TAIKO, "Notes_Giant.png", true)) : null;
		Notes_Giant[1] = bongoPacks ? PackTx(WarmNeed.Giant, CNotesSkins.File_(BONGO, "Notes_Giant.png", true)) : null;
		if (HitCircle[0] != null) HitCircle[0]!.bAddBlend = OpenTaiko.Skin.Game_JudgeFrame_AddBlend;
		if (HitCircle[1] != null) HitCircle[1]!.bAddBlend = OpenTaiko.Skin.Game_JudgeFrame_AddBlend;

		// the balloons are the Taiko set's whatever the game type
		Balloons = taikoPacks ? PackTx(WarmNeed.Always, T("Balloons.png", "")) : null;
		Note_Swap = taikoPacks ? null : PackTx(WarmNeed.Always, GAME + "Swap.png");
		Note_Kusu = taikoPacks ? null : PackTx(WarmNeed.Always, GAME + "Kusu.png");
		Note_FuseRoll = taikoPacks ? null : PackTx(WarmNeed.Always, GAME + "FuseRoll.png");
		Note_Adlib = taikoPacks ? null : PackTx(WarmNeed.Always, GAME + "Adlib.png");

		Balloon_Roll = PackTx(WarmNeed.Always, T("Roll.png", GAME + BALLOON + "Roll.png"));
		Balloon_Balloon = PackTx(WarmNeed.Always, T("Balloon.png", GAME + BALLOON + "Balloon.png"));
		Balloon_Breaking = new CTexture[6];
		Balloon_Breaking_Sheet = null;
		if (taikoPacks) {
			Balloon_Breaking_Sheet = PackTx(WarmNeed.Always, T("Breaking.png", ""));
		} else {
			for (int i = 0; i < 6; i++)
				Balloon_Breaking[i] = PackTx(WarmNeed.Always, GAME + BALLOON + "Breaking_" + i + ".png");
		}
		Kusudama_Number = PackTx(WarmNeed.Always, T(KUSUDAMA + "Kusudama_Number.png", GAME + BALLOON + KUSUDAMA + "Kusudama_Number.png"));
		Fuse_Number = PackTx(WarmNeed.Mine, T(FUSE + "Number_Fuse.png", GAME + BALLOON + FUSE + "Number_Fuse.png"));
		Fuse_Balloon = PackTx(WarmNeed.Mine, T(FUSE + "Fuse.png", GAME + BALLOON + FUSE + "Fuse.png"));

		string hit = GAME + EFFECTS + HIT;
		Effects_Hit_Explosion = PackTxAf(WarmNeed.Always, T(HIT + "Explosion.png", hit + "Explosion.png"));
		if (Effects_Hit_Explosion != null) Effects_Hit_Explosion.bAddBlend = OpenTaiko.Skin.Game_Effect_HitExplosion_AddBlend;
		Effects_Hit_Explosion_Big = PackTx(WarmNeed.Always, T(HIT + "Explosion_Big.png", hit + "Explosion_Big.png"));
		if (Effects_Hit_Explosion_Big != null) Effects_Hit_Explosion_Big.bAddBlend = OpenTaiko.Skin.Game_Effect_HitExplosionBig_AddBlend;
		Effects_Hit_FireWorks = PackTx(WarmNeed.Always, T(HIT + "FireWorks.png", hit + "FireWorks.png"));
		if (Effects_Hit_FireWorks != null) Effects_Hit_FireWorks.bAddBlend = OpenTaiko.Skin.Game_Effect_FireWorks_AddBlend;
		Effects_Hit_Bomb = PackTxAf(WarmNeed.Mine, T(HIT + "Bomb.png", hit + "Bomb.png"));

		Effects_Hit_Great = new CTexture[15];
		Effects_Hit_Great_Big = new CTexture[15];
		Effects_Hit_Good = new CTexture[15];
		Effects_Hit_Good_Big = new CTexture[15];
		for (int i = 0; i < 15; i++) {
			Effects_Hit_Great[i] = PackTx(WarmNeed.Always, T($"{HIT}Great{sep}{i}.png", $"{hit}Great{sep}{i}.png"));
			Effects_Hit_Great_Big[i] = PackTx(WarmNeed.Always, T($"{HIT}Great_Big{sep}{i}.png", $"{hit}Great_Big{sep}{i}.png"));
			Effects_Hit_Good[i] = PackTx(WarmNeed.Always, T($"{HIT}Good{sep}{i}.png", $"{hit}Good{sep}{i}.png"));
			Effects_Hit_Good_Big[i] = PackTx(WarmNeed.Always, T($"{HIT}Good_Big{sep}{i}.png", $"{hit}Good_Big{sep}{i}.png"));
		}

		Judge = PackTx(WarmNeed.Always, CNotesSkins.HasPacks(JUDGES) ? CNotesSkins.File_(JUDGES, "Judge.png") : GAME + "Judge.png");
	}

	/// <summary>The kusudama script's folder, relative to Graphics/.</summary>
	public static string KusudamaFolder()
		=> CNotesSkins.Dir(CNotesSkins.EKind.Taiko, "Kusudama") is string dir ? dir + Path.DirectorySeparatorChar
			: GAME + BALLOON + KUSUDAMA;

	public int[] CreateNumberedArrayFromInt(int total) {
		int[] array = new int[total];
		for (int i = 0; i < total; i++) {
			array[i] = i;
		}
		return array;
	}

	public CSkin.CSystemSound VoiceSelectOggOrWav(string basePath) {
		if (File.Exists(basePath + @$".ogg"))
			return new CSkin.CSystemSound(basePath + @$".ogg", false, false, true, ESoundGroup.Voice);
		else
			return new CSkin.CSystemSound(basePath + @$".wav", false, false, true, ESoundGroup.Voice);
	}


	public void ReloadCharacter(int old, int newC, int player, bool primary = false) {
		if (old == newC || (OpenTaiko.SaveFileInstances[player].data.Character == newC && !primary))
			return;

		if (old >= 0 && !primary) {
			OpenTaiko.SaveFileInstances[player].data.mountedCharacter = null;
			playerResourceRefCounts[player] = Characters[old][player].CharaUnload();
		}

		if ((newC >= 0 && OpenTaiko.SaveFileInstances[player].data.Character != newC) || primary) {
			Characters[newC][player].CharaLoad(playerResourceRefCounts[player]);
			playerResourceRefCounts[player] = null;
			OpenTaiko.SaveFileInstances[player].data.mountedCharacter = ((CCharacterLua)Characters[newC][player]).Script;
		}
	}

	/// <summary>
	/// Swaps the character animations loaded for a player slot without touching any
	/// save-file data.  Used by the virtual slot system when mounting or unmounting.
	/// Unlike <see cref="ReloadCharacter"/>, this never modifies
	/// <c>SaveFileInstances[player].data</c>.
	/// </summary>
	public void SwapCharacterAnimations(int oldIdx, int newIdx, int player) {
		if (oldIdx == newIdx) return;
		if (oldIdx >= 0)
			playerResourceRefCounts[player] = Characters[oldIdx][player].CharaUnload();
		if (newIdx >= 0) {
			Characters[newIdx][player].CharaLoad(playerResourceRefCounts[player]);
			playerResourceRefCounts[player] = null;
		}
	}

	public void DisposeTexture() {
		// concurrent
		var listTexture = Interlocked.Exchange(ref this.listTexture, []);
		for (int i = 0; i < listTexture.Count; ++i)
			listTexture.ElementAtOrDefault(i)?.Dispose();
		listTexture.Clear();
		gameplayTextures.Clear();
		_warmNeeds.Clear();
		_notesSkinTextures.Clear();

		foreach (var character in Characters) {
			character.Dispose();
		}

		LuaPuchicharaDb?.Dispose();
		LuaPuchicharaDb = null;

		LuaCharacterDb?.Dispose();
		LuaCharacterDb = null;

		//if (TJAPlayer3.ConfigIni.PreAssetsLoading)
		{
			foreach (var act in OpenTaiko.app.listTopLevelActivities) {
				act.ReleaseManagedResource();
				act.ReleaseUnmanagedResource();
			}
		}
	}
	#region Calibration

	public CTexture? CalibrateFG,
		CalibrateBG;

	#endregion

	#region 共通
	public CTexture? Tile_Black,
		Menu_Title,
		Menu_Highlight,
		Loading,
		Scanning_Loudness,
		NamePlateBase,
		Overlay;

	#endregion


	#region 3_段位選択画面 (textures still used by result/gameplay stages)

	public CTexture? Dani_Difficulty_Cymbol;
	public CTexture? Dani_Level_Number;
	public CTexture? Dani_DanIcon;
	public CTexture? Dani_DanIcon_Fade;

	#endregion

	#region 5_演奏画面
	#region 共通
	public CTexture? Note_Mine;
	public CTexture? Note_Swap,       // the former layout's purple, ADLib, kusudama and fuse roll sheets (null with a notes pack)
		Note_Kusu,
		Note_FuseRoll,
		Note_Adlib,
		Balloons;        // a Taiko notes pack's Balloons.png
	public CTexture? SENotesExtension,
		Notes_Arm,
		ScoreRank;
	public CTexture? Judge;
	public CTexture? Judge_Meter,
		Bar,
		Bar_Branch;
	// per game type (Taiko, Konga)
	public CTexture?[] Notes = null!,
		Rolls = null!,           // a notes pack's Rolls.png (null in the former layout, where Notes.png holds the rolls)
		HitCircle = null!,
		ChipEffect = null!,
		Notes_Partner = null!,
		Notes_Giant = null!;
	public CTexture?[] SENotes = null!;
	#endregion


	#region モブ
	#endregion
	#region 太鼓
	public CTexture?[] Taiko_Base = null!,
		Taiko_Frame = null!, // MTaiko下敷き
		Taiko_Background = null!;
	public CTexture? Taiko_Don_Left,
		Taiko_Don_Right,
		Taiko_Ka_Left,
		Taiko_Ka_Right,
		Taiko_Konga_Don,
		Taiko_Konga_Ka,
		Taiko_Konga_Clap,
		Taiko_LevelUp,
		Taiko_LevelDown,
		Taiko_Combo_Effect,
		Taiko_Combo_Text;
	public CTexture?[] Couse_Symbol = null!, // コースシンボル
		Couse_Symbol_Back = null!,
		Couse_Symbol_Back_Flash = null!,
		Taiko_PlayerNumber = null!;
	public CTexture?[] Taiko_Score = null!,
		Taiko_Combo = null!,
		Taiko_Combo_Guide = null!;
	#endregion
	#region ゲージ
	public CTexture?[] Gauge = null!,
		Gauge_Base = null!,
		Gauge_Base_Norma = null!,
		Gauge_Line = null!,
		Gauge_Rainbow = null!,
		Gauge_Rainbow_2PGauge = null!,
		Gauge_Rainbow_Flat = null!,
		Gauge_Clear = null!,
		Gauge_Killzone = null!,
		Gauge_Soul_Explosion = null!;
	public CTexture? Gauge_Soul,
		Gauge_Soul_Fire;
	public CTexture?[] Gauge_Dan = null!;
	public CTexture?[] Gauge_Dan_Rainbow = null!;
	#endregion
	#region 吹き出し
	public CTexture?[] Balloon_Combo = null!;
	public CTexture? Balloon_Roll,
		Balloon_Balloon;
	public CTexture? Balloon_Number_Roll,
		Balloon_Number_Combo/*,*/
		/*Balloon_Broken*/;
	public CTexture? Kusudama_Number;

	public CTexture? Fuse_Number,
		Fuse_Balloon;

	public CTexture?[] Balloon_Breaking = null!;
	public CTexture? Balloon_Breaking_Sheet;   // a notes pack's Breaking.png: the 6 pictures one above the other
	#endregion
	#region エフェクト
	public CTexture? Effects_Hit_Explosion,
		Effects_Hit_Bomb,
		Effects_Hit_Explosion_Big;
	public CTexture? Effects_Fire,
		Effects_Rainbow,
		Effects_GoGoSplash;
	public CTexture? Effects_Hit_FireWorks;
	public CTexture?[] Effects_Hit_Great = null!,
		Effects_Hit_Good = null!,
		Effects_Hit_Great_Big = null!,
		Effects_Hit_Good_Big = null!;
	public CTexture?[] Effects_Roll = null!;
	#endregion
	#region レーン
	public CTexture?[] Lane_Red = null!,
		Lane_Blue = null!,
		Lane_Clap = null!,
		Lane_Base = null!,
		Lane_Text = null!;
	public CTexture? Lane_Yellow;
	public CTexture? Lane_Background_Main,
		Lane_Background_AI,
		Lane_Background_Sub,
		Lane_Background_GoGo;
	#endregion
	#region ゲームモード
	public CTexture? GameMode_Timer_Frame;
	#endregion
	#region ランナー
	//public CTexture Runner;
	#endregion
	#region DanC
	public CTexture? DanC_Background;
	public CTexture?[] DanC_Gauge = null!;
	public CTexture? DanC_Base;
	public CTexture? DanC_Base_Small;


	public CTexture? DanC_Gauge_Base;
	public CTexture? DanC_Failed;
	public CTexture? DanC_Number,
		DanC_Small_Number,
		DanC_SmallBase,
		DanC_ExamType,
		DanC_ExamRange,
		DanC_Small_ExamCymbol,
		DanC_ExamCymbol,
		DanC_MiniNumber;
	public CTexture? DanC_Screen;
	#endregion
	#region PuchiChara

	public CPuchichara[] Puchichara = null!;
	public LuaPuchicharaDatabase? LuaPuchicharaDb;

	#endregion
	#region Training
	public CTexture? Tokkun_DownBG,
		Tokkun_BigTaiko,
		Tokkun_ProgressBar,
		Tokkun_ProgressBarWhite,
		Tokkun_GoGoPoint,
		Tokkun_JumpPoint,
		Tokkun_Background_Up,
		Tokkun_BigNumber,
		Tokkun_SmallNumber,
		Tokkun_Speed_Measure;
	#endregion

	#region [20_Tower]

	public CTexture? Tower_Miss;

	#endregion

	#region [22_AIBattle]

	public CTexture? AIBattle_SectionTime_Panel,
		AIBattle_SectionTime_Bar_Base,
		AIBattle_SectionTime_Bar_Normal,
		AIBattle_SectionTime_Bar_Finish,
		AIBattle_Batch_Base,
		AIBattle_Batch,
		AIBattle_Judge_Number;

	public CTexture?[] AIBattle_Judge_Meter = new CTexture?[2];

	#endregion


	#endregion

	#region 6_結果発表
	public CTexture? Result_Number;
	#endregion

	#region 7_終了画面
	//public CTexture Exit_Background/* , */
	/*Exit_Text; */
	#endregion

	#region [11_Characters]

	/*
	public CTexture?[][] Characters_Normal,
		Characters_Normal_Missed,
		Characters_Normal_MissedDown,
		Characters_Normal_Cleared,
		Characters_Normal_Maxed,
		Characters_MissIn,
		Characters_MissDownIn,
		Characters_GoGoTime,
		Characters_GoGoTime_Maxed,
		Characters_10Combo,
		Characters_10Combo_Clear,
		Characters_10Combo_Maxed,
		Characters_GoGoStart,
		Characters_GoGoStart_Clear,
		Characters_GoGoStart_Maxed,
		Characters_Become_Cleared,
		Characters_Become_Maxed,
		Characters_SoulOut,
		Characters_ClearOut,
		Characters_Return,
		Characters_Balloon_Breaking,
		Characters_Balloon_Broke,
		Characters_Balloon_Miss,
		Characters_Kusudama_Idle,
		Characters_Kusudama_Breaking,
		Characters_Kusudama_Broke,
		Characters_Kusudama_Miss,
		Characters_Title_Entry,
		Characters_Title_Normal,
		Characters_Menu_Loop,
		Characters_Menu_Select,
		Characters_Menu_Start,
		Characters_Menu_Wait,
		Characters_Result_Clear,
		Characters_Result_Failed,
		Characters_Result_Failed_In,
		Characters_Result_Normal,
		Characters_Tower_Standing,
		Characters_Tower_Climbing,
		Characters_Tower_Running,
		Characters_Tower_Clear,
		Characters_Tower_Fail,
		Characters_Tower_Standing_Tired,
		Characters_Tower_Climbing_Tired,
		Characters_Tower_Running_Tired,
		Characters_Tower_Clear_Tired;
	public CTexture?[] Characters_Result_Clear_1P,
		Characters_Result_Failed_1P,
		Characters_Result_Clear_2P,
		Characters_Result_Failed_2P;
	*/

	// One character folder; its per-player instances (each a Lua VM running the character script) are created
	// on first use. Creating all of them up front cost the boot ~150 ms per character (5 VMs each) for
	// characters that are mostly never selected.
	public class CCharacterLuaSet : CCharacter.Info, IDisposable {
		private readonly CCharacterLua?[] instances = new CCharacterLua?[OpenTaiko.MAX_PLAYERS]; // [iPlayer]
		public CCharacter.Info info => this;
		public CCharacterLua this[int p] {
			get {
				var inst = instances[p];
				if (inst != null) return inst;
				lock (instances) return instances[p] ??= new CCharacterLua(this);
			}
		}
		public CCharacterLua Preview => this[0]; // use P1's instance for preview

		public CCharacterLuaSet(string path, int i) : base(path, i) { }

		public void Dispose() {
			for (int p = 0; p < instances.Length; p++) {
				instances[p]?.Dispose();
				instances[p] = null;
			}
		}
	}

	public CCharacterLuaSet[] Characters = [];
	public LuaCharacter[] PlayerCharacters = new LuaCharacter[5];
	public LuaCharacterDatabase? LuaCharacterDb;

	private CCharacter.ResourceRefCounts?[] playerResourceRefCounts = new CCharacter.ResourceRefCounts?[OpenTaiko.MAX_PLAYERS];
	#endregion

	#region [12_OnlineLounge]

	public CTexture?
		//OnlineLounge_Background,
		OnlineLounge_Box,
		OnlineLounge_Side_Menu,
		OnlineLounge_Context,
		OnlineLounge_Return_Box,
		OnlineLounge_Song_Box;

	#endregion

	#region [ 解放用 ]
	public List<CTexture?> listTexture = new List<CTexture?>();
	#endregion

}
