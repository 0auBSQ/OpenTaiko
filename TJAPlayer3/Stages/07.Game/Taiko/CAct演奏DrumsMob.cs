using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace TJAPlayer3
{
    internal class CAct演奏DrumsMob : CActivity
    {
        /// <summary>
        /// 踊り子
        /// </summary>
        public CAct演奏DrumsMob()
        {
            base.b活性化してない = true;
        }

        public override void On活性化()
        {
            RandomMob = TJAPlayer3.Random.Next(TJAPlayer3.Skin.Game_Mob_Ptn);
            nMobBeat = TJAPlayer3.Skin.Game_Mob_Beat;

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
            if(!TJAPlayer3.stage演奏ドラム画面.bDoublePlay)
            {
                if (TJAPlayer3.Skin.Game_Mob_Ptn != 0 && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
                {

                    /*
                    TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, ctMob.n現在の値.ToString());
                    TJAPlayer3.act文字コンソール.tPrint(0, 20, C文字コンソール.Eフォント種別.白, ctMobPtn.n現在の値.ToString());
                    TJAPlayer3.act文字コンソール.tPrint(0, 30, C文字コンソール.Eフォント種別.白, ((int)ctMobPtn.n現在の値).ToString());
                    TJAPlayer3.act文字コンソール.tPrint(0, 40, C文字コンソール.Eフォント種別.白, TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[0].ToString());
                    TJAPlayer3.act文字コンソール.tPrint(0, 10, C文字コンソール.Eフォント種別.白, Math.Sin((float)this.ctMob.n現在の値 * (Math.PI / 180)).ToString());
                    */

                    if (HGaugeMethods.UNSAFE_IsRainbow(0))
                    {

                        if (!TJAPlayer3.stage演奏ドラム画面.bPAUSE) nNowMobCounter += (Math.Abs((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[0] / 60.0f) * (float)TJAPlayer3.FPS.DeltaTime) * 180 / nMobBeat;
                        bool endAnime = nNowMobCounter >= 180;

                        if (endAnime)
                        {
                            nNowMobCounter = 0;
                        }


                        if (TJAPlayer3.Tx.Mob[RandomMob] != null)
                            TJAPlayer3.Tx.Mob[RandomMob].t2D描画(TJAPlayer3.app.Device, 0, (TJAPlayer3.Skin.Resolution[1] - (TJAPlayer3.Tx.Mob[RandomMob].szテクスチャサイズ.Height - 70)) + -((float)Math.Sin(nNowMobCounter * (Math.PI / 180)) * 70));
                        
                    }

                }
            }
            return base.On進行描画();
        }
        #region[ private ]
        //-----------------
        private float nNowMobCounter;
        private float nMobBeat;
        private int RandomMob;
        //-----------------
        #endregion
    }
}
