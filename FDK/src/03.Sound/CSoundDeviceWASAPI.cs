using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using ManagedBass;
using ManagedBass.Wasapi;
using ManagedBass.Mix;

namespace FDK
{
	internal class CSoundDeviceWASAPI : ISoundDevice
	{
		// プロパティ

		public ESoundDeviceType SoundDeviceType
		{
			get;
			protected set;
		}
		public long OutputDelay
		{
			get;
			protected set;
		}
		public long BufferSize
		{
			get;
			protected set;
		}

		// CSoundTimer 用に公開しているプロパティ

		public long ElapsedTimeMs
		{
			get;
			protected set;
		}
		public long UpdateSystemTimeMs
		{
			get;
			protected set;
		}
		public CTimer SystemTimer
		{
			get;
			protected set;
		}

		public enum EWASAPIMode { Exclusion, Share }

		public int nMasterVolume
		{
			get
			{
				float volume = 0.0f;
				//if ( BassMix.BASS_Mixer_ChannelGetEnvelopePos( this.hMixer, BASSMIXEnvelope.BASS_MIXER_ENV_VOL, ref f音量 ) == -1 )
				//    return 100;
				//bool b = Bass.BASS_ChannelGetAttribute( this.hMixer, BASSAttribute.BASS_ATTRIB_VOL, ref f音量 );
				bool b = Bass.ChannelGetAttribute( this.hMixer, ChannelAttribute.Volume, out volume );
				if ( !b )
				{
					Errors be = Bass.LastError;
					Trace.TraceInformation( "WASAPI Master Volume Get Error: " + be.ToString() );
				}
				else
				{
					Trace.TraceInformation( "WASAPI Master Volume Get Success: " + (volume * 100) );

				}
				return (int) ( volume * 100 );
			}
			set
			{
				// bool b = Bass.BASS_SetVolume( value / 100.0f );
				// →Exclusiveモード時は無効

//				bool b = BassWasapi.BASS_WASAPI_SetVolume( BASSWASAPIVolume.BASS_WASAPI_VOL_SESSION, (float) ( value / 100 ) );
//				bool b = BassWasapi.BASS_WASAPI_SetVolume( BASSWASAPIVolume.BASS_WASAPI_CURVE_WINDOWS, (float) ( value / 100 ) );
				bool b = Bass.ChannelSetAttribute( this.hMixer, ChannelAttribute.Volume, (float) ( value / 100.0 ) );
				// If you would like to have a volume control in exclusive mode too, and you're using the BASSmix add-on,
				// you can adjust the source's BASS_ATTRIB_VOL setting via BASS_ChannelSetAttribute.
				// しかし、hMixerに対するBASS_ChannelSetAttribute()でBASS_ATTRIB_VOLを変更: なぜか出力音量に反映されず

				// Bass_SetVolume(): BASS_ERROR_NOTAVIL ("no sound" deviceには適用不可)

				// Mixer_ChannelSetEnvelope():

				//var nodes = new BASS_MIXER_NODE[ 1 ] { new BASS_MIXER_NODE( 0, (float) value ) };
				//bool b = BassMix.BASS_Mixer_ChannelSetEnvelope( this.hMixer, BASSMIXEnvelope.BASS_MIXER_ENV_VOL, nodes );
				//bool b = Bass.BASS_ChannelSetAttribute( this.hMixer, BASSAttribute.BASS_ATTRIB_VOL, value / 100.0f );
				if ( !b )
				{
					Errors be = Bass.LastError;
					Trace.TraceInformation( "WASAPI Master Volume Set Error: " + be.ToString() );
				}
				else
				{
					// int n = this.nMasterVolume;	
					// Trace.TraceInformation( "WASAPI Master Volume Set Success: " + value );

				}
			}
		}
		// メソッド

