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
using Silk.NET.Maths;
using SkiaSharp;
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
			base.IsDeActivated = true;
			bコマンドでの曲データ取得 = _bコマンドでの曲データ取得;
		}

		// CActivity 実装

		public override void Activate()
		{
			if ( this.IsActivated )
				return;
			base.Activate();

			try
			{
				this.ctNowEnumeratingSongs = new CCounter();	// 0, 1000, 17, CDTXMania.Timer );
				this.ctNowEnumeratingSongs.Start( 0, 100, 17, TJAPlayer3.Timer );
			}
			finally
			{
			}
		}
		public override void DeActivate()
		{
			if ( this.IsDeActivated )
				return;
			base.DeActivate();
			this.ctNowEnumeratingSongs = null;
		}
		public override void CreateManagedResource()
		{
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
				CCachedFontRenderer ftMessage = new CCachedFontRenderer(CFontRenderer.DefaultFontName, 40, CCachedFontRenderer.FontStyle.Bold );
				string[] strMessage = 
				{
					"     曲データの一覧を\n       取得しています。\n   しばらくお待ちください。",
					" Now enumerating songs.\n         Please wait..."
				};
				int ci = ( CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja" ) ? 0 : 1;
				if ( ( strMessage != null ) && ( strMessage.Length > 0 ) )
				{
					SKBitmap image = ftMessage.DrawText(strMessage[ ci ], Color.White);
					this.txMessage = new CTexture( image );
					this.txMessage.vc拡大縮小倍率 = new Vector3D<float>( 0.5f, 0.5f, 1f );
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
	
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource()
		{
			//CDTXMania.t安全にDisposeする( ref this.txDialogNowEnumeratingSongs );
			//CDTXMania.t安全にDisposeする( ref this.txNowEnumeratingSongs );
			TJAPlayer3.t安全にDisposeする( ref this.txMessage );
			base.ReleaseManagedResource();
		}

		public override int Draw()
		{
			if ( this.IsDeActivated )
			{
				return 0;
			}
			this.ctNowEnumeratingSongs.TickLoop();
			if ( TJAPlayer3.Tx.Enum_Song != null )
			{
                TJAPlayer3.Tx.Enum_Song.Opacity = (int) ( 176.0 + 80.0 * Math.Sin( (double) (2 * Math.PI * this.ctNowEnumeratingSongs.CurrentValue * 2 / 100.0 ) ) );
                TJAPlayer3.Tx.Enum_Song.t2D描画( 18, 7 );
			}
			if ( bコマンドでの曲データ取得 && TJAPlayer3.Tx.Config_Enum_Song != null )
			{
                TJAPlayer3.Tx.Config_Enum_Song.t2D描画( 180, 177 );
				this.txMessage.t2D描画( 190, 197 );
			}

			return 0;
		}


		private CCounter ctNowEnumeratingSongs;
		//private CTexture txNowEnumeratingSongs = null;
		//private CTexture txDialogNowEnumeratingSongs = null;
		private CTexture txMessage;
	}
}
