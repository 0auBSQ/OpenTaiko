using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using SharpDX;
using SharpDX.Direct3D9;
using SharpDX.Multimedia;
using DirectShowLib;

namespace FDK
{
	/// <summary>
	/// <para>DirectShowを用いたクリップ（動画＋音声）を扱う。</para>
	/// <para>１つのクリップにつき１つの CDirectShow インスタンスを生成する。</para>
	/// <para>再生の開始や停止などの操作の他、任意の時点でスナップイメージを取得することができる。</para>
	/// </summary>
	public class CDirectShow : IDisposable
	{
		// プロパティ

		public const uint WM_DSGRAPHNOTIFY = CWin32.WM_APP + 1;

		public enum Eグラフの状態 { 完全停止中, 再生のみ停止中, 再生中, 完全停止へ遷移中, 再生のみ停止へ遷移中, 再生へ遷移中, 未定 }
		public Eグラフの状態 eグラフの状態
		{
			get
			{
				var status = Eグラフの状態.未定;

				if( this.MediaCtrl != null )
				{
					FilterState fs;
					int hr = this.MediaCtrl.GetState( 0, out fs );		// それなりに重たいので注意。

					if( hr == CWin32.E_FAIL )
					{
						#region [ 失敗。]
						//-----------------
						status = Eグラフの状態.未定;
						//-----------------
						#endregion
					}
					else if( hr == CWin32.VFW_S_STATE_INTERMEDIATE )
					{
						#region [ 遷移中。]
						//-----------------
						switch( fs )
						{
							case FilterState.Running:
								status = Eグラフの状態.再生へ遷移中;
								break;

							case FilterState.Paused:
								status = Eグラフの状態.再生のみ停止へ遷移中;
								break;

							case FilterState.Stopped:
								status = Eグラフの状態.完全停止へ遷移中;
								break;

							default:
								status = Eグラフの状態.未定;
								break;
						}
						//-----------------
						#endregion
					}
					else
					{
						#region [ 安定状態。]
						//-----------------
						switch( fs )
						{
							case FilterState.Running:
								status = Eグラフの状態.再生中;
								break;

							case FilterState.Paused:
								status = Eグラフの状態.再生のみ停止中;
								break;

							case FilterState.Stopped:
								status = Eグラフの状態.完全停止中;
								break;

							default:
								status = Eグラフの状態.未定;
								break;
						}
						//-----------------
						#endregion
					}
				}
				return status;
			}
		}
		public bool b再生中;
		public bool bループ再生;

		public int n幅px
		{
			get;
			protected set;
		}
		public int n高さpx
		{
			get;
			protected set;
		}
		public int nスキャンライン幅byte
		{
			get;
			protected set;
		}
		public int nデータサイズbyte
		{
			get;
			protected set;
		}
		public bool b上下反転
		{
			get;
			protected set;
		}

		public bool b音声のみ
		{
			get;
			protected set;
		}
		
		public long n現在のグラフの再生位置ms
		{
			get
			{
				if( this.MediaSeeking == null )
					return 0;

				long current;
				int hr = this.MediaSeeking.GetCurrentPosition( out current );
				DsError.ThrowExceptionForHR( hr );
				return (long) ( current / ( 1000.0 * 10.0 ) );
			}
		}
		/// <summary>
		/// <para>無音:0～100:原音。set のみ。</para>
		/// </summary>
		public int n音量
		{
			get
			{
				return this._n音量;
			}
			set
			{
				if( this.BasicAudio == null )
					return;


				// 値を保存。

				this._n音量 = value;


				// リニア音量をデシベル音量に変換。

				int n音量db = 0;

				if( value == 0 )
				{
					n音量db = -10000;	// 完全無音
				}
				else
				{
					n音量db = (int) ( ( 20.0 * Math.Log10( ( (double) value ) / 100.0 ) ) * 100.0 );
				}


				// デシベル音量でグラフの音量を変更。

				this.BasicAudio.put_Volume( n音量db );
			}
		}
		/// <summary>
		/// <para>左:-100～中央:0～100:右。set のみ。</para>
		/// </summary>
		public int n位置
		{
			set
			{
				if( this.BasicAudio == null )
					return;

				// リニア位置をデシベル位置に変換。

				int n位置 = Math.Min( Math.Max( value, -100 ), +100 );
				int n位置db = 0;

				if( n位置 == 0 )
				{
					n位置db = 0;
				}
				else if( n位置 == -100 )
				{
					n位置db = -10000;
				}
				else if( n位置 == 100 )
				{
					n位置db = +10000;
				}
				else if( n位置 < 0 )
				{
					n位置db = (int) ( ( 20.0 * Math.Log10( ( (double) ( n位置 + 100 ) ) / 100.0 ) ) * 100.0 );
				}
				else
				{
					n位置db = (int) ( ( -20.0 * Math.Log10( ( (double) ( 100 - n位置 ) ) / 100.0 ) ) * 100.0 );
				}

				// デシベル位置でグラフの位置を変更。

				this.BasicAudio.put_Balance( n位置db );
			}
		}
		public IMediaControl MediaCtrl;
		public IMediaEventEx MediaEventEx;
		public IMediaSeeking MediaSeeking;
		public IBasicAudio BasicAudio;
		public IGraphBuilder graphBuilder;

		/// <summary>
		/// <para>CDirectShowインスタンスに固有のID。</para>
		/// <para>DirectShow イベントをウィンドウに発信する際、MessageID として "WM_APP+インスタンスID" を発信する。</para>
		/// <para>これにより、受け側でイベント発信インスタンスを特定することが可能になる。</para>
		/// </summary>
		public int nインスタンスID
		{
			get;
			protected set;
		}


		// メソッド

