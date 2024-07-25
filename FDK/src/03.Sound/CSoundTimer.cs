using System.Diagnostics;

namespace FDK {
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
						//if ( true )
						{
							return ctDInputTimer.SystemTimeMs;              // 仮にCSoundTimerをCTimer相当の動作にしてみた
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

			TimerCallback timerDelegate = new TimerCallback(SnapTimers);    // CSoundTimerをシステム時刻に変換するために、
			timer = new Timer(timerDelegate, null, 0, 1000);                // CSoundTimerとCTimerを両方とも走らせておき、
			ctDInputTimer = new CTimer(CTimer.TimerType.MultiMedia);          // 1秒に1回時差を測定するようにしておく
		}

		private void SnapTimers(object o)   // 1秒に1回呼び出され、2つのタイマー間の現在値をそれぞれ保持する。
		{
			try {
				this.nDInputTimerCounter = this.ctDInputTimer.SystemTimeMs;
				this.nSoundTimerCounter = this.SystemTimeMs;
				//Debug.WriteLine( "BaseCounter: " + nDInputTimerCounter + ", " + nSoundTimerCounter );
			} catch (Exception e)
			  // サウンド設定変更時に、timer.Dispose()した後、timerが実際に停止する前にここに来てしまう場合があり
			  // その際にNullReferenceExceptionが発生する
			  // timerが実際に停止したことを検出してから次の設定をすべきだが、実装が難しいため、
			  // ここで単に例外破棄することで代替する
			  {
				Trace.TraceInformation(e.ToString());
				Trace.TraceInformation("FDK: CSoundTimer.SnapTimers(): 例外発生しましたが、継続します。");
			}
		}
		public long nサウンドタイマーのシステム時刻msへの変換(long nDInputのタイムスタンプ) {
			return nDInputのタイムスタンプ - this.nDInputTimerCounter + this.nSoundTimerCounter;    // Timer違いによる時差を補正する
		}

		public override void Dispose() {
			// 特になし； ISoundDevice の解放は呼び出し元で行うこと。

			//sendinputスレッド削除
			if (timer != null) {
				timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
				// ここで、実際にtimerが停止したことを確認するコードを追加すべきだが、やり方わからず。
				// 代替策として、SnapTimers()中で、例外発生を破棄している。
				timer.Dispose();
				timer = null;
			}
			if (ct != null) {
				ct.Pause();
				ct.Dispose();
				ct = null;
			}
		}

		internal ISoundDevice Device = null;    // debugのため、一時的にprotectedをpublicにする。後で元に戻しておくこと。
												//protected Thread thSendInput = null;
												//protected Thread thSnapTimers = null;
		private CTimer ctDInputTimer = null;
		private long nDInputTimerCounter = 0;
		private long nSoundTimerCounter = 0;
		Timer timer = null;

		private CTimer ct = null;                               // TESTCODE
	}
}
