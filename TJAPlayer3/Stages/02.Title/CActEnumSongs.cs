using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Globalization;
using System.Runtime.Serialization.Formatters.Binary;
using SlimDX;
using SlimDX.Direct3D9;
using FDK;
using SampleFramework;

namespace TJAPlayer3
{
	internal class CActEnumSongs :  CActivity
	{
		public bool bコマンドでの曲データ取得;


		/// <summary>
		/// Constructor
		/// </summary>
		public CActEnumSongs()
		{
			Init( false );
		}

		public CActEnumSongs( bool _bコマンドでの曲データ取得 )
		{
			Init( _bコマンドでの曲データ取得 );
		}
		private void Init( bool _bコマンドでの曲データ取得 )
		{
			base.b活性化してない = true;
			bコマンドでの曲データ取得 = _bコマンドでの曲データ取得;
		}

		// CActivity 実装

		public override void On活性化()
		{
			if ( this.b活性化してる )
				return;
			base.On活性化();

			try
			{
				this.ctNowEnumeratingSongs = new CCounter();	// 0, 1000, 17, CDTXMania.Timer );
				this.ctNowEnumeratingSongs.t開始( 0, 100, 17, TJAPlayer3.Timer );
			}
			finally
			{
			}
		}
		public override void On非活性化()
		{
			if ( this.b活性化してない )
				return;
			base.On非活性化();
			this.ctNowEnumeratingSongs = null;
		}
		public override void OnManagedリソースの作成()
		{
			if ( this.b活性化してない )
				return;
			//string pathNowEnumeratingSongs = CSkin.Path( @"Graphics\ScreenTitle NowEnumeratingSongs.png" );
			//if ( File.Exists( pathNowEnumeratingSongs ) )
			//{
			//	this.txNowEnumeratingSongs = CDTXMania.tテクスチャの生成( pathNowEnumeratingSongs, false );
			//}
			//else
			//{
			//	this.txNowEnumeratingSongs = null;
			//}
			//string pathDialogNowEnumeratingSongs = CSkin.Path( @"Graphics\ScreenConfig NowEnumeratingSongs.png" );
			//if ( File.Exists( pathDialogNowEnumeratingSongs ) )
			//{
			//	this.txDialogNowEnumeratingSongs = CDTXMania.tテクスチャの生成( pathDialogNowEnumeratingSongs, false );
			//}
			//else
			//{
			//	this.txDialogNowEnumeratingSongs = null;
			//}

			try
			{
				System.Drawing.Font ftMessage = new System.Drawing.Font("MS UI Gothic", 40f, FontStyle.Bold, GraphicsUnit.Pixel );
				string[] strMessage = 
				{
					"     曲データの一覧を\n       取得しています。\n   しばらくお待ちください。",
					" Now enumerating songs.\n         Please wait..."
				};
				int ci = ( CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja" ) ? 0 : 1;
				if ( ( strMessage != null ) && ( strMessage.Length > 0 ) )
				{
					Bitmap image = new Bitmap( 1, 1 );
					Graphics graphics = Graphics.FromImage( image );
					SizeF ef = graphics.MeasureString( strMessage[ ci ], ftMessage );
					Size size = new Size( (int) Math.Ceiling( (double) ef.Width ), (int) Math.Ceiling( (double) ef.Height ) );
					graphics.Dispose();
					image.Dispose();
					image = new Bitmap( size.Width, size.Height );
					graphics = Graphics.FromImage( image );
					graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
					graphics.DrawString( strMessage[ ci ], ftMessage, Brushes.White, (float) 0f, (float) 0f );
					graphics.Dispose();
					this.txMessage = new CTexture( TJAPlayer3.app.Device, image, TJAPlayer3.TextureFormat );
					this.txMessage.vc拡大縮小倍率 = new Vector3( 0.5f, 0.5f, 1f );
					image.Dispose();
					TJAPlayer3.t安全にDisposeする( ref ftMessage );
				}
				else
				{
					this.txMessage = null;
				}
			}
			catch ( CTextureCreateFailedException e )
			{
				Trace.TraceError( "テクスチャの生成に失敗しました。(txMessage)" );
				Trace.TraceError( e.ToString() );
				Trace.TraceError( "例外が発生しましたが処理を継続します。 (761b726d-d27c-470d-be0b-a702971601b5)" );
				this.txMessage = null;
			}
	
			base.OnManagedリソースの作成();
		}
		public override void OnManagedリソースの解放()
		{
			if ( this.b活性化してない )
				return;

			//CDTXMania.t安全にDisposeする( ref this.txDialogNowEnumeratingSongs );
			//CDTXMania.t安全にDisposeする( ref this.txNowEnumeratingSongs );
			TJAPlayer3.t安全にDisposeする( ref this.txMessage );
			base.OnManagedリソースの解放();
		}

		public override int On進行描画()
		{
			if ( this.b活性化してない )
			{
				return 0;
			}
			this.ctNowEnumeratingSongs.t進行Loop();
			if ( TJAPlayer3.Tx.Enum_Song != null )
			{
                TJAPlayer3.Tx.Enum_Song.Opacity = (int) ( 176.0 + 80.0 * Math.Sin( (double) (2 * Math.PI * this.ctNowEnumeratingSongs.n現在の値 * 2 / 100.0 ) ) );
                TJAPlayer3.Tx.Enum_Song.t2D描画( TJAPlayer3.app.Device, 18, 7 );
			}
			if ( bコマンドでの曲データ取得 && TJAPlayer3.Tx.Config_Enum_Song != null )
			{
                TJAPlayer3.Tx.Config_Enum_Song.t2D描画( TJAPlayer3.app.Device, 180, 177 );
				this.txMessage.t2D描画( TJAPlayer3.app.Device, 190, 197 );
			}

			return 0;
		}


		private CCounter ctNowEnumeratingSongs;
		//private CTexture txNowEnumeratingSongs = null;
		//private CTexture txDialogNowEnumeratingSongs = null;
		private CTexture txMessage;
	}
}
