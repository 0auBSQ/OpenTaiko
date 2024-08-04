using System.Runtime.InteropServices;
using FDK;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko {
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
		public virtual void Start(int nLane, int nPlayer) {
			nY座標P2 = new int[] { 548, 612, 670, 712, 730, 780, 725, 690, 640 };
			if (OpenTaiko.Tx.Effects_Hit_FireWorks != null && OpenTaiko.Tx.Effects_Hit_FireWorks != null) {
				for (int i = 0; i < 9; i++) {
					for (int j = 0; j < 45; j++) {
						if (!this.st大音符花火[j].b使用中) {
							this.st大音符花火[j].b使用中 = true;
							this.st大音符花火[j].ct進行 = new CCounter(0, 40, 18, OpenTaiko.Timer); // カウンタ
							this.st大音符花火[j].fX = this.nX座標[i]; //X座標
							this.st大音符花火[j].fY = nPlayer == 0 ? this.nY座標[i] : this.nY座標P2[i];

							switch (nLane) {
								case 0:
									this.st大音符花火[j].nColor = 0;
									break;
								case 1:
									this.st大音符花火[j].nColor = 1;
									break;
							}

							switch (i) {
								case 0:
									this.st大音符花火[j].n開始フレーム = 0;
									this.st大音符花火[j].n終了フレーム = 16;
									break;
								case 1:
									this.st大音符花火[j].n開始フレーム = 3;
									this.st大音符花火[j].n終了フレーム = 19;
									break;
								case 2:
									this.st大音符花火[j].n開始フレーム = 6;
									this.st大音符花火[j].n終了フレーム = 22;
									break;
								case 3:
									this.st大音符花火[j].n開始フレーム = 9;
									this.st大音符花火[j].n終了フレーム = 25;
									break;
								case 4:
									this.st大音符花火[j].n開始フレーム = 12;
									this.st大音符花火[j].n終了フレーム = 28;
									break;
								case 5:
									this.st大音符花火[j].n開始フレーム = 15;
									this.st大音符花火[j].n終了フレーム = 31;
									break;
								case 6:
									this.st大音符花火[j].n開始フレーム = 18;
									this.st大音符花火[j].n終了フレーム = 34;
									break;
								case 7:
									this.st大音符花火[j].n開始フレーム = 21;
									this.st大音符花火[j].n終了フレーム = 37;
									break;
								case 8:
									this.st大音符花火[j].n開始フレーム = 24;
									this.st大音符花火[j].n終了フレーム = 40;
									break;
							}



							break;
						}
					}
				}
			}
		}

		public virtual void Start(int nLane, ENoteJudge judge, int player) {
			for (int j = 0; j < 3 * 4; j++) {
				if (!this.st状態[j].b使用中)
				//for( int n = 0; n < 1; n++ )
				{
					this.st状態[j].b使用中 = true;
					//this.st状態[ n ].ct進行 = new CCounter( 0, 9, 20, CDTXMania.Timer );
					this.st状態[j].ct進行 = new CCounter(0, 6, 25, OpenTaiko.Timer);
					this.st状態[j].judge = judge;
					this.st状態[j].nPlayer = player;
					this.st状態_大[j].nPlayer = player;

					switch (nLane) {
						case 0x11:
						case 0x12:
							this.st状態[j].nIsBig = 0;
							break;
						case 0x13:
						case 0x14:
						case 0x1A:
						case 0x1B:
							this.st状態_大[j].ct進行 = new CCounter(0, 9, 20, OpenTaiko.Timer);
							this.st状態_大[j].judge = judge;
							this.st状態_大[j].nIsBig = 1;
							break;
					}
					break;
				}
			}
		}

		// CActivity 実装

		public override void Activate() {
			for (int i = 0; i < 3 * 4; i++) {
				this.st状態[i].ct進行 = new CCounter();
				this.st状態[i].b使用中 = false;
				this.st状態_大[i].ct進行 = new CCounter();
			}
			for (int i = 0; i < 256; i++) {
				this.st紙吹雪[i] = new ST紙吹雪();
				this.st紙吹雪[i].b使用中 = false;
				this.st紙吹雪[i].ct進行 = new CCounter();
			}
			base.Activate();
		}
		public override void DeActivate() {
			for (int i = 0; i < 3 * 4; i++) {
				this.st状態[i].ct進行 = null;
				this.st状態_大[i].ct進行 = null;
			}
			for (int i = 0; i < 256; i++) {
				this.st紙吹雪[i].ct進行 = null;
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
					if (this.st状態[i].b使用中) {
						if (!this.st状態[i].ct進行.IsStoped) {
							this.st状態[i].ct進行.Tick();
							if (this.st状態[i].ct進行.IsEnded) {
								this.st状態[i].ct進行.Stop();
								this.st状態[i].b使用中 = false;
							}

							// (When performing calibration, reduce visual distraction
							// and current judgment feedback near the judgment position.)
							if (OpenTaiko.Tx.Effects_Hit_Explosion != null && !OpenTaiko.IsPerformingCalibration) {
								int n = this.st状態[i].nIsBig == 1 ? (nHeight * 2) : 0;

								int nX = 0;
								int nY = 0;

								if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
									nX = OpenTaiko.Skin.Game_Effects_Hit_Explosion_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * this.st状態[i].nPlayer);
									nY = OpenTaiko.Skin.Game_Effects_Hit_Explosion_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * this.st状態[i].nPlayer);
								} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
									nX = OpenTaiko.Skin.Game_Effects_Hit_Explosion_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * this.st状態[i].nPlayer);
									nY = OpenTaiko.Skin.Game_Effects_Hit_Explosion_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * this.st状態[i].nPlayer);
								} else {
									nX = OpenTaiko.Skin.Game_Effects_Hit_Explosion_X[this.st状態[i].nPlayer];
									nY = OpenTaiko.Skin.Game_Effects_Hit_Explosion_Y[this.st状態[i].nPlayer];
								}
								nX += OpenTaiko.stage演奏ドラム画面.GetJPOSCROLLX(this.st状態[i].nPlayer);
								nY += OpenTaiko.stage演奏ドラム画面.GetJPOSCROLLY(this.st状態[i].nPlayer);

								switch (st状態[i].judge) {
									case ENoteJudge.Perfect:
									case ENoteJudge.Great:
									case ENoteJudge.Auto:
										if (!OpenTaiko.ConfigIni.SimpleMode) OpenTaiko.Tx.Effects_Hit_Explosion.t2D描画(nX, nY, new Rectangle(this.st状態[i].ct進行.CurrentValue * nWidth, n, nWidth, nHeight));
										break;
									case ENoteJudge.Good:
										OpenTaiko.Tx.Effects_Hit_Explosion.t2D描画(nX, nY, new Rectangle(this.st状態[i].ct進行.CurrentValue * nWidth, n + nHeight, nWidth, nHeight));
										break;
									case ENoteJudge.Mine:
										OpenTaiko.Tx.Effects_Hit_Bomb?.t2D描画(nX, nY, new Rectangle(this.st状態[i].ct進行.CurrentValue * nBombWidth, 0, nBombWidth, nBombHeight));
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
					if (!this.st状態_大[i].ct進行.IsStoped) {
						this.st状態_大[i].ct進行.Tick();
						if (this.st状態_大[i].ct進行.IsEnded) {
							this.st状態_大[i].ct進行.Stop();
						}
						if (OpenTaiko.Tx.Effects_Hit_Explosion_Big != null && this.st状態_大[i].nIsBig == 1) {

							switch (st状態_大[i].judge) {
								case ENoteJudge.Perfect:
								case ENoteJudge.Great:
								case ENoteJudge.Auto:
									if (this.st状態_大[i].nIsBig == 1 && !OpenTaiko.ConfigIni.SimpleMode) {
										//float fX = 415 - ((TJAPlayer3.Tx.Effects_Hit_Explosion_Big.sz画像サイズ.Width * TJAPlayer3.Tx.Effects_Hit_Explosion_Big.vc拡大縮小倍率.X ) / 2.0f);
										//float fY = TJAPlayer3.Skin.nJudgePointY[ this.st状態_大[ i ].nPlayer ] - ((TJAPlayer3.Tx.Effects_Hit_Explosion_Big.sz画像サイズ.Height * TJAPlayer3.Tx.Effects_Hit_Explosion_Big.vc拡大縮小倍率.Y ) / 2.0f);
										//float fY = 257 - ((this.txアタックエフェクトUpper_big.sz画像サイズ.Height * this.txアタックエフェクトUpper_big.vc拡大縮小倍率.Y ) / 2.0f);

										////7
										float f倍率 = 0.5f + ((this.st状態_大[i].ct進行.CurrentValue * 0.5f) / 10.0f);
										//this.txアタックエフェクトUpper_big.vc拡大縮小倍率.X = f倍率;
										//this.txアタックエフェクトUpper_big.vc拡大縮小倍率.Y = f倍率;
										//this.txアタックエフェクトUpper_big.n透明度 = (int)(255 * f倍率);
										//this.txアタックエフェクトUpper_big.t2D描画( CDTXMania.app.Device, fX, fY );

										/*
                                        Matrix mat = Matrix.Identity;
                                        mat *= Matrix.Scaling( f倍率, f倍率, f倍率 );
                                        mat *= Matrix.Translation( TJAPlayer3.Skin.nScrollFieldX[0] - SampleFramework.GameWindowSize.Width / 2.0f, -(TJAPlayer3.Skin.nJudgePointY[ this.st状態[ i ].nPlayer ] - SampleFramework.GameWindowSize.Height / 2.0f), 0f );
                                        //mat *= Matrix.Billboard( new Vector3( 15, 15, 15 ), new Vector3(0, 0, 0), new Vector3( 0, 0, 0 ), new Vector3( 0, 0, 0 ) );
                                        //mat *= Matrix.Translation( 0f, 0f, 0f );


                                        TJAPlayer3.Tx.Effects_Hit_Explosion_Big.Opacity = 255;
                                        TJAPlayer3.Tx.Effects_Hit_Explosion_Big.t3D描画( mat );
                                        */

										float x = 0;
										float y = 0;

										if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
											x = OpenTaiko.Skin.Game_Effects_Hit_Explosion_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * this.st状態[i].nPlayer);
											y = OpenTaiko.Skin.Game_Effects_Hit_Explosion_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * this.st状態[i].nPlayer);
										} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
											x = OpenTaiko.Skin.Game_Effects_Hit_Explosion_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * this.st状態[i].nPlayer);
											y = OpenTaiko.Skin.Game_Effects_Hit_Explosion_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * this.st状態[i].nPlayer);
										} else {
											x = OpenTaiko.Skin.Game_Effects_Hit_Explosion_X[this.st状態[i].nPlayer];
											y = OpenTaiko.Skin.Game_Effects_Hit_Explosion_Y[this.st状態[i].nPlayer];
										}
										x += OpenTaiko.stage演奏ドラム画面.GetJPOSCROLLX(this.st状態[i].nPlayer);
										y += OpenTaiko.stage演奏ドラム画面.GetJPOSCROLLY(this.st状態[i].nPlayer);

										x -= (OpenTaiko.Tx.Effects_Hit_Explosion_Big.szTextureSize.Width * (f倍率 - 1.0f) / 2.0f);
										y -= (OpenTaiko.Tx.Effects_Hit_Explosion_Big.szTextureSize.Height * (f倍率 - 1.0f) / 2.0f);

										OpenTaiko.Tx.Effects_Hit_Explosion_Big.vcScaleRatio.X = f倍率;
										OpenTaiko.Tx.Effects_Hit_Explosion_Big.vcScaleRatio.Y = f倍率;
										OpenTaiko.Tx.Effects_Hit_Explosion_Big.t2D描画(x, y);
									}
									break;

								case ENoteJudge.Good:
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

					if (this.st大音符花火[i].b使用中) {
						this.st大音符花火[i].n前回のValue = this.st大音符花火[i].ct進行.CurrentValue;
						this.st大音符花火[i].ct進行.Tick();
						if (this.st大音符花火[i].ct進行.IsEnded) {
							this.st大音符花火[i].ct進行.Stop();
							this.st大音符花火[i].b使用中 = false;
						}
						/*
                        Matrix mat = Matrix.Identity;

                        mat *= Matrix.Translation(this.st大音符花火[i].fX - SampleFramework.GameWindowSize.Width / 2, -(this.st大音符花火[i].fY - SampleFramework.GameWindowSize.Height / 2), 0f);
                        */
						float fX = this.st大音符花火[i].fX - (192 / 2);
						float fY = this.st大音符花火[i].fY - (192 / 2);

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
					if (this.st紙吹雪[i].b使用中) {
						this.st紙吹雪[i].n前回のValue = this.st紙吹雪[i].ct進行.CurrentValue;
						this.st紙吹雪[i].ct進行.Tick();
						if (this.st紙吹雪[i].ct進行.IsEnded) {
							this.st紙吹雪[i].ct進行.Stop();
							this.st紙吹雪[i].b使用中 = false;
						} else if (this.st紙吹雪[i].fX > 1300 || this.st紙吹雪[i].fX < -20) {
							this.st紙吹雪[i].ct進行.Stop();
							this.st紙吹雪[i].b使用中 = false;
						}
						for (int n = this.st紙吹雪[i].n前回のValue; n < this.st紙吹雪[i].ct進行.CurrentValue; n++) {
							this.st紙吹雪[i].fX -= this.st紙吹雪[i].f加速度X;
							this.st紙吹雪[i].fY -= this.st紙吹雪[i].f加速度Y;
							this.st紙吹雪[i].f加速度X *= this.st紙吹雪[i].f加速度の加速度X;
							this.st紙吹雪[i].f加速度Y *= this.st紙吹雪[i].f加速度の加速度Y;
							this.st紙吹雪[i].f加速度Y -= this.st紙吹雪[i].f重力加速度;
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

		protected STSTATUS[] st状態 = new STSTATUS[3 * 4];
		protected STSTATUS_B[] st状態_大 = new STSTATUS_B[3 * 4];
		private ST大音符花火[] st大音符花火 = new ST大音符花火[45];

		protected int[] nX座標 = new int[] { 450, 521, 596, 686, 778, 863, 970, 1070, 1150 };
		protected int[] nY座標 = new int[] { 172, 108, 50, 8, -10, -60, -5, 30, 90 };
		protected int[] nY座標P2 = new int[] { 172, 108, 50, 8, -10, -60, -5, 30, 90 };

		[StructLayout(LayoutKind.Sequential)]
		protected struct STSTATUS {
			public bool b使用中;
			public CCounter ct進行;
			public ENoteJudge judge;
			public int nIsBig;
			public int n透明度;
			public int nPlayer;
		}
		[StructLayout(LayoutKind.Sequential)]
		protected struct STSTATUS_B {
			public CCounter ct進行;
			public ENoteJudge judge;
			public int nIsBig;
			public int n透明度;
			public int nPlayer;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct ST大音符花火 {
			public int nColor;
			public bool b使用中;
			public CCounter ct進行;
			public int n前回のValue;
			public float fX;
			public float fY;
			public int n開始フレーム;
			public int n終了フレーム;
		}

		private ST紙吹雪[] st紙吹雪 = new ST紙吹雪[256];
		[StructLayout(LayoutKind.Sequential)]
		private struct ST紙吹雪 {
			public int nGraphic;
			public int nColor;
			public bool b使用中;
			public CCounter ct進行;
			public int n前回のValue;
			public float fX;
			public float fY;
			public float f加速度X;
			public float f加速度Y;
			public float f加速度の加速度X;
			public float f加速度の加速度Y;
			public float f重力加速度;
			public float f半径;
			public float f角度;
		}
		//-----------------
		#endregion
	}
}

