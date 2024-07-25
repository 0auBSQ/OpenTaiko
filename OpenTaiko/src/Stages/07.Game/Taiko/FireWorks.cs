using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace TJAPlayer3 {
	internal class FireWorks : CActivity {
		// コンストラクタ

		public FireWorks() {
			base.IsDeActivated = true;
		}


		// メソッド

		/// <summary>
		/// 大音符の花火エフェクト
		/// </summary>
		/// <param name="nLane"></param>
		public virtual void Start(int nLane, int nPlayer, double x, double y) {
			if (TJAPlayer3.ConfigIni.SimpleMode) return;

			for (int i = 0; i < 32; i++) {
				if (!FireWork[i].IsUsing) {
					FireWork[i].IsUsing = true;
					FireWork[i].Lane = nLane;
					FireWork[i].Player = nPlayer;
					FireWork[i].X = x;
					FireWork[i].Y = y;
					FireWork[i].Counter = new CCounter(0, TJAPlayer3.Skin.Game_Effect_FireWorks[2] - 1, TJAPlayer3.Skin.Game_Effect_FireWorks_Timer, TJAPlayer3.Timer);
					break;
				}
			}
		}

		// CActivity 実装

		public override void Activate() {
			for (int i = 0; i < 32; i++) {
				FireWork[i] = new Status();
				FireWork[i].IsUsing = false;
				FireWork[i].Counter = new CCounter();
			}
			base.Activate();
		}
		public override void DeActivate() {
			for (int i = 0; i < 32; i++) {
				FireWork[i].Counter = null;
			}
			base.DeActivate();
		}
		public override void CreateManagedResource() {
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource() {
			base.ReleaseManagedResource();
		}
		public override int Draw() {
			if (!base.IsDeActivated && !TJAPlayer3.ConfigIni.SimpleMode) {
				for (int i = 0; i < 32; i++) {
					if (FireWork[i].IsUsing) {
						FireWork[i].Counter.Tick();
						TJAPlayer3.Tx.Effects_Hit_FireWorks?.t2D中心基準描画((float)FireWork[i].X, (float)FireWork[i].Y, 1, new Rectangle(FireWork[i].Counter.CurrentValue * TJAPlayer3.Skin.Game_Effect_FireWorks[0], 0, TJAPlayer3.Skin.Game_Effect_FireWorks[0], TJAPlayer3.Skin.Game_Effect_FireWorks[1]));
						if (FireWork[i].Counter.IsEnded) {
							FireWork[i].Counter.Stop();
							FireWork[i].IsUsing = false;
						}
					}
				}
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		[StructLayout(LayoutKind.Sequential)]
		private struct Status {
			public int Lane;
			public int Player;
			public bool IsUsing;
			public CCounter Counter;
			public double X;
			public double Y;
		}
		private Status[] FireWork = new Status[32];

		//-----------------
		#endregion
	}
}

