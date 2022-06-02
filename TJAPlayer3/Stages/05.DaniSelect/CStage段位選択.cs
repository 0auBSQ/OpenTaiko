﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using FDK;
using static TJAPlayer3.CActSelect曲リスト;

namespace TJAPlayer3
{
    class CStage段位選択 : CStage
    {
        public CStage段位選択()
        {
            base.eステージID = Eステージ.段位選択;
            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;

            base.list子Activities.Add(this.段位リスト = new CActSelect段位リスト());

            base.list子Activities.Add(this.actFOtoNowLoading = new CActFIFOStart());
            base.list子Activities.Add(this.段位挑戦選択画面 = new CActSelect段位挑戦選択画面());
            base.list子Activities.Add(this.actFOtoTitle = new CActFIFOBlack());
            base.list子Activities.Add(this.actPlayOption = new CActPlayOption());
            base.list子Activities.Add(this.PuchiChara = new PuchiChara());
        }

        public override void On活性化()
        {
            if (base.b活性化してる)
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

            base.On活性化();
        }

        public override void On非活性化()
        {
            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            // ctDonchan_Normal.t進行Loop();
            ctDonchan_In.t進行();
            ct待機.t進行();

            int stamp = this.段位リスト.ctDaniIn.n現在の値;

            float zoom = Math.Min(1.14f, Math.Max(1f, (float)Math.Pow(stamp / 3834f, 0.5f)));

            TJAPlayer3.Tx.Dani_Background.vc拡大縮小倍率.X = zoom;
            TJAPlayer3.Tx.Dani_Background.vc拡大縮小倍率.Y = zoom;
            TJAPlayer3.Tx.Dani_Background.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360);

            this.段位リスト.On進行描画();

            if (stamp < 6000)
            {
                #region [Dan intro anim]

                if (!bInSongPlayed)
                {
                    this.段位リスト.ctDaniIn = new CCounter(0, 6000, 1, TJAPlayer3.Timer);
                    TJAPlayer3.Skin.soundDanSongSelectIn.t再生する();
                    bInSongPlayed = true;
                }

                int doorLeft = 0;
                int doorRight = 640;
                if (stamp >= 3834)
                {
                    doorLeft -= stamp - 3834;
                    doorRight += stamp - 3834;
                }
                    
                TJAPlayer3.Tx.Dani_Dan_In.t2D描画(TJAPlayer3.app.Device, doorLeft, 0, new Rectangle(0, 0, 640, 720));
                TJAPlayer3.Tx.Dani_Dan_In.t2D描画(TJAPlayer3.app.Device, doorRight, 0, new Rectangle(640, 0, 640, 720));

                if (stamp <= 3834)
                {
                    #region [Dan intro letters]

                    int quarter = TJAPlayer3.Tx.Dani_Dan_Text.szテクスチャサイズ.Width / 4;

                    int[] xAxis = { 300, 980 };
                    int[] yAxis = { 198, 522 };
                    int[] appearStamps = { 1645, 2188, 2646, 3152 };

                    for (int i = 0; i < 4; i++)
                    {
                        if (stamp < appearStamps[i])
                            break;

                        TJAPlayer3.Tx.Dani_Dan_Text.Opacity = Math.Min(255, stamp - appearStamps[i]);

                        float ratio = (255 - TJAPlayer3.Tx.Dani_Dan_Text.Opacity) / 400f + 1f;

                        TJAPlayer3.Tx.Dani_Dan_Text.vc拡大縮小倍率.X = ratio;
                        TJAPlayer3.Tx.Dani_Dan_Text.vc拡大縮小倍率.Y = ratio;

                        TJAPlayer3.Tx.Dani_Dan_Text.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, xAxis[i % 2], yAxis[i / 2],
                            new Rectangle(quarter * i, 0, quarter, TJAPlayer3.Tx.Dani_Dan_Text.szテクスチャサイズ.Height));
                    }

                    #endregion
                }

                #endregion
            }
            else if (stamp == 6000)
            {
                if (!ctDonchan_In.b開始した)
                {
                    TJAPlayer3.Skin.soundDanSelectStart.t再生する();
                    TJAPlayer3.Skin.soundDanSelectBGM.t再生する();
                    ctDonchan_In.t開始(0, 180, 1.25f, TJAPlayer3.Timer);
                }

                TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.SongSelect_NamePlate_X[0], TJAPlayer3.Skin.SongSelect_NamePlate_Y[0] + 5, 0);
                ModIcons.tDisplayModsMenu(40, 672, 0);

