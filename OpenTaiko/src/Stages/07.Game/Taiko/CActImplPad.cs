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
			long num = SoundManager.PlayTimer.NowTimeMs;
			if (num < this.nFlashControlTimer) {
				this.nFlashControlTimer = num;
			}
			while ((num - this.nFlashControlTimer) >= 15) {
				for (int j = 0; j < 10; j++) {
					if (this.stPadState[j].nBrightness > 0) {
						this.stPadState[j].nBrightness--;
					}
				}
				this.nFlashControlTimer += 15;
			}
			long num3 = SoundManager.PlayTimer.NowTimeMs;
			if (num3 < this.nYCoordControlTimer) {
				this.nYCoordControlTimer = num3;
			}
			while ((num3 - this.nYCoordControlTimer) >= 5) {
				for (int k = 0; k < 10; k++) {
					this.stPadState[k].nYCoordOffsetdot += this.stPadState[k].nYCoordAccelerationdot;
					if (this.stPadState[k].nYCoordOffsetdot > 15) {
						this.stPadState[k].nYCoordOffsetdot = 15;
						this.stPadState[k].nYCoordAccelerationdot = -1;
					} else if (this.stPadState[k].nYCoordOffsetdot < 0) {
						this.stPadState[k].nYCoordOffsetdot = 0;
						this.stPadState[k].nYCoordAccelerationdot = 0;
					}
				}
				this.nYCoordControlTimer += 5;
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
