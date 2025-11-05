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
			PlayerLane[i].Draw();
		}
		return base.Draw();
	}

	public PlayerLane[] PlayerLane;

}
public class PlayerLane {
	public PlayerLane(int player) {
		Flash = new LaneFlash[2, (int)FlashType.Total];
		for (EGameType gt = 0; gt <= EGameType.Konga; ++gt) {
			Flash[(int)gt, (int)FlashType.Red] = new LaneFlash(ref OpenTaiko.Tx.Lane_Red[(int)gt], player);
			Flash[(int)gt, (int)FlashType.Blue] = new LaneFlash(ref OpenTaiko.Tx.Lane_Blue[(int)gt], player);
			Flash[(int)gt, (int)FlashType.Clap] = new LaneFlash(ref OpenTaiko.Tx.Lane_Clap[(int)gt], player);
			Flash[(int)gt, (int)FlashType.Hit] = new LaneFlash(ref OpenTaiko.Tx.Lane_Yellow, player);
		}
	}
	public void Start(FlashType flashType, EGameType gameType) {
		if (flashType == FlashType.Total) return;
		Flash[(int)gameType, (int)flashType].Start();
	}
	public void Draw() {
		for (EGameType gt = 0; gt <= EGameType.Konga; ++gt) {
			for (int j = 0; j < (int)FlashType.Total; j++) {
				this.Flash[(int)gt, j].Draw();
			}
		}
	}

	public LaneFlash[,] Flash; // [gameType, flashType]

	public enum FlashType {
		Red,
		Blue,
		Yellow = Blue,
		Clap,
		Hit,
		Total
	}
}