                #region [ キー関連 ]

                if (!this.段位リスト.bスクロール中 && !b選択した && !bDifficultyIn)
                {
                    if (TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.RightArrow) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RightChange))
                    {
                        this.段位リスト.t右に移動();
                    }

                    if (TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.LeftArrow) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LeftChange))
                    {
                        this.段位リスト.t左に移動();
                    }

                    if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Return) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.Decide))
                    {
                        //this.t段位を選択する();
                        TJAPlayer3.Skin.soundDanSongSelectCheck.t再生する();
                        this.bDifficultyIn = true;
                        this.段位挑戦選択画面.ctBarIn.t開始(0, 255, 1, TJAPlayer3.Timer);
                    }

                    if(TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Escape) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.Cancel))
                    {
                        TJAPlayer3.Skin.soundDanSelectBGM.t停止する();
                        TJAPlayer3.Skin.sound取消音.t再生する();
                        this.eフェードアウト完了時の戻り値 = E戻り値.タイトルに戻る;
                        this.actFOtoTitle.tフェードアウト開始();
                        base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
                        return 0;
                    }
                }

                #endregion

                #region [ どんちゃん関連 ]

                if(ctDonchan_In.n現在の値 != 90)
                {
                    float DonchanX = 0f, DonchanY = 0f;

                    DonchanX = (float)Math.Sin(ctDonchan_In.n現在の値 / 2 * (Math.PI / 180)) * 200f;
                    DonchanY = ((float)Math.Sin((90 + (ctDonchan_In.n現在の値 / 2)) * (Math.PI / 180)) * 150f);

                    // TJAPlayer3.Tx.SongSelect_Donchan_Normal[ctDonchan_Normal.n現在の値].Opacity = ctDonchan_In.n現在の値 * 2;
                    // TJAPlayer3.Tx.SongSelect_Donchan_Normal[ctDonchan_Normal.n現在の値].t2D描画(TJAPlayer3.app.Device, -200 + DonchanX, 336 - DonchanY);

                    CMenuCharacter.tMenuDisplayCharacter(0, (int)(-200 + DonchanX), (int)(336 - DonchanY), CMenuCharacter.ECharacterAnimation.NORMAL);

                    #region [PuchiChara]

                    this.PuchiChara.On進行描画(0 + 100, 336 + 230, false);

                    #endregion
                }

                #endregion

                this.段位挑戦選択画面.On進行描画();
            }

            if (段位挑戦選択画面.bOption) actPlayOption.On進行描画(0);

            if (ct待機.n現在の値 >= 3000)
            {
                TJAPlayer3.stage段位選択.t段位を選択する();
                ct待機.n現在の値 = 0;
                ct待機.t停止();
            }

            switch (base.eフェーズID)
            {
                case CStage.Eフェーズ.選曲_NowLoading画面へのフェードアウト:
                    if (this.actFOtoNowLoading.On進行描画() == 0)
                    {
                        break;
                    }
                    return (int)this.eフェードアウト完了時の戻り値;

                case CStage.Eフェーズ.共通_フェードアウト:
                    if (this.actFOtoTitle.On進行描画() == 0)
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
            TJAPlayer3.stage選曲.r確定された曲 = TJAPlayer3.Songs管理.list曲ルート_Dan[段位リスト.n現在の選択行];
            TJAPlayer3.stage選曲.r確定されたスコア = TJAPlayer3.Songs管理.list曲ルート_Dan[段位リスト.n現在の選択行].arスコア[(int)Difficulty.Dan];
            TJAPlayer3.stage選曲.n確定された曲の難易度[0] = (int)Difficulty.Dan;
            TJAPlayer3.stage選曲.str確定された曲のジャンル = TJAPlayer3.Songs管理.list曲ルート_Dan[段位リスト.n現在の選択行].strジャンル;
            if ((TJAPlayer3.stage選曲.r確定された曲 != null) && (TJAPlayer3.stage選曲.r確定されたスコア != null))
            {
                this.eフェードアウト完了時の戻り値 = E戻り値.選曲した;
                this.actFOtoNowLoading.tフェードアウト開始();                // #27787 2012.3.10 yyagi 曲決定時の画面フェードアウトの省略
                base.eフェーズID = CStage.Eフェーズ.選曲_NowLoading画面へのフェードアウト;
            }
            // TJAPlayer3.Skin.bgm選曲画面.t停止する();
            CSongSelectSongManager.stopSong();
        }

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
