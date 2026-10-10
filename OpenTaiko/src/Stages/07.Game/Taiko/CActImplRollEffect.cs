using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

internal class CActImplRollEffect : CActivity {
	// コンストラクタ

	public CActImplRollEffect() {
		base.IsDeActivated = true;
	}


	// メソッド
	public virtual void Start(int player) {
		if (OpenTaiko.ConfigIni.SimpleMode) return;
		if (OpenTaiko.Tx.Effects_Roll == null || OpenTaiko.Tx.Effects_Roll.Length == 0) return;

		for (int i = 0; i < ROLL_CHARA_COUNT; i++) {
			ref var rollChara = ref RollCharas[RollCharaTail];
			if (!rollChara.IsUsing) {
				RollCharaTail = (RollCharaTail + 1) % ROLL_CHARA_COUNT;
				rollChara.IsUsing = true;
				rollChara.Type = random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_Ptn);
				rollChara.OldValue = 0;
				rollChara.Counter = new CCounter(0, 5000, 1, OpenTaiko.Timer);
				if (OpenTaiko.stageGameScreen.isMultiPlay) {
					switch (player) {
						case 0:
							rollChara.X = OpenTaiko.Skin.Game_Effect_Roll_StartPoint_1P_X[random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_StartPoint_1P_X.Length)];
							rollChara.Y = OpenTaiko.Skin.Game_Effect_Roll_StartPoint_1P_Y[random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_StartPoint_1P_Y.Length)];
							rollChara.XAdd = OpenTaiko.Skin.Game_Effect_Roll_Speed_1P_X[random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_Speed_1P_X.Length)];
							rollChara.YAdd = OpenTaiko.Skin.Game_Effect_Roll_Speed_1P_Y[random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_Speed_1P_Y.Length)];
							break;
						case 1:
							rollChara.X = OpenTaiko.Skin.Game_Effect_Roll_StartPoint_2P_X[random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_StartPoint_2P_X.Length)];
							rollChara.Y = OpenTaiko.Skin.Game_Effect_Roll_StartPoint_2P_Y[random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_StartPoint_2P_Y.Length)];
							rollChara.XAdd = OpenTaiko.Skin.Game_Effect_Roll_Speed_2P_X[random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_Speed_2P_X.Length)];
							rollChara.YAdd = OpenTaiko.Skin.Game_Effect_Roll_Speed_2P_Y[random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_Speed_2P_Y.Length)];
							break;
						default:
							return;
					}
				} else {
					rollChara.X = OpenTaiko.Skin.Game_Effect_Roll_StartPoint_X[random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_StartPoint_X.Length)];
					rollChara.Y = OpenTaiko.Skin.Game_Effect_Roll_StartPoint_Y[random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_StartPoint_Y.Length)];
					rollChara.XAdd = OpenTaiko.Skin.Game_Effect_Roll_Speed_X[random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_Speed_X.Length)];
					rollChara.YAdd = OpenTaiko.Skin.Game_Effect_Roll_Speed_Y[random.Next(0, OpenTaiko.Skin.Game_Effect_Roll_Speed_Y.Length)];
				}
				break;
			}
		}

	}

	// CActivity 実装

	public override void Activate() {

		for (int i = 0; i < ROLL_CHARA_COUNT; i++) {
			ref var rollChara = ref RollCharas[i];
			rollChara = new RollChara();
			rollChara.IsUsing = false;
			rollChara.Counter = new CCounter();
		}
		RollCharaHead = RollCharaTail = 0;
		// SkinConfigで指定されたいくつかの変数からこのクラスに合ったものに変換していく

		base.Activate();
	}
	public override void DeActivate() {

		for (int i = 0; i < ROLL_CHARA_COUNT; i++) {
			RollCharas[i].Counter = null;
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

			if (OpenTaiko.ConfigIni.nPlayerCount > 2) return 0;

			var prevHead = RollCharaHead;
			for (int i = 0; i < ROLL_CHARA_COUNT; i++) {
				var iRollChara = (prevHead + i) % ROLL_CHARA_COUNT;
				ref var rollChara = ref RollCharas[iRollChara];
				if (!rollChara.IsUsing) {
					if (iRollChara == RollCharaTail)
						break;
				} else {
					rollChara.OldValue = rollChara.Counter.CurrentValue;
					rollChara.Counter.Tick();
					if (rollChara.Counter.IsEnded) {
						rollChara.Counter.Stop();
						rollChara.IsUsing = false;
						if (iRollChara == RollCharaHead)
							RollCharaHead = (RollCharaHead + 1) % ROLL_CHARA_COUNT;
					}
					for (int l = rollChara.OldValue; l < rollChara.Counter.CurrentValue; l++) {
						rollChara.X += rollChara.XAdd;
						rollChara.Y += rollChara.YAdd;
					}

					if (OpenTaiko.Tx.Effects_Roll[rollChara.Type] != null) {
						OpenTaiko.Tx.Effects_Roll[rollChara.Type]?.t2DDraw(rollChara.X, rollChara.Y);

						// 画面外にいたら描画をやめさせる
						if (rollChara.X < 0 - OpenTaiko.Tx.Effects_Roll[rollChara.Type].szTextureSize.Width || rollChara.X > OpenTaiko.Skin.Resolution[0]) {
							rollChara.Counter.Stop();
							rollChara.IsUsing = false;
						}

						if (rollChara.Y < 0 - OpenTaiko.Tx.Effects_Roll[rollChara.Type].szTextureSize.Height || rollChara.Y > OpenTaiko.Skin.Resolution[1]) {
							rollChara.Counter.Stop();
							rollChara.IsUsing = false;
						}
					}


				}
			}
		}
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	//private CTexture[] txChara;
	private int nTexSheetCount;

	[StructLayout(LayoutKind.Sequential)]
	private struct STRollChar {
		public int nColor;
		public bool bUse;
		public CCounter ctProgress;
		public int nPreviousValue;
		public float fX;
		public float fY;
		public float fXStartPoint;
		public float fYStartPoint;
		public float fProgressDirection; //進行方向 0:左→右 1:左下→右上 2:右→左
		public float fXAcceleration;
		public float fYAcceleration;
	}
	private STRollChar[] stRollChar = new STRollChar[64];

	[StructLayout(LayoutKind.Sequential)]
	private struct RollChara {
		public CCounter Counter;
		public int Type;
		public bool IsUsing;
		public float X;
		public float Y;
		public float XAdd;
		public float YAdd;
		public int OldValue;
	}

	private const int ROLL_CHARA_COUNT = 128; // circular queue
	private readonly RollChara[] RollCharas = new RollChara[ROLL_CHARA_COUNT];
	private int RollCharaHead;
	private int RollCharaTail;

	private Random random = new Random();

	private int[,] StartPoint;
	private int[,] StartPoint_1P;
	private int[,] StartPoint_2P;
	private float[,] Speed;
	private float[,] Speed_1P;
	private float[,] Speed_2P;
	private int CharaPtn;
	//-----------------
	#endregion
}