		/// <summary>
		/// WASAPIの初期化
		/// </summary>
		/// <param name="mode"></param>
		/// <param name="bufferSize">(未使用; 本メソッド内で自動設定する)</param>
		/// <param name="interval">(未使用; 本メソッド内で自動設定する)</param>
		public CSoundDeviceWASAPI( EWASAPIMode mode, long bufferSize, long interval )
		{
			// 初期化。

			Trace.TraceInformation( "BASS (WASAPI) の初期化を開始します。" );

			this.SoundDeviceType = ESoundDeviceType.Unknown;
			this.OutputDelay = 0;
			this.ElapsedTimeMs = 0;
			this.UpdateSystemTimeMs = CTimer.UnusedNum;
			this.SystemTimer = new CTimer( CTimer.TimerType.MultiMedia );
			this.b最初の実出力遅延算出 = true;

			// BASS の設定。

			this.bIsBASSFree = true;

		    if (!Bass.Configure( Configuration.UpdatePeriod, 0 )) // 0:BASSストリームの自動更新を行わない。
		    {
		        Trace.TraceWarning($"Configure({nameof(Configuration.UpdatePeriod)}) に失敗しました。[{Bass.LastError.ToString()}]");
		    }
		    if (!Bass.Configure( Configuration.UpdateThreads, 0 )) // 0:BASSストリームの自動更新を行わない。
		    {
		        Trace.TraceWarning($"Configure({nameof(Configuration.UpdateThreads)}) に失敗しました。[{Bass.LastError.ToString()}]");
		    }

			// BASS の初期化。

			int nデバイス = 0;		// 0:"no device" … BASS からはデバイスへアクセスさせない。アクセスは BASSWASAPI アドオンから行う。
			int n周波数 = 44100;	// 仮決め。lデバイス（≠ドライバ）がネイティブに対応している周波数であれば何でもいい？ようだ。BASSWASAPIでデバイスの周波数は変えられる。いずれにしろBASSMXで自動的にリサンプリングされる。
			//if( !Bass.Init( nデバイス, n周波数, DeviceInitFlags.Default, IntPtr.Zero ) )
			//	throw new Exception( string.Format( "BASS (WASAPI) の初期化に失敗しました。(BASS_Init)[{0}]", Bass.LastError.ToString() ) );

		    Bass.Configure(Configuration.LogarithmicVolumeCurve, true);

			#region [ デバッグ用: WASAPIデバイスのenumerateと、ログ出力 ]
			//(デバッグ用)
			Trace.TraceInformation("サウンドデバイス一覧:");
			int a;
			string strDefaultSoundDeviceName = null;
			DeviceInfo[] bassDevInfos = new DeviceInfo[Bass.DeviceCount];
			for(int j = 0; j < bassDevInfos.Length; j++)
            {
				bassDevInfos[j] = Bass.GetDeviceInfo(j);
            }
			for (a = 0; a < bassDevInfos.GetLength(0); a++)
			{
				{
					Trace.TraceInformation("Sound Device #{0}: {1}: IsDefault={2}, isEnabled={3}",
						a,
						bassDevInfos[a].Name,
						bassDevInfos[a].IsDefault,
						bassDevInfos[a].IsEnabled
					);
					if (bassDevInfos[a].IsDefault)
					{
						// これはOS標準のdefault device。後でWASAPIのdefault deviceと比較する。
						strDefaultSoundDeviceName = bassDevInfos[a].Name;
					}
				}
			}
			#endregion

			// BASS WASAPI の初期化。

			nデバイス = -1;
			n周波数 = 0;			// デフォルトデバイスの周波数 (0="mix format" sample rate)
			int nチャンネル数 = 0;	// デフォルトデバイスのチャンネル数 (0="mix format" channels)
			this.tWasapiProc = new WasapiProcedure( this.tWASAPI処理 );		// アンマネージに渡す delegate は、フィールドとして保持しておかないとGCでアドレスが変わってしまう。

			// WASAPIの更新間隔(period)は、バッファサイズにも影響を与える。
			// 更新間隔を最小にするには、BassWasapi.BASS_WASAPI_GetDeviceInfo( ndevNo ).minperiod の値を使えばよい。
			// これをやらないと、更新間隔ms=6ms となり、バッファサイズを 6ms x 4 = 24msより小さくできない。
			#region [ 既定の出力デバイスと設定されているWASAPIデバイスを検索し、更新間隔msを設定できる最小値にする ]
			int nDevNo = -1;
			WasapiDeviceInfo deviceInfo;
			for (int n = 0; BassWasapi.GetDeviceInfo(n, out deviceInfo); n++)
			{
				// #37940 2018.2.15: BASS_DEVICEINFOとBASS_WASAPI_DEVICEINFOで、IsDefaultとなっているデバイスが異なる場合がある。
				// (WASAPIでIsDefaultとなっているデバイスが正しくない場合がある)
				// そのため、BASS_DEVICEでIsDefaultとなっているものを探し、それと同じ名前のWASAPIデバイスを使用する。
				// #39490 2019.8.19: 更に、環境によっては同じ名前のWASAPIデバイスが複数定義されている場合があるため、
				// 実際に利用可能なWASAPIデバイスのみに対象を絞り込む。
				// (具体的には、defperiod, minperiod, mixchans, mixfreqがすべて0のデバイスは使用不可のため
				//  これらが0でないものを選択する)
				//if ( deviceInfo.IsDefault )
				if (deviceInfo.Name == strDefaultSoundDeviceName && deviceInfo.MixFrequency > 0)
				{
					nDevNo = n;
					#region [ 既定の出力デバイスの情報を表示 ]
					Trace.TraceInformation("WASAPI Device #{0}: {1}: IsDefault={2}, defPeriod={3}s, minperiod={4}s, mixchans={5}, mixfreq={6}",
						n,
						deviceInfo.Name,
						deviceInfo.IsDefault, deviceInfo.DefaultUpdatePeriod, deviceInfo.MinimumUpdatePeriod, deviceInfo.MixChannels, deviceInfo.MixFrequency);
					#endregion
					break;
				}
			}
			if (nDevNo != -1)
			{
				Trace.TraceInformation("Start Bass_Init(device=0(fixed value: no sound), deviceInfo.mixfreq=" + deviceInfo.MixFrequency + ", BASS_DEVICE_DEFAULT, Zero)");
				if (!Bass.Init(0, deviceInfo.MixFrequency, DeviceInitFlags.Default, IntPtr.Zero))  // device = 0:"no device": BASS からはデバイスへアクセスさせない。アクセスは BASSWASAPI アドオンから行う。
					throw new Exception(string.Format("BASS (WASAPI{0}) の初期化に失敗しました。(BASS_Init)[{1}]", mode.ToString(), Bass.LastError.ToString()));

				// Trace.TraceInformation( "Selected Default WASAPI Device: {0}", deviceInfo.name );
				// Trace.TraceInformation( "MinPeriod={0}, DefaultPeriod={1}", deviceInfo.minperiod, deviceInfo.defperiod );

				// n更新間隔ms = ( mode == Eデバイスモード.排他 )?	Convert.ToInt64(Math.Ceiling(deviceInfo.minperiod * 1000.0f)) : Convert.ToInt64(Math.Ceiling(deviceInfo.defperiod * 1000.0f));
				// 更新間隔として、WASAPI排他時はminperiodより大きい最小のms値を、WASAPI共有時はdefperiodより大きい最小のms値を用いる
				// Win10では、更新間隔がminperiod以下だと、確実にBASS_ERROR_UNKNOWNとなる。

				//if ( n希望バッファサイズms <= 0 || n希望バッファサイズms < n更新間隔ms + 1 )
				//{
				//	n希望バッファサイズms = n更新間隔ms + 1; // 2013.4.25 #31237 yyagi; バッファサイズ設定の完全自動化。更新間隔＝バッファサイズにするとBASS_ERROR_UNKNOWNになるので+1する。
				//}
			}
			else
			{
				Trace.TraceError("Error: Default WASAPI Device is not found.");
			}
			#endregion

			//Retry:
			var flags = ( mode == EWASAPIMode.Exclusion ) ? WasapiInitFlags.AutoFormat | WasapiInitFlags.Exclusive : WasapiInitFlags.Shared | WasapiInitFlags.AutoFormat;
			//var flags = ( mode == Eデバイスモード.排他 ) ? BASSWASAPIInit.BASS_WASAPI_AUTOFORMAT | BASSWASAPIInit.BASS_WASAPI_EVENT | BASSWASAPIInit.BASS_WASAPI_EXCLUSIVE : BASSWASAPIInit.BASS_WASAPI_AUTOFORMAT | BASSWASAPIInit.BASS_WASAPI_EVENT;
			
			if ( BassWasapi.Init( nデバイス, n周波数, nチャンネル数, flags, ( bufferSize / 1000.0f ), ( interval / 1000.0f ), this.tWasapiProc, IntPtr.Zero ) )
			{
				if( mode == EWASAPIMode.Exclusion )
				{
					#region [ 排他モードで作成成功。]
					//-----------------
					this.SoundDeviceType = ESoundDeviceType.ExclusiveWASAPI;

					nDevNo = BassWasapi.CurrentDevice;
					deviceInfo = BassWasapi.GetDeviceInfo( nDevNo );
					BassWasapi.GetInfo(out var wasapiInfo);
					int n1サンプルのバイト数 = 2 * wasapiInfo.Channels;	// default;
					switch( wasapiInfo.Format )		// BASS WASAPI で扱うサンプルはすべて 32bit float で固定されているが、デバイスはそうとは限らない。
					{
						case WasapiFormat.Bit8: n1サンプルのバイト数 = 1 * wasapiInfo.Channels; break;
						case WasapiFormat.Bit16: n1サンプルのバイト数 = 2 * wasapiInfo.Channels; break;
						case WasapiFormat.Bit24: n1サンプルのバイト数 = 3 * wasapiInfo.Channels; break;
						case WasapiFormat.Bit32: n1サンプルのバイト数 = 4 * wasapiInfo.Channels; break;
						case WasapiFormat.Float: n1サンプルのバイト数 = 4 * wasapiInfo.Channels; break;
						case WasapiFormat.Unknown: throw new ArgumentOutOfRangeException($"WASAPI format error ({wasapiInfo.ToString()})");
					}
					int n1秒のバイト数 = n1サンプルのバイト数 * wasapiInfo.Frequency;
					this.BufferSize = (long) ( wasapiInfo.BufferLength * 1000.0f / n1秒のバイト数 );
					this.OutputDelay = 0;	// 初期値はゼロ
					Trace.TraceInformation( "使用デバイス: #" + nDevNo + " : " + deviceInfo.Name );
					Trace.TraceInformation( "BASS を初期化しました。(WASAPI排他モード, {0}Hz, {1}ch, フォーマット:{2}, バッファ{3}bytes [{4}ms(希望{5}ms)], 更新間隔{6}ms)",
						wasapiInfo.Frequency,
						wasapiInfo.Channels,
						wasapiInfo.Format.ToString(),
						wasapiInfo.BufferLength,
						BufferSize.ToString(),
						bufferSize.ToString(),
						interval.ToString() );
					Trace.TraceInformation( "デバイスの最小更新時間={0}ms, 既定の更新時間={1}ms", deviceInfo.MinimumUpdatePeriod * 1000, deviceInfo.DefaultUpdatePeriod * 1000 );
					this.bIsBASSFree = false;
					//-----------------
					#endregion
				}
				else
				{
					#region [ 共有モードで作成成功。]
					//-----------------
					this.SoundDeviceType = ESoundDeviceType.SharedWASAPI;

					var devInfo = BassWasapi.GetDeviceInfo(BassWasapi.CurrentDevice);
					BassWasapi.GetInfo(out var wasapiInfo);
                    int n1サンプルのバイト数 = 2 * wasapiInfo.Channels; // default;
                    switch (wasapiInfo.Format)      // BASS WASAPI で扱うサンプルはすべて 32bit float で固定されているが、デバイスはそうとは限らない。
                    {
						case WasapiFormat.Bit8: n1サンプルのバイト数 = 1 * wasapiInfo.Channels; break;
						case WasapiFormat.Bit16: n1サンプルのバイト数 = 2 * wasapiInfo.Channels; break;
						case WasapiFormat.Bit24: n1サンプルのバイト数 = 3 * wasapiInfo.Channels; break;
						case WasapiFormat.Bit32: n1サンプルのバイト数 = 4 * wasapiInfo.Channels; break;
						case WasapiFormat.Float: n1サンプルのバイト数 = 4 * wasapiInfo.Channels; break;
						case WasapiFormat.Unknown: throw new ArgumentOutOfRangeException($"WASAPI format error ({wasapiInfo.ToString()})");
					}
                    int n1秒のバイト数 = n1サンプルのバイト数 * wasapiInfo.Frequency;
                    this.BufferSize = (long)(wasapiInfo.BufferLength * 1000.0f / n1秒のバイト数);

                    this.OutputDelay = 0;	// 初期値はゼロ
					
					Trace.TraceInformation( "BASS を初期化しました。(WASAPI共有モード, {0}ms, 更新間隔{1}ms)", bufferSize, devInfo.DefaultUpdatePeriod * 1000.0f );
					this.bIsBASSFree = false;
					//-----------------
					#endregion
				}

			}
			#region [ #31737 WASAPI排他モードのみ利用可能とし、WASAPI共有モードは使用できないようにするために、WASAPI共有モードでの初期化フローを削除する。 ]
			//else if ( mode == Eデバイスモード.排他 )
			//{
			//    Trace.TraceInformation("Failed to initialize setting BASS (WASAPI) mode [{0}]", Bass.BASS_ErrorGetCode().ToString() );
			//    #region [ 排他モードに失敗したのなら共有モードでリトライ。]
			//    //-----------------
			//    mode = Eデバイスモード.共有;
			//    goto Retry;
			//    //-----------------
			//    #endregion
			//}
			#endregion
			else
			{
				#region [ それでも失敗したら例外発生。]
				//-----------------
				Errors errcode = Bass.LastError;
				Bass.Free();
				this.bIsBASSFree = true;
				throw new Exception( string.Format( "BASS (WASAPI) の初期化に失敗しました。(BASS_WASAPI_Init)[{0}]", errcode ) );
				//-----------------
				#endregion
			}


			// WASAPI出力と同じフォーマットを持つ BASS ミキサーを作成。

			BassWasapi.GetInfo(out var info);
			this.hMixer = BassMix.CreateMixerStream(
				info.Frequency,
				info.Channels,
				BassFlags.MixerNonStop | BassFlags.Float | BassFlags.Decode);	// デコードのみ＝発声しない。WASAPIに出力されるだけ。
			if ( this.hMixer == 0 )
			{
				Errors errcode = Bass.LastError;
				BassWasapi.Free();
				Bass.Free();
				this.bIsBASSFree = true;
				throw new Exception( string.Format( "BASSミキサ(mixing)の作成に失敗しました。[{0}]", errcode ) );
			}


			// BASS ミキサーの1秒あたりのバイト数を算出。

			var mixerInfo = Bass.ChannelGetInfo( this.hMixer );
			long nミキサーの1サンプルあたりのバイト数 = mixerInfo.Channels * 4;	// 4 = sizeof(FLOAT)
			this.nミキサーの1秒あたりのバイト数 = nミキサーの1サンプルあたりのバイト数 * mixerInfo.Frequency;



			// 単純に、hMixerの音量をMasterVolumeとして制御しても、
			// ChannelGetData()の内容には反映されない。
			// そのため、もう一段mixerを噛ませて、一段先のmixerからChannelGetData()することで、
			// hMixerの音量制御を反映させる。
			this.hMixer_DeviceOut = BassMix.CreateMixerStream(
				info.Frequency,
				info.Channels,
				BassFlags.MixerNonStop | BassFlags.Float | BassFlags.Decode );	// デコードのみ＝発声しない。WASAPIに出力されるだけ。
			if ( this.hMixer_DeviceOut == 0 )
			{
				Errors errcode = Bass.LastError;
				BassWasapi.Free();
				Bass.Free();
				this.bIsBASSFree = true;
				throw new Exception( string.Format( "BASSミキサ(最終段)の作成に失敗しました。[{0}]", errcode ) );
			}

			{
				bool b1 = BassMix.MixerAddChannel( this.hMixer_DeviceOut, this.hMixer, BassFlags.Default );
				if ( !b1 )
				{
					Errors errcode = Bass.LastError;
					BassWasapi.Free();
					Bass.Free();
					this.bIsBASSFree = true;
					throw new Exception( string.Format( "BASSミキサ(最終段とmixing)の接続に失敗しました。[{0}]", errcode ) );
				};
			}


			// 出力を開始。

			BassWasapi.Start();
		}
		#region [ tサウンドを作成する() ]
		public CSound tCreateSound( string strファイル名, ESoundGroup soundGroup )
		{
			var sound = new CSound(soundGroup);
			sound.CreateWASAPISound( strファイル名, this.hMixer, this.SoundDeviceType );
			return sound;
		}

