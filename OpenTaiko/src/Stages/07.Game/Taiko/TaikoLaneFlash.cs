using FDK;
using static OpenTaiko.PlayerLane;

namespace OpenTaiko {
	internal class TaikoLaneFlash : CActivity {
		// コンストラクタ

		public TaikoLaneFlash() {
			base.IsDeActivated = true;
		}


		public override void Activate() {
			PlayerLane = new PlayerLane[OpenTaiko.ConfigIni.nPlayerCount];
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				PlayerLane[i] = new PlayerLane(i);
			}
			base.Activate();
		}
		public override void DeActivate() {
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				PlayerLane[i] = null;
			}
			base.DeActivate();
		}

		public override int Draw() {
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				for (int j = 0; j < (int)FlashType.Total; j++) {
					PlayerLane[i].Flash[j].Draw();
				}
			}
			return base.Draw();
		}

		public PlayerLane[] PlayerLane;

	}
	public class PlayerLane {
		public PlayerLane(int player) {
			Flash = new LaneFlash[(int)FlashType.Total];
			var _gt = OpenTaiko.ConfigIni.nGameType[OpenTaiko.GetActualPlayer(player)];

			for (int i = 0; i < (int)FlashType.Total; i++) {
				switch (i) {
					case (int)FlashType.Red:
						Flash[i] = new LaneFlash(ref OpenTaiko.Tx.Lane_Red[(int)_gt], player);
						break;
					case (int)FlashType.Blue:
						Flash[i] = new LaneFlash(ref OpenTaiko.Tx.Lane_Blue[(int)_gt], player);
						break;
					case (int)FlashType.Clap:
						Flash[i] = new LaneFlash(ref OpenTaiko.Tx.Lane_Clap[(int)_gt], player);
						break;
					case (int)FlashType.Hit:
						Flash[i] = new LaneFlash(ref OpenTaiko.Tx.Lane_Yellow, player);
						break;
					default:
						break;
				}
			}
		}
		public void Start(FlashType flashType) {
			if (flashType == FlashType.Total) return;
			Flash[(int)flashType].Start();
		}

		public LaneFlash[] Flash;

		public enum FlashType {
			Red,
			Blue,
			Clap,
			Hit,
			Total
		}
	}
}
