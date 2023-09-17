using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using ManagedBass;
using ManagedBass.Mix;

namespace FDK
{
	public class CSoundDeviceBASS : ISoundDevice
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

		public float CPUUsage => (float)Bass.CPUUsage;

		// マスターボリュームの制御コードは、WASAPI/ASIOで全く同じ。
		public int nMasterVolume
		{
			get
			{
				float fVolume = 0.0f;
				bool b = Bass.ChannelGetAttribute(this.MixerHandle, ChannelAttribute.Volume, out fVolume);
				if (!b)
				{
					Errors be = Bass.LastError;
					Trace.TraceInformation("BASS Master Volume Get Error: " + be.ToString());
				}
				return (int)(fVolume * 100);
			}
			set
			{
				bool b = Bass.ChannelSetAttribute(this.MixerHandle, ChannelAttribute.Volume, (float)(value / 100.0));
				if (!b)
				{
					Errors be = Bass.LastError;
					Trace.TraceInformation("BASS Master Volume Set Error: " + be.ToString());
				}
			}
		}

		public CSoundDeviceBASS(int updatePeriod, int bufferSize)
		{
			Trace.TraceInformation("Start initialization of BASS");
			this.SoundDeviceType = ESoundDeviceType.Unknown;
			this.OutputDelay = 0;
			this.ElapsedTimeMs = 0;
			this.UpdateSystemTimeMs = CTimer.UnusedNum;
			this.SystemTimer = new CTimer(CTimer.TimerType.MultiMedia);

			this.IsBASSSoundFree = true;

			// BASS の初期化。

			int freq = 44100;
			
			if (!Bass.Init(-1, freq, DeviceInitFlags.Default))
				throw new Exception(string.Format("BASS の初期化に失敗しました。(BASS_Init)[{0}]", Bass.LastError.ToString()));
			
			if (!Bass.Configure(Configuration.UpdatePeriod, updatePeriod))
			{
				Trace.TraceWarning($"BASS_SetConfig({nameof(Configuration.UpdatePeriod)}) に失敗しました。[{Bass.LastError}]");
			}
			if (!Bass.Configure(Configuration.UpdateThreads, 1))
			{
				Trace.TraceWarning($"BASS_SetConfig({nameof(Configuration.UpdateThreads)}) に失敗しました。[{Bass.LastError}]");
			}
			
			Bass.Configure(Configuration.PlaybackBufferLength, bufferSize);
			Bass.Configure(Configuration.LogarithmicVolumeCurve, true);

			this.STREAMPROC = new StreamProcedure(StreamProc);
			this.MainStreamHandle = Bass.CreateStream(freq, 2, BassFlags.Default, this.STREAMPROC, IntPtr.Zero);

			var flag = BassFlags.MixerNonStop| BassFlags.Decode;   // デコードのみ＝発声しない。
			this.MixerHandle = BassMix.CreateMixerStream(freq, 2, flag);

			if (this.MixerHandle == 0)
			{
				Errors err = Bass.LastError;
				Bass.Free();
				this.IsBASSSoundFree = true;
				throw new Exception(string.Format("BASSミキサ(mixing)の作成に失敗しました。[{0}]", err));
			}

			// BASS ミキサーの1秒あたりのバイト数を算出。

			this.IsBASSSoundFree = false;

			var mixerInfo = Bass.ChannelGetInfo(this.MixerHandle);
			int bytesPerSample = 2;
			long mixer_BlockAlign = mixerInfo.Channels * bytesPerSample;
			this.Mixer_BytesPerSec = mixer_BlockAlign * mixerInfo.Frequency;

			// 単純に、hMixerの音量をMasterVolumeとして制御しても、
			// ChannelGetData()の内容には反映されない。
			// そのため、もう一段mixerを噛ませて、一段先のmixerからChannelGetData()することで、
			// hMixerの音量制御を反映させる。
			Mixer_DeviceOut = BassMix.CreateMixerStream(
				freq, 2, flag);
			if (this.Mixer_DeviceOut == 0)
			{
				Errors errcode = Bass.LastError;
				Bass.Free();
				this.IsBASSSoundFree = true;
				throw new Exception(string.Format("BASSミキサ(最終段)の作成に失敗しました。[{0}]", errcode));
			}
			{
				bool b1 = BassMix.MixerAddChannel(this.Mixer_DeviceOut, this.MixerHandle, BassFlags.Default);
				if (!b1)
				{
					Errors errcode = Bass.LastError;
					Bass.Free();
					this.IsBASSSoundFree = true;
					throw new Exception(string.Format("BASSミキサ(最終段とmixing)の接続に失敗しました。[{0}]", errcode));
				};
			}

			this.SoundDeviceType = ESoundDeviceType.Bass;

			// 出力を開始。

			if (!Bass.Start())     // 範囲外の値を指定した場合は自動的にデフォルト値に設定される。
			{
				Errors err = Bass.LastError;
				Bass.Free();
				this.IsBASSSoundFree = true;
				throw new Exception("BASS デバイス出力開始に失敗しました。" + err.ToString());
			}
			else
			{
				Bass.GetInfo(out var info);

				this.BufferSize = this.OutputDelay = info.Latency + bufferSize;//求め方があっているのだろうか…

				Trace.TraceInformation("BASS デバイス出力開始:[{0}ms]", this.OutputDelay);
			}

			Bass.ChannelPlay(this.MainStreamHandle, false);

		}

