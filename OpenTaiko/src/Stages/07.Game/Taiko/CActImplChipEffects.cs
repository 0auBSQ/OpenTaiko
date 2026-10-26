using System.Runtime.InteropServices;
using FDK;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko;

internal class CActImplChipEffects : CActivity {
	// コンストラクタ

	public CActImplChipEffects() {
		//base.b活性化してない = true;
	}


	// メソッド
	public virtual void Start(int nPlayer, NotesManager.ENoteType Lane, EGameType gameType) {
		if (OpenTaiko.Tx.Gauge_Soul_Explosion != null && OpenTaiko.ConfigIni.nPlayerCount <= 2 && !OpenTaiko.ConfigIni.bAIBattleMode) {
			ref var st = ref states[StateTail];
			if (!st.bUse) {
				StateTail = (StateTail + 1) % STATE_COUNT;
				st.bUse = true;
				st.ctProgress = new CCounter(0, OpenTaiko.Skin.Game_Effect_NotesFlash[2], OpenTaiko.Skin.Game_Effect_NotesFlash_Timer, OpenTaiko.Timer);
				st.ctChipEffect = new CCounter(0, 24, 17, OpenTaiko.Timer);
				st.nPlayer = nPlayer;
				st.Lane = Lane;
				st.GameType = gameType;
			}
		}
	}

	// CActivity 実装

	public override void Activate() {
		for (int i = 0; i < STATE_COUNT; i++) {
			states[i] = new STChipEffect {
				bUse = false,
				ctProgress = new CCounter(),
				ctChipEffect = new CCounter()
			};
		}
		StateHead = StateTail = 0;
		base.Activate();
	}
	public override void DeActivate() {
		for (int i = 0; i < STATE_COUNT; i++) {
			ref var st = ref states[i];
			st.ctProgress = null;
			st.ctChipEffect = null;
			st.bUse = false;
		}
		base.DeActivate();
	}
	public override int Draw() {
		var prevHead = StateHead;
		for (int i = 0; i < STATE_COUNT; i++) {
			var iState = (prevHead + i) % STATE_COUNT;
			ref var st = ref states[iState];
			if (!st.bUse) {
				if (iState == StateTail)
					break;
			} else {
				st.ctProgress.Tick();
				st.ctChipEffect.Tick();
				if (st.ctProgress.IsEnded) {
					st.ctProgress.Stop();
					st.bUse = false;
					if (iState == StateHead)
						StateHead = (StateHead + 1) % STATE_COUNT;
				}

				switch (st.nPlayer) {
					case 0:
						OpenTaiko.Tx.Gauge_Soul_Explosion[OpenTaiko.P1IsBlue() ? 1 : 0]?.t2DCenterBasedDraw(OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[0], OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[0], new Rectangle(st.ctProgress.CurrentValue * OpenTaiko.Skin.Game_Effect_NotesFlash[0], 0, OpenTaiko.Skin.Game_Effect_NotesFlash[0], OpenTaiko.Skin.Game_Effect_NotesFlash[1]));

						if (st.ctChipEffect.CurrentValue < 13)
							NotesManager.DisplayNote(
								st.nPlayer,
								OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[0],
								OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[0],
								st.Lane,
								st.GameType);
						break;

					case 1:
						OpenTaiko.Tx.Gauge_Soul_Explosion[1]?.t2DCenterBasedDraw(OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[1], OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[1], new Rectangle(st.ctProgress.CurrentValue * OpenTaiko.Skin.Game_Effect_NotesFlash[0], 0, OpenTaiko.Skin.Game_Effect_NotesFlash[0], OpenTaiko.Skin.Game_Effect_NotesFlash[1]));
						if (st.ctChipEffect.CurrentValue < 13)
							NotesManager.DisplayNote(
								st.nPlayer,
								OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[1],
								OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[1],
								st.Lane,
								st.GameType);
						break;
				}

				{
					int fx = OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[st.nPlayer], fy = OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[st.nPlayer];
					if (st.ctChipEffect.CurrentValue < 12)
						NotesManager.DisplayChipEffect(fx, fy, st.Lane, st.GameType, new Color4(1.0f, 1.0f, 0.0f, 1.0f),
							(int)(st.ctChipEffect.CurrentValue * (float)(225 / 11)));
					if (st.ctChipEffect.CurrentValue > 12 && st.ctChipEffect.CurrentValue < 24)
						NotesManager.DisplayChipEffect(fx, fy, st.Lane, st.GameType, new Color4(1.0f, 1.0f, 1.0f, 1.0f),
							255 - (int)((st.ctChipEffect.CurrentValue - 10) * (float)(255 / 14)));
				}

			}
		}
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	//private CTexture[] txChara;

	[StructLayout(LayoutKind.Sequential)]
	private struct STChipEffect {
		public bool bUse;
		public CCounter ctProgress;
		public CCounter ctChipEffect;
		public int nPlayer;
		public NotesManager.ENoteType Lane;
		public EGameType GameType;
	}
	private const int STATE_COUNT = 128; // circular queue
	private readonly STChipEffect[] states = new STChipEffect[STATE_COUNT];
	private int StateHead;
	private int StateTail;

	//-----------------
	#endregion
}
