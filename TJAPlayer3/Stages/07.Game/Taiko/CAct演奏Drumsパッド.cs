using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏Drumsパッド : CActivity
	{
		// コンストラクタ

		public CAct演奏Drumsパッド()
		{
			ST基本位置[] st基本位置Array = new ST基本位置[ 10 ];

            //LC
			ST基本位置 st基本位置 = new ST基本位置();
			st基本位置.x = 263;
			st基本位置.y = 10;
			st基本位置.rc = new Rectangle( 0, 0, 0x60, 0x60 );
			st基本位置Array[ 0 ] = st基本位置;

            //HH
			ST基本位置 st基本位置2 = new ST基本位置();
			st基本位置2.x = 336;
			st基本位置2.y = 10;
			st基本位置2.rc = new Rectangle( 0x60, 0, 0x60, 0x60 );
			st基本位置Array[ 1 ] = st基本位置2;

            //SD
			ST基本位置 st基本位置3 = new ST基本位置();
			st基本位置3.x = 446;
			st基本位置3.y = 10;
			st基本位置3.rc = new Rectangle( 0, 0x60, 0x60, 0x60 );
			st基本位置Array[ 2 ] = st基本位置3;

            //BD
			ST基本位置 st基本位置4 = new ST基本位置();
            st基本位置4.x = 565;
            st基本位置4.y = 10;
            st基本位置4.rc = new Rectangle( 0, 0xc0, 0x60, 0x60);
			st基本位置Array[ 3 ] = st基本位置4;

            //HT
			ST基本位置 st基本位置5 = new ST基本位置();
			st基本位置5.x = 510;
			st基本位置5.y = 10;
			st基本位置5.rc = new Rectangle( 0x60, 0x60, 0x60, 0x60 );
			st基本位置Array[ 4 ] = st基本位置5;

            //LT
			ST基本位置 st基本位置6 = new ST基本位置();
			st基本位置6.x = 622;
			st基本位置6.y = 10;
			st基本位置6.rc = new Rectangle( 0xc0, 0x60, 0x60, 0x60 );
			st基本位置Array[ 5 ] = st基本位置6;

            //FT
			ST基本位置 st基本位置7 = new ST基本位置();
			st基本位置7.x = 672;
			st基本位置7.y = 10;
			st基本位置7.rc = new Rectangle( 288, 0x60, 0x60, 0x60 );
			st基本位置Array[ 6 ] = st基本位置7;

            //CY
			ST基本位置 st基本位置8 = new ST基本位置();
			st基本位置8.x = 0x2df;
			st基本位置8.y = 10;
			st基本位置8.rc = new Rectangle( 0xc0, 0, 0x60, 0x60 );
			st基本位置Array[ 7 ] = st基本位置8;

            //RD
			ST基本位置 st基本位置9 = new ST基本位置();
			st基本位置9.x = 0x317;
			st基本位置9.y = 10;
			st基本位置9.rc = new Rectangle( 288, 0, 0x60, 0x60 );
			st基本位置Array[ 8 ] = st基本位置9;

            //LP
            ST基本位置 st基本位置10 = new ST基本位置();
            st基本位置10.x = 0x18c;
            st基本位置10.y = 10;
            st基本位置10.rc = new Rectangle( 0x60, 0xc0, 0x60, 0x60);
            st基本位置Array[ 9 ] = st基本位置10;

			this.st基本位置 = st基本位置Array;
			base.b活性化してない = true;
		}
		
		
		// メソッド

		public void Hit( int nLane )
		{
			this.stパッド状態[ nLane ].n明るさ = 6;
			this.stパッド状態[ nLane ].nY座標加速度dot = 2;
		}


		// CActivity 実装

		public override void On活性化()
		{
			this.nフラッシュ制御タイマ = -1;
			this.nY座標制御タイマ = -1;
			for( int i = 0; i < 9; i++ )
			{
				STパッド状態 stパッド状態2 = new STパッド状態();
				STパッド状態 stパッド状態 = stパッド状態2;
				stパッド状態.nY座標オフセットdot = 0;
				stパッド状態.nY座標加速度dot = 0;
				stパッド状態.n明るさ = 0;
				this.stパッド状態[ i ] = stパッド状態;
			}
			base.On活性化();
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
				if( base.b初めての進行描画 )
				{
					this.nフラッシュ制御タイマ = (long)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
					this.nY座標制御タイマ = (long)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
					base.b初めての進行描画 = false;
				}
				long num = (long)(CSound管理.rc演奏用タイマ.n現在時刻 * (((double)TJAPlayer3.ConfigIni.n演奏速度) / 20.0));
				if ( num < this.nフラッシュ制御タイマ )
				{
					this.nフラッシュ制御タイマ = num;
				}
				while( ( num - this.nフラッシュ制御タイマ ) >= 15 )
				{
					for( int j = 0; j < 10; j++ )
					{
						if( this.stパッド状態[ j ].n明るさ > 0 )
						{
							this.stパッド状態[ j ].n明るさ--;
						}
					}
					this.nフラッシュ制御タイマ += 15;
				}
				long num3 = CSound管理.rc演奏用タイマ.n現在時刻;
				if( num3 < this.nY座標制御タイマ )
				{
					this.nY座標制御タイマ = num3;
				}
				while( ( num3 - this.nY座標制御タイマ ) >= 5 )
				{
					for( int k = 0; k < 10; k++ )
					{
						this.stパッド状態[ k ].nY座標オフセットdot += this.stパッド状態[ k ].nY座標加速度dot;
						if( this.stパッド状態[ k ].nY座標オフセットdot > 15 )
						{
							this.stパッド状態[ k ].nY座標オフセットdot = 15;
							this.stパッド状態[ k ].nY座標加速度dot = -1;
						}
						else if( this.stパッド状態[ k ].nY座標オフセットdot < 0 )
						{
							this.stパッド状態[ k ].nY座標オフセットdot = 0;
							this.stパッド状態[ k ].nY座標加速度dot = 0;
						}
					}
					this.nY座標制御タイマ += 5;
				}


			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		[StructLayout( LayoutKind.Sequential )]
		private struct STパッド状態
		{
			public int n明るさ;
			public int nY座標オフセットdot;
			public int nY座標加速度dot;
		}
		[StructLayout( LayoutKind.Sequential )]
		private struct ST基本位置
		{
			public int x;
			public int y;
			public Rectangle rc;
		}

		private long nY座標制御タイマ;
		private long nフラッシュ制御タイマ;
        private readonly int[] n描画順 = new int[] { 9, 3, 2, 6, 5, 4, 8, 7, 1, 0 };
                                                  // LP BD SD FT HT LT RD CY HH LC
		private STパッド状態[] stパッド状態 = new STパッド状態[ 10 ];
		private readonly ST基本位置[] st基本位置;
		//-----------------
		#endregion
	}
}
