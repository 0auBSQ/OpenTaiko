using FDK;

namespace OpenTaiko;

internal class CActTaikoScrollSpeed : CActivity {
	// Properties

	public double[] dbConfigScrollSpeed = new double[5];


	// Constructor

	public CActTaikoScrollSpeed() {
		base.IsDeActivated = true;
	}


	// CActivity implementation

	public override void Activate() {
		for (int i = 0; i < 5; i++) {
			this.dbConfigScrollSpeed[i] = (double)OpenTaiko.ConfigIni.nScrollSpeed[i];
			this.nScrollExclusiveTimer[i] = -1;
		}



		base.Activate();
	}
	public override unsafe int Draw() {
		if (!base.IsDeActivated) {
			if (base.IsFirstDraw) {
				for (int i = 0; i < 5; i++) {
					this.nScrollExclusiveTimer[i] = SoundManager.PlayTimer.NowTimeMs;

				}

				base.IsFirstDraw = false;
			}
			long nNowTime = SoundManager.PlayTimer.NowTimeMs;
			for (int i = 0; i < 5; i++) {
				double dbScrollSpeed = (double)OpenTaiko.ConfigIni.nScrollSpeed[i];
				if (nNowTime < this.nScrollExclusiveTimer[i]) {
					this.nScrollExclusiveTimer[i] = nNowTime;
				}
				var nSteps = (nNowTime - this.nScrollExclusiveTimer[i]) / 2.0; // 1 step per 2 ms
				if (nSteps > 0)
				{
					ref double rSpeed = ref this.dbConfigScrollSpeed[i];
					if (rSpeed < dbScrollSpeed) {
						rSpeed = Math.Min(dbScrollSpeed, rSpeed + 0.012 * nSteps);
					} else if (rSpeed > dbScrollSpeed) {
						rSpeed = Math.Max(dbScrollSpeed, rSpeed - 0.012 * nSteps);
					}
				}
				this.nScrollExclusiveTimer[i] = nNowTime;
			}
		}
		return 0;
	}


	#region [ private ]
	//-----------------
	private long[] nScrollExclusiveTimer = new long[5];
	//-----------------
	#endregion
}
