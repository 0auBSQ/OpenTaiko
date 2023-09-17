using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Threading;
using FDK.ExtensionMethods;
using ManagedBass;
using ManagedBass.Asio;
using ManagedBass.Wasapi;
using ManagedBass.Mix;
using ManagedBass.Fx;
using Silk.NET.Windowing;
using FDK.BassMixExtension;


namespace FDK
{
	public class SoundManager	// : CSound
	{
		private static ISoundDevice SoundDevice
		{
			get; set;
		}
		private static ESoundDeviceType SoundDeviceType
		{
			get; set;
		}
		public static CSoundTimer PlayTimer = null;
		public static bool bUseOSTimer = false;		// OSのタイマーを使うか、CSoundTimerを使うか。DTXCではfalse, DTXManiaではtrue。
													// DTXC(DirectSound)でCSoundTimerを使うと、内部で無音のループサウンドを再生するため
													// サウンドデバイスを占有してしまい、Viewerとして呼び出されるDTXManiaで、ASIOが使えなくなる。

													// DTXMania単体でこれをtrueにすると、WASAPI/ASIO時に演奏タイマーとしてFDKタイマーではなく
													// システムのタイマーを使うようになる。こうするとスクロールは滑らかになるが、音ズレが出るかもしれない。
		
		public static bool bIsTimeStretch = false;

		private static IWindow Window_;

		private static int _nMasterVolume;
		public int nMasterVolume
		{
			get
			{
				return _nMasterVolume;
			}
			//get
			//{
			//    if ( SoundDeviceType == ESoundDeviceType.ExclusiveWASAPI || SoundDeviceType == ESoundDeviceType.ASIO )
			//    {
			//        return Bass.BASS_GetConfig(BASSConfig.BASS_CONFIG_GVOL_STREAM ) / 100;
			//    }
			//    else
			//    {
			//        return 100;
			//    }
			//}
			//set
			//{
			//    if ( SoundDeviceType == ESoundDeviceType.ExclusiveWASAPI )
			//    {
			//			// LINEARでなくWINDOWS(2)を使う必要があるが、exclusive時は使用不可、またデバイス側が対応してないと使用不可
			//        bool b = BassWasapi.BASS_WASAPI_SetVolume( BASSWASAPIVolume.BASS_WASAPI_CURVE_LINEAR, value / 100.0f );
			//        if ( !b )
			//        {
			//            BASSError be = Bass.BASS_ErrorGetCode();
			//            Trace.TraceInformation( "WASAPI Master Volume Set Error: " + be.ToString() );
			//        }
			//    }
			//}
			//set
			//{
			//    if ( SoundDeviceType == ESoundDeviceType.ExclusiveWASAPI || SoundDeviceType == ESoundDeviceType.ASIO )
			//    {
			//        bool b = Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_STREAM, value * 100 );
			//        if ( !b )
			//        {
			//            BASSError be = Bass.BASS_ErrorGetCode();
			//            Trace.TraceInformation( "Master Volume Set Error: " + be.ToString() );
			//        }
			//    }
			//}
			//set
			//{
			//    if ( SoundDeviceType == ESoundDeviceType.ExclusiveWASAPI || SoundDeviceType == ESoundDeviceType.ASIO )
			//    {
			//        var nodes = new BASS_MIXER_NODE[ 1 ] { new BASS_MIXER_NODE( 0, (float) value ) };
			//        BassMix.BASS_Mixer_ChannelSetEnvelope( SoundDevice.hMixer, BASSMIXEnvelope.BASS_MIXER_ENV_VOL, nodes );
			//    }
			//}
			set
			{
				SoundDevice.nMasterVolume = value;
				_nMasterVolume = value;
			}
		}

		///// <summary>
		///// BASS時、mp3をストリーミング再生せずに、デコードしたraw wavをオンメモリ再生する場合はtrueにする。
		///// 特殊なmp3を使用時はシークが乱れるので、必要に応じてtrueにすること。(Config.iniのNoMP3Streamingで設定可能。)
		///// ただし、trueにすると、その分再生開始までの時間が長くなる。
		///// </summary>
		//public static bool bIsMP3DecodeByWindowsCodec = false;

