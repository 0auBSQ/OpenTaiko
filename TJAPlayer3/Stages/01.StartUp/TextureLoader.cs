using FDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;

namespace TJAPlayer3
{
    class TextureLoader
    {
        public const string BASE = @"Graphics\";
        const string GLOBAL = @"Global\";

        // Global assets 
        const string PUCHICHARA = @"PuchiChara\";
        const string CHARACTERS = @"Characters\";

        // Stage
        public const string TITLE = @"1_Title\";
        public const string CONFIG = @"2_Config\";
        public const string SONGSELECT = @"3_SongSelect\";
        public const string DANISELECT = @"3_DaniSelect\";
        const string SONGLOADING = @"4_SongLoading\";
        public const string GAME = @"5_Game\";
        public const string RESULT = @"6_Result\";
        public const string EXIT = @"7_Exit\";
        public const string DANRESULT = @"7_DanResult\";
        const string TOWERRESULT = @"8_TowerResult\";
        public const string HEYA = @"10_Heya\";
        
        const string MODALS = @"11_Modals\";
        public const string ONLINELOUNGE = @"12_OnlineLounge\";
        public const string TOWERSELECT = @"13_TowerSelect\";
        public const string OPENENCYCLOPEDIA = @"15_OpenEncyclopedia\";

        // InGame
        public const string DANCER = @"2_Dancer\";
        const string MOB = @"3_Mob\";
        const string COURSESYMBOL = @"4_CourseSymbol\";
        public const string BACKGROUND = @"5_Background\";
        const string TAIKO = @"6_Taiko\";
        const string GAUGE = @"7_Gauge\";
        public const string FOOTER = @"8_Footer\";
        public const string END = @"9_End\";
        const string EFFECTS = @"10_Effects\";
        const string BALLOON = @"11_Balloon\";
        const string LANE = @"12_Lane\";
        const string GENRE = @"13_Genre\";
        const string GAMEMODE = @"14_GameMode\";
        const string FAILED = @"15_Failed\";
        public const string RUNNER = @"16_Runner\";
        const string TRAINING = @"19_Training\";
        const string DANC = @"17_DanC\";
        const string TOWER = @"20_Tower\";
        const string MODICONS = @"21_ModIcons\";
        const string AIBATTLE = @"22_AIBattle\";

        // Special balloons
        const string KUSUDAMA = @"Kusudama\";
        const string FUSE = @"Fuseroll\";

        // Tower infos
        const string TOWERDON = @"Tower_Don\";
        const string TOWERFLOOR = @"Tower_Floors\";

        // InGame_Effects
        const string FIRE = @"Fire\";
        const string HIT = @"Hit\";
        const string ROLL = @"Roll\";
        const string SPLASH = @"Splash\";

        public Dictionary<string, CTexture> trackedTextures = new Dictionary<string, CTexture>();


        public TextureLoader()
        {
            // コンストラクタ
        }

        internal CTexture TxC(string FileName)
        {
            var tex = TJAPlayer3.tテクスチャの生成(CSkin.Path(BASE + FileName));
            listTexture.Add(tex);
            return tex;
        }

        internal CTexture TxCGlobal(string FileName)
        {
            var tex = TJAPlayer3.tテクスチャの生成(TJAPlayer3.strEXEのあるフォルダ + GLOBAL + FileName);
            listTexture.Add(tex);
            return tex;
        }

        internal CTexture TxCAbsolute(string FileName)
        {
            var tex = TJAPlayer3.tテクスチャの生成(FileName);
            listTexture.Add(tex);
            return tex;
        }

        internal CTextureAf TxCAf(string FileName)
        {
            var tex = TJAPlayer3.tテクスチャの生成Af(CSkin.Path(BASE + FileName));
            listTexture.Add(tex);
            return tex;
        }
        internal CTexture TxCGen(string FileName)
        {
            return TJAPlayer3.tテクスチャの生成(CSkin.Path(BASE + GAME + GENRE + FileName + ".png"));
        }

        internal CTexture TxCSong(string path)
        {
            return TxCUntrackedSong(path);
        }

        private CTexture[] TxCSong(int count, string format, int start = 0)
        {
            return TxCSong(format, Enumerable.Range(start, count).Select(o => o.ToString()).ToArray());
        }

        private CTexture[] TxCSong(string format, params string[] parts)
        {
            return parts.Select(o => TxCSong(string.Format(format, o))).ToArray();
        }

        public CTexture[] TxCSongFolder(string folder)
        {
            var count = TJAPlayer3.t連番画像の枚数を数える(folder);
            var texture = count == 0 ? null : TxCSong(count, folder + "{0}.png");
            return texture;
        }

        internal CTexture TxCUntrackedSong(string path)
        {
            return TJAPlayer3.tテクスチャの生成(path);
        }