		public CDirectShow()
		{
		}
		public CDirectShow( string fileName, IntPtr hWnd, bool bオーディオレンダラなし )
		{
			// 初期化。

			this.n幅px = 0;
			this.n高さpx = 0;
			this.b上下反転 = false;
			this.nスキャンライン幅byte = 0;
			this.nデータサイズbyte = 0;
			this.b音声のみ = false;
			this.graphBuilder = null;
			this.MediaCtrl = null;
			this.b再生中 = false;
			this.bループ再生 = false;


			// 静的リストに登録し、インスタンスIDを得る。

			CDirectShow.tインスタンスを登録する( this );


			// 並列処理準備。

			if( CDirectShow.n並列度 == 0 )	// 算出がまだなら算出する。
				CDirectShow.n並列度 = Environment.ProcessorCount;	// 並列度＝CPU数とする。

			unsafe
			{
				this.dgライン描画ARGB32 = new DGライン描画[ CDirectShow.n並列度 ];
				this.dgライン描画XRGB32 = new DGライン描画[ CDirectShow.n並列度 ];

				for( int i = 0; i < CDirectShow.n並列度; i++ )
				{
					this.dgライン描画ARGB32[ i ] = new DGライン描画( this.tライン描画ARGB32 );
					this.dgライン描画XRGB32[ i ] = new DGライン描画( this.tライン描画XRGB32 );
				}
			}

			try
			{
				int hr = 0;


				// グラフビルダを生成。

				this.graphBuilder = (IGraphBuilder) new FilterGraph();
#if DEBUG
				// ROT への登録。
				this.rot = new DsROTEntry( graphBuilder );
#endif


				// QueryInterface。存在しなければ null。

				this.MediaCtrl = this.graphBuilder as IMediaControl;
				this.MediaEventEx = this.graphBuilder as IMediaEventEx;
				this.MediaSeeking = this.graphBuilder as IMediaSeeking;
				this.BasicAudio = this.graphBuilder as IBasicAudio;


				// IMemoryRenderer をグラフに挿入。

				AMMediaType mediaType = null;

				this.memoryRendererObject = new MemoryRenderer();
				this.memoryRenderer = (IMemoryRenderer) this.memoryRendererObject;
				var baseFilter = (IBaseFilter) this.memoryRendererObject;

				hr = this.graphBuilder.AddFilter( baseFilter, "MemoryRenderer" );
				DsError.ThrowExceptionForHR( hr );


				// fileName からグラフを自動生成。

				hr = this.graphBuilder.RenderFile( fileName, null );	// IMediaControl.RenderFile() は推奨されない
				DsError.ThrowExceptionForHR( hr );


				// 音声のみ？

				{
					IBaseFilter videoRenderer;
					IPin videoInputPin;
                    IBaseFilter audioRenderer;
                    IPin audioInputPin;
#if MemoryRenderer
                    CDirectShow.tビデオレンダラとその入力ピンを探して返す( this.graphBuilder, out videoRenderer, out videoInputPin );
#else
                    CDirectShow.SearchMMRenderers( this.graphBuilder, out videoRenderer, out videoInputPin, out audioRenderer, out audioInputPin );
#endif
					if( videoRenderer == null && audioRenderer != null )
						this.b音声のみ = true;
					else
					{
						C共通.tCOMオブジェクトを解放する( ref videoInputPin );
						C共通.tCOMオブジェクトを解放する( ref videoRenderer );
                        C共通.tCOMオブジェクトを解放する( ref audioInputPin );
                        C共通.tCOMオブジェクトを解放する( ref audioRenderer );
					}
				}


				// イメージ情報を取得。

				if( !this.b音声のみ )
				{
					long n;
					int m;
					this.memoryRenderer.GetWidth( out n );
					this.n幅px = (int) n;
					this.memoryRenderer.GetHeight( out n );
					this.n高さpx = (int) n;
					this.memoryRenderer.IsBottomUp( out m );
					this.b上下反転 = ( m != 0 );
					this.memoryRenderer.GetBufferSize( out n );
					this.nデータサイズbyte = (int) n;
					this.nスキャンライン幅byte = (int) this.nデータサイズbyte / this.n高さpx;
					// C共通.tCOMオブジェクトを解放する( ref baseFilter );		なんかキャスト元のオブジェクトまで解放されるので解放禁止。
				}


				// グラフを修正する。

				if( bオーディオレンダラなし )
				{
					WaveFormat dummy1;
					byte[] dummy2;
					CDirectShow.tオーディオレンダラをNullレンダラに変えてフォーマットを取得する( this.graphBuilder, out dummy1, out dummy2 );
				}


				// その他の処理。

				this.t再生準備開始();	// 1回以上 IMediaControl を呼び出してないと、IReferenceClock は取得できない。
				this.t遷移完了まで待って状態を取得する();	// 完全に Pause へ遷移するのを待つ。（環境依存）


				// イベント用ウィンドウハンドルを設定。

				this.MediaEventEx.SetNotifyWindow( hWnd, (int) WM_DSGRAPHNOTIFY, new IntPtr( this.nインスタンスID ) );
			}
#if !DEBUG
			catch( Exception e )
			{
				C共通.t例外の詳細をログに出力する( e );
				this.Dispose();
				throw;	// 例外発出。
			}
#endif
			finally
			{
			}
		}

