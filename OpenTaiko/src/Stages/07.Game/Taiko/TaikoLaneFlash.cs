using FDK;
using static OpenTaiko.PlayerLane;

namespace OpenTaiko;

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

		Flash[(int)FlashType.Red] = new LaneFlash(ref OpenTaiko.Tx.Lane_Red[(int)_gt], player);
		Flash[(int)FlashType.Blue] = new LaneFlash(ref OpenTaiko.Tx.Lane_Blue[(int)_gt], player);
		Flash[(int)FlashType.Clap] = new LaneFlash(ref OpenTaiko.Tx.Lane_Clap[(int)_gt], player);
		Flash[(int)FlashType.Hit] = new LaneFlash(ref OpenTaiko.Tx.Lane_Yellow, player);
	}
	public void Start(FlashType flashType) {
		if (flashType == FlashType.Total) return;
		Flash[(int)flashType].Start();
	}

	public LaneFlash[] Flash;

	public enum FlashType {
		Red,
		Blue,
		Yellow = Blue,
		Clap,
		Hit,
		Total
	}
}
