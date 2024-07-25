using System.Diagnostics;
using ManagedBass;
using ManagedBass.Asio;
using ManagedBass.Mix;

namespace FDK {
	/// <summary>
	/// 全ASIOデバイスを列挙する静的クラス。
	/// BASS_Init()やBASS_ASIO_Init()の状態とは無関係に使用可能。
	/// </summary>
	public static class CEnumerateAllAsioDevices {
		public static string[] GetAllASIODevices() {
			try {
				string[] bassAsioDevName = new string[BassAsio.DeviceCount];
				for (int i = 0; i < bassAsioDevName.Length; i++)
					bassAsioDevName[i] = BassAsio.GetDeviceInfo(i).Name;

				if (bassAsioDevName.Length != 0)
					return bassAsioDevName;
			} catch (Exception e) {
				Trace.TraceWarning($"Exception occured in GetAllASIODevices ({e})");
			}

			return new string[] { "None" };
		}
	}

	internal class CSoundDeviceASIO : ISoundDevice {
		// プロパティ

		public ESoundDeviceType SoundDeviceType {
			get;
			protected set;
		}
		public long OutputDelay {
			get;
			protected set;
		}
		public long BufferSize {
			get;
			protected set;
		}
		public int ASIODevice {
			get;
			set;
		}

		// CSoundTimer 用に公開しているプロパティ

		public long ElapsedTimeMs {
			get;
			protected set;
		}
		public long UpdateSystemTimeMs {
			get;
			protected set;
		}
		public CTimer SystemTimer {
			get;
			protected set;
		}


		// マスターボリュームの制御コードは、WASAPI/ASIOで全く同じ。
		public int nMasterVolume {
			get {
				float f音量 = 0.0f;
				bool b = Bass.ChannelGetAttribute(this.hMixer, ChannelAttribute.Volume, out f音量);
				if (!b) {
					Errors be = Bass.LastError;
					Trace.TraceInformation("ASIO Master Volume Get Error: " + be.ToString());
				} else {
					//Trace.TraceInformation( "ASIO Master Volume Get Success: " + (f音量 * 100) );

				}
				return (int)(f音量 * 100);
			}
			set {
				bool b = Bass.ChannelSetAttribute(this.hMixer, ChannelAttribute.Volume, (float)(value / 100.0));
				if (!b) {
					Errors be = Bass.LastError;
					Trace.TraceInformation("ASIO Master Volume Set Error: " + be.ToString());
				} else {
					// int n = this.nMasterVolume;	
					// Trace.TraceInformation( "ASIO Master Volume Set Success: " + value );
				}
			}
		}

		// メソッド

