using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using FDK;
using static TJAPlayer3.CActSelect曲リスト;
using System.Diagnostics;

namespace TJAPlayer3
{
    class CStage段位選択 : CStage
    {
        public CStage段位選択()
        {
            base.eステージID = Eステージ.段位選択;
            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;

            base.ChildActivities.Add(this.段位リスト = new CActSelect段位リスト());

            base.ChildActivities.Add(this.actFOtoNowLoading = new CActFIFOStart());
            base.ChildActivities.Add(this.段位挑戦選択画面 = new CActSelect段位挑戦選択画面());
            base.ChildActivities.Add(this.actFOtoTitle = new CActFIFOBlack());
            base.ChildActivities.Add(this.actPlayOption = new CActPlayOption());
            base.ChildActivities.Add(this.PuchiChara = new PuchiChara());
        }

        public override void Activate()
        {
            if (base.IsActivated)
                return;

            this.b選択した = false;

            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
            this.eフェードアウト完了時の戻り値 = E戻り値.継続;

            ct待機 = new CCounter();
            ctDonchan_In = new CCounter();

            // ctDonchan_Normal = new CCounter(0, TJAPlayer3.Tx.SongSelect_Donchan_Normal.Length - 1, 1000 / 45, TJAPlayer3.Timer);
            CMenuCharacter.tMenuResetTimer(CMenuCharacter.ECharacterAnimation.NORMAL); 


            bInSongPlayed = false;
            
            this.PuchiChara.IdleAnimation();
            
            Background = new ScriptBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.DANISELECT}Script.lua"));
            Background.Init();

            base.Activate();
        }

        public override void DeActivate()
        {
            TJAPlayer3.t安全にDisposeする(ref Background);
            
            base.DeActivate();
        }

        public override void CreateManagedResource()
        {

            base.CreateManagedResource();
        }

        public override void ReleaseManagedResource()
        {

            base.ReleaseManagedResource();
        }

