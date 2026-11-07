using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

internal class CActImplPad : CActivity {
	// コンストラクタ

	public CActImplPad() {
		base.IsDeActivated = true;
	}


	// メソッド

	public void Hit(int nLane) {
		this.stPadState[nLane].nBrightness = 6;
		this.stPadState[nLane].nYCoordAccelerationdot = 2;
	}


	// CActivity 実装

	public override void Activate() {
		this.nFlashControlTimer = -1;
		this.nYCoordControlTimer = -1;
		for (int i = 0; i < 9; i++) {
			STPadState stPadState2 = new STPadState();
			STPadState stPadState = stPadState2;
			stPadState.nYCoordOffsetdot = 0;
			stPadState.nYCoordAccelerationdot = 0;
			stPadState.nBrightness = 0;
			this.stPadState[i] = stPadState;
		}
		base.Activate();
	}
	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}
	public override int Draw() {
		if (!base.IsDeActivated) {
			if (base.IsFirstDraw) {
				this.nFlashControlTimer = SoundManager.PlayTimer.NowTimeMs;
				this.nYCoordControlTimer = SoundManager.PlayTimer.NowTimeMs;
				base.IsFirstDraw = false;
			}
			long msNow = SoundManager.PlayTimer.NowTimeMs;
			if (msNow < this.nFlashControlTimer) {
				this.nFlashControlTimer = msNow;
			}
			var nStepsFlash = (msNow - this.nFlashControlTimer) / 15;
			if (nStepsFlash > 0) {
				for (int j = 0; j < 10; j++) {
					ref int rBright = ref this.stPadState[j].nBrightness;
					if (rBright > 0)
						rBright = (int)Math.Max(0, rBright - nStepsFlash);
				}
				this.nFlashControlTimer += 15 * nStepsFlash;
			}
			if (msNow < this.nYCoordControlTimer) {
				this.nYCoordControlTimer = msNow;
			}
			var nStepsY = (msNow - this.nYCoordControlTimer) / 5;
			if (nStepsY > 0) {
				for (int k = 0; k < 10; k++) {
					ref STPadState state = ref this.stPadState[k];
					state.nYCoordOffsetdot += (int)(state.nYCoordAccelerationdot * nStepsY);
					if (state.nYCoordOffsetdot > 15) {
						state.nYCoordOffsetdot = 15;
						state.nYCoordAccelerationdot = -1;
					} else if (state.nYCoordOffsetdot < 0) {
						state.nYCoordOffsetdot = 0;
						state.nYCoordAccelerationdot = 0;
					}
				}
				this.nYCoordControlTimer += 5 * nStepsY;
			}


		}
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	[StructLayout(LayoutKind.Sequential)]
	private struct STPadState {
		public int nBrightness;
		public int nYCoordOffsetdot;
		public int nYCoordAccelerationdot;
	}

	private long nYCoordControlTimer;
	private long nFlashControlTimer;
	private STPadState[] stPadState = new STPadState[10];
	//-----------------
	#endregion
}
