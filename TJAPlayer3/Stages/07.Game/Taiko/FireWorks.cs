using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using SlimDX;
using FDK;

namespace TJAPlayer3
{
    internal class FireWorks : CActivity
    {
        // コンストラクタ

        public FireWorks()
        {
            base.b活性化してない = true;
        }


        // メソッド

        /// <summary>
        /// 大音符の花火エフェクト
        /// </summary>
        /// <param name="nLane"></param>
        public virtual void Start(int nLane, int nPlayer, double x, double y)
        {
            for (int i = 0; i < 32; i++)
            {
                if(!FireWork[i].IsUsing)
                {
                    FireWork[i].IsUsing = true;
                    FireWork[i].Lane = nLane;
                    FireWork[i].Player = nPlayer;
                    FireWork[i].X = x;
                    FireWork[i].Y = y;
                    FireWork[i].Counter = new CCounter(0, TJAPlayer3.Skin.Game_Effect_FireWorks[2] - 1, TJAPlayer3.Skin.Game_Effect_FireWorks_Timer, TJAPlayer3.Timer);
                    break;
                }
            }
        }

        // CActivity 実装

        public override void On活性化()
        {
            for (int i = 0; i < 32; i++)
            {
                FireWork[i] = new Status();
                FireWork[i].IsUsing = false;
                FireWork[i].Counter = new CCounter();
            }
            base.On活性化();
        }
        public override void On非活性化()
        {
            for (int i = 0; i < 32; i++)
            {
                FireWork[i].Counter = null;
            }
            base.On非活性化();
        }
        public override void OnManagedリソースの作成()
        {
            if (!base.b活性化してない)
            {
                base.OnManagedリソースの作成();
            }
        }
        public override void OnManagedリソースの解放()
        {
            if (!base.b活性化してない)
            {
                base.OnManagedリソースの解放();
            }
        }
        public override int On進行描画()
        {
            if (!base.b活性化してない)
            {
                for (int i = 0; i < 32; i++)
                {
                    if(FireWork[i].IsUsing)
                    {
                        FireWork[i].Counter.t進行();
                        TJAPlayer3.Tx.Effects_Hit_FireWorks.t2D中心基準描画(TJAPlayer3.app.Device, (float)FireWork[i].X, (float)FireWork[i].Y, 1, new Rectangle(FireWork[i].Counter.n現在の値 * TJAPlayer3.Skin.Game_Effect_FireWorks[0], 0, TJAPlayer3.Skin.Game_Effect_FireWorks[0], TJAPlayer3.Skin.Game_Effect_FireWorks[1]));
                        if (FireWork[i].Counter.b終了値に達した)
                        {
                            FireWork[i].Counter.t停止();
                            FireWork[i].IsUsing = false;
                        }
                    }
                }
            }
            return 0;
        }


        // その他

        #region [ private ]
        //-----------------
        [StructLayout(LayoutKind.Sequential)]
        private struct Status
        {
            public int Lane;
            public int Player;
            public bool IsUsing;
            public CCounter Counter;
            public double X;
            public double Y;
        }
        private Status[] FireWork = new Status[32];

        //-----------------
        #endregion
    }
}
　
