using System.Runtime.InteropServices;
using FDK;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko;

internal class CActImplFireworks : CActivity {
	// コンストラクタ

	public CActImplFireworks() {
		base.IsDeActivated = true;
	}


	// メソッド

	/// <summary>
	/// 大音符の花火エフェクト
	/// </summary>
	/// <param name="nLane"></param>
	public virtual void Start(NotesManager.ENoteType nLane, EGameType gameType, int nPlayer) {
		nYCoordP2 = new int[] { 548, 612, 670, 712, 730, 780, 725, 690, 640 };
		if (OpenTaiko.Tx.Effects_Hit_FireWorks != null && OpenTaiko.Tx.Effects_Hit_FireWorks != null) {
			for (int i = 0; i < 9; i++) {
				for (int j = 0; j < 45; j++) {
					if (!this.stBigNoteFirework[j].bUse) {
						this.stBigNoteFirework[j].bUse = true;
						this.stBigNoteFirework[j].ctProgress = new CCounter(0, 40, 18, OpenTaiko.Timer); // カウンタ
						this.stBigNoteFirework[j].fX = this.nXCoord[i]; //X座標
						this.stBigNoteFirework[j].fY = nPlayer == 0 ? this.nYCoord[i] : this.nYCoordP2[i];

						switch (i) {
							case 0:
								this.stBigNoteFirework[j].nStartFrame = 0;
								this.stBigNoteFirework[j].nEndFrame = 16;
								break;
							case 1:
								this.stBigNoteFirework[j].nStartFrame = 3;
								this.stBigNoteFirework[j].nEndFrame = 19;
								break;
							case 2:
								this.stBigNoteFirework[j].nStartFrame = 6;
								this.stBigNoteFirework[j].nEndFrame = 22;
								break;
							case 3:
								this.stBigNoteFirework[j].nStartFrame = 9;
								this.stBigNoteFirework[j].nEndFrame = 25;
								break;
							case 4:
								this.stBigNoteFirework[j].nStartFrame = 12;
								this.stBigNoteFirework[j].nEndFrame = 28;
								break;
							case 5:
								this.stBigNoteFirework[j].nStartFrame = 15;
								this.stBigNoteFirework[j].nEndFrame = 31;
								break;
							case 6:
								this.stBigNoteFirework[j].nStartFrame = 18;
								this.stBigNoteFirework[j].nEndFrame = 34;
								break;
							case 7:
								this.stBigNoteFirework[j].nStartFrame = 21;
								this.stBigNoteFirework[j].nEndFrame = 37;
								break;
							case 8:
								this.stBigNoteFirework[j].nStartFrame = 24;
								this.stBigNoteFirework[j].nEndFrame = 40;
								break;
						}



						break;
					}
				}
			}
		}
	}

	public virtual void Start(NotesManager.ENoteType nLane, EGameType gameType, ENoteJudge judge, bool isBigInput, int player) {
		for (int j = 0; j < 3 * 4; j++) {
			if (!this.stState[j].bUse)
			//for( int n = 0; n < 1; n++ )
			{
				this.stState[j].bUse = true;
				//this.st状態[ n ].ct進行 = new CCounter( 0, 9, 20, CDTXMania.Timer );
				this.stState[j].ctProgress = new CCounter(0, 6, 25, OpenTaiko.Timer);
				this.stState[j].judge = judge;
				this.stState[j].nPlayer = player;
				if (NotesManager.IsBigNoteTaiko(nLane, gameType) && isBigInput) {
					this.stState_Big[j].ctProgress = new CCounter(0, 9, 20, OpenTaiko.Timer);
					this.stState_Big[j].judge = judge;
					this.stState_Big[j].nPlayer = player;
					this.stState[j].IsBig = this.stState_Big[j].IsBig = true;
				} else {
					this.stState[j].IsBig = false;
				}
				break;
			}
		}
	}

	// CActivity 実装

