using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using SlimDX.DirectInput;

namespace TJAPlayer3
{
    class CActSelect段位挑戦選択画面 : CActivity
    {
        public override void On活性化()
        {
            ctBarIn = new CCounter();
            ctBarOut = new CCounter();
            ctBarOut.n現在の値 = 255;
            TJAPlayer3.stage段位選択.bDifficultyIn = false;

            base.On活性化();
        }

        public override void On非活性化()
        {
            base.On非活性化();
        }

        public override int On進行描画()
        {
            if(TJAPlayer3.stage段位選択.bDifficultyIn || ctBarOut.n現在の値 < ctBarOut.n終了値)
            {
                ctBarIn.t進行();
                ctBarOut.t進行();

                TJAPlayer3.Tx.Challenge_Select[0].Opacity = TJAPlayer3.stage段位選択.bDifficultyIn ? ctBarIn.n現在の値 : 255 - ctBarOut.n現在の値;
                TJAPlayer3.Tx.Challenge_Select[1].Opacity = TJAPlayer3.stage段位選択.bDifficultyIn ? ctBarIn.n現在の値 : 255 - ctBarOut.n現在の値;
                TJAPlayer3.Tx.Challenge_Select[2].Opacity = TJAPlayer3.stage段位選択.bDifficultyIn ? ctBarIn.n現在の値 : 255 - ctBarOut.n現在の値;

                TJAPlayer3.Tx.Challenge_Select[0].t2D描画(TJAPlayer3.app.Device, 0, 0);

                TJAPlayer3.Tx.Challenge_Select[2].t2D描画(TJAPlayer3.app.Device, 228 + 228 * (2 - n現在の選択行), 0, new Rectangle(228 + 228 * (2 - n現在の選択行), 0, 228, 720));

                TJAPlayer3.Tx.Challenge_Select[1].t2D描画(TJAPlayer3.app.Device, 0, 0);

                if (ctBarIn.b終了値に達した && !TJAPlayer3.stage段位選択.b選択した)
                {
                    if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)Key.RightArrow) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue))
                    {
                        if (n現在の選択行 - 1 >= 0)
                        {
                            TJAPlayer3.Skin.sound変更音.t再生する();
                            n現在の選択行--;
                        }
                    }

                    if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)Key.LeftArrow) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue))
                    {
                        if (n現在の選択行 + 1 <= 2)
                        {
                            TJAPlayer3.Skin.sound変更音.t再生する();
                            n現在の選択行++;
                        }
                    }

                    if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)Key.Return) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed))
                    {
                        if (n現在の選択行 == 0)
                        {
                            this.ctBarOut.t開始(0, 255, 0.5f, TJAPlayer3.Timer);
                            TJAPlayer3.Skin.sound取消音.t再生する();
                            TJAPlayer3.stage段位選択.bDifficultyIn = false;
                        }
                        else if (n現在の選択行 == 1)
                        {
                            TJAPlayer3.Skin.soundDanSongSelect.t再生する();
                            TJAPlayer3.stage段位選択.ct待機.t開始(0, 3000, 1, TJAPlayer3.Timer);
                        }
                    }
                }
            }

            return base.On進行描画();
        }

        public CCounter ctBarIn;
        public CCounter ctBarOut;

        private int n現在の選択行;
    }
}
