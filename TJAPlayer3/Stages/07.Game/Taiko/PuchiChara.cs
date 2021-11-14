using TJAPlayer3;
using FDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TJAPlayer3
{
    class PuchiChara : CActivity
    {
        public PuchiChara()
        {
            base.b活性化してない = true;
        }

        public override void On活性化()
        {
            Counter = new CCounter(0, TJAPlayer3.Skin.Game_PuchiChara[2] - 1, TJAPlayer3.Skin.Game_PuchiChara_Timer * 0.5f, TJAPlayer3.Timer);
            SineCounter = new CCounter(0, 360, TJAPlayer3.Skin.Game_PuchiChara_SineTimer, CSound管理.rc演奏用タイマ);
            SineCounterIdle = new CCounter(1, 360, (float)TJAPlayer3.Skin.Game_PuchiChara_SineTimer * 2f, TJAPlayer3.Timer);
            this.inGame = false;
            base.On活性化();
        }
        public override void On非活性化()
        {
            Counter = null;
            SineCounter = null;
            SineCounterIdle = null;
            base.On非活性化();
        }
        
        public void ChangeBPM(double bpm)
        {
            Counter = new CCounter(0, TJAPlayer3.Skin.Game_PuchiChara[2] - 1, (int)(TJAPlayer3.Skin.Game_PuchiChara_Timer * bpm / TJAPlayer3.Skin.Game_PuchiChara[2]), TJAPlayer3.Timer);
            SineCounter = new CCounter(1, 360, TJAPlayer3.Skin.Game_PuchiChara_SineTimer * bpm / 180, CSound管理.rc演奏用タイマ);
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
        public int On進行描画(int x, int y, bool isGrowing, int alpha = 255, bool isBalloon = false, int player = 0)
        {
            if (!TJAPlayer3.ConfigIni.ShowPuchiChara) return base.On進行描画();
            if (Counter == null || SineCounter == null || TJAPlayer3.Tx.PuchiChara == null) return base.On進行描画();
            Counter.t進行Loop();
            SineCounter.t進行LoopDb();
            SineCounterIdle.t進行Loop();
            
            /*
            TJAPlayer3.act文字コンソール.tPrint(700, 500, C文字コンソール.Eフォント種別.白, Counter.n現在の値.ToString());
            TJAPlayer3.act文字コンソール.tPrint(700, 520, C文字コンソール.Eフォント種別.白, SineCounter.n現在の値.ToString());
            TJAPlayer3.act文字コンソール.tPrint(700, 540, C文字コンソール.Eフォント種別.白, SineCounterIdle.n現在の値.ToString());
            */

            if (inGame)
                sineY = (double)SineCounter.n現在の値;
            else
                sineY = (double)SineCounterIdle.n現在の値;

            // TJAPlayer3.act文字コンソール.tPrint(700, 560, C文字コンソール.Eフォント種別.白, sineY.ToString());

            sineY = Math.Sin(sineY * (Math.PI / 180)) * (TJAPlayer3.Skin.Game_PuchiChara_Sine * (isBalloon ? TJAPlayer3.Skin.Game_PuchiChara_Scale[1] : TJAPlayer3.Skin.Game_PuchiChara_Scale[0]));

            // TJAPlayer3.act文字コンソール.tPrint(700, 580, C文字コンソール.Eフォント種別.白, sineY.ToString());

            TJAPlayer3.Tx.PuchiChara.vc拡大縮小倍率 = new SlimDX.Vector3((isBalloon ? TJAPlayer3.Skin.Game_PuchiChara_Scale[1] : TJAPlayer3.Skin.Game_PuchiChara_Scale[0]));
            TJAPlayer3.Tx.PuchiChara.Opacity = alpha;

            // (isGrowing ? TJAPlayer3.Skin.Game_PuchiChara[1] : 0) => Height

            /* To do :
            **
            ** - Yellow light color filter when isGrowing is true
            */

            int puriChar = TJAPlayer3.NamePlateConfig.data.PuchiChara[player];
            
            // To change later
            if (puriChar < 0)
                puriChar = 0;
            else if (puriChar >= 120)
                puriChar = 119;

            int puriColumn = puriChar % 5;
            int puriRow = puriChar / 5;

            int adjustedX = x - 32;
            int adjustedY = y - 32;

            TJAPlayer3.Tx.PuchiChara.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, adjustedX, adjustedY + (int)sineY, new Rectangle((Counter.n現在の値 + 2 * puriColumn) * TJAPlayer3.Skin.Game_PuchiChara[0], puriRow * TJAPlayer3.Skin.Game_PuchiChara[1], TJAPlayer3.Skin.Game_PuchiChara[0], TJAPlayer3.Skin.Game_PuchiChara[1]));

            // TJAPlayer3.Tx.PuchiChara.t2D中心基準描画(TJAPlayer3.app.Device, x, y + (int)sineY, new Rectangle((Counter.n現在の値 + 2 * puriColumn) * TJAPlayer3.Skin.Game_PuchiChara[0], puriRow * TJAPlayer3.Skin.Game_PuchiChara[1], TJAPlayer3.Skin.Game_PuchiChara[0], TJAPlayer3.Skin.Game_PuchiChara[1]));
            
            return base.On進行描画();
        }

        public double sineY;

        public CCounter Counter;
        private CCounter SineCounter;
        private CCounter SineCounterIdle;
        private bool inGame;
    }
}