		public void t再生準備開始()
		{
			if( this.MediaCtrl != null )
			{
				int hr = this.MediaCtrl.Pause();		// 再生準備を開始する。ここでは準備が完了するまで待たない。
				DsError.ThrowExceptionForHR( hr );
			}
		}
		public void t再生開始()
		{
			if( this.MediaCtrl != null && --this.n再生一時停止呼び出しの累積回数 <= 0 )
			{
				//this.t遷移完了まで待って状態を取得する();		// 再生準備（だろう）がまだ完了してなければ、待つ。	→ 意外と重い処理なので外部で判断して実行するよう変更する。(2011.8.7)

				int hr = this.MediaCtrl.Run();					// 再生開始。
				DsError.ThrowExceptionForHR( hr );

				this.n再生一時停止呼び出しの累積回数 = 0;		// 一時停止回数はここでリセットされる。
				this.b再生中 = true;
			}
		}
		public void t再生一時停止()
		{
			if( this.MediaCtrl != null && this.n再生一時停止呼び出しの累積回数 == 0 )
			{
				int hr = this.MediaCtrl.Pause();
				DsError.ThrowExceptionForHR( hr );
			}
			this.n再生一時停止呼び出しの累積回数++;
			this.b再生中 = false;
		}
		public void t再生停止()
		{
			if( this.MediaCtrl != null )
			{
				int hr = this.MediaCtrl.Stop();
				DsError.ThrowExceptionForHR( hr );
			}

			// 次への準備。
			//this.t再生位置を変更する( 0.0 );		→ より細かく制御するために、FDK外部で制御するように変更。(2011.8.7)
			//this.t再生準備開始();

			this.n再生一時停止呼び出しの累積回数 = 0;	// 停止すると、一時停止呼び出し累積回数はリセットされる。
			this.b再生中 = false;
		}
		public void t再生位置を変更( double db再生位置ms )
		{
			if( this.MediaSeeking == null )
				return;

			int hr = this.MediaSeeking.SetPositions(
				DsLong.FromInt64( (long) ( db再生位置ms * 1000.0 * 10.0 ) ),
				AMSeekingSeekingFlags.AbsolutePositioning,
				null,
				AMSeekingSeekingFlags.NoPositioning );

			DsError.ThrowExceptionForHR( hr );
		}
		public void t最初から再生開始()
		{
			this.t再生位置を変更( 0.0 );
			this.t再生開始();
		}
		public Eグラフの状態 t遷移完了まで待って状態を取得する()
		{
			var status = Eグラフの状態.未定;

			if( this.MediaCtrl != null )
			{
				FilterState fs;
				int hr = this.MediaCtrl.GetState( 1000, out fs );	// 遷移完了まで最大1000ms待つ。
			}
			return this.eグラフの状態;
		}
		public unsafe void t現時点における最新のスナップイメージをTextureに転写する( CTexture texture )
		{
			int hr;

			#region [ 再生してないなら何もせず帰還。（一時停止中はOK。）]
			//-----------------
			if( !this.b再生中 )
				return;
			//-----------------
			#endregion
			#region [ 音声のみなら何もしない。]
			//-----------------
			if( this.b音声のみ )
				return;
			//-----------------
			#endregion

			DataRectangle dr = texture.texture.LockRectangle( 0, LockFlags.Discard );
			try
			{
				if( this.nスキャンライン幅byte == dr.Pitch )
				{
					#region [ (A) ピッチが合うので、テクスチャに直接転送する。]
					//-----------------
					hr = this.memoryRenderer.GetCurrentBuffer( dr.DataPointer, this.nデータサイズbyte );
					DsError.ThrowExceptionForHR( hr );
					//-----------------
					#endregion
				}
				else
				{
					this.b上下反転 = false;		// こちらの方法では常に正常

					#region [ (B) ピッチが合わないので、メモリに転送してからテクスチャに転送する。]
					//-----------------

					#region [ IMemoryRenderer からバッファにイメージデータを読み込む。]
					//-----------------
					if( this.ip == IntPtr.Zero )
						this.ip = Marshal.AllocCoTaskMem( this.nデータサイズbyte );

					hr = this.memoryRenderer.GetCurrentBuffer( this.ip, this.nデータサイズbyte );
					DsError.ThrowExceptionForHR( hr );
					//-----------------
					#endregion

					#region [ テクスチャにスナップイメージを転送。]
					//-----------------
					bool bARGB32 = true;

					switch( texture.Format )
					{
						case Format.A8R8G8B8:
							bARGB32 = true;
							break;

						case Format.X8R8G8B8:
							bARGB32 = false;
							break;

						default:
							return;		// 未対応のフォーマットは無視。
					}

					// スレッドプールを使って並列転送する準備。

					this.ptrSnap = (byte*) this.ip.ToPointer();
					var ptr = stackalloc UInt32*[ CDirectShow.n並列度 ];	// stackalloc（GC対象外、メソッド終了時に自動開放）は、スタック変数相手にしか使えない。
					ptr[ 0 ] = (UInt32*) dr.DataPointer.ToPointer();	//		↓
					for( int i = 1; i < CDirectShow.n並列度; i++ )			// スタック変数で確保、初期化して…
						ptr[ i ] = ptr[ i - 1 ] + this.n幅px;				//		↓
					this.ptrTexture = ptr;									// スタック変数をクラスメンバに渡す（これならOK）。


					// 並列度が１ならシングルスレッド、２以上ならマルチスレッドで転送する。
					// → CPUが１つの場合、わざわざスレッドプールのスレッドで処理するのは無駄。

					if( CDirectShow.n並列度 == 1 )
					{
						if( bARGB32 )
							this.tライン描画ARGB32( 0 );
						else
							this.tライン描画XRGB32( 0 );
					}
					else
					{
						// 転送開始。

						var ar = new IAsyncResult[ CDirectShow.n並列度 ];
						for( int i = 0; i < CDirectShow.n並列度; i++ )
						{
							ar[ i ] = ( bARGB32 ) ?
								this.dgライン描画ARGB32[ i ].BeginInvoke( i, null, null ) :
								this.dgライン描画XRGB32[ i ].BeginInvoke( i, null, null );
						}


						// 転送完了待ち。

						for( int i = 0; i < CDirectShow.n並列度; i++ )
						{
							if( bARGB32 )
								this.dgライン描画ARGB32[ i ].EndInvoke( ar[ i ] );
							else
								this.dgライン描画XRGB32[ i ].EndInvoke( ar[ i ] );
						}
					}

					this.ptrSnap = null;
					this.ptrTexture = null;
					//-----------------
					#endregion

					//-----------------
					#endregion
				}
			}
			finally
			{
				texture.texture.UnlockRectangle( 0 );
			}
		}

		private IntPtr ip = IntPtr.Zero;

