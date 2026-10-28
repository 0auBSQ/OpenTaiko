using System.Diagnostics;

namespace FDK;

public class CSoundTimer : CTimerBase {
	private bool IsDeviceTypeSupported() => SoundManager.IsDeviceTypeSupported(this.Device.SoundDeviceType);

	private long SystemTimeMsBase(Func<long> getOSTime) {
		if (this.IsDeviceTypeSupported()) {
			// a device that has just replaced another: the double path carries the time over until it plays
			if (this.rebasePending)
				return (long)this.SystemTimsMsBase_Double(() => getOSTime());

			// BASS 系の ISoundDevice.n経過時間ms はオーディオバッファの更新間隔ずつでしか更新されないため、単にこれを返すだけではとびとびの値になる。
			// そこで、更新間隔の最中に呼ばれた場合は、システムタイマを使って補間する。
			// この場合の経過時間との誤差は更新間隔以内に収まるので問題ないと判断する。
			// ただし、ASIOの場合は、転送byte数から時間算出しているため、ASIOの音声合成処理の負荷が大きすぎる場合(処理時間が実時間を超えている場合)は
			// 動作がおかしくなる。(具体的には、ここで返すタイマー値の逆行が発生し、スクロールが巻き戻る)
			// この場合の対策は、ASIOのバッファ量を増やして、ASIOの音声合成処理の負荷を下げること。

			if (this.Device.UpdateSystemTimeMs == CTimer.UnusedNum) // #33890 2014.5.27 yyagi
			{
				// 環境によっては、ASIOベースの演奏タイマーが動作する前(つまりASIOのサウンド転送が始まる前)に
				// DTXデータの演奏が始まる場合がある。
				// その場合、"this.Device.n経過時間を更新したシステム時刻" が正しい値でないため、
				// 演奏タイマの値が正しいものとはならない。そして、演奏タイマーの動作が始まると同時に、
				// 演奏タイマの値がすっ飛ぶ(極端な負の値になる)ため、演奏のみならず画面表示もされない状態となる。
				// (画面表示はタイマの値に連動して行われるが、0以上のタイマ値に合わせて動作するため、
				//  不の値が来ると画面に何も表示されなくなる)

				// そこで、演奏タイマが動作を始める前(this.Device.n経過時間を更新したシステム時刻ms == CTimer.n未使用)は、
				// 補正部分をゼロにして、n経過時間msだけを返すようにする。
				// こうすることで、演奏タイマが動作を始めても、破綻しなくなる。
				return this.Device.ElapsedTimeMs + (long)this.msDeviceOffset;
			} else {
				if (FDK.SoundManager.bUseOSTimer) {
					return getOSTime(); // match TimerType.GameTimeAtDraw behavior
				} else {
					return this.Device.ElapsedTimeMs
						   + (this.Device.SystemTimer.SystemTimeMs - this.Device.UpdateSystemTimeMs)
						   + (long)this.msDeviceOffset;
				}
			}
		}
		return CTimerBase.UnusedNum;
	}

	public override long SystemTimeMs => SystemTimeMsBase(() => Game.TimeMs);
	public override long SystemTimeMsReal => SystemTimeMsBase(() => Game.TimeMsReal);

	private double SystemTimsMsBase_Double(Func<double> getOSTime) {
		if (this.IsDeviceTypeSupported()) {
			if (this.rebasePending) {
				double now = this.Device.SystemTimer.SystemTimeMs_Double;
				double sinceSwitch = now - this.msSwitchSystemTime;
				if (double.IsNaN(this.msFirstPendingRead)) this.msFirstPendingRead = now;
				// on with the clock until the new device has played, in an update stamped after the clock moved
				// on: the ones stamped while the game thread was rebuilding the device carry its stale time
				if (this.Device.UpdateSystemTimeMs == CTimer.UnusedNum || this.Device.dbUpdateSystemTimeMs <= this.msFirstPendingRead)
					return this.msCarried + sinceSwitch;
				// from here its own count goes on from the time reached, whatever it starts at (its first value
				// is less its latency or unplayed buffer)
				this.rebasePending = false;
				this.msDeviceOffset = this.msCarried + sinceSwitch
					- (this.Device.dbElapsedTimeMs + (this.Device.SystemTimer.SystemTimeMs_Double - this.Device.dbUpdateSystemTimeMs));
			}
			if (this.Device.UpdateSystemTimeMs == CTimer.UnusedNum) {
				return this.Device.dbElapsedTimeMs + this.msDeviceOffset;
			} else {
				if (FDK.SoundManager.bUseOSTimer) {
					return getOSTime();
				} else {
					return this.Device.dbElapsedTimeMs
						   + (this.Device.SystemTimer.SystemTimeMs_Double - this.Device.dbUpdateSystemTimeMs)
						   + this.msDeviceOffset;
				}
			}
		}
		return CTimerBase.UnusedNum;
	}

	// The output device was rebuilt: the new one counts its time from zero, so the timer goes on from the
	// time it had (msCarried, read before the old device closed) instead of jumping back with it.
	private double msDeviceOffset;
	private bool rebasePending;
	private double msCarried, msSwitchSystemTime, msFirstPendingRead;
	internal void SwitchDevice(ISoundDevice device, double msCarried) {
		this.Device = device;
		this.msDeviceOffset = 0;
		this.msCarried = msCarried;
		this.msSwitchSystemTime = device.SystemTimer.SystemTimeMs_Double;
		this.msFirstPendingRead = double.NaN;
		this.rebasePending = true;
	}

	public override double SystemTimeMs_Double => SystemTimsMsBase_Double(() => Game.dbTimeMs);
	public override double SystemTimeMsReal_Double => SystemTimsMsBase_Double(() => Game.dbTimeMsReal);

	internal CSoundTimer(ISoundDevice device) {
		this.Device = device;
		ctDInputTimer = new CTimer(CTimer.TimerType.GameTimeReal);
	}

	public override void Update() {
		base.Update();
		// snap timers regularly, at integer frame
		if (this.UpdateSystemTime - this.msSoundTimerOffset >= msSnapTimersInternal) {
			this.SnapTimers();
		}
	}


	const int msSnapTimersInternal = 1000;

	private void SnapTimers() {
		this.msDInputTimerOffset = this.ctDInputTimer.SystemTimeMsReal;
		this.msSoundTimerOffset = this.SystemTimeMsReal;
	}
	public long msGetPreciseNowSoundTimerTime()
		=> this.msDInputTimeToSoundTimerTime(this.ctDInputTimer.SystemTimeMsReal);
	private long msDInputTimeToSoundTimerTime(long msDInputTime) {
		return msDInputTime - this.msDInputTimerOffset + this.msSoundTimerOffset;    // Timer違いによる時差を補正する
	}

	public override void Dispose() {
		this.ctDInputTimer?.Dispose();
	}

	internal ISoundDevice Device = null;    // debugのため、一時的にprotectedをpublicにする。後で元に戻しておくこと。
											//protected Thread thSendInput = null;
											//protected Thread thSnapTimers = null;
	private CTimer ctDInputTimer = null;
	private long msDInputTimerOffset = 0;
	private long msSoundTimerOffset = 0;
}
