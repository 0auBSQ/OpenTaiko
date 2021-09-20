using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
	internal class CActSelectInformation : CActivity
	{
		// コンストラクタ

		public CActSelectInformation()
		{
			base.b活性化してない = true;
		}


		// CActivity 実装

		public override void On活性化()
		{
			this.n画像Index上 = -1;
			this.n画像Index下 = 0;

            this.bFirst = true;
            this.ct進行用 = new CCounter( 0, 3000, 3, TJAPlayer3.Timer );
			base.On活性化();
		}
		public override void On非活性化()
		{
			this.ctスクロール用 = null;
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
                this.txInfo_Back = TJAPlayer3.tテクスチャの生成( CSkin.Path( @"Graphics\5_information_BG.png" ) );
                this.txInfo[ 0 ] = TJAPlayer3.tテクスチャの生成( CSkin.Path( @"Graphics\5_information.png" ) );
                this.txInfo[ 1 ] = TJAPlayer3.tテクスチャの生成( CSkin.Path( @"Graphics\5_information2.png" ) );
				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if( !base.b活性化してない )
			{
				TJAPlayer3.tテクスチャの解放( ref this.txInfo_Back );
				TJAPlayer3.tテクスチャの解放( ref this.txInfo[ 0 ] );
				TJAPlayer3.tテクスチャの解放( ref this.txInfo[ 1 ] );
				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			if( !base.b活性化してない )
			{
				if( base.b初めての進行描画 )
				{
					base.b初めての進行描画 = false;
				}

                if( this.txInfo_Back != null )
                    this.txInfo_Back.t2D描画( TJAPlayer3.app.Device, 340, 600 );


				this.ct進行用.t進行Loop();
                if( this.bFirst )
                {
                    this.ct進行用.n現在の値 = 300;
                }

                #region[ 透明度制御 ]
                if( this.txInfo[ 0 ] != null && this.txInfo[ 1 ] != null )
                {
                    if( this.ct進行用.n現在の値 < 255 )
                    {
                        this.txInfo[ 0 ].Opacity = this.ct進行用.n現在の値;
                        this.txInfo[ 1 ].Opacity = 255 - this.ct進行用.n現在の値;
                    }
                    else if( this.ct進行用.n現在の値 >= 255 && this.ct進行用.n現在の値 < 1245 )
                    {
                        this.bFirst = false;
                        this.txInfo[ 0 ].Opacity = 255;
                        this.txInfo[ 1 ].Opacity = 0;
                    }
                    else if( this.ct進行用.n現在の値 >= 1245 && this.ct進行用.n現在の値 < 1500 )
                    {
                        this.txInfo[ 0 ].Opacity = 255 - ( this.ct進行用.n現在の値 - 1245 );
                        this.txInfo[ 1 ].Opacity = this.ct進行用.n現在の値 - 1245;
                    }
                    else if( this.ct進行用.n現在の値 >= 1500 && this.ct進行用.n現在の値 <= 3000 )
                    {
                        this.txInfo[ 0 ].Opacity = 0;
                        this.txInfo[ 1 ].Opacity = 255;
                    }

                    this.txInfo[ 0 ].t2D描画( TJAPlayer3.app.Device, 340, 600 );
                    this.txInfo[ 1 ].t2D描画( TJAPlayer3.app.Device, 340, 600 );
                }

                #endregion


			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		[StructLayout( LayoutKind.Sequential )]
		private struct STINFO
		{
			public int nTexture番号;
			public Point pt左上座標;
			public STINFO( int nTexture番号, int x, int y )
			{
				this.nTexture番号 = nTexture番号;
				this.pt左上座標 = new Point( x, y );
			}
		}

		private CCounter ctスクロール用;
		private int n画像Index下;
		private int n画像Index上;
		private readonly STINFO[] stInfo = new STINFO[] {
			new STINFO( 0, 0, 0 ),
			new STINFO( 0, 0, 49 ),
			new STINFO( 0, 0, 97 ),
			new STINFO( 0, 0, 147 ),
			new STINFO( 0, 0, 196 ),
			new STINFO( 1, 0, 0 ),
			new STINFO( 1, 0, 49 ),
			new STINFO( 1, 0, 97 ),
			new STINFO( 1, 0, 147 )
		};
        private CTexture txInfo_Back;
		private CTexture[] txInfo = new CTexture[ 2 ];
        private bool bFirst;
        private CCounter ct進行用;
		//-----------------
		#endregion
	}
}
