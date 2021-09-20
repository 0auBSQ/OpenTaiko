using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using SlimDX;
using FDK;

namespace TJAPlayer3
{
	internal class Rainbow : CActivity
	{
		// コンストラクタ

		public Rainbow()
		{
			base.b活性化してない = true;
		}
		
        public virtual void Start( int player )
		{
            if (TJAPlayer3.Tx.Effects_Rainbow != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (!this.Rainbow1P[i].IsUsing && player == 0)
                    {
                        this.Rainbow1P[i].IsUsing = true;
                        this.Rainbow1P[i].Counter = new CCounter(0, 164, TJAPlayer3.Skin.Game_Effect_Rainbow_Timer, TJAPlayer3.Timer); // カウンタ
                        this.Rainbow1P[i].Player = player;
                        break;
                    }
                    if (!this.Rainbow2P[i].IsUsing && player == 1)
                    {
                        this.Rainbow2P[i].IsUsing = true;
                        this.Rainbow2P[i].Counter = new CCounter(0, 164, TJAPlayer3.Skin.Game_Effect_Rainbow_Timer, TJAPlayer3.Timer); // カウンタ
                        this.Rainbow2P[i].Player = player;
                        break;
                    }
                }
            }
		}


		// CActivity 実装

		public override void On活性化()
		{
            for( int i = 0; i < 2; i++ )
			{
				this.Rainbow1P[ i ].Counter = new CCounter();
				this.Rainbow2P[ i ].Counter = new CCounter();
			}
            base.On活性化();
		}
		public override void On非活性化()
		{
            for( int i = 0; i < 2; i++ )
			{
				this.Rainbow1P[ i ].Counter = null;
				this.Rainbow2P[ i ].Counter = null;
			}
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if( !base.b活性化してない )
			{
				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			if( !base.b活性化してない )
			{
                for (int f = 0; f < 2; f++)
                {
                    if (this.Rainbow1P[f].IsUsing)
                    {
                        this.Rainbow1P[f].Counter.t進行();
                        if (this.Rainbow1P[f].Counter.b終了値に達した)
                        {
                            this.Rainbow1P[f].Counter.t停止();
                            this.Rainbow1P[f].IsUsing = false;
                        }

                        if(TJAPlayer3.Tx.Effects_Rainbow != null && this.Rainbow1P[f].Player == 0 ) //画像が出来るまで
                        {
                            //this.st虹[f].ct進行.n現在の値 = 164;

                            if (this.Rainbow1P[f].Counter.n現在の値 < 82)
                            {
                                int nRectX = ((this.Rainbow1P[f].Counter.n現在の値 * 920) / 85);
                                TJAPlayer3.Tx.Effects_Rainbow.t2D描画(TJAPlayer3.app.Device, 360, -100, new Rectangle(0, 0, nRectX, 410));
                            }
                            else if (this.Rainbow1P[f].Counter.n現在の値 >= 82)
                            {
                                int nRectX = (((this.Rainbow1P[f].Counter.n現在の値 - 82) * 920) / 85);
                                TJAPlayer3.Tx.Effects_Rainbow.t2D描画(TJAPlayer3.app.Device, 360 + nRectX, -100, new Rectangle(nRectX, 0, 920 - nRectX, 410));
                            }

                        }

                    }
                }
                for (int f = 0; f < 2; f++)
                {
                    if (this.Rainbow2P[f].IsUsing)
                    {
                        this.Rainbow2P[f].Counter.t進行();
                        if (this.Rainbow2P[f].Counter.b終了値に達した)
                        {
                            this.Rainbow2P[f].Counter.t停止();
                            this.Rainbow2P[f].IsUsing = false;
                        }

                        if(TJAPlayer3.Tx.Effects_Rainbow != null && this.Rainbow2P[f].Player == 1 ) //画像が出来るまで
                        {
                            //this.st虹[f].ct進行.n現在の値 = 164;

                            if (this.Rainbow2P[f].Counter.n現在の値 < 82)
                            {
                                int nRectX = ((this.Rainbow2P[f].Counter.n現在の値 * 920) / 85);
                                TJAPlayer3.Tx.Effects_Rainbow.t2D上下反転描画(TJAPlayer3.app.Device, 360, 410, new Rectangle(0, 0, nRectX, 410));
                            }
                            else if (this.Rainbow2P[f].Counter.n現在の値 >= 82)
                            {
                                int nRectX = (((this.Rainbow2P[f].Counter.n現在の値 - 82) * 920) / 85);
                                TJAPlayer3.Tx.Effects_Rainbow.t2D上下反転描画(TJAPlayer3.app.Device, 360 + nRectX, 410, new Rectangle(nRectX, 0, 920 - nRectX, 410));
                            }

                        }

                    }
                }
			}
            return base.On進行描画();
        }
		

		// その他

		#region [ private ]
		//-----------------

        [StructLayout(LayoutKind.Sequential)]
        private struct StructRainbow
        {
            public bool IsUsing;
            public int Player;
            public CCounter Counter;
            public float X;
        }

        private StructRainbow[] Rainbow1P = new StructRainbow[2];
        private StructRainbow[] Rainbow2P = new StructRainbow[2];

		//-----------------
		#endregion
	}
}