		public static void tグラフを解析しデバッグ出力する( IGraphBuilder graphBuilder )
		{
			if( graphBuilder == null )
			{
				Debug.WriteLine( "指定されたグラフが null です。" );
				return;
			}

			int hr = 0;

			IEnumFilters eFilters;
			hr = graphBuilder.EnumFilters( out eFilters );
			DsError.ThrowExceptionForHR( hr );
			{
				var filters = new IBaseFilter[ 1 ];
				while( eFilters.Next( 1, filters, IntPtr.Zero ) == CWin32.S_OK )
				{
					FilterInfo filterInfo;
					hr = filters[ 0 ].QueryFilterInfo( out filterInfo );
					DsError.ThrowExceptionForHR( hr );
					{
						Debug.WriteLine( filterInfo.achName );		// フィルタ名表示。
						if( filterInfo.pGraph != null )
							C共通.tCOMオブジェクトを解放する( ref filterInfo.pGraph );
					}

					IEnumPins ePins;
					hr = filters[ 0 ].EnumPins( out ePins );
					DsError.ThrowExceptionForHR( hr );
					{
						var pins = new IPin[ 1 ];
						while( ePins.Next( 1, pins, IntPtr.Zero ) == CWin32.S_OK )
						{
							PinInfo pinInfo;
							hr = pins[ 0 ].QueryPinInfo( out pinInfo );
							DsError.ThrowExceptionForHR( hr );
							{
								Debug.Write( "  " + pinInfo.name );	// ピン名表示。
								Debug.Write( ( pinInfo.dir == PinDirection.Input ) ? " ← " : " → " );

								IPin connectPin;
								hr = pins[ 0 ].ConnectedTo( out connectPin );
								if( hr != CWin32.S_OK )
									Debug.WriteLine( "(未接続)" );
								else
								{
									DsError.ThrowExceptionForHR( hr );

									PinInfo connectPinInfo;
									hr = connectPin.QueryPinInfo( out connectPinInfo );
									DsError.ThrowExceptionForHR( hr );
									{
										FilterInfo connectFilterInfo;
										hr = connectPinInfo.filter.QueryFilterInfo( out connectFilterInfo );
										DsError.ThrowExceptionForHR( hr );
										{
											Debug.Write( "[" + connectFilterInfo.achName + "]." );	// 接続先フィルタ名

											if( connectFilterInfo.pGraph != null )
												C共通.tCOMオブジェクトを解放する( ref connectFilterInfo.pGraph );
										}

										Debug.WriteLine( connectPinInfo.name );		// 接続先ピン名
										if( connectPinInfo.filter != null )
											C共通.tCOMオブジェクトを解放する( ref connectPinInfo.filter );
										DsUtils.FreePinInfo( connectPinInfo );
									}
									C共通.tCOMオブジェクトを解放する( ref connectPin );
								}
								if( pinInfo.filter != null )
									C共通.tCOMオブジェクトを解放する( ref pinInfo.filter );
								DsUtils.FreePinInfo( pinInfo );
							}
							C共通.tCOMオブジェクトを解放する( ref pins[ 0 ] );
						}
					}
					C共通.tCOMオブジェクトを解放する( ref ePins );

					C共通.tCOMオブジェクトを解放する( ref filters[ 0 ] );
				}
			}
			C共通.tCOMオブジェクトを解放する( ref eFilters );

			Debug.Flush();
		}
		public static void tオーディオレンダラをNullレンダラに変えてフォーマットを取得する( IGraphBuilder graphBuilder, out WaveFormat wfx, out byte[] wfx拡張データ )
		{
			int hr = 0;

			IBaseFilter audioRenderer = null;
			IPin rendererInputPin = null;
			IPin rendererConnectedOutputPin = null;
			IBaseFilter nullRenderer = null;
			IPin nullRendererInputPin = null;
			wfx = null;
			wfx拡張データ = new byte[ 0 ];

			try
			{
				// audioRenderer を探す。

				audioRenderer = CDirectShow.tオーディオレンダラを探して返す( graphBuilder );
				if( audioRenderer == null )
					return;		// なかった

				#region [ 音量ゼロで一度再生する。（オーディオレンダラの入力ピンMediaTypeが、接続時とは異なる「正しいもの」に変わる可能性があるため。）]
				//-----------------
				{
					// ここに来た時点で、グラフのビデオレンダラは無効化（NullRendererへの置換や除去など）しておくこと。
					// さもないと、StopWhenReady() 時に一瞬だけ Activeウィンドウが表示されてしまう。

					var mediaCtrl = (IMediaControl) graphBuilder;
					var basicAudio = (IBasicAudio) graphBuilder;
					
					basicAudio.put_Volume( -10000 );	// 最小音量
					

					// グラフを再生してすぐ止める。（Paused → Stopped へ遷移する）
					
					mediaCtrl.StopWhenReady();

		
					// グラフが Stopped に遷移完了するまで待つ。（StopWhenReady() はグラフが Stopped になるのを待たずに帰ってくる。）

					FilterState fs = FilterState.Paused;
					hr = CWin32.S_FALSE;
					while( fs != FilterState.Stopped || hr != CWin32.S_OK )
						hr = mediaCtrl.GetState( 10, out fs );
					

					// 終了処理。

					basicAudio.put_Volume( 0 );			// 最大音量
					
					basicAudio = null;
					mediaCtrl = null;
				}
				//-----------------
				#endregion

				// audioRenderer の入力ピンを探す。

				rendererInputPin = t最初の入力ピンを探して返す( audioRenderer );
				if( rendererInputPin == null )
					return;


				// WAVEフォーマットを取得し、wfx 引数へ格納する。

				var type = new AMMediaType();
				hr = rendererInputPin.ConnectionMediaType( type );
				DsError.ThrowExceptionForHR( hr );
				try
				{
					wfx = new WaveFormat();

					#region [ type.formatPtr から wfx に、拡張領域を除くデータをコピーする。]
					//-----------------
					var wfxTemp = new WaveFormatEx();	// SlimDX.Multimedia.WaveFormat は Marshal.PtrToStructure() で使えないので、それが使える DirectShowLib.WaveFormatEx を介して取得する。（面倒…）
					Marshal.PtrToStructure( type.formatPtr, (object) wfxTemp );
					wfx = WaveFormat.CreateCustomFormat((WaveFormatEncoding)wfxTemp.wFormatTag, wfxTemp.nSamplesPerSec, wfxTemp.nChannels, wfxTemp.nAvgBytesPerSec, wfxTemp.nBlockAlign, wfxTemp.wBitsPerSample);
					//-----------------
					#endregion
					#region [ 拡張領域が存在するならそれを wfx拡張データ に格納する。 ]
					//-----------------
					int nWaveFormatEx本体サイズ = 16 + 2; // sizeof( WAVEFORMAT ) + sizof( WAVEFORMATEX.cbSize )
					int nはみ出しサイズbyte = type.formatSize - nWaveFormatEx本体サイズ;

					if( nはみ出しサイズbyte > 0 )
					{
						wfx拡張データ = new byte[ nはみ出しサイズbyte ];
						var hGC = GCHandle.Alloc( wfx拡張データ, GCHandleType.Pinned );	// 動くなよー
						unsafe
						{
							byte* src = (byte*) type.formatPtr.ToPointer();
							byte* dst = (byte*) hGC.AddrOfPinnedObject().ToPointer();
							CWin32.CopyMemory( dst, src + nWaveFormatEx本体サイズ, (uint) nはみ出しサイズbyte );
						}
						hGC.Free();
					}
					//-----------------
					#endregion
				}
				finally
				{
					if( type != null )
						DsUtils.FreeAMMediaType( type );
				}


				// audioRenderer につながる出力ピンを探す。

				hr = rendererInputPin.ConnectedTo( out rendererConnectedOutputPin );
				DsError.ThrowExceptionForHR( hr );


				// audioRenderer をグラフから切断する。

				rendererInputPin.Disconnect();
				rendererConnectedOutputPin.Disconnect();


				// audioRenderer をグラフから除去する。

				hr = graphBuilder.RemoveFilter( audioRenderer );
				DsError.ThrowExceptionForHR( hr );


				// nullRenderer を作成し、グラフに追加する。

				nullRenderer = (IBaseFilter) new NullRenderer();
				hr = graphBuilder.AddFilter( nullRenderer, "Audio Null Renderer" );
				DsError.ThrowExceptionForHR( hr );


				// nullRenderer の入力ピンを探す。

				hr = nullRenderer.FindPin( "In", out nullRendererInputPin );
				DsError.ThrowExceptionForHR( hr );


				// nullRenderer をグラフに接続する。

				hr = rendererConnectedOutputPin.Connect( nullRendererInputPin, null );
				DsError.ThrowExceptionForHR( hr );
			}
			finally
			{
				C共通.tCOMオブジェクトを解放する( ref nullRendererInputPin );
				C共通.tCOMオブジェクトを解放する( ref nullRenderer );
				C共通.tCOMオブジェクトを解放する( ref rendererConnectedOutputPin );
				C共通.tCOMオブジェクトを解放する( ref rendererInputPin );
				C共通.tCOMオブジェクトを解放する( ref audioRenderer );
			}
		}
		public static void ConnectNullRendererFromSampleGrabber(IGraphBuilder graphBuilder, IBaseFilter sampleGrabber)
		{
			int hr = 0;
			IBaseFilter videoRenderer = null;
			IPin videoRendererInputPin = null;
			IBaseFilter audioRenderer = null;
			IPin audioRendererInputPin = null;
			IPin connectedOutputPin = null;
			IPin nullRendererInputPin = null;
			IPin grabberOutputPin = null;
			IPin grabberOutputConnectedPin = null;

			try
			{
				// videoRenderer を探す。
				CDirectShow.SearchMMRenderers(graphBuilder, out videoRenderer, out videoRendererInputPin, out audioRenderer, out audioRendererInputPin);
				if (videoRenderer != null && videoRendererInputPin != null)
				{
					// 既存のレンダラにつながっているピン対を取得
					hr = videoRendererInputPin.ConnectedTo(out connectedOutputPin);
					DsError.ThrowExceptionForHR(hr);

					// それらを切断。前段の出力ピンとビデオレンダラの入力ピンを切断する。双方向から切断しないとグラフから切り離されないので注意。
					hr = videoRendererInputPin.Disconnect();
					DsError.ThrowExceptionForHR(hr);
					hr = connectedOutputPin.Disconnect();
					DsError.ThrowExceptionForHR(hr);

					// ビデオレンダラをグラフから除去し、ヌルレンダラを追加
					hr = graphBuilder.RemoveFilter(videoRenderer);
					DsError.ThrowExceptionForHR(hr);
					IBaseFilter nullRenderer = new NullRenderer() as IBaseFilter;
					hr = graphBuilder.AddFilter(nullRenderer, "Video Null Renderer");
					DsError.ThrowExceptionForHR(hr);

					// nullRenderer の入力ピンを探す。
					hr = nullRenderer.FindPin("In", out nullRendererInputPin);
					DsError.ThrowExceptionForHR(hr);
					hr = nullRendererInputPin.Disconnect();
					DsError.ThrowExceptionForHR(hr);

					// グラバの Out と Null Renderer の In を接続する。
					hr = sampleGrabber.FindPin("Out", out grabberOutputPin);
					DsError.ThrowExceptionForHR(hr);
					hr = grabberOutputPin.ConnectedTo(out grabberOutputConnectedPin);
					// grabberのoutに何もつながっていない場合(つまり、grabberのoutとrendererのinが直結している場合)は、
					// grabberのoutと、別のフィルタのinの間の切断処理を行わない。
					if (hr != CWin32.S_OK)
					{
						//Debug.WriteLine("grabber out: 未接続:");
					}
					else
					{
						hr = grabberOutputConnectedPin.Disconnect();
						DsError.ThrowExceptionForHR(hr);
						hr = grabberOutputPin.Disconnect();
						DsError.ThrowExceptionForHR(hr);
					}
					hr = grabberOutputPin.Connect(nullRendererInputPin, null);
					DsError.ThrowExceptionForHR(hr);
				}

				if ( audioRenderer != null && audioRendererInputPin != null )
				{
					C共通.tCOMオブジェクトを解放する(ref connectedOutputPin);

					// 既存のレンダラにつながっているピン対を取得
					hr = audioRendererInputPin.ConnectedTo(out connectedOutputPin);
					DsError.ThrowExceptionForHR(hr);

					// それらを切断。前段の出力ピンとビデオレンダラの入力ピンを切断する。双方向から切断しないとグラフから切り離されないので注意。
					hr = audioRendererInputPin.Disconnect();
					DsError.ThrowExceptionForHR(hr);
					hr = connectedOutputPin.Disconnect();
					DsError.ThrowExceptionForHR(hr);

					// ビデオレンダラをグラフから除去し、ヌルレンダラを追加
					hr = graphBuilder.RemoveFilter(audioRenderer);
					DsError.ThrowExceptionForHR(hr);
					IBaseFilter nullRenderer = new NullRenderer() as IBaseFilter;
					hr = graphBuilder.AddFilter(nullRenderer, "Audio Null Renderer");
					DsError.ThrowExceptionForHR(hr);

					C共通.tCOMオブジェクトを解放する(ref nullRendererInputPin);
					hr = nullRenderer.FindPin("In", out nullRendererInputPin);
					DsError.ThrowExceptionForHR(hr);
					hr = connectedOutputPin.Connect(nullRendererInputPin, null);
					DsError.ThrowExceptionForHR(hr);
				}
			}
			finally
			{
				C共通.tCOMオブジェクトを解放する(ref connectedOutputPin);
				C共通.tCOMオブジェクトを解放する(ref videoRendererInputPin);
				C共通.tCOMオブジェクトを解放する(ref videoRenderer);
				C共通.tCOMオブジェクトを解放する(ref audioRenderer);
				C共通.tCOMオブジェクトを解放する(ref audioRendererInputPin);
				C共通.tCOMオブジェクトを解放する(ref nullRendererInputPin);
				C共通.tCOMオブジェクトを解放する(ref grabberOutputPin);
				C共通.tCOMオブジェクトを解放する(ref grabberOutputConnectedPin);
			}
		}
		private static void SearchMMRenderers( IFilterGraph graph, out IBaseFilter videoRenderer, out IPin inputVPin, out IBaseFilter audioRenderer, out IPin inputAPin )
		{
			int hr = 0;
			string strVRフィルタ名 = null;
			string strVRピンID = null;
			string strARフィルタ名 = null;
			string strARピンID = null;

			// ビデオレンダラと入力ピンを探し、そのフィルタ名とピンIDを控える。

			IEnumFilters eFilters;
			hr = graph.EnumFilters( out eFilters );
			DsError.ThrowExceptionForHR( hr );
			try
			{
				var filters = new IBaseFilter[ 1 ];
				while( eFilters.Next( 1, filters, IntPtr.Zero ) == CWin32.S_OK )
				{
					try
					{
						#region [ 出力ピンがない（レンダラである）ことを確認する。]
						//-----------------
						IEnumPins ePins;
						bool b出力ピンがある = false;

						hr = filters[ 0 ].EnumPins( out ePins );
						DsError.ThrowExceptionForHR( hr );
						try
						{
							var pins = new IPin[ 1 ];
							while( ePins.Next( 1, pins, IntPtr.Zero ) == CWin32.S_OK )
							{
								try
								{
									if( b出力ピンがある )
										continue;

									PinDirection dir;
									hr = pins[ 0 ].QueryDirection( out dir );
									DsError.ThrowExceptionForHR( hr );
									if( dir == PinDirection.Output )
										b出力ピンがある = true;
								}
								finally
								{
									C共通.tCOMオブジェクトを解放する( ref pins[ 0 ] );
								}
							}
						}
						finally
						{
							C共通.tCOMオブジェクトを解放する( ref ePins );
						}

						if( b出力ピンがある )
							continue;	// 次のフィルタへ

						//-----------------
						#endregion
						#region [ 接続中の入力ピンが MEDIATYPE_Video に対応していたら、フィルタ名とピンIDを取得する。]
						//-----------------
						hr = filters[ 0 ].EnumPins( out ePins );
						DsError.ThrowExceptionForHR( hr );
						try
						{
							var pins = new IPin[ 1 ];
							while( ePins.Next( 1, pins, IntPtr.Zero ) == CWin32.S_OK )
							{
								try
								{
									if( !string.IsNullOrEmpty( strVRフィルタ名 ) )
										continue;

									var mediaType = new AMMediaType();

									#region [ 現在接続中の MediaType を取得。つながってなければ次のピンへ。]
									//-----------------
									hr = pins[ 0 ].ConnectionMediaType( mediaType );
									if( hr == CWin32.VFW_E_NOT_CONNECTED )
										continue;	// つながってない
									DsError.ThrowExceptionForHR( hr );
									//-----------------
									#endregion

									try
									{
										if( mediaType.majorType.Equals( MediaType.Video ) )
										{
											#region [ フィルタ名取得！]
											//-----------------
											FilterInfo filterInfo;
											hr = filters[ 0 ].QueryFilterInfo( out filterInfo );
											DsError.ThrowExceptionForHR( hr );
											strVRフィルタ名 = filterInfo.achName;
											C共通.tCOMオブジェクトを解放する( ref filterInfo.pGraph );
											//-----------------
											#endregion
											#region [ ピンID取得！]
											//-----------------
											hr = pins[ 0 ].QueryId( out strVRピンID );
											DsError.ThrowExceptionForHR( hr );
											//-----------------
											#endregion
										}
										else if( mediaType.majorType.Equals( MediaType.Audio ) )
										{
											FilterInfo filterInfo;
											hr = filters[0].QueryFilterInfo(out filterInfo);
											DsError.ThrowExceptionForHR(hr);
											strARフィルタ名 = filterInfo.achName;
											C共通.tCOMオブジェクトを解放する(ref filterInfo.pGraph);
											hr = pins[0].QueryId(out strARピンID);
											DsError.ThrowExceptionForHR(hr);
										}
									}
									finally
									{
										DsUtils.FreeAMMediaType( mediaType );
									}
								}
								finally
								{
									C共通.tCOMオブジェクトを解放する( ref pins[ 0 ] );
								}
							}
						}
						finally
						{
							C共通.tCOMオブジェクトを解放する( ref ePins );
						}

						//-----------------
						#endregion
					}
					finally
					{
						C共通.tCOMオブジェクトを解放する( ref filters[ 0 ] );
					}
				}
			}
			finally
			{
				C共通.tCOMオブジェクトを解放する( ref eFilters );
			}


			// 改めてフィルタ名とピンIDからこれらのインターフェースを取得し、戻り値として返す。

			videoRenderer = null;
			inputVPin = null;
			audioRenderer = null;
			inputAPin = null;

			if( !string.IsNullOrEmpty( strVRフィルタ名 ) )
			{
				hr = graph.FindFilterByName( strVRフィルタ名, out videoRenderer );
				DsError.ThrowExceptionForHR( hr );

				hr = videoRenderer.FindPin( strVRピンID, out inputVPin );
				DsError.ThrowExceptionForHR( hr );
			}

			if( !string.IsNullOrEmpty( strARフィルタ名 ) )
			{
				hr = graph.FindFilterByName(strARフィルタ名, out audioRenderer);
				DsError.ThrowExceptionForHR(hr);

				hr = audioRenderer.FindPin(strARピンID, out inputAPin);
				DsError.ThrowExceptionForHR(hr);
			}
		}

