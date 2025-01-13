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
			this.dbConfigScrollSpeed[i] = (double)OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.GetActualPlayer(i)];
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
				double dbScrollSpeed = (double)OpenTaiko.ConfigIni.nScrollSpeed[OpenTaiko.GetActualPlayer(i)];
				if (nNowTime < this.nScrollExclusiveTimer[i]) {
					this.nScrollExclusiveTimer[i] = nNowTime;
				}
				while ((nNowTime - this.nScrollExclusiveTimer[i]) >= 2)                               // 1 loop per 2 ms
				{
					if (this.dbConfigScrollSpeed[i] < dbScrollSpeed) {
						this.dbConfigScrollSpeed[i] += 0.012;

						if (this.dbConfigScrollSpeed[i] > dbScrollSpeed) {
							this.dbConfigScrollSpeed[i] = dbScrollSpeed;
						}
					} else if (this.dbConfigScrollSpeed[i] > dbScrollSpeed) {
						this.dbConfigScrollSpeed[i] -= 0.012;

						if (this.dbConfigScrollSpeed[i] < dbScrollSpeed) {
							this.dbConfigScrollSpeed[i] = dbScrollSpeed;
						}
					}
					this.nScrollExclusiveTimer[i] += 2;
				}
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