	public override void Activate() {
		for (int i = 0; i < 3 * 4; i++) {
			this.stState[i].ctProgress = new CCounter();
			this.stState[i].bUse = false;
			this.stState_Big[i].ctProgress = new CCounter();
		}
		for (int i = 0; i < 256; i++) {
			this.stConfetti[i] = new STConfetti();
			this.stConfetti[i].bUse = false;
			this.stConfetti[i].ctProgress = new CCounter();
		}
		base.Activate();
	}
	public override void DeActivate() {
		for (int i = 0; i < 3 * 4; i++) {
			this.stState[i].ctProgress = null;
			this.stState_Big[i].ctProgress = null;
		}
		for (int i = 0; i < 256; i++) {
			this.stConfetti[i].ctProgress = null;
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
		if (!base.IsDeActivated) {
			int nWidth = (OpenTaiko.Tx.Effects_Hit_Explosion.szTextureSize.Width / 7);
			int nHeight = (OpenTaiko.Tx.Effects_Hit_Explosion.szTextureSize.Height / 4);
			int nBombWidth = (OpenTaiko.Tx.Effects_Hit_Bomb.szTextureSize.Width / 7);
			int nBombHeight = (OpenTaiko.Tx.Effects_Hit_Bomb.szTextureSize.Height / 4);
			for (int i = 0; i < 3 * 4; i++) {
				ref STSTATUS state = ref this.stState[i];
				if (state.bUse) {
					if (!state.ctProgress.IsStopped) {
						state.ctProgress.Tick();
						if (state.ctProgress.IsEnded) {
							state.ctProgress.Stop();
							state.bUse = false;
						}

						// (When performing calibration, reduce visual distraction
						// and current judgment feedback near the judgment position.)
						if (OpenTaiko.Tx.Effects_Hit_Explosion != null) {
							int n = state.IsBig ? (nHeight * 2) : 0;

							int nX = 0;
							int nY = 0;

							if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
								nX = OpenTaiko.Skin.Game_Effects_Hit_Explosion_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * state.nPlayer);
								nY = OpenTaiko.Skin.Game_Effects_Hit_Explosion_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * state.nPlayer);
							} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
								nX = OpenTaiko.Skin.Game_Effects_Hit_Explosion_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * state.nPlayer);
								nY = OpenTaiko.Skin.Game_Effects_Hit_Explosion_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * state.nPlayer);
							} else {
								nX = OpenTaiko.Skin.Game_Effects_Hit_Explosion_X[state.nPlayer];
								nY = OpenTaiko.Skin.Game_Effects_Hit_Explosion_Y[state.nPlayer];
							}
							nX += OpenTaiko.stageGameScreen.GetJPOSCROLLX(state.nPlayer);
							nY += OpenTaiko.stageGameScreen.GetJPOSCROLLY(state.nPlayer);

							switch (stState[i].judge) {
								case ENoteJudge.Perfect:
								case ENoteJudge.Great:
								case ENoteJudge.Auto:
									if (!OpenTaiko.ConfigIni.SimpleMode) OpenTaiko.Tx.Effects_Hit_Explosion.t2DDraw(nX, nY, new Rectangle(state.ctProgress.CurrentValue * nWidth, n, nWidth, nHeight));
									break;
								case ENoteJudge.Good:
									OpenTaiko.Tx.Effects_Hit_Explosion.t2DDraw(nX, nY, new Rectangle(state.ctProgress.CurrentValue * nWidth, n + nHeight, nWidth, nHeight));
									break;
								case ENoteJudge.Mine:
									OpenTaiko.Tx.Effects_Hit_Bomb?.t2DDraw(nX, nY, new Rectangle(state.ctProgress.CurrentValue * nBombWidth, 0, nBombWidth, nBombHeight));
									break;
								case ENoteJudge.Miss:
								case ENoteJudge.Bad:
									break;
							}
						}
					}
				}
			}

			for (int i = 0; i < 3 * 4; i++) {
				ref STSTATUS_B state = ref this.stState_Big[i];
				if (!state.ctProgress.IsStopped) {
					state.ctProgress.Tick();
					if (state.ctProgress.IsEnded) {
						state.ctProgress.Stop();
					}
					if (OpenTaiko.Tx.Effects_Hit_Explosion_Big != null && state.IsBig) {

						switch (state.judge) {
							case ENoteJudge.Perfect:
							case ENoteJudge.Great:
							case ENoteJudge.Auto: // Great color
							case ENoteJudge.Good: // OK color
								if (state.IsBig && !OpenTaiko.ConfigIni.SimpleMode) {
									//float fX = 415 - ((TJAPlayer3.Tx.Effects_Hit_Explosion_Big.sz画像サイズ.Width * TJAPlayer3.Tx.Effects_Hit_Explosion_Big.vc拡大縮小倍率.X ) / 2.0f);
									//float fY = TJAPlayer3.Skin.nJudgePointY[ this.st状態_大[ i ].nPlayer ] - ((TJAPlayer3.Tx.Effects_Hit_Explosion_Big.sz画像サイズ.Height * TJAPlayer3.Tx.Effects_Hit_Explosion_Big.vc拡大縮小倍率.Y ) / 2.0f);
									//float fY = 257 - ((this.txアタックエフェクトUpper_big.sz画像サイズ.Height * this.txアタックエフェクトUpper_big.vc拡大縮小倍率.Y ) / 2.0f);

									////7
									float fScale = 0.5f + (state.ctProgress.CurrentValue * 0.5f / 10.0f);
									//this.txアタックエフェクトUpper_big.vc拡大縮小倍率.X = f倍率;
									//this.txアタックエフェクトUpper_big.vc拡大縮小倍率.Y = f倍率;
									//this.txアタックエフェクトUpper_big.n透明度 = (int)(255 * f倍率);
									//this.txアタックエフェクトUpper_big.t2D描画( CDTXMania.app.Device, fX, fY );

									float x = 0;
									float y = 0;

									if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
										x = OpenTaiko.Skin.Game_Effects_Hit_Explosion_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * state.nPlayer);
										y = OpenTaiko.Skin.Game_Effects_Hit_Explosion_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * state.nPlayer);
									} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
										x = OpenTaiko.Skin.Game_Effects_Hit_Explosion_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * state.nPlayer);
										y = OpenTaiko.Skin.Game_Effects_Hit_Explosion_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * state.nPlayer);
									} else {
										x = OpenTaiko.Skin.Game_Effects_Hit_Explosion_X[state.nPlayer];
										y = OpenTaiko.Skin.Game_Effects_Hit_Explosion_Y[state.nPlayer];
									}
									x += OpenTaiko.stageGameScreen.GetJPOSCROLLX(state.nPlayer);
									y += OpenTaiko.stageGameScreen.GetJPOSCROLLY(state.nPlayer);

									x -= (OpenTaiko.Tx.Effects_Hit_Explosion_Big.szTextureSize.Width * (fScale - 1.0f) / 2.0f);
									y -= (OpenTaiko.Tx.Effects_Hit_Explosion_Big.szTextureSize.Height * (fScale - 1.0f) / 2.0f);

									if (state.judge is ENoteJudge.Good) // TODO: add Explosion_Big for 可/OK
										OpenTaiko.Tx.Effects_Hit_Explosion_Big.color4 = new Color4(4.0f, 4.0f, 4.0f, 1.0f); // HACK: made whiter
									else
										OpenTaiko.Tx.Effects_Hit_Explosion_Big.color4 = new Color4(1.0f, 1.0f, 1.0f, 1.0f);

									OpenTaiko.Tx.Effects_Hit_Explosion_Big.vcScaleRatio.X = fScale;
									OpenTaiko.Tx.Effects_Hit_Explosion_Big.vcScaleRatio.Y = fScale;
									OpenTaiko.Tx.Effects_Hit_Explosion_Big.t2DDraw(x, y);
								}
								break;

							case ENoteJudge.Miss:
							case ENoteJudge.Bad:
								break;
						}
					}
				}
			}

			for (int i = 0; i < 45; i++) {
				if (OpenTaiko.Skin.nScrollFieldX[0] != 414)
					break;

				if (this.stBigNoteFirework[i].bUse) {
					this.stBigNoteFirework[i].nPreviousValue = this.stBigNoteFirework[i].ctProgress.CurrentValue;
					this.stBigNoteFirework[i].ctProgress.Tick();
					if (this.stBigNoteFirework[i].ctProgress.IsEnded) {
						this.stBigNoteFirework[i].ctProgress.Stop();
						this.stBigNoteFirework[i].bUse = false;
					}
					/*
                    Matrix mat = Matrix.Identity;

                    mat *= Matrix.Translation(this.st大音符花火[i].fX - SampleFramework.GameWindowSize.Width / 2, -(this.st大音符花火[i].fY - SampleFramework.GameWindowSize.Height / 2), 0f);
                    */
					float fX = this.stBigNoteFirework[i].fX - (192 / 2);
					float fY = this.stBigNoteFirework[i].fY - (192 / 2);

					//if(CDTXMania.Tx.Effects_Hit_FireWorks[ 0 ] != null && this.st大音符花火[ i ].nColor == 0 )
					//{
					//    if( this.st大音符花火[ i ].n開始フレーム <= this.st大音符花火[ i ].ct進行.n現在の値 && this.st大音符花火[ i ].n終了フレーム > this.st大音符花火[ i ].ct進行.n現在の値 )
					//    {
					//        //this.tx大音符花火[ 0 ].t3D描画(CDTXMania.app.Device, mat, new Rectangle( ( this.st大音符花火[i].ct進行.n現在の値 - this.st大音符花火[ i ].n開始フレーム ) * 192, 0, 192, 192 ));
					//        //this.tx大音符花火[ 0 ].t3D描画( CDTXMania.app.Device, mat, fX, fY, new Rectangle( ( this.st大音符花火[i].ct進行.n現在の値 - this.st大音符花火[ i ].n開始フレーム ) * 192, 0, 192, 192 ) );
					//        CDTXMania.Tx.Effects_Hit_FireWorks[ 0 ].t2D描画( CDTXMania.app.Device, (int)fX, (int)fY, new Rectangle( ( this.st大音符花火[i].ct進行.n現在の値 - this.st大音符花火[ i ].n開始フレーム ) * 192, 0, 192, 192 ) );
					//    }
					//}
					////if(CDTXMania.Tx.Effects_Hit_FireWorks[ 1 ] != null && this.st大音符花火[ i ].nColor == 1 )
					//{
					//    if( this.st大音符花火[ i ].n開始フレーム <= this.st大音符花火[ i ].ct進行.n現在の値 && this.st大音符花火[ i ].n終了フレーム > this.st大音符花火[ i ].ct進行.n現在の値 )
					//    {
					//        //this.tx大音符花火[ 1 ].t3D描画( CDTXMania.app.Device, mat, fX, fY, );
					//        //CDTXMania.Tx.Effects_Hit_FireWorks[ 1 ].t2D描画( CDTXMania.app.Device, (int)fX, (int)fY, new Rectangle( ( this.st大音符花火[i].ct進行.n現在の値 - this.st大音符花火[ i ].n開始フレーム ) * 192, 0, 192, 192 ) );
					//    }
					//}
				}

			}

			for (int i = 0; i < 256; i++) {
				if (this.stConfetti[i].bUse) {
					this.stConfetti[i].nPreviousValue = this.stConfetti[i].ctProgress.CurrentValue;
					this.stConfetti[i].ctProgress.Tick();
					if (this.stConfetti[i].ctProgress.IsEnded) {
						this.stConfetti[i].ctProgress.Stop();
						this.stConfetti[i].bUse = false;
					} else if (this.stConfetti[i].fX > 1300 || this.stConfetti[i].fX < -20) {
						this.stConfetti[i].ctProgress.Stop();
						this.stConfetti[i].bUse = false;
					}
					for (int n = this.stConfetti[i].nPreviousValue; n < this.stConfetti[i].ctProgress.CurrentValue; n++) {
						this.stConfetti[i].fX -= this.stConfetti[i].fAccelerationX;
						this.stConfetti[i].fY -= this.stConfetti[i].fAccelerationY;
						this.stConfetti[i].fAccelerationX *= this.stConfetti[i].fAccelerationAccelerationX;
						this.stConfetti[i].fAccelerationY *= this.stConfetti[i].fAccelerationAccelerationY;
						this.stConfetti[i].fAccelerationY -= this.stConfetti[i].fGravityAcceleration;
					}
					/*
                    Matrix mat = Matrix.Identity;

                    float x = (float)(this.st紙吹雪[i].f半径 * Math.Cos((Math.PI / 2 * this.st紙吹雪[i].ct進行.n現在の値) / 100.0)) * 2.3f;
                    mat *= Matrix.Scaling(x, x, 1f);
                    mat *= Matrix.Translation(this.st紙吹雪[i].fX - SampleFramework.GameWindowSize.Width / 2, -(this.st紙吹雪[i].fY - SampleFramework.GameWindowSize.Height / 2), 0f);
                    */

					/*if (this.tx紙吹雪 != null)
                    {
                        this.tx紙吹雪.t3D描画(CDTXMania.app.Device, mat, new Rectangle( 32 * this.st紙吹雪[ i ].nGraphic, 32 * this.st紙吹雪[ i ].nColor, 32, 32 ));

                    } */
				}

			}
		}
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	//private CTextureAf txアタックエフェクトUpper;
	//private CTexture txアタックエフェクトUpper_big;
	//private CTextureAf[] tx大音符花火 = new CTextureAf[2];
	//private CTexture tx紙吹雪;

	protected STSTATUS[] stState = new STSTATUS[3 * 4];
	protected STSTATUS_B[] stState_Big = new STSTATUS_B[3 * 4];
	private STBigNoteFirework[] stBigNoteFirework = new STBigNoteFirework[45];

	protected int[] nXCoord = new int[] { 450, 521, 596, 686, 778, 863, 970, 1070, 1150 };
	protected int[] nYCoord = new int[] { 172, 108, 50, 8, -10, -60, -5, 30, 90 };
	protected int[] nYCoordP2 = new int[] { 172, 108, 50, 8, -10, -60, -5, 30, 90 };

	[StructLayout(LayoutKind.Sequential)]
	protected struct STSTATUS {
		public bool bUse;
		public CCounter ctProgress;
		public ENoteJudge judge;
		public bool IsBig;
		public int nOpacity;
		public int nPlayer;
	}
	[StructLayout(LayoutKind.Sequential)]
	protected struct STSTATUS_B {
		public CCounter ctProgress;
		public ENoteJudge judge;
		public bool IsBig;
		public int nOpacity;
		public int nPlayer;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct STBigNoteFirework {
		public bool bUse;
		public CCounter ctProgress;
		public int nPreviousValue;
		public float fX;
		public float fY;
		public int nStartFrame;
		public int nEndFrame;
	}

	private STConfetti[] stConfetti = new STConfetti[256];
	[StructLayout(LayoutKind.Sequential)]
	private struct STConfetti {
		public int nGraphic;
		public int nColor;
		public bool bUse;
		public CCounter ctProgress;
		public int nPreviousValue;
		public float fX;
		public float fY;
		public float fAccelerationX;
		public float fAccelerationY;
		public float fAccelerationAccelerationX;
		public float fAccelerationAccelerationY;
		public float fGravityAcceleration;
		public float fRadius;
		public float fAngle;
	}
	//-----------------
	#endregion
}