		public static void tビデオレンダラをグラフから除去する( IGraphBuilder graphBuilder )
		{
			int hr = 0;

			IBaseFilter videoRenderer = null;
			IPin renderInputPin = null;
			IPin connectedOutputPin = null;

			try
			{
				// videoRenderer を探す。
				
				CDirectShow.tビデオレンダラとその入力ピンを探して返す( graphBuilder, out videoRenderer, out renderInputPin );
				if( videoRenderer == null || renderInputPin == null )
					return;		// なかった

				#region [ renderInputPin へ接続している前段の出力ピン connectedOutputPin を探す。 ]
				//-----------------
				renderInputPin.ConnectedTo( out connectedOutputPin );
				//-----------------
				#endregion

				if( connectedOutputPin == null )
					return;		// なかった


				// 前段の出力ピンとビデオレンダラの入力ピンを切断する。双方向から切断しないとグラフから切り離されないので注意。

				renderInputPin.Disconnect();
				connectedOutputPin.Disconnect();


				// ビデオレンダラをグラフから除去。

				graphBuilder.RemoveFilter( videoRenderer );
			}
			finally
			{
				C共通.tCOMオブジェクトを解放する( ref connectedOutputPin );
				C共通.tCOMオブジェクトを解放する( ref renderInputPin );
				C共通.tCOMオブジェクトを解放する( ref videoRenderer );
			}
		}

