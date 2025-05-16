using System.Diagnostics;

namespace FDK;

public class CSoundTimer : CTimerBase {
	public override long SystemTimeMs {
		get {
			if (this.Device.SoundDeviceType == ESoundDeviceType.Bass ||
				this.Device.SoundDeviceType == ESoundDeviceType.ExclusiveWASAPI ||
				this.Device.SoundDeviceType == ESoundDeviceType.SharedWASAPI ||
				this.Device.SoundDeviceType == ESoundDeviceType.ASIO) {
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
					return this.Device.ElapsedTimeMs;
				} else {
					if (FDK.SoundManager.bUseOSTimer)
					{
						return Game.TimeMs; // match TimerType.MultiMedia behavior
					} else {
						return this.Device.ElapsedTimeMs
							   + (this.Device.SystemTimer.SystemTimeMs - this.Device.UpdateSystemTimeMs);
					}
				}
			}
			return CTimerBase.UnusedNum;
		}
	}

	internal CSoundTimer(ISoundDevice device) {
		this.Device = device;
		ctDInputTimer = new CTimer(CTimer.TimerType.PerformanceCounter);
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
		this.msDInputTimerOffset = this.ctDInputTimer.SystemTimeMs;
		this.msSoundTimerOffset = this.SystemTimeMs;
	}
	public long msGetPreciseNowSoundTimerTime()
		=> this.msDInputTimeToSoundTimerTime(this.ctDInputTimer.SystemTimeMs);
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