		public static int nMixing = 0;
		public int GetMixingStreams()
		{
			return nMixing;
		}
		public static int nStreams = 0;
		public int GetStreams()
		{
			return nStreams;
		}
		#region [ WASAPI/ASIO/DirectSound設定値 ]
		/// <summary>
		/// <para>WASAPI 排他モード出力における再生遅延[ms]（の希望値）。最終的にはこの数値を基にドライバが決定する）。</para>
		/// <para>0以下の値を指定すると、この数値はWASAPI初期化時に自動設定する。正数を指定すると、その値を設定しようと試みる。</para>
		/// </summary>
		public static int SoundDelayExclusiveWASAPI = 0;		// SSTでは、50ms
		public int GetSoundExclusiveWASAPI()
		{
			return SoundDelayExclusiveWASAPI;
		}
		public void SetSoundDelayExclusiveWASAPI( int value )
		{
			SoundDelayExclusiveWASAPI = value;
		}
		/// <summary>
		/// <para>WASAPI BASS出力における再生遅延[ms]。ユーザが決定する。</para>
		/// </summary>
		private static int SoundDelayBASS = 15;
		/// <para>BASSバッファの更新間隔。出力間隔ではないので注意。</para>
		/// <para>SoundDelay よりも小さい値であること。（小さすぎる場合はBASSによって自動修正される。）</para>
		/// </summary>
		private static int SoundUpdatePeriodBASS = 1;
		/// <summary>
		/// <para>WASAPI 共有モード出力における再生遅延[ms]。ユーザが決定する。</para>
		/// </summary>
		public static int SoundDelaySharedWASAPI = 100;
		/// <summary>
		/// <para>排他WASAPIバッファの更新間隔。出力間隔ではないので注意。</para>
		/// <para>→ 自動設定されるのでSoundDelay よりも小さい値であること。（小さすぎる場合はBASSによって自動修正される。）</para>
		/// </summary>
		public static int SoundUpdatePeriodExclusiveWASAPI = 6;
		/// <summary>
		/// <para>共有WASAPIバッファの更新間隔。出力間隔ではないので注意。</para>
		/// <para>SoundDelay よりも小さい値であること。（小さすぎる場合はBASSによって自動修正される。）</para>
		/// </summary>
		public static int SoundUpdatePeriodSharedWASAPI = 6;
		///// <summary>
		///// <para>ASIO 出力における再生遅延[ms]（の希望値）。最終的にはこの数値を基にドライバが決定する）。</para>
		///// </summary>
		//public static int SoundDelayASIO = 0;					// SSTでは50ms。0にすると、デバイスの設定値をそのまま使う。
		/// <summary>
		/// <para>ASIO 出力におけるバッファサイズ。</para>
		/// </summary>
		public static int SoundDelayASIO = 0;						// 0にすると、デバイスの設定値をそのまま使う。
		public int GetSoundDelayASIO()
		{
			return SoundDelayASIO;
		}
		public void SetSoundDelayASIO(int value)
		{
			SoundDelayASIO = value;
		}
		public static int ASIODevice = 0;
		public int GetASIODevice()
		{
			return ASIODevice;
		}
		public void SetASIODevice(int value)
		{
			ASIODevice = value;
		}
		/// <summary>
		/// <para>DirectSound 出力における再生遅延[ms]。ユーザが決定する。</para>
		/// </summary>
		public static int SoundDelayDirectSound = 100;

		public long GetSoundDelay()
		{
			if ( SoundDevice != null )
			{
				return SoundDevice.BufferSize;
			}
			else
			{
				return -1;
			}
		}

		#endregion


		/// <summary>
		/// DTXMania用コンストラクタ
		/// </summary>
		/// <param name="handle"></param>
		/// <param name="soundDeviceType"></param>
		/// <param name="nSoundDelayExclusiveWASAPI"></param>
		/// <param name="nSoundDelayASIO"></param>
		/// <param name="nASIODevice"></param>
		public SoundManager( IWindow window, ESoundDeviceType soundDeviceType, int nSoundDelayBASS, int nSoundDelayExclusiveWASAPI, int nSoundDelayASIO, int nASIODevice, bool _bUseOSTimer )
		{
			Window_ = window;
			SoundDevice = null;
			//bUseOSTimer = false;
			tInitialize( soundDeviceType, nSoundDelayBASS, nSoundDelayExclusiveWASAPI, nSoundDelayASIO, nASIODevice, _bUseOSTimer );
		}
		public void Dispose()
		{
			t終了();
		}

		//public static void t初期化()
		//{
		//    t初期化( ESoundDeviceType.DirectSound, 0, 0, 0 );
		//}

