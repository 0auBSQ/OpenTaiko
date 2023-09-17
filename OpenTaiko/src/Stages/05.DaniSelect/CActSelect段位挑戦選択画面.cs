using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;

namespace TJAPlayer3
{
    class CActSelect段位挑戦選択画面 : CActivity
    {
        public override void Activate()
        {
            ctBarIn = new CCounter();
            ctBarOut = new CCounter();
            ctBarOut.CurrentValue = 255;
            TJAPlayer3.stage段位選択.bDifficultyIn = false;
            bOption = false;

            base.Activate();
        }

        public override void DeActivate()
        {
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
            if(TJAPlayer3.stage段位選択.bDifficultyIn || ctBarOut.CurrentValue < ctBarOut.EndValue)
            {
                ctBarIn.Tick();
                ctBarOut.Tick();

                TJAPlayer3.Tx.Challenge_Select[0].Opacity = TJAPlayer3.stage段位選択.bDifficultyIn ? ctBarIn.CurrentValue : 255 - ctBarOut.CurrentValue;
                TJAPlayer3.Tx.Challenge_Select[1].Opacity = TJAPlayer3.stage段位選択.bDifficultyIn ? ctBarIn.CurrentValue : 255 - ctBarOut.CurrentValue;
                TJAPlayer3.Tx.Challenge_Select[2].Opacity = TJAPlayer3.stage段位選択.bDifficultyIn ? ctBarIn.CurrentValue : 255 - ctBarOut.CurrentValue;

                TJAPlayer3.Tx.Challenge_Select[0].t2D描画(0, 0);

                int selectIndex = (2 - n現在の選択行);
                int[] challenge_select_rect = TJAPlayer3.Skin.DaniSelect_Challenge_Select_Rect[selectIndex];

                TJAPlayer3.Tx.Challenge_Select[2].t2D描画(TJAPlayer3.Skin.DaniSelect_Challenge_Select_X[selectIndex], TJAPlayer3.Skin.DaniSelect_Challenge_Select_Y[selectIndex], 
                    new Rectangle(challenge_select_rect[0], challenge_select_rect[1], challenge_select_rect[2], challenge_select_rect[3]));

                TJAPlayer3.Tx.Challenge_Select[1].t2D描画(0, 0);


                if (TJAPlayer3.stage段位選択.ct待機.IsStarted)
                    return base.Draw();

                #region [Key bindings]

                if (ctBarIn.IsEnded && !TJAPlayer3.stage段位選択.b選択した && bOption == false)
                {
                    if (TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue))
                    {
                        if (n現在の選択行 - 1 >= 0)
                        {
                            TJAPlayer3.Skin.sound変更音.t再生する();
                            n現在の選択行--;
                        }
                    }

                    if (TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow) ||
                    TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue))
                    {
                        if (n現在の選択行 + 1 <= 2)
                        {
                            TJAPlayer3.Skin.sound変更音.t再生する();
                            n現在の選択行++;
                        }
                    }

                    if (TJAPlayer3.Input管理.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed) ||
                        TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed))
                    {
                        if (n現在の選択行 == 0)
                        {
                            this.ctBarOut.Start(0, 255, 0.5f, TJAPlayer3.Timer);
                            TJAPlayer3.Skin.sound取消音.t再生する();
                            TJAPlayer3.stage段位選択.bDifficultyIn = false;
                        }
                        else if (n現在の選択行 == 1)
                        {
                            //TJAPlayer3.Skin.soundDanSongSelect.t再生する();
                            TJAPlayer3.Skin.sound決定音.t再生する();
                            TJAPlayer3.Skin.voiceMenuDanSelectConfirm[TJAPlayer3.SaveFile]?.t再生する();
                            TJAPlayer3.stage段位選択.ct待機.Start(0, 3000, 1, TJAPlayer3.Timer);
                        }
                        else if (n現在の選択行 == 2)
                        {
                            bOption = true;
                        }
                    }
                }

                #endregion
            }

            return base.Draw();
        }

        public CCounter ctBarIn;
        public CCounter ctBarOut;

        public bool bOption;

        private int n現在の選択行;
    }
}
