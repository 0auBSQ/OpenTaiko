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

			base.IsDeActivated = true;

        }

        public override void Activate()
        {
            this.ct連打枠カウンター = new CCounter[ 5 ];
            this.ct連打アニメ = new CCounter[5];
            FadeOut = new Animations.FadeOut[5];
            for ( int i = 0; i < 5; i++ )
            {
                this.ct連打枠カウンター[ i ] = new CCounter();
                this.ct連打アニメ[i] = new CCounter();
                // 後から変えれるようにする。大体10フレーム分。
                FadeOut[i] = new Animations.FadeOut(167);
            }
            this.b表示 = new bool[]{ false, false, false, false, false };
            this.n連打数 = new int[ 5 ];

            base.Activate();
        }

        public override void DeActivate()
        {
            for (int i = 0; i < 5; i++)
            {
                ct連打枠カウンター[i] = null;
                ct連打アニメ[i] = null;
                FadeOut[i] = null;
            }
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

        public override int Draw( )
        {
            return base.Draw();
        }

        public int On進行描画( int n連打数, int player )
        {
            if (TJAPlayer3.ConfigIni.nPlayerCount > 2) return base.Draw();

            this.ct連打枠カウンター[ player ].Tick();
            this.ct連打アニメ[player].Tick();
            FadeOut[player].Tick();
            //1PY:-3 2PY:514
            //仮置き
            int[] nRollBalloon = new int[] { -3, 514, 0, 0 };
            int[] nRollNumber = new int[] { 48, 559, 0, 0 };
            for( int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++ )
            {
                //CDTXMania.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, this.ct連打枠カウンター[player].n現在の値.ToString());
                if ( this.ct連打枠カウンター[ player ].IsUnEnded)
                {
                    if (ct連打枠カウンター[player].CurrentValue > 66 && !FadeOut[player].Counter.IsTicked)
                    {
                        FadeOut[player].Start();
                    }
                    var opacity = (int)FadeOut[player].GetAnimation();

                    if(ct連打枠カウンター[player].CurrentValue == 0 || ct連打枠カウンター[player].CurrentValue == 60)
                    {
                        bNowRollAnime = 0;
                        TJAPlayer3.Tx.Balloon_Number_Roll.Opacity = 64;
                    }
                    else if (ct連打枠カウンター[player].CurrentValue == 1 || ct連打枠カウンター[player].CurrentValue == 59)
                    {
                        bNowRollAnime = 1;
                        TJAPlayer3.Tx.Balloon_Number_Roll.Opacity = 128;
                    }
                    else if (ct連打枠カウンター[player].CurrentValue == 2 || ct連打枠カウンター[player].CurrentValue == 58)
                    {
                        bNowRollAnime = 2;
                        TJAPlayer3.Tx.Balloon_Number_Roll.Opacity = 192;
                    }
                    else if (ct連打枠カウンター[player].CurrentValue == 3 || ct連打枠カウンター[player].CurrentValue == 57)
                    {
                        bNowRollAnime = 3;
                        TJAPlayer3.Tx.Balloon_Number_Roll.Opacity = 255;
                    }
                    else if (ct連打枠カウンター[player].CurrentValue >= 4 || ct連打枠カウンター[player].CurrentValue <= 56)
                    {
                        bNowRollAnime = 4;
                        TJAPlayer3.Tx.Balloon_Number_Roll.Opacity = 255;
                    }

                    float width = TJAPlayer3.Tx.Balloon_Roll.szテクスチャサイズ.Width / 5.0f;
                    float height = TJAPlayer3.Tx.Balloon_Roll.szテクスチャサイズ.Height;

                    TJAPlayer3.Tx.Balloon_Roll?.t2D描画(TJAPlayer3.Skin.Game_Balloon_Roll_Frame_X[player], TJAPlayer3.Skin.Game_Balloon_Roll_Frame_Y[player], new RectangleF(0 + bNowRollAnime * width, 0, width, height));
                    this.t文字表示(TJAPlayer3.Skin.Game_Balloon_Roll_Number_X[player], TJAPlayer3.Skin.Game_Balloon_Roll_Number_Y[player], n連打数, player);
                }
            }

            return base.Draw();
        }

        public void t枠表示時間延長(int player, bool first)
        {
            if ((this.ct連打枠カウンター[player].CurrentValue >= 6 && !first) || first)
                this.ct連打枠カウンター[player] = new CCounter(0, 60, 40, TJAPlayer3.Timer);

            if(!first)
                this.ct連打枠カウンター[player].CurrentValue = 5;
            else
                this.ct連打枠カウンター[player].CurrentValue = 0;
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

        private void t文字表示( int x, int y, int num, int nPlayer)
        {
            TJAPlayer3.Tx.Balloon_Number_Roll.vc拡大縮小倍率.X = TJAPlayer3.Skin.Game_Balloon_Roll_Number_Scale;
            TJAPlayer3.Tx.Balloon_Number_Roll.vc拡大縮小倍率.Y = TJAPlayer3.Skin.Game_Balloon_Roll_Number_Scale + RollScale[this.ct連打アニメ[nPlayer].CurrentValue];

            int[] nums = CConversion.SeparateDigits(num);
            for (int j = 0; j < nums.Length; j++)
            {
                float offset = j - (nums.Length / 2.0f);
                float _x = x - (TJAPlayer3.Skin.Game_Balloon_Number_Interval[0] * offset);
                float _y = y - (TJAPlayer3.Skin.Game_Balloon_Number_Interval[1] * offset);

                float width = TJAPlayer3.Tx.Balloon_Number_Roll.sz画像サイズ.Width / 10.0f;
                float height = TJAPlayer3.Tx.Balloon_Number_Roll.sz画像サイズ.Height;

                TJAPlayer3.Tx.Balloon_Number_Roll.t2D拡大率考慮下基準描画(_x, _y, new RectangleF(width * nums[j], 0, width, height));
            }
        }
    }
}
