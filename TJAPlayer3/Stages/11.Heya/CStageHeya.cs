using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using FDK;
using SlimDX.DirectInput;
using static TJAPlayer3.CActSelect曲リスト;

namespace TJAPlayer3
{
    class CStageHeya : CStage
    {
        public CStageHeya()
        {
            base.eステージID = Eステージ.Heya;
            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;

            base.list子Activities.Add(this.actFOtoTitle = new CActFIFOBlack());

            base.list子Activities.Add(this.PuchiChara = new PuchiChara());
        }

        public override void On活性化()
        {
            if (base.b活性化してる)
                return;

            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
            this.eフェードアウト完了時の戻り値 = E戻り値.継続;

            ctDonchan_In = new CCounter();
            ctDonchan_Normal = new CCounter(0, TJAPlayer3.Tx.SongSelect_Donchan_Normal.Length - 1, 1000 / 45, TJAPlayer3.Timer);


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
            ctDonchan_Normal.t進行Loop();
            ctDonchan_In.t進行();

            TJAPlayer3.Tx.Heya_Background.t2D描画(TJAPlayer3.app.Device, 0, 0);

            if (!ctDonchan_In.b開始した)
            {
                TJAPlayer3.Skin.soundHeyaBGM.t再生する();
                ctDonchan_In.t開始(0, 180, 1.25f, TJAPlayer3.Timer);    
            }

            TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.SongSelect_NamePlate_X[0], TJAPlayer3.Skin.SongSelect_NamePlate_Y[0] + 5, 0);

            #region [ どんちゃん関連 ]

                if (ctDonchan_In.n現在の値 != 90)
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

            #region [ キー関連 ]

            if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)Key.RightArrow) ||
                TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue))
            {
                //this.段位リスト.t右に移動();
            }

            if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)Key.LeftArrow) ||
                TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue))
            {
                //this.段位リスト.t左に移動();
            }

            if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)Key.Return) ||
                TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed) ||
                TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed))
            {
                //this.t段位を選択する();
                TJAPlayer3.Skin.sound決定音.t再生する();
                //this.段位挑戦選択画面.ctBarIn.t開始(0, 255, 1, TJAPlayer3.Timer);
            }

            if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)Key.Escape))
            {
                TJAPlayer3.Skin.soundHeyaBGM.t停止する();
                TJAPlayer3.Skin.sound取消音.t再生する();
                this.eフェードアウト完了時の戻り値 = E戻り値.タイトルに戻る;
                this.actFOtoTitle.tフェードアウト開始();
                base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
                return 0;
            }

            #endregion

            switch (base.eフェーズID)
            {
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

        public bool bInSongPlayed;

        private CCounter ctDonchan_In;
        private CCounter ctDonchan_Normal;

        private PuchiChara PuchiChara;

        public E戻り値 eフェードアウト完了時の戻り値;

        public CActFIFOBlack actFOtoTitle;
    }
}