		private static IPin t最初の入力ピンを探して返す( IBaseFilter baseFilter )
		{
			int hr = 0;

			IPin firstInputPin = null;

			IEnumPins ePins;
			hr = baseFilter.EnumPins( out ePins );
			DsError.ThrowExceptionForHR( hr );
			try
			{
				var pins = new IPin[ 1 ];
				while( ePins.Next( 1, pins, IntPtr.Zero ) == CWin32.S_OK )
				{
					PinInfo pinfo = new PinInfo() { filter = null };
					try
					{
						hr = pins[ 0 ].QueryPinInfo( out pinfo );
						DsError.ThrowExceptionForHR( hr );

						if( pinfo.dir == PinDirection.Input )
						{
							firstInputPin = pins[ 0 ];
							break;
						}
					}
					finally
					{
						if( pinfo.filter != null )
							C共通.tCOMオブジェクトを解放する( ref pinfo.filter );
						DsUtils.FreePinInfo( pinfo );

						if( firstInputPin == null )
							C共通.tCOMオブジェクトを解放する( ref pins[ 0 ] );
					}
				}
			}
			finally
			{
				C共通.tCOMオブジェクトを解放する( ref ePins );
			}

			return firstInputPin;
		}
		private static void tビデオレンダラとその入力ピンを探して返す( IFilterGraph graph, out IBaseFilter videoRenderer, out IPin inputPin )
		{
			int hr = 0;
			string strフィルタ名 = null;
			string strピンID = null;


			// ビデオレンダラと入力ピンを探し、そのフィルタ名とピンIDを控える。

			IEnumFilters eFilters;
			hr = graph.EnumFilters( out eFilters );
			DsError.ThrowExceptionForHR( hr );
			try
			{
				var filters = new IBaseFilter[ 1 ];
				while( eFilters.Next( 1, filters, IntPtr.Zero ) == CWin32.S_OK )
				{
					try
					{
						#region [ 出力ピンがない（レンダラである）ことを確認する。]
						//-----------------
						IEnumPins ePins;
						bool b出力ピンがある = false;

						hr = filters[ 0 ].EnumPins( out ePins );
						DsError.ThrowExceptionForHR( hr );
						try
						{
							var pins = new IPin[ 1 ];
							while( ePins.Next( 1, pins, IntPtr.Zero ) == CWin32.S_OK )
							{
								try
								{
									if( b出力ピンがある )
										continue;

									PinDirection dir;
									hr = pins[ 0 ].QueryDirection( out dir );
									DsError.ThrowExceptionForHR( hr );
									if( dir == PinDirection.Output )
										b出力ピンがある = true;
								}
								finally
								{
									C共通.tCOMオブジェクトを解放する( ref pins[ 0 ] );
								}
							}
						}
						finally
						{
							C共通.tCOMオブジェクトを解放する( ref ePins );
						}

						if( b出力ピンがある )
							continue;	// 次のフィルタへ

						//-----------------
						#endregion
						#region [ 接続中の入力ピンが MEDIATYPE_Video に対応していたら、フィルタ名とピンIDを取得する。]
						//-----------------
						hr = filters[ 0 ].EnumPins( out ePins );
						DsError.ThrowExceptionForHR( hr );
						try
						{
							var pins = new IPin[ 1 ];
							while( ePins.Next( 1, pins, IntPtr.Zero ) == CWin32.S_OK )
							{
								try
								{
									if( !string.IsNullOrEmpty( strフィルタ名 ) )
										continue;

									var mediaType = new AMMediaType();

									#region [ 現在接続中の MediaType を取得。つながってなければ次のピンへ。]
									//-----------------
									hr = pins[ 0 ].ConnectionMediaType( mediaType );
									if( hr == CWin32.VFW_E_NOT_CONNECTED )
										continue;	// つながってない
									DsError.ThrowExceptionForHR( hr );
									//-----------------
									#endregion

									try
									{
										if( mediaType.majorType.Equals( MediaType.Video ) )
										{
											#region [ フィルタ名取得！]
											//-----------------
											FilterInfo filterInfo;
											hr = filters[ 0 ].QueryFilterInfo( out filterInfo );
											DsError.ThrowExceptionForHR( hr );
											strフィルタ名 = filterInfo.achName;
											C共通.tCOMオブジェクトを解放する( ref filterInfo.pGraph );
											//-----------------
											#endregion
											#region [ ピンID取得！]
											//-----------------
											hr = pins[ 0 ].QueryId( out strピンID );
											DsError.ThrowExceptionForHR( hr );
											//-----------------
											#endregion

											continue;	// 次のピンへ。
										}
									}
									finally
									{
										DsUtils.FreeAMMediaType( mediaType );
									}
								}
								finally
								{
									C共通.tCOMオブジェクトを解放する( ref pins[ 0 ] );
								}
							}
						}
						finally
						{
							C共通.tCOMオブジェクトを解放する( ref ePins );
						}

						//-----------------
						#endregion
					}
					finally
					{
						C共通.tCOMオブジェクトを解放する( ref filters[ 0 ] );
					}
				}
			}
			finally
			{
				C共通.tCOMオブジェクトを解放する( ref eFilters );
			}


			// 改めてフィルタ名とピンIDからこれらのインターフェースを取得し、戻り値として返す。

			videoRenderer = null;
			inputPin = null;

			if( !string.IsNullOrEmpty( strフィルタ名 ) )
			{
				hr = graph.FindFilterByName( strフィルタ名, out videoRenderer );
				DsError.ThrowExceptionForHR( hr );

				hr = videoRenderer.FindPin( strピンID, out inputPin );
				DsError.ThrowExceptionForHR( hr );
			}
		}
		private static IBaseFilter tオーディオレンダラを探して返す( IFilterGraph graph )
		{
			int hr = 0;
			IBaseFilter audioRenderer = null;

			IEnumFilters eFilters;
			hr = graph.EnumFilters( out eFilters );
			DsError.ThrowExceptionForHR( hr );
			try
			{
				var filters = new IBaseFilter[ 1 ];
				while( eFilters.Next( 1, filters, IntPtr.Zero ) == CWin32.S_OK )
				{
					if( ( filters[ 0 ] as IAMAudioRendererStats ) != null )
					{
						audioRenderer = filters[ 0 ];
						break;
					}

					C共通.tCOMオブジェクトを解放する( ref filters[ 0 ] );
				}
			}
			finally
			{
				C共通.tCOMオブジェクトを解放する( ref eFilters );
			}
			return audioRenderer;
		}


