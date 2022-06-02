using FDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TJAPlayer3
{
    /*
    ** class CSongSelectSongManager
    ** playSongIfPossible : Play song is enabled and not playing, supports both intro and regular song
    ** stopSong : Stop song without disabling it
    ** enable : Enable the menu song
    ** disable : Disable the menu song
    */
    #region [Generic static class for handling the menu song]

    static internal class CSongSelectSongManager
    {
        public static void playSongIfPossible()
        {
            if (CSongSelectSongManager.isSongDisabled)
                return;

            if (TJAPlayer3.ConfigIni.bBGM音を発声する && !TJAPlayer3.Skin.bgm選曲画面イン.b再生中 && !TJAPlayer3.Skin.bgm選曲画面.b再生中)
            {
                if (inSongPlayed == false)
                {
                    TJAPlayer3.Skin.bgm選曲画面イン.t再生する();
                    CSongSelectSongManager.inSongPlayed = true;
                }
                else
                {
                    TJAPlayer3.Skin.bgm選曲画面.t再生する();
                }
            }
                
        }

        public static void stopSong()
        {
            TJAPlayer3.Skin.bgm選曲画面イン.t停止する();
            TJAPlayer3.Skin.bgm選曲画面.t停止する();
            CSongSelectSongManager.inSongPlayed = false;
        }

        public static void enable()
        {
            CSongSelectSongManager.isSongDisabled = false;
        }

        public static void disable()
        {
            CSongSelectSongManager.stopSong();
            CSongSelectSongManager.isSongDisabled = true;
        }


        private static bool inSongPlayed = false;
        private static bool isSongDisabled = false;
    }

    #endregion

    internal class CStage選曲 : CStage
    {
        // プロパティ
        public int nスクロールバー相対y座標
        {
            get
            {
                if (act曲リスト != null)
                {
                    return act曲リスト.nスクロールバー相対y座標;
                }
                else
                {
                    return 0;
                }
            }
        }
        public bool bIsEnumeratingSongs
        {
            get
            {
                return act曲リスト.bIsEnumeratingSongs;
            }
            set
            {
                act曲リスト.bIsEnumeratingSongs = value;
            }
        }
        public bool bIsPlayingPremovie
        {
            get
            {
                return this.actPreimageパネル.bIsPlayingPremovie;
            }
        }
        public bool bスクロール中
        {
            get
            {
                return this.act曲リスト.bスクロール中;
            }
        }
        public int[] n確定された曲の難易度 = new int[2];

        public string str確定された曲のジャンル
        {
            get;
            set;
        }
        public Cスコア r確定されたスコア
        {
            get;
            set;
        }
        public C曲リストノード r確定された曲
        {
            get;
            set;
        }
        public int n現在選択中の曲の難易度
        {
            get
            {
                return this.act曲リスト.n現在選択中の曲の現在の難易度レベル;
            }
        }
        public Cスコア r現在選択中のスコア
        {
            get
            {
                return this.act曲リスト.r現在選択中のスコア;
            }
        }
        public C曲リストノード r現在選択中の曲
        {
            get
            {
                return this.act曲リスト.r現在選択中の曲;
            }
        }

        // コンストラクタ
        public CStage選曲()
        {
            base.eステージID = CStage.Eステージ.選曲;
            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
            base.b活性化してない = true;
            base.list子Activities.Add(this.actオプションパネル = new CActオプションパネル());
            base.list子Activities.Add(this.actFIFO = new CActFIFOBlack());
            base.list子Activities.Add(this.actFIfrom結果画面 = new CActFIFOBlack());
            //base.list子Activities.Add( this.actFOtoNowLoading = new CActFIFOBlack() );
            base.list子Activities.Add(this.actFOtoNowLoading = new CActFIFOStart());
            base.list子Activities.Add(this.act曲リスト = new CActSelect曲リスト());
            base.list子Activities.Add(this.actステータスパネル = new CActSelectステータスパネル());
            base.list子Activities.Add(this.act演奏履歴パネル = new CActSelect演奏履歴パネル());
            base.list子Activities.Add(this.actPreimageパネル = new CActSelectPreimageパネル());
            base.list子Activities.Add(this.actPresound = new CActSelectPresound());
            base.list子Activities.Add(this.actArtistComment = new CActSelectArtistComment());
            base.list子Activities.Add(this.actInformation = new CActSelectInformation());
            base.list子Activities.Add(this.actSortSongs = new CActSortSongs());
            base.list子Activities.Add(this.actShowCurrentPosition = new CActSelectShowCurrentPosition());
            base.list子Activities.Add(this.actQuickConfig = new CActSelectQuickConfig());
            base.list子Activities.Add(this.act難易度選択画面 = new CActSelect難易度選択画面());
            base.list子Activities.Add(this.actPlayOption = new CActPlayOption());

            base.list子Activities.Add(this.PuchiChara = new PuchiChara());

            for (int i = 0; i < 10; i++)
            {
                stTimer[i].ch = i.ToString().ToCharArray()[0];
                stTimer[i].pt = new Point(46 * i, 0);
            }

            for(int i = 0; i < 10; i++)
            {
                stSongNumber[i].ch = i.ToString().ToCharArray()[0];
                stSongNumber[i].pt = new Point(27 * i, 0);
            }

            for(int i = 0; i < 10; i++)
            {
                stBoardNumber[i].ch = i.ToString().ToCharArray()[0];
                stBoardNumber[i].pt = new Point(15 * i, 0);
            }

            this.CommandHistory = new CCommandHistory();        // #24063 2011.1.16 yyagi
        }


        // メソッド

        public void t選択曲変更通知()
        {
            this.actPreimageパネル.t選択曲が変更された();
            this.actPresound.t選択曲が変更された();
            this.act演奏履歴パネル.t選択曲が変更された();
            this.actステータスパネル.t選択曲が変更された();
            this.actArtistComment.t選択曲が変更された();

            #region [ プラグインにも通知する（BOX, RANDOM, BACK なら通知しない）]
            //---------------------
            if (TJAPlayer3.app != null)
            {
                var c曲リストノード = TJAPlayer3.stage選曲.r現在選択中の曲;
                var cスコア = TJAPlayer3.stage選曲.r現在選択中のスコア;

                if (c曲リストノード != null && cスコア != null && c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.SCORE)
                {
                    string str選択曲ファイル名 = cスコア.ファイル情報.ファイルの絶対パス;
                    int n曲番号inブロック = TJAPlayer3.stage選曲.act曲リスト.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(c曲リストノード);

                    foreach (TJAPlayer3.STPlugin stPlugin in TJAPlayer3.app.listプラグイン)
                    {
                        Directory.SetCurrentDirectory(stPlugin.strプラグインフォルダ);
                        stPlugin.plugin.On選択曲変更(str選択曲ファイル名, n曲番号inブロック);
                        Directory.SetCurrentDirectory(TJAPlayer3.strEXEのあるフォルダ);
                    }
                }
            }
            //---------------------
            #endregion
        }

        // CStage 実装

        /// <summary>
        /// 曲リストをリセットする
        /// </summary>
        /// <param name="cs"></param>
        public void Refresh(CSongs管理 cs, bool bRemakeSongTitleBar)
        {
            this.act曲リスト.Refresh(cs, bRemakeSongTitleBar);
        }

        public override void On活性化()
        {
            Trace.TraceInformation("選曲ステージを活性化します。");
            Trace.Indent();
            try
            {
                n確定された曲の難易度 = new int[2];
                this.eフェードアウト完了時の戻り値 = E戻り値.継続;

                // BGM played
                this.bBGM再生済み = false;
                
                this.ftフォント = new Font("MS UI Gothic", 26f, GraphicsUnit.Pixel);
                for (int i = 0; i < 2; i++)
                    this.ctキー反復用[i] = new CCounter(0, 0, 0, TJAPlayer3.Timer);

                //ctDonchan_Normal = new CCounter(0, TJAPlayer3.Tx.SongSelect_Donchan_Normal.Length - 1, 1000 / 45, TJAPlayer3.Timer);
                CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.NORMAL);

                //ctDonchan_Select = new CCounter();
                //ctDonchan_Jump[0] = new CCounter();
                //ctDonchan_Jump[1] = new CCounter();

                CMenuCharacter.tDisableCounter(CMenuCharacter.ECharacterAnimation.SELECT);
                CMenuCharacter.tDisableCounter(CMenuCharacter.ECharacterAnimation.START);

                ctBackgroundFade = new CCounter();
                ctCreditAnime = new CCounter(0, 4500, 1, TJAPlayer3.Timer);
                ctTimer = new CCounter(0, 100, 1000, TJAPlayer3.Timer);

                this.PuchiChara.IdleAnimation();

                ctBackgroundFade.n現在の値 = 600;

                /*
                if(TJAPlayer3.ConfigIni.bBGM音を発声する && !TJAPlayer3.Skin.bgm選曲画面イン.b再生中 && !TJAPlayer3.Skin.bgm選曲画面.b再生中)
                    TJAPlayer3.Skin.bgm選曲画面イン.t再生する();
                */


                for (int i = 0; i < 3; i++)
                    r[i] = new Random();

                //this.act難易度選択画面.bIsDifficltSelect = true;
                base.On活性化();

                this.actステータスパネル.t選択曲が変更された();	// 最大ランクを更新
                // Discord Presenceの更新
                Discord.UpdatePresence("", Properties.Discord.Stage_SongSelect, TJAPlayer3.StartupTime);

               

                if(r現在選択中の曲 != null)
                    NowGenre = r現在選択中の曲.strジャンル;
            }
            finally
            {
                TJAPlayer3.ConfigIni.eScrollMode = EScrollMode.Normal;
                TJAPlayer3.ConfigIni.bスクロールモードを上書き = false;
                Trace.TraceInformation("選曲ステージの活性化を完了しました。");
                Trace.Unindent();
            }
        }
        public override void On非活性化()
        {
            Trace.TraceInformation("選曲ステージを非活性化します。");
            Trace.Indent();
            try
            {
                if (this.ftフォント != null)
                {
                    this.ftフォント.Dispose();
                    this.ftフォント = null;
                }
                for (int i = 0; i < 2; i++)
                {
                    this.ctキー反復用[i] = null;
                }
                base.On非活性化();
            }
            finally
            {
                Trace.TraceInformation("選曲ステージの非活性化を完了しました。");
                Trace.Unindent();
            }
        }
        public override void OnManagedリソースの作成()
        {
            if (!base.b活性化してない)
            {
                this.ct背景スクロール用タイマー = new CCounter(0, TJAPlayer3.Tx.SongSelect_Background.szテクスチャサイズ.Width, 30, TJAPlayer3.Timer);
                base.OnManagedリソースの作成();
            }
        }
        public override void OnManagedリソースの解放()
        {
            if (!base.b活性化してない)
            {
                base.OnManagedリソースの解放();
            }
        }
        public override int On進行描画()
        {
            if (!base.b活性化してない)
            {
                this.ct背景スクロール用タイマー.t進行Loop();
                #region [ 初めての進行描画 ]
                //---------------------
                if (base.b初めての進行描画)
                {
                    this.ct登場時アニメ用共通 = new CCounter(0, 100, 3, TJAPlayer3.Timer);
                    if (TJAPlayer3.r直前のステージ == TJAPlayer3.stage結果)
                    {
                        this.actFIfrom結果画面.tフェードイン開始();
                        base.eフェーズID = CStage.Eフェーズ.選曲_結果画面からのフェードイン;
                    }
                    else
                    {
                        this.actFIFO.tフェードイン開始();
                        base.eフェーズID = CStage.Eフェーズ.共通_フェードイン;
                    }
                    this.t選択曲変更通知();
                    base.b初めての進行描画 = false;
                }
                //---------------------
                #endregion


                ctTimer.t進行();
                ctCreditAnime.t進行Loop();
                ctBackgroundFade.t進行();

                //ctDonchan_Select.t進行();
                //ctDonchan_Jump[0].t進行();
                //ctDonchan_Jump[1].t進行();
                //ctDonchan_Normal.t進行Loop();

                this.ct登場時アニメ用共通.t進行();

                if (TJAPlayer3.Tx.SongSelect_Background != null)
                    TJAPlayer3.Tx.SongSelect_Background.t2D描画(TJAPlayer3.app.Device, 0, 0);

                if (this.r現在選択中の曲 != null)
                {
                   // if (this.nStrジャンルtoNum(this.r現在選択中の曲.strジャンル) != 0 || r現在選択中の曲.eノード種別 == C曲リストノード.Eノード種別.BOX || r現在選択中の曲.eノード種別 == C曲リストノード.Eノード種別.SCORE)
                    // {
                        if (this.NowUseGenre)
                            nGenreBack = this.nStrジャンルtoNum(this.NowGenre);
                        else
                            nGenreBack = this.NowBg;

                        if (this.OldUseGenre)
                            nOldGenreBack = this.nStrジャンルtoNum(this.OldGenre);
                        else
                            nOldGenreBack = this.OldBg;
                    // }
                    if (TJAPlayer3.Tx.SongSelect_GenreBack[nGenreBack] != null)
                    {
                        for (int i = 0; i < (1280 / TJAPlayer3.Tx.SongSelect_Background.szテクスチャサイズ.Width) + 2; i++)
                        {
                            if (TJAPlayer3.Tx.SongSelect_GenreBack[nGenreBack] != null)
                            {
                                TJAPlayer3.Tx.SongSelect_GenreBack[nGenreBack].color4 = C変換.ColorToColor4(this.NowBgColor);
                                TJAPlayer3.Tx.SongSelect_GenreBack[nGenreBack].Opacity = 255;
                                TJAPlayer3.Tx.SongSelect_GenreBack[nGenreBack].t2D描画(TJAPlayer3.app.Device, -(int)ct背景スクロール用タイマー.n現在の値 + TJAPlayer3.Tx.SongSelect_Background.szテクスチャサイズ.Width * i, 0);
                            }
                            if (TJAPlayer3.Tx.SongSelect_GenreBack[nOldGenreBack] != null)
                            {
                                TJAPlayer3.Tx.SongSelect_GenreBack[nOldGenreBack].color4 = C変換.ColorToColor4(this.OldBgColor);
                                TJAPlayer3.Tx.SongSelect_GenreBack[nOldGenreBack].Opacity = 600 - ctBackgroundFade.n現在の値;
                                TJAPlayer3.Tx.SongSelect_GenreBack[nOldGenreBack].t2D描画(TJAPlayer3.app.Device, -(int)ct背景スクロール用タイマー.n現在の値 + TJAPlayer3.Tx.SongSelect_Background.szテクスチャサイズ.Width * i, 0);
                            }
                        }
                    }
                }

                this.act曲リスト.On進行描画();
                int y = 0;
                if (this.ct登場時アニメ用共通.b進行中)
                {
                    double db登場割合 = ((double)this.ct登場時アニメ用共通.n現在の値) / 100.0;   // 100が最終値
                    double dbY表示割合 = Math.Sin(Math.PI / 2 * db登場割合);
                    y = ((int)(TJAPlayer3.Tx.SongSelect_Header.sz画像サイズ.Height * dbY表示割合)) - TJAPlayer3.Tx.SongSelect_Header.sz画像サイズ.Height;
                }
                
                TJAPlayer3.Tx.SongSelect_Header?.t2D描画(TJAPlayer3.app.Device, 0, 0);
                
                tTimerDraw((100 - ctTimer.n現在の値).ToString());

                tSongNumberDraw(1097, 167, NowSong.ToString());
                tSongNumberDraw(1190, 167, MaxSong.ToString());

                this.actInformation.On進行描画();

                #region[ 下部テキスト ]

                /*
                if (TJAPlayer3.Tx.SongSelect_Auto != null)
                {
                    if (TJAPlayer3.ConfigIni.b太鼓パートAutoPlay)
                    {
                        TJAPlayer3.Tx.SongSelect_Auto.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.SongSelect_Auto_X[0], TJAPlayer3.Skin.SongSelect_Auto_Y[0]);
                    }
                    if (TJAPlayer3.ConfigIni.nPlayerCount > 1 && TJAPlayer3.ConfigIni.b太鼓パートAutoPlay2P)
                    {
                        TJAPlayer3.Tx.SongSelect_Auto.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.SongSelect_Auto_X[1], TJAPlayer3.Skin.SongSelect_Auto_Y[1]);
                    }
                }
                */

                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    ModIcons.tDisplayModsMenu(40 + i * 980, 672, i);
                }

                



                if (TJAPlayer3.ConfigIni.bTokkunMode)
                    TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, "GAME: TRAINING MODE");
                if (TJAPlayer3.ConfigIni.eGameMode == EGame.完走叩ききりまショー)
                    TJAPlayer3.act文字コンソール.tPrint(0, 16, C文字コンソール.Eフォント種別.白, "GAME: SURVIVAL");
                if (TJAPlayer3.ConfigIni.eGameMode == EGame.完走叩ききりまショー激辛)
                    TJAPlayer3.act文字コンソール.tPrint(0, 16, C文字コンソール.Eフォント種別.白, "GAME: SURVIVAL HARD");
                if (TJAPlayer3.ConfigIni.bSuperHard)
                    TJAPlayer3.act文字コンソール.tPrint(0, 32, C文字コンソール.Eフォント種別.赤, "SUPER HARD MODE : ON");
                if (TJAPlayer3.ConfigIni.eScrollMode == EScrollMode.BMSCROLL)
                    TJAPlayer3.act文字コンソール.tPrint(0, 48, C文字コンソール.Eフォント種別.赤, "BMSCROLL : ON");
                else if (TJAPlayer3.ConfigIni.eScrollMode == EScrollMode.HBSCROLL)
                    TJAPlayer3.act文字コンソール.tPrint(0, 48, C文字コンソール.Eフォント種別.赤, "HBSCROLL : ON");

                #endregion

                this.actPreimageパネル.On進行描画();

                this.actPresound.On進行描画();

                this.act演奏履歴パネル.On進行描画();

                this.actShowCurrentPosition.On進行描画();                               // #27648 2011.3.28 yyagi

                // Select screen song
                if (base.eフェーズID == CStage.Eフェーズ.共通_通常状態)
                {
                    CSongSelectSongManager.playSongIfPossible();
                }

                if (this.ctDiffSelect移動待ち != null)
                    this.ctDiffSelect移動待ち.t進行();


                #region [Character & PuchiChara]


                //if (this.ctDonchan_Select.b終了値に達してない)

                for (int player = 0; player < TJAPlayer3.ConfigIni.nPlayerCount; player++)
                {
                    CCounter ___cc = CMenuCharacter._getReferenceCounter(CMenuCharacter.ECharacterAnimation.SELECT)[player];

                    int _charaId = TJAPlayer3.NamePlateConfig.data.Character[TJAPlayer3.GetActualPlayer(player)];

                    int chara_x = TJAPlayer3.Skin.Characters_Menu_X[_charaId][player];
                    int chara_y = TJAPlayer3.Skin.Characters_Menu_Y[_charaId][player];

                    int puchi_x = player == 0 ? 0 + 100 : 981 + 250;
                    int puchi_y = player == 0 ? 330 + 230 : 330 + 230;

                    if (___cc != null && ___cc.b終了値に達してない)
                    {
                        CMenuCharacter.tMenuDisplayCharacter(player, chara_x, chara_y, CMenuCharacter.ECharacterAnimation.SELECT);

                        this.PuchiChara.On進行描画(puchi_x, puchi_y, false, 255, false, player);
                    }
                    else
                    {
                        CCounter ___cj = CMenuCharacter._getReferenceCounter(CMenuCharacter.ECharacterAnimation.START)[player];


                        if (___cj != null && ___cj.n終了値 > 0)
                        {
                            CMenuCharacter.tMenuDisplayCharacter(player, chara_x, chara_y, CMenuCharacter.ECharacterAnimation.START);

                            this.PuchiChara.On進行描画(puchi_x, puchi_y, false, 255, false, player);
                        }
                        else
                        {
                            CMenuCharacter.tMenuDisplayCharacter(player, chara_x, chara_y, CMenuCharacter.ECharacterAnimation.NORMAL);

                            this.PuchiChara.On進行描画(puchi_x, puchi_y, false, 255, false, player);
                        }
                    }
                }


                #endregion

                #region [ Nameplate ]
                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.SongSelect_NamePlate_X[i], TJAPlayer3.Skin.SongSelect_NamePlate_Y[i], i);
                }
                #endregion

                #region [Pad displayables]

                int defaultTable = Math.Max(0, Math.Min((int)Difficulty.Edit + 1, TJAPlayer3.ConfigIni.nDefaultCourse));

                int[] currentPads = new int[2] {
                    defaultTable,
                    defaultTable };

                int tablesGap = 1034;

                //int currentPad = (int)Difficulty.Edit + 1;
                if (TJAPlayer3.stage選曲.act難易度選択画面.bIsDifficltSelect)
                {
                    if (TJAPlayer3.stage選曲.act難易度選択画面.n現在の選択行[0] >= 2)
                        currentPads[0] = TJAPlayer3.stage選曲.act難易度選択画面.n現在の選択行[0] - 2;
                    if (TJAPlayer3.ConfigIni.nPlayerCount > 1 && TJAPlayer3.stage選曲.act難易度選択画面.n現在の選択行[1] >= 2)
                        currentPads[1] = TJAPlayer3.stage選曲.act難易度選択画面.n現在の選択行[1] - 2;
                }


                /*
                TJAPlayer3.Tx.SongSelect_Table[currentPads[0]]?.t2D描画(TJAPlayer3.app.Device, 0, 0);
                if (TJAPlayer3.ConfigIni.nPlayerCount > 1)
                    TJAPlayer3.Tx.SongSelect_Table[currentPads[1]]?.t2D描画(TJAPlayer3.app.Device, tablesGap, 0);
                */


                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    int p = TJAPlayer3.GetActualPlayer(i);

                    TJAPlayer3.Tx.SongSelect_Table[currentPads[i]]?.t2D描画(TJAPlayer3.app.Device, i * tablesGap, 0);

                    CActSelect曲リスト.CScorePad[] SPArrRef = CSongDict.ScorePads[p];

                    // Current board
                    for (int j = 0; j < 10; j++)
                    {
                        tBoardNumberDraw(this.ptBoardNumber[j].X - 10 + i * tablesGap, this.ptBoardNumber[j].Y, j < 7 ?
                            SPArrRef[currentPads[i]].ScoreRankCount[j].ToString()
                            : SPArrRef[currentPads[i]].CrownCount[j - 7].ToString());
                    }

                }

                TJAPlayer3.Tx.SongSelect_Coin_Slot?.t2D描画(TJAPlayer3.app.Device, 0, 0,
                    new Rectangle(0, 0, 640 + ((TJAPlayer3.ConfigIni.nPlayerCount > 1) ? 640 : 0), 720));

                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    int p = TJAPlayer3.GetActualPlayer(i);

                    if (TJAPlayer3.NamePlateConfig.data.Medals[p] >= 0)
                        tBoardNumberDraw(this.ptBoardNumber[10].X - 10 + i * 1140, this.ptBoardNumber[10].Y, TJAPlayer3.NamePlateConfig.data.Medals[p].ToString());

                    #region [HiScore plate]

                    var song = this.r現在選択中の曲;

                    if (song != null && song.eノード種別 == C曲リストノード.Eノード種別.SCORE)
                    {
                        var closest = this.act曲リスト.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song);
                        var score = song.arスコア[closest];

                        if (score != null)
                        {
                            int posx = (i == 1) ? 1280 - this.ptBoardNumber[11].X : this.ptBoardNumber[11].X;
                            int displayedScore = 0;
                            int table = 0;

                            TJAPlayer3.Tx.SongSelect_High_Score?.t2D中心基準描画(TJAPlayer3.app.Device, posx, this.ptBoardNumber[11].Y);

                            if (this.n現在選択中の曲の難易度 > (int)Difficulty.Edit)
                                table = 0;
                            else if (currentPads[i] <= (int)Difficulty.Edit)
                                table = currentPads[i];
                            else
                                table = closest;

                            displayedScore = score.GPInfo[p].nHighScore[table];

                            if (this.n現在選択中の曲の難易度 <= (int)Difficulty.Edit)
                                TJAPlayer3.Tx.Dani_Difficulty_Cymbol.t2D中心基準描画(TJAPlayer3.app.Device,
                                    posx - 78,
                                    this.ptBoardNumber[11].Y + 2,
                                    new Rectangle(table * 53, 0, 53, 53));

                            tBoardNumberDraw(posx - 10, this.ptBoardNumber[11].Y + 6, displayedScore.ToString());
                        }

                    }

                    #endregion
                }

                #endregion

                #region [ Inputs ]

                // キー入力
                if (base.eフェーズID == CStage.Eフェーズ.共通_通常状態
                    && TJAPlayer3.act現在入力を占有中のプラグイン == null)
                {
                    #region [ 簡易CONFIGでMore、またはShift+F1: 詳細CONFIG呼び出し ]
                    if (actQuickConfig.bGotoDetailConfig)
                    {   // 詳細CONFIG呼び出し
                        actQuickConfig.tDeativatePopupMenu();
                        this.actPresound.tサウンド停止();
                        this.eフェードアウト完了時の戻り値 = E戻り値.コンフィグ呼び出し;  // #24525 2011.3.16 yyagi: [SHIFT]-[F1]でCONFIG呼び出し
                        this.actFIFO.tフェードアウト開始();
                        base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
                        TJAPlayer3.Skin.sound取消音.t再生する();
                        return 0;
                    }
                    #endregion

                    if (this.act曲リスト.isContextBoxOpened == true)
                    {
                        // Handle menu contexts
                        bool __done = this.act曲リスト.tMenuContextController(this.act曲リスト.latestContext);
                        if (__done == true)
                        {
                            if (this.act曲リスト.latestContext == eMenuContext.SearchByDifficulty)
                            {
                                #region [Trigger context box]

                                this.act曲リスト.r現在選択中の曲.list子リスト = CSongDict.tFetchSongsByDifficulty(
                                    this.act曲リスト.r現在選択中の曲,
                                    this.act曲リスト.tMenuContextGetVar(0),
                                    this.act曲リスト.tMenuContextGetVar(1));

                                CSongSelectSongManager.disable();

                                TJAPlayer3.Skin.sound決定音.t再生する();
                                this.act曲リスト.ctBarFlash.t開始(0, 2700, 1, TJAPlayer3.Timer);
                                this.act曲リスト.ctBoxOpen.t開始(200, 2700, 1.3f, TJAPlayer3.Timer);
                                this.act曲リスト.bBoxOpen = true;

                                //this.ctDonchan_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Donchan_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
                                CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);

                                #endregion
                            }

                            this.act曲リスト.tMenuContextDisable();
                        }
                    }
                    else if (!this.actSortSongs.bIsActivePopupMenu && !this.actQuickConfig.bIsActivePopupMenu && !this.act難易度選択画面.bIsDifficltSelect)
                    {
                        #region [ ESC ]
                        if ((TJAPlayer3.Pad.b押されたDGB(Eパッド.Cancel) || TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Escape)) && (this.act曲リスト.r現在選択中の曲 != null))// && (  ) ) )
                            if (this.act曲リスト.r現在選択中の曲.r親ノード == null)
                            {   // [ESC]
                                this.actPresound.tサウンド停止();
                                CSongSelectSongManager.enable();

                                TJAPlayer3.Skin.sound取消音.t再生する();
                                this.eフェードアウト完了時の戻り値 = E戻り値.タイトルに戻る;
                                this.actFIFO.tフェードアウト開始();
                                base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
                                return 0;
                            }
                            else
                            {
                                if (this.act曲リスト.ctBoxOpen.b終了値に達した || this.act曲リスト.ctBoxOpen.n現在の値 == 0)
                                {
                                    this.actPresound.tサウンド停止();
                                    CSongSelectSongManager.enable();

                                    TJAPlayer3.Skin.sound取消音.t再生する();
                                    this.act曲リスト.ctBarFlash.t開始(0, 2700, 1, TJAPlayer3.Timer);
                                    this.act曲リスト.ctBoxOpen.t開始(200, 2700, 1.3f, TJAPlayer3.Timer);
                                    this.act曲リスト.bBoxClose = true;
                                    //this.ctDonchan_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Donchan_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
                                    CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);
                                }
                            }
                        #endregion
                        #region [ Shift-F1: CONFIG画面 ]
                        if ((TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.RightShift) || TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.LeftShift)) &&
                            TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.F1))
                        {   // [SHIFT] + [F1] CONFIG
                            this.actPresound.tサウンド停止();
                            this.eフェードアウト完了時の戻り値 = E戻り値.コンフィグ呼び出し;  // #24525 2011.3.16 yyagi: [SHIFT]-[F1]でCONFIG呼び出し
                            this.actFIFO.tフェードアウト開始();
                            base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
                            TJAPlayer3.Skin.sound取消音.t再生する();
                            return 0;
                        }
                        #endregion
                        #region [ F2 簡易オプション ]
                        if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.F2))
                        {
                            TJAPlayer3.Skin.sound変更音.t再生する();
                            this.actQuickConfig.tActivatePopupMenu(E楽器パート.DRUMS);
                        }
                        #endregion
                        #region [ F3 1PオートON/OFF ]
                        if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.F3))
                        {
                            TJAPlayer3.Skin.sound変更音.t再生する();
                            C共通.bToggleBoolian(ref TJAPlayer3.ConfigIni.b太鼓パートAutoPlay);
                        }
                        #endregion
                        #region [ F4 2PオートON/OFF ]
                        if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.F4))
                        {
                            if (TJAPlayer3.ConfigIni.nPlayerCount > 1)
                            {
                                TJAPlayer3.Skin.sound変更音.t再生する();
                                C共通.bToggleBoolian(ref TJAPlayer3.ConfigIni.b太鼓パートAutoPlay2P);
                            }
                        }
                        #endregion
                        #region [ F5 スーパーハード ]
                        if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.F5))
                        {
                            TJAPlayer3.Skin.sound変更音.t再生する();
                            C共通.bToggleBoolian(ref TJAPlayer3.ConfigIni.bSuperHard);
                        }
                        #endregion
                        #region [ F6 SCROLL ]
                        if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.F6))
                        {
                            TJAPlayer3.Skin.sound変更音.t再生する();
                            TJAPlayer3.ConfigIni.bスクロールモードを上書き = true;
                            switch ((int)TJAPlayer3.ConfigIni.eScrollMode)
                            {
                                case 0:
                                    TJAPlayer3.ConfigIni.eScrollMode = EScrollMode.BMSCROLL;
                                    break;
                                case 1:
                                    TJAPlayer3.ConfigIni.eScrollMode = EScrollMode.HBSCROLL;
                                    break;
                                case 2:
                                    TJAPlayer3.ConfigIni.eScrollMode = EScrollMode.Normal;
                                    TJAPlayer3.ConfigIni.bスクロールモードを上書き = false;
                                    break;
                            }
                        }
                        #endregion
                        #region [ F7 TokkunMode ]
                        if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.F7))
                        {
                            if (TJAPlayer3.ConfigIni.nPlayerCount < 2)
                            {
                                TJAPlayer3.Skin.sound変更音.t再生する();
                                C共通.bToggleBoolian(ref TJAPlayer3.ConfigIni.bTokkunMode);
                            }
                        }
                        #endregion
                        #region [ F8 ランダム選曲 ]
                        if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.F8))
                        {
                            if (TJAPlayer3.Skin.sound曲決定音.b読み込み成功)
                                TJAPlayer3.Skin.sound曲決定音.t再生する();
                            else
                                TJAPlayer3.Skin.sound決定音.t再生する();
                            this.t曲をランダム選択する();
                        }
                        #endregion 

                        if (this.act曲リスト.r現在選択中の曲 != null)
                        {
                            
                            if (this.act曲リスト.ctBoxOpen.b終了値に達した || this.act曲リスト.ctBoxOpen.n現在の値 == 0)
                            {
                                if (!this.bスクロール中)
                                {
                                    #region [ Decide ]
                                    if ((TJAPlayer3.Pad.b押されたDGB(Eパッド.Decide) ||
                                    ((TJAPlayer3.ConfigIni.bEnterがキー割り当てのどこにも使用されていない && TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Return)))))
                                    {

                                        if (this.act曲リスト.r現在選択中の曲 != null)
                                        {
                                            switch (this.act曲リスト.r現在選択中の曲.eノード種別)
                                            {
                                                case C曲リストノード.Eノード種別.SCORE:
                                                    {
                                                        // Maybe auxilliary don select here too ?

                                                        if (this.n現在選択中の曲の難易度 >= (int)Difficulty.Tower)
                                                        {
                                                            if (TJAPlayer3.ConfigIni.nPlayerCount == 1)
                                                            {
                                                                // Init tower variables 
                                                                if (this.n現在選択中の曲の難易度 == (int)Difficulty.Tower)
                                                                    CFloorManagement.reinitialize(this.r現在選択中の曲.arスコア[(int)Difficulty.Tower].譜面情報.nLife);
                                                                
                                                                TJAPlayer3.Skin.sound曲決定音.t再生する();

                                                                this.t曲を選択する();
                                                            }
                                                            else
                                                            {
                                                                TJAPlayer3.Skin.soundError.t再生する();
                                                            }
                                                        }
                                                        else
                                                        {
                                                            // Called here
                                                            TJAPlayer3.Skin.sound決定音.t再生する();
                                                            this.act難易度選択画面.bIsDifficltSelect = true;
                                                            this.act難易度選択画面.t選択画面初期化();
                                                            this.act曲リスト.ctBarFlash.t開始(0, 2700, 1, TJAPlayer3.Timer);
                                                            this.act曲リスト.ctDifficultyIn.t開始(0, 3200, 1, TJAPlayer3.Timer);
                                                            //this.ctDonchan_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Donchan_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
                                                            CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);
                                                        }
                                                    }
                                                    break;
                                                case C曲リストノード.Eノード種別.BOX:
                                                    {

                                                        #region [Pre-generated folders]

                                                        if (this.act曲リスト.r現在選択中の曲.strジャンル == "Favorite")
                                                        {
                                                            this.act曲リスト.r現在選択中の曲.list子リスト = CSongDict.tFetchFavoriteFolder(this.act曲リスト.r現在選択中の曲);
                                                        }
                                                        else if (this.act曲リスト.r現在選択中の曲.strジャンル == "最近遊んだ曲")
                                                        {
                                                            this.act曲リスト.r現在選択中の曲.list子リスト = CSongDict.tFetchRecentlyPlayedSongsFolder(this.act曲リスト.r現在選択中の曲);
                                                        }
                                                        else if (this.act曲リスト.r現在選択中の曲.strジャンル == "SearchD")
                                                        {
                                                            // Todo : Add a small prompt to choose the difficulty
                                                            this.act曲リスト.tMenuContextTrigger(eMenuContext.SearchByDifficulty);
                                                            TJAPlayer3.Skin.sound決定音.t再生する();
                                                            goto Decided;
                                                            //this.act曲リスト.r現在選択中の曲.list子リスト = CSongDict.tFetchSongsByDifficulty(this.act曲リスト.r現在選択中の曲, (int)Difficulty.Oni, 8);
                                                        }

                                                        #endregion

                                                        CSongSelectSongManager.disable();

                                                        TJAPlayer3.Skin.sound決定音.t再生する();
                                                        this.act曲リスト.ctBarFlash.t開始(0, 2700, 1, TJAPlayer3.Timer);
                                                        this.act曲リスト.ctBoxOpen.t開始(200, 2700, 1.3f, TJAPlayer3.Timer);
                                                        this.act曲リスト.bBoxOpen = true;
                                                        //this.ctDonchan_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Donchan_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
                                                        CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);
                                                    }
                                                    break;
                                                case C曲リストノード.Eノード種別.BACKBOX:
                                                    {
                                                        // TOJIRU
                                                        CSongSelectSongManager.enable();

                                                        TJAPlayer3.Skin.sound取消音.t再生する();
                                                        this.act曲リスト.ctBarFlash.t開始(0, 2700, 1, TJAPlayer3.Timer);
                                                        this.act曲リスト.ctBoxOpen.t開始(200, 2700, 1.3f, TJAPlayer3.Timer);
                                                        this.act曲リスト.bBoxClose = true;
                                                        //this.ctDonchan_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Donchan_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
                                                        CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);
                                                    }
                                                    break;
                                                case C曲リストノード.Eノード種別.RANDOM:
                                                    {
                                                        this.t曲をランダム選択する();

                                                        //this.ctDonchan_Select.t開始(0, TJAPlayer3.Tx.SongSelect_Donchan_Select.Length - 1, 1000 / 45, TJAPlayer3.Timer);
                                                        CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.SELECT);
                                                    }
                                                    break;
                                            }

                                            goto Decided;

                                        }
                                    }
                                    #endregion

                                }

                                #region [ Favorite ]

                                if (!this.bスクロール中)
                                {
                                    if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.LeftControl))
                                    {
                                        CSongUniqueID csu = this.r現在選択中の曲.uniqueId;

                                        if (csu != null)
                                        {
                                            TJAPlayer3.Skin.sound決定音.t再生する();
                                            TJAPlayer3.Favorites.tToggleFavorite(csu.data.id);
                                        }
                                    }
                                }

                                #endregion

                                #region [ Up ]
                                if (!this.bスクロール中)
                                {
                                    this.ctキー反復用.Up.tキー反復(TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.LeftArrow), new CCounter.DGキー処理(this.tカーソルを上へ移動する));
                                    //this.ctキー反復用.Up.tキー反復( CDTXMania.Input管理.Keyboard.bキーが押されている( (int) SlimDXKeys.Key.UpArrow ) || CDTXMania.Input管理.Keyboard.bキーが押されている( (int) SlimDXKeys.Key.LeftArrow ), new CCounter.DGキー処理( this.tカーソルを上へ移動する ) );
                                    if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LeftChange))
                                    {
                                        this.tカーソルを上へ移動する();
                                    }
                                }
                                else
                                {
                                    if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LeftChange))
                                    {
                                        //this.ctDonchan_Jump[0].t開始(0, TJAPlayer3.Tx.SongSelect_Donchan_Jump.Length + 8, 1000 / 45, TJAPlayer3.Timer);
                                        //this.ctDonchan_Jump[1].t開始(0, TJAPlayer3.Tx.SongSelect_Donchan_Jump.Length + 8, 1000 / 45, TJAPlayer3.Timer);
                                        CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.START);

                                        for (int i = 0; i < 7; i++) tカーソルスキップ(true);
                                    }
                                }
                                #endregion

                                #region [ Down ]
                                if (!this.bスクロール中)
                                {
                                    this.ctキー反復用.Down.tキー反復(TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.RightArrow), new CCounter.DGキー処理(this.tカーソルを下へ移動する));
                                    //this.ctキー反復用.Down.tキー反復( CDTXMania.Input管理.Keyboard.bキーが押されている( (int) SlimDXKeys.Key.DownArrow ) || CDTXMania.Input管理.Keyboard.bキーが押されている( (int) SlimDXKeys.Key.RightArrow ), new CCounter.DGキー処理( this.tカーソルを下へ移動する ) );
                                    
                                    if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RightChange))
                                    {
                                        this.tカーソルを下へ移動する();
                                    }
                                }
                                else
                                {
                                    if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RightChange))
                                    {
                                        //this.ctDonchan_Jump[0].t開始(0, TJAPlayer3.Tx.SongSelect_Donchan_Jump.Length + 8, 1000 / 45, TJAPlayer3.Timer);
                                        //this.ctDonchan_Jump[1].t開始(0, TJAPlayer3.Tx.SongSelect_Donchan_Jump.Length + 8, 1000 / 45, TJAPlayer3.Timer);
                                        CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.START);

                                        for (int i = 0; i < 7; i++) tカーソルスキップ(false);
                                    }
                                }
                            #endregion

                            Decided:;

                            }
                            #region [ Upstairs ]
                            if (((this.act曲リスト.r現在選択中の曲 != null) && (this.act曲リスト.r現在選択中の曲.r親ノード != null)) && (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.FT) || TJAPlayer3.Pad.b押されたGB(Eパッド.Cancel)))
                            {
                                this.actPresound.tサウンド停止();
                                TJAPlayer3.Skin.sound取消音.t再生する();
                                this.act曲リスト.tBOXを出る();
                                this.t選択曲変更通知();
                            }
                            #endregion
                            #region [ BDx2: 簡易CONFIG ]
                            if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Space))
                            {
                                TJAPlayer3.Skin.sound変更音.t再生する();
                                this.actSortSongs.tActivatePopupMenu(E楽器パート.DRUMS, ref this.act曲リスト);
                            }
                            #endregion
                            #region [ HHx2: 難易度変更 ]
                            if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.HH) || TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.HHO))
                            {   // [HH]x2 難易度変更
                                CommandHistory.Add(E楽器パート.DRUMS, EパッドFlag.HH);
                                EパッドFlag[] comChangeDifficulty = new EパッドFlag[] { EパッドFlag.HH, EパッドFlag.HH };
                                if (CommandHistory.CheckCommand(comChangeDifficulty, E楽器パート.DRUMS))
                                {
                                    Debug.WriteLine("ドラムス難易度変更");
                                    this.act曲リスト.t難易度レベルをひとつ進める();
                                    TJAPlayer3.Skin.sound変更音.t再生する();
                                }
                            }
                            #endregion
                        }
                    }




                    #region [ Minus & Equals Sound Group Level ]
                    KeyboardSoundGroupLevelControlHandler.Handle(
                        TJAPlayer3.Input管理.Keyboard, TJAPlayer3.SoundGroupLevelController, TJAPlayer3.Skin, true);
                    #endregion

                    this.actSortSongs.t進行描画();
                    this.actQuickConfig.t進行描画();
                }

                #endregion

                //------------------------------
                if (this.act難易度選択画面.bIsDifficltSelect)
                {
                    if (this.act曲リスト.ctDifficultyIn.n現在の値 >= 1255)
                    {
                        this.act難易度選択画面.On進行描画();
                    }
                }
                //------------------------------


                if (TJAPlayer3.ConfigIni.nPlayerCount == 1)
                {
                    var opacity = 0;

                    if (ctCreditAnime.n現在の値 <= 510)
                        opacity = ctCreditAnime.n現在の値 / 2;
                    else if (ctCreditAnime.n現在の値 <= 4500 - 510)
                        opacity = 255;
                    else
                        opacity = 255 - ((ctCreditAnime.n現在の値 - (4500 - 510)) / 2);

                    TJAPlayer3.Tx.SongSelect_Credit.Opacity = opacity;

                    TJAPlayer3.Tx.SongSelect_Credit.t2D描画(TJAPlayer3.app.Device, 0, 0);
                }

                for (int i = 0; i < 2; i++)
                {
                    CCounter ___cs = CMenuCharacter._getReferenceCounter(CMenuCharacter.ECharacterAnimation.START)[i];

                    //if (this.ctDonchan_Jump[i].n現在の値 >= this.ctDonchan_Jump[i].n終了値)
                    if (___cs != null && ___cs.n現在の値 >= ___cs.n終了値)
                    {
                        //this.ctDonchan_Jump[i].t停止();
                        ___cs.t停止();

                        if (!this.act難易度選択画面.bIsDifficltSelect)
                        {
                            //this.ctDonchan_Jump[i].n現在の値 = 0;
                            ___cs.n現在の値 = 0;
                        }
                    }
                }

                if (act難易度選択画面.bOption[0]) actPlayOption.On進行描画(0);
                if (act難易度選択画面.bOption[1]) actPlayOption.On進行描画(1);

                switch (base.eフェーズID)
                {
                    case CStage.Eフェーズ.共通_フェードイン:
                        if (this.actFIFO.On進行描画() != 0)
                        {
                            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
                        }
                        break;

                    case CStage.Eフェーズ.共通_フェードアウト:
                        if (this.actFIFO.On進行描画() == 0)
                        {
                            break;
                        }
                        return (int)this.eフェードアウト完了時の戻り値;

                    case CStage.Eフェーズ.選曲_結果画面からのフェードイン:
                        if (this.actFIfrom結果画面.On進行描画() != 0)
                        {
                            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
                        }
                        break;

                    case CStage.Eフェーズ.選曲_NowLoading画面へのフェードアウト:
                        if (this.actFOtoNowLoading.On進行描画() == 0)
                        {
                            break;
                        }
                        return (int)this.eフェードアウト完了時の戻り値;
                }
            }
            return 0;
        }
        public enum E戻り値 : int
        {
            継続,
            タイトルに戻る,
            選曲した,
            オプション呼び出し,
            コンフィグ呼び出し,
            スキン変更
        }


        // その他

        #region [ private ]
        //-----------------
        [StructLayout(LayoutKind.Sequential)]
        private struct STキー反復用カウンタ
        {
            public CCounter Up;
            public CCounter Down;
            public CCounter this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return this.Up;

                        case 1:
                            return this.Down;
                    }
                    throw new IndexOutOfRangeException();
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            this.Up = value;
                            return;

                        case 1:
                            this.Down = value;
                            return;
                    }
                    throw new IndexOutOfRangeException();
                }
            }
        }
        private CCounter ctTimer;
        private CCounter ctCreditAnime;
        private Random[] r = new Random[3];
        public CCounter ctBackgroundFade;
        public string NowGenre;
        public string OldGenre;
        public int NowBg;
        public int OldBg;
        public Color NowBgColor = Color.White;
        public Color OldBgColor = Color.White;
        public bool NowUseGenre;
        public bool OldUseGenre;
        private CActSelectArtistComment actArtistComment;
        private CActFIFOBlack actFIFO;
        private CActFIFOBlack actFIfrom結果画面;
        //private CActFIFOBlack actFOtoNowLoading;
        public CActFIFOStart actFOtoNowLoading;
        private CActSelectInformation actInformation;
        private CActSelectPreimageパネル actPreimageパネル;
        public CActSelectPresound actPresound;
        private CActオプションパネル actオプションパネル;
        private CActSelectステータスパネル actステータスパネル;
        public CActSelect演奏履歴パネル act演奏履歴パネル;
        public CActSelect曲リスト act曲リスト;
        private CActSelectShowCurrentPosition actShowCurrentPosition;
        public CActSelect難易度選択画面 act難易度選択画面;
        public CActPlayOption actPlayOption;

        public CActSortSongs actSortSongs;
        private CActSelectQuickConfig actQuickConfig;

        private const int MaxSong = 3;
        public int NowSong = 1;

        //private CCounter ctDonchan_Normal;
        //private CCounter ctDonchan_Select;
        //public CCounter[] ctDonchan_Jump = new CCounter[2];

        private PuchiChara PuchiChara;

        private int nGenreBack;
        private int nOldGenreBack;
        public bool bBGM再生済み;
        public bool bBGMIn再生した;
        private STキー反復用カウンタ ctキー反復用;
        public CCounter ct登場時アニメ用共通;
        private CCounter ct背景スクロール用タイマー;
        private E戻り値 eフェードアウト完了時の戻り値;
        private Font ftフォント;
        //private CTexture tx下部パネル;
        //private CTexture tx上部パネル;
        //private CTexture tx背景;
        //      private CTexture[] txジャンル別背景 = new CTexture[9];
        //      private CTexture[] tx難易度別背景 = new CTexture[5];
        //      private CTexture tx難易度名;
        //      private CTexture tx下部テキスト;
        private CCounter ctDiffSelect移動待ち;

        private STNumber[] stTimer = new STNumber[10];
        private STNumber[] stSongNumber = new STNumber[10];
        private STNumber[] stBoardNumber = new STNumber[10];

        public struct STNumber
        {
            public char ch;
            public Point pt;
        }
        private struct STCommandTime        // #24063 2011.1.16 yyagi コマンド入力時刻の記録用
        {
            public E楽器パート eInst;        // 使用楽器
            public EパッドFlag ePad;       // 押されたコマンド(同時押しはOR演算で列挙する)
            public long time;               // コマンド入力時刻
        }

        private Point[] ptBoardNumber =
            { new Point(72, 283), new Point(135, 283), new Point(200, 283), new Point(72, 258), new Point(135, 258), new Point(200, 258), new Point(200, 233), new Point(72, 311), new Point(135, 311), new Point(200, 311), new Point(84, 360), new Point(124, 416) };

        public void tBoardNumberDraw(int x, int y, string str)
        {
            for (int j = 0; j < str.Length; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (str[j] == stSongNumber[i].ch)
                    {
                        TJAPlayer3.Tx.SongSelect_BoardNumber.t2D描画(TJAPlayer3.app.Device, x - (str.Length * 15 + 9 * str.Length - str.Length * 15) / 2 + 15 / 2, (float)y - 17 / 2, new RectangleF(stBoardNumber[i].pt.X, stBoardNumber[i].pt.Y, 15, 17));
                        x += 9;
                    }
                }
            }
        }

        private void tSongNumberDraw(int x, int y, string str)
        {
            for (int j = 0; j < str.Length; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (str[j] == stSongNumber[i].ch)
                    {
                        TJAPlayer3.Tx.SongSelect_Song_Number.t2D描画(TJAPlayer3.app.Device, x - (str.Length * 27 + 27 * str.Length - str.Length * 27) / 2 + 27 / 2, (float)y, new RectangleF(stSongNumber[i].pt.X, stSongNumber[i].pt.Y, 27, 29));
                        x += str.Length >= 2 ? 16 : 27;
                    }
                }
            }
        }

        private void tTimerDraw(string str)
        {
            int x = 1171, y = 57;

            for (int j = 0; j < str.Length; j++)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (TJAPlayer3.ConfigIni.bEnableCountdownTimer)
                    {
                        if (str[j] == stTimer[i].ch)
                        {
                            TJAPlayer3.Tx.SongSelect_Timer.t2D描画(TJAPlayer3.app.Device, x - (str.Length * 46 + 46 * str.Length - str.Length * 46) / 2 + 46 / 2, (float)y, new RectangleF(stTimer[i].pt.X, stTimer[i].pt.Y, 46, 64));
                            x += str.Length >= 3 ? 40 : 46;
                        }
                    }
                }
            }
        }

        private class CCommandHistory       // #24063 2011.1.16 yyagi コマンド入力履歴を保持_確認するクラス
        {
            readonly int buffersize = 16;
            private List<STCommandTime> stct;

            public CCommandHistory()        // コンストラクタ
            {
                stct = new List<STCommandTime>(buffersize);
            }

            /// <summary>
            /// コマンド入力履歴へのコマンド追加
            /// </summary>
            /// <param name="_eInst">楽器の種類</param>
            /// <param name="_ePad">入力コマンド(同時押しはOR演算で列挙すること)</param>
            public void Add(E楽器パート _eInst, EパッドFlag _ePad)
            {
                STCommandTime _stct = new STCommandTime
                {
                    eInst = _eInst,
                    ePad = _ePad,
                    time = TJAPlayer3.Timer.n現在時刻
                };

                if (stct.Count >= buffersize)
                {
                    stct.RemoveAt(0);
                }
                stct.Add(_stct);
                //Debug.WriteLine( "CMDHIS: 楽器=" + _stct.eInst + ", CMD=" + _stct.ePad + ", time=" + _stct.time );
            }
            public void RemoveAt(int index)
            {
                stct.RemoveAt(index);
            }

            /// <summary>
            /// コマンド入力に成功しているか調べる
            /// </summary>
            /// <param name="_ePad">入力が成功したか調べたいコマンド</param>
            /// <param name="_eInst">対象楽器</param>
            /// <returns>コマンド入力成功時true</returns>
            public bool CheckCommand(EパッドFlag[] _ePad, E楽器パート _eInst)
            {
                int targetCount = _ePad.Length;
                int stciCount = stct.Count;
                if (stciCount < targetCount)
                {
                    //Debug.WriteLine("NOT start checking...stciCount=" + stciCount + ", targetCount=" + targetCount);
                    return false;
                }

                long curTime = TJAPlayer3.Timer.n現在時刻;
                //Debug.WriteLine("Start checking...targetCount=" + targetCount);
                for (int i = targetCount - 1, j = stciCount - 1; i >= 0; i--, j--)
                {
                    if (_ePad[i] != stct[j].ePad)
                    {
                        //Debug.WriteLine( "CMD解析: false targetCount=" + targetCount + ", i=" + i + ", j=" + j + ": ePad[]=" + _ePad[i] + ", stci[j] = " + stct[j].ePad );
                        return false;
                    }
                    if (stct[j].eInst != _eInst)
                    {
                        //Debug.WriteLine( "CMD解析: false " + i );
                        return false;
                    }
                    if (curTime - stct[j].time > 500)
                    {
                        //Debug.WriteLine( "CMD解析: false " + i + "; over 500ms" );
                        return false;
                    }
                    curTime = stct[j].time;
                }

                //Debug.Write( "CMD解析: 成功!(" + _ePad.Length + ") " );
                //for ( int i = 0; i < _ePad.Length; i++ ) Debug.Write( _ePad[ i ] + ", " );
                //Debug.WriteLine( "" );
                //stct.RemoveRange( 0, targetCount );			// #24396 2011.2.13 yyagi 
                stct.Clear();                                   // #24396 2011.2.13 yyagi Clear all command input history in case you succeeded inputting some command

                return true;
            }
        }
        private CCommandHistory CommandHistory;

        private void tカーソルを下へ移動する()
        {
            if ((this.act曲リスト.r次の曲(r現在選択中の曲).eノード種別 == C曲リストノード.Eノード種別.SCORE) || this.act曲リスト.r次の曲(r現在選択中の曲).eノード種別 == C曲リストノード.Eノード種別.BACKBOX)
            {
                TJAPlayer3.stage選曲.bBGMIn再生した = false;
                TJAPlayer3.Skin.bgm選曲画面イン.n位置_現在のサウンド = 0;
                TJAPlayer3.Skin.bgm選曲画面.n位置_現在のサウンド = 0;

                CSongSelectSongManager.disable();
            }
            else
            {
                CSongSelectSongManager.enable();
                CSongSelectSongManager.playSongIfPossible();
            }
            this.ctBackgroundFade.t開始(0, 600, 1, TJAPlayer3.Timer);
            if (this.act曲リスト.ctBarOpen.n現在の値 >= 200 || this.ctBackgroundFade.n現在の値 >= 600 - 255)
            {
                TJAPlayer3.stage選曲.OldGenre = this.r現在選択中の曲.strジャンル;
                TJAPlayer3.stage選曲.OldUseGenre = !this.r現在選択中の曲.isChangedBgType;
                TJAPlayer3.stage選曲.OldBg = this.r現在選択中の曲.BgType;
                TJAPlayer3.stage選曲.OldBgColor = this.r現在選択中の曲.BgColor;
            }
                
            this.act曲リスト.t次に移動();
            TJAPlayer3.Skin.soundカーソル移動音.t再生する();
        }
        private void tカーソルを上へ移動する()
        {
            if ((this.act曲リスト.r前の曲(r現在選択中の曲).eノード種別 == C曲リストノード.Eノード種別.SCORE) || this.act曲リスト.r前の曲(r現在選択中の曲).eノード種別 == C曲リストノード.Eノード種別.BACKBOX)
            {
                TJAPlayer3.stage選曲.bBGMIn再生した = false;

                CSongSelectSongManager.disable();
            }
            else
            {
                CSongSelectSongManager.enable();
                CSongSelectSongManager.playSongIfPossible();
            }

            this.ctBackgroundFade.t開始(0, 600, 1, TJAPlayer3.Timer);
            if (this.act曲リスト.ctBarOpen.n現在の値 >= 200 || this.ctBackgroundFade.n現在の値 >= 600 - 255)
            {
                TJAPlayer3.stage選曲.OldGenre = this.r現在選択中の曲.strジャンル;
                TJAPlayer3.stage選曲.OldUseGenre = !this.r現在選択中の曲.isChangedBgType;
                TJAPlayer3.stage選曲.OldBg = this.r現在選択中の曲.BgType;
                TJAPlayer3.stage選曲.OldBgColor = this.r現在選択中の曲.BgColor;
            }           
                
            this.act曲リスト.t前に移動();
            TJAPlayer3.Skin.soundカーソル移動音.t再生する();
        }
        private void tカーソルスキップ(bool Up)
        {
            this.ctBackgroundFade.t開始(0, 600, 1, TJAPlayer3.Timer);
            if (this.act曲リスト.ctBarOpen.n現在の値 >= 200 || this.ctBackgroundFade.n現在の値 >= 600 - 255)
            {
                TJAPlayer3.stage選曲.OldGenre = this.r現在選択中の曲.strジャンル;
                TJAPlayer3.stage選曲.OldUseGenre = !this.r現在選択中の曲.isChangedBgType;
                TJAPlayer3.stage選曲.OldBg = this.r現在選択中の曲.BgType;
                TJAPlayer3.stage選曲.OldBgColor = this.r現在選択中の曲.BgColor;
            }

            if (Up) this.act曲リスト.t前に移動();
            else this.act曲リスト.t次に移動();

            TJAPlayer3.Skin.soundSkip.t再生する();
        }

        private void t曲をランダム選択する()
        {
            C曲リストノード song = this.act曲リスト.r現在選択中の曲;
            if ((song.stackランダム演奏番号.Count == 0) || (song.listランダム用ノードリスト == null))
            {
                if (song.listランダム用ノードリスト == null)
                {
                    song.listランダム用ノードリスト = this.t指定された曲が存在する場所の曲を列挙する_子リスト含む(song);
                }
                int count = song.listランダム用ノードリスト.Count;
                if (count == 0)
                {
                    return;
                }
                int[] numArray = new int[count];
                for (int i = 0; i < count; i++)
                {
                    numArray[i] = i;
                }
                for (int j = 0; j < (count * 1.5); j++)
                {
                    int index = TJAPlayer3.Random.Next(count);
                    int num5 = TJAPlayer3.Random.Next(count);
                    int num6 = numArray[num5];
                    numArray[num5] = numArray[index];
                    numArray[index] = num6;
                }
                for (int k = 0; k < count; k++)
                {
                    song.stackランダム演奏番号.Push(numArray[k]);
                }

                if (TJAPlayer3.ConfigIni.bLogDTX詳細ログ出力)
                {
                    StringBuilder builder = new StringBuilder(0x400);
                    builder.Append(string.Format("ランダムインデックスリストを作成しました: {0}曲: ", song.stackランダム演奏番号.Count));
                    for (int m = 0; m < count; m++)
                    {
                        builder.Append(string.Format("{0} ", numArray[m]));
                    }
                    Trace.TraceInformation(builder.ToString());
                }
            }

            // Third assignment
            this.r確定された曲 = song.listランダム用ノードリスト[song.stackランダム演奏番号.Pop()];
            this.n確定された曲の難易度[0] = this.act曲リスト.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(this.r確定された曲);
            if (TJAPlayer3.ConfigIni.nPlayerCount > 1)
            {
                this.n確定された曲の難易度[1] = this.act曲リスト.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(this.r確定された曲);
            }
                
            this.r確定されたスコア = this.r確定された曲.arスコア[this.n確定された曲の難易度[0]];
            this.str確定された曲のジャンル = this.r確定された曲.strジャンル;

            TJAPlayer3.Skin.sound曲決定音.t再生する();

            this.eフェードアウト完了時の戻り値 = E戻り値.選曲した;
            this.actFOtoNowLoading.tフェードアウト開始();                    // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
            base.eフェーズID = CStage.Eフェーズ.選曲_NowLoading画面へのフェードアウト;

            #region [Log]

            if (TJAPlayer3.ConfigIni.bLogDTX詳細ログ出力)
            {
                int[] numArray2 = song.stackランダム演奏番号.ToArray();
                StringBuilder builder2 = new StringBuilder(0x400);
                builder2.Append("ランダムインデックスリスト残り: ");
                if (numArray2.Length > 0)
                {
                    for (int n = 0; n < numArray2.Length; n++)
                    {
                        builder2.Append(string.Format("{0} ", numArray2[n]));
                    }
                }
                else
                {
                    builder2.Append("(なし)");
                }
                Trace.TraceInformation(builder2.ToString());
            }

            #endregion

            CSongSelectSongManager.stopSong();
            
        }
        private void t曲を選択する()
        {
            // First assignation
            this.r確定された曲 = this.act曲リスト.r現在選択中の曲;
            this.r確定されたスコア = this.act曲リスト.r現在選択中のスコア;

            this.n確定された曲の難易度[0] = this.act曲リスト.n現在選択中の曲の現在の難易度レベル;
            this.str確定された曲のジャンル = this.r確定された曲.strジャンル;

            if ((this.r確定された曲 != null) && (this.r確定されたスコア != null))
            {
                this.eフェードアウト完了時の戻り値 = E戻り値.選曲した;
                this.actFOtoNowLoading.tフェードアウト開始();                // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
                base.eフェーズID = CStage.Eフェーズ.選曲_NowLoading画面へのフェードアウト;
            }
            // TJAPlayer3.Skin.bgm選曲画面.t停止する();
            CSongSelectSongManager.stopSong();
        }
        public void t曲を選択する(int nCurrentLevel, int player)
        {
            // Second assignation
            this.r確定された曲 = this.act曲リスト.r現在選択中の曲;
            this.r確定されたスコア = this.act曲リスト.r現在選択中のスコア;

            this.n確定された曲の難易度[player] = nCurrentLevel;
            this.str確定された曲のジャンル = this.r確定された曲.strジャンル;

            if ((this.r確定された曲 != null) && (this.r確定されたスコア != null))
            {
                this.eフェードアウト完了時の戻り値 = E戻り値.選曲した;
                this.actFOtoNowLoading.tフェードアウト開始();                // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
                base.eフェーズID = CStage.Eフェーズ.選曲_NowLoading画面へのフェードアウト;
            }

            // TJAPlayer3.Skin.bgm選曲画面.t停止する();
            CSongSelectSongManager.stopSong();
        }

        // Foreach randomly selectable songs
        private List<C曲リストノード> t指定された曲が存在する場所の曲を列挙する_子リスト含む(C曲リストノード song)
        {
            List<C曲リストノード> list = new List<C曲リストノード>();
            song = song.r親ノード;
            if ((song == null) && (TJAPlayer3.Songs管理.list曲ルート.Count > 0))
            {
                foreach (C曲リストノード c曲リストノード in TJAPlayer3.Songs管理.list曲ルート)
                {
                    if ((c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.SCORE) || (c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.SCORE_MIDI))
                    {
                        // Don't add Dan/Tower charts for Random
                        int diff = this.act曲リスト.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(c曲リストノード);
                        if (diff < (int)Difficulty.Tower)
                            list.Add(c曲リストノード);
                    }
                    if ((c曲リストノード.list子リスト != null) && TJAPlayer3.ConfigIni.bランダムセレクトで子BOXを検索対象とする)
                    {
                        this.t指定された曲の子リストの曲を列挙する_孫リスト含む(c曲リストノード, ref list);
                    }
                }
                return list;
            }
            this.t指定された曲の子リストの曲を列挙する_孫リスト含む(song, ref list);
            return list;
        }

        private void t指定された曲の子リストの曲を列挙する_孫リスト含む(C曲リストノード r親, ref List<C曲リストノード> list)
        {
            if ((r親 != null) && (r親.list子リスト != null))
            {
                foreach (C曲リストノード c曲リストノード in r親.list子リスト)
                {
                    if ((c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.SCORE) || (c曲リストノード.eノード種別 == C曲リストノード.Eノード種別.SCORE_MIDI))
                    {
                        // Don't add Dan/Tower charts for Random
                        int diff = this.act曲リスト.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(c曲リストノード);
                        if (diff < (int)Difficulty.Tower)
                            list.Add(c曲リストノード);
                    }
                    if ((c曲リストノード.list子リスト != null) && TJAPlayer3.ConfigIni.bランダムセレクトで子BOXを検索対象とする)
                    {
                        this.t指定された曲の子リストの曲を列挙する_孫リスト含む(c曲リストノード, ref list);
                    }
                }
            }
        }

        public int nStrジャンルtoNum(string strジャンル)
        {
            return this.act曲リスト.nStrジャンルtoNumBox(strジャンル);
        }
        //-----------------
        #endregion
    }
}