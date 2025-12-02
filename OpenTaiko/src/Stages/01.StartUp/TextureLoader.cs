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
	public static string SONGLOADING = @$"4_SongLoading{Path.DirectorySeparatorChar}";
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
		var texpath = (localize) ? HLocalizedPath.GetAvailableLocalizedPath(CSkin.Path(BASE + FileName)) : CSkin.Path(BASE + FileName);
		var tex = OpenTaiko.tテクスチャの生成(texpath, false);

		listTexture.Add(tex);
		return tex;
	}

	internal CTexture TxCGlobal(string FileName) {
		var tex = OpenTaiko.tテクスチャの生成(OpenTaiko.strEXEのあるフォルダ + GLOBAL + FileName, false);
		listTexture.Add(tex);
		return tex;
	}

	internal CTexture TxCAbsolute(string FileName) {
		var tex = OpenTaiko.tテクスチャの生成(FileName, false);
		listTexture.Add(tex);
		return tex;
	}

	internal CTextureAf TxCAf(string FileName) {
		var tex = OpenTaiko.tテクスチャの生成Af(CSkin.Path(BASE + FileName));
		listTexture.Add(tex);
		return tex;
	}
	internal CTexture TxCGen(string FileName) {
		return OpenTaiko.tテクスチャの生成(CSkin.Path(BASE + GAME + GENRE + FileName + ".png"), false);
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
		var count = OpenTaiko.t連番画像の枚数を数える(folder);
		var texture = count == 0 ? null : TxCSong(count, folder + "{0}.png");
		return texture;
	}

	internal CTexture TxCUntrackedSong(string path) {
		return OpenTaiko.tテクスチャの生成(path, false);
	}

	public void LoadTexture() {
		CalibrateFG = TxC(CONFIG + $@"Calibration{Path.DirectorySeparatorChar}FG.png");
		CalibrateBG = TxC(CONFIG + $@"Calibration{Path.DirectorySeparatorChar}BG.png");

		#region 共通
		Tile_Black = TxC(@$"Tile_Black.png");
		Menu_Title = TxC(@$"Menu_Title.png");
		Menu_Highlight = TxC(@$"Menu_Highlight.png");
		Enum_Song = TxC(@$"Enum_Song.png");
		Loading = TxC(@$"Loading.png");
		Scanning_Loudness = TxC(@$"Scanning_Loudness.png");
		Overlay = TxC(@$"Overlay.png");
		Network_Connection = TxC(@$"Network_Connection.png");
		// Readme = TxC(@$"Readme.png");
		NamePlateBase = TxC(@$"NamePlate.png");
		NamePlate_Extension = TxC(@$"NamePlate_Extension.png");

		#endregion

		#region 2_コンフィグ画面
		//Config_Background = TxC(CONFIG + @$"Background.png");
		//Config_Header = TxC(CONFIG + @$"Header.png");
		Config_Cursor = TxC(CONFIG + @$"Cursor.png");
		Config_ItemBox = TxC(CONFIG + @$"ItemBox.png");
		Config_Arrow = TxC(CONFIG + @$"Arrow.png");
		Config_KeyAssign = TxC(CONFIG + @$"KeyAssign.png");
		Config_Font = TxC(CONFIG + @$"Font.png");
		Config_Font_Bold = TxC(CONFIG + @$"Font_Bold.png");
		Config_Enum_Song = TxC(CONFIG + @$"Enum_Song.png");
		#endregion

		#region 3_選曲画面
		SongSelect_Background = TxC(SONGSELECT + @$"Background.png");
		SongSelect_Header = TxC(SONGSELECT + @$"Header.png");
		SongSelect_Footer = TxC(SONGSELECT + @$"Footer.png");
		SongSelect_Coin_Slot[0] = TxC(SONGSELECT + @$"Coin_Slot.png");
		SongSelect_Coin_Slot[1] = TxC(SONGSELECT + @$"Coin_Slot_3P.png");
		SongSelect_Coin_Slot[2] = TxC(SONGSELECT + @$"Coin_Slot_4P.png");
		SongSelect_Coin_Slot[3] = TxC(SONGSELECT + @$"Coin_Slot_5P.png");

		SongSelect_Level = TxC(SONGSELECT + @$"Level.png");
		SongSelect_Branch = TxC(SONGSELECT + @$"Branch.png");
		SongSelect_Branch_Text = TxC(SONGSELECT + @$"Branch_Text.png");
		SongSelect_Bar_Center = TxC(SONGSELECT + @$"Bar_Center.png");
		SongSelect_Lock = TxC(SONGSELECT + @$"Lock.png");

		SongSelect_Frame_Score[0] = TxC(SONGSELECT + @$"Frame_Score.png");
		SongSelect_Frame_Score[1] = TxC(SONGSELECT + @$"Frame_Score_Tower.png");
		SongSelect_Frame_Score[2] = TxC(SONGSELECT + @$"Frame_Score_Dan.png");

		SongSelect_Tower_Side = TxC(SONGSELECT + @$"Tower_Side.png");

		SongSelect_Frame_Box = TxC(SONGSELECT + @$"Frame_Box.png");
		SongSelect_Frame_BackBox = TxC(SONGSELECT + @$"Frame_BackBox.png");
		SongSelect_Frame_Random = TxC(SONGSELECT + @$"Frame_Random.png");
		SongSelect_Bar_Genre_Back = TxC(SONGSELECT + @$"Bar_Genre_Back.png");
		SongSelect_Bar_Genre_Locked = TxC(SONGSELECT + @$"Bar_Genre_Locked.png");
		SongSelect_Bar_Genre_Locked_Top = TxC(SONGSELECT + @$"Bar_Genre_Locked_Top.png");
		SongSelect_Bar_Genre_Random = TxC(SONGSELECT + @$"Bar_Genre_Random.png");
		SongSelect_Bar_Genre_RecentryPlaySong = TxC(SONGSELECT + @$"Bar_Genre_RecentryPlaySong.png");
		SongSelect_Bar_Select = TxC(SONGSELECT + @$"Bar_Select.png");
		SongSelect_Level_Number = TxC(SONGSELECT + @$"Level_Number.png");
		SongSelect_Level_Number_Big = TxC(SONGSELECT + @$"Level_Number_Big.png");
		SongSelect_Level_Number_Big_Colored = TxC(SONGSELECT + @$"Level_Number_Big_Colored.png");
		SongSelect_Level_Number_Colored = TxC(SONGSELECT + @$"Level_Number_Colored.png");
		SongSelect_Level_Number_Big_Icon = TxC(SONGSELECT + @$"Level_Number_Big_Icon.png");
		SongSelect_Level_Number_Icon = TxC(SONGSELECT + @$"Level_Number_Icon.png");
		SongSelect_Bpm_Number = TxC(SONGSELECT + @$"Bpm_Number.png");
		SongSelect_Floor_Number = TxC(SONGSELECT + @$"Floor_Number.png");
		SongSelect_Credit = TxC(SONGSELECT + @$"Credit.png");
		SongSelect_Timer = TxC(SONGSELECT + @$"Timer.png");
		SongSelect_Explicit = TxC(SONGSELECT + @$"Explicit.png");
		SongSelect_Movie = TxC(SONGSELECT + @$"Movie.png");
		SongSelect_Song_Number = TxC(SONGSELECT + @$"Song_Number.png");
		SongSelect_Bar_Genre_Overlay = TxC(SONGSELECT + @$"Bar_Genre_Overlay.png");
		SongSelect_Crown = TxC(SONGSELECT + @$"SongSelect_Crown.png");
		SongSelect_ScoreRank = TxC(SONGSELECT + @$"ScoreRank.png");
		SongSelect_BoardNumber = TxC(SONGSELECT + @$"BoardNumber.png");
		SongSelect_Difficulty_Cymbol = TxC(SONGSELECT + "Difficulty_Cymbol.png");
		SongSelect_Unlock_Conditions = TxC(SONGSELECT + "Unlock_Conditions.png");

		SongSelect_Favorite = TxC(SONGSELECT + @$"Favorite.png");
		SongSelect_High_Score = TxC(SONGSELECT + @$"High_Score.png");

		SongSelect_Level_Icons = TxC(SONGSELECT + @$"Level_Icons.png");
		SongSelect_Search_Arrow = TxC(SONGSELECT + @$"Search{Path.DirectorySeparatorChar}Search_Arrow.png");
		SongSelect_Search_Arrow_Glow = TxC(SONGSELECT + @$"Search{Path.DirectorySeparatorChar}Search_Arrow_Glow.png");
		SongSelect_Search_Window = TxC(SONGSELECT + @$"Search{Path.DirectorySeparatorChar}Search_Window.png");

		for (int i = 0; i < (int)Difficulty.Total; i++) {
			SongSelect_ScoreWindow[i] = TxC(SONGSELECT + @$"ScoreWindow_" + i.ToString() + ".png");
		}

		SongSelect_ScoreWindow_Text = TxC(SONGSELECT + @$"ScoreWindow_Text.png");



		{
			string[] genre_files = Directory.GetFiles(CSkin.Path(BASE + SONGSELECT + @$"Bar_Genre{Path.DirectorySeparatorChar}"), "Bar_Genre_*.png");
			SongSelect_Bar_Genre = new();
			for (int i = 0; i < genre_files.Length; i++) {
				string name = Path.GetFileNameWithoutExtension(genre_files[i]).Split('_')[2];
				if (name != "Overlap") SongSelect_Bar_Genre.Add(name, TxC(SONGSELECT + @$"Bar_Genre{Path.DirectorySeparatorChar}Bar_Genre_" + name + ".png"));
			}
		}
		{
			string[] genre_files = Directory.GetFiles(CSkin.Path(BASE + SONGSELECT + @$"Bar_Genre{Path.DirectorySeparatorChar}"), "Bar_Genre_Overlap_*.png");
			SongSelect_Bar_Genre_Overlap = new();
			for (int i = 0; i < genre_files.Length; i++) {
				string name = Path.GetFileNameWithoutExtension(genre_files[i]).Split('_')[3];
				SongSelect_Bar_Genre_Overlap.Add(name, TxC(SONGSELECT + @$"Bar_Genre{Path.DirectorySeparatorChar}Bar_Genre_Overlap_" + name + ".png"));
			}
		}

		{
			string[] genre_files = Directory.GetFiles(CSkin.Path(BASE + SONGSELECT + @$"Genre_Background{Path.DirectorySeparatorChar}"), "GenreBackground_*.png");
			SongSelect_GenreBack = new();
			for (int i = 0; i < genre_files.Length; i++) {
				string name = Path.GetFileNameWithoutExtension(genre_files[i]).Split('_')[1];
				SongSelect_GenreBack.Add(name, TxC(SONGSELECT + @$"Genre_Background{Path.DirectorySeparatorChar}GenreBackground_" + name + ".png"));
			}
		}

		{
			string[] genre_files = Directory.GetFiles(CSkin.Path(BASE + SONGSELECT + @$"Box_Chara{Path.DirectorySeparatorChar}"), "Box_Chara_*.png");
			SongSelect_Box_Chara = new();
			for (int i = 0; i < genre_files.Length; i++) {
				string name = Path.GetFileNameWithoutExtension(genre_files[i]).Split('_')[2];
				SongSelect_Box_Chara.Add(name, TxC(SONGSELECT + @$"Box_Chara{Path.DirectorySeparatorChar}Box_Chara_" + name + ".png"));
			}
		}

		for (int i = 0; i < SongSelect_Table.Length; i++) {
			SongSelect_Table[i] = TxC(SONGSELECT + @$"Table{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
		}

		SongSelect_Song_Panel[0] = TxC(SONGSELECT + @$"Song_Panel{Path.DirectorySeparatorChar}Song_Panel_Box.png");
		SongSelect_Song_Panel[1] = TxC(SONGSELECT + @$"Song_Panel{Path.DirectorySeparatorChar}Song_Panel_Song.png");
		SongSelect_Song_Panel[2] = TxC(SONGSELECT + @$"Song_Panel{Path.DirectorySeparatorChar}Song_Panel_Dan.png");
		SongSelect_Song_Panel[3] = TxC(SONGSELECT + @$"Song_Panel{Path.DirectorySeparatorChar}Song_Panel_Tower.png");
		SongSelect_Song_Panel[4] = TxC(SONGSELECT + @$"Song_Panel{Path.DirectorySeparatorChar}Song_Panel_Locked_Song.png");
		SongSelect_Song_Panel[5] = TxC(SONGSELECT + @$"Song_Panel{Path.DirectorySeparatorChar}Song_Panel_Locked_Asset.png");

		#region [ 難易度選択画面 ]
		Difficulty_Bar = TxC(SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Bar.png");
		Difficulty_Number = TxC(SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Number.png");
		Difficulty_Number_Colored = TxC(SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Number_Colored.png");
		Difficulty_Number_Icon = TxC(SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Number_Icon.png");
		Difficulty_Star = TxC(SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Star.png");
		Difficulty_Crown = TxC(SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Crown.png");
		Difficulty_Option = TxC($"{SONGSELECT}Difficulty_Select/Difficulty_Option.png");
		Difficulty_Option_Select = TxC($"{SONGSELECT}Difficulty_Select/Difficulty_Option_Select.png");

		Difficulty_Select_Bar[0] = TxC(SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Select_Bar.png");
		Difficulty_Select_Bar[1] = TxC(SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Select_Bar2.png");
		Difficulty_Select_Bar[2] = TxC(SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Select_Bar3.png");
		Difficulty_Select_Bar[3] = TxC(SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Select_Bar4.png");
		Difficulty_Select_Bar[4] = TxC(SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Select_Bar5.png");

		{
			string[] genre_files = Directory.GetFiles(CSkin.Path(BASE + SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Back{Path.DirectorySeparatorChar}"), "Difficulty_Back_*.png");
			Difficulty_Back = new();
			for (int i = 0; i < genre_files.Length; i++) {
				string name = Path.GetFileNameWithoutExtension(genre_files[i]).Split('_')[2];
				Difficulty_Back.Add(name, TxC(SONGSELECT + @$"Difficulty_Select{Path.DirectorySeparatorChar}Difficulty_Back{Path.DirectorySeparatorChar}Difficulty_Back_" + name + ".png"));
			}
		}
		#endregion

		NewHeya_Close = TxC(SONGSELECT + @$"NewHeya{Path.DirectorySeparatorChar}Close.png");
		NewHeya_Close_Select = TxC(SONGSELECT + @$"NewHeya{Path.DirectorySeparatorChar}Close_Select.png");
		NewHeya_PlayerPlate[0] = TxC(SONGSELECT + @$"NewHeya{Path.DirectorySeparatorChar}PlayerPlate_1P.png");
		NewHeya_PlayerPlate[1] = TxC(SONGSELECT + @$"NewHeya{Path.DirectorySeparatorChar}PlayerPlate_2P.png");
		NewHeya_PlayerPlate[2] = TxC(SONGSELECT + @$"NewHeya{Path.DirectorySeparatorChar}PlayerPlate_3P.png");
		NewHeya_PlayerPlate[3] = TxC(SONGSELECT + @$"NewHeya{Path.DirectorySeparatorChar}PlayerPlate_4P.png");
		NewHeya_PlayerPlate[4] = TxC(SONGSELECT + @$"NewHeya{Path.DirectorySeparatorChar}PlayerPlate_5P.png");
		NewHeya_PlayerPlate_Select = TxC(SONGSELECT + @$"NewHeya{Path.DirectorySeparatorChar}PlayerPlate_Select.png");
		NewHeya_ModeBar = TxC(SONGSELECT + @$"NewHeya{Path.DirectorySeparatorChar}ModeBar.png");
		NewHeya_ModeBar_Select = TxC(SONGSELECT + @$"NewHeya{Path.DirectorySeparatorChar}ModeBar_Select.png");
		NewHeya_Box = TxC(SONGSELECT + @$"NewHeya{Path.DirectorySeparatorChar}Box.png");
		NewHeya_Lock = TxC(SONGSELECT + @$"NewHeya{Path.DirectorySeparatorChar}Lock.png");

		#endregion

		#region 3_段位選択画面

		//Dani_Background = TxC(DANISELECT + "Background.png");
		Dani_Difficulty_Cymbol = TxC(DANISELECT + "Difficulty_Cymbol.png");
		Dani_Level_Number = TxC(DANISELECT + "Level_Number.png");
		Dani_Soul_Number = TxC(DANISELECT + "SoulNumber.png");
		Dani_Exam_Number = TxC(DANISELECT + "ExamNumber.png");
		Dani_Bar_Center = TxC(DANISELECT + "Bar_Center.png");
		Dani_Bar_Back = TxC(DANISELECT + "Bar_Back.png");
		Dani_Bar_Folder = TxC(DANISELECT + "Bar_Folder.png");
		Dani_Bar_Folder_Back = TxC(DANISELECT + "Bar_Folder_Back.png");
		Dani_Bar_Random = TxC(DANISELECT + "Bar_Random.png");
		Dani_Plate = TxC(DANISELECT + "Plate.png");
		Dani_Plate_Extra = TxC(DANISELECT + "Plate_Extra.png");

		for (int i = 0; i < Challenge_Select.Length; i++)
			Challenge_Select[i] = TxC(DANISELECT + "Challenge_Select_" + i.ToString() + ".png");

		//Dani_Dan_In = TxC(DANISELECT + "Dan_In.png");
		//Dani_Dan_Text = TxC(DANISELECT + "Dan_Text.png");

		Dani_DanPlates = TxC(DANISELECT + "DanPlates.png");
		Dani_DanPlates_Back = TxC(DANISELECT + "DanPlates_Back.png");
		Dani_DanIcon = TxC(DANISELECT + "DanIcon.png");
		Dani_DanIcon_Fade = TxC(DANISELECT + "DanIcon_Fade.png");
		Dani_DanSides = TxC(DANISELECT + "DanSides.png");

		for (int i = 0; i < Dani_Bloc.Length; i++)
			Dani_Bloc[i] = TxC(DANISELECT + "Bloc" + i.ToString() + ".png");

		#endregion

		#region 4_読み込み画面

		SongLoading_Plate = TxC(SONGLOADING + @$"Plate.png");
		SongLoading_Bg = TxC(SONGLOADING + @$"Bg.png");
		SongLoading_BgWait = TxC(SONGLOADING + @$"Bg_Wait.png");
		SongLoading_Chara = TxC(SONGLOADING + @$"Chara.png");
		SongLoading_Fade = TxC(SONGLOADING + @$"Fade.png");
		SongLoading_Bg_Dan = TxC(SONGLOADING + @$"Bg_Dan.png");

		SongLoading_Plate_AI = TxC(SONGLOADING + @$"Plate_AI.png");
		SongLoading_Bg_AI = TxC(SONGLOADING + @$"Bg_AI.png");
		SongLoading_Bg_AI_Wait = TxC(SONGLOADING + @$"Bg_AI_Wait.png");
		SongLoading_Fade_AI = TxC(SONGLOADING + @$"Fade_AI.png");
		SongLoading_Fade_AI_Anime_Base = TxC(SONGLOADING + @$"Fade_AI_Anime_Base.png");
		SongLoading_Fade_AI_Anime_Ring = TxC(SONGLOADING + @$"Fade_AI_Anime_Ring.png");
		SongLoading_Fade_AI_Anime_NowLoading = TxC(SONGLOADING + @$"Fade_AI_Anime_NowLoading.png");
		SongLoading_Fade_AI_Anime_Start = TxC(SONGLOADING + @$"Fade_AI_Anime_Start.png");
		SongLoading_Fade_AI_Anime_LoadBar_Base = TxC(SONGLOADING + @$"Fade_AI_Anime_LoadBar_Base.png");
		SongLoading_Fade_AI_Anime_LoadBar = TxC(SONGLOADING + @$"Fade_AI_Anime_LoadBar.png");

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

		OpenTaiko.Skin.Game_Gauge_Rainbow_Ptn = OpenTaiko.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + GAUGE + @$"Rainbow{Path.DirectorySeparatorChar}"));
		if (OpenTaiko.Skin.Game_Gauge_Rainbow_Ptn != 0) {
			Gauge_Rainbow = new CTexture[OpenTaiko.Skin.Game_Gauge_Rainbow_Ptn];
			for (int i = 0; i < OpenTaiko.Skin.Game_Gauge_Rainbow_Ptn; i++) {
				Gauge_Rainbow[i] = TxC(GAME + GAUGE + @$"Rainbow{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			}
		}

		OpenTaiko.Skin.Game_Gauge_Rainbow_Flat_Ptn = OpenTaiko.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + GAUGE + @$"Rainbow_Flat{Path.DirectorySeparatorChar}"));
		if (OpenTaiko.Skin.Game_Gauge_Rainbow_Flat_Ptn != 0) {
			Gauge_Rainbow_Flat = new CTexture[OpenTaiko.Skin.Game_Gauge_Rainbow_Flat_Ptn];
			for (int i = 0; i < OpenTaiko.Skin.Game_Gauge_Rainbow_Flat_Ptn; i++) {
				Gauge_Rainbow_Flat[i] = TxC(GAME + GAUGE + @$"Rainbow_Flat{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			}
		}

		OpenTaiko.Skin.Game_Gauge_Rainbow_2PGauge_Ptn = OpenTaiko.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + GAUGE + @$"Rainbow_2PGauge{Path.DirectorySeparatorChar}"));
		if (OpenTaiko.Skin.Game_Gauge_Rainbow_2PGauge_Ptn != 0) {
			Gauge_Rainbow_2PGauge = new CTexture[OpenTaiko.Skin.Game_Gauge_Rainbow_2PGauge_Ptn];
			for (int i = 0; i < OpenTaiko.Skin.Game_Gauge_Rainbow_2PGauge_Ptn; i++) {
				Gauge_Rainbow_2PGauge[i] = TxC(GAME + GAUGE + @$"Rainbow_2PGauge{Path.DirectorySeparatorChar}" + i.ToString() + ".png");
			}
		}

		// Dan

		OpenTaiko.Skin.Game_Gauge_Dan_Rainbow_Ptn = OpenTaiko.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + DANC + @$"Rainbow{Path.DirectorySeparatorChar}"));
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
		if (Effects_Hit_Explosion != null) Effects_Hit_Explosion.b加算合成 = OpenTaiko.Skin.Game_Effect_HitExplosion_AddBlend;
		Effects_Hit_Explosion_Big = TxC(GAME + EFFECTS + @$"Hit{Path.DirectorySeparatorChar}Explosion_Big.png");
		if (Effects_Hit_Explosion_Big != null) Effects_Hit_Explosion_Big.b加算合成 = OpenTaiko.Skin.Game_Effect_HitExplosionBig_AddBlend;
		Effects_Hit_FireWorks = TxC(GAME + EFFECTS + @$"Hit{Path.DirectorySeparatorChar}FireWorks.png");
		if (Effects_Hit_FireWorks != null) Effects_Hit_FireWorks.b加算合成 = OpenTaiko.Skin.Game_Effect_FireWorks_AddBlend;

		Effects_Hit_Bomb = TxCAf(GAME + EFFECTS + @$"Hit{Path.DirectorySeparatorChar}Bomb.png");


		Effects_Fire = TxC(GAME + EFFECTS + @$"Fire.png");
		if (Effects_Fire != null) Effects_Fire.b加算合成 = OpenTaiko.Skin.Game_Effect_Fire_AddBlend;

		Effects_Rainbow = TxC(GAME + EFFECTS + @$"Rainbow.png");

		Effects_GoGoSplash = TxC(GAME + EFFECTS + @$"GoGoSplash.png");
		if (Effects_GoGoSplash != null) Effects_GoGoSplash.b加算合成 = OpenTaiko.Skin.Game_Effect_GoGoSplash_AddBlend;
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
		OpenTaiko.Skin.Game_Effect_Roll_Ptn = OpenTaiko.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + EFFECTS + @$"Roll{Path.DirectorySeparatorChar}"));
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
		DanC_ExamUnit = TxC(GAME + DANC + @$"ExamUnit.png");
		DanC_Screen = TxC(GAME + DANC + @$"Screen.png");
		DanC_SmallBase = TxC(GAME + DANC + @$"SmallBase.png");
		DanC_Small_ExamCymbol = TxC(GAME + DANC + @$"Small_ExamCymbol.png");
		DanC_ExamCymbol = TxC(GAME + DANC + @$"ExamCymbol.png");
		DanC_MiniNumber = TxC(GAME + DANC + @$"MiniNumber.png");

		#endregion

		#region PuchiChara

		var puchicharaDirs = System.IO.Directory.GetDirectories(OpenTaiko.strEXEのあるフォルダ + GLOBAL + PUCHICHARA);
		OpenTaiko.Skin.Puchichara_Ptn = puchicharaDirs.Length;

		Puchichara = new CPuchichara[OpenTaiko.Skin.Puchichara_Ptn];
		OpenTaiko.Skin.Puchicharas_Name = new string[OpenTaiko.Skin.Puchichara_Ptn];

		for (int i = 0; i < OpenTaiko.Skin.Puchichara_Ptn; i++) {
			Puchichara[i] = new CPuchichara(puchicharaDirs[i]);

			OpenTaiko.Skin.Puchicharas_Name[i] = System.IO.Path.GetFileName(puchicharaDirs[i]);
		}

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
			OpenTaiko.Skin.Game_Tower_Ptn_Base[i] = OpenTaiko.t連番画像の枚数を数える((towerDirectories[i] + @$"{Path.DirectorySeparatorChar}Base{Path.DirectorySeparatorChar}"), "Base");
			OpenTaiko.Skin.Game_Tower_Ptn_Deco[i] = OpenTaiko.t連番画像の枚数を数える((towerDirectories[i] + @$"{Path.DirectorySeparatorChar}Deco{Path.DirectorySeparatorChar}"), "Deco");

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

		#region [21_ModIcons]

		HiSp = new CTexture[14];
		for (int i = 0; i < HiSp.Length; i++) {
			HiSp[i] = TxC(GAME + MODICONS + @$"HS{Path.DirectorySeparatorChar}" + i.ToString() + @$".png");
		}

		Mod_Timing = new CTexture[5];
		for (int i = 0; i < Mod_Timing.Length; i++) {
			Mod_Timing[i] = TxC(GAME + MODICONS + @$"Timing{Path.DirectorySeparatorChar}" + i.ToString() + @$".png");
		}

		Mod_SongSpeed = new CTexture[2];
		for (int i = 0; i < Mod_SongSpeed.Length; i++) {
			Mod_SongSpeed[i] = TxC(GAME + MODICONS + @$"SongSpeed{Path.DirectorySeparatorChar}" + i.ToString() + @$".png");
		}

		Mod_Fun = new CTexture[3];
		for (int i = 0; i < Mod_Fun.Length; i++) {
			Mod_Fun[i] = TxC(GAME + MODICONS + @$"Fun{Path.DirectorySeparatorChar}" + i.ToString() + @$".png");
		}

		Mod_Doron = TxC(GAME + MODICONS + @$"Doron.png");
		Mod_Stealth = TxC(GAME + MODICONS + @$"Stealth.png");
		Mod_Mirror = TxC(GAME + MODICONS + @$"Mirror.png");
		Mod_Super = TxC(GAME + MODICONS + @$"Super.png");
		Mod_Hyper = TxC(GAME + MODICONS + @$"Hyper.png");
		Mod_Random = TxC(GAME + MODICONS + @$"Random.png");
		Mod_Auto = TxC(GAME + MODICONS + @$"Auto.png");
		Mod_Just = TxC(GAME + MODICONS + @$"Just.png");
		Mod_Safe = TxC(GAME + MODICONS + @$"Safe.png");
		Mod_None = TxC(GAME + MODICONS + @$"None.png");

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
		Result_Dan = TxC(RESULT + @$"Dan.png");

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

		OpenTaiko.Skin.Result_Gauge_Rainbow_Ptn = OpenTaiko.t連番画像の枚数を数える(CSkin.Path(BASE + RESULT + @$"Rainbow{Path.DirectorySeparatorChar}"));
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

		for (int i = 0; i < 3; i++)
			Result_Crown[i] = TxC(RESULT + @$"Crown{Path.DirectorySeparatorChar}Crown_" + i.ToString() + ".png");

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

		OpenTaiko.Skin.Game_Tower_Ptn_Result = OpenTaiko.t連番画像の枚数を数える(CSkin.Path(BASE + TOWERRESULT + @$"Tower{Path.DirectorySeparatorChar}"));
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
		Heya_Center_Menu_Bar = TxC(HEYA + @$"Center_Menu_Bar.png");
		Heya_Center_Menu_Box = TxC(HEYA + @$"Center_Menu_Box.png");
		Heya_Center_Menu_Box_Slot = TxC(HEYA + @$"Center_Menu_Box_Slot.png");
		Heya_Side_Menu = TxC(HEYA + @$"Side_Menu.png");
		Heya_Render_Field = TxC(HEYA + @$"Render_Field.png");
		Heya_Center_Menu_Background = TxC(HEYA + @$"Center_Menu_Background.png");
		Heya_Description_Panel = TxC(HEYA + @$"Description_Panel.png");
		Heya_Box = TxC(HEYA + @$"Box.png");
		Heya_Lock = TxC(HEYA + @$"Lock.png");

		#endregion

		#region [11_Characters]

		string[] charaDirs = System.IO.Directory.GetDirectories(OpenTaiko.strEXEのあるフォルダ + GLOBAL + CHARACTERS);

		Characters = new CCharacterLua[charaDirs.Length];

		string[] charaDirNames = new string[charaDirs.Length];
		for (int i = 0; i < charaDirs.Length; i++) {
			//Characters[i] = new CCharacterLegacy(charaDirs[i], i);
			Characters[i] = new CCharacterLua(charaDirs[i], i);
			charaDirNames[i] = Characters[i].dirName;
		}

		for (int i = 0; i < 5; i++) {
			OpenTaiko.SaveFileInstances[i].tReindexCharacter(charaDirNames);
			this.ReloadCharacter(-1, OpenTaiko.SaveFileInstances[i].data.Character, i, true);
		}


		#endregion

		#region [12_OnlineLounge]

		//OnlineLounge_Background = TxC(ONLINELOUNGE + @"Background.png");
		OnlineLounge_Box = TxC(ONLINELOUNGE + @"Box.png");
		OnlineLounge_Center_Menu_Bar = TxC(ONLINELOUNGE + @"Center_Menu_Bar.png");
		OnlineLounge_Center_Menu_Box_Slot = TxC(ONLINELOUNGE + @"Center_Menu_Box_Slot.png");
		OnlineLounge_Side_Menu = TxC(ONLINELOUNGE + @"Side_Menu.png");
		OnlineLounge_Context = TxC(ONLINELOUNGE + @"Context.png");
		OnlineLounge_Song_Box = TxC(ONLINELOUNGE + @"Song_Box.png");
		OnlineLounge_Return_Box = TxC(ONLINELOUNGE + @"Return_Box.png");

		#endregion

		#region [13_TowerSelect]

		TowerSelect_Tower = TxC(TOWERSELECT + @"Tower.png");

		#endregion

		if (OpenTaiko.ConfigIni.PreAssetsLoading) {
			foreach (var act in OpenTaiko.app.listTopLevelActivities) {
				act.CreateManagedResource();
				act.CreateUnmanagedResource();
			}
		}
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
			int i = old;

			CCharacter.CharaUnload(player, Characters[i]);
			OpenTaiko.SaveFileInstances[player].data.mountedCharacter = null;
		}

		if ((newC >= 0 &&
			 OpenTaiko.SaveFileInstances[player].data.Character != newC || primary)) {
			int i = newC;

			CCharacter.CharaLoad(player, Characters[i]);
			OpenTaiko.SaveFileInstances[player].data.mountedCharacter = ((CCharacterLua)Characters[i]).GetScript(player);
		}
	}

	public void DisposeTexture() {
		// concurrent
		var listTexture = Interlocked.Exchange(ref this.listTexture, []);
		for (int i = 0; i < listTexture.Count; ++i)
			listTexture.ElementAtOrDefault(i)?.Dispose();
		listTexture.Clear();

		foreach (CCharacter character in Characters) {
			character.Dispose();
		}

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
		Enum_Song,
		Loading,
		Scanning_Loudness,
		NamePlateBase,
		NamePlate_Extension,
		Overlay,
		Readme,
		Network_Connection;

	#endregion

	#region 2_コンフィグ画面
	public CTexture
		/*Config_Background,
        Config_Header,*/
		Config_Cursor,
		Config_ItemBox,
		Config_Arrow,
		Config_KeyAssign,
		Config_Font,
		Config_Font_Bold,
		Config_Enum_Song;
	#endregion

	#region 3_選曲画面

	public CTexture SongSelect_Background,
		SongSelect_Header,
		SongSelect_Footer,
		SongSelect_Auto,
		SongSelect_Level,
		SongSelect_Branch,
		SongSelect_Branch_Text,
		SongSelect_Lock,
		SongSelect_Frame_Box,
		SongSelect_Frame_BackBox,
		SongSelect_Frame_Random,
		SongSelect_Bar_Center,
		SongSelect_Bar_Genre_Back,
		SongSelect_Bar_Genre_Locked,
		SongSelect_Bar_Genre_Locked_Top,
		SongSelect_Bar_Genre_Random,
		SongSelect_Bar_Genre_RecentryPlaySong,
		SongSelect_Level_Number,
		SongSelect_Level_Number_Colored,
		SongSelect_Level_Number_Icon,
		SongSelect_Level_Number_Big,
		SongSelect_Level_Number_Big_Colored,
		SongSelect_Level_Number_Big_Icon,
		SongSelect_Bpm_Number,
		SongSelect_Floor_Number,
		SongSelect_Bar_Select,
		SongSelect_Bar_Genre_Overlay,
		SongSelect_Credit,
		SongSelect_Timer,
		SongSelect_Explicit,
		SongSelect_Movie,
		SongSelect_Crown,
		SongSelect_ScoreRank,
		SongSelect_Song_Number,
		SongSelect_BoardNumber,
		SongSelect_Difficulty_Cymbol,
		SongSelect_Unlock_Conditions,
		SongSelect_Tower_Side,

		SongSelect_Favorite,
		SongSelect_High_Score,

		SongSelect_Level_Icons,
		SongSelect_Search_Arrow,
		SongSelect_Search_Arrow_Glow,
		SongSelect_Search_Window,

		SongSelect_ScoreWindow_Text;
	public Dictionary<string, CTexture> SongSelect_GenreBack,
		SongSelect_Bar_Genre,
		SongSelect_Bar_Genre_Overlap,
		SongSelect_Box_Chara;
	public CTexture[]
		SongSelect_ScoreWindow = new CTexture[(int)Difficulty.Total],
		SongSelect_Frame_Score = new CTexture[3],
		SongSelect_NamePlate = new CTexture[1],
		SongSelect_Song_Panel = new CTexture[6],
		SongSelect_Coin_Slot = new CTexture[4],
		SongSelect_Table = new CTexture[6];

	#region [ 難易度選択画面 ]
	public CTexture Difficulty_Bar;
	public CTexture Difficulty_Number;
	public CTexture Difficulty_Number_Colored;
	public CTexture Difficulty_Number_Icon;
	public CTexture Difficulty_Star;
	public CTexture Difficulty_Crown;
	public CTexture Difficulty_Option;
	public CTexture Difficulty_Option_Select;

	public CTexture[] Difficulty_Select_Bar = new CTexture[5];
	public Dictionary<string, CTexture> Difficulty_Back;
	#endregion

	public CTexture NewHeya_Close;
	public CTexture NewHeya_Close_Select;
	public CTexture[] NewHeya_PlayerPlate = new CTexture[5];
	public CTexture NewHeya_PlayerPlate_Select;
	public CTexture NewHeya_ModeBar;
	public CTexture NewHeya_ModeBar_Select;
	public CTexture NewHeya_Box;
	public CTexture NewHeya_Lock;

	#endregion

	#region 3_段位選択画面

	//public CTexture Dani_Background;
	public CTexture Dani_Difficulty_Cymbol;
	public CTexture Dani_Level_Number;
	public CTexture Dani_Soul_Number;
	public CTexture Dani_Exam_Number;
	public CTexture Dani_Bar_Center;
	public CTexture Dani_Bar_Back;
	public CTexture Dani_Bar_Folder;
	public CTexture Dani_Bar_Folder_Back;
	public CTexture Dani_Bar_Random;
	public CTexture Dani_Plate;
	public CTexture Dani_Plate_Extra;

	public CTexture[] Challenge_Select = new CTexture[3];

	//public CTexture Dani_Dan_In;
	//public CTexture Dani_Dan_Text;

	public CTexture Dani_DanPlates;
	public CTexture Dani_DanPlates_Back;
	public CTexture Dani_DanIcon;
	public CTexture Dani_DanIcon_Fade;
	public CTexture Dani_DanSides;
	public CTexture[] Dani_Bloc = new CTexture[4];

	#endregion

	#region 4_読み込み画面
	public CTexture SongLoading_Plate,
		SongLoading_Bg,
		SongLoading_BgWait,
		SongLoading_Chara,
		SongLoading_Bg_Dan,
		SongLoading_Fade,

		SongLoading_Plate_AI,
		SongLoading_Bg_AI,
		SongLoading_Bg_AI_Wait,
		SongLoading_Fade_AI,
		SongLoading_Fade_AI_Anime_Base,
		SongLoading_Fade_AI_Anime_Ring,
		SongLoading_Fade_AI_Anime_NowLoading,
		SongLoading_Fade_AI_Anime_Start,
		SongLoading_Fade_AI_Anime_LoadBar_Base,
		SongLoading_Fade_AI_Anime_LoadBar;
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
		Taiko_PlayerNumber,
		Taiko_NamePlate; // ネームプレート
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
	#region ステージ失敗
	public CTexture Failed_Game,
		Failed_Stage;
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
		DanC_MiniNumber,
		DanC_ExamUnit;
	public CTexture DanC_Screen;
	#endregion
	#region PuchiChara

	public CPuchichara[] Puchichara;

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

	#region [21_ModIcons]

	public CTexture[] Mod_Timing,
		Mod_SongSpeed,
		Mod_Fun,
		HiSp;
	public CTexture Mod_None,
		Mod_Doron,
		Mod_Stealth,
		Mod_Mirror,
		Mod_Random,
		Mod_Super,
		Mod_Hyper,
		Mod_Just,
		Mod_Safe,
		Mod_Auto;

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
		Result_Gauge_Killzone,

		Result_Dan;

	public CTexture[]
		Result_Panel_5P = new CTexture[5],
		Result_Panel_4P = new CTexture[4],
		Result_Rainbow = new CTexture[41],
		//Result_Background = new CTexture[6],
		Result_Crown = new CTexture[3],

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
		Heya_Center_Menu_Bar,
		Heya_Center_Menu_Box,
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
	public CCharacterLua[] Characters = [];

	#endregion

	#region [12_OnlineLounge]

	public CTexture
		//OnlineLounge_Background,
		OnlineLounge_Box,
		OnlineLounge_Center_Menu_Bar,
		OnlineLounge_Center_Menu_Box_Slot,
		OnlineLounge_Side_Menu,
		OnlineLounge_Context,
		OnlineLounge_Return_Box,
		OnlineLounge_Song_Box;

	#endregion

	#region [13_TowerSelect]

	public CTexture
		TowerSelect_Tower;

	#endregion


	#region [ 解放用 ]
	public List<CTexture> listTexture = new List<CTexture>();
	#endregion

}