		public CSoundDeviceASIO(long bufferSize, int deviceIndex) {
			// 初期化。

			Trace.TraceInformation("BASS (ASIO) の初期化を開始します。");
			this.SoundDeviceType = ESoundDeviceType.Unknown;
			this.OutputDelay = 0;
			this.ElapsedTimeMs = 0;
			this.UpdateSystemTimeMs = CTimer.UnusedNum;
			this.SystemTimer = new CTimer(CTimer.TimerType.MultiMedia);
			this.ASIODevice = deviceIndex;

			// BASS の設定。

			this.bIsBASSFree = true;

			if (!Bass.Configure(Configuration.UpdatePeriod, 0)) // 0:BASSストリームの自動更新を行わない。
			{
				Trace.TraceWarning($"BASS_SetConfig({nameof(Configuration.UpdatePeriod)}) に失敗しました。[{Bass.LastError.ToString()}]");
			}
			if (!Bass.Configure(Configuration.UpdateThreads, 0)) // 0:BASSストリームの自動更新を行わない。
			{
				Trace.TraceWarning($"BASS_SetConfig({nameof(Configuration.UpdateThreads)}) に失敗しました。[{Bass.LastError.ToString()}]");
			}

			// BASS の初期化。

			int nデバイス = 0;      // 0:"no device" … BASS からはデバイスへアクセスさせない。アクセスは BASSASIO アドオンから行う。
			int n周波数 = 44100;   // 仮決め。最終的な周波数はデバイス（≠ドライバ）が決める。
			if (!Bass.Init(nデバイス, n周波数, DeviceInitFlags.Default, IntPtr.Zero))
				throw new Exception(string.Format("BASS の初期化に失敗しました。(BASS_Init)[{0}]", Bass.LastError.ToString()));

			Bass.Configure(Configuration.LogarithmicVolumeCurve, true);

			//Debug.WriteLine( "BASS_Init()完了。" );
			#region [ デバッグ用: ASIOデバイスのenumerateと、ログ出力 ]
			//			CEnumerateAllAsioDevices.GetAllASIODevices();
			//Debug.WriteLine( "BassAsio.BASS_ASIO_GetDeviceInfo():" );
			//            int a, count = 0;
			//            BASS_ASIO_DEVICEINFO asioDevInfo;
			//            for ( a = 0; ( asioDevInfo = BassAsio.BASS_ASIO_GetDeviceInfo( a ) ) != null; a++ )
			//            {
			//                Trace.TraceInformation( "ASIO Device {0}: {1}, driver={2}", a, asioDevInfo.name, asioDevInfo.driver );
			//                count++; // count it
			//            }
			#endregion

			// BASS ASIO の初期化。
			AsioInfo asioInfo;
			if (BassAsio.Init(ASIODevice, AsioInitFlags.Thread))    // 専用スレッドにて起動
			{
				#region [ ASIO の初期化に成功。]
				//-----------------
				this.SoundDeviceType = ESoundDeviceType.ASIO;
				BassAsio.GetInfo(out asioInfo);
				this.n出力チャンネル数 = asioInfo.Outputs;
				this.db周波数 = BassAsio.Rate;
				this.fmtASIOデバイスフォーマット = BassAsio.ChannelGetFormat(false, 0);

				Trace.TraceInformation("BASS を初期化しました。(ASIO, デバイス:\"{0}\", 入力{1}, 出力{2}, {3}Hz, バッファ{4}～{6}sample ({5:0.###}～{7:0.###}ms), デバイスフォーマット:{8})",
					asioInfo.Name,
					asioInfo.Inputs,
					asioInfo.Outputs,
					this.db周波数.ToString("0.###"),
					asioInfo.MinBufferLength, asioInfo.MinBufferLength * 1000 / this.db周波数,
					asioInfo.MaxBufferLength, asioInfo.MaxBufferLength * 1000 / this.db周波数,
					this.fmtASIOデバイスフォーマット.ToString()
					);
				this.bIsBASSFree = false;
				#region [ debug: channel format ]
				//BASS_ASIO_CHANNELINFO chinfo = new BASS_ASIO_CHANNELINFO();
				//int chan = 0;
				//while ( true )
				//{
				//    if ( !BassAsio.BASS_ASIO_ChannelGetInfo( false, chan, chinfo ) )
				//        break;
				//    Debug.WriteLine( "Ch=" + chan + ": " + chinfo.name.ToString() + ", " + chinfo.group.ToString() + ", " + chinfo.format.ToString() );
				//    chan++;
				//}
				#endregion
				//-----------------
				#endregion
			} else {
				#region [ ASIO の初期化に失敗。]
				//-----------------
				Errors errcode = Bass.LastError;
				string errmes = errcode.ToString();
				if (errcode == Errors.OK) {
					errmes = "BASS_OK; The device may be dissconnected";
				}
				Bass.Free();
				this.bIsBASSFree = true;
				throw new Exception(string.Format("BASS (ASIO) の初期化に失敗しました。(BASS_ASIO_Init)[{0}]", errmes));
				//-----------------
				#endregion
			}


			// ASIO 出力チャンネルの初期化。

			this.tAsioProc = new AsioProcedure(this.tAsio処理);       // アンマネージに渡す delegate は、フィールドとして保持しておかないとGCでアドレスが変わってしまう。
			if (!BassAsio.ChannelEnable(false, 0, this.tAsioProc, IntPtr.Zero))     // 出力チャンネル0 の有効化。
			{
				#region [ ASIO 出力チャンネルの初期化に失敗。]
				//-----------------
				BassAsio.Free();
				Bass.Free();
				this.bIsBASSFree = true;
				throw new Exception(string.Format("Failed BASS_ASIO_ChannelEnable() [{0}]", BassAsio.LastError.ToString()));
				//-----------------
				#endregion
			}
			for (int i = 1; i < this.n出力チャンネル数; i++)        // 出力チャネルを全てチャネル0とグループ化する。
			{                                                       // チャネル1だけを0とグループ化すると、3ch以上の出力をサポートしたカードでの動作がおかしくなる
				if (!BassAsio.ChannelJoin(false, i, 0)) {
					#region [ 初期化に失敗。]
					//-----------------
					BassAsio.Free();
					Bass.Free();
					this.bIsBASSFree = true;
					throw new Exception(string.Format("Failed BASS_ASIO_ChannelJoin({1}) [{0}]", BassAsio.LastError, i));
					//-----------------
					#endregion
				}
			}
			if (!BassAsio.ChannelSetFormat(false, 0, this.fmtASIOチャンネルフォーマット))  // 出力チャンネル0のフォーマット
			{
				#region [ ASIO 出力チャンネルの初期化に失敗。]
				//-----------------
				BassAsio.Free();
				Bass.Free();
				this.bIsBASSFree = true;
				throw new Exception(string.Format("Failed BASS_ASIO_ChannelSetFormat() [{0}]", BassAsio.LastError.ToString()));
				//-----------------
				#endregion
			}

			// ASIO 出力と同じフォーマットを持つ BASS ミキサーを作成。

			var flag = BassFlags.MixerNonStop | BassFlags.Decode;   // デコードのみ＝発声しない。ASIO に出力されるだけ。
			if (this.fmtASIOデバイスフォーマット == AsioSampleFormat.Float)
				flag |= BassFlags.Float;
			this.hMixer = BassMix.CreateMixerStream((int)this.db周波数, this.n出力チャンネル数, flag);

			if (this.hMixer == 0) {
				Errors err = Bass.LastError;
				BassAsio.Free();
				Bass.Free();
				this.bIsBASSFree = true;
				throw new Exception(string.Format("BASSミキサ(mixing)の作成に失敗しました。[{0}]", err));
			}

			// BASS ミキサーの1秒あたりのバイト数を算出。

			var mixerInfo = Bass.ChannelGetInfo(this.hMixer);
			int nサンプルサイズbyte = 0;
			switch (this.fmtASIOチャンネルフォーマット) {
				case AsioSampleFormat.Bit16: nサンプルサイズbyte = 2; break;
				case AsioSampleFormat.Bit24: nサンプルサイズbyte = 3; break;
				case AsioSampleFormat.Bit32: nサンプルサイズbyte = 4; break;
				case AsioSampleFormat.Float: nサンプルサイズbyte = 4; break;
			}
			//long nミキサーの1サンプルあたりのバイト数 = /*mixerInfo.chans*/ 2 * nサンプルサイズbyte;
			long nミキサーの1サンプルあたりのバイト数 = mixerInfo.Channels * nサンプルサイズbyte;
			this.nミキサーの1秒あたりのバイト数 = nミキサーの1サンプルあたりのバイト数 * mixerInfo.Frequency;


			// 単純に、hMixerの音量をMasterVolumeとして制御しても、
			// ChannelGetData()の内容には反映されない。
			// そのため、もう一段mixerを噛ませて、一段先のmixerからChannelGetData()することで、
			// hMixerの音量制御を反映させる。
			this.hMixer_DeviceOut = BassMix.CreateMixerStream(
				(int)this.db周波数, this.n出力チャンネル数, flag);
			if (this.hMixer_DeviceOut == 0) {
				Errors errcode = Bass.LastError;
				BassAsio.Free();
				Bass.Free();
				this.bIsBASSFree = true;
				throw new Exception(string.Format("BASSミキサ(最終段)の作成に失敗しました。[{0}]", errcode));
			}
			{
				bool b1 = BassMix.MixerAddChannel(this.hMixer_DeviceOut, this.hMixer, BassFlags.Default);
				if (!b1) {
					Errors errcode = Bass.LastError;
					BassAsio.Free();
					Bass.Free();
					this.bIsBASSFree = true;
					throw new Exception(string.Format("BASSミキサ(最終段とmixing)の接続に失敗しました。[{0}]", errcode));
				};
			}


			// 出力を開始。

			this.nバッファサイズsample = (int)(bufferSize * this.db周波数 / 1000.0);
			//this.nバッファサイズsample = (int)  nバッファサイズbyte;
			if (!BassAsio.Start(this.nバッファサイズsample))       // 範囲外の値を指定した場合は自動的にデフォルト値に設定される。
			{
				Errors err = BassAsio.LastError;
				BassAsio.Free();
				Bass.Free();
				this.bIsBASSFree = true;
				throw new Exception("ASIO デバイス出力開始に失敗しました。" + err.ToString());
			} else {
				int n遅延sample = BassAsio.GetLatency(false); // この関数は BASS_ASIO_Start() 後にしか呼び出せない。
				int n希望遅延sample = (int)(bufferSize * this.db周波数 / 1000.0);
				this.BufferSize = this.OutputDelay = (long)(n遅延sample * 1000.0f / this.db周波数);
				Trace.TraceInformation("ASIO デバイス出力開始：バッファ{0}sample(希望{1}) [{2}ms(希望{3}ms)]", n遅延sample, n希望遅延sample, this.OutputDelay, bufferSize);
			}
		}

