using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FDK;

namespace TJAPlayer3
{
    internal class CAct演奏Drums連打 : CActivity
    {


        public CAct演奏Drums連打()
        {
            ST文字位置[] st文字位置Array = new ST文字位置[ 11 ];

			ST文字位置 st文字位置 = new ST文字位置();
			st文字位置.ch = '0';
			st文字位置.pt = new Point( 0, 0 );
			st文字位置Array[ 0 ] = st文字位置;
			ST文字位置 st文字位置2 = new ST文字位置();
			st文字位置2.ch = '1';
			st文字位置2.pt = new Point( 62, 0 );
			st文字位置Array[ 1 ] = st文字位置2;
			ST文字位置 st文字位置3 = new ST文字位置();
			st文字位置3.ch = '2';
			st文字位置3.pt = new Point( 124, 0 );
			st文字位置Array[ 2 ] = st文字位置3;
			ST文字位置 st文字位置4 = new ST文字位置();
			st文字位置4.ch = '3';
			st文字位置4.pt = new Point( 186, 0 );
			st文字位置Array[ 3 ] = st文字位置4;
			ST文字位置 st文字位置5 = new ST文字位置();
			st文字位置5.ch = '4';
			st文字位置5.pt = new Point( 248, 0 );
			st文字位置Array[ 4 ] = st文字位置5;
			ST文字位置 st文字位置6 = new ST文字位置();
			st文字位置6.ch = '5';
			st文字位置6.pt = new Point( 310, 0 );
			st文字位置Array[ 5 ] = st文字位置6;
			ST文字位置 st文字位置7 = new ST文字位置();
			st文字位置7.ch = '6';
			st文字位置7.pt = new Point( 372, 0 );
			st文字位置Array[ 6 ] = st文字位置7;
			ST文字位置 st文字位置8 = new ST文字位置();
			st文字位置8.ch = '7';
			st文字位置8.pt = new Point( 434, 0 );
			st文字位置Array[ 7 ] = st文字位置8;
			ST文字位置 st文字位置9 = new ST文字位置();
			st文字位置9.ch = '8';
			st文字位置9.pt = new Point( 496, 0 );
			st文字位置Array[ 8 ] = st文字位置9;
			ST文字位置 st文字位置10 = new ST文字位置();
			st文字位置10.ch = '9';
			st文字位置10.pt = new Point( 558, 0 );
			st文字位置Array[ 9 ] = st文字位置10;

			this.st文字位置 = st文字位置Array;

			base.b活性化してない = true;

        }

        public override void On活性化()
        {
            this.ct連打枠カウンター = new CCounter[ 4 ];
            this.ct連打アニメ = new CCounter[4];
            FadeOut = new Animations.FadeOut[4];
            for ( int i = 0; i < 4; i++ )
            {
                this.ct連打枠カウンター[ i ] = new CCounter();
                this.ct連打アニメ[i] = new CCounter();
                // 後から変えれるようにする。大体10フレーム分。
                FadeOut[i] = new Animations.FadeOut(167);
            }
            this.b表示 = new bool[]{ false, false, false, false };
            this.n連打数 = new int[ 4 ];

            base.On活性化();
        }