		#region [ 静的インスタンス管理 ]
		//-----------------
		public const int nインスタンスIDの最大数 = 100;
		protected static Dictionary<int, CDirectShow> dicインスタンス = new Dictionary<int, CDirectShow>();	// <インスタンスID, そのIDを持つインスタンス>

		protected static void tインスタンスを登録する( CDirectShow ds )
		{
			for( int i = 1; i < CDirectShow.nインスタンスIDの最大数; i++ )
			{
				if( !CDirectShow.dicインスタンス.ContainsKey( i ) )		// 空いている番号を使う。
				{
					ds.nインスタンスID = i;
					CDirectShow.dicインスタンス.Add( i, ds );
					break;
				}
			}
		}
		protected static void tインスタンスを解放する( int nインスタンスID )
		{
			if( CDirectShow.dicインスタンス.ContainsKey( nインスタンスID ) )
				CDirectShow.dicインスタンス.Remove( nインスタンスID );
		}
		//-----------------
		#endregion

		#region [ Dispose-Finalize パターン実装 ]
		//-----------------
		public virtual void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );	// ちゃんと Dispose されたので、ファイナライズ不要であることを CLR に伝える。
		}
		protected virtual void Dispose( bool bManagedリソースも解放する )
		{
			if( bManagedリソースも解放する )
			{
				#region [ ROTから解放する。]
				//-----------------
#if DEBUG
					C共通.tDisposeする( ref this.rot );
#endif
				//-----------------
				#endregion
				
				CDirectShow.tインスタンスを解放する( this.nインスタンスID );
			}

			#region [ インターフェース参照をなくし、COMオブジェクトを解放する。 ]
			//-----------------
			if( this.ip != IntPtr.Zero )
			{
				Marshal.FreeCoTaskMem( this.ip );
				this.ip = IntPtr.Zero;
			}

			if( this.MediaCtrl != null )
			{
				this.MediaCtrl.Stop();
				this.MediaCtrl = null;
			}

			if( this.MediaEventEx != null )
			{
				this.MediaEventEx.SetNotifyWindow( IntPtr.Zero, 0, IntPtr.Zero );
				this.MediaEventEx = null;
			}

			if( this.MediaSeeking != null )
				this.MediaSeeking = null;

			if( this.BasicAudio != null )
				this.BasicAudio = null;

			C共通.tCOMオブジェクトを解放する( ref this.nullRenderer );
			C共通.tCOMオブジェクトを解放する( ref this.memoryRenderer );
			C共通.tCOMオブジェクトを解放する( ref this.memoryRendererObject );
			C共通.tCOMオブジェクトを解放する( ref this.graphBuilder );
			//-----------------
			#endregion

			C共通.t完全なガベージコレクションを実施する();
		}
        ~CDirectShow()
		{
			// ファイナライザが呼ばれたということは、Dispose() されなかったということ。
			// この場合、Managed リソースは先にファイナライズされている可能性があるので、Unmamaed リソースのみを解放する。
			
			this.Dispose( false );
        }
		//-----------------
		#endregion

		#region [ protected ]
		//-----------------
		protected MemoryRenderer memoryRendererObject = null;
		protected IMemoryRenderer memoryRenderer = null;
		protected IBaseFilter nullRenderer = null;
		protected int n再生一時停止呼び出しの累積回数 = 0;
		//-----------------
		#endregion

		#region [ private ]
		//-----------------
		private int _n音量 = 100;
