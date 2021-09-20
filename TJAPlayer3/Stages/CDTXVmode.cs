using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using FDK;


namespace TJAPlayer3
{
	public class CDTXVmode
	{
		public enum ECommand
		{
			Stop,
			Play,
			Preview
		}

		/// <summary>
		/// DTXVモードかどうか
		/// </summary>
		public bool Enabled
		{
			get;
			set;
		}

		/// <summary>
		/// プレビューサウンドの再生が発生した
		/// </summary>
		public bool Preview
		{
			get;
			set;
		}

		/// <summary>
		/// 外部から再指示が発生したか
		/// </summary>
		public bool Refreshed
		{
			get;
			set;
		}

		/// <summary>
		/// 演奏開始小節番号
		/// </summary>
		public int nStartBar
		{
			get;
			set;
		}

		/// <summary>
		/// DTXファイルの再読み込みが必要かどうか
		/// </summary>
		public bool NeedReload
		{
			get;
			private set;
//			private set;	// 本来はprivate setにすべきだが、デバッグが簡単になるので、しばらくはprivateなしのままにする。
		}

		/// <summary>
		/// DTXCからのコマンド
		/// </summary>
		public ECommand Command
		{
			get;
			private set;
		}

		public ESoundDeviceType soundDeviceType
		{
			get;
			private set;
		}
		public int nASIOdevice
		{
			get;
			private set;
		}
		/// <summary>
		/// 前回からサウンドデバイスが変更されたか
		/// </summary>
		public bool ChangedSoundDevice
		{
			get;
			private set;
		}

		public string filename
		{
			get
			{
				return last_path;
			}
		}

		public string previewFilename
		{
			get;
			private set;
		}
		public int previewVolume
		{
			get;
			private set;
		}
		public int previewPan
		{
			get;
			private set;
		}
		public bool GRmode
		{
			get;
			private set;
		}
		public bool lastGRmode
		{
			get;
			private set;
		}
		public bool TimeStretch
		{
			get;
			private set;
		}
		public bool lastTimeStretch
		{
			get;
			private set;
		}
		public bool VSyncWait
		{
			get;
			private set;
		}
		public bool lastVSyncWait
		{
			get;
			private set;
		}


		/// <summary>
		/// コンストラクタ
		/// </summary>
		public CDTXVmode()
		{
			this.last_path = "";
			this.last_timestamp = DateTime.MinValue;
			this.Enabled = false;
			this.nStartBar = 0;
			this.Refreshed = false;
			this.NeedReload = false;
			this.previewFilename = "";
			this.GRmode = false;
			this.lastGRmode = false;
			this.TimeStretch = false;
			this.lastTimeStretch = false;
			this.VSyncWait = true;
			this.lastVSyncWait = true;
		}

