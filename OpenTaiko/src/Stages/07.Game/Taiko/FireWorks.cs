using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

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
	public virtual void Start(NotesManager.ENoteType nLane, EGameType gameType, int nPlayer, double x, double y) {
		if (OpenTaiko.ConfigIni.SimpleMode) return;

		ref var fireWork = ref FireWork[FireWorkTail];
		if (!fireWork.IsUsing) {
			FireWorkTail = (FireWorkTail + 1) % FIRE_WORK_COUNT;
			fireWork.IsUsing = true;
			fireWork.Lane = nLane;
			fireWork.GameType = gameType;
			fireWork.Player = nPlayer;
			fireWork.X = x;
			fireWork.Y = y;
			fireWork.Counter = new CCounter(0, OpenTaiko.Skin.Game_Effect_FireWorks[2] - 1, OpenTaiko.Skin.Game_Effect_FireWorks_Timer, OpenTaiko.Timer);
		}
	}

	// CActivity 実装

	public override void Activate() {
		for (int i = 0; i < FIRE_WORK_COUNT; i++) {
			ref var fireWork = ref FireWork[i];
			fireWork = new Status();
			fireWork.IsUsing = false;
			fireWork.Counter = new CCounter();
		}
		base.Activate();
	}
	public override void DeActivate() {
		for (int i = 0; i < FIRE_WORK_COUNT; i++) {
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
		if (!base.IsDeActivated && !OpenTaiko.ConfigIni.SimpleMode) {
			var prevHead = FireWorkHead;
			for (int i = 0; i < FIRE_WORK_COUNT; i++) {
				var iFireWork = (prevHead + i) % FIRE_WORK_COUNT;
				ref var fireWork = ref FireWork[iFireWork];
				if (!fireWork.IsUsing) {
					if (iFireWork == FireWorkTail)
						break;
				} else {
					fireWork.Counter!.Tick();
					OpenTaiko.Tx.Effects_Hit_FireWorks?.t2DCenterBasedDraw((float)fireWork.X, (float)fireWork.Y, 1, new Rectangle(fireWork.Counter.CurrentValue * OpenTaiko.Skin.Game_Effect_FireWorks[0], 0, OpenTaiko.Skin.Game_Effect_FireWorks[0], OpenTaiko.Skin.Game_Effect_FireWorks[1]));
					if (fireWork.Counter.IsEnded) {
						fireWork.Counter.Stop();
						fireWork.IsUsing = false;
						if (iFireWork == FireWorkHead)
							FireWorkHead = (FireWorkHead + 1) % FIRE_WORK_COUNT;
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
		public NotesManager.ENoteType Lane;
		public EGameType GameType;
		public int Player;
		public bool IsUsing;
		public CCounter? Counter;
		public double X;
		public double Y;
	}
	private const int FIRE_WORK_COUNT = 32;
	private Status[] FireWork = new Status[FIRE_WORK_COUNT];
	private int FireWorkHead;
	private int FireWorkTail;

	//-----------------
	#endregion
}
