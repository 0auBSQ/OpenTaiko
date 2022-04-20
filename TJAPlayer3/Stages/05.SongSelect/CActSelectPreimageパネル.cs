using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D9;
using FDK;

using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;

namespace TJAPlayer3
{
	internal class CActSelectPreimageパネル : CActivity
	{
		// メソッド

		public CActSelectPreimageパネル()
		{
			base.b活性化してない = true;
		}
		public void t選択曲が変更された()
		{
			this.ct遅延表示 = new CCounter( -TJAPlayer3.ConfigIni.n曲が選択されてからプレビュー画像が表示開始されるまでのウェイトms, 100, 1, TJAPlayer3.Timer );
			this.b新しいプレビューファイルを読み込んだ = false;
		}

		public bool bIsPlayingPremovie		// #27060
		{
			get
			{
				return (this.avi != null);
			}
		}

		// CActivity 実装

		public override void On活性化()
		{
			this.n本体X = 8;
			this.n本体Y = 0x39;
			this.r表示するプレビュー画像 = this.txプレビュー画像がないときの画像;
			this.str現在のファイル名 = "";
			this.b新しいプレビューファイルを読み込んだ = false;
			base.On活性化();
		}
		public override void On非活性化()
		{
			this.ct登場アニメ用 = null;
			this.ct遅延表示 = null;
			if( this.avi != null )
			{
				this.avi.Dispose();
				this.avi = null;
			}
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
				this.txパネル本体 = TJAPlayer3.tテクスチャの生成( CSkin.Path( @"Graphics\5_preimage panel.png" ), false );
				this.txセンサ = TJAPlayer3.tテクスチャの生成( CSkin.Path( @"Graphics\5_sensor.png" ), false );
				//this.txセンサ光 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\5_sensor light.png" ), false );
				this.txプレビュー画像 = null;
				this.txプレビュー画像がないときの画像 = TJAPlayer3.tテクスチャの生成( CSkin.Path( @"Graphics\5_preimage default.png" ), false );
				this.sfAVI画像 = Surface.CreateOffscreenPlain( TJAPlayer3.app.Device, 0xcc, 0x10d, TJAPlayer3.app.GraphicsDeviceManager.CurrentSettings.BackBufferFormat, Pool.SystemMemory );
				this.nAVI再生開始時刻 = -1;
				this.n前回描画したフレーム番号 = -1;
				this.b動画フレームを作成した = false;
				this.pAVIBmp = IntPtr.Zero;
				this.tプレビュー画像_動画の変更();
				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if( !base.b活性化してない )
			{
				TJAPlayer3.tテクスチャの解放( ref this.txパネル本体 );
				TJAPlayer3.tテクスチャの解放( ref this.txセンサ );
				TJAPlayer3.tテクスチャの解放( ref this.txセンサ光 );
				TJAPlayer3.tテクスチャの解放( ref this.txプレビュー画像 );
				TJAPlayer3.tテクスチャの解放( ref this.txプレビュー画像がないときの画像 );
				if( this.sfAVI画像 != null )
				{
					this.sfAVI画像.Dispose();
					this.sfAVI画像 = null;
				}
				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			if( !base.b活性化してない )
			{
				if( base.b初めての進行描画 )
				{
					this.ct登場アニメ用 = new CCounter( 0, 100, 5, TJAPlayer3.Timer );
					this.ctセンサ光 = new CCounter( 0, 100, 30, TJAPlayer3.Timer );
					this.ctセンサ光.n現在の値 = 70;
					base.b初めての進行描画 = false;
				}
				this.ct登場アニメ用.t進行();
				this.ctセンサ光.t進行Loop();
				if( ( !TJAPlayer3.stage選曲.bスクロール中 && ( this.ct遅延表示 != null ) ) && this.ct遅延表示.b進行中 )
				{
					this.ct遅延表示.t進行();
					if ( ( this.ct遅延表示.n現在の値 >= 0 ) && this.b新しいプレビューファイルをまだ読み込んでいない )
					{
						this.tプレビュー画像_動画の変更();
						TJAPlayer3.Timer.t更新();
						this.ct遅延表示.n現在の経過時間ms = TJAPlayer3.Timer.n現在時刻;
						this.b新しいプレビューファイルを読み込んだ = true;
					}
					else if ( this.ct遅延表示.b終了値に達した && this.ct遅延表示.b進行中 )
					{
						this.ct遅延表示.t停止();
					}
				}
				else if( ( ( this.avi != null ) && ( this.sfAVI画像 != null ) ) && ( this.nAVI再生開始時刻 != -1 ) )
				{
					int time = (int) ( ( TJAPlayer3.Timer.n現在時刻 - this.nAVI再生開始時刻 ) * ( ( (double) TJAPlayer3.ConfigIni.n演奏速度 ) / 20.0 ) );
					int frameNoFromTime = this.avi.GetFrameNoFromTime( time );
					if( frameNoFromTime >= this.avi.GetMaxFrameCount() )
					{
						this.nAVI再生開始時刻 = TJAPlayer3.Timer.n現在時刻;
					}
					else if( ( this.n前回描画したフレーム番号 != frameNoFromTime ) && !this.b動画フレームを作成した )
					{
						this.b動画フレームを作成した = true;
						this.n前回描画したフレーム番号 = frameNoFromTime;
						this.pAVIBmp = this.avi.GetFramePtr( frameNoFromTime );
					}
				}
				this.t描画処理_パネル本体();
				//this.t描画処理_ジャンル文字列();
				this.t描画処理_プレビュー画像();
				//this.t描画処理_センサ光();
				//this.t描画処理_センサ本体();
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		private CAvi avi;
		private bool b動画フレームを作成した;
		private CCounter ctセンサ光;
		private CCounter ct遅延表示;
		private CCounter ct登場アニメ用;
		private long nAVI再生開始時刻;
		private int n前回描画したフレーム番号;
		private int n本体X;
		private int n本体Y;
		private IntPtr pAVIBmp;
		private readonly Rectangle rcセンサ光 = new Rectangle( 0, 0xc0, 0x40, 0x40 );
		private readonly Rectangle rcセンサ本体下半分 = new Rectangle( 0x40, 0, 0x40, 0x80 );
		private readonly Rectangle rcセンサ本体上半分 = new Rectangle( 0, 0, 0x40, 0x80 );
		private CTexture r表示するプレビュー画像;
		private Surface sfAVI画像;
		private string str現在のファイル名;
		private CTexture txセンサ;
		private CTexture txセンサ光;
		private CTexture txパネル本体;
		private CTexture txプレビュー画像;
		private CTexture txプレビュー画像がないときの画像;
		private bool b新しいプレビューファイルを読み込んだ;
		private bool b新しいプレビューファイルをまだ読み込んでいない
		{
			get
			{
				return !this.b新しいプレビューファイルを読み込んだ;
			}
			set
			{
				this.b新しいプレビューファイルを読み込んだ = !value;
			}
		}

		private unsafe void tサーフェイスをクリアする( Surface sf )
		{
			DataRectangle rectangle = sf.LockRectangle( LockFlags.None );
			DataStream data = new DataStream(rectangle.DataPointer, sf.Description.Width * rectangle.Pitch, true, false);
			switch ( ( rectangle.Pitch / sf.Description.Width ) )
			{
				case 4:
					{
						uint* numPtr = (uint*) data.DataPointer.ToPointer();
						for( int i = 0; i < sf.Description.Height; i++ )
						{
							for( int j = 0; j < sf.Description.Width; j++ )
							{
								( numPtr + ( i * sf.Description.Width ) )[ j ] = 0;
							}
						}
						break;
					}
				case 2:
					{
						ushort* numPtr2 = (ushort*) data.DataPointer.ToPointer();
						for( int k = 0; k < sf.Description.Height; k++ )
						{
							for( int m = 0; m < sf.Description.Width; m++ )
							{
								( numPtr2 + ( k * sf.Description.Width ) )[ m ] = 0;
							}
						}
						break;
					}
			}
			sf.UnlockRectangle();
		}
		private void tプレビュー画像_動画の変更()
		{
			if( this.avi != null )
			{
				this.avi.Dispose();
				this.avi = null;
			}
			this.pAVIBmp = IntPtr.Zero;
			this.nAVI再生開始時刻 = -1;
			if( !TJAPlayer3.ConfigIni.bストイックモード )
			{
				if( this.tプレビュー動画の指定があれば構築する() )
				{
					return;
				}
				if( this.tプレビュー画像の指定があれば構築する() )
				{
					return;
				}
				if( this.t背景画像があればその一部からプレビュー画像を構築する() )
				{
					return;
				}
			}
			this.r表示するプレビュー画像 = this.txプレビュー画像がないときの画像;
			this.str現在のファイル名 = "";
		}
		private bool tプレビュー画像の指定があれば構築する()
		{
			Cスコア cスコア = TJAPlayer3.stage選曲.r現在選択中のスコア;
			if( ( cスコア == null ) || string.IsNullOrEmpty( cスコア.譜面情報.Preimage ) )
			{
				return false;
			}
			string str = cスコア.ファイル情報.フォルダの絶対パス + cスコア.譜面情報.Preimage;
			if( !str.Equals( this.str現在のファイル名 ) )
			{
				TJAPlayer3.tテクスチャの解放( ref this.txプレビュー画像 );
				this.str現在のファイル名 = str;
				if( !File.Exists( this.str現在のファイル名 ) )
				{
					Trace.TraceWarning( "ファイルが存在しません。({0})", new object[] { this.str現在のファイル名 } );
					return false;
				}
				this.txプレビュー画像 = TJAPlayer3.tテクスチャの生成( this.str現在のファイル名, false );
				if( this.txプレビュー画像 != null )
				{
					this.r表示するプレビュー画像 = this.txプレビュー画像;
				}
				else
				{
					this.r表示するプレビュー画像 = this.txプレビュー画像がないときの画像;
				}
			}
			return true;
		}
		private bool tプレビュー動画の指定があれば構築する()
		{
			Cスコア cスコア = TJAPlayer3.stage選曲.r現在選択中のスコア;
			if( ( TJAPlayer3.ConfigIni.bAVI有効 && ( cスコア != null ) ) && !string.IsNullOrEmpty( cスコア.譜面情報.Premovie ) )
			{
				string filename = cスコア.ファイル情報.フォルダの絶対パス + cスコア.譜面情報.Premovie;
				if( filename.Equals( this.str現在のファイル名 ) )
				{
					return true;
				}
				if( this.avi != null )
				{
					this.avi.Dispose();
					this.avi = null;
				}
				this.str現在のファイル名 = filename;
				if( !File.Exists( this.str現在のファイル名 ) )
				{
					Trace.TraceWarning( "ファイルが存在しません。({0})", new object[] { this.str現在のファイル名 } );
					return false;
				}
				try
				{
					this.avi = new CAvi( filename );
					this.nAVI再生開始時刻 = TJAPlayer3.Timer.n現在時刻;
					this.n前回描画したフレーム番号 = -1;
					this.b動画フレームを作成した = false;
					this.tサーフェイスをクリアする( this.sfAVI画像 );
					Trace.TraceInformation( "動画を生成しました。({0})", new object[] { filename } );
				}
				catch (Exception e)
				{
					Trace.TraceError( e.ToString() );
					Trace.TraceError( "動画の生成に失敗しました。({0})", new object[] { filename } );
					this.avi = null;
					this.nAVI再生開始時刻 = -1;
				}
			}
			return false;
		}
		private bool t背景画像があればその一部からプレビュー画像を構築する()
		{
			Cスコア cスコア = TJAPlayer3.stage選曲.r現在選択中のスコア;
			if( ( cスコア == null ) || string.IsNullOrEmpty( cスコア.譜面情報.Backgound ) )
			{
				return false;
			}
			string path = cスコア.ファイル情報.フォルダの絶対パス + cスコア.譜面情報.Backgound;
			if( !path.Equals( this.str現在のファイル名 ) )
			{
				if( !File.Exists( path ) )
				{
					Trace.TraceWarning( "ファイルが存在しません。({0})", new object[] { path } );
					return false;
				}
				TJAPlayer3.tテクスチャの解放( ref this.txプレビュー画像 );
				this.str現在のファイル名 = path;
				Bitmap image = null;
				Bitmap bitmap2 = null;
				Bitmap bitmap3 = null;
				try
				{
					image = new Bitmap( this.str現在のファイル名 );
					bitmap2 = new Bitmap(SampleFramework.GameWindowSize.Width, SampleFramework.GameWindowSize.Height);
					Graphics graphics = Graphics.FromImage( bitmap2 );
					int x = 0;
					for (int i = 0; i < SampleFramework.GameWindowSize.Height; i += image.Height)
					{
						for (x = 0; x < SampleFramework.GameWindowSize.Width; x += image.Width)
						{
							graphics.DrawImage( image, x, i, image.Width, image.Height );
						}
					}
					graphics.Dispose();
					bitmap3 = new Bitmap( 0xcc, 0x10d );
					graphics = Graphics.FromImage( bitmap3 );
					graphics.DrawImage( bitmap2, 5, 5, new Rectangle( 0x157, 0x6d, 0xcc, 0x10d ), GraphicsUnit.Pixel );
					graphics.Dispose();
					this.txプレビュー画像 = new CTexture( TJAPlayer3.app.Device, bitmap3, TJAPlayer3.TextureFormat );
					this.r表示するプレビュー画像 = this.txプレビュー画像;
				}
				catch (Exception e)
				{
					Trace.TraceError( e.ToString() );
					Trace.TraceError( "背景画像の読み込みに失敗しました。({0})", new object[] { this.str現在のファイル名 } );
					this.r表示するプレビュー画像 = this.txプレビュー画像がないときの画像;
					return false;
				}
				finally
				{
					if( image != null )
					{
						image.Dispose();
					}
					if( bitmap2 != null )
					{
						bitmap2.Dispose();
					}
					if( bitmap3 != null )
					{
						bitmap3.Dispose();
					}
				}
			}
			return true;
		}
        /// <summary>
        /// 一時的に使用禁止。
        /// </summary>
		private void t描画処理_ジャンル文字列()
		{
			C曲リストノード c曲リストノード = TJAPlayer3.stage選曲.r現在選択中の曲;
			Cスコア cスコア = TJAPlayer3.stage選曲.r現在選択中のスコア;
			if( ( c曲リストノード != null ) && ( cスコア != null ) )
			{
				string str = "";
				switch( c曲リストノード.eノード種別 )
				{
					case C曲リストノード.Eノード種別.SCORE:
						if( ( c曲リストノード.strジャンル == null ) || ( c曲リストノード.strジャンル.Length <= 0 ) )
						{
							if( ( cスコア.譜面情報.ジャンル != null ) && ( cスコア.譜面情報.ジャンル.Length > 0 ) )
							{
								str = cスコア.譜面情報.ジャンル;
							}
#if false	// #32644 2013.12.21 yyagi "Unknown"なジャンル表示を削除。DTX/BMSなどの種別表示もしない。
							else
							{
								switch( cスコア.譜面情報.曲種別 )
								{
									case CDTX.E種別.DTX:
										str = "DTX";
										break;

									case CDTX.E種別.GDA:
										str = "GDA";
										break;

									case CDTX.E種別.G2D:
										str = "G2D";
										break;

									case CDTX.E種別.BMS:
										str = "BMS";
										break;

									case CDTX.E種別.BME:
										str = "BME";
										break;
								}
								str = "Unknown";
							}
#endif
							break;
						}
						str = c曲リストノード.strジャンル;
						break;

					case C曲リストノード.Eノード種別.SCORE_MIDI:
						str = "MIDI";
						break;

					case C曲リストノード.Eノード種別.BOX:
						str = "MusicBox";
						break;

					case C曲リストノード.Eノード種別.BACKBOX:
						str = "BackBox";
						break;

					case C曲リストノード.Eノード種別.RANDOM:
						str = "Random";
						break;

					default:
						str = "Unknown";
						break;
				}
				TJAPlayer3.act文字コンソール.tPrint( this.n本体X + 0x12, this.n本体Y - 1, C文字コンソール.Eフォント種別.赤細, str );
			}
		}
		private void t描画処理_センサ光()
		{
			int num = (int)this.ctセンサ光.n現在の値;
			if( num < 12 )
			{
				int x = this.n本体X + 0xcc;
				int y = this.n本体Y + 0x7b;
				if( this.txセンサ光 != null )
				{
					this.txセンサ光.vc拡大縮小倍率 = new Vector3( 1f, 1f, 1f );
					this.txセンサ光.Opacity = 0xff;
					this.txセンサ光.t2D描画( TJAPlayer3.app.Device, x, y, new Rectangle( ( num % 4 ) * 0x40, ( num / 4 ) * 0x40, 0x40, 0x40 ) );
				}
			}
			else if( num < 0x18 )
			{
				int num4 = num - 11;
				double num5 = ( (double) num4 ) / 11.0;
				double num6 = 1.0 + ( num5 * 0.5 );
				int num7 = (int) ( 64.0 * num6 );
				int num8 = (int) ( 64.0 * num6 );
				int num9 = ( ( this.n本体X + 0xcc ) + 0x20 ) - ( num7 / 2 );
				int num10 = ( ( this.n本体Y + 0x7b ) + 0x20 ) - ( num8 / 2 );
				if( this.txセンサ光 != null )
				{
					this.txセンサ光.vc拡大縮小倍率 = new Vector3( (float) num6, (float) num6, 1f );
					this.txセンサ光.Opacity = (int) ( 255.0 * ( 1.0 - num5 ) );
					this.txセンサ光.t2D描画( TJAPlayer3.app.Device, num9, num10, this.rcセンサ光 );
				}
			}
		}
		private void t描画処理_センサ本体()
		{
			int x = this.n本体X + 0xcd;
			int y = this.n本体Y - 4;
			if( this.txセンサ != null )
			{
				this.txセンサ.t2D描画( TJAPlayer3.app.Device, x, y, this.rcセンサ本体上半分 );
				y += 0x80;
				this.txセンサ.t2D描画( TJAPlayer3.app.Device, x, y, this.rcセンサ本体下半分 );
			}
		}
		private void t描画処理_パネル本体()
		{
			if( this.ct登場アニメ用.b終了値に達した || ( this.txパネル本体 != null ) )
			{
				this.n本体X = 16;
				this.n本体Y = 86;
			}
			else
			{
				double num = ( (double) this.ct登場アニメ用.n現在の値 ) / 100.0;
				double num2 = Math.Cos( ( 1.5 + ( 0.5 * num ) ) * Math.PI );
				this.n本体X = 8;
				if (this.txパネル本体 != null)
					this.n本体Y = 0x39 - ( (int) ( this.txパネル本体.sz画像サイズ.Height * ( 1.0 - ( num2 * num2 ) ) ) );
				else
					this.n本体Y = 8;
			}
			if( this.txパネル本体 != null )
			{
				this.txパネル本体.t2D描画( TJAPlayer3.app.Device, this.n本体X, this.n本体Y );
			}
		}
		private unsafe void t描画処理_プレビュー画像()
		{
			if( !TJAPlayer3.stage選曲.bスクロール中 && ( ( ( this.ct遅延表示 != null ) && ( this.ct遅延表示.n現在の値 > 0 ) ) && !this.b新しいプレビューファイルをまだ読み込んでいない ) )
			{
				int x = this.n本体X + 0x12;
				int y = this.n本体Y + 0x10;
				float num3 = ( (float) this.ct遅延表示.n現在の値 ) / 100f;
				float num4 = 0.9f + ( 0.1f * num3 );
				if( ( this.nAVI再生開始時刻 != -1 ) && ( this.sfAVI画像 != null ) )
				{
					if( this.b動画フレームを作成した && ( this.pAVIBmp != IntPtr.Zero ) )
					{
						DataRectangle rectangle = this.sfAVI画像.LockRectangle( LockFlags.None );
						DataStream data = new DataStream(rectangle.DataPointer, this.sfAVI画像.Description.Width * rectangle.Pitch, true, false); ;
						int num5 = rectangle.Pitch / this.sfAVI画像.Description.Width;
						BitmapUtil.BITMAPINFOHEADER* pBITMAPINFOHEADER = (BitmapUtil.BITMAPINFOHEADER*) this.pAVIBmp.ToPointer();
						if( pBITMAPINFOHEADER->biBitCount == 0x18 )
						{
							switch( num5 )
							{
								case 2:
									this.avi.tBitmap24ToGraphicsStreamR5G6B5( pBITMAPINFOHEADER, data, this.sfAVI画像.Description.Width, this.sfAVI画像.Description.Height );
									break;

								case 4:
									this.avi.tBitmap24ToGraphicsStreamX8R8G8B8( pBITMAPINFOHEADER, data, this.sfAVI画像.Description.Width, this.sfAVI画像.Description.Height );
									break;
							}
						}
						this.sfAVI画像.UnlockRectangle();
						this.b動画フレームを作成した = false;
					}
					using( Surface surface = TJAPlayer3.app.Device.GetBackBuffer( 0, 0 ) )
					{
						try
						{
							TJAPlayer3.app.Device.UpdateSurface( this.sfAVI画像, new SharpDX.Rectangle( 0, 0, this.sfAVI画像.Description.Width, this.sfAVI画像.Description.Height ), surface, new SharpDX.Point( x, y ) );
						}
						catch( Exception e )	// #32335 2013.10.26 yyagi: codecがないと、D3DERR_INVALIDCALLが発生する場合がある
						{
							Trace.TraceError( "codecがないと、D3DERR_INVALIDCALLが発生する場合がある" );
							Trace.TraceError( e.ToString() );
							Trace.TraceError( "例外が発生しましたが処理を継続します。 (ba21ae56-afaa-47b9-a5c7-1a6bb21085eb)" );
						}
						return;
					}
				}
				if( this.r表示するプレビュー画像 != null )
				{
					/*
					int width = this.r表示するプレビュー画像.sz画像サイズ.Width;
					int height = this.r表示するプレビュー画像.sz画像サイズ.Height;
					if( width > 400 )
					{
						width = 400;
					}
					if( height > 400 )
					{
						height = 400;
					}
					*/

					// Placeholder
					int width = 200;
					int height = 200;

					float xRatio = width / (float)this.r表示するプレビュー画像.sz画像サイズ.Width;
					float yRatio = height / (float)this.r表示するプレビュー画像.sz画像サイズ.Height;

					x += ( 400 - ( (int) ( width * num4 ) ) ) / 2;
					y += ( 400 - ( (int) ( height * num4 ) ) ) / 2;

					this.r表示するプレビュー画像.Opacity = (int) ( 255f * num3 );
					this.r表示するプレビュー画像.vc拡大縮小倍率.X = num4 * xRatio;
					this.r表示するプレビュー画像.vc拡大縮小倍率.Y = num4 * xRatio;

					// this.r表示するプレビュー画像.t2D描画( TJAPlayer3.app.Device, x + 22, y + 12, new Rectangle( 0, 0, width, height ) );

					// Temporary addition
					this.r表示するプレビュー画像.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 120, 110);
				}
			}
		}
		//-----------------
		#endregion
	}
}
