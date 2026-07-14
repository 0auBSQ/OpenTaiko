using ManagedBass;

namespace FDK;

partial class CSoundDeviceBASS {
	/// <summary>
	/// The device's native output sample rate (Android AudioManager PROPERTY_OUTPUT_SAMPLE_RATE),
	/// filled by the Android host before the game constructs SoundManager. Initializing BASS at the
	/// native rate avoids a resampler between the mixer and the output stream.
	/// </summary>
	public static int AndroidSampleRate = 0;

	/// <summary>
	/// The device's native burst size in sample frames (AudioManager PROPERTY_OUTPUT_FRAMES_PER_BUFFER),
	/// filled by the Android host. Aligning BASS's device period to it lets the AAudio output run the
	/// LOW_LATENCY (fast-mixer) path instead of falling back to bigger, safer buffers.
	/// </summary>
	public static int AndroidFramesPerBuffer = 0;

	/// <summary>
	/// Android low-latency output configuration. MUST run before Bass.Init: BASS reads the device
	/// period/buffer configs when it opens the AAudio (Android 8.1+; OpenSL ES before that) stream.
	/// Initializing at the device's native rate removes the output resampler. The period/buffer stay
	/// moderate rather than single-burst: the mixer is fed by a managed StreamProcedure, and under
	/// the interpreter a 2-4ms deadline is easily missed (underruns/silence on real hardware, where
	/// bursts are far smaller than the emulator's). Burst-aligned when the burst info is available.
	/// Returns the sample rate Bass.Init should use.
	/// </summary>
	private static int ConfigureAndroidLowLatency() {
		int freq = AndroidSampleRate > 0 ? AndroidSampleRate : 48000;
		int periodMs = 10, devBufMs = 40;
		if (AndroidFramesPerBuffer > 0) {
			double burstMs = AndroidFramesPerBuffer * 1000.0 / freq;
			// Period = enough whole bursts to reach ~10ms; buffer = 3 periods of margin.
			int bursts = Math.Max(1, (int)Math.Ceiling(10.0 / burstMs));
			Bass.Configure(Configuration.DevicePeriod, -(bursts * AndroidFramesPerBuffer));
			periodMs = (int)Math.Ceiling(bursts * burstMs);
			devBufMs = Math.Max(30, periodMs * 3);
		}
		Bass.Configure(Configuration.DeviceBufferLength, devBufMs);
		System.Diagnostics.Trace.TraceInformation(
			$"Android audio config: rate={freq} burst={AndroidFramesPerBuffer} period~{periodMs}ms deviceBuffer={devBufMs}ms");
		return freq;
	}

	/// <summary>Reset the device configs to safe defaults and retry Bass.Init once (called when the
	/// tuned init fails on this device).</summary>
	private static bool RetryAndroidDefaultInit(out int freq) {
		Bass.Configure(Configuration.DevicePeriod, 10);
		Bass.Configure(Configuration.DeviceBufferLength, 60);
		freq = 48000;
		return Bass.Init(-1, freq, DeviceInitFlags.Default);
	}
}