#if DEBUG
		private DsROTEntry rot = null;
#endif

		// 可能な数のスレッドを使用して画像を転送する。大きい画像ほど有効。多すぎるとプール内のスレッドが空くまで待たされるので注意。
		private static int n並列度 = 0;	// 0 の場合、最初の生成時に並列度を決定する。

		private DGライン描画[] dgライン描画ARGB32;
		private DGライン描画[] dgライン描画XRGB32;
		private unsafe delegate void DGライン描画( int n );
		private unsafe byte* ptrSnap = null;
		private unsafe UInt32** ptrTexture = null;

		private unsafe void tライン描画XRGB32( int n )
		{
			// Snap は RGB32、Textureは X8R8G8B8

			UInt32* ptrTexture = this.ptrTexture[ n ];
			for( int y = n; y < this.n高さpx; y += CDirectShow.n並列度 )
			{
				byte* ptrPixel = ptrSnap + ( ( ( this.n高さpx - y ) - 1 ) * this.nスキャンライン幅byte );

				// アルファ無視なので一括コピー。CopyMemory() は自前でループ展開するよりも速い。
				CWin32.CopyMemory( (void*) ptrTexture, (void*) ptrPixel, (uint) ( this.n幅px * 4 ) );

				ptrTexture += this.n幅px * CDirectShow.n並列度;
			}
		}
		private unsafe void tライン描画ARGB32( int n )
		{
			// Snap は RGB32、Textureは A8R8G8B8

			UInt32* ptrTexture = this.ptrTexture[ n ];
			for( int y = n; y < this.n高さpx; y += CDirectShow.n並列度 )
			{
				UInt32* ptrPixel = (UInt32*) ( ptrSnap + ( ( ( this.n高さpx - y ) - 1 ) * this.nスキャンライン幅byte ) );

				//for( int x = 0; x < this.n幅px; x++ )
				//	*( ptrTexture + x ) = 0xFF000000 | *ptrPixel++;
				//			↓ループ展開により高速化。160fps の曲が 200fps まで上がった。

				if( this.n幅px == 0 ) goto LEXIT;
				UInt32* pt = ptrTexture;
				UInt32 nAlpha = 0xFF000000;
				int d = ( this.n幅px % 32 );

				switch( d )
				{
					case 1: goto L031;
					case 2: goto L030;
					case 3: goto L029;
					case 4: goto L028;
					case 5: goto L027;
					case 6: goto L026;
					case 7: goto L025;
					case 8: goto L024;
					case 9: goto L023;
					case 10: goto L022;
					case 11: goto L021;
					case 12: goto L020;
					case 13: goto L019;
					case 14: goto L018;
					case 15: goto L017;
					case 16: goto L016;
					case 17: goto L015;
					case 18: goto L014;
					case 19: goto L013;
					case 20: goto L012;
					case 21: goto L011;
					case 22: goto L010;
					case 23: goto L009;
					case 24: goto L008;
					case 25: goto L007;
					case 26: goto L006;
					case 27: goto L005;
					case 28: goto L004;
					case 29: goto L003;
					case 30: goto L002;
					case 31: goto L001;
				}

			L000: *pt++ = nAlpha | *ptrPixel++;
			L001: *pt++ = nAlpha | *ptrPixel++;
			L002: *pt++ = nAlpha | *ptrPixel++;
			L003: *pt++ = nAlpha | *ptrPixel++;
			L004: *pt++ = nAlpha | *ptrPixel++;
			L005: *pt++ = nAlpha | *ptrPixel++;
			L006: *pt++ = nAlpha | *ptrPixel++;
			L007: *pt++ = nAlpha | *ptrPixel++;
			L008: *pt++ = nAlpha | *ptrPixel++;
			L009: *pt++ = nAlpha | *ptrPixel++;
			L010: *pt++ = nAlpha | *ptrPixel++;
			L011: *pt++ = nAlpha | *ptrPixel++;
			L012: *pt++ = nAlpha | *ptrPixel++;
			L013: *pt++ = nAlpha | *ptrPixel++;
			L014: *pt++ = nAlpha | *ptrPixel++;
			L015: *pt++ = nAlpha | *ptrPixel++;
			L016: *pt++ = nAlpha | *ptrPixel++;
			L017: *pt++ = nAlpha | *ptrPixel++;
			L018: *pt++ = nAlpha | *ptrPixel++;
			L019: *pt++ = nAlpha | *ptrPixel++;
			L020: *pt++ = nAlpha | *ptrPixel++;
			L021: *pt++ = nAlpha | *ptrPixel++;
			L022: *pt++ = nAlpha | *ptrPixel++;
			L023: *pt++ = nAlpha | *ptrPixel++;
			L024: *pt++ = nAlpha | *ptrPixel++;
			L025: *pt++ = nAlpha | *ptrPixel++;
			L026: *pt++ = nAlpha | *ptrPixel++;
			L027: *pt++ = nAlpha | *ptrPixel++;
			L028: *pt++ = nAlpha | *ptrPixel++;
			L029: *pt++ = nAlpha | *ptrPixel++;
			L030: *pt++ = nAlpha | *ptrPixel++;
			L031: *pt++ = nAlpha | *ptrPixel++;
				if( ( pt - ptrTexture ) < this.n幅px ) goto L000;

			LEXIT:
				ptrTexture += this.n幅px * CDirectShow.n並列度;
			}
		}
		//-----------------
		#endregion
	}
}
