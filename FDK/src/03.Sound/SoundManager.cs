using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ManagedBass;
using Silk.NET.Windowing;


namespace FDK;

public class SoundManager   // : CSound
{
	public static bool IsDeviceTypeSupported(ESoundDeviceType type)
		=> type is ESoundDeviceType.Bass
			or ESoundDeviceType.ASIO
			or ESoundDeviceType.ExclusiveWASAPI
			or ESoundDeviceType.SharedWASAPI;

	/// <summary>The devices tried in turn for a requested one: WASAPI and ASIO mean BASS outside Windows (a
	/// Config.ini copied from Windows); then the requested device, the ones after it in the setting's order,
	/// BASS when it was not among them, and none (Unknown) last.</summary>
	public static List<ESoundDeviceType> DeviceTryOrder(ESoundDeviceType requested, bool isWindows) {
		ESoundDeviceType[] order = {
			ESoundDeviceType.Bass,
			ESoundDeviceType.ExclusiveWASAPI,
			ESoundDeviceType.SharedWASAPI,
			ESoundDeviceType.ASIO,
		};
		if (!isWindows && requested is ESoundDeviceType.ExclusiveWASAPI or ESoundDeviceType.SharedWASAPI or ESoundDeviceType.ASIO)
			requested = ESoundDeviceType.Bass;
		var list = new List<ESoundDeviceType>();
		int start = Array.IndexOf(order, requested);
		if (start >= 0) {
			for (int i = start; i < order.Length; i++)
				list.Add(order[i]);
		}
		if (!list.Contains(ESoundDeviceType.Bass))
			list.Add(ESoundDeviceType.Bass);
		list.Add(ESoundDeviceType.Unknown);
		return list;
	}

	private static ISoundDevice? SoundDevice {
		get; set;
	}
	private static ESoundDeviceType SoundDeviceType {
		get; set;
	}
	[AllowNull] public static CSoundTimer PlayTimer = null!;   // set when the sound device opens; null again after tEnd
	public static bool bUseOSTimer = false;     // OSのタイマーを使うか、CSoundTimerを使うか。DTXCではfalse, DTXManiaではtrue。
												// DTXC(DirectSound)でCSoundTimerを使うと、内部で無音のループサウンドを再生するため
												// サウンドデバイスを占有してしまい、Viewerとして呼び出されるDTXManiaで、ASIOが使えなくなる。

	// DTXMania単体でこれをtrueにすると、WASAPI/ASIO時に演奏タイマーとしてFDKタイマーではなく
	// システムのタイマーを使うようになる。こうするとスクロールは滑らかになるが、音ズレが出るかもしれない。

	public static bool bIsTimeStretch = false;

	/// <summary>Silences the output device's master mixer (offline video export). Per-channel volume
	/// attributes are untouched, so the exporter's sound-event capture still reads true levels.</summary>
	public static void MuteMasterOutput() {
		try { if (SoundDevice != null) SoundDevice.nMasterVolume = 0; } catch { /* best effort */ }
	}

	private static IWindow Window_ = null!;

