using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;

namespace TJAPlayer3
{
    internal class CActCalibrationMode : CActivity
    {
        public CActCalibrationMode() { }

        public override void Activate()
        {
            base.Activate();
        }

        public override void DeActivate()
        {
            Stop();
            Offsets.Clear();

            base.DeActivate();
        }

        public void Start()
        {
            CalibrateTick = new CCounter(0, 500, 1, TJAPlayer3.Timer);
        }

        public void Stop()
        {
            CalibrateTick = new CCounter();
            Offsets.Clear();
        }

        public int Update()
        {
            if (IsDeActivated || CalibrateTick.IsStoped)
                return 1;

            CalibrateTick.Tick();

            bool decide = TJAPlayer3.ConfigIni.KeyAssign.KeyIsPressed(TJAPlayer3.ConfigIni.KeyAssign.Taiko.LeftRed) ||
                TJAPlayer3.ConfigIni.KeyAssign.KeyIsPressed(TJAPlayer3.ConfigIni.KeyAssign.Taiko.RightRed) ||
                TJAPlayer3.ConfigIni.KeyAssign.KeyIsPressed(TJAPlayer3.ConfigIni.KeyAssign.System.Decide) ||
                TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Return);

            if (CalibrateTick.IsEnded)
            {
                TJAPlayer3.Skin.calibrationTick.tPlay();
                CalibrateTick.Start(0, 500, 1, TJAPlayer3.Timer);
            }

            if (TJAPlayer3.ConfigIni.KeyAssign.KeyIsPressed(TJAPlayer3.ConfigIni.KeyAssign.Taiko.LeftBlue) ||
                TJAPlayer3.ConfigIni.KeyAssign.KeyIsPressed(TJAPlayer3.ConfigIni.KeyAssign.Taiko.LeftChange) ||
                TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.LeftArrow))
            {
                buttonIndex = Math.Max(buttonIndex - 1, 0);
                TJAPlayer3.Skin.soundChangeSFX.tPlay();
            }
            else if (TJAPlayer3.ConfigIni.KeyAssign.KeyIsPressed(TJAPlayer3.ConfigIni.KeyAssign.Taiko.RightBlue) ||
                TJAPlayer3.ConfigIni.KeyAssign.KeyIsPressed(TJAPlayer3.ConfigIni.KeyAssign.Taiko.RightChange) ||
                TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.RightArrow))
            {
                buttonIndex = Math.Min(buttonIndex + 1, 2);
                TJAPlayer3.Skin.soundChangeSFX.tPlay();
            }
            else if (buttonIndex == 0 && decide)
            {
                Offsets.Clear();
                TJAPlayer3.Skin.soundDecideSFX.tPlay();
            }
            else if (buttonIndex == 1 && decide)
            {
                AddOffset();
            }
            else if (buttonIndex == 2 && decide)
            {
                TJAPlayer3.ConfigIni.nGlobalOffsetMs = GetAverageOffset();
                TJAPlayer3.Skin.soundDecideSFX.tPlay();
                Stop();

                return 0;
            }
            else if (TJAPlayer3.ConfigIni.KeyAssign.KeyIsPressed(TJAPlayer3.ConfigIni.KeyAssign.System.Cancel) ||
                TJAPlayer3.InputManager.Keyboard.KeyPressed((int)SlimDXKeys.Key.Escape))
            {
                TJAPlayer3.Skin.soundCancelSFX.tPlay();
                Stop();

                return 0;
            }

            return 0;
        }

        public override int Draw()
        {
            if (IsDeActivated || CalibrateTick.IsStoped)
                return 1;

            TJAPlayer3.Tx.CalibrateBG?.t2D描画(BGs[buttonIndex].X, BGs[buttonIndex].Y, BGs[buttonIndex]);
            TJAPlayer3.Tx.CalibrateFG?.t2D描画(0, 0);

            //507,178
            TJAPlayer3.act文字コンソール.tPrint(507, 178, C文字コンソール.Eフォント種別.白, "AVERAGE OFFSET: " + GetAverageOffset());
            TJAPlayer3.act文字コンソール.tPrint(507, 202, C文字コンソール.Eフォント種別.白, "MIN OFFSET    : " + GetLowestOffset());
            TJAPlayer3.act文字コンソール.tPrint(507, 226, C文字コンソール.Eフォント種別.白, "MAX OFFSET    : " + GetHighestOffset());
            TJAPlayer3.act文字コンソール.tPrint(507, 250, C文字コンソール.Eフォント種別.白, "LAST OFFSET   : " + GetLastOffset());
            TJAPlayer3.act文字コンソール.tPrint(507, 274, C文字コンソール.Eフォント種別.白, "OFFSET COUNT  : " + (Offsets != null ? Offsets.Count : 0));
            TJAPlayer3.act文字コンソール.tPrint(507, 298, Math.Abs(CurrentOffset()) <= 50 ? C文字コンソール.Eフォント種別.赤 : C文字コンソール.Eフォント種別.白, "CURRENT OFFSET: " + CurrentOffset());

            return 0;
        }

        public void AddOffset() { Offsets.Add(CurrentOffset()); }

        public int GetAverageOffset() 
        {
            if (Offsets != null)
                return Offsets.Count > 0 ? (int)Offsets.Average() : 0;
            return 0;
        }
        public int GetLowestOffset()
        {
            if (Offsets != null)
                return Offsets.Count > 0 ? Offsets.Min() : 0;
            return 0;
        }
        public int GetHighestOffset()
        {
            if (Offsets != null)
                return Offsets.Count > 0 ? Offsets.Max() : 0;
            return 0;
        }
        public int GetLastOffset()
        {
            if (Offsets != null)
                return Offsets.Count > 0 ? Offsets.Last() : 0;
            return 0;
        }
        public int CurrentOffset()
        {
            return CalibrateTick.CurrentValue > 250 ? CalibrateTick.CurrentValue - 500 : CalibrateTick.CurrentValue;
        }

        public bool IsStarted { get { return CalibrateTick.IsStarted; } }
        #region Private
        private CCounter CalibrateTick = new CCounter();
        private List<int> Offsets = new List<int>();

        private int buttonIndex = 1;
        private Rectangle[] BGs = new Rectangle[3]
        {
            new Rectangle(371, 724, 371, 209),
            new Rectangle(774, 724, 371, 209),
            new Rectangle(1179, 724, 371, 209)
        };
        #endregion
    }
}