        public override int Draw()
        {
            // ctDonchan_Normal.t進行Loop();
            ctDonchan_In.Tick();
            ct待機.Tick();

            int stamp = this.段位リスト.ctDaniIn.CurrentValue;

            float zoom = Math.Min(1.14f, Math.Max(1f, (float)Math.Pow(stamp / 3834f, 0.5f)));

            Background.Update();
            Background.Draw();

            //TJAPlayer3.Tx.Dani_Background.vc拡大縮小倍率.X = zoom;
            //TJAPlayer3.Tx.Dani_Background.vc拡大縮小倍率.Y = zoom;
            //TJAPlayer3.Tx.Dani_Background.t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.Resolution[0] / 2, TJAPlayer3.Skin.Resolution[1] / 2);

            this.段位リスト.Draw();

            if (stamp < 6000)
            {
                #region [Dan intro anim]

                if (!bInSongPlayed)
                {
                    this.段位リスト.ctDaniIn = new CCounter(0, 6000, 1, TJAPlayer3.Timer);
                    TJAPlayer3.Skin.soundDanSongSelectIn.t再生する();
                    bInSongPlayed = true;
                }

                /*
                int dani_dan_in_width = TJAPlayer3.Tx.Dani_Dan_In.szテクスチャサイズ.Width / 2;
                int dani_dan_in_height = TJAPlayer3.Tx.Dani_Dan_In.szテクスチャサイズ.Height;

                int doorLeft = 0;
                int doorRight = dani_dan_in_width;
                if (stamp >= 3834)
                {
                    double screen_ratio = TJAPlayer3.Skin.Resolution[0] / 1280.0;

                    doorLeft -= (int)((stamp - 3834) * screen_ratio);
                    doorRight += (int)((stamp - 3834) * screen_ratio);
                }

                TJAPlayer3.Tx.Dani_Dan_In.t2D描画(doorLeft, 0, new Rectangle(0, 0, dani_dan_in_width, dani_dan_in_height));
                TJAPlayer3.Tx.Dani_Dan_In.t2D描画(doorRight, 0, new Rectangle(dani_dan_in_width, 0, dani_dan_in_width, dani_dan_in_height));
                */
                if (stamp <= 3834)
                {
                    #region [Dan intro letters]

                    //int quarter = TJAPlayer3.Tx.Dani_Dan_Text.szテクスチャサイズ.Width / 4;

                    /*
                    int[] xAxis = { 300, 980 };
                    int[] yAxis = { 198, 522 };
                    */

                    /*
                    int[] appearStamps = { 1645, 2188, 2646, 3152 };

                    for (int i = 0; i < 4; i++)
                    {
                        int x = TJAPlayer3.Skin.DaniSelect_Dan_Text_X[i];
                        int y = TJAPlayer3.Skin.DaniSelect_Dan_Text_Y[i];

                        if (stamp < appearStamps[i])
                            break;

                        TJAPlayer3.Tx.Dani_Dan_Text.Opacity = Math.Min(255, stamp - appearStamps[i]);

                        float ratio = (255 - TJAPlayer3.Tx.Dani_Dan_Text.Opacity) / 400f + 1f;

                        TJAPlayer3.Tx.Dani_Dan_Text.vc拡大縮小倍率.X = ratio;
                        TJAPlayer3.Tx.Dani_Dan_Text.vc拡大縮小倍率.Y = ratio;

                        TJAPlayer3.Tx.Dani_Dan_Text.t2D拡大率考慮中央基準描画(x, y,
                            new Rectangle(quarter * i, 0, quarter, TJAPlayer3.Tx.Dani_Dan_Text.szテクスチャサイズ.Height));
                    }
                    */

                    #endregion
                }

                #endregion
            }
            else if (stamp == 6000)
            {
                if (!ctDonchan_In.IsStarted)
                {
                    //TJAPlayer3.Skin.soundDanSelectStart.t再生する();
                    TJAPlayer3.Skin.voiceMenuDanSelectStart[TJAPlayer3.SaveFile]?.t再生する();
                    TJAPlayer3.Skin.soundDanSelectBGM.t再生する();
                    ctDonchan_In.Start(0, 180, 1.25f, TJAPlayer3.Timer);
                }

                TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.SongSelect_NamePlate_X[0], TJAPlayer3.Skin.SongSelect_NamePlate_Y[0], 0);
                ModIcons.tDisplayModsMenu(TJAPlayer3.Skin.SongSelect_ModIcons_X[0], TJAPlayer3.Skin.SongSelect_ModIcons_Y[0], 0);;

                #region [ キー関連 ]

                if (!this.段位リスト.bスクロール中 && !b選択した && !bDifficultyIn)
                {
                    int returnTitle()
                    {
                        TJAPlayer3.Skin.soundDanSelectBGM.t停止する();
                        TJAPlayer3.Skin.sound取消音.t再生する();
                        this.eフェードアウト完了時の戻り値 = E戻り値.タイトルに戻る;
                        this.actFOtoTitle.tフェードアウト開始();
                        base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
                        return 0;
                    }

                    if (TJAPlayer3.Input管理.Keyboard.KeyPressing((int)SlimDXKeys.Key.RightArrow) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RightChange))
                    {
                        this.段位リスト.t右に移動();
                    }

                    if (TJAPlayer3.Input管理.Keyboard.KeyPressing((int)SlimDXKeys.Key.LeftArrow) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LeftChange))
                    {
                        this.段位リスト.t左に移動();
                    }