	private static int _nMasterVolume;
	public int nMasterVolume {
		get {
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
		set {
			SoundDevice!.nMasterVolume = value;
			_nMasterVolume = value;
		}
	}

	///// <summary>
	///// BASS時、mp3をストリーミング再生せずに、デコードしたraw wavをオンメモリ再生する場合はtrueにする。
	///// 特殊なmp3を使用時はシークが乱れるので、必要に応じてtrueにすること。(Config.iniのNoMP3Streamingで設定可能。)
	///// ただし、trueにすると、その分再生開始までの時間が長くなる。
	///// </summary>
	//public static bool bIsMP3DecodeByWindowsCodec = false;

	// unused (for debugging only?)
	public static int nMixing = 0;
	public int GetMixingStreams() {
		return nMixing;
	}
	public static int nStreams = 0;
	public int GetStreams() {
		return nStreams;
	}
	#region [ WASAPI/ASIO/DirectSound設定値 ]
	/// <summary>
	/// <para>WASAPI 排他モード出力における再生遅延[ms]（の希望値）。最終的にはこの数値を基にドライバが決定する）。</para>
	/// <para>0以下の値を指定すると、この数値はWASAPI初期化時に自動設定する。正数を指定すると、その値を設定しようと試みる。</para>
	/// </summary>
	public static int SoundDelayExclusiveWASAPI = 0;        // SSTでは、50ms
	public int GetSoundExclusiveWASAPI() {
		return SoundDelayExclusiveWASAPI;
	}
	public void SetSoundDelayExclusiveWASAPI(int value) {
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
	public static int SoundDelayASIO = 0;                       // 0にすると、デバイスの設定値をそのまま使う。
	public int GetSoundDelayASIO() {
		return SoundDelayASIO;
	}
	public void SetSoundDelayASIO(int value) {
		SoundDelayASIO = value;
	}
	public static int ASIODevice = 0;
	public int GetASIODevice() {
		return ASIODevice;
	}
	public void SetASIODevice(int value) {
		ASIODevice = value;
	}
	/// <summary>
	/// <para>DirectSound 出力における再生遅延[ms]。ユーザが決定する。</para>
	/// </summary>
	public static int SoundDelayDirectSound = 100;

	public long GetSoundDelay() {
		if (SoundDevice != null) {
			return SoundDevice.BufferSize;
		} else {
			return -1;
		}
	}

	public static long nBytesPerSec => SoundDevice!.nBytesPerSec;
	#endregion


	/// <summary>
	/// DTXMania用コンストラクタ
	/// </summary>
	/// <param name="handle"></param>
	/// <param name="soundDeviceType"></param>
	/// <param name="nSoundDelayExclusiveWASAPI"></param>
	/// <param name="nSoundDelayASIO"></param>
	/// <param name="nASIODevice"></param>
	public SoundManager(IWindow window, ESoundDeviceType soundDeviceType, int nSoundDelayBASS, int nSoundDelayExclusiveWASAPI, int nSoundDelayASIO, int nASIODevice, bool _bUseOSTimer) {
		Window_ = window;
		SoundDevice = null;
		//bUseOSTimer = false;
		tInitialize(soundDeviceType, nSoundDelayBASS, nSoundDelayExclusiveWASAPI, nSoundDelayASIO, nASIODevice, _bUseOSTimer);
	}

	public void Dispose() {
		tEnd();
	}

	//public static void t初期化()
	//{
	//    t初期化( ESoundDeviceType.DirectSound, 0, 0, 0 );
	//}

	public void tInitialize(ESoundDeviceType soundDeviceType, int _nSoundDelayBASS, int _nSoundDelayExclusiveWASAPI, int _nSoundDelayASIO, int _nASIODevice, IntPtr handle) {
		//if ( !bInitialized )
		{
			tInitialize(soundDeviceType, _nSoundDelayBASS, _nSoundDelayExclusiveWASAPI, _nSoundDelayASIO, _nASIODevice);
			//bInitialized = true;
		}
	}
	public void tInitialize(ESoundDeviceType soundDeviceType, int _nSoundDelayBASS, int _nSoundDelayExclusiveWASAPI, int _nSoundDelayASIO, int _nASIODevice) {
		tInitialize(soundDeviceType, _nSoundDelayBASS, _nSoundDelayExclusiveWASAPI, _nSoundDelayASIO, _nASIODevice, false);
	}

	public void tInitialize(ESoundDeviceType soundDeviceType, int _nSoundDelayBASS, int _nSoundDelayExclusiveWASAPI, int _nSoundDelayASIO, int _nASIODevice, bool _bUseOSTimer) {
		//SoundDevice = null;						// 後で再初期化することがあるので、null初期化はコンストラクタに回す
		// a running play timer stays: tReloadSoundDeviceAndSound moves it to the new device, and the input thread
		// reads it at every key press and release while the device is being rebuilt
		nMixing = 0;

		SoundDelayBASS = _nSoundDelayBASS;
		SoundDelayExclusiveWASAPI = _nSoundDelayExclusiveWASAPI;
		SoundDelaySharedWASAPI = _nSoundDelayExclusiveWASAPI;
		SoundDelayASIO = _nSoundDelayASIO;
		ASIODevice = _nASIODevice;
		bUseOSTimer = _bUseOSTimer;

		requestedDeviceType = soundDeviceType;
		if (!OperatingSystem.IsWindows() && soundDeviceType is ESoundDeviceType.ExclusiveWASAPI or ESoundDeviceType.SharedWASAPI or ESoundDeviceType.ASIO)
			Trace.TraceInformation($"{soundDeviceType} is only available on Windows; using BASS.");
		foreach (var deviceType in DeviceTryOrder(soundDeviceType, OperatingSystem.IsWindows())) {
			SoundDeviceType = deviceType;
			try {
				tReloadSoundDeviceAndSound();
				break;
			} catch (Exception e) {
				Trace.TraceError(e.ToString());
				Trace.TraceError("例外が発生しましたが処理を継続します。 (2609806d-23e8-45c2-9389-b427e80915bc)");
				if (deviceType == ESoundDeviceType.Unknown) {
					Trace.TraceError(string.Format("サウンドデバイスの初期化に失敗しました。"));
					break;
				}
			}
		}
		onSilentDevice = false;
		WatchOutputDevices();
		if (IsDeviceTypeSupported(soundDeviceType)) {
			//Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_UPDATETHREADS, 4 );
			//Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0 );

			Trace.TraceInformation("BASS_CONFIG_UpdatePeriod=" + Bass.GetConfig(Configuration.UpdatePeriod));
			Trace.TraceInformation("BASS_CONFIG_UpdateThreads=" + Bass.GetConfig(Configuration.UpdateThreads));
		}
	}

	public void tDisableUpdateBufferAutomatically() {
		//Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_UPDATETHREADS, 0 );
		//Bass.BASS_SetConfig( BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0 );

		//Trace.TraceInformation( "BASS_CONFIG_UpdatePeriod=" + Bass.BASS_GetConfig( BASSConfig.BASS_CONFIG_UPDATEPERIOD ) );
		//Trace.TraceInformation( "BASS_CONFIG_UpdateThreads=" + Bass.BASS_GetConfig( BASSConfig.BASS_CONFIG_UPDATETHREADS ) );
	}


	public static void tEnd() {
		SoundDevice!.Dispose();
		PlayTimer.Dispose();    // Global.Bass を解放した後に解放すること。（Global.Bass で参照されているため）
		PlayTimer = null;       // a later tInitialize starts a new one
	}


	public static void tReloadSoundDeviceAndSound() {
		#region [ すでにサウンドデバイスと演奏タイマが構築されていれば解放する。]
		//-----------------
		if (SoundDevice != null) {
			// an ASIO driver stops asking for sound before anything it reads is released
			(SoundDevice as CSoundDeviceASIO)?.StopOutput();

			// what goes on across the new device, read while the old one is still there: the play timer's time,
			// what each sound was doing, and how long the old output has been silent (so the sounds go on from
			// where they would be)
			if (IsDeviceTypeSupported(SoundDevice.SoundDeviceType)) {
				carriedTimerMs = PlayTimer?.SystemTimeMs_Double;
				carriedOutageMs = SoundDevice.UpdateSystemTimeMs == CTimer.UnusedNum ? 0.0
					: Math.Max(0.0, SoundDevice.SystemTimer.SystemTimeMs_Double - SoundDevice.dbUpdateSystemTimeMs);
				carriedStates = CSound.tCaptureStates();
				carriedAtTimestamp = Stopwatch.GetTimestamp();
			}

			// すでに生成済みのサウンドがあれば初期状態に戻す。

			CSound.tResetAllSound();        // リソースは解放するが、CSoundのインスタンスは残す。


			// サウンドデバイスを解放する。演奏タイマは残し、新しいデバイスに付け替える。

			SoundDevice.Dispose();
			SoundDevice = null;   // a failed rebuild below must not close it twice
		}
		//-----------------
		#endregion

		#region [ 新しいサウンドデバイスを構築する。]
		//-----------------
		switch (SoundDeviceType) {
			case ESoundDeviceType.Bass:
				SoundDevice = new CSoundDeviceBASS(updatePeriod: SoundUpdatePeriodBASS, bufferSize: SoundDelayBASS, device: silentFallback ? 0 : -1);
				break;

			case ESoundDeviceType.ExclusiveWASAPI:
				SoundDevice = new CSoundDeviceWASAPI(CSoundDeviceWASAPI.EWASAPIMode.Exclusion, SoundDelayExclusiveWASAPI, SoundUpdatePeriodExclusiveWASAPI);
				break;

			case ESoundDeviceType.SharedWASAPI:
				SoundDevice = new CSoundDeviceWASAPI(CSoundDeviceWASAPI.EWASAPIMode.Share, SoundDelaySharedWASAPI, SoundUpdatePeriodSharedWASAPI);
				break;

			case ESoundDeviceType.ASIO:
				SoundDevice = new CSoundDeviceASIO(SoundDelayASIO, ASIODevice);
				break;

			default:
				throw new Exception(string.Format("未対応の SoundDeviceType です。[{0}]", SoundDeviceType.ToString()));
		}
		//-----------------
		#endregion
		#region [ 新しい演奏タイマを構築する。]
		//-----------------
		if (PlayTimer == null)
			PlayTimer = new CSoundTimer(SoundDevice);
		else
			PlayTimer.SwitchDevice(SoundDevice, carriedTimerMs ?? 0.0);
		//-----------------
		#endregion

		SoundDevice.nMasterVolume = _nMasterVolume;                 // サウンドデバイスに対して、マスターボリュームを再設定する

		CSound.tReloadSound(SoundDevice);       // すでに生成済みのサウンドがあれば作り直す。
		// the sounds go on where they would be: the old output's silence plus the rebuild itself (real time:
		// the game's clock stands still meanwhile)
		if (carriedStates != null)
			CSound.tRestoreStates(carriedStates, carriedOutageMs + Stopwatch.GetElapsedTime(carriedAtTimestamp).TotalMilliseconds);
		carriedTimerMs = null;
		carriedStates = null;
		carriedOutageMs = 0.0;
	}
	private static double? carriedTimerMs;
	private static List<CSound.PlayState>? carriedStates;
	private static double carriedOutageMs;
	private static long carriedAtTimestamp;
	private static bool silentFallback;

	#region [ Output device changes ]
	// Windows tells which output is the default and when an output goes away (unplugged headphones). The
	// sound device is then rebuilt on the current default, on the game's thread (tCheckDeviceChange), once
	// the notifications have settled. When no output can be opened, a silent device keeps the timer and the
	// sounds going until Windows reports an output again.
	private const double DeviceChangeSettleMs = 400;
	private static ManagedBass.Wasapi.WasapiNotifyProcedure? notifyProc;   // referenced for as long as BASSWASAPI may call it
	private static long deviceChangeTick = -1;   // Environment.TickCount64 of the last change; -1: none pending
	private static ESoundDeviceType requestedDeviceType = ESoundDeviceType.Unknown;

	private static void WatchOutputDevices() {
		if (!OperatingSystem.IsWindows() || notifyProc != null) return;
		notifyProc = (notify, device, user) => {
			if (notify is ManagedBass.Wasapi.WasapiNotificationType.DefaultOutput
				or ManagedBass.Wasapi.WasapiNotificationType.Disabled
				or ManagedBass.Wasapi.WasapiNotificationType.Enabled
				or ManagedBass.Wasapi.WasapiNotificationType.Fail)
				Interlocked.Exchange(ref deviceChangeTick, Environment.TickCount64);
		};
		try {
			if (!ManagedBass.Wasapi.BassWasapi.SetNotify(notifyProc, IntPtr.Zero))
				Trace.TraceWarning($"Output device changes are not followed (BASS_WASAPI_SetNotify: {Bass.LastError}).");
		} catch (Exception e) {
			Trace.TraceWarning("Output device changes are not followed: " + e.Message);
		}
	}

	/// <summary>True when an output device change is waiting to be applied by <see cref="tCheckDeviceChange"/>.</summary>
	public static bool IsDeviceChangeDue {
		get {
			long tick = Interlocked.Read(ref deviceChangeTick);
			return tick >= 0 && Environment.TickCount64 - tick >= DeviceChangeSettleMs;
		}
	}

	/// <summary>Rebuilds the sound device on the current default output when Windows reported a change, or the
	/// ASIO device when its driver asked for a reset or stopped. Call from the thread that plays the sounds, with
	/// nothing else playing them meanwhile.</summary>
	public static void tCheckDeviceChange() {
		if (!IsDeviceChangeDue) return;
		Interlocked.Exchange(ref deviceChangeTick, -1);
		if (requestedDeviceType is ESoundDeviceType.Unknown) return;
		if (requestedDeviceType is ESoundDeviceType.ASIO) {
			string? reason = AsioReopenReason();
			if (reason == null) return;
			Trace.TraceInformation($"Reopening the sound device (ASIO): {reason}.");
		} else {
			if (!OutputChanged()) return;   // a microphone, or an output not in use
			Trace.TraceInformation($"The output device changed; reopening the sound device ({requestedDeviceType}).");
		}
		foreach (var deviceType in DeviceTryOrder(requestedDeviceType, OperatingSystem.IsWindows())) {
			if (deviceType is ESoundDeviceType.Unknown) continue;
			if (deviceType is ESoundDeviceType.ASIO && requestedDeviceType is not ESoundDeviceType.ASIO) continue;
			SoundDeviceType = deviceType;
			try {
				tReloadSoundDeviceAndSound();
				onSilentDevice = false;
				Trace.TraceInformation($"Sound device reopened ({deviceType}).");
				return;
			} catch (Exception e) {
				Trace.TraceWarning($"Reopening the sound device as {deviceType} failed: {e.Message}");
			}
		}
		// no output opens (none is plugged in): a silent device until Windows reports one again
		SoundDeviceType = ESoundDeviceType.Bass;
		silentFallback = true;
		try {
			tReloadSoundDeviceAndSound();
			onSilentDevice = true;
			Trace.TraceWarning("No output device can be opened; sounds play silently until one is available.");
		} catch (Exception e) {
			Trace.TraceError("Even the silent sound device could not be opened: " + e);
		} finally {
			silentFallback = false;
		}
	}
	private static bool onSilentDevice;

	// BASSASIO calls this on the driver's thread when the driver asks to be reset (its device went away or
	// changed) or changes its sample rate: the game thread looks at the driver once the notifications settled
	internal static readonly ManagedBass.Asio.AsioNotifyProcedure AsioNotify = (notify, user) => {
		Interlocked.Or(ref asioNotifications, 1 << (int)notify);
		Interlocked.Exchange(ref deviceChangeTick, Environment.TickCount64);
	};
	private static int asioNotifications;   // one bit per kind of notification received since the last look
	private const long AsioStallMs = 250;

	// ASIO names its device, so a change of the default output does not concern it. It is reopened when it no
	// longer asks for sound, when its sample rate changed, or when it could not be opened before. A reset request
	// alone is not followed while the driver still asks for sound: some drivers crash when they are closed while
	// they change their device. Returns why it is reopened, or null.
	private static string? AsioReopenReason() {
		int notified = Interlocked.Exchange(ref asioNotifications, 0);
		if (SoundDevice is not CSoundDeviceASIO asio || asio.SoundDeviceType != ESoundDeviceType.ASIO) return "it is not open";
		long now = Environment.TickCount64;
		bool opening = now - asio.OpenedTick <= AsioOpeningMs;
		long silentMs = now - asio.LastCallbackTick;
		// a driver that has not begun asking for sound yet is not stalled during its first second
		if (silentMs > AsioStallMs && (asio.HasCalledBack || !opening)) return $"the driver stopped asking for sound {silentMs} ms ago";
		// some drivers ask for a reset as they open: that one is not followed, or the device would reopen in a loop
		if (opening) return null;
		if ((notified & (1 << (int)ManagedBass.Asio.AsioNotify.Rate)) != 0) return "the driver changed its sample rate";
		if ((notified & (1 << (int)ManagedBass.Asio.AsioNotify.Reset)) != 0)
			Trace.TraceInformation($"The ASIO driver asked for a reset; it still asks for sound ({silentMs} ms ago), so it is left open.");
		return null;
	}
	private const long AsioOpeningMs = 1000;

	// the output in use is gone, the default output is another one, or there is one again after none
	private static bool OutputChanged() {
		string? defaultOutput = DefaultOutputName();
		if (onSilentDevice) return defaultOutput != null;
		var (name, usable) = CurrentOutput();
		if (name == null) return false;   // not a device that can be followed
		return !usable || (defaultOutput != null && name != defaultOutput);
	}

	// the system's default output, by name (the names BASS and BASSWASAPI give the same device are equal);
	// BASS's own "Default" and "No sound" entries, which have no driver, are not outputs of their own
	private static string? DefaultOutputName() {
		for (int i = 1; Bass.GetDeviceInfo(i, out var info); i++) {
			if (info.IsDefault && info.IsEnabled && !string.IsNullOrEmpty(info.Driver)) return info.Name;
		}
		return null;
	}

	private static (string? name, bool usable) CurrentOutput() {
		try {
			if (SoundDevice is CSoundDeviceWASAPI) {
				int dev = ManagedBass.Wasapi.BassWasapi.CurrentDevice;
				if (!ManagedBass.Wasapi.BassWasapi.GetDeviceInfo(dev, out var info)) return ("", false);
				return (info.Name, info.IsEnabled && !info.IsUnplugged && !info.IsDisabled && ManagedBass.Wasapi.BassWasapi.IsStarted);
			}
			if (SoundDevice is CSoundDeviceBASS) {
				int dev = Bass.CurrentDevice;
				if (dev < 1 || !Bass.GetDeviceInfo(dev, out var info)) return ("", false);
				// BASS's "Default" device (what BASS opens) moves to the default output by itself
				if (string.IsNullOrEmpty(info.Driver)) return (null, true);
				return (info.Name, info.IsEnabled);
			}
		} catch (Exception e) {
			Trace.TraceWarning("Could not read the output device's state: " + e.Message);
		}
		return (null, true);
	}
	#endregion

	// a sound fed from code through a stream procedure (a video's audio track)
	public CSound tCreateUserSound(int frequency, int channels, double durationSeconds, StreamProcedure proc, ESoundGroup soundGroup) {
		if (SoundDeviceType == ESoundDeviceType.Unknown) {
			throw new Exception(string.Format("未対応の SoundDeviceType です。[{0}]", SoundDeviceType.ToString()));
		}
		return SoundDevice!.tCreateUserSound(frequency, channels, durationSeconds, proc, soundGroup);
	}

	public CSound? tCreateSound(string filename, ESoundGroup soundGroup) {
		if (!File.Exists(filename)) {
			Trace.TraceWarning($"[i18n] File does not exist: {filename}");
			return null;
		}

		if (SoundDeviceType == ESoundDeviceType.Unknown) {
			throw new Exception(string.Format("未対応の SoundDeviceType です。[{0}]", SoundDeviceType.ToString()));
		}
		try {
			return SoundDevice!.tCreateSound(filename, soundGroup);
		} catch (Exception ex) {
			Trace.TraceWarning($"Failed to create sound: {System.IO.Path.GetFileName(filename)}: {ex.Message}");
			return null;
		}
	}

	private static DateTime lastUpdateTime = DateTime.MinValue;

	public void tDisposeSound(CSound? csound) {
		csound?.tDispose(true);         // インスタンスは存続→破棄にする。
	}

	public string GetCurrentSoundDeviceType() {
		switch (SoundDeviceType) {
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

	public void AddMixer(CSound cs, double dbPlaybackSpeed, bool _bPlayEndAfterPlaybackContinuesChip) {
		cs.bPlayEndAfterPlaybackContinuesChip = _bPlayEndAfterPlaybackContinuesChip;
		cs.PlaySpeed = dbPlaybackSpeed;
		cs.AddBassSoundFromMixer();
	}
	public void AddMixer(CSound cs, double dbPlaybackSpeed) {
		cs.PlaySpeed = dbPlaybackSpeed;
		cs.AddBassSoundFromMixer();
	}
	public void AddMixer(CSound cs) {
		cs.AddBassSoundFromMixer();
	}
	public void RemoveMixer(CSound cs) {
		cs.tRemoveFromMixer();
	}
}