		public void tInitialize( ESoundDeviceType soundDeviceType, int _nSoundDelayBASS, int _nSoundDelayExclusiveWASAPI, int _nSoundDelayASIO, int _nASIODevice, IntPtr handle )
		{
			//if ( !bInitialized )
			{
				tInitialize( soundDeviceType, _nSoundDelayBASS, _nSoundDelayExclusiveWASAPI, _nSoundDelayASIO, _nASIODevice );
				//bInitialized = true;
			}
		}
		public void tInitialize( ESoundDeviceType soundDeviceType, int _nSoundDelayBASS, int _nSoundDelayExclusiveWASAPI, int _nSoundDelayASIO, int _nASIODevice )
		{
			tInitialize( soundDeviceType, _nSoundDelayBASS, _nSoundDelayExclusiveWASAPI, _nSoundDelayASIO, _nASIODevice, false );
		}

		public void tInitialize( ESoundDeviceType soundDeviceType, int _nSoundDelayBASS, int _nSoundDelayExclusiveWASAPI, int _nSoundDelayASIO, int _nASIODevice, bool _bUseOSTimer )
		{
			//SoundDevice = null;						// 後で再初期化することがあるので、null初期化はコンストラクタに回す
			PlayTimer = null;						// Global.Bass 依存（つまりユーザ依存）
			nMixing = 0;

			SoundDelayBASS = _nSoundDelayBASS;
			SoundDelayExclusiveWASAPI = _nSoundDelayExclusiveWASAPI;
			SoundDelaySharedWASAPI = _nSoundDelayExclusiveWASAPI;
            SoundDelayASIO = _nSoundDelayASIO;
			ASIODevice = _nASIODevice;
			bUseOSTimer = _bUseOSTimer;

			ESoundDeviceType[] ESoundDeviceTypes = new ESoundDeviceType[5]
			{
				ESoundDeviceType.Bass,
				ESoundDeviceType.ExclusiveWASAPI,
				ESoundDeviceType.SharedWASAPI,
				ESoundDeviceType.ASIO,
				ESoundDeviceType.Unknown
			};

			int initialDevice;
			switch ( soundDeviceType )
			{
				case ESoundDeviceType.Bass:
					initialDevice = 0;
					break;
				case ESoundDeviceType.ExclusiveWASAPI:
					initialDevice = 1;
					break;
				case ESoundDeviceType.SharedWASAPI:
					initialDevice = 2;
					break;
				case ESoundDeviceType.ASIO:
					initialDevice = 3;
					break;
				default:
					initialDevice = 4;
					break;
			}
			for ( SoundDeviceType = ESoundDeviceTypes[ initialDevice ]; ; SoundDeviceType = ESoundDeviceTypes[ ++initialDevice ] )
			{
				try
				{
					tReloadSoundDeviceAndSound();
					break;
				}
				catch ( Exception e )
				{
					Trace.TraceError( e.ToString() );
					Trace.TraceError( "例外が発生しましたが処理を継続します。 (2609806d-23e8-45c2-9389-b427e80915bc)" );
					if ( ESoundDeviceTypes[ initialDevice ] == ESoundDeviceType.Unknown )
					{
						Trace.TraceError( string.Format( "サウンドデバイスの初期化に失敗しました。" ) );
						break;
					}
				}
			}
			if ( soundDeviceType == ESoundDeviceType.Bass
				|| soundDeviceType == ESoundDeviceType.ExclusiveWASAPI
				|| soundDeviceType == ESoundDeviceType.SharedWASAPI
				|| soundDeviceType == ESoundDeviceType.ASIO )
			{
				//Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_UPDATETHREADS, 4 );
				//Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0 );

				Trace.TraceInformation( "BASS_CONFIG_UpdatePeriod=" + Bass.GetConfig( Configuration.UpdatePeriod ) );
				Trace.TraceInformation( "BASS_CONFIG_UpdateThreads=" + Bass.GetConfig( Configuration.UpdateThreads ) );
			}
		}

		public void tDisableUpdateBufferAutomatically()
		{
			//Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_UPDATETHREADS, 0 );
			//Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0 );

			//Trace.TraceInformation( "BASS_CONFIG_UpdatePeriod=" + Bass.BASS_GetConfig( BASSConfig.BASS_CONFIG_UPDATEPERIOD ) );
			//Trace.TraceInformation( "BASS_CONFIG_UpdateThreads=" + Bass.BASS_GetConfig( BASSConfig.BASS_CONFIG_UPDATETHREADS ) );
		}


