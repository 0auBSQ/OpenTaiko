using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace TJAPlayer3
{
    internal class CAct演奏DrumsDancer : CActivity
    {
        /// <summary>
        /// 踊り子
        /// </summary>
        public CAct演奏DrumsDancer()
        {
            base.b活性化してない = true;
        }

        public override void On活性化()
        {
            this.ct踊り子モーション = new CCounter();
            base.On活性化();
        }

        public override void On非活性化()
        {
            this.ct踊り子モーション = null;
            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            this.ar踊り子モーション番号 = C変換.ar配列形式のstringをint配列に変換して返す(TJAPlayer3.Skin.Game_Dancer_Motion);
            if(this.ar踊り子モーション番号 == null) ar踊り子モーション番号 = C変換.ar配列形式のstringをint配列に変換して返す("0,0");
            this.ct踊り子モーション = new CCounter(0, this.ar踊り子モーション番号.Length - 1, 0.01, CSound管理.rc演奏用タイマ);
            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            if( this.b初めての進行描画 )
            {
                this.b初めての進行描画 = true;
            }

            if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
            {
                if (this.ct踊り子モーション != null || TJAPlayer3.Skin.Game_Dancer_Ptn != 0) this.ct踊り子モーション.t進行LoopDb();

                if (TJAPlayer3.ConfigIni.ShowDancer && this.ct踊り子モーション != null && TJAPlayer3.Skin.Game_Dancer_Ptn != 0)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        if (TJAPlayer3.Tx.Dancer[i][this.ar踊り子モーション番号[(int)this.ct踊り子モーション.n現在の値]] != null)
                        {
                            if ((int)TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[0] >= TJAPlayer3.Skin.Game_Dancer_Gauge[i])
                                TJAPlayer3.Tx.Dancer[i][this.ar踊り子モーション番号[(int)this.ct踊り子モーション.n現在の値]].t2D中心基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Dancer_X[i], TJAPlayer3.Skin.Game_Dancer_Y[i]);
                        }
                    }
                }
            }

            return base.On進行描画();
        }

        #region[ private ]
        //-----------------
        public int[] ar踊り子モーション番号;
        public CCounter ct踊り子モーション;
        //-----------------
        #endregion
    }
}
