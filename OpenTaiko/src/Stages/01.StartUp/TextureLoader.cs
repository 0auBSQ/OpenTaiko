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
	public static string EXIT = @$"7_Exit{Path.DirectorySeparatorChar}";
	public static string DANRESULT = @$"7_DanResult{Path.DirectorySeparatorChar}";
	public static string TOWERRESULT = @$"8_TowerResult{Path.DirectorySeparatorChar}";
	public static string HEYA = @$"10_Heya{Path.DirectorySeparatorChar}";

	public static string MODALS = @$"11_Modals{Path.DirectorySeparatorChar}";
	public static string ONLINELOUNGE = @$"12_OnlineLounge{Path.DirectorySeparatorChar}";
	public static string TOWERSELECT = @$"13_TowerSelect{Path.DirectorySeparatorChar}";

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

	// Tower infos
	public static string TOWERDON = @$"Tower_Don{Path.DirectorySeparatorChar}";
	public static string TOWERFLOOR = @$"Tower_Floors{Path.DirectorySeparatorChar}";

	// InGame_Effects
	public static string FIRE = @$"Fire{Path.DirectorySeparatorChar}";
	public static string HIT = @$"Hit{Path.DirectorySeparatorChar}";
	public static string ROLL = @$"Roll{Path.DirectorySeparatorChar}";
	public static string SPLASH = @$"Splash{Path.DirectorySeparatorChar}";

	public Dictionary<string, CTexture> trackedTextures = new Dictionary<string, CTexture>();

	public TextureLoader() {
		// Constructor
	}

	internal CTexture TxC(string FileName, bool localize = true) {
		tTickTextureProgress();
		var texpath = (localize) ? HLocalizedPath.GetAvailableLocalizedPath(CSkin.Path(BASE + FileName)) : CSkin.Path(BASE + FileName);
		var tex = OpenTaiko.tTextureCreate(texpath, false);

		listTexture.Add(tex);
		return tex;
	}

	internal CTexture TxCGlobal(string FileName) {
		tTickTextureProgress();
		var tex = OpenTaiko.tTextureCreate(OpenTaiko.strEXEFolder + GLOBAL + FileName, false);
		listTexture.Add(tex);
		return tex;
	}

	internal CTexture TxCAbsolute(string FileName) {
		tTickTextureProgress();
		var tex = OpenTaiko.tTextureCreate(FileName, false);
		listTexture.Add(tex);
		return tex;
	}

	internal CTextureAf TxCAf(string FileName) {
		tTickTextureProgress();
		var tex = OpenTaiko.tTextureCreateAf(CSkin.Path(BASE + FileName));
		listTexture.Add(tex);
		return tex;
	}
	internal CTexture TxCGen(string FileName) {
		tTickTextureProgress();
		return OpenTaiko.tTextureCreate(CSkin.Path(BASE + GAME + GENRE + FileName + ".png"), false);
	}

	internal CTexture TxCSong(string path) {
		return TxCUntrackedSong(path);
	}

	private CTexture[] TxCSong(int count, string format, int start = 0) {
		return TxCSong(format, Enumerable.Range(start, count).Select(o => o.ToString()).ToArray());
	}

	private CTexture[] TxCSong(string format, params string[] parts) {
		return parts.Select(o => TxCSong(string.Format(format, o))).ToArray();
	}

	public CTexture[] TxCSongFolder(string folder) {
		// Match .png case-insensitively so mixed-case frame names work on case-sensitive filesystems (iOS).
		if (!Directory.Exists(folder)) return null;
		var byIndex = new Dictionary<int, string>();
		foreach (var file in Directory.EnumerateFiles(folder, "*.png", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive })) {
			if (int.TryParse(Path.GetFileNameWithoutExtension(file), out int n))
				byIndex[n] = file;
		}
		var textures = new List<CTexture>();
		for (int i = 0; byIndex.TryGetValue(i, out string path); i++)
			textures.Add(TxCSong(path));
		return textures.Count == 0 ? null : textures.ToArray();
	}

	internal CTexture TxCUntrackedSong(string path) {
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

		#region General

		Notes = new CTexture[2];
		Notes[0] = TxC(GAME + @$"Notes.png");
		Notes[1] = TxC(GAME + @$"Notes_Konga.png");

		Note_Mine = TxC(GAME + @$"Mine.png");
		Note_Swap = TxC(GAME + @$"Swap.png");
		Note_Kusu = TxC(GAME + @$"Kusu.png");
		Note_FuseRoll = TxC(GAME + @$"FuseRoll.png");
		Note_Adlib = TxC(GAME + @$"Adlib.png");

		Judge_Frame = TxC(GAME + @$"Notes.png");

		SENotes = new CTexture[2];
		SENotes[0] = TxC(GAME + @$"SENotes.png");
		SENotes[1] = TxC(GAME + @$"SENotes_Konga.png");

		SENotesExtension = TxC(GAME + @$"SENotes_Extension.png");

		Notes_Arm = TxC(GAME + @$"Notes_Arm.png");
		Judge = TxC(GAME + @$"Judge.png");
		ChipEffect = TxC(GAME + @$"ChipEffect.png");
		ScoreRank = TxC(GAME + @$"ScoreRank.png");

		Judge_Meter = TxC(GAME + @$"Judge_Meter.png");
		Bar = TxC(GAME + @$"Bar.png");
		Bar_Branch = TxC(GAME + @$"Bar_Branch.png");

		Vector2 judgeDiff = new(OpenTaiko.Skin.Game_Judge_X[1] - OpenTaiko.Skin.Game_Judge_X[0], OpenTaiko.Skin.Game_Judge_Y[1] - OpenTaiko.Skin.Game_Judge_Y[0]);
		OpenTaiko.Skin.Init_Game_Notes_Arm_Configs(CSkin.ToVector2(Notes_Arm.szTextureSize), CSkin.ToVector2(OpenTaiko.Skin.Game_Notes_Size), judgeDiff,
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
		Taiko_Background[1] = TxC(GAME + TAIKO + @$"2P_Background.png");
		Taiko_Background[2] = TxC(GAME + TAIKO + @$"Dan_Background.png");
		Taiko_Background[3] = TxC(GAME + TAIKO + @$"Tower_Background.png");
		Taiko_Background[4] = TxC(GAME + TAIKO + @$"1P_Background_Right.png");
		Taiko_Background[5] = TxC(GAME + TAIKO + @$"1P_Background_Tokkun.png");
		Taiko_Background[6] = TxC(GAME + TAIKO + @$"2P_Background_Tokkun.png");
		Taiko_Background[7] = TxC(GAME + TAIKO + @$"3P_Background.png");
		Taiko_Background[8] = TxC(GAME + TAIKO + @$"4P_Background.png");
		Taiko_Background[9] = TxC(GAME + TAIKO + @$"AI_Background.png");
		Taiko_Background[10] = TxC(GAME + TAIKO + @$"Boss_Background.png");
		Taiko_Background[11] = TxC(GAME + TAIKO + @$"5P_Background.png");

		Taiko_Frame = new CTexture[7];
		Taiko_Frame[0] = TxC(GAME + TAIKO + @$"1P_Frame.png");
		Taiko_Frame[1] = TxC(GAME + TAIKO + @$"2P_Frame.png");
		Taiko_Frame[2] = TxC(GAME + TAIKO + @$"Tower_Frame.png");
		Taiko_Frame[3] = TxC(GAME + TAIKO + @$"Tokkun_Frame.png");
		Taiko_Frame[4] = TxC(GAME + TAIKO + @$"2P_None_Frame.png");
		Taiko_Frame[5] = TxC(GAME + TAIKO + @$"AI_Frame.png");
		Taiko_Frame[6] = TxC(GAME + TAIKO + @$"4PPlay_Frame.png");

		Taiko_PlayerNumber = new CTexture[5];
		Taiko_PlayerNumber[0] = TxC(GAME + TAIKO + @$"1P_PlayerNumber.png");
		Taiko_PlayerNumber[1] = TxC(GAME + TAIKO + @$"2P_PlayerNumber.png");
		Taiko_PlayerNumber[2] = TxC(GAME + TAIKO + @$"3P_PlayerNumber.png");
		Taiko_PlayerNumber[3] = TxC(GAME + TAIKO + @$"4P_PlayerNumber.png");
		Taiko_PlayerNumber[4] = TxC(GAME + TAIKO + @$"5P_PlayerNumber.png");


		Taiko_Base = new CTexture[2];
		Taiko_Base[0] = TxC(GAME + TAIKO + @$"Base.png");
		Taiko_Base[1] = TxC(GAME + TAIKO + @$"Base_Konga.png");

		Taiko_Don_Left = TxC(GAME + TAIKO + @$"Don.png");
		Taiko_Don_Right = TxC(GAME + TAIKO + @$"Don.png");
		Taiko_Ka_Left = TxC(GAME + TAIKO + @$"Ka.png");
		Taiko_Ka_Right = TxC(GAME + TAIKO + @$"Ka.png");

		Taiko_Konga_Don = TxC(GAME + TAIKO + @$"Don_Konga.png");
		Taiko_Konga_Ka = TxC(GAME + TAIKO + @$"Ka_Konga.png");
		Taiko_Konga_Clap = TxC(GAME + TAIKO + @$"Clap.png");

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
		Gauge[1] = TxC(GAME + GAUGE + @$"2P.png");
		Gauge[2] = TxC(GAME + GAUGE + @$"1P_Right.png");
		Gauge[3] = TxC(GAME + GAUGE + @$"1P_4PGauge.png");
		Gauge[4] = TxC(GAME + GAUGE + @$"2P_4PGauge.png");
		Gauge[5] = TxC(GAME + GAUGE + @$"3P_4PGauge.png");
		Gauge[6] = TxC(GAME + GAUGE + @$"4P_4PGauge.png");
		Gauge[7] = TxC(GAME + GAUGE + @$"5P_4PGauge.png");

		Gauge_Base = new CTexture[8];
		Gauge_Base[0] = TxC(GAME + GAUGE + @$"1P_Base.png");
		Gauge_Base[1] = TxC(GAME + GAUGE + @$"2P_Base.png");
		Gauge_Base[2] = TxC(GAME + GAUGE + @$"1P_Base_Right.png");
		Gauge_Base[3] = TxC(GAME + GAUGE + @$"1P_Base_4PGauge.png");
		Gauge_Base[4] = TxC(GAME + GAUGE + @$"2P_Base_4PGauge.png");
		Gauge_Base[5] = TxC(GAME + GAUGE + @$"3P_Base_4PGauge.png");
		Gauge_Base[6] = TxC(GAME + GAUGE + @$"4P_Base_4PGauge.png");
		Gauge_Base[7] = TxC(GAME + GAUGE + @$"5P_Base_4PGauge.png");

		Gauge_Line = new CTexture[2];
		Gauge_Line[0] = TxC(GAME + GAUGE + @$"1P_Line.png");
		Gauge_Line[1] = TxC(GAME + GAUGE + @$"2P_Line.png");

		Gauge_Clear = new CTexture[3];
		Gauge_Clear[0] = TxC(GAME + GAUGE + @$"Clear.png");
		Gauge_Clear[1] = TxC(GAME + GAUGE + @$"Clear_2PGauge.png");
		Gauge_Clear[2] = TxC(GAME + GAUGE + @$"Clear_4PGauge.png");

		Gauge_Base_Norma = new CTexture[3];
		Gauge_Base_Norma[0] = TxC(GAME + GAUGE + @$"Norma_Base.png");
		Gauge_Base_Norma[1] = TxC(GAME + GAUGE + @$"Norma_Base_2PGauge.png");
		Gauge_Base_Norma[2] = TxC(GAME + GAUGE + @$"Norma_Base_4PGauge.png");

		Gauge_Killzone = new CTexture[3];
		Gauge_Killzone[0] = TxC(GAME + GAUGE + @$"Killzone.png");
		Gauge_Killzone[1] = TxC(GAME + GAUGE + @$"Killzone_2PGauge.png");
		Gauge_Killzone[2] = TxC(GAME + GAUGE + @$"Killzone_4PGauge.png");

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
				Gauge_Rainbow_Flat[i] = TxC(GAME + GAUGE + @$"Rainbow_Flat{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			}
		}

		OpenTaiko.Skin.Game_Gauge_Rainbow_2PGauge_Ptn = OpenTaiko.tSequenceImageSheetCountCount(CSkin.Path(BASE + GAME + GAUGE + @$"Rainbow_2PGauge{Path.DirectorySeparatorChar}"));
		if (OpenTaiko.Skin.Game_Gauge_Rainbow_2PGauge_Ptn != 0) {
			Gauge_Rainbow_2PGauge = new CTexture[OpenTaiko.Skin.Game_Gauge_Rainbow_2PGauge_Ptn];
			for (int i = 0; i < OpenTaiko.Skin.Game_Gauge_Rainbow_2PGauge_Ptn; i++) {
				Gauge_Rainbow_2PGauge[i] = TxC(GAME + GAUGE + @$"Rainbow_2PGauge{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			}
		}

		// Dan

		OpenTaiko.Skin.Game_Gauge_Dan_Rainbow_Ptn = OpenTaiko.tSequenceImageSheetCountCount(CSkin.Path(BASE + GAME + DANC + @$"Rainbow{Path.DirectorySeparatorChar}"));
		if (OpenTaiko.Skin.Game_Gauge_Dan_Rainbow_Ptn != 0) {
			Gauge_Dan_Rainbow = new CTexture[OpenTaiko.Skin.Game_Gauge_Dan_Rainbow_Ptn];
			for (int i = 0; i < OpenTaiko.Skin.Game_Gauge_Dan_Rainbow_Ptn; i++) {
				Gauge_Dan_Rainbow[i] = TxC(GAME + DANC + @$"Rainbow{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			}
		}

		Gauge_Dan = new CTexture[6];

		Gauge_Dan[0] = TxC(GAME + GAUGE + @$"1P_Dan_Base.png");
		Gauge_Dan[1] = TxC(GAME + GAUGE + @$"1P_Dan.png");
		Gauge_Dan[2] = TxC(GAME + GAUGE + @$"1P_Dan_Clear_Base.png");
		Gauge_Dan[3] = TxC(GAME + GAUGE + @$"1P_Dan_Clear.png");
		Gauge_Dan[4] = TxC(GAME + GAUGE + @$"1P_Dan_Base_Right.png");
		Gauge_Dan[5] = TxC(GAME + GAUGE + @$"1P_Dan_Right.png");

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
		Balloon_Roll = TxC(GAME + BALLOON + @$"Roll.png");
		Balloon_Balloon = TxC(GAME + BALLOON + @$"Balloon.png");
		Balloon_Number_Roll = TxC(GAME + BALLOON + @$"Number_Roll.png");
		Balloon_Number_Combo = TxC(GAME + BALLOON + @$"Number_Combo.png");

		Balloon_Breaking = new CTexture[6];
		for (int i = 0; i < 6; i++) {
			Balloon_Breaking[i] = TxC(GAME + BALLOON + @$"Breaking_" + i.ToString() + ".png");
		}

		Kusudama_Number = TxC(GAME + BALLOON + KUSUDAMA + @$"Kusudama_Number.png");

		Fuse_Number = TxC(GAME + BALLOON + FUSE + @$"Number_Fuse.png");
		Fuse_Balloon = TxC(GAME + BALLOON + FUSE + @$"Fuse.png");

		#endregion

		#region Effects

		Effects_Hit_Explosion = TxCAf(GAME + EFFECTS + @$"Hit{Path.DirectorySeparatorChar}Explosion.png");
		if (Effects_Hit_Explosion != null) Effects_Hit_Explosion.bAddBlend = OpenTaiko.Skin.Game_Effect_HitExplosion_AddBlend;
		Effects_Hit_Explosion_Big = TxC(GAME + EFFECTS + @$"Hit{Path.DirectorySeparatorChar}Explosion_Big.png");
		if (Effects_Hit_Explosion_Big != null) Effects_Hit_Explosion_Big.bAddBlend = OpenTaiko.Skin.Game_Effect_HitExplosionBig_AddBlend;
		Effects_Hit_FireWorks = TxC(GAME + EFFECTS + @$"Hit{Path.DirectorySeparatorChar}FireWorks.png");
		if (Effects_Hit_FireWorks != null) Effects_Hit_FireWorks.bAddBlend = OpenTaiko.Skin.Game_Effect_FireWorks_AddBlend;

		Effects_Hit_Bomb = TxCAf(GAME + EFFECTS + @$"Hit{Path.DirectorySeparatorChar}Bomb.png");


		Effects_Fire = TxC(GAME + EFFECTS + @$"Fire.png");
		if (Effects_Fire != null) Effects_Fire.bAddBlend = OpenTaiko.Skin.Game_Effect_Fire_AddBlend;

		Effects_Rainbow = TxC(GAME + EFFECTS + @$"Rainbow.png");

		Effects_GoGoSplash = TxC(GAME + EFFECTS + @$"GoGoSplash.png");
		if (Effects_GoGoSplash != null) Effects_GoGoSplash.bAddBlend = OpenTaiko.Skin.Game_Effect_GoGoSplash_AddBlend;
		Effects_Hit_Great = new CTexture[15];
		Effects_Hit_Great_Big = new CTexture[15];
		Effects_Hit_Good = new CTexture[15];
		Effects_Hit_Good_Big = new CTexture[15];
		for (int i = 0; i < 15; i++) {
			Effects_Hit_Great[i] = TxC(GAME + EFFECTS + @$"Hit{Path.DirectorySeparatorChar}" + @$"Great{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			Effects_Hit_Great_Big[i] = TxC(GAME + EFFECTS + @$"Hit{Path.DirectorySeparatorChar}" + @$"Great_Big{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			Effects_Hit_Good[i] = TxC(GAME + EFFECTS + @$"Hit{Path.DirectorySeparatorChar}" + @$"Good{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			Effects_Hit_Good_Big[i] = TxC(GAME + EFFECTS + @$"Hit{Path.DirectorySeparatorChar}" + @$"Good_Big{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
		}
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
			Lane_Red[i] = TxC(GAME + LANE + @$"Red" + _suffixes[i] + @$".png");
			Lane_Blue[i] = TxC(GAME + LANE + @$"Blue" + _suffixes[i] + @$".png");
			Lane_Clap[i] = TxC(GAME + LANE + @$"Clap" + _suffixes[i] + @$".png");
		}


		Lane_Yellow = TxC(GAME + LANE + @$"Yellow.png");
		Lane_Background_Main = TxC(GAME + LANE + @$"Background_Main.png");
		Lane_Background_AI = TxC(GAME + LANE + @$"Background_AI.png");
		Lane_Background_Sub = TxC(GAME + LANE + @$"Background_Sub.png");
		Lane_Background_GoGo = TxC(GAME + LANE + @$"Background_GoGo.png");

		#endregion

		#region GameMode

		GameMode_Timer_Tick = TxC(GAME + GAMEMODE + @$"Timer_Tick.png");
		GameMode_Timer_Frame = TxC(GAME + GAMEMODE + @$"Timer_Frame.png");

		#endregion

		#region DanC

		DanC_Background = TxC(GAME + DANC + @$"Background.png");
		DanC_Gauge = new CTexture[4];
		var type = new string[] { "Normal", "Reach", "Clear", "Flush" };
		for (int i = 0; i < 4; i++) {
			DanC_Gauge[i] = TxC(GAME + DANC + @$"Gauge_" + type[i] + ".png");
		}
		DanC_Base = TxC(GAME + DANC + @$"Base.png");
		DanC_Base_Small = TxC(GAME + DANC + @$"Base_Small.png");

		DanC_Gauge_Base = TxC(GAME + DANC + @$"Gauge_Base.png");
		DanC_Failed = TxC(GAME + DANC + @$"Failed.png");
		DanC_Number = TxC(GAME + DANC + @$"Number.png");
		DanC_Small_Number = TxC(GAME + DANC + @$"Small_Number.png");
		DanC_ExamType = TxC(GAME + DANC + @$"ExamType.png");
		DanC_ExamRange = TxC(GAME + DANC + @$"ExamRange.png");
		DanC_Screen = TxC(GAME + DANC + @$"Screen.png");
		DanC_SmallBase = TxC(GAME + DANC + @$"SmallBase.png");
		DanC_Small_ExamCymbol = TxC(GAME + DANC + @$"Small_ExamCymbol.png");
		DanC_ExamCymbol = TxC(GAME + DANC + @$"ExamCymbol.png");
		DanC_MiniNumber = TxC(GAME + DANC + @$"MiniNumber.png");

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

		Tokkun_DownBG = TxC(GAME + TRAINING + @$"Down.png");
		Tokkun_BigTaiko = TxC(GAME + TRAINING + @$"BigTaiko.png");
		Tokkun_ProgressBar = TxC(GAME + TRAINING + @$"ProgressBar_Red.png");
		Tokkun_ProgressBarWhite = TxC(GAME + TRAINING + @$"ProgressBar_White.png");
		Tokkun_GoGoPoint = TxC(GAME + TRAINING + @$"GoGoPoint.png");
		Tokkun_JumpPoint = TxC(GAME + TRAINING + @$"JumpPoint.png");
		Tokkun_Background_Up = TxC(GAME + TRAINING + @$"Background_Up.png");
		Tokkun_BigNumber = TxC(GAME + TRAINING + @$"BigNumber.png");
		Tokkun_SmallNumber = TxC(GAME + TRAINING + @$"SmallNumber.png");
		Tokkun_Speed_Measure = TxC(GAME + TRAINING + @$"Speed_Measure.png");

		#endregion

		#region [20_Tower]

		Tower_Miss = TxC(GAME + TOWER + @$"Miss.png");

		// Tower elements
		string[] towerDirectories = System.IO.Directory.GetDirectories(CSkin.Path(BASE + GAME + TOWER + TOWERFLOOR));
		OpenTaiko.Skin.Game_Tower_Ptn = towerDirectories.Length;
		OpenTaiko.Skin.Game_Tower_Names = new string[OpenTaiko.Skin.Game_Tower_Ptn];
		for (int i = 0; i < OpenTaiko.Skin.Game_Tower_Ptn; i++)
			OpenTaiko.Skin.Game_Tower_Names[i] = new DirectoryInfo(towerDirectories[i]).Name;
		Tower_Top = new CTexture[OpenTaiko.Skin.Game_Tower_Ptn];
		Tower_Base = new CTexture[OpenTaiko.Skin.Game_Tower_Ptn][];
		Tower_Deco = new CTexture[OpenTaiko.Skin.Game_Tower_Ptn][];

		OpenTaiko.Skin.Game_Tower_Ptn_Base = new int[OpenTaiko.Skin.Game_Tower_Ptn];
		OpenTaiko.Skin.Game_Tower_Ptn_Deco = new int[OpenTaiko.Skin.Game_Tower_Ptn];

		for (int i = 0; i < OpenTaiko.Skin.Game_Tower_Ptn; i++) {
			OpenTaiko.Skin.Game_Tower_Ptn_Base[i] = OpenTaiko.tSequenceImageSheetCountCount((towerDirectories[i] + @$"{Path.DirectorySeparatorChar}Base{Path.DirectorySeparatorChar}"), "Base");
			OpenTaiko.Skin.Game_Tower_Ptn_Deco[i] = OpenTaiko.tSequenceImageSheetCountCount((towerDirectories[i] + @$"{Path.DirectorySeparatorChar}Deco{Path.DirectorySeparatorChar}"), "Deco");

			Tower_Top[i] = TxC(GAME + TOWER + TOWERFLOOR + OpenTaiko.Skin.Game_Tower_Names[i] + @$"{Path.DirectorySeparatorChar}Top.png");

			Tower_Base[i] = new CTexture[OpenTaiko.Skin.Game_Tower_Ptn_Base[i]];
			Tower_Deco[i] = new CTexture[OpenTaiko.Skin.Game_Tower_Ptn_Deco[i]];

			for (int j = 0; j < OpenTaiko.Skin.Game_Tower_Ptn_Base[i]; j++) {
				Tower_Base[i][j] = TxC(GAME + TOWER + TOWERFLOOR + OpenTaiko.Skin.Game_Tower_Names[i] + @$"{Path.DirectorySeparatorChar}Base{Path.DirectorySeparatorChar}Base" + j.ToString() + ".png");
			}

			for (int j = 0; j < OpenTaiko.Skin.Game_Tower_Ptn_Deco[i]; j++) {
				Tower_Deco[i][j] = TxC(GAME + TOWER + TOWERFLOOR + OpenTaiko.Skin.Game_Tower_Names[i] + @$"{Path.DirectorySeparatorChar}Deco{Path.DirectorySeparatorChar}Deco" + j.ToString() + ".png");
			}
		}



		#endregion

		#region [22_AIBattle]

		AIBattle_SectionTime_Panel = TxC(GAME + AIBATTLE + @$"SectionTime_Panel.png");

		AIBattle_SectionTime_Bar_Base = TxC(GAME + AIBATTLE + @$"SectionTime_Bar_Base.png");
		AIBattle_SectionTime_Bar_Finish = TxC(GAME + AIBATTLE + @$"SectionTime_Bar_Finish.png");
		AIBattle_SectionTime_Bar_Normal = TxC(GAME + AIBATTLE + @$"SectionTime_Bar_Normal.png");

		AIBattle_Batch_Base = TxC(GAME + AIBATTLE + @$"Batch_Base.png");
		AIBattle_Batch = TxC(GAME + AIBATTLE + @$"Batch.png");

		AIBattle_Judge_Meter[0] = TxC(GAME + AIBATTLE + @$"Judge_Meter.png");
		AIBattle_Judge_Meter[1] = TxC(GAME + AIBATTLE + @$"Judge_Meter_AI.png");

		AIBattle_Judge_Number = TxC(GAME + AIBATTLE + @$"Judge_Number.png");

		#endregion

		#endregion

		#region 6_結果発表

		Result_Gauge[0] = TxC(RESULT + @$"Gauge.png");
		Result_Gauge_Base[0] = TxC(RESULT + @$"Gauge_Base.png");
		Result_Gauge[1] = TxC(RESULT + @$"Gauge_2.png");
		Result_Gauge_Base[1] = TxC(RESULT + @$"Gauge_Base_2.png");
		Result_Gauge[2] = TxC(RESULT + @$"Gauge_3.png");
		Result_Gauge_Base[2] = TxC(RESULT + @$"Gauge_Base_3.png");
		Result_Gauge[3] = TxC(RESULT + @$"Gauge_4.png");
		Result_Gauge_Base[3] = TxC(RESULT + @$"Gauge_Base_4.png");
		Result_Gauge[4] = TxC(RESULT + @$"Gauge_5.png");
		Result_Gauge_Base[4] = TxC(RESULT + @$"Gauge_Base_5.png");

		Result_Gauge_Frame = TxC(RESULT + @$"Gauge_Frame.png");
		Result_Gauge_Clear = TxC(RESULT + @$"Gauge_Clear.png");
		Result_Gauge_Clear_Base = TxC(RESULT + @$"Gauge_Clear_Base.png");
		Result_Gauge_Killzone = TxC(RESULT + @$"Gauge_Killzone.png");

		Result_Header = TxC(RESULT + @$"Header.png");
		Result_Number = TxC(RESULT + @$"Number.png");
		Result_Panel = TxC(RESULT + @$"Panel.png");
		Result_Panel_2P = TxC(RESULT + @$"Panel_2.png");
		Result_Soul_Text = TxC(RESULT + @$"Soul_Text.png");
		Result_Soul_Fire = TxC(RESULT + @$"Result_Soul_Fire.png");
		Result_Diff_Bar = TxC(RESULT + @$"DifficultyBar.png");
		Result_Score_Number = TxC(RESULT + @$"Score_Number.png");

		Result_CrownEffect = TxC(RESULT + @$"CrownEffect.png");
		Result_ScoreRankEffect = TxC(RESULT + @$"ScoreRankEffect.png");
		//Result_Cloud = TxC(RESULT + @$"Cloud.png");
		Result_Shine = TxC(RESULT + @$"Shine.png");

		Result_Speech_Bubble[0] = TxC(RESULT + @$"Speech_Bubble.png");
		Result_Speech_Bubble[1] = TxC(RESULT + @$"Speech_Bubble_2.png");

		Result_Speech_Bubble_V2[0] = TxC(RESULT + @$"Speech_Bubble_V2_Left.png");
		Result_Speech_Bubble_V2[1] = TxC(RESULT + @$"Speech_Bubble_V2_Right.png");
		Result_Speech_Bubble_V2[2] = TxC(RESULT + @$"Speech_Bubble_V2_4P_5P.png");

		Result_Flower = TxC(RESULT + @$"Flower{Path.DirectorySeparatorChar}Flower.png");

		for (int i = 0; i < 4; i++)
			Result_Panel_4P[i] = TxC(RESULT + @$"Panel_4P_" + (i + 1).ToString() + ".png");

		for (int i = 0; i < 5; i++)
			Result_Panel_5P[i] = TxC(RESULT + @$"Panel_5P_" + (i + 1).ToString() + ".png");

		for (int i = 1; i <= 5; i++)
			Result_Flower_Rotate[i - 1] = TxC(RESULT + @$"Flower{Path.DirectorySeparatorChar}Rotate_" + i.ToString() + ".png");

		//for (int i = 0; i < 3; i++)
		//Result_Work[i] = TxC(RESULT + @$"Work{Path.DirectorySeparatorChar}" + i.ToString() + ".png");

		OpenTaiko.Skin.Result_Gauge_Rainbow_Ptn = OpenTaiko.tSequenceImageSheetCountCount(CSkin.Path(BASE + RESULT + @$"Rainbow{Path.DirectorySeparatorChar}"));
		if (OpenTaiko.Skin.Result_Gauge_Rainbow_Ptn != 0) {
			Result_Rainbow = new CTexture[OpenTaiko.Skin.Result_Gauge_Rainbow_Ptn];
			for (int i = 0; i < OpenTaiko.Skin.Result_Gauge_Rainbow_Ptn; i++) {
				Result_Rainbow[i] = TxC(RESULT + @$"Rainbow{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			}
		}

		//for (int i = 0; i < 6; i++)
		//Result_Background[i] = TxC(RESULT + @$"Background_" + i.ToString() + ".png");

		//for (int i = 0; i < 4; i++)
		//Result_Mountain[i] = TxC(RESULT + @$"Background_Mountain_" + i.ToString() + ".png");

		#endregion

		#region 7_AIResults

		Result_AIBattle_Panel_AI = TxC(RESULT + @$"AIBattle{Path.DirectorySeparatorChar}Panel_AI.png");
		Result_AIBattle_Batch = TxC(RESULT + @$"AIBattle{Path.DirectorySeparatorChar}Batch.png");
		Result_AIBattle_SectionPlate = TxC(RESULT + @$"AIBattle{Path.DirectorySeparatorChar}SectionPlate.png");
		Result_AIBattle_WinFlag_Clear = TxC(RESULT + @$"AIBattle{Path.DirectorySeparatorChar}WinFlag_Win.png");
		Result_AIBattle_WinFlag_Lose = TxC(RESULT + @$"AIBattle{Path.DirectorySeparatorChar}WinFlag_Lose.png");

		#endregion

		#region [7_DanResults]

		//DanResult_Background = TxC(DANRESULT + @$"Background.png");
		DanResult_Rank = TxC(DANRESULT + @$"Rank.png");
		DanResult_SongPanel_Base = TxC(DANRESULT + @$"SongPanel_Base.png");
		DanResult_StatePanel_Base = TxC(DANRESULT + @$"StatePanel_Base.png");
		DanResult_SongPanel_Main = TxC(DANRESULT + @$"SongPanel_Main.png");
		DanResult_StatePanel_Main = TxC(DANRESULT + @$"StatePanel_Main.png");

		#endregion

		#region [8_TowerResults]

		OpenTaiko.Skin.Game_Tower_Ptn_Result = OpenTaiko.tSequenceImageSheetCountCount(CSkin.Path(BASE + TOWERRESULT + @$"Tower{Path.DirectorySeparatorChar}"));
		TowerResult_Tower = new CTexture[OpenTaiko.Skin.Game_Tower_Ptn_Result];

		TowerResult_Background = TxC(TOWERRESULT + @$"Background.png");
		TowerResult_Panel = TxC(TOWERRESULT + @$"Panel.png");

		TowerResult_ScoreRankEffect = TxC(TOWERRESULT + @$"ScoreRankEffect.png");

		for (int i = 0; i < OpenTaiko.Skin.Game_Tower_Ptn_Result; i++) {
			TowerResult_Tower[i] = TxC(TOWERRESULT + @$"Tower{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
		}

		#endregion

		#region [10_Heya]

		//Heya_Background = TxC(HEYA + @$"Background.png");
		Heya_Center_Menu_Box_Slot = TxC(HEYA + @$"Center_Menu_Box_Slot.png");
		Heya_Side_Menu = TxC(HEYA + @$"Side_Menu.png");
		Heya_Render_Field = TxC(HEYA + @$"Render_Field.png");
		Heya_Center_Menu_Background = TxC(HEYA + @$"Center_Menu_Background.png");
		Heya_Description_Panel = TxC(HEYA + @$"Description_Panel.png");
		Heya_Box = TxC(HEYA + @$"Box.png");
		Heya_Lock = TxC(HEYA + @$"Lock.png");

		#endregion

		#region [11_Characters]

		string[] charaDirs = OpenTaiko.GetMergedDirectories(OpenTaiko.strEXEFolder + GLOBAL + CHARACTERS);

		Characters = new CCharacterLuaSet[charaDirs.Length];

		string[] charaDirNames = new string[charaDirs.Length];
		for (int i = 0; i < charaDirs.Length; i++) {
			Characters[i] = new(charaDirs[i], i);
			charaDirNames[i] = Characters[i].dirName;
		}

		for (int i = 0; i < 5; i++) {
			OpenTaiko.SaveFileInstances[i].tReindexCharacter(charaDirNames);
			this.ReloadCharacter(-1, OpenTaiko.SaveFileInstances[i].data.Character, i, true);
			PaletteManager.RestoreFromSave(i);
			PlayerCharacters[i] = new LuaCharacter(i);

			// If the saved puchichara folder no longer exists, fall back to index 0.
			string puchi = OpenTaiko.SaveFileInstances[i].data.PuchiChara;
			if (OpenTaiko.Skin.Puchicharas_Name.Length > 0 && !OpenTaiko.Skin.Puchicharas_NameToIndex.ContainsKey(puchi))
				OpenTaiko.SaveFileInstances[i].data.PuchiChara = OpenTaiko.Skin.Puchicharas_Name[0];
		}

		LuaCharacterDb?.Dispose();
		LuaCharacterDb = new LuaCharacterDatabase(Characters);

		OpenTaiko.Databases?.LoadThemeSettings();
		CVirtualSlotManager.Initialize();

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
		bReportTextureProgress = false;
		try {
			File.WriteAllText(strTextureCountCache, nTexturesLoaded.ToString());
		} catch { /* non-fatal: just means next boot reuses the previous estimate */ }
		#endregion
	}

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

	public CTexture CalibrateFG,
		CalibrateBG;

	#endregion

	#region 共通
	public CTexture Tile_Black,
		Menu_Title,
		Menu_Highlight,
		Loading,
		Scanning_Loudness,
		NamePlateBase,
		Overlay;

	#endregion


	#region 3_段位選択画面 (textures still used by result/gameplay stages)

	public CTexture Dani_Difficulty_Cymbol;
	public CTexture Dani_Level_Number;
	public CTexture Dani_DanIcon;
	public CTexture Dani_DanIcon_Fade;

	#endregion

	#region 5_演奏画面
	#region 共通
	public CTexture Judge_Frame,
		Note_Mine,
		Note_Swap,
		Note_Kusu,
		Note_FuseRoll,
		Note_Adlib,
		SENotesExtension,
		Notes_Arm,
		ChipEffect,
		ScoreRank,
		Judge;
	public CTexture Judge_Meter,
		Bar,
		Bar_Branch;
	public CTexture[] Notes,
		SENotes;
	#endregion


	#region モブ
	#endregion
	#region 太鼓
	public CTexture[] Taiko_Base,
		Taiko_Frame, // MTaiko下敷き
		Taiko_Background;
	public CTexture Taiko_Don_Left,
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
	public CTexture[] Couse_Symbol, // コースシンボル
		Couse_Symbol_Back,
		Couse_Symbol_Back_Flash,
		Taiko_PlayerNumber;
	public CTexture[] Taiko_Score,
		Taiko_Combo,
		Taiko_Combo_Guide;
	#endregion
	#region ゲージ
	public CTexture[] Gauge,
		Gauge_Base,
		Gauge_Base_Norma,
		Gauge_Line,
		Gauge_Rainbow,
		Gauge_Rainbow_2PGauge,
		Gauge_Rainbow_Flat,
		Gauge_Clear,
		Gauge_Killzone,
		Gauge_Soul_Explosion;
	public CTexture Gauge_Soul,
		Gauge_Soul_Fire;
	public CTexture[] Gauge_Dan;
	public CTexture[] Gauge_Dan_Rainbow;
	#endregion
	#region 吹き出し
	public CTexture[] Balloon_Combo;
	public CTexture Balloon_Roll,
		Balloon_Balloon,
		Balloon_Number_Roll,
		Balloon_Number_Combo/*,*/
		/*Balloon_Broken*/;
	public CTexture Kusudama_Number;

	public CTexture Fuse_Number,
		Fuse_Balloon;

	public CTexture[] Balloon_Breaking;
	#endregion
	#region エフェクト
	public CTexture Effects_Hit_Explosion,
		Effects_Hit_Bomb,
		Effects_Hit_Explosion_Big,
		Effects_Fire,
		Effects_Rainbow,
		Effects_GoGoSplash,
		Effects_Hit_FireWorks;
	public CTexture[] Effects_Hit_Great,
		Effects_Hit_Good,
		Effects_Hit_Great_Big,
		Effects_Hit_Good_Big;
	public CTexture[] Effects_Roll;
	#endregion
	#region レーン
	public CTexture[] Lane_Red,
		Lane_Blue,
		Lane_Clap,
		Lane_Base,
		Lane_Text;
	public CTexture Lane_Yellow;
	public CTexture Lane_Background_Main,
		Lane_Background_AI,
		Lane_Background_Sub,
		Lane_Background_GoGo;
	#endregion
	#region ゲームモード
	public CTexture GameMode_Timer_Frame,
		GameMode_Timer_Tick;
	#endregion
	#region ランナー
	//public CTexture Runner;
	#endregion
	#region DanC
	public CTexture DanC_Background;
	public CTexture[] DanC_Gauge;
	public CTexture DanC_Base;
	public CTexture DanC_Base_Small;


	public CTexture DanC_Gauge_Base;
	public CTexture DanC_Failed;
	public CTexture DanC_Number,
		DanC_Small_Number,
		DanC_SmallBase,
		DanC_ExamType,
		DanC_ExamRange,
		DanC_Small_ExamCymbol,
		DanC_ExamCymbol,
		DanC_MiniNumber;
	public CTexture DanC_Screen;
	#endregion
	#region PuchiChara

	public CPuchichara[] Puchichara;
	public LuaPuchicharaDatabase? LuaPuchicharaDb;

	#endregion
	#region Training
	public CTexture Tokkun_DownBG,
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

	public CTexture Tower_Miss;

	public CTexture[] Tower_Top;

	public CTexture[][] Tower_Base,
		Tower_Deco;


	#endregion

	#region [22_AIBattle]

	public CTexture AIBattle_SectionTime_Panel,
		AIBattle_SectionTime_Bar_Base,
		AIBattle_SectionTime_Bar_Normal,
		AIBattle_SectionTime_Bar_Finish,
		AIBattle_Batch_Base,
		AIBattle_Batch,
		AIBattle_Judge_Number;

	public CTexture[] AIBattle_Judge_Meter = new CTexture[2];

	#endregion


	#endregion

	#region 6_結果発表
	public CTexture Result_Header,
		Result_Number,
		Result_Panel,
		Result_Panel_2P,
		Result_Soul_Text,
		Result_Soul_Fire,
		Result_Diff_Bar,
		Result_Score_Number,

		Result_CrownEffect,
		Result_ScoreRankEffect,

		//Result_Cloud,
		Result_Flower,
		Result_Shine,
		Result_Gauge_Frame,
		Result_Gauge_Clear,
		Result_Gauge_Clear_Base,
		Result_Gauge_Killzone;

	public CTexture[]
		Result_Panel_5P = new CTexture[5],
		Result_Panel_4P = new CTexture[4],
		Result_Rainbow = new CTexture[41],
		//Result_Background = new CTexture[6],
		Result_Flower_Rotate = new CTexture[5],
		//Result_Work = new CTexture[3],

		Result_Gauge = new CTexture[5],
		Result_Gauge_Base = new CTexture[5],
		Result_Speech_Bubble = new CTexture[2],
		Result_Speech_Bubble_V2 = new CTexture[3]
/*,
Result_Mountain = new CTexture[4]*/;
	#endregion

	#region 7_AIResults
	public CTexture Result_AIBattle_Panel_AI,
		Result_AIBattle_Batch,
		Result_AIBattle_SectionPlate,
		Result_AIBattle_WinFlag_Clear,
		Result_AIBattle_WinFlag_Lose;
	#endregion

	#region 7_終了画面
	//public CTexture Exit_Background/* , */
	/*Exit_Text; */
	#endregion

	#region [7_DanResults]

	public CTexture
		//DanResult_Background,
		DanResult_Rank,
		DanResult_SongPanel_Base,
		DanResult_StatePanel_Base,
		DanResult_SongPanel_Main,
		DanResult_StatePanel_Main;

	#endregion

	#region [8_TowerResults]

	public CTexture TowerResult_Background,
		TowerResult_ScoreRankEffect,
		TowerResult_Panel;
	public CTexture[]
		TowerResult_Tower;

	#endregion

	#region [10_Heya]

	public CTexture
		//Heya_Background,
		Heya_Center_Menu_Box_Slot,
		Heya_Side_Menu,
		Heya_Box,
		Heya_Render_Field,
		Heya_Center_Menu_Background,
		Heya_Description_Panel,
		Heya_Lock;

	#endregion

	#region [11_Characters]

	/*
	public CTexture[][] Characters_Normal,
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
	public CTexture[] Characters_Heya_Preview,
		Characters_Heya_Render,
		Characters_Result_Clear_1P,
		Characters_Result_Failed_1P,
		Characters_Result_Clear_2P,
		Characters_Result_Failed_2P;
	*/

	public class CCharacterLuaSet : CCharacter.Info, IDisposable {
		public readonly CCharacterLua[] instances; // [iPlayer]
		public CCharacter.Info info => this;
		public CCharacterLua this[int p] => instances[p];

		public CCharacterLuaSet(string path, int i) : base(path, i) {
			instances = Enumerable.Range(0, OpenTaiko.MAX_PLAYERS)
				.Select(i => new CCharacterLua(this))
				.ToArray();
		}

		public void Dispose() {
			foreach (var instance in instances)
				instance.Dispose();
		}
	}

	public CCharacterLuaSet[] Characters = [];
	public LuaCharacter[] PlayerCharacters = new LuaCharacter[5];
	public LuaCharacterDatabase? LuaCharacterDb;

	private CCharacter.ResourceRefCounts?[] playerResourceRefCounts = new CCharacter.ResourceRefCounts?[OpenTaiko.MAX_PLAYERS];
	#endregion

	#region [12_OnlineLounge]

	public CTexture
		//OnlineLounge_Background,
		OnlineLounge_Box,
		OnlineLounge_Side_Menu,
		OnlineLounge_Context,
		OnlineLounge_Return_Box,
		OnlineLounge_Song_Box;

	#endregion

	#region [ 解放用 ]
	public List<CTexture> listTexture = new List<CTexture>();
	#endregion

}
