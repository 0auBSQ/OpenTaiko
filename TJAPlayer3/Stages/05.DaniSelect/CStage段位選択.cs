using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using SlimDX.DirectInput;

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
            ctDonchan_Normal = new CCounter(0, TJAPlayer3.Tx.SongSelect_Donchan_Normal.Length - 1, 1000 / 45, TJAPlayer3.Timer);

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
            ctDonchan_Normal.t進行Loop();
            ctDonchan_In.t進行();
            ct待機.t進行();

            TJAPlayer3.Tx.Dani_Background.t2D描画(TJAPlayer3.app.Device, 0, 0);

            this.段位リスト.On進行描画();

            if(this.段位リスト.ctDaniIn.n現在の値 == 3000)
            {
                if (!ctDonchan_In.b開始した)
                {
                    TJAPlayer3.Skin.soundDanSelectStart.t再生する();
                    TJAPlayer3.Skin.soundDanSelectBGM.t再生する();
                    ctDonchan_In.t開始(0, 180, 1.25f, TJAPlayer3.Timer);
                }

                TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.SongSelect_NamePlate_X[0], TJAPlayer3.Skin.SongSelect_NamePlate_Y[0] + 5, 0);

                #region [ キー関連 ]

                if (!this.段位リスト.bスクロール中 && !b選択した && !bDifficultyIn)
                {
                    if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)Key.RightArrow) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue))
                    {
                        this.段位リスト.t右に移動();
                    }

                    if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)Key.LeftArrow) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue))
                    {
                        this.段位リスト.t左に移動();
                    }

                    if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)Key.Return) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed))
                    {
                        //this.t段位を選択する();
                        TJAPlayer3.Skin.soundDanSongSelectCheck.t再生する();
                        this.bDifficultyIn = true;
                        this.段位挑戦選択画面.ctBarIn.t開始(0, 255, 1, TJAPlayer3.Timer);
                    }

                    if(TJAPlayer3.Input管理.Keyboard.bキーが押された((int)Key.Escape))
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

                    TJAPlayer3.Tx.SongSelect_Donchan_Normal[ctDonchan_Normal.n現在の値].Opacity = ctDonchan_In.n現在の値 * 2;
                    TJAPlayer3.Tx.SongSelect_Donchan_Normal[ctDonchan_Normal.n現在の値].t2D描画(TJAPlayer3.app.Device, -200 + DonchanX, 336 - DonchanY);

                    #region [PuchiChara]

                    this.PuchiChara.On進行描画(0 + 100, 336 + 230, false);

                    #endregion
                }

                #endregion

                this.段位挑戦選択画面.On進行描画();
            }

            if(ct待機.n現在の値 >= 3000)
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

        private CCounter ctDonchan_In;
        private CCounter ctDonchan_Normal;

        private PuchiChara PuchiChara;

        public E戻り値 eフェードアウト完了時の戻り値;

        public CActFIFOStart actFOtoNowLoading;
        public CActFIFOBlack actFOtoTitle;
        public CActSelect段位リスト 段位リスト;
        public CActSelect段位挑戦選択画面 段位挑戦選択画面;
    }
}