        public override void On非活性化()
        {
            for (int i = 0; i < 4; i++)
            {
                ct連打枠カウンター[i] = null;
                ct連打アニメ[i] = null;
                FadeOut[i] = null;
            }
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

        public override int On進行描画( )
        {
            return base.On進行描画();
        }

        public int On進行描画( int n連打数, int player )
        {
            this.ct連打枠カウンター[ player ].t進行();
            this.ct連打アニメ[player].t進行();
            FadeOut[player].Tick();
            //1PY:-3 2PY:514
            //仮置き
            int[] nRollBalloon = new int[] { -3, 514, 0, 0 };
            int[] nRollNumber = new int[] { 48, 559, 0, 0 };
            for( int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++ )
            {
                //CDTXMania.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, this.ct連打枠カウンター[player].n現在の値.ToString());
                if ( this.ct連打枠カウンター[ player ].b終了値に達してない)
                {
                    if (ct連打枠カウンター[player].n現在の値 > 66 && !FadeOut[player].Counter.b進行中)
                    {
                        FadeOut[player].Start();
                    }
                    var opacity = (int)FadeOut[player].GetAnimation();

                    if(ct連打枠カウンター[player].n現在の値 == 0 || ct連打枠カウンター[player].n現在の値 == 60)
                    {
                        bNowRollAnime = 0;
                        TJAPlayer3.Tx.Balloon_Number_Roll.Opacity = 64;
                    }
                    else if (ct連打枠カウンター[player].n現在の値 == 1 || ct連打枠カウンター[player].n現在の値 == 59)
                    {
                        bNowRollAnime = 1;
                        TJAPlayer3.Tx.Balloon_Number_Roll.Opacity = 128;
                    }
                    else if (ct連打枠カウンター[player].n現在の値 == 2 || ct連打枠カウンター[player].n現在の値 == 58)
                    {
                        bNowRollAnime = 2;
                        TJAPlayer3.Tx.Balloon_Number_Roll.Opacity = 192;
                    }
                    else if (ct連打枠カウンター[player].n現在の値 == 3 || ct連打枠カウンター[player].n現在の値 == 57)
                    {
                        bNowRollAnime = 3;
                        TJAPlayer3.Tx.Balloon_Number_Roll.Opacity = 255;
                    }
                    else if (ct連打枠カウンター[player].n現在の値 >= 4 || ct連打枠カウンター[player].n現在の値 <= 56)
                    {
                        bNowRollAnime = 4;
                        TJAPlayer3.Tx.Balloon_Number_Roll.Opacity = 255;
                    }

                    TJAPlayer3.Tx.Balloon_Roll?.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Balloon_Roll_Frame_X[player], TJAPlayer3.Skin.Game_Balloon_Roll_Frame_Y[player], new RectangleF(0 + bNowRollAnime * 334, 0, 334, 204)); ;
                    this.t文字表示(TJAPlayer3.Skin.Game_Balloon_Roll_Number_X[player], TJAPlayer3.Skin.Game_Balloon_Roll_Number_Y[player], n連打数.ToString(), n連打数, player);
                }
            }

            return base.On進行描画();
        }

        public void t枠表示時間延長(int player, bool first)
        {
            if ((this.ct連打枠カウンター[player].n現在の値 >= 6 && !first) || first)
                this.ct連打枠カウンター[player] = new CCounter(0, 60, 40, TJAPlayer3.Timer);

            if(!first)
                this.ct連打枠カウンター[player].n現在の値 = 5;
            else
                this.ct連打枠カウンター[player].n現在の値 = 0;
        }

        public int bNowRollAnime;
        public bool[] b表示;
        public int[] n連打数;
        public CCounter[] ct連打枠カウンター;
        //private CTexture tx連打枠;
        //private CTexture tx連打数字;
        private readonly ST文字位置[] st文字位置;
        public CCounter[] ct連打アニメ;
        private float[] RollScale = new float[]
        {
            0.000f,
            0.123f, // リピート
            0.164f,
            0.164f,
            0.164f,
            0.137f,
            0.110f,
            0.082f,
            0.055f,
            0.000f
        };
        private Animations.FadeOut[] FadeOut;

        [StructLayout(LayoutKind.Sequential)]
        private struct ST文字位置
        {
            public char ch;
            public Point pt;
        }

        private void t文字表示( int x, int y, string str, int n連打, int nPlayer)
		{
            int n桁数 = n連打.ToString().Length;
            
            //CDTXMania.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, ct連打アニメ[nPlayer].n現在の値.ToString());
            foreach ( char ch in str )
			{
				for( int i = 0; i < this.st文字位置.Length; i++ )
				{
					if( this.st文字位置[ i ].ch == ch )
					{
						Rectangle rectangle = new Rectangle(TJAPlayer3.Skin.Game_Balloon_Number_Size[0] * i, 0, TJAPlayer3.Skin.Game_Balloon_Number_Size[0], TJAPlayer3.Skin.Game_Balloon_Number_Size[1]);

						if(TJAPlayer3.Tx.Balloon_Number_Roll != null )
						{
                            TJAPlayer3.Tx.Balloon_Number_Roll.vc拡大縮小倍率.X = TJAPlayer3.Skin.Game_Balloon_Roll_Number_Scale;
                            TJAPlayer3.Tx.Balloon_Number_Roll.vc拡大縮小倍率.Y = TJAPlayer3.Skin.Game_Balloon_Roll_Number_Scale + RollScale[this.ct連打アニメ[nPlayer].n現在の値];
                            TJAPlayer3.Tx.Balloon_Number_Roll.t2D拡大率考慮下基準描画( TJAPlayer3.app.Device, x - ( ( (TJAPlayer3.Skin.Game_Balloon_Number_Padding + 2) * n桁数 ) / 2 ), y, rectangle );
						}
						break;
					}
				}
				x += ( TJAPlayer3.Skin.Game_Balloon_Number_Padding - ( n桁数 > 2 ? n桁数 * 2 : 0 ) );
			}
		}
    }
}
