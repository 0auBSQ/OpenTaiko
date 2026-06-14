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
			for (int i = 0; i < 128; i++) {
				if (!st[i].bUse) {
					st[i].bUse = true;
					st[i].ctProgress = new CCounter(0, OpenTaiko.Skin.Game_Effect_NotesFlash[2], OpenTaiko.Skin.Game_Effect_NotesFlash_Timer, OpenTaiko.Timer);
					st[i].ctChipEffect = new CCounter(0, 24, 17, OpenTaiko.Timer);
					st[i].nPlayer = nPlayer;
					st[i].Lane = Lane;
					st[i].GameType = gameType;
					break;
				}
			}
		}
	}

	// CActivity 実装

	public override void Activate() {
		for (int i = 0; i < 128; i++) {
			st[i] = new STChipEffect {
				bUse = false,
				ctProgress = new CCounter(),
				ctChipEffect = new CCounter()
			};
		}
		base.Activate();
	}
	public override void DeActivate() {
		for (int i = 0; i < 128; i++) {
			st[i].ctProgress = null;
			st[i].ctChipEffect = null;
			st[i].bUse = false;
		}
		base.DeActivate();
	}
	public override int Draw() {
		for (int i = 0; i < 128; i++) {
			if (st[i].bUse) {
				st[i].ctProgress.Tick();
				st[i].ctChipEffect.Tick();
				if (st[i].ctProgress.IsEnded) {
					st[i].ctProgress.Stop();
					st[i].bUse = false;
				}

				switch (st[i].nPlayer) {
					case 0:
						OpenTaiko.Tx.Gauge_Soul_Explosion[OpenTaiko.P1IsBlue() ? 1 : 0]?.t2DCenterBasedDraw(OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[0], OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[0], new Rectangle(st[i].ctProgress.CurrentValue * OpenTaiko.Skin.Game_Effect_NotesFlash[0], 0, OpenTaiko.Skin.Game_Effect_NotesFlash[0], OpenTaiko.Skin.Game_Effect_NotesFlash[1]));

						if (this.st[i].ctChipEffect.CurrentValue < 13)
							NotesManager.DisplayNote(
								st[i].nPlayer,
								OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[0],
								OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[0],
								st[i].Lane,
								st[i].GameType);
						break;

					case 1:
						OpenTaiko.Tx.Gauge_Soul_Explosion[1]?.t2DCenterBasedDraw(OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[1], OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[1], new Rectangle(st[i].ctProgress.CurrentValue * OpenTaiko.Skin.Game_Effect_NotesFlash[0], 0, OpenTaiko.Skin.Game_Effect_NotesFlash[0], OpenTaiko.Skin.Game_Effect_NotesFlash[1]));
						if (this.st[i].ctChipEffect.CurrentValue < 13)
							NotesManager.DisplayNote(
								st[i].nPlayer,
								OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[1],
								OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[1],
								st[i].Lane,
								st[i].GameType);
						break;
				}

				if (OpenTaiko.Tx.ChipEffect != null) {
					// TODO: Generate chip effect from note image?
					int laneXOffset = NotesManager.IsPurpleNoteTaiko(st[i].Lane, st[i].GameType) ? NotesManager.NoteTextureColumnFast(NotesManager.ENoteType.DonBig)
						: (st[i].GameType is EGameType.Konga || st[i].Lane > NotesManager.ENoteType.KaBig) ? NotesManager.NoteTextureColumnFast(NotesManager.ENoteType.Don)
						: NotesManager.NoteTextureColumnFast(st[i].Lane);

					if (this.st[i].ctChipEffect.CurrentValue < 12) {
						OpenTaiko.Tx.ChipEffect.color4 = new Color4(1.0f, 1.0f, 0.0f, 1.0f);
						OpenTaiko.Tx.ChipEffect.Opacity = (int)(this.st[i].ctChipEffect.CurrentValue * (float)(225 / 11));
						OpenTaiko.Tx.ChipEffect.t2DCenterBasedDraw(OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[st[i].nPlayer], OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[st[i].nPlayer], new Rectangle(laneXOffset * OpenTaiko.Skin.Game_Notes_Size[0], 0, OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1]));
					}
					if (this.st[i].ctChipEffect.CurrentValue > 12 && this.st[i].ctChipEffect.CurrentValue < 24) {
						OpenTaiko.Tx.ChipEffect.color4 = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
						OpenTaiko.Tx.ChipEffect.Opacity = 255 - (int)((this.st[i].ctChipEffect.CurrentValue - 10) * (float)(255 / 14));
						OpenTaiko.Tx.ChipEffect.t2DCenterBasedDraw(OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_X[st[i].nPlayer], OpenTaiko.Skin.Game_Effect_FlyingNotes_EndPoint_Y[st[i].nPlayer], new Rectangle(laneXOffset * OpenTaiko.Skin.Game_Notes_Size[0], 0, OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1]));
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

	[StructLayout(LayoutKind.Sequential)]
	private struct STChipEffect {
		public bool bUse;
		public CCounter ctProgress;
		public CCounter ctChipEffect;
		public int nPlayer;
		public NotesManager.ENoteType Lane;
		public EGameType GameType;
	}
	private STChipEffect[] st = new STChipEffect[128];

	//-----------------
	#endregion
}
