﻿using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

internal class Rainbow : CActivity {
	// コンストラクタ

	public Rainbow() {
		base.IsDeActivated = true;
	}

	public virtual void Start(int player) {
		if (OpenTaiko.Tx.Effects_Rainbow != null && !OpenTaiko.ConfigIni.SimpleMode) {
			for (int i = 0; i < 2; i++) {
				if (!this.Rainbow1P[i].IsUsing && player == 0) {
					this.Rainbow1P[i].IsUsing = true;
					this.Rainbow1P[i].Counter = new CCounter(0, 164, OpenTaiko.Skin.Game_Effect_Rainbow_Timer, OpenTaiko.Timer); // カウンタ
					this.Rainbow1P[i].Player = player;
					break;
				}
				if (!this.Rainbow2P[i].IsUsing && player == 1) {
					this.Rainbow2P[i].IsUsing = true;
					this.Rainbow2P[i].Counter = new CCounter(0, 164, OpenTaiko.Skin.Game_Effect_Rainbow_Timer, OpenTaiko.Timer); // カウンタ
					this.Rainbow2P[i].Player = player;
					break;
				}
			}
		}
	}


	// CActivity 実装

	public override void Activate() {
		for (int i = 0; i < 2; i++) {
			this.Rainbow1P[i].Counter = new CCounter();
			this.Rainbow2P[i].Counter = new CCounter();
		}
		base.Activate();
	}
	public override void DeActivate() {
		for (int i = 0; i < 2; i++) {
			this.Rainbow1P[i].Counter = null;
			this.Rainbow2P[i].Counter = null;
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
			if (OpenTaiko.ConfigIni.nPlayerCount > 2) return base.Draw();
			for (int f = 0; f < 2; f++) {
				if (this.Rainbow1P[f].IsUsing) {
					this.Rainbow1P[f].Counter.Tick();
					if (this.Rainbow1P[f].Counter.IsEnded) {
						this.Rainbow1P[f].Counter.Stop();
						this.Rainbow1P[f].IsUsing = false;
					}

					if (OpenTaiko.Tx.Effects_Rainbow != null && this.Rainbow1P[f].Player == 0) //画像が出来るまで
					{
						//this.st虹[f].ct進行.n現在の値 = 164;

						if (this.Rainbow1P[f].Counter.CurrentValue < 82) {
							int nRectX = ((this.Rainbow1P[f].Counter.CurrentValue * OpenTaiko.Tx.Effects_Rainbow.szTextureSize.Width) / 85);
							OpenTaiko.Tx.Effects_Rainbow.t2D描画(OpenTaiko.Skin.Game_Effect_Rainbow_X[0], OpenTaiko.Skin.Game_Effect_Rainbow_Y[0],
								new Rectangle(0, 0, nRectX, OpenTaiko.Tx.Effects_Rainbow.szTextureSize.Height));
						} else if (this.Rainbow1P[f].Counter.CurrentValue >= 82) {
							int nRectX = (((this.Rainbow1P[f].Counter.CurrentValue - 82) * OpenTaiko.Tx.Effects_Rainbow.szTextureSize.Width) / 85);
							OpenTaiko.Tx.Effects_Rainbow.t2D描画(OpenTaiko.Skin.Game_Effect_Rainbow_X[0] + nRectX, OpenTaiko.Skin.Game_Effect_Rainbow_Y[0],
								new Rectangle(nRectX, 0, OpenTaiko.Tx.Effects_Rainbow.szTextureSize.Width - nRectX, OpenTaiko.Tx.Effects_Rainbow.szTextureSize.Height));
						}

					}

				}
			}
			for (int f = 0; f < 2; f++) {
				if (this.Rainbow2P[f].IsUsing) {
					this.Rainbow2P[f].Counter.Tick();
					if (this.Rainbow2P[f].Counter.IsEnded) {
						this.Rainbow2P[f].Counter.Stop();
						this.Rainbow2P[f].IsUsing = false;
					}

					if (OpenTaiko.Tx.Effects_Rainbow != null && this.Rainbow2P[f].Player == 1) //画像が出来るまで
					{
						//this.st虹[f].ct進行.n現在の値 = 164;

						if (this.Rainbow2P[f].Counter.CurrentValue < 82) {
							int nRectX = ((this.Rainbow2P[f].Counter.CurrentValue * OpenTaiko.Tx.Effects_Rainbow.szTextureSize.Width) / 85);
							OpenTaiko.Tx.Effects_Rainbow.t2D上下反転描画(OpenTaiko.Skin.Game_Effect_Rainbow_X[0], OpenTaiko.Skin.Game_Effect_Rainbow_Y[1],
								new Rectangle(0, 0, nRectX, OpenTaiko.Tx.Effects_Rainbow.szTextureSize.Height));
						} else if (this.Rainbow2P[f].Counter.CurrentValue >= 82) {
							int nRectX = (((this.Rainbow2P[f].Counter.CurrentValue - 82) * OpenTaiko.Tx.Effects_Rainbow.szTextureSize.Width) / 85);
							OpenTaiko.Tx.Effects_Rainbow.t2D上下反転描画(OpenTaiko.Skin.Game_Effect_Rainbow_X[0] + nRectX, OpenTaiko.Skin.Game_Effect_Rainbow_Y[1],
								new Rectangle(nRectX, 0, OpenTaiko.Tx.Effects_Rainbow.szTextureSize.Width - nRectX, OpenTaiko.Tx.Effects_Rainbow.szTextureSize.Height));
						}

					}

				}
			}
		}
		return base.Draw();
	}


	// その他

	#region [ private ]
	//-----------------

	[StructLayout(LayoutKind.Sequential)]
	private struct StructRainbow {
		public bool IsUsing;
		public int Player;
		public CCounter Counter;
		public float X;
	}

	private StructRainbow[] Rainbow1P = new StructRainbow[2];
	private StructRainbow[] Rainbow2P = new StructRainbow[2];

	//-----------------
	#endregion
}
