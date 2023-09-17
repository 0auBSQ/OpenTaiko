using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
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
            base.IsDeActivated = true;
        }
		
		
		// メソッド
        public virtual void Start( int nCombo, int player )
		{
            this.NowDrawBalloon = 0;
            this.ct進行[ player ] = new CCounter( 1, 42, 70, TJAPlayer3.Timer );
            this.nCombo_渡[player] = nCombo;
		}

		// CActivity 実装

		public override void Activate()
		{
            for( int i = 0; i < 2; i++ )
            {
                this.nCombo_渡[ i ] = 0;
                this.ct進行[ i ] = new CCounter();
            }

            base.Activate();
		}
		public override void DeActivate()
		{
            for( int i = 0; i < 2; i++ )
            {
                this.ct進行[ i ] = null;
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
		public override int Draw()
		{
			if( !base.IsDeActivated )
			{
                if (TJAPlayer3.ConfigIni.nPlayerCount > 2) return 0;
                for( int i = 0; i < 2; i++ )
                {
                    if (TJAPlayer3.ConfigIni.bAIBattleMode) break;

                    int j = i;
                    if (TJAPlayer3.PlayerSide == 1 && TJAPlayer3.ConfigIni.nPlayerCount == 1)
                        j = 1;

                    if ( !this.ct進行[ i ].IsStoped )
                    {
                        this.ct進行[ i ].Tick();
                        if( this.ct進行[ i ].IsEnded )
                        {
                            this.ct進行[ i ].Stop();
                        }
                    }

                    if( TJAPlayer3.Tx.Balloon_Combo[ j ] != null && TJAPlayer3.Tx.Balloon_Number_Combo != null)
                    {
                        //半透明4f
                        if( this.ct進行[ i ].CurrentValue == 1 || this.ct進行[ i ].CurrentValue == 42 )
                        {
                            TJAPlayer3.Tx.Balloon_Number_Combo.Opacity = 0;
                            TJAPlayer3.Tx.Balloon_Combo[j].Opacity = 64;
                            NowDrawBalloon = 0;
                        }
                        else if( this.ct進行[ i ].CurrentValue == 2 || this.ct進行[ i ].CurrentValue == 41 )
                        {
                            TJAPlayer3.Tx.Balloon_Number_Combo.Opacity = 0;
                            TJAPlayer3.Tx.Balloon_Combo[j].Opacity = 128;
                            NowDrawBalloon = 0;
                        }
                        else if( this.ct進行[ i ].CurrentValue == 3 || this.ct進行[ i ].CurrentValue == 40 )
                        {
                            NowDrawBalloon = 1;
                            TJAPlayer3.Tx.Balloon_Combo[j].Opacity = 255;
                            TJAPlayer3.Tx.Balloon_Number_Combo.Opacity = 128;
                        }
                        else if( this.ct進行[ i ].CurrentValue == 4 || this.ct進行[ i ].CurrentValue == 39 )
                        {
                            NowDrawBalloon = 2;
                            TJAPlayer3.Tx.Balloon_Combo[j].Opacity = 255;
                            TJAPlayer3.Tx.Balloon_Number_Combo.Opacity = 255;
                        }
                        else if( this.ct進行[ i ].CurrentValue == 5 || this.ct進行[ i ].CurrentValue == 38 )
                        {
                            NowDrawBalloon = 2;
                            TJAPlayer3.Tx.Balloon_Combo[j].Opacity = 255;
                            TJAPlayer3.Tx.Balloon_Number_Combo.Opacity = 255;
                        }
                        else if( this.ct進行[ i ].CurrentValue >= 6 || this.ct進行[ i ].CurrentValue <= 37 )
                        {
                            NowDrawBalloon = 2;
                            TJAPlayer3.Tx.Balloon_Combo[j].Opacity = 255;
                            TJAPlayer3.Tx.Balloon_Number_Combo.Opacity = 255;
                        }

                        if( this.ct進行[ i ].IsTicked )
                        {
                            int plate_width = TJAPlayer3.Tx.Balloon_Combo[j].szテクスチャサイズ.Width / 3;
                            int plate_height = TJAPlayer3.Tx.Balloon_Combo[j].szテクスチャサイズ.Height;
                            TJAPlayer3.Tx.Balloon_Combo[ j ].t2D描画( TJAPlayer3.Skin.Game_Balloon_Combo_X[ i ], TJAPlayer3.Skin.Game_Balloon_Combo_Y[ i ], new RectangleF(NowDrawBalloon * plate_width, 0, plate_width, plate_height) );
                            if( this.nCombo_渡[ i ] < 1000 ) //2016.08.23 kairera0467 仮実装。
                            {
                                this.t小文字表示( TJAPlayer3.Skin.Game_Balloon_Combo_Number_X[ i], TJAPlayer3.Skin.Game_Balloon_Combo_Number_Y[ i ], this.nCombo_渡[ i ], j);
                                TJAPlayer3.Tx.Balloon_Number_Combo.t2D描画( TJAPlayer3.Skin.Game_Balloon_Combo_Text_X[ i] + 6 - NowDrawBalloon * 3, TJAPlayer3.Skin.Game_Balloon_Combo_Text_Y[ i ], 
                                    new Rectangle(TJAPlayer3.Skin.Game_Balloon_Combo_Text_Rect[0], TJAPlayer3.Skin.Game_Balloon_Combo_Text_Rect[1], TJAPlayer3.Skin.Game_Balloon_Combo_Text_Rect[2], TJAPlayer3.Skin.Game_Balloon_Combo_Text_Rect[3]));
                            }
                            else
                            {
                                this.t小文字表示( TJAPlayer3.Skin.Game_Balloon_Combo_Number_Ex_X[ i], TJAPlayer3.Skin.Game_Balloon_Combo_Number_Ex_Y[ i ], this.nCombo_渡[ i ], j );
                                TJAPlayer3.Tx.Balloon_Number_Combo.vc拡大縮小倍率.X = 1.0f;
                                TJAPlayer3.Tx.Balloon_Number_Combo.t2D描画( TJAPlayer3.Skin.Game_Balloon_Combo_Text_Ex_X[ i] + 6 - NowDrawBalloon * 3, TJAPlayer3.Skin.Game_Balloon_Combo_Text_Ex_Y[ i ],
                                    new Rectangle(TJAPlayer3.Skin.Game_Balloon_Combo_Text_Rect[0], TJAPlayer3.Skin.Game_Balloon_Combo_Text_Rect[1], TJAPlayer3.Skin.Game_Balloon_Combo_Text_Rect[2], TJAPlayer3.Skin.Game_Balloon_Combo_Text_Rect[3]));
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
        private CCounter[] ct進行 = new CCounter[ 5 ];
        //private CTexture[] tx吹き出し本体 = new CTexture[ 2 ];
        //private CTexture tx数字;
        private int[] nCombo_渡 = new int[ 5 ];

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

		private void t小文字表示( int x, int y, int num, int player )
        {
            int[] nums = CConversion.SeparateDigits(num);
            for (int j = 0; j < nums.Length; j++)
            {
                float _x = x - (TJAPlayer3.Skin.Game_Balloon_Combo_Number_Interval[0] * (j - nums.Length));
                float _y = y - (TJAPlayer3.Skin.Game_Balloon_Combo_Number_Interval[1] * (j - nums.Length));

                float width = TJAPlayer3.Skin.Game_Balloon_Combo_Number_Size[0];
                float height = TJAPlayer3.Skin.Game_Balloon_Combo_Number_Size[1];

                TJAPlayer3.Tx.Balloon_Number_Combo.t2D描画(_x, _y, new RectangleF(width * nums[j], height * player, width, height));
            }
        }
		//-----------------
		#endregion
	}
}
