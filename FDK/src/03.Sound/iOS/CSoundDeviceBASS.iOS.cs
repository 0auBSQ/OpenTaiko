using ManagedBass;

namespace FDK;

partial class CSoundDeviceBASS {
	/// <summary>
	/// Hardware output latency in milliseconds, used to size the audio buffer; 0 falls back to BASS's measured latency.
	/// </summary>
	public static int iOSHardwareLatencyMs = 0;

	/// <summary>
	/// Configures BASS to play sounds straight through Bass.ChannelPlay rather than a StreamProc/mixer
	/// pipeline (BassMix produces no audio on iOS). MixerHandle = 0 tells CSound to play directly.
	/// </summary>
	private void InitializeDirectPlayback() {
		Bass.Configure(Configuration.DeviceBufferLength, 15);
		Bass.Configure(Configuration.LogarithmicVolumeCurve, true);

		this.MixerHandle = CSound.NoMixerHandle;
		this.Mixer_DeviceOut = -1;
		this.MainStreamHandle = -1;
		this.IsBASSSoundFree = false;
		this.SoundDeviceType = ESoundDeviceType.Bass;

		if (!Bass.Start()) {
			Errors err = Bass.LastError;
			Bass.Free();
			this.IsBASSSoundFree = true;
			throw new Exception("BASS デバイス出力開始に失敗しました。" + err.ToString());
		}

		Bass.GetInfo(out var info);
		int latencyMs = iOSHardwareLatencyMs > 0 ? iOSHardwareLatencyMs : info.Latency + 20;
		this.BufferSize = this.OutputDelay = latencyMs;

		// Drive CSoundTimer from the system clock (no mixer feeds it).
		this.UpdateSystemTimeMs = this.SystemTimer.SystemTimeMs;
	}
}
