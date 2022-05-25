using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using FDK;
using static TJAPlayer3.CActSelect曲リスト;

namespace TJAPlayer3
{
    class CStageOnlineLounge : CStage
    {

        public CStageOnlineLounge()
        {
            base.eステージID = Eステージ.OnlineLounge;
            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;

            // Load CActivity objects here
            // base.list子Activities.Add(this.act = new CAct());

            base.list子Activities.Add(this.actFOtoTitle = new CActFIFOBlack());

        }

        public override void On活性化()
        {
            // On activation

            if (base.b活性化してる)
                return;

            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
            this.eフェードアウト完了時の戻り値 = E戻り値.継続;

            this.currentMenu = ECurrentMenu.MAIN;
            this.menuPointer = ECurrentMenu.CDN_SELECT;
            this.menus = new CMenuInfo[(int)ECurrentMenu.TOTAL];

            for (int i = 0; i < (int)ECurrentMenu.TOTAL; i++)
                this.menus[i] = new CMenuInfo(CLangManager.LangInstance.GetString(400 + i));


            if (!string.IsNullOrEmpty(TJAPlayer3.ConfigIni.FontName))
            {
                this.pfOLFont = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 14);
                this.pfOLFontLarge = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), 28);
            }
            else
            {
                this.pfOLFont = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 14);
                this.pfOLFontLarge = new CPrivateFastFont(new FontFamily("MS UI Gothic"), 28);
            }
                


            dbCDN = TJAPlayer3.Databases.DBCDN;
            dbCDNData = null;

            IsDownloading = false;

            #region [Main menu]

            this.ttkMainMenuOpt = new TitleTextureKey[3];

            this.ttkMainMenuOpt[0] = new TitleTextureKey(CLangManager.LangInstance.GetString(400), this.pfOLFont, Color.White, Color.DarkRed, 1000);
            this.ttkMainMenuOpt[1] = new TitleTextureKey(CLangManager.LangInstance.GetString(402), this.pfOLFont, Color.White, Color.DarkRed, 1000);
            this.ttkMainMenuOpt[2] = new TitleTextureKey(CLangManager.LangInstance.GetString(407) + " (Not available)", this.pfOLFont, Color.White, Color.DarkRed, 1000);

            this.mainMenu = new ECurrentMenu[] { ECurrentMenu.RETURN, ECurrentMenu.CDN_SELECT, ECurrentMenu.MULTI_SELECT };

            this.mainMenuIndex = 0;

            #endregion

            #region [CDN Select]

            int keyCount = dbCDN.data.Count;

            this.ttkCDNSelectOpt = new TitleTextureKey[keyCount + 1];

            this.ttkCDNSelectOpt[0] = new TitleTextureKey(CLangManager.LangInstance.GetString(401), this.pfOLFont, Color.White, Color.DarkRed, 1000);

            for (int i = 0; i < keyCount; i++)
            {
                this.ttkCDNSelectOpt[i + 1] = new TitleTextureKey(dbCDN.data.ElementAt(i).Key, this.pfOLFont, Color.White, Color.DarkRed, 1000);
            }

            this.CDNSelectIndex = 0;

            #endregion

            #region [CDN Option]

            this.ttkCDNOptionOpt = new TitleTextureKey[4];

            this.ttkCDNOptionOpt[0] = new TitleTextureKey(CLangManager.LangInstance.GetString(401), this.pfOLFont, Color.White, Color.DarkRed, 1000);
            this.ttkCDNOptionOpt[1] = new TitleTextureKey(CLangManager.LangInstance.GetString(404), this.pfOLFont, Color.White, Color.DarkRed, 1000);
            this.ttkCDNOptionOpt[2] = new TitleTextureKey(CLangManager.LangInstance.GetString(405) + " (Not available)", this.pfOLFont, Color.White, Color.DarkRed, 1000);
            this.ttkCDNOptionOpt[3] = new TitleTextureKey(CLangManager.LangInstance.GetString(406) + " (Not available)", this.pfOLFont, Color.White, Color.DarkRed, 1000);

            this.cdnOptMenu = new ECurrentMenu[] { ECurrentMenu.CDN_SELECT, ECurrentMenu.CDN_SONGS, ECurrentMenu.CDN_CHARACTERS, ECurrentMenu.CDN_PUCHICHARAS };

            this.cdnOptMenuIndex = 0;

            #endregion

            base.On活性化();
        }

        public override void On非活性化()
        {
            // On de-activation

            TJAPlayer3.Songs管理.UpdateDownloadBox();

            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            // Ressource allocation

            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            // Ressource freeing

            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            TJAPlayer3.Tx.OnlineLounge_Background.t2D描画(TJAPlayer3.app.Device, 0, 0);

            #region [Menus]


            #region [Base Menus]

            TitleTextureKey[] _ref = this.ttkMainMenuOpt;
            int _selector = mainMenuIndex;

            if (currentMenu == ECurrentMenu.CDN_SELECT)
            {
                _ref = this.ttkCDNSelectOpt;
                _selector = CDNSelectIndex;
            }
            else if (currentMenu == ECurrentMenu.CDN_OPTION)
            {
                _ref = this.ttkCDNOptionOpt;
                _selector = cdnOptMenuIndex;
            }
                

            if (currentMenu == ECurrentMenu.MAIN
                || currentMenu == ECurrentMenu.CDN_SELECT
                || currentMenu == ECurrentMenu.CDN_OPTION)
            {
                int baseY = 360 - _ref.Length * 40;

                for (int i = 0; i < _ref.Length; i++)
                {
                    CTexture tmpTex = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(_ref[i]);

                    if (_selector != i)
                    {
                        tmpTex.color4 = C変換.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.OnlineLounge_Side_Menu?.tUpdateColor4(C変換.ColorToColor4(Color.DarkGray));
                    }
                    else
                    {
                        tmpTex.color4 = C変換.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.OnlineLounge_Side_Menu?.tUpdateColor4(C変換.ColorToColor4(Color.White));
                    }

                    TJAPlayer3.Tx.OnlineLounge_Side_Menu?.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 640, baseY + 80 * i);
                    tmpTex.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 640, baseY + 18 + 80 * i);
                }
            }

            #endregion

            #region [Song list menu]

            if (currentMenu == ECurrentMenu.CDN_SONGS)
            {
                _ref = this.ttkCDNSongList;
                _selector = cdnSongListIndex;

                int baseY = 360;

                for (int i = -4; i < 4; i++)
                {
                    int pos = (_ref.Length * 5 + _selector + i) % _ref.Length;

                    CTexture tmpTex = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(_ref[pos]);
                    CTexture tmpSubtitle = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkCDNSongSubtitles[pos]);

                    if (i != 0)
                    {
                        tmpTex.color4 = C変換.ColorToColor4(Color.DarkGray);
                        tmpSubtitle.color4 = C変換.ColorToColor4(Color.DarkGray);
                        TJAPlayer3.Tx.OnlineLounge_Song_Box?.tUpdateColor4(C変換.ColorToColor4(Color.DarkGray));
                    }
                    else
                    {
                        tmpTex.color4 = C変換.ColorToColor4(Color.White);
                        tmpSubtitle.color4 = C変換.ColorToColor4(Color.White);
                        TJAPlayer3.Tx.OnlineLounge_Song_Box?.tUpdateColor4(C変換.ColorToColor4(Color.White));
                    }

                    TJAPlayer3.Tx.OnlineLounge_Song_Box?.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 350, baseY + 100 * i);
                    tmpTex.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 350, baseY + 18 + 100 * i);
                    tmpSubtitle.t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 350, baseY + 46 + 100 * i);

                    if (pos != 0 && i == 0)
                    {
                        TJAPlayer3.Tx.OnlineLounge_Context.t2D描画(TJAPlayer3.app.Device, 0, 0);

                        var song_ = apiMethods.FetchedSongsList[pos - 1];

                        int[] diffs = new int[]
                        {
                            song_.D0,
                            song_.D1,
                            song_.D2,
                            song_.D3,
                            song_.D4,
                            song_.D5,
                            song_.D6,
                        };

                        #region [Charter Name]

                        if (song_.charter != null && song_.charter.charter_name != null && song_.charter.charter_name != "")
                        {
                            var charter_ = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(
                                    new TitleTextureKey("Charter : " + song_.charter.charter_name, this.pfOLFontLarge, Color.White, Color.Black, 1000));
                            charter_?.t2D中心基準描画(TJAPlayer3.app.Device, 980, 300);
                        }

                        #endregion

                        #region [Song Genre]

                        if (song_.Genre != null && song_.Genre.genre != null && song_.Genre.genre != "")
                        {
                            var genre_ = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(
                                    new TitleTextureKey(song_.Genre.genre, this.pfOLFontLarge, Color.White, Color.Black, 1000));
                            genre_?.t2D中心基準描画(TJAPlayer3.app.Device, 980, 340);
                        }

                        #endregion

                        #region [Difficulties]

                        for (int k = 0; k < (int)Difficulty.Total; k++)
                        {
                            int diff = diffs[k];

                            int column = (k >= 3) ? 240 : 0;
                            int row = 60 * (k % 3);

                            if (diff > 0)
                            {
                                TJAPlayer3.Tx.Couse_Symbol[k]?.t2D中心基準描画(TJAPlayer3.app.Device, 800 + column, 480 + row);

                                var difnb_ = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(
                                    new TitleTextureKey(diff.ToString(), this.pfOLFontLarge, (diff > 10) ? Color.Red : Color.White, Color.Black, 1000));
                                difnb_?.t2D中心基準描画(TJAPlayer3.app.Device, 900 + column, 480 + 14 + row);
                            }
                            
                        }

                        #endregion


                    }
                        
                }
            }

            #endregion

            if (IsDownloading)
            {
                TJAPlayer3.Tx.OnlineLounge_Box.t2D描画(TJAPlayer3.app.Device, 0, 0);

                var text = TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(
                                    new TitleTextureKey("DownloadNow", this.pfOLFontLarge, Color.White, Color.Black, 1000));
                text.t2D中心基準描画(TJAPlayer3.app.Device, 640, 605);
            }

            #endregion



            #region [Input]

            if (!IsDownloading)
            {
                if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.RightArrow) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue))
                {
                    if (this.tMove(1))
                    {
                        TJAPlayer3.Skin.sound変更音.t再生する();
                    }
                }

                else if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.LeftArrow) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue))
                {
                    if (this.tMove(-1))
                    {
                        TJAPlayer3.Skin.sound変更音.t再生する();
                    }
                }

                else if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Escape))
                {

                    #region [Fast return (Escape)]

                    TJAPlayer3.Skin.sound取消音.t再生する();

                    if (currentMenu == ECurrentMenu.MAIN)
                    {
                        // Return to title screen
                        TJAPlayer3.Skin.soundOnlineLoungeBGM.t停止する();
                        this.eフェードアウト完了時の戻り値 = E戻り値.タイトルに戻る;
                        this.actFOtoTitle.tフェードアウト開始();
                        base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
                    }
                    else if (currentMenu == ECurrentMenu.CDN_SELECT || currentMenu == ECurrentMenu.MULTI_SELECT)
                    {
                        // Return to base menu
                        currentMenu = ECurrentMenu.MAIN;
                    }
                    else if (currentMenu == ECurrentMenu.CDN_OPTION)
                    {
                        // Return to CDN select menu
                        currentMenu = ECurrentMenu.CDN_SELECT;
                    }
                    else if (currentMenu == ECurrentMenu.CDN_SONGS || currentMenu == ECurrentMenu.CDN_CHARACTERS || currentMenu == ECurrentMenu.CDN_PUCHICHARAS)
                    {
                        // Return to CDN select option
                        currentMenu = ECurrentMenu.CDN_OPTION;
                    }

                    return 0;

                    #endregion
                }

                else if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Return) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed))
                {

                    #region [Decide]

                    if (currentMenu == ECurrentMenu.MAIN)
                    {
                        // Base menu
                        currentMenu = mainMenu[mainMenuIndex];
                        if (currentMenu == ECurrentMenu.RETURN)
                        {
                            // Quit
                            TJAPlayer3.Skin.sound取消音.t再生する();
                            TJAPlayer3.Skin.soundOnlineLoungeBGM.t停止する();
                            this.eフェードアウト完了時の戻り値 = E戻り値.タイトルに戻る;
                            this.actFOtoTitle.tフェードアウト開始();
                            base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
                        }
                        else
                        {
                            TJAPlayer3.Skin.sound決定音.t再生する();
                        }
                    }
                    else if (currentMenu == ECurrentMenu.CDN_SELECT)
                    {
                        // CDN Select Menu
                        if (CDNSelectIndex > 0)
                        {
                            currentMenu = ECurrentMenu.CDN_OPTION;
                            dbCDNData = dbCDN.data.ElementAt(CDNSelectIndex - 1).Value;
                            TJAPlayer3.Skin.sound決定音.t再生する();
                        }
                        else
                        {
                            currentMenu = ECurrentMenu.MAIN;
                            TJAPlayer3.Skin.sound取消音.t再生する();
                        }
                    }
                    else if (currentMenu == ECurrentMenu.CDN_OPTION)
                    {
                        // CDN Option Menu
                        currentMenu = cdnOptMenu[cdnOptMenuIndex];
                        if (currentMenu == ECurrentMenu.CDN_SELECT)
                            TJAPlayer3.Skin.sound取消音.t再生する();
                        else
                        {
                            if (currentMenu == ECurrentMenu.CDN_SONGS)
                            {
                                apiMethods = new API(dbCDNData);
                                apiMethods.tLoadSongsFromInternalCDN();

                                #region [Generate song list values]

                                int songCountPlusOne = apiMethods.FetchedSongsList.Length + 1;

                                this.ttkCDNSongList = new TitleTextureKey[songCountPlusOne];
                                this.ttkCDNSongSubtitles = new TitleTextureKey[songCountPlusOne];

                                this.ttkCDNSongList[0] = new TitleTextureKey(CLangManager.LangInstance.GetString(401), this.pfOLFont, Color.White, Color.DarkRed, 1000);
                                this.ttkCDNSongSubtitles[0] = new TitleTextureKey("", this.pfOLFont, Color.White, Color.DarkRed, 1000);

                                for (int i = 0; i < apiMethods.FetchedSongsList.Length; i++)
                                {
                                    this.ttkCDNSongList[i + 1] = new TitleTextureKey(apiMethods.FetchedSongsList[i].SongTitle, this.pfOLFont, Color.White, Color.DarkRed, 1000);

                                    string subtitle_ = apiMethods.FetchedSongsList[i].SongSubtitle;
                                    if (subtitle_.Length >= 2)
                                        subtitle_ = subtitle_.Substring(2);
                                    this.ttkCDNSongSubtitles[i + 1] = new TitleTextureKey(subtitle_, this.pfOLFont, Color.White, Color.DarkRed, 1000);
                                }

                                this.cdnSongListIndex = 0;

                                #endregion
                            }
                            TJAPlayer3.Skin.sound決定音.t再生する();
                        }

                    }
                    else if (currentMenu == ECurrentMenu.CDN_SONGS)
                    {
                        if (this.cdnSongListIndex == 0)
                        {
                            TJAPlayer3.Skin.sound取消音.t再生する();
                            currentMenu = ECurrentMenu.CDN_OPTION;
                        }
                        else
                        {
                            if (this.cdnSongListIndex < apiMethods.FetchedSongsList.Length)
                            {
                                var song = apiMethods.FetchedSongsList[this.cdnSongListIndex - 1];
                                var zipPath = $@"Cache\{song.Md5}.zip";

                                if (System.IO.Directory.Exists($@"Songs\S3 Download\{song.Md5}"))
                                {
                                    TJAPlayer3.Skin.soundError.t再生する();
                                }
                                else
                                {
                                    TJAPlayer3.Skin.sound決定音.t再生する();
                                    System.Threading.Thread download =
                                        new System.Threading.Thread(new System.Threading.ThreadStart(DownloadSong));
                                    download.Start();
                                }

                            }
                        }
                    }


                    #endregion
                }
            }
            #endregion

            // Menu exit fade out transition
            #region [FadeOut]

                switch (base.eフェーズID)
            {
                case CStage.Eフェーズ.共通_フェードアウト:
                    if (this.actFOtoTitle.On進行描画() == 0)
                    {
                        break;
                    }
                    return (int)this.eフェードアウト完了時の戻り値;

            }

            #endregion

            return 0;
        }

        public bool tMove(int val)
        {
            if (currentMenu == ECurrentMenu.MAIN)
            {
                if (mainMenuIndex + val < 0 || mainMenuIndex + val >= mainMenu.Length)
                    return false;

                mainMenuIndex += val;
            }
            else if (currentMenu == ECurrentMenu.CDN_SELECT)
            {
                if (CDNSelectIndex + val < 0 || CDNSelectIndex + val >= ttkCDNSelectOpt.Length)
                    return false;

                CDNSelectIndex += val;
            }
            else if (currentMenu == ECurrentMenu.CDN_OPTION)
            {
                if (cdnOptMenuIndex + val < 0 || cdnOptMenuIndex + val >= cdnOptMenu.Length)
                    return false;

                cdnOptMenuIndex += val;
            }
            else if (currentMenu == ECurrentMenu.CDN_SONGS)
            {
                cdnSongListIndex = (ttkCDNSongList.Length + cdnSongListIndex + val) % ttkCDNSongList.Length;
            }

            return true;
        }

        #region [Song Downloading]

        private string GetAssignedLanguageValue(Dictionary<string, string> ens)
        {
            if (ens.ContainsKey(TJAPlayer3.ConfigIni.sLang))
                return ens[TJAPlayer3.ConfigIni.sLang];
            return ens["default"];
        }

        private void DownloadSong()
        {
            IsDownloading = true;

            // Create Cache folder if does not exist
            Directory.CreateDirectory($@"Cache\");

            var song = apiMethods.FetchedSongsList[this.cdnSongListIndex - 1];
            var zipPath = $@"Cache\{song.SongTitle}-{song.Md5}.zip";

            try
            {
                // Download zip from cdn
                System.Net.WebClient wc = new System.Net.WebClient();

                wc.DownloadFile($"{dbCDNData.BaseUrl}{GetAssignedLanguageValue(dbCDNData.Download)}{song.Id}", zipPath);
                wc.Dispose();

                // Fetch closest Download folder node
                C曲リストノード downloadBox = null;
                for (int i = 0; i < TJAPlayer3.Songs管理.list曲ルート.Count; i++)
                {
                    if (TJAPlayer3.Songs管理.list曲ルート[i].strジャンル == "Download"
                        && TJAPlayer3.Songs管理.list曲ルート[i].eノード種別 == C曲リストノード.Eノード種別.BOX)
                        downloadBox = TJAPlayer3.Songs管理.list曲ルート[i];
                }

                // If there is at least one download folder, transfer the zip contents in it
                if (downloadBox != null)
                {
                    var path = downloadBox.arスコア[0].ファイル情報.フォルダの絶対パス;
                    var genredPath = $@"{path}\{song.Genre.genre}\";

                    if (!Directory.Exists(genredPath))
                    {
                        // Create Genre sub-folder if does not exist
                        Directory.CreateDirectory(genredPath);

                        // Search a corresponding box-def if exists
                        C曲リストノード correspondingBox = null;
                        for (int i = 0; i < TJAPlayer3.Songs管理.list曲ルート.Count; i++)
                        {
                            if (TJAPlayer3.Songs管理.list曲ルート[i].strジャンル == song.Genre.genre
                                && TJAPlayer3.Songs管理.list曲ルート[i].eノード種別 == C曲リストノード.Eノード種別.BOX)
                                correspondingBox = TJAPlayer3.Songs管理.list曲ルート[i];
                        }

                        var newBoxDef = $@"{genredPath}\box.def";

                        if (correspondingBox == null)
                        {
                            // Generate box.def if none available
                            
                            //File.Create(newBoxDef);

                            StreamWriter sw = new StreamWriter(newBoxDef, false, Encoding.GetEncoding(TJAPlayer3.sEncType));

                            sw.WriteLine($@"#TITLE:{song.Genre.genre}");
                            sw.WriteLine($@"#GENRE:{song.Genre.genre}");
                            sw.WriteLine($@"#BOXEXPLANATION1:");
                            sw.WriteLine($@"#BOXEXPLANATION2:");
                            sw.WriteLine($@"#BOXEXPLANATION3:");
                            sw.WriteLine($@"#BGCOLOR:#ff00a2");
                            sw.WriteLine($@"#BOXCOLOR:#ff00a2");
                            sw.WriteLine($@"#BOXTYPE:0");
                            sw.WriteLine($@"#BGTYPE:1");
                            sw.WriteLine($@"#BOXCHARA:0");
                            sw.Close();
                        }
                        else
                        {
                            // Copy the existing box.def if available
                            var corPath = correspondingBox.arスコア[0].ファイル情報.フォルダの絶対パス;

                            File.Copy($@"{corPath}\box.def", newBoxDef);
                        }

                        
                    }
                    

                    var songPath = $@"{genredPath}{song.SongTitle}-{song.Md5}";

                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, songPath);
                }

                //System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, $@"Songs\S3 Download\{song.Md5}");
            }
            catch (Exception e)
            {
                Trace.TraceInformation(e.ToString());
                TJAPlayer3.Skin.soundError.t再生する();
            }


            IsDownloading = false;
        }

        #endregion

        #region [Enums]

        public enum E戻り値 : int
        {
            継続,
            タイトルに戻る,
            選曲した
        }

        public enum ECurrentMenu : int
        {
            RETURN,         // Return button
            MAIN,           // Choice between select CDN and Online multiplayer
            CDN_SELECT,     // Select a registered CDN
            CDN_OPTION,     // Select between Download songs, Download characters and Download puchicharas
            CDN_SONGS,      // List songs
            CDN_CHARACTERS, // List characters
            CDN_PUCHICHARAS,// List puchicharas
            MULTI_SELECT,   // Main online multiplayer menu
            TOTAL,          // Submenus count
        }

        #endregion

        #region [Private]

        private ECurrentMenu currentMenu;
        private ECurrentMenu menuPointer;
        private CMenuInfo[] menus;
        public E戻り値 eフェードアウト完了時の戻り値;
        public CActFIFOBlack actFOtoTitle;


        private CPrivateFastFont pfOLFont;
        private CPrivateFastFont pfOLFontLarge;

        private DBCDN dbCDN;
        private DBCDN.CDNData dbCDNData;
        private API apiMethods;

        // Main Menu
        private TitleTextureKey[] ttkMainMenuOpt;
        private ECurrentMenu[] mainMenu;
        private int mainMenuIndex;

        // CDN Select
        private TitleTextureKey[] ttkCDNSelectOpt;
        private int CDNSelectIndex;

        // CDN Option
        private TitleTextureKey[] ttkCDNOptionOpt;
        private ECurrentMenu[] cdnOptMenu;
        private int cdnOptMenuIndex;

        // CDN List songs option
        private TitleTextureKey[] ttkCDNSongList;
        private TitleTextureKey[] ttkCDNSongSubtitles;
        private int cdnSongListIndex;

        private bool IsDownloading;

        private class CMenuInfo
        {
            public CMenuInfo(string ttl)
            {
                title = ttl;
            }

            public string title;
        }

        #endregion

    }
}
