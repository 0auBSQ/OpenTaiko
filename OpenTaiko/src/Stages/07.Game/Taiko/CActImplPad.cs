using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko {
	internal class CActImplPad : CActivity {
		// コンストラクタ

		public CActImplPad() {
			base.IsDeActivated = true;
		}


		// メソッド

		public void Hit(int nLane) {
			this.stパッド状態[nLane].n明るさ = 6;
			this.stパッド状態[nLane].nY座標加速度dot = 2;
		}


		// CActivity 実装

		public override void Activate() {
			this.nフラッシュ制御タイマ = -1;
			this.nY座標制御タイマ = -1;
			for (int i = 0; i < 9; i++) {
				STパッド状態 stパッド状態2 = new STパッド状態();
				STパッド状態 stパッド状態 = stパッド状態2;
				stパッド状態.nY座標オフセットdot = 0;
				stパッド状態.nY座標加速度dot = 0;
				stパッド状態.n明るさ = 0;
				this.stパッド状態[i] = stパッド状態;
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
					this.nフラッシュ制御タイマ = (long)(SoundManager.PlayTimer.NowTime * OpenTaiko.ConfigIni.SongPlaybackSpeed);
					this.nY座標制御タイマ = (long)(SoundManager.PlayTimer.NowTime * OpenTaiko.ConfigIni.SongPlaybackSpeed);
					base.IsFirstDraw = false;
				}
				long num = (long)(SoundManager.PlayTimer.NowTime * OpenTaiko.ConfigIni.SongPlaybackSpeed);
				if (num < this.nフラッシュ制御タイマ) {
					this.nフラッシュ制御タイマ = num;
				}
				while ((num - this.nフラッシュ制御タイマ) >= 15) {
					for (int j = 0; j < 10; j++) {
						if (this.stパッド状態[j].n明るさ > 0) {
							this.stパッド状態[j].n明るさ--;
						}
					}
					this.nフラッシュ制御タイマ += 15;
				}
				long num3 = SoundManager.PlayTimer.NowTime;
				if (num3 < this.nY座標制御タイマ) {
					this.nY座標制御タイマ = num3;
				}
				while ((num3 - this.nY座標制御タイマ) >= 5) {
					for (int k = 0; k < 10; k++) {
						this.stパッド状態[k].nY座標オフセットdot += this.stパッド状態[k].nY座標加速度dot;
						if (this.stパッド状態[k].nY座標オフセットdot > 15) {
							this.stパッド状態[k].nY座標オフセットdot = 15;
							this.stパッド状態[k].nY座標加速度dot = -1;
						} else if (this.stパッド状態[k].nY座標オフセットdot < 0) {
							this.stパッド状態[k].nY座標オフセットdot = 0;
							this.stパッド状態[k].nY座標加速度dot = 0;
						}
					}
					this.nY座標制御タイマ += 5;
				}


			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		[StructLayout(LayoutKind.Sequential)]
		private struct STパッド状態 {
			public int n明るさ;
			public int nY座標オフセットdot;
			public int nY座標加速度dot;
		}

		private long nY座標制御タイマ;
		private long nフラッシュ制御タイマ;
		private STパッド状態[] stパッド状態 = new STパッド状態[10];
		//-----------------
		#endregion
	}
}