		#region [ tCreateSound() ]
		public CSound tCreateSound(string strFilename, ESoundGroup soundGroup)
		{
			var sound = new CSound(soundGroup);
			sound.CreateBassSound(strFilename, this.MixerHandle);
			return sound;
		}

		public void tCreateSound(string strFilename, CSound sound)
		{
			sound.CreateBassSound(strFilename, this.MixerHandle);
		}
		#endregion


		#region [ Dispose-Finallizeパターン実装 ]
		//-----------------
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected void Dispose(bool bManagedDispose)
		{
			this.SoundDeviceType = ESoundDeviceType.Unknown;      // まず出力停止する(Dispose中にクラス内にアクセスされることを防ぐ)
			if (MainStreamHandle != -1)
			{
				Bass.StreamFree(this.MainStreamHandle);
			}
			if (MixerHandle != -1)
			{
				Bass.StreamFree(this.MixerHandle);
			}
			if (!this.IsBASSSoundFree)
			{
				Bass.Stop();
				Bass.Free();// システムタイマより先に呼び出すこと。（Stream処理() の中でシステムタイマを参照してるため）
			}

			if (bManagedDispose)
			{
				SystemTimer.Dispose();
				this.SystemTimer = null;
			}
		}
		~CSoundDeviceBASS()
		{
			this.Dispose(false);
		}
		//-----------------
		#endregion

		public int StreamProc(int handle, IntPtr buffer, int length, IntPtr user)
		{
			// BASSミキサからの出力データをそのまま ASIO buffer へ丸投げ。

			int num = Bass.ChannelGetData(this.Mixer_DeviceOut, buffer, length);      // num = 実際に転送した長さ

			if (num == -1) num = 0;

			// 経過時間を更新。
			// データの転送差分ではなく累積転送バイト数から算出する。

			this.ElapsedTimeMs = (this.TotalByteCount * 1000 / this.Mixer_BytesPerSec) - this.OutputDelay;
			this.UpdateSystemTimeMs = this.SystemTimer.SystemTimeMs;


			// 経過時間を更新後に、今回分の累積転送バイト数を反映。

			this.TotalByteCount += num;
			return num;
		}
		private long Mixer_BytesPerSec = 0;
		private long TotalByteCount = 0;

		protected int MainStreamHandle = -1;
		protected int MixerHandle = -1;
		protected int Mixer_DeviceOut = -1;
		protected StreamProcedure STREAMPROC = null;
		private bool IsBASSSoundFree = true;

		//WASAPIとASIOはLinuxでは使えないので、ここだけで良し
	}
}