		public static void t終了()
		{
			SoundDevice.Dispose();
			PlayTimer.Dispose();	// Global.Bass を解放した後に解放すること。（Global.Bass で参照されているため）
		}


		public static void tReloadSoundDeviceAndSound()
		{
			#region [ すでにサウンドデバイスと演奏タイマが構築されていれば解放する。]
			//-----------------
			if ( SoundDevice != null )
			{
				// すでに生成済みのサウンドがあれば初期状態に戻す。

				CSound.tResetAllSound();		// リソースは解放するが、CSoundのインスタンスは残す。


				// サウンドデバイスと演奏タイマを解放する。

				SoundDevice.Dispose();
				PlayTimer?.Dispose();	// Global.SoundDevice を解放した後に解放すること。（Global.SoundDevice で参照されているため）
			}
			//-----------------
			#endregion

			#region [ 新しいサウンドデバイスを構築する。]
			//-----------------
			switch ( SoundDeviceType )
			{
				case ESoundDeviceType.Bass:
					SoundDevice = new CSoundDeviceBASS( SoundDelayBASS, SoundUpdatePeriodBASS );
					break;

				case ESoundDeviceType.ExclusiveWASAPI:
					SoundDevice = new CSoundDeviceWASAPI( CSoundDeviceWASAPI.EWASAPIMode.Exclusion, SoundDelayExclusiveWASAPI, SoundUpdatePeriodExclusiveWASAPI );
					break;

				case ESoundDeviceType.SharedWASAPI:
					SoundDevice = new CSoundDeviceWASAPI( CSoundDeviceWASAPI.EWASAPIMode.Share, SoundDelaySharedWASAPI, SoundUpdatePeriodSharedWASAPI );
					break;

				case ESoundDeviceType.ASIO:
					SoundDevice = new CSoundDeviceASIO( SoundDelayASIO, ASIODevice );
					break;

				default:
					throw new Exception( string.Format( "未対応の SoundDeviceType です。[{0}]", SoundDeviceType.ToString() ) );
			}
			//-----------------
			#endregion
			#region [ 新しい演奏タイマを構築する。]
			//-----------------
			PlayTimer = new CSoundTimer( SoundDevice );
			//-----------------
			#endregion

			SoundDevice.nMasterVolume = _nMasterVolume;					// サウンドデバイスに対して、マスターボリュームを再設定する

			CSound.tReloadSound( SoundDevice );		// すでに生成済みのサウンドがあれば作り直す。
		}
		public CSound tCreateSound( string filename, ESoundGroup soundGroup )
		{
            if( !File.Exists( filename ) )
            {
                Trace.TraceWarning($"[i18n] File does not exist: {filename}");
                return null;
            }

			if ( SoundDeviceType == ESoundDeviceType.Unknown )
			{
				throw new Exception( string.Format( "未対応の SoundDeviceType です。[{0}]", SoundDeviceType.ToString() ) );
			}
			return SoundDevice.tCreateSound( filename, soundGroup );
		}

		private static DateTime lastUpdateTime = DateTime.MinValue;

		public void tDisposeSound( CSound csound )
		{
		    csound?.tDispose( true );			// インスタンスは存続→破棄にする。
		}

		public string GetCurrentSoundDeviceType()
		{
			switch ( SoundDeviceType )
			{
				case ESoundDeviceType.Bass:
					return "Bass";
				case ESoundDeviceType.ExclusiveWASAPI:
					return "Exclusive WASAPI";
				case ESoundDeviceType.SharedWASAPI:
					return "Shared WASAPI";
				case ESoundDeviceType.ASIO:
					return "ASIO";
				default:
					return "Unknown";
			}
		}

		public void AddMixer( CSound cs, double db再生速度, bool _b演奏終了後も再生が続くチップである )
		{
			cs.b演奏終了後も再生が続くチップである = _b演奏終了後も再生が続くチップである;
			cs.PlaySpeed = db再生速度;
			cs.AddBassSoundFromMixer();
		}
		public void AddMixer( CSound cs, double db再生速度 )
		{
			cs.PlaySpeed = db再生速度;
			cs.AddBassSoundFromMixer();
		}
		public void AddMixer( CSound cs )
		{
			cs.AddBassSoundFromMixer();
		}
		public void RemoveMixer( CSound cs )
		{
			cs.tRemoveSoundFromMixer();
		}
	}
}