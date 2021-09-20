﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using SlimDX;
using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏Drumsコンボ吹き出し : CActivity
	{
        // コンストラクタ

        /// <summary>
        /// 100コンボごとに出る吹き出し。
        /// 本当は「10000点」のところも動かしたいけど、技術不足だし保留。
        /// </summary>
        public CAct演奏Drumsコンボ吹き出し()
        {
            for (int i = 0; i < 10; i++)
            {
                this.st小文字位置[i].ch = i.ToString().ToCharArray()[0];
                this.st小文字位置[i].pt = new Point(i * 53, 0);
            }
            base.b活性化してない = true;
        }
		
		
		// メソッド
        public virtual void Start( int nCombo, int player )
		{
            this.NowDrawBalloon = 0;
            this.ct進行[ player ] = new CCounter( 1, 42, 70, TJAPlayer3.Timer );
            this.nCombo_渡[player] = nCombo;
		}

		// CActivity 実装

		public override void On活性化()
		{
            for( int i = 0; i < 2; i++ )
            {
                this.nCombo_渡[ i ] = 0;
                this.ct進行[ i ] = new CCounter();
            }

            base.On活性化();
		}
		public override void On非活性化()
		{
            for( int i = 0; i < 2; i++ )
            {
                this.ct進行[ i ] = null;
            }
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
                //this.tx吹き出し本体[ 0 ] = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_combo balloon.png" ) );
                //if (CDTXMania.stage演奏ドラム画面.bDoublePlay)
                //    this.tx吹き出し本体[ 1 ] = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_combo balloon_2P.png" ) );
                //this.tx数字 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\7_combo balloon_number.png" ) );
				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if( !base.b活性化してない )
			{
                //CDTXMania.tテクスチャの解放( ref this.tx吹き出し本体[ 0 ] );
                //if (CDTXMania.stage演奏ドラム画面.bDoublePlay)
                //    CDTXMania.tテクスチャの解放( ref this.tx吹き出し本体[ 1 ] );
                //CDTXMania.tテクスチャの解放( ref this.tx数字 );
				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			if( !base.b活性化してない )
			{
                for( int i = 0; i < 2; i++ )
                {
                    if( !this.ct進行[ i ].b停止中 )
                    {
                        this.ct進行[ i ].t進行();
                        if( this.ct進行[ i ].b終了値に達した )
                        {
                            this.ct進行[ i ].t停止();
                        }
                    }

                    if( TJAPlayer3.Tx.Balloon_Combo[ i ] != null )
                    {
                        //半透明4f
                        if( this.ct進行[ i ].n現在の値 == 1 || this.ct進行[ i ].n現在の値 == 42 )
                        {
                            TJAPlayer3.Tx.Balloon_Number_Combo.Opacity = 0;
                            TJAPlayer3.Tx.Balloon_Combo[i].Opacity = 64;
                            NowDrawBalloon = 0;
                        }
                        else if( this.ct進行[ i ].n現在の値 == 2 || this.ct進行[ i ].n現在の値 == 41 )
                        {
                            TJAPlayer3.Tx.Balloon_Number_Combo.Opacity = 0;
                            TJAPlayer3.Tx.Balloon_Combo[i].Opacity = 128;
                            NowDrawBalloon = 0;
                        }
                        else if( this.ct進行[ i ].n現在の値 == 3 || this.ct進行[ i ].n現在の値 == 40 )
                        {
                            NowDrawBalloon = 1;
                            TJAPlayer3.Tx.Balloon_Combo[i].Opacity = 255;
                            TJAPlayer3.Tx.Balloon_Number_Combo.Opacity = 128;
                        }
                        else if( this.ct進行[ i ].n現在の値 == 4 || this.ct進行[ i ].n現在の値 == 39 )
                        {
                            NowDrawBalloon = 2;
                            TJAPlayer3.Tx.Balloon_Combo[i].Opacity = 255;
                            TJAPlayer3.Tx.Balloon_Number_Combo.Opacity = 255;
                        }
                        else if( this.ct進行[ i ].n現在の値 == 5 || this.ct進行[ i ].n現在の値 == 38 )
                        {
                            NowDrawBalloon = 2;
                            TJAPlayer3.Tx.Balloon_Combo[i].Opacity = 255;
                            TJAPlayer3.Tx.Balloon_Number_Combo.Opacity = 255;
                        }
                        else if( this.ct進行[ i ].n現在の値 >= 6 || this.ct進行[ i ].n現在の値 <= 37 )
                        {
                            NowDrawBalloon = 2;
                            TJAPlayer3.Tx.Balloon_Combo[i].Opacity = 255;
                            TJAPlayer3.Tx.Balloon_Number_Combo.Opacity = 255;
                        }

                        if( this.ct進行[ i ].b進行中 )
                        {
                            TJAPlayer3.Tx.Balloon_Combo[ i ].t2D描画( TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Balloon_Combo_X[ i ], TJAPlayer3.Skin.Game_Balloon_Combo_Y[ i ], new RectangleF(NowDrawBalloon * 360f, 0, 360f, 192) );
                            if( this.nCombo_渡[ i ] < 1000 ) //2016.08.23 kairera0467 仮実装。
                            {
                                this.t小文字表示( TJAPlayer3.Skin.Game_Balloon_Combo_Number_X[ i], TJAPlayer3.Skin.Game_Balloon_Combo_Number_Y[ i ], string.Format( "{0,4:###0}", this.nCombo_渡[ i ] ), i);
                                TJAPlayer3.Tx.Balloon_Number_Combo.t2D描画( TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Balloon_Combo_Text_X[ i] + 6 - NowDrawBalloon * 3, TJAPlayer3.Skin.Game_Balloon_Combo_Text_Y[ i ], new Rectangle(0, 124, 100, 30));
                            }
                            else
                            {
                                this.t小文字表示( TJAPlayer3.Skin.Game_Balloon_Combo_Number_Ex_X[ i], TJAPlayer3.Skin.Game_Balloon_Combo_Number_Ex_Y[ i ], string.Format( "{0,4:###0}", this.nCombo_渡[ i ] ), i );
                                TJAPlayer3.Tx.Balloon_Number_Combo.vc拡大縮小倍率.X = 1.0f;
                                TJAPlayer3.Tx.Balloon_Number_Combo.t2D描画( TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Balloon_Combo_Text_Ex_X[ i] + 6 - NowDrawBalloon * 3, TJAPlayer3.Skin.Game_Balloon_Combo_Text_Ex_Y[ i ], new Rectangle( 0, 124, 100, 30 ) );
                            }
                        }
                    }
                }
			}
			return 0;
		}
		

		// その他

		#region [ private ]
		//-----------------
        private CCounter[] ct進行 = new CCounter[ 2 ];
        //private CTexture[] tx吹き出し本体 = new CTexture[ 2 ];
        //private CTexture tx数字;
        private int[] nCombo_渡 = new int[ 2 ];

        private int NowDrawBalloon;

        [StructLayout(LayoutKind.Sequential)]
        private struct ST文字位置
        {
            public char ch;
            public Point pt;
            public ST文字位置( char ch, Point pt )
            {
                this.ch = ch;
                this.pt = pt;
            }
        }
        private ST文字位置[] st小文字位置 = new ST文字位置[10];

		private void t小文字表示( int x, int y, string str, int player )
		{
			foreach( char ch in str )
			{
				for( int i = 0; i < this.st小文字位置.Length; i++ )
				{
					if( this.st小文字位置[ i ].ch == ch )
					{
						Rectangle rectangle = new Rectangle( this.st小文字位置[ i ].pt.X, this.st小文字位置[ i ].pt.Y + player * 62, 53, 62 );
						if(TJAPlayer3.Tx.Balloon_Number_Combo != null )
						{
                            if (int.Parse(str) >= 1000)
                                TJAPlayer3.Tx.Balloon_Number_Combo.vc拡大縮小倍率.X = 0.8f;
                            else
                                TJAPlayer3.Tx.Balloon_Number_Combo.vc拡大縮小倍率.X = 1.0f;

                            TJAPlayer3.Tx.Balloon_Number_Combo.t2D描画( TJAPlayer3.app.Device, x, y, rectangle );
						}
						break;
					}
				}
                x += (int)(45 * TJAPlayer3.Tx.Balloon_Number_Combo.vc拡大縮小倍率.X);
			}
		}
		//-----------------
		#endregion
	}
}
