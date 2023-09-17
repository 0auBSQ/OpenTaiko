using TJAPlayer3;
using FDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Silk.NET.Maths;

using Rectangle = System.Drawing.Rectangle;

namespace TJAPlayer3
{
    class PuchiChara : CActivity
    {
        public PuchiChara()
        {
            base.IsDeActivated = true;
        }

        public override void Activate()
        {
            Counter = new CCounter(0, TJAPlayer3.Skin.Game_PuchiChara[2] - 1, TJAPlayer3.Skin.Game_PuchiChara_Timer * 0.5f, TJAPlayer3.Timer);
            SineCounter = new CCounter(0, 360, TJAPlayer3.Skin.Game_PuchiChara_SineTimer, SoundManager.PlayTimer);
            SineCounterIdle = new CCounter(1, 360, (float)TJAPlayer3.Skin.Game_PuchiChara_SineTimer * 2f, TJAPlayer3.Timer);
            this.inGame = false;
            base.Activate();
        }
        public override void DeActivate()
        {
            Counter = null;
            SineCounter = null;
            SineCounterIdle = null;
            base.DeActivate();
        }

        public static int tGetPuchiCharaIndexByName(int p)
        {
            var _pc = TJAPlayer3.SaveFileInstances[p].data.PuchiChara;
            var _pcs = TJAPlayer3.Skin.Puchicharas_Name;
            int puriChar = 0;
            if (_pcs.Contains(_pc))
                puriChar = _pcs.ToList().IndexOf(_pc);

            return puriChar;
        }
        
        public void ChangeBPM(double bpm)
        {
            Counter = new CCounter(0, TJAPlayer3.Skin.Game_PuchiChara[2] - 1, (int)(TJAPlayer3.Skin.Game_PuchiChara_Timer * bpm / TJAPlayer3.Skin.Game_PuchiChara[2]), TJAPlayer3.Timer);
            SineCounter = new CCounter(1, 360, TJAPlayer3.Skin.Game_PuchiChara_SineTimer * bpm / 180, SoundManager.PlayTimer);
            this.inGame = true;
        }

        public void IdleAnimation()
        {
            this.inGame = false;
        }

        /// <summary>
        /// ぷちキャラを描画する。(オーバーライドじゃないよ)
        /// </summary>
        /// <param name="x">X座標(中央)</param>
        /// <param name="y">Y座標(中央)</param>
        /// <param name="alpha">不透明度</param>
        /// <returns></returns>
        public int On進行描画(int x, int y, bool isGrowing, int alpha = 255, bool isBalloon = false, int player = 0, float scale = 1.0f)
        {
            if (!TJAPlayer3.ConfigIni.ShowPuchiChara) return base.Draw();
            if (Counter == null || SineCounter == null || TJAPlayer3.Tx.Puchichara == null) return base.Draw();
            Counter.TickLoop();
            SineCounter.TickLoopDB();
            SineCounterIdle.TickLoop();

            int p = TJAPlayer3.GetActualPlayer(player);

            /*
            TJAPlayer3.act文字コンソール.tPrint(700, 500, C文字コンソール.Eフォント種別.白, Counter.n現在の値.ToString());
            TJAPlayer3.act文字コンソール.tPrint(700, 520, C文字コンソール.Eフォント種別.白, SineCounter.n現在の値.ToString());
            TJAPlayer3.act文字コンソール.tPrint(700, 540, C文字コンソール.Eフォント種別.白, SineCounterIdle.n現在の値.ToString());
            */

            if (inGame)
                sineY = (double)SineCounter.CurrentValue;
            else
                sineY = (double)SineCounterIdle.CurrentValue;

            // TJAPlayer3.act文字コンソール.tPrint(700, 560, C文字コンソール.Eフォント種別.白, sineY.ToString());

            sineY = Math.Sin(sineY * (Math.PI / 180)) * (TJAPlayer3.Skin.Game_PuchiChara_Sine * (isBalloon ? TJAPlayer3.Skin.Game_PuchiChara_Scale[1] : TJAPlayer3.Skin.Game_PuchiChara_Scale[0]));

            // TJAPlayer3.act文字コンソール.tPrint(700, 580, C文字コンソール.Eフォント種別.白, sineY.ToString());

            //int puriChar = Math.Max(0, Math.Min(TJAPlayer3.Skin.Puchichara_Ptn - 1, TJAPlayer3.NamePlateConfig.data.PuchiChara[p]));

            int puriChar = PuchiChara.tGetPuchiCharaIndexByName(p);

            var chara = TJAPlayer3.Tx.Puchichara[puriChar].tx;
            //TJAPlayer3.Tx.PuchiChara[puriChar];

            if (chara != null)
            {
                float puchiScale = TJAPlayer3.Skin.Resolution[1] / 720.0f;

                chara.vc拡大縮小倍率 = new Vector3D<float>((isBalloon ? TJAPlayer3.Skin.Game_PuchiChara_Scale[1] * puchiScale : TJAPlayer3.Skin.Game_PuchiChara_Scale[0] * puchiScale));
                chara.vc拡大縮小倍率.X *= scale;
                chara.vc拡大縮小倍率.Y *= scale;
                chara.Opacity = alpha;

                // (isGrowing ? TJAPlayer3.Skin.Game_PuchiChara[1] : 0) => Height

                /* To do :
                **
                ** - Yellow light color filter when isGrowing is true
                */

                int adjustedX = x - 32;
                int adjustedY = y - 32;

                chara.t2D拡大率考慮中央基準描画(adjustedX, adjustedY + (int)sineY, new Rectangle((Counter.CurrentValue + 2) * TJAPlayer3.Skin.Game_PuchiChara[0], 0, TJAPlayer3.Skin.Game_PuchiChara[0], TJAPlayer3.Skin.Game_PuchiChara[1]));
            }

            return base.Draw();
        }

        public double sineY;

        public CCounter Counter;
        private CCounter SineCounter;
        private CCounter SineCounterIdle;
        private bool inGame;
    }
}