		public void tCreateSound( string strファイル名, CSound sound )
		{
			sound.CreateWASAPISound( strファイル名, this.hMixer, this.SoundDeviceType );
		}
		#endregion

		#region [ Dispose-Finallizeパターン実装 ]
		//-----------------
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );
		}
		protected void Dispose( bool bManagedDispose )
		{
			SoundDeviceType = ESoundDeviceType.Unknown;		// まず出力停止する(Dispose中にクラス内にアクセスされることを防ぐ)
			if ( hMixer != -1 )
			{
				Bass.StreamFree( hMixer );
			}
			if ( !bIsBASSFree )
			{
				BassWasapi.Free();	// システムタイマより先に呼び出すこと。（tWasapi処理() の中でシステムタイマを参照してるため）
				Bass.Free();
			}
			if( bManagedDispose )
			{
				SystemTimer.Dispose();
				SystemTimer = null;
			}
		}
		~CSoundDeviceWASAPI()
		{
			this.Dispose( false );
		}
		//-----------------
		#endregion

		protected int hMixer = -1;
		protected int hMixer_DeviceOut = -1;
		protected WasapiProcedure tWasapiProc = null;

		protected int tWASAPI処理( IntPtr buffer, int length, IntPtr user )
		{
			// BASSミキサからの出力データをそのまま WASAPI buffer へ丸投げ。

			int num = Bass.ChannelGetData( this.hMixer_DeviceOut, buffer, length );		// num = 実際に転送した長さ
			if ( num == -1 ) num = 0;


			// 経過時間を更新。
			// データの転送差分ではなく累積転送バイト数から算出する。

			int n未再生バイト数 = BassWasapi.GetData( null, (int) DataFlags.Available );	// 誤差削減のため、必要となるギリギリ直前に取得する。
			this.ElapsedTimeMs = ( this.n累積転送バイト数 - n未再生バイト数 ) * 1000 / this.nミキサーの1秒あたりのバイト数;
			this.UpdateSystemTimeMs = this.SystemTimer.SystemTimeMs;

			// 実出力遅延を更新。
			// 未再生バイト数の平均値。

			long n今回の遅延ms = n未再生バイト数 * 1000 / this.nミキサーの1秒あたりのバイト数;
			this.OutputDelay = ( this.b最初の実出力遅延算出 ) ? n今回の遅延ms : ( this.OutputDelay + n今回の遅延ms ) / 2;
			this.b最初の実出力遅延算出 = false;

			
			// 経過時間を更新後に、今回分の累積転送バイト数を反映。
			
			this.n累積転送バイト数 += num;
			return num;
		}

		private long nミキサーの1秒あたりのバイト数 = 0;
		private long n累積転送バイト数 = 0;
		private bool b最初の実出力遅延算出 = true;
		private bool bIsBASSFree = true;
	}
}