		#region [ tサウンドを作成する() ]
		public CSound tCreateSound(string strファイル名, ESoundGroup soundGroup) {
			var sound = new CSound(soundGroup);
			sound.CreateASIOSound(strファイル名, this.hMixer);
			return sound;
		}

		public void tCreateSound(string strファイル名, CSound sound) {
			sound.CreateASIOSound(strファイル名, this.hMixer);
		}
		#endregion


		#region [ Dispose-Finallizeパターン実装 ]
		//-----------------
		public void Dispose() {
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected void Dispose(bool bManagedDispose) {
			SoundDeviceType = ESoundDeviceType.Unknown;     // まず出力停止する(Dispose中にクラス内にアクセスされることを防ぐ)
			if (hMixer != -1) {
				Bass.StreamFree(hMixer);
			}
			if (!bIsBASSFree) {
				BassAsio.Free();    // システムタイマより先に呼び出すこと。（tAsio処理() の中でシステムタイマを参照してるため）
				Bass.Free();
			}

			if (bManagedDispose) {
				SystemTimer.Dispose();
				SystemTimer = null;
			}
		}
		~CSoundDeviceASIO() {
			this.Dispose(false);
		}
		//-----------------
		#endregion


		protected int hMixer = -1;
		protected int hMixer_DeviceOut = -1;
		protected int n出力チャンネル数 = 0;
		protected double db周波数 = 0.0;
		protected int nバッファサイズsample = 0;
		protected AsioSampleFormat fmtASIOデバイスフォーマット = AsioSampleFormat.Unknown;
		protected AsioSampleFormat fmtASIOチャンネルフォーマット = AsioSampleFormat.Bit16;     // 16bit 固定
																					//protected BASSASIOFormat fmtASIOチャンネルフォーマット = BASSASIOFormat.BASS_ASIO_FORMAT_32BIT;// 16bit 固定
		protected AsioProcedure tAsioProc = null;

		protected int tAsio処理(bool input, int channel, IntPtr buffer, int length, IntPtr user) {
			if (input) return 0;


			// BASSミキサからの出力データをそのまま ASIO buffer へ丸投げ。

			int num = Bass.ChannelGetData(this.hMixer_DeviceOut, buffer, length);       // num = 実際に転送した長さ

			if (num == -1) num = 0;


			// 経過時間を更新。
			// データの転送差分ではなく累積転送バイト数から算出する。

			this.ElapsedTimeMs = (this.n累積転送バイト数 * 1000 / this.nミキサーの1秒あたりのバイト数) - this.OutputDelay;
			this.UpdateSystemTimeMs = this.SystemTimer.SystemTimeMs;


			// 経過時間を更新後に、今回分の累積転送バイト数を反映。

			this.n累積転送バイト数 += num;
			return num;
		}

		private long nミキサーの1秒あたりのバイト数 = 0;
		private long n累積転送バイト数 = 0;
		private bool bIsBASSFree = true;
	}
}
