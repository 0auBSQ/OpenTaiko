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
            ctMob = new CCounter();
            //ctMob = new CCounter(1, 180, 60.0 / TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM * TJAPlayer3.Skin.Game_Mob_Beat / 180 / (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0), CSound管理.rc演奏用タイマ);
            ctMobPtn = new CCounter();
            RandomMob = TJAPlayer3.Random.Next(TJAPlayer3.Skin.Game_Mob_Ptn);
            base.On活性化();
        }

        public override void On非活性化()
        {
            ctMob = null;
            ctMobPtn = null;
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
                if (ctMob != null) ctMob.t進行LoopDb();
                if (ctMobPtn != null || TJAPlayer3.Skin.Game_Mob_Ptn != 0) ctMobPtn.t進行LoopDb();

                if (TJAPlayer3.Skin.Game_Mob_Ptn != 0 && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
                {

                    /*
                    TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, ctMob.n現在の値.ToString());
                    TJAPlayer3.act文字コンソール.tPrint(0, 20, C文字コンソール.Eフォント種別.白, ctMobPtn.n現在の値.ToString());
                    TJAPlayer3.act文字コンソール.tPrint(0, 30, C文字コンソール.Eフォント種別.白, ((int)ctMobPtn.n現在の値).ToString());
                    TJAPlayer3.act文字コンソール.tPrint(0, 40, C文字コンソール.Eフォント種別.白, TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[0].ToString());
                    TJAPlayer3.act文字コンソール.tPrint(0, 10, C文字コンソール.Eフォント種別.白, Math.Sin((float)this.ctMob.n現在の値 * (Math.PI / 180)).ToString());
                    */

                    if (TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[0] >= 100)
                    {
                        

                        
                        if (TJAPlayer3.Tx.Mob[RandomMob] != null)
                            TJAPlayer3.Tx.Mob[RandomMob].t2D描画(TJAPlayer3.app.Device, 0, (720 - (TJAPlayer3.Tx.Mob[RandomMob].szテクスチャサイズ.Height - 70)) + -((float)Math.Sin((float)this.ctMob.n現在の値 * (Math.PI / 180)) * 70));
                        
                    }

                }
            }
            return base.On進行描画();
        }
        #region[ private ]
        //-----------------
        public CCounter ctMob;
        public CCounter ctMobPtn;
        private int RandomMob;
        //-----------------
        #endregion
    }
}