                    if (TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.Decide))
                    {
                        switch(段位リスト.currentBar.eノード種別)
                        {
                            case C曲リストノード.Eノード種別.SCORE:
                            case C曲リストノード.Eノード種別.RANDOM:
                                {
                                    //this.t段位を選択する();
                                    //TJAPlayer3.Skin.soundDanSongSelectCheck.t再生する();
                                    TJAPlayer3.Skin.voiceMenuDanSelectPrompt[TJAPlayer3.SaveFile]?.t再生する();
                                    this.bDifficultyIn = true;
                                    this.段位挑戦選択画面.ctBarIn.Start(0, 255, 1, TJAPlayer3.Timer);
                                }
                                break;
                            case C曲リストノード.Eノード種別.BOX:
                                {
                                    TJAPlayer3.Skin.sound決定音.t再生する();
                                    段位リスト.tOpenFolder(段位リスト.currentBar);
                                }
                                break;
                            case C曲リストノード.Eノード種別.BACKBOX:
                                {
                                    if (TJAPlayer3.Songs管理.list曲ルート.Contains(段位リスト.currentBar.r親ノード) && 段位リスト.currentBar.r親ノード.strジャンル == "段位道場")
                                    {
                                        return returnTitle();
                                    }
                                    else
                                    {
                                        TJAPlayer3.Skin.sound決定音.t再生する();
                                        段位リスト.tCloseFolder(段位リスト.currentBar);
                                    }
                                }
                                break;
                        }
                    }

                    if(TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.Cancel))
                    {
                        return returnTitle();
                    }
                }

                #endregion

                #region [ どんちゃん関連 ]

                if(ctDonchan_In.CurrentValue != 90)
                {
                    float DonchanX = 0f, DonchanY = 0f;

                    DonchanX = (float)Math.Sin(ctDonchan_In.CurrentValue / 2 * (Math.PI / 180)) * 200f;
                    DonchanY = ((float)Math.Sin((90 + (ctDonchan_In.CurrentValue / 2)) * (Math.PI / 180)) * 150f);

                    // TJAPlayer3.Tx.SongSelect_Donchan_Normal[ctDonchan_Normal.n現在の値].Opacity = ctDonchan_In.n現在の値 * 2;
                    // TJAPlayer3.Tx.SongSelect_Donchan_Normal[ctDonchan_Normal.n現在の値].t2D描画(-200 + DonchanX, 336 - DonchanY);

                    //CMenuCharacter.tMenuDisplayCharacter(0, (int)(-200 + DonchanX), (int)(336 - DonchanY), CMenuCharacter.ECharacterAnimation.NORMAL);

                    int chara_x = TJAPlayer3.Skin.SongSelect_NamePlate_X[0] + TJAPlayer3.Tx.NamePlateBase.szテクスチャサイズ.Width / 2;
                    int chara_y = TJAPlayer3.Skin.SongSelect_NamePlate_Y[0];

                    CMenuCharacter.tMenuDisplayCharacter(
                        0,
                        chara_x,
                        chara_y, 
                        CMenuCharacter.ECharacterAnimation.NORMAL);

                    #region [PuchiChara]

                    int puchi_x = chara_x + TJAPlayer3.Skin.Adjustments_MenuPuchichara_X[0];
                    int puchi_y = chara_y + TJAPlayer3.Skin.Adjustments_MenuPuchichara_Y[0];

                    //this.PuchiChara.On進行描画(0 + 100, 336 + 230, false);
                    this.PuchiChara.On進行描画(puchi_x, puchi_y, false);

                    #endregion
                }

                #endregion

                this.段位挑戦選択画面.Draw();
            }

            if (段位挑戦選択画面.bOption) actPlayOption.On進行描画(0);

            if (ct待機.CurrentValue >= 3000)
            {
                if (段位リスト.currentBar.eノード種別 == C曲リストノード.Eノード種別.RANDOM)
                {
                    if (!tSelectSongRandomly())
                    {
                        bDifficultyIn = false;
                        b選択した = false;
                        TJAPlayer3.Skin.soundError.t再生する();
                    }
                }
                else
                {
                    TJAPlayer3.stage段位選択.t段位を選択する();
                }
                ct待機.CurrentValue = 0;
                ct待機.Stop();
            }

            switch (base.eフェーズID)
            {
                case CStage.Eフェーズ.選曲_NowLoading画面へのフェードアウト:
                    if (this.actFOtoNowLoading.Draw() == 0)
                    {
                        break;
                    }
                    return (int)this.eフェードアウト完了時の戻り値;

                case CStage.Eフェーズ.共通_フェードアウト:
                    if (this.actFOtoTitle.Draw() == 0)
                    {
                        break;
                    }
                    return (int)this.eフェードアウト完了時の戻り値;

            }

            return 0;
        }

        public enum E戻り値 : int
        {
            継続,
            タイトルに戻る,
            選曲した
        }

        public void t段位を選択する()
        {
            this.b選択した = true;
            TJAPlayer3.stage選曲.r確定された曲 = 段位リスト.listSongs[段位リスト.n現在の選択行];
            TJAPlayer3.stage選曲.r確定されたスコア = 段位リスト.listSongs[段位リスト.n現在の選択行].arスコア[(int)Difficulty.Dan];
            TJAPlayer3.stage選曲.n確定された曲の難易度[0] = (int)Difficulty.Dan;
            TJAPlayer3.stage選曲.str確定された曲のジャンル = 段位リスト.listSongs[段位リスト.n現在の選択行].strジャンル;
            if ((TJAPlayer3.stage選曲.r確定された曲 != null) && (TJAPlayer3.stage選曲.r確定されたスコア != null))
            {
                this.eフェードアウト完了時の戻り値 = E戻り値.選曲した;
                this.actFOtoNowLoading.tフェードアウト開始();                // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
                base.eフェーズID = CStage.Eフェーズ.選曲_NowLoading画面へのフェードアウト;
            }
            // TJAPlayer3.Skin.bgm選曲画面.t停止する();
            CSongSelectSongManager.stopSong();
        }

        private bool tSelectSongRandomly()
        {
            this.b選択した = true;
            var mandatoryDiffs = new List<int>();
            C曲リストノード song = 段位リスト.currentBar;

            song.stackランダム演奏番号.Clear();
            song.listランダム用ノードリスト = null;

            if ((song.stackランダム演奏番号.Count == 0) || (song.listランダム用ノードリスト == null))
            {
                if (song.listランダム用ノードリスト == null)
                {
                    List<C曲リストノード> songs = new List<C曲リストノード>();
                    TJAPlayer3.stage選曲.t指定された曲の子リストの曲を列挙する_孫リスト含む(song.r親ノード, ref songs, ref mandatoryDiffs, true);
                    song.listランダム用ノードリスト = songs;
                }
                int count = song.listランダム用ノードリスト.Count;
                if (count == 0)
                {
                    return false;
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
            TJAPlayer3.stage選曲.r確定された曲 = song.listランダム用ノードリスト[song.stackランダム演奏番号.Pop()];
            TJAPlayer3.stage選曲.n確定された曲の難易度[0] = (int)Difficulty.Dan;

            TJAPlayer3.stage選曲.r確定されたスコア = TJAPlayer3.stage選曲.r確定された曲.arスコア[TJAPlayer3.stage選曲.act曲リスト.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(TJAPlayer3.stage選曲.r確定された曲)];
            TJAPlayer3.stage選曲.str確定された曲のジャンル = TJAPlayer3.stage選曲.r確定された曲.strジャンル;

            //TJAPlayer3.Skin.sound曲決定音.t再生する();

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

            return true;
        }

        private ScriptBG Background;

        public CCounter ct待機;

        public bool b選択した;
        public bool bDifficultyIn;

        public bool bInSongPlayed;

        private CCounter ctDonchan_In;
        // private CCounter ctDonchan_Normal;

        private PuchiChara PuchiChara;

        public E戻り値 eフェードアウト完了時の戻り値;

        public CActFIFOStart actFOtoNowLoading;
        public CActFIFOBlack actFOtoTitle;
        public CActSelect段位リスト 段位リスト;
        public CActSelect段位挑戦選択画面 段位挑戦選択画面;
        public CActPlayOption actPlayOption;
    }
}