		/// <summary>
		/// DTXファイルのリロードが必要かどうか判定する
		/// </summary>
		/// <param name="filename">DTXファイル名</param>
		/// <returns>再読込が必要ならtrue</returns>
		/// <remarks>プロパティNeedReloadにも結果が入る</remarks>
		/// <remarks>これを呼び出すたびに、Refreshedをtrueにする</remarks>
		/// <exception cref="FileNotFoundException"></exception>
		public bool bIsNeedReloadDTX( string filename )
		{
			if ( !File.Exists( filename ) )			// 指定したファイルが存在しないなら例外終了
			{
				Trace.TraceError( "ファイルが見つかりません。({0})", filename );
				throw new FileNotFoundException();
				//return false;
			}

			this.Refreshed = true;

			// 前回とファイル名が異なるか、タイムスタンプが更新されているか、
			// GRmode等の設定を変更したなら、DTX要更新
			DateTime current_timestamp = File.GetLastWriteTime( filename );
			if ( last_path != filename || current_timestamp > last_timestamp ||
				this.lastGRmode != this.GRmode || this.lastTimeStretch != this.TimeStretch || this.lastVSyncWait != this.VSyncWait )
			{
				this.last_path = filename;
				this.last_timestamp = current_timestamp;
				this.lastGRmode = this.GRmode;
				this.lastTimeStretch = this.TimeStretch;
				this.lastVSyncWait = this.VSyncWait;

				this.NeedReload = true;
				return true;
			}
			this.NeedReload = false;
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="arg"></param>
		/// <param name="nStartBar"></param>
		/// <param name="command"></param>
		/// <returns>DTXV用の引数であればtrue</returns>
		/// <remarks>内部でEnabled, nStartBar, Command, NeedReload, filename, last_path, last_timestampを設定する</remarks>
		public bool ParseArguments( string arg )
		{
			bool ret = false, analyzing = true;
			this.nStartBar = 0;

			if ( arg != null ) 
			{
				while ( analyzing )
				{
					if ( arg == "" )
					{
						analyzing = false;
					}
					else if ( arg.StartsWith( "-V", StringComparison.OrdinalIgnoreCase ) )		// サウンド再生
					{
						// -Vvvv,ppp,"filename"の形式。 vvv=volume, ppp=pan.
						this.Enabled = true;
						this.Command = ECommand.Preview;
						this.Refreshed = true;
						ret = true;
						arg = arg.Substring( 2 );

						int pVol = arg.IndexOf( ',' );									//Trace.TraceInformation( "pVol=" + pVol );
						string strVol = arg.Substring( 0, pVol );						//Trace.TraceInformation( "strVol=" + strVol );
						this.previewVolume = Convert.ToInt32( strVol );					//Trace.TraceInformation( "previewVolume=" + previewVolume );
						int pPan = arg.IndexOf( ',', pVol + 1 );						//Trace.TraceInformation( "pPan=" + pPan );
						string strPan = arg.Substring( pVol + 1, pPan - pVol - 1 );		//Trace.TraceInformation( "strPan=" + strPan );
						this.previewPan = Convert.ToInt32( strPan );					//Trace.TraceInformation( "previewPan=" + previewPan );

						arg = arg.Substring( pPan + 1 );
						arg = arg.Trim( new char[] { '\"' } );
						this.previewFilename = arg;
						analyzing = false;
					}
					// -S  -Nxxx  filename
					else if ( arg.StartsWith( "-S", StringComparison.OrdinalIgnoreCase ) )		// DTXV再生停止
					{
						this.Enabled = true;
						this.Command = ECommand.Stop;
						this.Refreshed = true;
						ret = true;
						arg = arg.Substring( 2 );
					}
					else if ( arg.StartsWith( "-D", StringComparison.OrdinalIgnoreCase ) )
					{
						// -DW, -DA1など
						arg = arg.Substring( 2 );	// -D を削除
						switch ( arg[ 0 ] )
						{
							#region [ DirectSound ]
							case 'D':
								if ( this.soundDeviceType != ESoundDeviceType.DirectSound )
								{
									this.ChangedSoundDevice = true;
									this.soundDeviceType = ESoundDeviceType.DirectSound;
								}
								else
								{
									this.ChangedSoundDevice = false;
								}
								arg = arg.Substring( 1 );
								break;
							#endregion
							#region [ WASAPI ]
							case 'W':
								if ( this.soundDeviceType != ESoundDeviceType.ExclusiveWASAPI )
								{
									this.ChangedSoundDevice = true;
									this.soundDeviceType = ESoundDeviceType.ExclusiveWASAPI;
								}
								else
								{
									this.ChangedSoundDevice = false;
								}
								arg = arg.Substring( 1 );
								break;
							#endregion
							#region [ ASIO ]
							case 'A':
								if ( this.soundDeviceType != ESoundDeviceType.ASIO )
								{
									this.ChangedSoundDevice = true;
									this.soundDeviceType = ESoundDeviceType.ASIO;
								}
								else
								{
									this.ChangedSoundDevice = false;
								}
								arg = arg.Substring( 1 );

								int nAsioDev = 0, p = 0;
								while ( true )
								{
									char c = arg[ 0 ];
									if ( '0' <= c && c <= '9' )
									{
										nAsioDev *= 10;
										nAsioDev += c - '0';
										p++;
										arg = arg.Substring( 1 );
										continue;
									}
									else
									{
										break;
									}
								}
								if ( this.nASIOdevice != nAsioDev )
								{
									this.ChangedSoundDevice = true;
									this.nASIOdevice = nAsioDev;
								}
								break;
							#endregion
						}
						#region [ GRmode, TimeStretch, VSyncWait ]
						{
							// Reload判定は、-Nのところで行う
							this.GRmode =      ( arg[ 0 ] == 'Y' );
							this.TimeStretch = ( arg[ 1 ] == 'Y' );
							this.VSyncWait =   ( arg[ 2 ] == 'Y' );

							arg = arg.Substring( 3 );
						}
						#endregion
					}
					else if ( arg.StartsWith( "-N", StringComparison.OrdinalIgnoreCase ) )
					{
						this.Enabled = true;
						this.Command = ECommand.Play;
						ret = true;

						arg = arg.Substring( 2 );					// "-N"を除去
						string[] p = arg.Split( new char[] { ' ' } );
						this.nStartBar = int.Parse( p[ 0 ] );			// 再生開始小節
						if ( this.nStartBar < 0 )
						{
							this.nStartBar = -1;
						}

						int startIndex = arg.IndexOf( ' ' );
						string filename = arg.Substring( startIndex + 1 );	// 再生ファイル名(フルパス) これで引数が終わっていることを想定
						try
						{
							filename = filename.Trim( new char[] { '\"' } );
							bIsNeedReloadDTX( filename );
						}
						catch (Exception e)	// 指定ファイルが存在しない
						{
							Trace.TraceError( e.ToString() );
							Trace.TraceError( "例外が発生しましたが処理を継続します。 (d309a608-7311-411e-a565-19226c3116c2)" );
						}
						arg = "";
						analyzing = false;
					}
				}
			}
string[] s = { "Stop", "Play", "Preview" };
Trace.TraceInformation( "Command: " + s[ (int) this.Command ] );
			return ret;
		}

		/// <summary>
		/// Viewer関連の設定のみを更新して、Config.iniに書き出す
		/// </summary>
		public void tUpdateConfigIni()
		{
			CConfigIni cc = new CConfigIni();
			string path = TJAPlayer3.strEXEのあるフォルダ + "Config.ini";
			if ( File.Exists( path ) )
			{
				FileInfo fi = new FileInfo( path );
				if ( fi.Length > 0 )	// Config.iniが0byteだったなら、読み込まない
				{
					try
					{
						cc.tファイルから読み込み( path );
					}
					catch (Exception e)
					{
						//ConfigIni = new CConfigIni();	// 存在してなければ新規生成
						Trace.TraceError( e.ToString() );
						Trace.TraceError( "例外が発生しましたが処理を継続します。 (825f9ba6-9164-4f2e-8c41-edf4d73c06c9)" );
					}
				}
				fi = null;
			}

			cc.nViewerScrollSpeed     = TJAPlayer3.ConfigIni.n譜面スクロール速度;
			cc.bViewerShowDebugStatus = TJAPlayer3.ConfigIni.b演奏情報を表示する;
			cc.bViewerVSyncWait       = TJAPlayer3.ConfigIni.b垂直帰線待ちを行う;
			cc.bViewerTimeStretch     = TJAPlayer3.ConfigIni.bTimeStretch;
			cc.bViewerDrums有効       = true;

			cc.t書き出し( path );
		}

		private string last_path;
		private DateTime last_timestamp;

	}
}