        public void LoadTexture()
        {
            #region 共通
            Tile_Black = TxC(@"Tile_Black.png");
            Menu_Title = TxC(@"Menu_Title.png");
            Menu_Highlight = TxC(@"Menu_Highlight.png");
            Enum_Song = TxC(@"Enum_Song.png");
            Loading = TxC(@"Loading.png");
            Scanning_Loudness = TxC(@"Scanning_Loudness.png");
            Overlay = TxC(@"Overlay.png");
            Network_Connection = TxC(@"Network_Connection.png");
            Readme = TxC(@"Readme.png");
            NamePlate = new CTexture[2];
            NamePlateBase = TxC(@"NamePlate.png");
            NamePlate_Extension = TxC(@"NamePlate_Extension.png");
            NamePlate[0] = TxC(@"1P_NamePlate.png");
            NamePlate[1] = TxC(@"2P_NamePlate.png");
            NamePlate_Effect[0] = TxC(@"9_NamePlateEffect\GoldMStar.png");
            NamePlate_Effect[1] = TxC(@"9_NamePlateEffect\PurpleMStar.png");
            NamePlate_Effect[2] = TxC(@"9_NamePlateEffect\GoldBStar.png");
            NamePlate_Effect[3] = TxC(@"9_NamePlateEffect\PurpleBStar.png");
            NamePlate_Effect[4] = TxC(@"9_NamePlateEffect\Slash.png");

            TJAPlayer3.Skin.Config_NamePlate_Ptn_Title = System.IO.Directory.GetDirectories(CSkin.Path(BASE + @"9_NamePlateEffect\Title\")).Length;
            TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes = new int[TJAPlayer3.Skin.Config_NamePlate_Ptn_Title];

            NamePlate_Title = new CTexture[TJAPlayer3.Skin.Config_NamePlate_Ptn_Title][];
            NamePlate_Title_Big = new CTexture[TJAPlayer3.Skin.Config_NamePlate_Ptn_Title];
            NamePlate_Title_Small = new CTexture[TJAPlayer3.Skin.Config_NamePlate_Ptn_Title];

            for (int i = 0; i < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title; i++)
            {
                TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[i] = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + @"9_NamePlateEffect\Title\" + i.ToString() + @"\"));
                NamePlate_Title[i] = new CTexture[TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[i]];

                for (int j = 0; j < TJAPlayer3.Skin.Config_NamePlate_Ptn_Title_Boxes[i]; j++)
                {
                    NamePlate_Title[i][j] = TxC(@"9_NamePlateEffect\Title\" + i.ToString() + @"\" + j.ToString() + @".png");
                }

                NamePlate_Title_Big[i] = TxC(@"9_NamePlateEffect\Title\" + i.ToString() + @"\Big.png");
                NamePlate_Title_Small[i] = TxC(@"9_NamePlateEffect\Title\" + i.ToString() + @"\Small.png");
            }


            #endregion

            #region 1_タイトル画面
            //Title_Background = TxC(TITLE + @"Background.png");
            Entry_Bar = TxC(TITLE + @"Entry_Bar.png");
            Entry_Bar_Text = TxC(TITLE + @"Entry_Bar_Text.png");

            Banapas_Load[0] = TxC(TITLE + @"Banapas_Load.png");
            Banapas_Load[1] = TxC(TITLE + @"Banapas_Load_Text.png");
            Banapas_Load[2] = TxC(TITLE + @"Banapas_Load_Anime.png");

            Banapas_Load_Clear[0] = TxC(TITLE + @"Banapas_Load_Clear.png");
            Banapas_Load_Clear[1] = TxC(TITLE + @"Banapas_Load_Clear_Anime.png");

            Banapas_Load_Failure[0] = TxC(TITLE + @"Banapas_Load_Failure.png");
            Banapas_Load_Failure[1] = TxC(TITLE + @"Banapas_Load_Clear_Anime.png");

            Entry_Player[0] = TxC(TITLE + @"Entry_Player.png");
            Entry_Player[1] = TxC(TITLE + @"Entry_Player_Select_Bar.png");
            Entry_Player[2] = TxC(TITLE + @"Entry_Player_Select.png");

            ModeSelect_Bar = new CTexture[CMainMenuTab.__MenuCount + 1];
            ModeSelect_Bar_Chara = new CTexture[CMainMenuTab.__MenuCount];

            for (int i = 0; i < CMainMenuTab.__MenuCount; i++)
            {
                ModeSelect_Bar[i] = TxC(TITLE + @"ModeSelect_Bar_" + i.ToString() + ".png");
            }
            
            for(int i = 0; i < CMainMenuTab.__MenuCount; i++)
            {
                ModeSelect_Bar_Chara[i] = TxC(TITLE + @"ModeSelect_Bar_Chara_" + i.ToString() + ".png");
            }

            ModeSelect_Bar[CMainMenuTab.__MenuCount] = TxC(TITLE + @"ModeSelect_Bar_Overlay.png");

            #endregion

            #region 2_コンフィグ画面
            //Config_Background = TxC(CONFIG + @"Background.png");
            //Config_Header = TxC(CONFIG + @"Header.png");
            Config_Cursor = TxC(CONFIG + @"Cursor.png");
            Config_ItemBox = TxC(CONFIG + @"ItemBox.png");
            Config_Arrow = TxC(CONFIG + @"Arrow.png");
            Config_KeyAssign = TxC(CONFIG + @"KeyAssign.png");
            Config_Font = TxC(CONFIG + @"Font.png");
            Config_Font_Bold = TxC(CONFIG + @"Font_Bold.png");
            Config_Enum_Song = TxC(CONFIG + @"Enum_Song.png");
            #endregion

            #region 3_選曲画面
            SongSelect_Background = TxC(SONGSELECT + @"Background.png");
            SongSelect_Header = TxC(SONGSELECT + @"Header.png");
            SongSelect_Footer = TxC(SONGSELECT + @"Footer.png");
            SongSelect_Coin_Slot[0] = TxC(SONGSELECT + @"Coin_Slot.png");
            SongSelect_Coin_Slot[1] = TxC(SONGSELECT + @"Coin_Slot_3P.png");
            SongSelect_Coin_Slot[2] = TxC(SONGSELECT + @"Coin_Slot_4P.png");
            SongSelect_Coin_Slot[3] = TxC(SONGSELECT + @"Coin_Slot_5P.png");

            SongSelect_Auto = TxC(SONGSELECT + @"Auto.png");
            SongSelect_Level = TxC(SONGSELECT + @"Level.png");
            SongSelect_Branch = TxC(SONGSELECT + @"Branch.png");
            SongSelect_Branch_Text = TxC(SONGSELECT + @"Branch_Text.png");
            SongSelect_Bar_Center = TxC(SONGSELECT + @"Bar_Center.png");
            SongSelect_Lock = TxC(SONGSELECT + @"Lock.png");

            SongSelect_Frame_Score[0] = TxC(SONGSELECT + @"Frame_Score.png");
            SongSelect_Frame_Score[1] = TxC(SONGSELECT + @"Frame_Score_Tower.png");
            SongSelect_Frame_Score[2] = TxC(SONGSELECT + @"Frame_Score_Dan.png");

            SongSelect_Tower_Side = TxC(SONGSELECT + @"Tower_Side.png");

            SongSelect_Frame_Box = TxC(SONGSELECT + @"Frame_Box.png");
            SongSelect_Frame_BackBox = TxC(SONGSELECT + @"Frame_BackBox.png");
            SongSelect_Frame_Random = TxC(SONGSELECT + @"Frame_Random.png");
            SongSelect_Bar_Genre_Back = TxC(SONGSELECT + @"Bar_Genre_Back.png");
            SongSelect_Bar_Genre_Locked = TxC(SONGSELECT + @"Bar_Genre_Locked.png");
            SongSelect_Bar_Genre_Random = TxC(SONGSELECT + @"Bar_Genre_Random.png");
            SongSelect_Bar_Genre_RecentryPlaySong = TxC(SONGSELECT + @"Bar_Genre_RecentryPlaySong.png");
            SongSelect_Bar_Select = TxC(SONGSELECT + @"Bar_Select.png");
            SongSelect_Level_Number = TxC(SONGSELECT + @"Level_Number.png");
            SongSelect_Level_Number_Big = TxC(SONGSELECT + @"Level_Number_Big.png");
            SongSelect_Level_Number_Big_Colored = TxC(SONGSELECT + @"Level_Number_Big_Colored.png");
            SongSelect_Level_Number_Colored = TxC(SONGSELECT + @"Level_Number_Colored.png");
            SongSelect_Level_Number_Big_Icon = TxC(SONGSELECT + @"Level_Number_Big_Icon.png");
            SongSelect_Level_Number_Icon = TxC(SONGSELECT + @"Level_Number_Icon.png");
            SongSelect_Bpm_Number = TxC(SONGSELECT + @"Bpm_Number.png");
            SongSelect_Floor_Number = TxC(SONGSELECT + @"Floor_Number.png");
            SongSelect_Credit = TxC(SONGSELECT + @"Credit.png");
            SongSelect_Timer = TxC(SONGSELECT + @"Timer.png");
            SongSelect_Explicit = TxC(SONGSELECT + @"Explicit.png");
            SongSelect_Song_Number = TxC(SONGSELECT + @"Song_Number.png");
            SongSelect_Bar_Genre_Overlay = TxC(SONGSELECT + @"Bar_Genre_Overlay.png");
            SongSelect_Crown = TxC(SONGSELECT + @"SongSelect_Crown.png");
            SongSelect_ScoreRank = TxC(SONGSELECT + @"ScoreRank.png");
            SongSelect_BoardNumber = TxC(SONGSELECT + @"BoardNumber.png");
            SongSelect_Difficulty_Cymbol = TxC(SONGSELECT + "Difficulty_Cymbol.png");

            SongSelect_Favorite = TxC(SONGSELECT + @"Favorite.png");
            SongSelect_High_Score = TxC(SONGSELECT + @"High_Score.png");

            SongSelect_Level_Icons = TxC(SONGSELECT + @"Level_Icons.png");
            SongSelect_Search_Arrow = TxC(SONGSELECT + @"Search\Search_Arrow.png");
            SongSelect_Search_Arrow_Glow = TxC(SONGSELECT + @"Search\Search_Arrow_Glow.png");
            SongSelect_Search_Window = TxC(SONGSELECT + @"Search\Search_Window.png");

            for (int i = 0; i < (int)Difficulty.Total; i++)
            {
                SongSelect_ScoreWindow[i] = TxC(SONGSELECT + @"ScoreWindow_" + i.ToString() + ".png");
            }

            SongSelect_ScoreWindow_Text = TxC(SONGSELECT + @"ScoreWindow_Text.png");

            TJAPlayer3.Skin.SongSelect_Bar_Genre_Count = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + SONGSELECT + @"Bar_Genre\"), "Bar_Genre_");

            if (TJAPlayer3.Skin.SongSelect_Bar_Genre_Count != 0)
            {
                SongSelect_Bar_Genre = new CTexture[TJAPlayer3.Skin.SongSelect_Bar_Genre_Count];
                SongSelect_Bar_Genre_Overlap = new CTexture[TJAPlayer3.Skin.SongSelect_Bar_Genre_Count];
                for (int i = 0; i < SongSelect_Bar_Genre.Length; i++)
                {
                    SongSelect_Bar_Genre[i] = TxC(SONGSELECT + @"Bar_Genre\Bar_Genre_" + i.ToString() + ".png");
                    SongSelect_Bar_Genre_Overlap[i] = TxC(SONGSELECT + @"Bar_Genre\Bar_Genre_Overlap_" + i.ToString() + ".png");
                }
            }

            TJAPlayer3.Skin.SongSelect_Genre_Background_Count = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + SONGSELECT + @"Genre_Background\"), "GenreBackground_");

            if (TJAPlayer3.Skin.SongSelect_Genre_Background_Count != 0)
            {
                SongSelect_GenreBack = new CTexture[TJAPlayer3.Skin.SongSelect_Genre_Background_Count];
                for (int i = 0; i < SongSelect_GenreBack.Length; i++)
                {
                    SongSelect_GenreBack[i] = TxC(SONGSELECT + @"Genre_Background\GenreBackground_" + i.ToString() + ".png");
                }
            }

            TJAPlayer3.Skin.SongSelect_Box_Chara_Count = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + SONGSELECT + @"Box_Chara\"), "Box_Chara_");

            if (TJAPlayer3.Skin.SongSelect_Box_Chara_Count != 0)
            {
                SongSelect_Box_Chara = new CTexture[TJAPlayer3.Skin.SongSelect_Box_Chara_Count];
                for (int i = 0; i < SongSelect_Box_Chara.Length; i++)
                {
                    SongSelect_Box_Chara[i] = TxC(SONGSELECT + @"Box_Chara\Box_Chara_" + i.ToString() + ".png");
                }
            }

            for (int i = 0; i < SongSelect_Table.Length; i++)
            {
                SongSelect_Table[i] = TxC(SONGSELECT + @"Table\" + i.ToString() + ".png");
            }

            SongSelect_Song_Panel[0] = TxC(SONGSELECT + @"Song_Panel\Song_Panel_Box.png");
            SongSelect_Song_Panel[1] = TxC(SONGSELECT + @"Song_Panel\Song_Panel_Song.png");
            SongSelect_Song_Panel[2] = TxC(SONGSELECT + @"Song_Panel\Song_Panel_Dan.png");
            SongSelect_Song_Panel[3] = TxC(SONGSELECT + @"Song_Panel\Song_Panel_Tower.png");
            SongSelect_Song_Panel[4] = TxC(SONGSELECT + @"Song_Panel\Song_Panel_Locked_Song.png");
            SongSelect_Song_Panel[5] = TxC(SONGSELECT + @"Song_Panel\Song_Panel_Locked_Asset.png");

            #region [ 難易度選択画面 ]
            Difficulty_Bar = TxC(SONGSELECT + @"Difficulty_Select\Difficulty_Bar.png");
            Difficulty_Number = TxC(SONGSELECT + @"Difficulty_Select\Difficulty_Number.png");
            Difficulty_Number_Colored = TxC(SONGSELECT + @"Difficulty_Select\Difficulty_Number_Colored.png");
            Difficulty_Number_Icon = TxC(SONGSELECT + @"Difficulty_Select\Difficulty_Number_Icon.png");
            Difficulty_Star = TxC(SONGSELECT + @"Difficulty_Select\Difficulty_Star.png");
            Difficulty_Crown = TxC(SONGSELECT + @"Difficulty_Select\Difficulty_Crown.png");
            Difficulty_Option = TxC($"{SONGSELECT}Difficulty_Select/Difficulty_Option.png");
            Difficulty_Option_Select = TxC($"{SONGSELECT}Difficulty_Select/Difficulty_Option_Select.png");

            Difficulty_Select_Bar[0] = TxC(SONGSELECT + @"Difficulty_Select\Difficulty_Select_Bar.png");
            Difficulty_Select_Bar[1] = TxC(SONGSELECT + @"Difficulty_Select\Difficulty_Select_Bar2.png");
            Difficulty_Select_Bar[2] = TxC(SONGSELECT + @"Difficulty_Select\Difficulty_Select_Bar3.png");
            Difficulty_Select_Bar[3] = TxC(SONGSELECT + @"Difficulty_Select\Difficulty_Select_Bar4.png");
            Difficulty_Select_Bar[4] = TxC(SONGSELECT + @"Difficulty_Select\Difficulty_Select_Bar5.png");

            TJAPlayer3.Skin.SongSelect_Difficulty_Background_Count = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + SONGSELECT + @"Difficulty_Select\Difficulty_Back\"), "Difficulty_Back_");

            if (TJAPlayer3.Skin.SongSelect_Difficulty_Background_Count != 0)
            {
                Difficulty_Back = new CTexture[TJAPlayer3.Skin.SongSelect_Difficulty_Background_Count];
                for (int i = 0; i < Difficulty_Back.Length; i++)
                {
                    Difficulty_Back[i] = TxC(SONGSELECT + @"Difficulty_Select\Difficulty_Back\Difficulty_Back_" + i.ToString() + ".png");
                }
            }
            #endregion

            #endregion

            #region 3_段位選択画面

            //Dani_Background = TxC(DANISELECT + "Background.png");
            Dani_Difficulty_Cymbol = TxC(DANISELECT + "Difficulty_Cymbol.png");
            Dani_Level_Number = TxC(DANISELECT + "Level_Number.png");
            Dani_Soul_Number = TxC(DANISELECT + "SoulNumber.png");
            Dani_Exam_Number = TxC(DANISELECT + "ExamNumber.png");
            Dani_Bar_Center = TxC(DANISELECT + "Bar_Center.png");
            Dani_Plate = TxC(DANISELECT + "Plate.png");

            for (int i = 0; i < Challenge_Select.Length; i++)
                Challenge_Select[i] = TxC(DANISELECT + "Challenge_Select_" + i.ToString() + ".png");

            //Dani_Dan_In = TxC(DANISELECT + "Dan_In.png");
            //Dani_Dan_Text = TxC(DANISELECT + "Dan_Text.png");

            Dani_DanPlates = TxC(DANISELECT + "DanPlates.png");
            Dani_DanIcon = TxC(DANISELECT + "DanIcon.png");
            Dani_DanIcon_Fade = TxC(DANISELECT + "DanIcon_Fade.png");
            Dani_DanSides = TxC(DANISELECT + "DanSides.png");

            for (int i = 0; i < Dani_Bloc.Length; i++)
                Dani_Bloc[i] = TxC(DANISELECT + "Bloc" + i.ToString() + ".png");

            #endregion

            #region 4_読み込み画面

            SongLoading_Plate = TxC(SONGLOADING + @"Plate.png");
            SongLoading_Bg = TxC(SONGLOADING + @"Bg.png");
            SongLoading_BgWait = TxC(SONGLOADING + @"Bg_Wait.png");
            SongLoading_Chara = TxC(SONGLOADING + @"Chara.png");
            SongLoading_Fade = TxC(SONGLOADING + @"Fade.png");
            SongLoading_Bg_Dan = TxC(SONGLOADING + @"Bg_Dan.png");

            SongLoading_Plate_AI = TxC(SONGLOADING + @"Plate_AI.png");
            SongLoading_Bg_AI = TxC(SONGLOADING + @"Bg_AI.png");
            SongLoading_Bg_AI_Wait = TxC(SONGLOADING + @"Bg_AI_Wait.png");
            SongLoading_Fade_AI = TxC(SONGLOADING + @"Fade_AI.png");
            SongLoading_Fade_AI_Anime_Base = TxC(SONGLOADING + @"Fade_AI_Anime_Base.png");
            SongLoading_Fade_AI_Anime_Ring = TxC(SONGLOADING + @"Fade_AI_Anime_Ring.png");
            SongLoading_Fade_AI_Anime_NowLoading = TxC(SONGLOADING + @"Fade_AI_Anime_NowLoading.png");
            SongLoading_Fade_AI_Anime_Start = TxC(SONGLOADING + @"Fade_AI_Anime_Start.png");
            SongLoading_Fade_AI_Anime_LoadBar_Base = TxC(SONGLOADING + @"Fade_AI_Anime_LoadBar_Base.png");
            SongLoading_Fade_AI_Anime_LoadBar = TxC(SONGLOADING + @"Fade_AI_Anime_LoadBar.png");

            #endregion

            #region 5_演奏画面

            #region General

            Notes = new CTexture[2];
            Notes[0] = TxC(GAME + @"Notes.png");
            Notes[1] = TxC(GAME + @"Notes_Konga.png");

            Note_Mine = TxC(GAME + @"Mine.png");
            Note_Swap = TxC(GAME + @"Swap.png");
            Note_Kusu = TxC(GAME + @"Kusu.png");
            Note_FuseRoll = TxC(GAME + @"FuseRoll.png");
            Note_Adlib = TxC(GAME + @"Adlib.png");

            Judge_Frame = TxC(GAME + @"Notes.png");

            SENotes = new CTexture[2];
            SENotes[0] = TxC(GAME + @"SENotes.png");
            SENotes[1] = TxC(GAME + @"SENotes_Konga.png");

            SENotesExtension = TxC(GAME + @"SENotes_Extension.png");

            Notes_Arm = TxC(GAME + @"Notes_Arm.png");
            Judge = TxC(GAME + @"Judge.png");
            ChipEffect = TxC(GAME + @"ChipEffect.png");
            ScoreRank = TxC(GAME + @"ScoreRank.png");

            Judge_Meter = TxC(GAME + @"Judge_Meter.png");
            Bar = TxC(GAME + @"Bar.png");
            Bar_Branch = TxC(GAME + @"Bar_Branch.png");

            var _presetsDefs = CSkin.Path(BASE + GAME + BACKGROUND + @"Presets.json");
            if (File.Exists(_presetsDefs))
                TJAPlayer3.Skin.Game_SkinScenes = ConfigManager.GetConfig<DBSkinPreset.SkinPreset>(_presetsDefs);
            else
                TJAPlayer3.Skin.Game_SkinScenes = new DBSkinPreset.SkinPreset();

            #endregion

            #region Mob

            TJAPlayer3.Skin.Game_Mob_Ptn = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + MOB));
            Mob = new CTexture[TJAPlayer3.Skin.Game_Mob_Ptn];
            for (int i = 0; i < TJAPlayer3.Skin.Game_Mob_Ptn; i++)
            {
                Mob[i] = TxC(GAME + MOB + i.ToString() + ".png");
            }

            #endregion

            #region Taiko

            Taiko_Background = new CTexture[12];
            Taiko_Background[0] = TxC(GAME + TAIKO + @"1P_Background.png");
            Taiko_Background[1] = TxC(GAME + TAIKO + @"2P_Background.png");
            Taiko_Background[2] = TxC(GAME + TAIKO + @"Dan_Background.png");
            Taiko_Background[3] = TxC(GAME + TAIKO + @"Tower_Background.png");
            Taiko_Background[4] = TxC(GAME + TAIKO + @"1P_Background_Right.png");
            Taiko_Background[5] = TxC(GAME + TAIKO + @"1P_Background_Tokkun.png");
            Taiko_Background[6] = TxC(GAME + TAIKO + @"2P_Background_Tokkun.png");
            Taiko_Background[7] = TxC(GAME + TAIKO + @"3P_Background.png");
            Taiko_Background[8] = TxC(GAME + TAIKO + @"4P_Background.png");
            Taiko_Background[9] = TxC(GAME + TAIKO + @"AI_Background.png");
            Taiko_Background[10] = TxC(GAME + TAIKO + @"Boss_Background.png");
            Taiko_Background[11] = TxC(GAME + TAIKO + @"5P_Background.png");

            Taiko_Frame = new CTexture[7];
            Taiko_Frame[0] = TxC(GAME + TAIKO + @"1P_Frame.png");
            Taiko_Frame[1] = TxC(GAME + TAIKO + @"2P_Frame.png");
            Taiko_Frame[2] = TxC(GAME + TAIKO + @"Tower_Frame.png");
            Taiko_Frame[3] = TxC(GAME + TAIKO + @"Tokkun_Frame.png");
            Taiko_Frame[4] = TxC(GAME + TAIKO + @"2P_None_Frame.png");
            Taiko_Frame[5] = TxC(GAME + TAIKO + @"AI_Frame.png");
            Taiko_Frame[6] = TxC(GAME + TAIKO + @"4PPlay_Frame.png");

            Taiko_PlayerNumber = new CTexture[5];
            Taiko_PlayerNumber[0] = TxC(GAME + TAIKO + @"1P_PlayerNumber.png");
            Taiko_PlayerNumber[1] = TxC(GAME + TAIKO + @"2P_PlayerNumber.png");
            Taiko_PlayerNumber[2] = TxC(GAME + TAIKO + @"3P_PlayerNumber.png");
            Taiko_PlayerNumber[3] = TxC(GAME + TAIKO + @"4P_PlayerNumber.png");
            Taiko_PlayerNumber[4] = TxC(GAME + TAIKO + @"5P_PlayerNumber.png");


            Taiko_Base = new CTexture[2];
            Taiko_Base[0] = TxC(GAME + TAIKO + @"Base.png");
            Taiko_Base[1] = TxC(GAME + TAIKO + @"Base_Konga.png");

            Taiko_Don_Left = TxC(GAME + TAIKO + @"Don.png");
            Taiko_Don_Right = TxC(GAME + TAIKO + @"Don.png");
            Taiko_Ka_Left = TxC(GAME + TAIKO + @"Ka.png");
            Taiko_Ka_Right = TxC(GAME + TAIKO + @"Ka.png");

            Taiko_Konga_Don = TxC(GAME + TAIKO + @"Don_Konga.png");
            Taiko_Konga_Ka = TxC(GAME + TAIKO + @"Ka_Konga.png");
            Taiko_Konga_Clap = TxC(GAME + TAIKO + @"Clap.png");

            Taiko_LevelUp = TxC(GAME + TAIKO + @"LevelUp.png");
            Taiko_LevelDown = TxC(GAME + TAIKO + @"LevelDown.png");
            Couse_Symbol = new CTexture[(int)Difficulty.Total + 1]; // +1は真打ちモードの分
            Couse_Symbol_Back = new CTexture[(int)Difficulty.Total + 1]; // +1は真打ちモードの分
            Couse_Symbol_Back_Flash = new CTexture[(int)Difficulty.Total + 1]; // +1は真打ちモードの分
            string[] Couse_Symbols = new string[(int)Difficulty.Total + 1] { "Easy", "Normal", "Hard", "Oni", "Edit", "Tower", "Dan", "Shin" };
            for (int i = 0; i < (int)Difficulty.Total + 1; i++)
            {
                Couse_Symbol[i] = TxC(GAME + COURSESYMBOL + Couse_Symbols[i] + ".png");
                Couse_Symbol_Back[i] = TxC(GAME + COURSESYMBOL + Couse_Symbols[i] + "_Back.png");
                Couse_Symbol_Back_Flash[i] = TxC(GAME + COURSESYMBOL + Couse_Symbols[i] + "_Back_Flash.png");
            }

            Taiko_Score = new CTexture[6];
            Taiko_Score[0] = TxC(GAME + TAIKO + @"Score.png");
            Taiko_Score[1] = TxC(GAME + TAIKO + @"Score_1P.png");
            Taiko_Score[2] = TxC(GAME + TAIKO + @"Score_2P.png");
            Taiko_Score[3] = TxC(GAME + TAIKO + @"Score_3P.png");
            Taiko_Score[4] = TxC(GAME + TAIKO + @"Score_4P.png");
            Taiko_Score[5] = TxC(GAME + TAIKO + @"Score_5P.png");
            Taiko_Combo = new CTexture[4];
            Taiko_Combo[0] = TxC(GAME + TAIKO + @"Combo.png");
            Taiko_Combo[1] = TxC(GAME + TAIKO + @"Combo_Big.png");
            Taiko_Combo[2] = TxC(GAME + TAIKO + @"Combo_Midium.png");
            Taiko_Combo[3] = TxC(GAME + TAIKO + @"Combo_Huge.png");
            Taiko_Combo_Effect = TxC(GAME + TAIKO + @"Combo_Effect.png");
            Taiko_Combo_Text = TxC(GAME + TAIKO + @"Combo_Text.png");

            Taiko_Combo_Guide = new CTexture[3];
            for (int i = 0; i < Taiko_Combo_Guide.Length; i++)
            {
                Taiko_Combo_Guide[i] = TxC(GAME + TAIKO + @"Combo_Guide" + i.ToString() + ".png");
            }

            #endregion

            #region Gauge

            Gauge = new CTexture[8];
            Gauge[0] = TxC(GAME + GAUGE + @"1P.png");
            Gauge[1] = TxC(GAME + GAUGE + @"2P.png");
            Gauge[2] = TxC(GAME + GAUGE + @"1P_Right.png");
            Gauge[3] = TxC(GAME + GAUGE + @"1P_4PGauge.png");
            Gauge[4] = TxC(GAME + GAUGE + @"2P_4PGauge.png");
            Gauge[5] = TxC(GAME + GAUGE + @"3P_4PGauge.png");
            Gauge[6] = TxC(GAME + GAUGE + @"4P_4PGauge.png");
            Gauge[7] = TxC(GAME + GAUGE + @"5P_4PGauge.png");

            Gauge_Base = new CTexture[8];
            Gauge_Base[0] = TxC(GAME + GAUGE + @"1P_Base.png");
            Gauge_Base[1] = TxC(GAME + GAUGE + @"2P_Base.png");
            Gauge_Base[2] = TxC(GAME + GAUGE + @"1P_Base_Right.png");
            Gauge_Base[3] = TxC(GAME + GAUGE + @"1P_Base_4PGauge.png");
            Gauge_Base[4] = TxC(GAME + GAUGE + @"2P_Base_4PGauge.png");
            Gauge_Base[5] = TxC(GAME + GAUGE + @"3P_Base_4PGauge.png");
            Gauge_Base[6] = TxC(GAME + GAUGE + @"4P_Base_4PGauge.png");
            Gauge_Base[7] = TxC(GAME + GAUGE + @"5P_Base_4PGauge.png");

            Gauge_Line = new CTexture[2];
            Gauge_Line[0] = TxC(GAME + GAUGE + @"1P_Line.png");
            Gauge_Line[1] = TxC(GAME + GAUGE + @"2P_Line.png");

            Gauge_Clear = new CTexture[3];
            Gauge_Clear[0] = TxC(GAME + GAUGE + @"Clear.png");
            Gauge_Clear[1] = TxC(GAME + GAUGE + @"Clear_2PGauge.png");
            Gauge_Clear[2] = TxC(GAME + GAUGE + @"Clear_4PGauge.png");

            Gauge_Base_Norma = new CTexture[3];
            Gauge_Base_Norma[0] = TxC(GAME + GAUGE + @"Norma_Base.png");
            Gauge_Base_Norma[1] = TxC(GAME + GAUGE + @"Norma_Base_2PGauge.png");
            Gauge_Base_Norma[2] = TxC(GAME + GAUGE + @"Norma_Base_4PGauge.png");

            Gauge_Killzone = new CTexture[3];
            Gauge_Killzone[0] = TxC(GAME + GAUGE + @"Killzone.png");
            Gauge_Killzone[1] = TxC(GAME + GAUGE + @"Killzone_2PGauge.png");
            Gauge_Killzone[2] = TxC(GAME + GAUGE + @"Killzone_4PGauge.png");

            TJAPlayer3.Skin.Game_Gauge_Rainbow_Ptn = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + GAUGE + @"Rainbow\"));
            if (TJAPlayer3.Skin.Game_Gauge_Rainbow_Ptn != 0)
            {
                Gauge_Rainbow = new CTexture[TJAPlayer3.Skin.Game_Gauge_Rainbow_Ptn];
                for (int i = 0; i < TJAPlayer3.Skin.Game_Gauge_Rainbow_Ptn; i++)
                {
                    Gauge_Rainbow[i] = TxC(GAME + GAUGE + @"Rainbow\" + i.ToString() + ".png");
                }
            }

            TJAPlayer3.Skin.Game_Gauge_Rainbow_Flat_Ptn = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + GAUGE + @"Rainbow_Flat\"));
            if (TJAPlayer3.Skin.Game_Gauge_Rainbow_Flat_Ptn != 0)
            {
                Gauge_Rainbow_Flat = new CTexture[TJAPlayer3.Skin.Game_Gauge_Rainbow_Flat_Ptn];
                for (int i = 0; i < TJAPlayer3.Skin.Game_Gauge_Rainbow_Flat_Ptn; i++)
                {
                    Gauge_Rainbow_Flat[i] = TxC(GAME + GAUGE + @"Rainbow_Flat\" + i.ToString() + ".png");
                }
            }

            TJAPlayer3.Skin.Game_Gauge_Rainbow_2PGauge_Ptn = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + GAUGE + @"Rainbow_2PGauge\"));
            if (TJAPlayer3.Skin.Game_Gauge_Rainbow_2PGauge_Ptn != 0)
            {
                Gauge_Rainbow_2PGauge = new CTexture[TJAPlayer3.Skin.Game_Gauge_Rainbow_2PGauge_Ptn];
                for (int i = 0; i < TJAPlayer3.Skin.Game_Gauge_Rainbow_2PGauge_Ptn; i++)
                {
                    Gauge_Rainbow_2PGauge[i] = TxC(GAME + GAUGE + @"Rainbow_2PGauge\" + i.ToString() + ".png");
                }
            }

            // Dan

            TJAPlayer3.Skin.Game_Gauge_Dan_Rainbow_Ptn = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + DANC + @"Rainbow\"));
            if (TJAPlayer3.Skin.Game_Gauge_Dan_Rainbow_Ptn != 0)
            {
                Gauge_Dan_Rainbow = new CTexture[TJAPlayer3.Skin.Game_Gauge_Dan_Rainbow_Ptn];
                for (int i = 0; i < TJAPlayer3.Skin.Game_Gauge_Dan_Rainbow_Ptn; i++)
                {
                    Gauge_Dan_Rainbow[i] = TxC(GAME + DANC + @"Rainbow\" + i.ToString() + ".png");
                }
            }

            Gauge_Dan = new CTexture[6];

            Gauge_Dan[0] = TxC(GAME + GAUGE + @"1P_Dan_Base.png");
            Gauge_Dan[1] = TxC(GAME + GAUGE + @"1P_Dan.png");
            Gauge_Dan[2] = TxC(GAME + GAUGE + @"1P_Dan_Clear_Base.png");
            Gauge_Dan[3] = TxC(GAME + GAUGE + @"1P_Dan_Clear.png");
            Gauge_Dan[4] = TxC(GAME + GAUGE + @"1P_Dan_Base_Right.png");
            Gauge_Dan[5] = TxC(GAME + GAUGE + @"1P_Dan_Right.png");

            Gauge_Soul = TxC(GAME + GAUGE + @"Soul.png");
            Gauge_Flash = TxC(GAME + GAUGE + @"Flash.png");
            Gauge_Soul_Fire = TxC(GAME + GAUGE + @"Fire.png");
            Gauge_Soul_Explosion = new CTexture[2];
            Gauge_Soul_Explosion[0] = TxC(GAME + GAUGE + @"1P_Explosion.png");
            Gauge_Soul_Explosion[1] = TxC(GAME + GAUGE + @"2P_Explosion.png");

            #endregion

            #region Balloon

            Balloon_Combo = new CTexture[2];
            Balloon_Combo[0] = TxC(GAME + BALLOON + @"Combo_1P.png");
            Balloon_Combo[1] = TxC(GAME + BALLOON + @"Combo_2P.png");
            Balloon_Roll = TxC(GAME + BALLOON + @"Roll.png");
            Balloon_Balloon = TxC(GAME + BALLOON + @"Balloon.png");
            Balloon_Number_Roll = TxC(GAME + BALLOON + @"Number_Roll.png");
            Balloon_Number_Combo = TxC(GAME + BALLOON + @"Number_Combo.png");

            Balloon_Breaking = new CTexture[6];
            for (int i = 0; i < 6; i++)
            {
                Balloon_Breaking[i] = TxC(GAME + BALLOON + @"Breaking_" + i.ToString() + ".png");
            }

            Kusudama = TxC(GAME + BALLOON + KUSUDAMA + @"Kusudama.png");
            Kusudama_Clear = TxC(GAME + BALLOON + KUSUDAMA + @"Kusudama_Clear.png");
            Kusudama_Fail = TxC(GAME + BALLOON + KUSUDAMA + @"Kusudama_Fail.png");
            Kusudama_Number = TxC(GAME + BALLOON + KUSUDAMA + @"Kusudama_Number.png");
            Kusudama_Back = TxC(GAME + BALLOON + KUSUDAMA + @"Kusudama_Back.png");

            Fuse_Number = TxC(GAME + BALLOON + FUSE + @"Number_Fuse.png");
            Fuse_Balloon = TxC(GAME + BALLOON + FUSE + @"Fuse.png");

            #endregion

            #region Effects

            Effects_Hit_Explosion = TxCAf(GAME + EFFECTS + @"Hit\Explosion.png");
            if (Effects_Hit_Explosion != null) Effects_Hit_Explosion.b加算合成 = TJAPlayer3.Skin.Game_Effect_HitExplosion_AddBlend;
            Effects_Hit_Explosion_Big = TxC(GAME + EFFECTS + @"Hit\Explosion_Big.png");
            if (Effects_Hit_Explosion_Big != null) Effects_Hit_Explosion_Big.b加算合成 = TJAPlayer3.Skin.Game_Effect_HitExplosionBig_AddBlend;
            Effects_Hit_FireWorks = TxC(GAME + EFFECTS + @"Hit\FireWorks.png");
            if (Effects_Hit_FireWorks != null) Effects_Hit_FireWorks.b加算合成 = TJAPlayer3.Skin.Game_Effect_FireWorks_AddBlend;

            Effects_Hit_Bomb = TxCAf(GAME + EFFECTS + @"Hit\Bomb.png");


            Effects_Fire = TxC(GAME + EFFECTS + @"Fire.png");
            if (Effects_Fire != null) Effects_Fire.b加算合成 = TJAPlayer3.Skin.Game_Effect_Fire_AddBlend;

            Effects_Rainbow = TxC(GAME + EFFECTS + @"Rainbow.png");

            Effects_GoGoSplash = TxC(GAME + EFFECTS + @"GoGoSplash.png");
            if (Effects_GoGoSplash != null) Effects_GoGoSplash.b加算合成 = TJAPlayer3.Skin.Game_Effect_GoGoSplash_AddBlend;
            Effects_Hit_Great = new CTexture[15];
            Effects_Hit_Great_Big = new CTexture[15];
            Effects_Hit_Good = new CTexture[15];
            Effects_Hit_Good_Big = new CTexture[15];
            for (int i = 0; i < 15; i++)
            {
                Effects_Hit_Great[i] = TxC(GAME + EFFECTS + @"Hit\" + @"Great\" + i.ToString() + ".png");
                Effects_Hit_Great_Big[i] = TxC(GAME + EFFECTS + @"Hit\" + @"Great_Big\" + i.ToString() + ".png");
                Effects_Hit_Good[i] = TxC(GAME + EFFECTS + @"Hit\" + @"Good\" + i.ToString() + ".png");
                Effects_Hit_Good_Big[i] = TxC(GAME + EFFECTS + @"Hit\" + @"Good_Big\" + i.ToString() + ".png");
            }
            TJAPlayer3.Skin.Game_Effect_Roll_Ptn = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + EFFECTS + @"Roll\"));
            Effects_Roll = new CTexture[TJAPlayer3.Skin.Game_Effect_Roll_Ptn];
            for (int i = 0; i < TJAPlayer3.Skin.Game_Effect_Roll_Ptn; i++)
            {
                Effects_Roll[i] = TxC(GAME + EFFECTS + @"Roll\" + i.ToString() + ".png");
            }

            #endregion
            
            #region Lane

            Lane_Base = new CTexture[3];
            Lane_Text = new CTexture[3];
            string[] Lanes = new string[3] { "Normal", "Expert", "Master" };
            for (int i = 0; i < 3; i++)
            {
                Lane_Base[i] = TxC(GAME + LANE + "Base_" + Lanes[i] + ".png");
                Lane_Text[i] = TxC(GAME + LANE + "Text_" + Lanes[i] + ".png");
            }

            Lane_Red = new CTexture[2];
            Lane_Blue = new CTexture[2];
            Lane_Clap = new CTexture[2];

            var _suffixes = new string[] { "", "_Konga" };

            for (int i = 0; i < Lane_Red.Length; i++)
            {
                Lane_Red[i] = TxC(GAME + LANE + @"Red" + _suffixes[i] + @".png");
                Lane_Blue[i] = TxC(GAME + LANE + @"Blue" + _suffixes[i] + @".png");
                Lane_Clap[i] = TxC(GAME + LANE + @"Clap" + _suffixes[i] + @".png");
            }
            

            Lane_Yellow = TxC(GAME + LANE + @"Yellow.png");
            Lane_Background_Main = TxC(GAME + LANE + @"Background_Main.png");
            Lane_Background_AI = TxC(GAME + LANE + @"Background_AI.png");
            Lane_Background_Sub = TxC(GAME + LANE + @"Background_Sub.png");
            Lane_Background_GoGo = TxC(GAME + LANE + @"Background_GoGo.png");

            #endregion

            #region 終了演出

            End_Clear_Chara = TxC(GAME + END + @"Clear_Chara.png");
            End_Star = TxC(GAME + END + @"Star.png");

            End_Clear_Text = new CTexture[2];
            End_Clear_Text[0] = TxC(GAME + END + @"Clear_Text.png");
            End_Clear_Text[1] = TxC(GAME + END + @"Clear_Text_End.png");

            End_Clear_L = new CTexture[5];
            End_Clear_R = new CTexture[5];
            for (int i = 0; i < 5; i++)
            {
                End_Clear_L[i] = TxC(GAME + END + @"Clear\" + @"Clear_L_" + i.ToString() + ".png");
                End_Clear_R[i] = TxC(GAME + END + @"Clear\" + @"Clear_R_" + i.ToString() + ".png");
            
            }
            End_Clear_Text_ = TxC(GAME + END + @"Clear\" + @"Clear_Text.png");
            End_Clear_Text_Effect = TxC(GAME + END + @"Clear\" + @"Clear_Text_Effect.png");
            if (End_Clear_Text_Effect != null) End_Clear_Text_Effect.b加算合成 = true;

            ClearFailed = TxC(GAME + END + @"ClearFailed\" + "Clear_Failed.png");
            ClearFailed1 = TxC(GAME + END + @"ClearFailed\" + "Clear_Failed1.png");
            ClearFailed2 = TxC(GAME + END + @"ClearFailed\" + "Clear_Failed2.png");

            End_ClearFailed = new CTexture[26];
            for (int i = 0; i < 26; i++)
                End_ClearFailed[i] = TxC(GAME + END + @"ClearFailed\" + i.ToString() + ".png");

            End_FullCombo = new CTexture[67];
            for (int i = 0; i < 67; i++)
                End_FullCombo[i] = TxC(GAME + END + @"FullCombo\" + i.ToString() + ".png");
            
            End_FullComboLoop = new CTexture[3];
            for (int i = 0; i < 3; i++)
                End_FullComboLoop[i] = TxC(GAME + END + @"FullCombo\" + "loop_" + i.ToString() + ".png");

            End_DondaFullComboBg = TxC(GAME + END + @"DondaFullCombo\" + "bg.png");
            
            End_DondaFullCombo = new CTexture[62];
            for (int i = 0; i < 62; i++)
                End_DondaFullCombo[i] = TxC(GAME + END + @"DondaFullCombo\" + i.ToString() + ".png");

            End_DondaFullComboLoop = new CTexture[3];
            for (int i = 0; i < 3; i++)
                End_DondaFullComboLoop[i] = TxC(GAME + END + @"DondaFullCombo\" + "loop_" + i.ToString() + ".png");


            End_Goukaku = new CTexture[3];

            for (int i = 0; i < End_Goukaku.Length; i++)
            {
                End_Goukaku[i] = TxC(GAME + END + @"Dan" + i.ToString() + ".png");
            }

            #endregion

            #region GameMode

            GameMode_Timer_Tick = TxC(GAME + GAMEMODE + @"Timer_Tick.png");
            GameMode_Timer_Frame = TxC(GAME + GAMEMODE + @"Timer_Frame.png");
            
            #endregion

            #region ClearFailed

            Failed_Game = TxC(GAME + FAILED + @"Game.png");
            Failed_Stage = TxC(GAME + FAILED + @"Stage.png");
            
            #endregion

            #region Runner

            //Runner = TxC(GAME + RUNNER + @"0.png");

            #endregion

            #region DanC

            DanC_Background = TxC(GAME + DANC + @"Background.png");
            DanC_Gauge = new CTexture[4];
            var type = new string[] { "Normal", "Reach", "Clear", "Flush" };
            for (int i = 0; i < 4; i++)
            {
                DanC_Gauge[i] = TxC(GAME + DANC + @"Gauge_" + type[i] + ".png");
            }
            DanC_Base = TxC(GAME + DANC + @"Base.png");
            DanC_Base_Small = TxC(GAME + DANC + @"Base_Small.png");

            DanC_Gauge_Base = TxC(GAME + DANC + @"Gauge_Base.png");
            DanC_Failed = TxC(GAME + DANC + @"Failed.png");
            DanC_Number = TxC(GAME + DANC + @"Number.png");
            DanC_Small_Number = TxC(GAME + DANC + @"Small_Number.png");
            DanC_ExamType = TxC(GAME + DANC + @"ExamType.png");
            DanC_ExamRange = TxC(GAME + DANC + @"ExamRange.png");
            DanC_ExamUnit = TxC(GAME + DANC + @"ExamUnit.png");
            DanC_Screen = TxC(GAME + DANC + @"Screen.png");
            DanC_SmallBase = TxC(GAME + DANC + @"SmallBase.png");
            DanC_Small_ExamCymbol = TxC(GAME + DANC + @"Small_ExamCymbol.png");
            DanC_ExamCymbol = TxC(GAME + DANC + @"ExamCymbol.png");
            DanC_MiniNumber = TxC(GAME + DANC + @"MiniNumber.png");

            #endregion

            #region PuchiChara

            var puchicharaDirs = System.IO.Directory.GetDirectories(TJAPlayer3.strEXEのあるフォルダ + GLOBAL + PUCHICHARA);
            TJAPlayer3.Skin.Puchichara_Ptn = puchicharaDirs.Length;

            Puchichara = new CPuchichara[TJAPlayer3.Skin.Puchichara_Ptn];
            TJAPlayer3.Skin.Puchicharas_Name = new string[TJAPlayer3.Skin.Puchichara_Ptn];

            for (int i = 0; i < TJAPlayer3.Skin.Puchichara_Ptn; i++)
            {
                Puchichara[i] = new CPuchichara(puchicharaDirs[i]);

                TJAPlayer3.Skin.Puchicharas_Name[i] = System.IO.Path.GetFileName(puchicharaDirs[i]);
            }

            ///TJAPlayer3.Skin.Puchichara_Ptn = 5 * Math.Max(1, (PuchiChara.szテクスチャサイズ.Height / 256));


            #endregion

            #region Training

            Tokkun_DownBG = TxC(GAME + TRAINING + @"Down.png");
            Tokkun_BigTaiko = TxC(GAME + TRAINING + @"BigTaiko.png");
            Tokkun_ProgressBar = TxC(GAME + TRAINING + @"ProgressBar_Red.png");
            Tokkun_ProgressBarWhite = TxC(GAME + TRAINING + @"ProgressBar_White.png");
            Tokkun_GoGoPoint = TxC(GAME + TRAINING + @"GoGoPoint.png");
            Tokkun_JumpPoint = TxC(GAME + TRAINING + @"JumpPoint.png");
            Tokkun_Background_Up = TxC(GAME + TRAINING + @"Background_Up.png");
            Tokkun_BigNumber = TxC(GAME + TRAINING + @"BigNumber.png");
            Tokkun_SmallNumber = TxC(GAME + TRAINING + @"SmallNumber.png");
            Tokkun_Speed_Measure = TxC(GAME + TRAINING + @"Speed_Measure.png");
            
            #endregion

            #region [20_Tower]

            Tower_Sky_Gradient = TxC(GAME + TOWER + @"Sky_Gradient.png");

            Tower_Miss = TxC(GAME + TOWER + @"Miss.png");

            // Tower elements
            TJAPlayer3.Skin.Game_Tower_Ptn = System.IO.Directory.GetDirectories(CSkin.Path(BASE + GAME + TOWER + TOWERFLOOR)).Length;
            Tower_Top = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn];
            Tower_Base = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn][];
            Tower_Deco = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn][];

            TJAPlayer3.Skin.Game_Tower_Ptn_Base = new int[TJAPlayer3.Skin.Game_Tower_Ptn];
            TJAPlayer3.Skin.Game_Tower_Ptn_Deco = new int[TJAPlayer3.Skin.Game_Tower_Ptn];

            for (int i = 0; i < TJAPlayer3.Skin.Game_Tower_Ptn; i++)
            {
                TJAPlayer3.Skin.Game_Tower_Ptn_Base[i] = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + TOWER + TOWERFLOOR + i.ToString() + @"\Base\"), "Base");
                TJAPlayer3.Skin.Game_Tower_Ptn_Deco[i] = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + TOWER + TOWERFLOOR + i.ToString() + @"\Deco\"), "Deco");

                Tower_Top[i] = TxC(GAME + TOWER + TOWERFLOOR + i.ToString() + @"\Top.png");

                Tower_Base[i] = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn_Base[i]];
                Tower_Deco[i] = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn_Deco[i]];

                for (int j = 0; j < TJAPlayer3.Skin.Game_Tower_Ptn_Base[i]; j++)
                {
                    Tower_Base[i][j] = TxC(GAME + TOWER + TOWERFLOOR + i.ToString() + @"\Base\Base" + j.ToString() + ".png");
                }

                for (int j = 0; j < TJAPlayer3.Skin.Game_Tower_Ptn_Deco[i]; j++)
                {
                    Tower_Deco[i][j] = TxC(GAME + TOWER + TOWERFLOOR + i.ToString() + @"\Deco\Deco" + j.ToString() + ".png");
                }
            }

            // Tower climbing Don
            TJAPlayer3.Skin.Game_Tower_Ptn_Don = System.IO.Directory.GetDirectories(CSkin.Path(BASE + GAME + TOWER + TOWERDON)).Length;
            Tower_Don_Climbing = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn_Don][];
            Tower_Don_Jump = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn_Don][];
            Tower_Don_Running = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn_Don][];
            Tower_Don_Standing = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn_Don][];

            TJAPlayer3.Skin.Game_Tower_Ptn_Don_Climbing = new int[TJAPlayer3.Skin.Game_Tower_Ptn_Don];
            TJAPlayer3.Skin.Game_Tower_Ptn_Don_Jump = new int[TJAPlayer3.Skin.Game_Tower_Ptn_Don];
            TJAPlayer3.Skin.Game_Tower_Ptn_Don_Running = new int[TJAPlayer3.Skin.Game_Tower_Ptn_Don];
            TJAPlayer3.Skin.Game_Tower_Ptn_Don_Standing = new int[TJAPlayer3.Skin.Game_Tower_Ptn_Don];

            for (int i = 0; i < TJAPlayer3.Skin.Game_Tower_Ptn_Don; i++)
            {
                TJAPlayer3.Skin.Game_Tower_Ptn_Don_Climbing[i] = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + TOWER + TOWERDON + i.ToString() + @"\Climbing\"), "Climbing");
                TJAPlayer3.Skin.Game_Tower_Ptn_Don_Running[i] = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + TOWER + TOWERDON + i.ToString() + @"\Running\"), "Running");
                TJAPlayer3.Skin.Game_Tower_Ptn_Don_Standing[i] = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + TOWER + TOWERDON + i.ToString() + @"\Standing\"), "Standing");
                TJAPlayer3.Skin.Game_Tower_Ptn_Don_Jump[i] = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + GAME + TOWER + TOWERDON + i.ToString() + @"\Jump\"), "Jump");

                Tower_Don_Climbing[i] = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn_Don_Climbing[i]];
                Tower_Don_Running[i] = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn_Don_Running[i]];
                Tower_Don_Standing[i] = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn_Don_Standing[i]];
                Tower_Don_Jump[i] = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn_Don_Jump[i]];

                for (int j = 0; j < TJAPlayer3.Skin.Game_Tower_Ptn_Don_Climbing[i]; j++)
                {
                    Tower_Don_Climbing[i][j] = TxC(GAME + TOWER + TOWERDON + i.ToString() + @"\Climbing\Climbing" + j.ToString() + ".png");
                }

                for (int j = 0; j < TJAPlayer3.Skin.Game_Tower_Ptn_Don_Running[i]; j++)
                {
                    Tower_Don_Running[i][j] = TxC(GAME + TOWER + TOWERDON + i.ToString() + @"\Running\Running" + j.ToString() + ".png");
                }

                for (int j = 0; j < TJAPlayer3.Skin.Game_Tower_Ptn_Don_Standing[i]; j++)
                {
                    Tower_Don_Standing[i][j] = TxC(GAME + TOWER + TOWERDON + i.ToString() + @"\Standing\Standing" + j.ToString() + ".png");
                }

                for (int j = 0; j < TJAPlayer3.Skin.Game_Tower_Ptn_Don_Jump[i]; j++)
                {
                    Tower_Don_Jump[i][j] = TxC(GAME + TOWER + TOWERDON + i.ToString() + @"\Jump\Jump" + j.ToString() + ".png");
                }
            }

            #endregion

            #region [21_ModIcons]

            HiSp = new CTexture[14];
            for (int i = 0; i < HiSp.Length; i++)
            {
                HiSp[i] = TxC(GAME + MODICONS + @"HS\" + i.ToString() + @".png");
            }

            Mod_Timing = new CTexture[5];
            for (int i = 0; i < Mod_Timing.Length; i++)
            {
                Mod_Timing[i] = TxC(GAME + MODICONS + @"Timing\" + i.ToString() + @".png");
            }

            Mod_SongSpeed = new CTexture[2];
            for (int i = 0; i < Mod_SongSpeed.Length; i++)
            {
                Mod_SongSpeed[i] = TxC(GAME + MODICONS + @"SongSpeed\" + i.ToString() + @".png");
            }

            Mod_Fun = new CTexture[3];
            for (int i = 0; i < Mod_Fun.Length; i++)
            {
                Mod_Fun[i] = TxC(GAME + MODICONS + @"Fun\" + i.ToString() + @".png");
            }

            Mod_Doron = TxC(GAME + MODICONS + @"Doron.png");
            Mod_Stealth = TxC(GAME + MODICONS + @"Stealth.png");
            Mod_Mirror = TxC(GAME + MODICONS + @"Mirror.png");
            Mod_Super = TxC(GAME + MODICONS + @"Super.png");
            Mod_Hyper = TxC(GAME + MODICONS + @"Hyper.png");
            Mod_Random = TxC(GAME + MODICONS + @"Random.png");
            Mod_Auto = TxC(GAME + MODICONS + @"Auto.png");
            Mod_Just = TxC(GAME + MODICONS + @"Just.png");
            Mod_Safe = TxC(GAME + MODICONS + @"Safe.png");
            Mod_None = TxC(GAME + MODICONS + @"None.png");

            #endregion

            #region [22_AIBattle]

            AIBattle_SectionTime_Panel = TxC(GAME + AIBATTLE + @"SectionTime_Panel.png");

            AIBattle_SectionTime_Bar_Base = TxC(GAME + AIBATTLE + @"SectionTime_Bar_Base.png");
            AIBattle_SectionTime_Bar_Finish = TxC(GAME + AIBATTLE + @"SectionTime_Bar_Finish.png");
            AIBattle_SectionTime_Bar_Normal = TxC(GAME + AIBATTLE + @"SectionTime_Bar_Normal.png");

            AIBattle_Batch_Base = TxC(GAME + AIBATTLE + @"Batch_Base.png");
            AIBattle_Batch = TxC(GAME + AIBATTLE + @"Batch.png");

            AIBattle_Judge_Meter[0] = TxC(GAME + AIBATTLE + @"Judge_Meter.png");
            AIBattle_Judge_Meter[1] = TxC(GAME + AIBATTLE + @"Judge_Meter_AI.png");

            AIBattle_Judge_Number = TxC(GAME + AIBATTLE + @"Judge_Number.png");

            #endregion

            #endregion

            #region 6_結果発表
            Result_FadeIn = TxC(RESULT + @"FadeIn.png");

            Result_Gauge[0] = TxC(RESULT + @"Gauge.png");
            Result_Gauge_Base[0] = TxC(RESULT + @"Gauge_Base.png");
            Result_Gauge[1] = TxC(RESULT + @"Gauge_2.png");
            Result_Gauge_Base[1] = TxC(RESULT + @"Gauge_Base_2.png");
            Result_Gauge[2] = TxC(RESULT + @"Gauge_3.png");
            Result_Gauge_Base[2] = TxC(RESULT + @"Gauge_Base_3.png");
            Result_Gauge[3] = TxC(RESULT + @"Gauge_4.png");
            Result_Gauge_Base[3] = TxC(RESULT + @"Gauge_Base_4.png");
            Result_Gauge[4] = TxC(RESULT + @"Gauge_5.png");
            Result_Gauge_Base[4] = TxC(RESULT + @"Gauge_Base_5.png");

            Result_Gauge_Frame = TxC(RESULT + @"Gauge_Frame.png");
            Result_Gauge_Clear = TxC(RESULT + @"Gauge_Clear.png");
            Result_Gauge_Clear_Base = TxC(RESULT + @"Gauge_Clear_Base.png");
            Result_Gauge_Killzone = TxC(RESULT + @"Gauge_Killzone.png");

            Result_Header = TxC(RESULT + @"Header.png");
            Result_Number = TxC(RESULT + @"Number.png");
            Result_Panel = TxC(RESULT + @"Panel.png");
            Result_Panel_2P = TxC(RESULT + @"Panel_2.png");
            Result_Soul_Text = TxC(RESULT + @"Soul_Text.png");
            Result_Soul_Fire = TxC(RESULT + @"Result_Soul_Fire.png");
            Result_Diff_Bar = TxC(RESULT + @"DifficultyBar.png");
            Result_Score_Number = TxC(RESULT + @"Score_Number.png");
            Result_Dan = TxC(RESULT + @"Dan.png");

            Result_CrownEffect = TxC(RESULT + @"CrownEffect.png");
            Result_ScoreRankEffect = TxC(RESULT + @"ScoreRankEffect.png");
            //Result_Cloud = TxC(RESULT + @"Cloud.png");
            Result_Shine = TxC(RESULT + @"Shine.png");

            Result_Speech_Bubble[0] = TxC(RESULT + @"Speech_Bubble.png");
            Result_Speech_Bubble[1] = TxC(RESULT + @"Speech_Bubble_2.png");

            Result_Flower = TxC(RESULT + @"Flower\Flower.png");

            for (int i = 0; i < 4; i++)
                Result_Panel_4P[i] = TxC(RESULT + @"Panel_4P_" + (i + 1).ToString() + ".png");

            for (int i = 0; i < 5; i++)
                Result_Panel_5P[i] = TxC(RESULT + @"Panel_5P_" + (i + 1).ToString() + ".png");

            for (int i = 1; i <= 5; i++)
                Result_Flower_Rotate[i - 1] = TxC(RESULT + @"Flower\Rotate_" + i.ToString() + ".png");

            //for (int i = 0; i < 3; i++)
                //Result_Work[i] = TxC(RESULT + @"Work\" + i.ToString() + ".png");


            for (int i = 0; i < 41; i++)
                Result_Rainbow[i] = TxC(RESULT + @"Rainbow\" + i.ToString() + ".png");

            //for (int i = 0; i < 6; i++)
                //Result_Background[i] = TxC(RESULT + @"Background_" + i.ToString() + ".png");

            //for (int i = 0; i < 4; i++)
                //Result_Mountain[i] = TxC(RESULT + @"Background_Mountain_" + i.ToString() + ".png");

            for (int i = 0; i < 3; i++)
                Result_Crown[i] = TxC(RESULT + @"Crown\Crown_" + i.ToString() + ".png");

            #endregion

            #region 7_終了画面
            //Exit_Background = TxC(EXIT + @"Background.png");
            #endregion

            #region 7_AIResults

            Result_AIBattle_Panel_AI = TxC(RESULT + @"AIBattle\Panel_AI.png");
            Result_AIBattle_Batch = TxC(RESULT + @"AIBattle\Batch.png");
            Result_AIBattle_SectionPlate = TxC(RESULT + @"AIBattle\SectionPlate.png");
            Result_AIBattle_WinFlag_Clear = TxC(RESULT + @"AIBattle\WinFlag_Win.png");
            Result_AIBattle_WinFlag_Lose = TxC(RESULT + @"AIBattle\WinFlag_Lose.png");

            #endregion

            #region [7_DanResults]

            //DanResult_Background = TxC(DANRESULT + @"Background.png");
            DanResult_Rank = TxC(DANRESULT + @"Rank.png");
            DanResult_SongPanel_Base = TxC(DANRESULT + @"SongPanel_Base.png");
            DanResult_StatePanel_Base = TxC(DANRESULT + @"StatePanel_Base.png");
            DanResult_SongPanel_Main = TxC(DANRESULT + @"SongPanel_Main.png");
            DanResult_StatePanel_Main = TxC(DANRESULT + @"StatePanel_Main.png");

            #endregion

            #region [8_TowerResults]

            TJAPlayer3.Skin.Game_Tower_Ptn_Result = TJAPlayer3.t連番画像の枚数を数える(CSkin.Path(BASE + TOWERRESULT + @"Tower\"));
            TowerResult_Tower = new CTexture[TJAPlayer3.Skin.Game_Tower_Ptn_Result];

            TowerResult_Background = TxC(TOWERRESULT + @"Background.png");
            TowerResult_Panel = TxC(TOWERRESULT + @"Panel.png");

            TowerResult_ScoreRankEffect = TxC(TOWERRESULT + @"ScoreRankEffect.png");

            for (int i = 0; i < TJAPlayer3.Skin.Game_Tower_Ptn_Result; i++)
            {
                TowerResult_Tower[i] = TxC(TOWERRESULT + @"Tower\" + i.ToString() + ".png");
            }

            #endregion

            #region [10_Heya]

            //Heya_Background = TxC(HEYA + @"Background.png");
            Heya_Center_Menu_Bar = TxC(HEYA + @"Center_Menu_Bar.png");
            Heya_Center_Menu_Box = TxC(HEYA + @"Center_Menu_Box.png");
            Heya_Center_Menu_Box_Slot = TxC(HEYA + @"Center_Menu_Box_Slot.png");
            Heya_Side_Menu = TxC(HEYA + @"Side_Menu.png");
            Heya_Render_Field = TxC(HEYA + @"Render_Field.png");
            Heya_Box = TxC(HEYA + @"Box.png");
            Heya_Lock = TxC(HEYA + @"Lock.png");

            #endregion

            #region [11_Characters]

            #region [Character count initialisations]

            var charaDirs = System.IO.Directory.GetDirectories(TJAPlayer3.strEXEのあるフォルダ + GLOBAL + CHARACTERS);
            TJAPlayer3.Skin.Characters_Ptn = charaDirs.Length;

            Characters_Heya_Preview = new CTexture[TJAPlayer3.Skin.Characters_Ptn];
            Characters_Heya_Render = new CTexture[TJAPlayer3.Skin.Characters_Ptn];
            Characters = new CCharacter[TJAPlayer3.Skin.Characters_Ptn];

            Characters_Normal = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Normal_Missed = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Normal_MissedDown = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Normal_Cleared = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Normal_Maxed = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_GoGoTime = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_GoGoTime_Maxed = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_10Combo = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_10Combo_Maxed = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_GoGoStart = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_GoGoStart_Maxed = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Become_Cleared = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Become_Maxed = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Return = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Balloon_Breaking = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Balloon_Broke = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Balloon_Miss = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Title_Entry = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Title_Normal = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Result_Clear = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Result_Failed = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Result_Failed_In = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Result_Normal = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Menu_Loop = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Menu_Start = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];
            Characters_Menu_Select = new CTexture[TJAPlayer3.Skin.Characters_Ptn][];

            TJAPlayer3.Skin.Characters_DirName = new string[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Normal_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Normal_Missed_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Normal_MissedDown_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Normal_Cleared_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Normal_Maxed_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_GoGoTime_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_GoGoTime_Maxed_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_10Combo_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_10Combo_Maxed_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_GoGoStart_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_GoGoStart_Maxed_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Become_Cleared_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Become_Maxed_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Return_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Balloon_Breaking_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Balloon_Broke_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Balloon_Miss_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Title_Entry_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Title_Normal_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Result_Clear_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Result_Failed_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Result_Failed_In_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Result_Normal_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Menu_Loop_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Menu_Start_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Menu_Select_Ptn = new int[TJAPlayer3.Skin.Characters_Ptn];

            TJAPlayer3.Skin.Characters_Resolution = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Heya_Render_Offset = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_X = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Y = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_4P = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_5P = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_X_AI = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Y_AI = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Balloon_X = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Balloon_Y = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Balloon_4P = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Balloon_5P = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_Normal = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_10Combo = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_10ComboMax = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_Miss = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_MissDown = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_ClearIn = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_Clear = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_ClearMax = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_GoGoStart = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_GoGoStartMax = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_GoGo = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_GoGoMax = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_SoulIn = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Motion_Return = new int[TJAPlayer3.Skin.Characters_Ptn][];
            TJAPlayer3.Skin.Characters_Beat_Normal = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_Miss = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_MissDown = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_Clear = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_GoGo = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_10Combo = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_10ComboMax = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_ClearIn = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_ClearMax = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_GoGoStart = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_GoGoStartMax = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_GoGoMax = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_SoulIn = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Beat_Return = new float[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Balloon_Timer = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Balloon_Delay = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Balloon_FadeOut = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Title_Entry_AnimationDuration = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Title_Normal_AnimationDuration = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Menu_Loop_AnimationDuration = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Menu_Select_AnimationDuration = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Menu_Start_AnimationDuration = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Result_Normal_AnimationDuration = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Result_Clear_AnimationDuration = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Result_Failed_In_AnimationDuration = new int[TJAPlayer3.Skin.Characters_Ptn];
            TJAPlayer3.Skin.Characters_Result_Failed_AnimationDuration = new int[TJAPlayer3.Skin.Characters_Ptn];

            for (int i = 0; i < charaDirs.Length; i++)
            {
                TJAPlayer3.Skin.Characters_DirName[i] = System.IO.Path.GetFileName(charaDirs[i]);
            }

            #endregion

            for (int i = 0; i < 5; i++)
            {
                TJAPlayer3.SaveFileInstances[i].tReindexCharacter(TJAPlayer3.Skin.Characters_DirName);
                this.ReloadCharacter(-1, TJAPlayer3.SaveFileInstances[i].data.Character, i, true);
            }
                

            for (int i = 0; i < TJAPlayer3.Skin.Characters_Ptn; i++)
            {
                Characters_Heya_Preview[i] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Normal\0.png");
                Characters_Heya_Render[i] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Render.png");

                TJAPlayer3.Skin.Characters_Resolution[i] = new int[] { 1280, 720 };
                TJAPlayer3.Skin.Characters_Heya_Render_Offset[i] = new int[] { 0, 0 };

                var _str = "";
                TJAPlayer3.Skin.LoadSkinConfigFromFile(charaDirs[i] + @"\CharaConfig.txt", ref _str);

                string[] delimiter = { "\n", "\r" };
                string[] strSingleLine = _str.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in strSingleLine)
                {
                    if (line.StartsWith("Chara_Resolution=")) // required for Heya resolution compatibility
                    {
                        string[] values = line.Substring(17).Trim().Split(',');
                        TJAPlayer3.Skin.Characters_Resolution[i][0] = int.Parse(values[0]);
                        TJAPlayer3.Skin.Characters_Resolution[i][1] = int.Parse(values[1]);
                    }
                    else if (line.StartsWith("Heya_Chara_Render_Offset="))
                    {
                        string[] values = line.Substring(25).Trim().Split(',');
                        TJAPlayer3.Skin.Characters_Heya_Render_Offset[i][0] = int.Parse(values[0]);
                        TJAPlayer3.Skin.Characters_Heya_Render_Offset[i][1] = int.Parse(values[1]);
                    }
                }

                Characters[i] = new CCharacter(charaDirs[i]);
            }
                

            #endregion

            #region [11_Modals]

            Modal_Full = new CTexture[6];
            Modal_Half = new CTexture[6];
            Modal_Half_4P = new CTexture[6];
            Modal_Half_5P = new CTexture[6];
            for (int i = 0; i < 5; i++)
            {
                Modal_Full[i] = TxC(MODALS + i.ToString() + @"_full.png");
                Modal_Half[i] = TxC(MODALS + i.ToString() + @"_half.png");
                Modal_Half_4P[i] = TxC(MODALS + i.ToString() + @"_half_4P.png");
                Modal_Half_5P[i] = TxC(MODALS + i.ToString() + @"_half_5P.png");
            }
            Modal_Full[Modal_Full.Length - 1] = TxC(MODALS + @"Coin_full.png");
            Modal_Half[Modal_Full.Length - 1] = TxC(MODALS + @"Coin_half.png");
            Modal_Half_4P[Modal_Full.Length - 1] = TxC(MODALS + @"Coin_half_4P.png");
            Modal_Half_5P[Modal_Full.Length - 1] = TxC(MODALS + @"Coin_half_5P.png");

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

            #region [15_OpenEncyclopedia]

            //OpenEncyclopedia_Background = TxC(OPENENCYCLOPEDIA + @"Background.png");
            OpenEncyclopedia_Context = TxC(OPENENCYCLOPEDIA + @"Context.png");
            OpenEncyclopedia_Side_Menu = TxC(OPENENCYCLOPEDIA + @"Side_Menu.png");
            OpenEncyclopedia_Return_Box = TxC(OPENENCYCLOPEDIA + @"Return_Box.png");

            #endregion

        }

        public int[] CreateNumberedArrayFromInt(int total)
        {
            int[] array = new int[total];
            for (int i = 0; i < total; i++)
            {
                array[i] = i;
            }
            return array;
        }

        public CSkin.Cシステムサウンド VoiceSelectOggOrWav(string basePath)
        {
            if (File.Exists(basePath + @".ogg"))
                return new CSkin.Cシステムサウンド(basePath + @".ogg", false, false, true, ESoundGroup.Voice);
            else
                return new CSkin.Cシステムサウンド(basePath + @".wav", false, false, true, ESoundGroup.Voice);
        }

        public void ReloadCharacter(int old, int newC, int player, bool primary = false)
        {
            if (old == newC)
                return;

            if (old >= 0 && 
                (TJAPlayer3.SaveFileInstances[0].data.Character != old || player == 0) &&
                (TJAPlayer3.SaveFileInstances[1].data.Character != old || player == 1) &&
                (TJAPlayer3.SaveFileInstances[2].data.Character != old || player == 2) &&
                (TJAPlayer3.SaveFileInstances[3].data.Character != old || player == 3) &&
                (TJAPlayer3.SaveFileInstances[4].data.Character != old || player == 4))
            {
                int i = old;

                #region [Dispose the previous character]

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Menu_Loop_Ptn[i]; j++)
                    Characters_Menu_Loop[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Menu_Start_Ptn[i]; j++)
                    Characters_Menu_Start[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Menu_Select_Ptn[i]; j++)
                    Characters_Menu_Select[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Result_Normal_Ptn[i]; j++)
                    Characters_Result_Normal[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Result_Failed_In_Ptn[i]; j++)
                    Characters_Result_Failed_In[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Result_Failed_Ptn[i]; j++)
                    Characters_Result_Failed[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Result_Clear_Ptn[i]; j++)
                    Characters_Result_Clear[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Title_Normal_Ptn[i]; j++)
                    Characters_Title_Normal[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Title_Entry_Ptn[i]; j++)
                    Characters_Title_Entry[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Normal_Ptn[i]; j++)
                    Characters_Normal[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Normal_Missed_Ptn[i]; j++)
                    Characters_Normal_Missed[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Normal_MissedDown_Ptn[i]; j++)
                    Characters_Normal_MissedDown[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Normal_Cleared_Ptn[i]; j++)
                    Characters_Normal_Cleared[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Normal_Maxed_Ptn[i]; j++)
                    Characters_Normal_Maxed[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_GoGoTime_Ptn[i]; j++)
                    Characters_GoGoTime[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_GoGoTime_Maxed_Ptn[i]; j++)
                    Characters_GoGoTime_Maxed[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_GoGoStart_Ptn[i]; j++)
                    Characters_GoGoStart[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_GoGoStart_Maxed_Ptn[i]; j++)
                    Characters_GoGoStart_Maxed[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_10Combo_Ptn[i]; j++)
                    Characters_10Combo[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_10Combo_Maxed_Ptn[i]; j++)
                    Characters_10Combo_Maxed[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Become_Cleared_Ptn[i]; j++)
                    Characters_Become_Cleared[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Become_Maxed_Ptn[i]; j++)
                    Characters_Become_Maxed[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Return_Ptn[i]; j++)
                    Characters_Return[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Balloon_Breaking_Ptn[i]; j++)
                    Characters_Balloon_Breaking[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Balloon_Broke_Ptn[i]; j++)
                    Characters_Balloon_Broke[i][j]?.Dispose();

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Balloon_Miss_Ptn[i]; j++)
                    Characters_Balloon_Miss[i][j]?.Dispose();

                #endregion
            }

            string charaPath = TJAPlayer3.strEXEのあるフォルダ + GLOBAL + CHARACTERS + TJAPlayer3.Skin.Characters_DirName[newC];

            if ((newC >= 0 &&
                TJAPlayer3.SaveFileInstances[0].data.Character != newC &&
                TJAPlayer3.SaveFileInstances[1].data.Character != newC &&
                TJAPlayer3.SaveFileInstances[2].data.Character != newC &&
                TJAPlayer3.SaveFileInstances[3].data.Character != newC &&
                TJAPlayer3.SaveFileInstances[4].data.Character != newC) || primary)
            {
                int i = newC;

                #region [Allocate the new character]

                #region [Character individual values count initialisation]

                TJAPlayer3.Skin.Characters_Normal_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Normal\");
                TJAPlayer3.Skin.Characters_Normal_Missed_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Miss\");
                TJAPlayer3.Skin.Characters_Normal_MissedDown_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\MissDown\");
                TJAPlayer3.Skin.Characters_Normal_Cleared_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Clear\");
                TJAPlayer3.Skin.Characters_Normal_Maxed_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Clear_Max\");
                TJAPlayer3.Skin.Characters_GoGoTime_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\GoGo\");
                TJAPlayer3.Skin.Characters_GoGoTime_Maxed_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\GoGo_Max\");
                TJAPlayer3.Skin.Characters_10Combo_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\10combo\");
                TJAPlayer3.Skin.Characters_10Combo_Maxed_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\10combo_Max\");
                TJAPlayer3.Skin.Characters_GoGoStart_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\GoGoStart\");
                TJAPlayer3.Skin.Characters_GoGoStart_Maxed_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\GoGoStart_Max\");
                TJAPlayer3.Skin.Characters_Become_Cleared_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Clearin\");
                TJAPlayer3.Skin.Characters_Become_Maxed_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Soulin\");
                TJAPlayer3.Skin.Characters_Return_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Return\");
                TJAPlayer3.Skin.Characters_Balloon_Breaking_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Balloon_Breaking\");
                TJAPlayer3.Skin.Characters_Balloon_Broke_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Balloon_Broke\");
                TJAPlayer3.Skin.Characters_Balloon_Miss_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Balloon_Miss\");
                TJAPlayer3.Skin.Characters_Title_Entry_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Title_Entry\");
                TJAPlayer3.Skin.Characters_Title_Normal_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Title_Normal\");
                TJAPlayer3.Skin.Characters_Menu_Loop_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Menu_Loop\");
                TJAPlayer3.Skin.Characters_Menu_Select_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Menu_Select\");
                TJAPlayer3.Skin.Characters_Menu_Start_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Menu_Start\");
                TJAPlayer3.Skin.Characters_Result_Clear_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Result_Clear\");
                TJAPlayer3.Skin.Characters_Result_Failed_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Result_Failed\");
                TJAPlayer3.Skin.Characters_Result_Failed_In_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Result_Failed_In\");
                TJAPlayer3.Skin.Characters_Result_Normal_Ptn[i] = TJAPlayer3.t連番画像の枚数を数える(charaPath + @"\Result_Normal\");

                Characters_Normal[i] = new CTexture[TJAPlayer3.Skin.Characters_Normal_Ptn[i]];
                Characters_Normal_Missed[i] = new CTexture[TJAPlayer3.Skin.Characters_Normal_Missed_Ptn[i]];
                Characters_Normal_MissedDown[i] = new CTexture[TJAPlayer3.Skin.Characters_Normal_MissedDown_Ptn[i]];
                Characters_Normal_Cleared[i] = new CTexture[TJAPlayer3.Skin.Characters_Normal_Cleared_Ptn[i]];
                Characters_Normal_Maxed[i] = new CTexture[TJAPlayer3.Skin.Characters_Normal_Maxed_Ptn[i]];
                Characters_GoGoTime[i] = new CTexture[TJAPlayer3.Skin.Characters_GoGoTime_Ptn[i]];
                Characters_GoGoTime_Maxed[i] = new CTexture[TJAPlayer3.Skin.Characters_GoGoTime_Maxed_Ptn[i]];
                Characters_10Combo[i] = new CTexture[TJAPlayer3.Skin.Characters_10Combo_Ptn[i]];
                Characters_10Combo_Maxed[i] = new CTexture[TJAPlayer3.Skin.Characters_10Combo_Maxed_Ptn[i]];
                Characters_GoGoStart[i] = new CTexture[TJAPlayer3.Skin.Characters_GoGoStart_Ptn[i]];
                Characters_GoGoStart_Maxed[i] = new CTexture[TJAPlayer3.Skin.Characters_GoGoStart_Maxed_Ptn[i]];
                Characters_Become_Cleared[i] = new CTexture[TJAPlayer3.Skin.Characters_Become_Cleared_Ptn[i]];
                Characters_Become_Maxed[i] = new CTexture[TJAPlayer3.Skin.Characters_Become_Maxed_Ptn[i]];
                Characters_Return[i] = new CTexture[TJAPlayer3.Skin.Characters_Return_Ptn[i]];
                Characters_Balloon_Breaking[i] = new CTexture[TJAPlayer3.Skin.Characters_Balloon_Breaking_Ptn[i]];
                Characters_Balloon_Broke[i] = new CTexture[TJAPlayer3.Skin.Characters_Balloon_Broke_Ptn[i]];
                Characters_Balloon_Miss[i] = new CTexture[TJAPlayer3.Skin.Characters_Balloon_Miss_Ptn[i]];
                Characters_Title_Entry[i] = new CTexture[TJAPlayer3.Skin.Characters_Title_Entry_Ptn[i]];
                Characters_Title_Normal[i] = new CTexture[TJAPlayer3.Skin.Characters_Title_Normal_Ptn[i]];
                Characters_Result_Clear[i] = new CTexture[TJAPlayer3.Skin.Characters_Result_Clear_Ptn[i]];
                Characters_Result_Failed[i] = new CTexture[TJAPlayer3.Skin.Characters_Result_Failed_Ptn[i]];
                Characters_Result_Failed_In[i] = new CTexture[TJAPlayer3.Skin.Characters_Result_Failed_In_Ptn[i]];
                Characters_Result_Normal[i] = new CTexture[TJAPlayer3.Skin.Characters_Result_Normal_Ptn[i]];
                Characters_Menu_Loop[i] = new CTexture[TJAPlayer3.Skin.Characters_Menu_Loop_Ptn[i]];
                Characters_Menu_Start[i] = new CTexture[TJAPlayer3.Skin.Characters_Menu_Start_Ptn[i]];
                Characters_Menu_Select[i] = new CTexture[TJAPlayer3.Skin.Characters_Menu_Select_Ptn[i]];

                #endregion

                #region [Characters asset loading]

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Menu_Loop_Ptn[i]; j++)
                    Characters_Menu_Loop[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Menu_Loop\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Menu_Select_Ptn[i]; j++)
                    Characters_Menu_Select[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Menu_Select\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Menu_Start_Ptn[i]; j++)
                    Characters_Menu_Start[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Menu_Start\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Result_Normal_Ptn[i]; j++)
                    Characters_Result_Normal[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Result_Normal\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Result_Failed_In_Ptn[i]; j++)
                    Characters_Result_Failed_In[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Result_Failed_In\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Result_Failed_Ptn[i]; j++)
                    Characters_Result_Failed[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Result_Failed\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Result_Clear_Ptn[i]; j++)
                    Characters_Result_Clear[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Result_Clear\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Title_Normal_Ptn[i]; j++)
                    Characters_Title_Normal[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Title_Normal\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Title_Entry_Ptn[i]; j++)
                    Characters_Title_Entry[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Title_Entry\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Normal_Ptn[i]; j++)
                    Characters_Normal[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Normal\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Normal_Missed_Ptn[i]; j++)
                    Characters_Normal_Missed[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Miss\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Normal_MissedDown_Ptn[i]; j++)
                    Characters_Normal_MissedDown[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\MissDown\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Normal_Cleared_Ptn[i]; j++)
                    Characters_Normal_Cleared[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Clear\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Normal_Maxed_Ptn[i]; j++)
                    Characters_Normal_Maxed[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Clear_Max\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_GoGoTime_Ptn[i]; j++)
                    Characters_GoGoTime[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\GoGo\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_GoGoTime_Maxed_Ptn[i]; j++)
                    Characters_GoGoTime_Maxed[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\GoGo_Max\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_GoGoStart_Ptn[i]; j++)
                    Characters_GoGoStart[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\GoGoStart\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_GoGoStart_Maxed_Ptn[i]; j++)
                    Characters_GoGoStart_Maxed[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\GoGoStart_Max\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_10Combo_Ptn[i]; j++)
                    Characters_10Combo[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\10combo\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_10Combo_Maxed_Ptn[i]; j++)
                    Characters_10Combo_Maxed[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\10combo_Max\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Become_Cleared_Ptn[i]; j++)
                    Characters_Become_Cleared[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Clearin\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Become_Maxed_Ptn[i]; j++)
                    Characters_Become_Maxed[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Soulin\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Return_Ptn[i]; j++)
                    Characters_Return[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Return\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Balloon_Breaking_Ptn[i]; j++)
                    Characters_Balloon_Breaking[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Balloon_Breaking\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Balloon_Broke_Ptn[i]; j++)
                    Characters_Balloon_Broke[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Balloon_Broke\" + j.ToString() + @".png");

                for (int j = 0; j < TJAPlayer3.Skin.Characters_Balloon_Miss_Ptn[i]; j++)
                    Characters_Balloon_Miss[i][j] = TxCGlobal(CHARACTERS + TJAPlayer3.Skin.Characters_DirName[i] + @"\Balloon_Miss\" + j.ToString() + @".png");

                #endregion

                #region [Parse individual character parameters]

                #region [Default values]

                TJAPlayer3.Skin.Characters_X[i] = new int[] { 0, 0 };
                TJAPlayer3.Skin.Characters_Y[i] = new int[] { 0, 537 };
                TJAPlayer3.Skin.Characters_4P[i] = new int[] { 165, 68 };
                TJAPlayer3.Skin.Characters_5P[i] = new int[] { 165, 40 };
                TJAPlayer3.Skin.Characters_X_AI[i] = new int[] { 472, 602 };
                TJAPlayer3.Skin.Characters_Y_AI[i] = new int[] { 152, 152 };
                TJAPlayer3.Skin.Characters_Balloon_X[i] = new int[] { 240, 240, 0, 0 };
                TJAPlayer3.Skin.Characters_Balloon_Y[i] = new int[] { 0, 297, 0, 0 };
                TJAPlayer3.Skin.Characters_Balloon_4P[i] = new int[] { 0, -176 };
                TJAPlayer3.Skin.Characters_Balloon_5P[i] = new int[] { 0, -168 };
                TJAPlayer3.Skin.Characters_Motion_Normal[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_Normal_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_10Combo[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_10Combo_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_10ComboMax[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_10Combo_Maxed_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_Miss[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_Normal_Missed_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_MissDown[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_Normal_MissedDown_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_ClearIn[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_Become_Cleared_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_Clear[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_Normal_Cleared_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_ClearMax[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_Normal_Maxed_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_GoGoStart[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_GoGoStart_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_GoGoStartMax[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_GoGoStart_Maxed_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_GoGo[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_GoGoTime_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_GoGoMax[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_GoGoTime_Maxed_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_SoulIn[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_Become_Maxed_Ptn[i]);
                TJAPlayer3.Skin.Characters_Motion_Return[i] = CreateNumberedArrayFromInt(TJAPlayer3.Skin.Characters_Return_Ptn[i]);
                TJAPlayer3.Skin.Characters_Beat_Normal[i] = 1;
                TJAPlayer3.Skin.Characters_Beat_Miss[i] = 1;
                TJAPlayer3.Skin.Characters_Beat_MissDown[i] = 1;
                TJAPlayer3.Skin.Characters_Beat_Clear[i] = 2;
                TJAPlayer3.Skin.Characters_Beat_GoGo[i] = 2;
                TJAPlayer3.Skin.Characters_Beat_10Combo[i] = 1.5f;
                TJAPlayer3.Skin.Characters_Beat_10ComboMax[i] = 1.5f;
                TJAPlayer3.Skin.Characters_Beat_ClearIn[i] = 1.5f;
                TJAPlayer3.Skin.Characters_Beat_ClearMax[i] = 1.5f;
                TJAPlayer3.Skin.Characters_Beat_GoGoStart[i] = 1.5f;
                TJAPlayer3.Skin.Characters_Beat_GoGoStartMax[i] = 1.5f;
                TJAPlayer3.Skin.Characters_Beat_GoGoMax[i] = 2;
                TJAPlayer3.Skin.Characters_Beat_SoulIn[i] = 1.5f;
                TJAPlayer3.Skin.Characters_Beat_Return[i] = 1.5f;
                TJAPlayer3.Skin.Characters_Balloon_Timer[i] = 28;
                TJAPlayer3.Skin.Characters_Balloon_Delay[i] = 500;
                TJAPlayer3.Skin.Characters_Balloon_FadeOut[i] = 84;
                TJAPlayer3.Skin.Characters_Title_Entry_AnimationDuration[i] = 1000;
                TJAPlayer3.Skin.Characters_Title_Normal_AnimationDuration[i] = 1000;
                TJAPlayer3.Skin.Characters_Menu_Loop_AnimationDuration[i] = 1000;
                TJAPlayer3.Skin.Characters_Menu_Select_AnimationDuration[i] = 1000;
                TJAPlayer3.Skin.Characters_Menu_Start_AnimationDuration[i] = 1000;
                TJAPlayer3.Skin.Characters_Result_Normal_AnimationDuration[i] = 1000;
                TJAPlayer3.Skin.Characters_Result_Clear_AnimationDuration[i] = 1000;
                TJAPlayer3.Skin.Characters_Result_Failed_In_AnimationDuration[i] = 1000;
                TJAPlayer3.Skin.Characters_Result_Failed_AnimationDuration[i] = 1000;

                #endregion

                var _str = "";
                TJAPlayer3.Skin.LoadSkinConfigFromFile(charaPath + @"\CharaConfig.txt", ref _str);

                string[] delimiter = { "\n" };
                string[] strSingleLine = _str.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

                foreach (string s in strSingleLine)
                {
                    string str = s.Replace('\t', ' ').TrimStart(new char[] { '\t', ' ' });
                    if ((str.Length != 0) && (str[0] != ';'))
                    {
                        try
                        {
                            string strCommand;
                            string strParam;
                            string[] strArray = str.Split(new char[] { '=' });

                            if (strArray.Length == 2)
                            {
                                strCommand = strArray[0].Trim();
                                strParam = strArray[1].Trim();

                                switch (strCommand)
                                {
                                    case "Game_Chara_X":
                                    {
                                        string[] strSplit = strParam.Split(',');
                                        for (int k = 0; k < 2; k++)
                                        {
                                            TJAPlayer3.Skin.Characters_X[i][k] = int.Parse(strSplit[k]);
                                        }
                                        break;
                                    }
                                    case "Game_Chara_Y":
                                    {
                                        string[] strSplit = strParam.Split(',');
                                        for (int k = 0; k < 2; k++)
                                        {
                                            TJAPlayer3.Skin.Characters_Y[i][k] = int.Parse(strSplit[k]);
                                        }
                                        break;
                                    }
                                    case "Game_Chara_4P":
                                    {
                                        string[] strSplit = strParam.Split(',');
                                        for (int k = 0; k < 2; k++)
                                        {
                                            TJAPlayer3.Skin.Characters_4P[i][k] = int.Parse(strSplit[k]);
                                        }
                                        break;
                                    }
                                    case "Game_Chara_5P":
                                    {
                                        string[] strSplit = strParam.Split(',');
                                        for (int k = 0; k < 2; k++)
                                        {
                                            TJAPlayer3.Skin.Characters_5P[i][k] = int.Parse(strSplit[k]);
                                        }
                                        break;
                                    }
                                    case "Game_Chara_X_AI":
                                    {
                                        string[] strSplit = strParam.Split(',');
                                        for (int k = 0; k < 2; k++)
                                        {
                                            TJAPlayer3.Skin.Characters_X_AI[i][k] = int.Parse(strSplit[k]);
                                        }
                                        break;
                                    }
                                    case "Game_Chara_Y_AI":
                                    {
                                        string[] strSplit = strParam.Split(',');
                                        for (int k = 0; k < 2; k++)
                                        {
                                            TJAPlayer3.Skin.Characters_Y_AI[i][k] = int.Parse(strSplit[k]);
                                        }
                                        break;
                                    }
                                    case "Game_Chara_Balloon_X":
                                    {
                                        string[] strSplit = strParam.Split(',');
                                        for (int k = 0; k < 2; k++)
                                        {
                                            TJAPlayer3.Skin.Characters_Balloon_X[i][k] = int.Parse(strSplit[k]);
                                        }
                                        break;
                                    }
                                    case "Game_Chara_Balloon_Y":
                                    {
                                        string[] strSplit = strParam.Split(',');
                                        for (int k = 0; k < 2; k++)
                                        {
                                            TJAPlayer3.Skin.Characters_Balloon_Y[i][k] = int.Parse(strSplit[k]);
                                        }
                                        break;
                                    }
                                    case "Game_Chara_Balloon_4P":
                                    {
                                        string[] strSplit = strParam.Split(',');
                                        for (int k = 0; k < 2; k++)
                                        {
                                            TJAPlayer3.Skin.Characters_Balloon_4P[i][k] = int.Parse(strSplit[k]);
                                        }
                                        break;
                                    }
                                    case "Game_Chara_Balloon_5P":
                                    {
                                        string[] strSplit = strParam.Split(',');
                                        for (int k = 0; k < 2; k++)
                                        {
                                            TJAPlayer3.Skin.Characters_Balloon_5P[i][k] = int.Parse(strSplit[k]);
                                        }
                                        break;
                                    }
                                    case "Game_Chara_Balloon_Timer":
                                    {
                                        if (int.Parse(strParam) > 0)
                                            TJAPlayer3.Skin.Characters_Balloon_Timer[i] = int.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Balloon_Delay":
                                    {
                                        if (int.Parse(strParam) > 0)
                                            TJAPlayer3.Skin.Characters_Balloon_Delay[i] = int.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Balloon_FadeOut":
                                    {
                                        if (int.Parse(strParam) > 0)
                                            TJAPlayer3.Skin.Characters_Balloon_FadeOut[i] = int.Parse(strParam);
                                        break;
                                    }
                                    // パターン数の設定はTextureLoader.csで反映されます。
                                    case "Game_Chara_Motion_Normal":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_Normal[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Motion_10Combo":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_10Combo[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Motion_10Combo_Max":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_10ComboMax[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Motion_Miss":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_Miss[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Motion_MissDown":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_MissDown[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Motion_ClearIn":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_ClearIn[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Motion_Clear":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_Clear[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Motion_GoGoStart":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_GoGoStart[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        TJAPlayer3.Skin.Characters_Motion_GoGoStartMax[i] = TJAPlayer3.Skin.Characters_Motion_GoGoStart[i];
                                        break;
                                    }
                                    case "Game_Chara_Motion_GoGoStart_Max":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_GoGoStartMax[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Motion_GoGo":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_GoGo[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        TJAPlayer3.Skin.Characters_Motion_GoGoMax[i] = TJAPlayer3.Skin.Characters_Motion_GoGo[i];
                                        break;
                                    }
                                    case "Game_Chara_Motion_GoGo_Max":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_GoGoMax[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Motion_SoulIn":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_SoulIn[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Motion_Return":
                                    {
                                        TJAPlayer3.Skin.Characters_Motion_Return[i] = C変換.ar配列形式のstringをint配列に変換して返す(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_Normal":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_Normal[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_10Combo":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_10Combo[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_10ComboMax":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_10ComboMax[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_Miss":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_Miss[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_MissDown":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_MissDown[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_ClearIn":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_ClearIn[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_Clear":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_Clear[i] = float.Parse(strParam);
                                        TJAPlayer3.Skin.Characters_Beat_ClearMax[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_ClearMax":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_ClearMax[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_GoGoStart":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_GoGoStart[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_GoGoStartMax":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_GoGoStartMax[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_GoGo":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_GoGo[i] = float.Parse(strParam);
                                        TJAPlayer3.Skin.Characters_Beat_GoGoMax[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_GoGoMax":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_GoGoMax[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_SoulIn":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_SoulIn[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Game_Chara_Beat_Return":
                                    {
                                        TJAPlayer3.Skin.Characters_Beat_Return[i] = float.Parse(strParam);
                                        break;
                                    }
                                    case "Chara_Entry_AnimationDuration":
                                    {
                                        TJAPlayer3.Skin.Characters_Title_Entry_AnimationDuration[i] = int.Parse(strParam);
                                        break;
                                    }
                                    case "Chara_Normal_AnimationDuration":
                                    {
                                        TJAPlayer3.Skin.Characters_Title_Normal_AnimationDuration[i] = int.Parse(strParam);
                                        break;
                                    }
                                    case "Chara_Menu_Loop_AnimationDuration":
                                    {
                                        TJAPlayer3.Skin.Characters_Menu_Loop_AnimationDuration[i] = int.Parse(strParam);
                                        break;
                                    }
                                    case "Chara_Menu_Select_AnimationDuration":
                                    {
                                        TJAPlayer3.Skin.Characters_Menu_Select_AnimationDuration[i] = int.Parse(strParam);
                                        break;
                                    }
                                    case "Chara_Menu_Start_AnimationDuration":
                                    {
                                        TJAPlayer3.Skin.Characters_Menu_Start_AnimationDuration[i] = int.Parse(strParam);
                                        break;
                                    }
                                    case "Chara_Result_Normal_AnimationDuration":
                                    {
                                        TJAPlayer3.Skin.Characters_Result_Normal_AnimationDuration[i] = int.Parse(strParam);
                                        break;
                                    }
                                    case "Chara_Result_Clear_AnimationDuration":
                                    {
                                        TJAPlayer3.Skin.Characters_Result_Clear_AnimationDuration[i] = int.Parse(strParam);
                                        break;
                                    }
                                    case "Chara_Result_Failed_In_AnimationDuration":
                                    {
                                        TJAPlayer3.Skin.Characters_Result_Failed_In_AnimationDuration[i] = int.Parse(strParam);
                                        break;
                                    }
                                    case "Chara_Result_Failed_AnimationDuration":
                                    {
                                        TJAPlayer3.Skin.Characters_Result_Failed_AnimationDuration[i] = int.Parse(strParam);
                                        break;
                                    }
                                    default: { break; }
                                }
                            }
                            continue;
                        }
                        catch (Exception exception)
                        {
                            Trace.TraceError(exception.ToString());
                            Trace.TraceError("例外が発生しましたが処理を継続します。 (6a32cc37-1527-412e-968a-512c1f0135cd)");
                            continue;
                        }
                    }
                }

                #endregion

                #endregion
            }

            #region [Voice samples]

            var _skin = TJAPlayer3.Skin;

            #region [Dispose previously allocated sound effects]

            _skin.voiceClearFailed[player]?.Dispose();
            _skin.voiceClearClear[player]?.Dispose();
            _skin.voiceClearFullCombo[player]?.Dispose();
            _skin.voiceClearAllPerfect[player]?.Dispose();
            _skin.voiceAIWin[player]?.Dispose();
            _skin.voiceAILose[player]?.Dispose();
            _skin.voiceMenuSongSelect[player]?.Dispose();
            _skin.voiceMenuSongDecide[player]?.Dispose();
            _skin.voiceMenuSongDecide_AI[player]?.Dispose();
            _skin.voiceMenuDiffSelect[player]?.Dispose();
            _skin.voiceMenuDanSelectStart[player]?.Dispose();
            _skin.voiceMenuDanSelectPrompt[player]?.Dispose();
            _skin.voiceMenuDanSelectConfirm[player]?.Dispose();
            _skin.voiceTitleSanka[player]?.Dispose();
            _skin.voiceTowerMiss[player]?.Dispose();
            _skin.voiceResultBestScore[player]?.Dispose();
            _skin.voiceResultClearFailed[player]?.Dispose();
            _skin.voiceResultClearSuccess[player]?.Dispose();
            _skin.voiceResultDanFailed[player]?.Dispose();
            _skin.voiceResultDanRedPass[player]?.Dispose();
            _skin.voiceResultDanGoldPass[player]?.Dispose();

            #endregion

            #region [Allocate and load the new samples]

            _skin.voiceClearFailed[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Clear\Failed");
            _skin.voiceClearClear[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Clear\Clear");
            _skin.voiceClearFullCombo[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Clear\FullCombo");
            _skin.voiceClearAllPerfect[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Clear\AllPerfect");
            _skin.voiceAIWin[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Clear\AIBattle_Win");
            _skin.voiceAILose[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Clear\AIBattle_Lose");
            _skin.voiceMenuSongSelect[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Menu\SongSelect");
            _skin.voiceMenuSongDecide[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Menu\SongDecide");
            _skin.voiceMenuSongDecide_AI[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Menu\SongDecide_AI");
            _skin.voiceMenuDiffSelect[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Menu\DiffSelect");
            _skin.voiceMenuDanSelectStart[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Menu\DanSelectStart");
            _skin.voiceMenuDanSelectPrompt[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Menu\DanSelectPrompt");
            _skin.voiceMenuDanSelectConfirm[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Menu\DanSelectConfirm");
            _skin.voiceTitleSanka[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Title\Sanka");
            _skin.voiceTowerMiss[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Tower\Miss");
            _skin.voiceResultBestScore[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Result\BestScore");
            _skin.voiceResultClearFailed[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Result\ClearFailed");
            _skin.voiceResultClearSuccess[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Result\ClearSuccess");
            _skin.voiceResultDanFailed[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Result\DanFailed");
            _skin.voiceResultDanRedPass[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Result\DanRedPass");
            _skin.voiceResultDanGoldPass[player] = VoiceSelectOggOrWav(charaPath + @"\Sounds\Result\DanGoldPass");

            #endregion

            #endregion
        }

        public void DisposeTexture()
        {
            foreach (var tex in listTexture)
            {
                var texture = tex;
                TJAPlayer3.tテクスチャの解放(ref texture);
                texture?.Dispose();
                texture = null;
            }
            listTexture.Clear();
        }

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
        public CTexture[] NamePlate;

        public CTexture[] NamePlate_Effect = new CTexture[5];

        public CTexture[][] NamePlate_Title;
        public CTexture[] NamePlate_Title_Big;
        public CTexture[] NamePlate_Title_Small;

        #endregion

        #region 1_タイトル画面

        public CTexture 
            //Title_Background,
            Entry_Bar,
            Entry_Bar_Text;

        public CTexture[] Banapas_Load = new CTexture[3];
        public CTexture[] Banapas_Load_Clear = new CTexture[2];
        public CTexture[] Banapas_Load_Failure = new CTexture[2];
        public CTexture[] Entry_Player = new CTexture[3];
        public CTexture[] ModeSelect_Bar;
        public CTexture[] ModeSelect_Bar_Chara;

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
            SongSelect_Crown,
            SongSelect_ScoreRank,
            SongSelect_Song_Number,
            SongSelect_BoardNumber,
            SongSelect_Difficulty_Cymbol,
            SongSelect_Tower_Side,

            SongSelect_Favorite,
            SongSelect_High_Score,

            SongSelect_Level_Icons,
            SongSelect_Search_Arrow,
            SongSelect_Search_Arrow_Glow,
            SongSelect_Search_Window,

            SongSelect_ScoreWindow_Text;
        public CTexture[] SongSelect_GenreBack,
            SongSelect_Bar_Genre,
            SongSelect_Bar_Genre_Overlap,
            SongSelect_Box_Chara,
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
        public CTexture[] Difficulty_Back;
        #endregion

        #endregion

        #region 3_段位選択画面

        //public CTexture Dani_Background;
        public CTexture Dani_Difficulty_Cymbol;
        public CTexture Dani_Level_Number;
        public CTexture Dani_Soul_Number;
        public CTexture Dani_Exam_Number;
        public CTexture Dani_Bar_Center;
        public CTexture Dani_Plate;

        public CTexture[] Challenge_Select = new CTexture[3];

        //public CTexture Dani_Dan_In;
        //public CTexture Dani_Dan_Text;

        public CTexture Dani_DanPlates;
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
        public CTexture[] Mob;
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
            Gauge_Flash,
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
        public CTexture Kusudama,
            Kusudama_Clear,
            Kusudama_Fail,
            Kusudama_Number,
            Kusudama_Back;

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
        #region 終了演出
        public CTexture End_Clear_Chara;
        public CTexture[] End_Clear_Text;
        public CTexture End_Star;

        public CTexture[] End_Clear_L,
            End_Clear_R,
            End_ClearFailed,
            End_FullCombo,
            End_FullComboLoop,
            End_DondaFullCombo,
            End_DondaFullComboLoop,
            End_Goukaku;
        public CTexture End_Clear_Text_,
            End_Clear_Text_Effect,
            ClearFailed,
            ClearFailed1,
            ClearFailed2,
            End_DondaFullComboBg;

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

        public CTexture Tower_Sky_Gradient,
            Tower_Miss;

        public CTexture[] Tower_Top;

        public CTexture[][] Tower_Base,
            Tower_Deco,
            Tower_Don_Running,
            Tower_Don_Standing,
            Tower_Don_Climbing,
            Tower_Don_Jump;


        #endregion

        #region [21_ModIcons]

        public CTexture[] Mod_Timing,
            Mod_SongSpeed,
            Mod_Fun,
            HiSp;
        public CTexture     Mod_None,
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
        public CTexture Result_FadeIn,
            Result_Header,
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
            Result_Speech_Bubble = new CTexture[2]
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
            Heya_Lock;

        #endregion

        #region [11_Characters]

        public CTexture[][] Characters_Normal,
            Characters_Normal_Missed,
            Characters_Normal_MissedDown,
            Characters_Normal_Cleared,
            Characters_Normal_Maxed,
            Characters_GoGoTime,
            Characters_GoGoTime_Maxed,
            Characters_10Combo,
            Characters_10Combo_Maxed,
            Characters_GoGoStart,
            Characters_GoGoStart_Maxed,
            Characters_Become_Cleared,
            Characters_Become_Maxed,
            Characters_Return,
            Characters_Balloon_Breaking,
            Characters_Balloon_Broke,
            Characters_Balloon_Miss,
            Characters_Title_Entry,
            Characters_Title_Normal,
            Characters_Menu_Loop,
            Characters_Menu_Select,
            Characters_Menu_Start,
            Characters_Result_Clear,
            Characters_Result_Failed,
            Characters_Result_Failed_In,
            Characters_Result_Normal;

        public CTexture[] Characters_Heya_Preview,
            Characters_Heya_Render;
        public CCharacter[] Characters;

        #endregion

        #region [11_Modals]

        public CTexture[] Modal_Full,
            Modal_Half,
            Modal_Half_4P,
            Modal_Half_5P;

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

        #region [15_OpenEncyclopedia]

        public CTexture 
            //OpenEncyclopedia_Background,
            OpenEncyclopedia_Context,
            OpenEncyclopedia_Return_Box,
            OpenEncyclopedia_Side_Menu;

        #endregion


        #region [ 解放用 ]
        public List<CTexture> listTexture = new List<CTexture>();
        #endregion

    }
}
