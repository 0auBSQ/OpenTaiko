using System.Runtime.InteropServices;
using FDK;
using Rectangle = System.Drawing.Rectangle;

namespace TJAPlayer3 {
	internal class CActImplLaneTaiko : CActivity {
		/// <summary>
		/// レーンを描画するクラス。
		/// 
		/// 
		/// </summary>
		public CActImplLaneTaiko() {
			base.IsDeActivated = true;
		}

		public override void Activate() {
			for (int i = 0; i < 5; i++) {
				this.st状態[i].ct進行 = new CCounter();
				this.stBranch[i].ct分岐アニメ進行 = new CCounter();
				this.stBranch[i].nフラッシュ制御タイマ = -1;
				this.stBranch[i].nBranchレイヤー透明度 = 0;
				this.stBranch[i].nBranch文字透明度 = 0;
				this.stBranch[i].nY座標 = 0;

				this.n総移動時間[i] = -1;
			}
			this.ctゴーゴー = new CCounter();


			this.ctゴーゴー炎 = new CCounter(0, 6, 50, TJAPlayer3.Timer);
			base.Activate();
		}

		public override void DeActivate() {
			for (int i = 0; i < 5; i++) {
				this.st状態[i].ct進行 = null;
				this.stBranch[i].ct分岐アニメ進行 = null;
			}
			this.ctゴーゴー = null;

			base.DeActivate();
		}

		public override void CreateManagedResource() {
			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {
			base.ReleaseManagedResource();
		}

		public override int Draw() {
			if (base.IsFirstDraw) {
				for (int i = 0; i < 5; i++)
					this.stBranch[i].nフラッシュ制御タイマ = (long)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed);
				base.IsFirstDraw = false;
			}

			//それぞれが独立したレイヤーでないといけないのでforループはパーツごとに分離すること。

			if (TJAPlayer3.ConfigIni.nPlayerCount <= 2 && !TJAPlayer3.ConfigIni.bAIBattleMode) TJAPlayer3.stage演奏ドラム画面.actMtaiko.DrawBackSymbol();

			#region[ レーン本体 ]


			int[] x = new int[5];
			int[] y = new int[5];

			for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++) {
				if (TJAPlayer3.ConfigIni.nPlayerCount == 5) {
					x[i] = TJAPlayer3.Skin.Game_Lane_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * i);
					y[i] = TJAPlayer3.Skin.Game_Lane_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * i);
				} else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3) {
					x[i] = TJAPlayer3.Skin.Game_Lane_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * i);
					y[i] = TJAPlayer3.Skin.Game_Lane_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * i);
				} else {
					x[i] = TJAPlayer3.Skin.Game_Lane_X[i];
					y[i] = TJAPlayer3.Skin.Game_Lane_Y[i];
				}
			}

			for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++) {
				if (i == 1 && TJAPlayer3.ConfigIni.bAIBattleMode && TJAPlayer3.Tx.Lane_Background_AI != null)
					TJAPlayer3.Tx.Lane_Background_AI?.t2D描画(x[i], y[i]);
				else
					TJAPlayer3.Tx.Lane_Background_Main?.t2D描画(x[i], y[i]);
			}

			#endregion

			if (TJAPlayer3.ConfigIni.nPlayerCount > 2 && !TJAPlayer3.ConfigIni.bAIBattleMode) TJAPlayer3.stage演奏ドラム画面.actMtaiko.DrawBackSymbol();

			for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++) {
				#region[ 分岐アニメ制御タイマー ]
				long num = FDK.SoundManager.PlayTimer.NowTime;
				if (num < this.stBranch[i].nフラッシュ制御タイマ) {
					this.stBranch[i].nフラッシュ制御タイマ = num;
				}
				while ((num - this.stBranch[i].nフラッシュ制御タイマ) >= 30) {
					if (this.stBranch[i].nBranchレイヤー透明度 <= 255) {
						this.stBranch[i].nBranchレイヤー透明度 += 10;
					}

					if (this.stBranch[i].nBranch文字透明度 >= 0) {
						this.stBranch[i].nBranch文字透明度 -= 10;
					}

					if (this.stBranch[i].nY座標 != 0 && this.stBranch[i].nY座標 <= 20) {
						this.stBranch[i].nY座標++;
					}

					this.stBranch[i].nフラッシュ制御タイマ += 8;
				}

				if (!this.stBranch[i].ct分岐アニメ進行.IsStoped) {
					this.stBranch[i].ct分岐アニメ進行.Tick();
					if (this.stBranch[i].ct分岐アニメ進行.IsEnded) {
						this.stBranch[i].ct分岐アニメ進行.Stop();
					}
				}
				#endregion
			}
			#region[ 分岐レイヤー ]
			for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++) {
				if (TJAPlayer3.stage演奏ドラム画面.bUseBranch[i] == true) {
					#region[ 動いていない ]
					switch (TJAPlayer3.stage演奏ドラム画面.nレーン用表示コース[i]) {
						case CDTX.ECourse.eNormal:
							if (TJAPlayer3.Tx.Lane_Base[0] != null) {
								TJAPlayer3.Tx.Lane_Base[0].Opacity = 255;
								TJAPlayer3.Tx.Lane_Base[0].t2D描画(x[i], y[i]);
							}
							break;
						case CDTX.ECourse.eExpert:
							if (TJAPlayer3.Tx.Lane_Base[1] != null) {
								TJAPlayer3.Tx.Lane_Base[1].Opacity = 255;
								TJAPlayer3.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
							}
							break;
						case CDTX.ECourse.eMaster:
							if (TJAPlayer3.Tx.Lane_Base[2] != null) {
								TJAPlayer3.Tx.Lane_Base[2].Opacity = 255;
								TJAPlayer3.Tx.Lane_Base[2].t2D描画(x[i], y[i]);
							}
							break;
					}
					#endregion

					if (TJAPlayer3.ConfigIni.nBranchAnime == 1) {
						#region[ AC7～14風の背後レイヤー ]
						if (this.stBranch[i].ct分岐アニメ進行.IsTicked) {
							int n透明度 = ((100 - this.stBranch[i].ct分岐アニメ進行.CurrentValue) * 0xff) / 100;

							if (this.stBranch[i].ct分岐アニメ進行.IsEnded) {
								n透明度 = 255;
								this.stBranch[i].ct分岐アニメ進行.Stop();
							}

							#region[ 普通譜面_レベルアップ ]
							//普通→玄人
							if (this.stBranch[i].nBefore == CDTX.ECourse.eNormal && this.stBranch[i].nAfter == CDTX.ECourse.eExpert) {
								if (TJAPlayer3.Tx.Lane_Base[0] != null && TJAPlayer3.Tx.Lane_Base[1] != null) {
									TJAPlayer3.Tx.Lane_Base[0].t2D描画(x[i], y[i]);
									TJAPlayer3.Tx.Lane_Base[1].Opacity = this.stBranch[i].nBranchレイヤー透明度;
									TJAPlayer3.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
								}
							}
							//普通→達人
							if (this.stBranch[i].nBefore == CDTX.ECourse.eNormal && this.stBranch[i].nAfter == CDTX.ECourse.eMaster) {
								if (this.stBranch[i].ct分岐アニメ進行.CurrentValue < 100) {
									n透明度 = ((100 - this.stBranch[i].ct分岐アニメ進行.CurrentValue) * 0xff) / 100;
								}
								if (TJAPlayer3.Tx.Lane_Base[0] != null && TJAPlayer3.Tx.Lane_Base[2] != null) {
									TJAPlayer3.Tx.Lane_Base[0].t2D描画(x[i], y[i]);
									TJAPlayer3.Tx.Lane_Base[2].t2D描画(x[i], y[i]);
									TJAPlayer3.Tx.Lane_Base[2].Opacity = this.stBranch[i].nBranchレイヤー透明度;
								}
							}
							#endregion
							#region[ 玄人譜面_レベルアップ ]
							if (this.stBranch[i].nBefore == CDTX.ECourse.eExpert && this.stBranch[i].nAfter == CDTX.ECourse.eMaster) {
								if (TJAPlayer3.Tx.Lane_Base[1] != null && TJAPlayer3.Tx.Lane_Base[2] != null) {
									TJAPlayer3.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
									TJAPlayer3.Tx.Lane_Base[2].t2D描画(x[i], y[i]);
									TJAPlayer3.Tx.Lane_Base[2].Opacity = this.stBranch[i].nBranchレイヤー透明度;
								}
							}
							#endregion
							#region[ 玄人譜面_レベルダウン ]
							if (this.stBranch[i].nBefore == CDTX.ECourse.eExpert && this.stBranch[i].nAfter == CDTX.ECourse.eNormal) {
								if (TJAPlayer3.Tx.Lane_Base[1] != null && TJAPlayer3.Tx.Lane_Base[0] != null) {
									TJAPlayer3.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
									TJAPlayer3.Tx.Lane_Base[0].t2D描画(x[i], y[i]);
									TJAPlayer3.Tx.Lane_Base[0].Opacity = this.stBranch[i].nBranchレイヤー透明度;
								}
							}
							#endregion
							#region[ 達人譜面_レベルダウン ]
							if (this.stBranch[i].nBefore == CDTX.ECourse.eMaster && this.stBranch[i].nAfter == CDTX.ECourse.eNormal) {
								if (TJAPlayer3.Tx.Lane_Base[2] != null && TJAPlayer3.Tx.Lane_Base[0] != null) {
									TJAPlayer3.Tx.Lane_Base[2].t2D描画(x[i], y[i]);
									TJAPlayer3.Tx.Lane_Base[0].t2D描画(x[i], y[i]);
									TJAPlayer3.Tx.Lane_Base[0].Opacity = this.stBranch[i].nBranchレイヤー透明度;
								}
							}
							#endregion
						}
						#endregion
					} else if (TJAPlayer3.ConfigIni.nBranchAnime == 0) {
						TJAPlayer3.stage演奏ドラム画面.actLane.Draw();
					}
				}
			}
			#endregion

			for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++) {
				#region[ ゴーゴータイムレーン背景レイヤー ]
				if (TJAPlayer3.Tx.Lane_Background_GoGo != null && TJAPlayer3.stage演奏ドラム画面.bIsGOGOTIME[i]) {
					if (!this.ctゴーゴー.IsStoped) {
						this.ctゴーゴー.Tick();
					}

					if (this.ctゴーゴー.CurrentValue <= 4) {
						TJAPlayer3.Tx.Lane_Background_GoGo.vcScaleRatio.Y = 0.2f;
						TJAPlayer3.Tx.Lane_Background_GoGo.t2D描画(x[i], y[i] + 54);
					} else if (this.ctゴーゴー.CurrentValue <= 5) {
						TJAPlayer3.Tx.Lane_Background_GoGo.vcScaleRatio.Y = 0.4f;
						TJAPlayer3.Tx.Lane_Background_GoGo.t2D描画(x[i], y[i] + 40);
					} else if (this.ctゴーゴー.CurrentValue <= 6) {
						TJAPlayer3.Tx.Lane_Background_GoGo.vcScaleRatio.Y = 0.6f;
						TJAPlayer3.Tx.Lane_Background_GoGo.t2D描画(x[i], y[i] + 26);
					} else if (this.ctゴーゴー.CurrentValue <= 8) {
						TJAPlayer3.Tx.Lane_Background_GoGo.vcScaleRatio.Y = 0.8f;
						TJAPlayer3.Tx.Lane_Background_GoGo.t2D描画(x[i], y[i] + 13);
					} else if (this.ctゴーゴー.CurrentValue >= 9) {
						TJAPlayer3.Tx.Lane_Background_GoGo.vcScaleRatio.Y = 1.0f;
						TJAPlayer3.Tx.Lane_Background_GoGo.t2D描画(x[i], y[i]);
					}
				}
				#endregion
			}

			for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++) {
				if (TJAPlayer3.stage演奏ドラム画面.bUseBranch[i] == true) {
					#region NullCheck

					bool _laneNull = false;

					for (int j = 0; j < TJAPlayer3.Tx.Lane_Text.Length; j++) {
						if (TJAPlayer3.Tx.Lane_Text[j] == null) {
							_laneNull = true;
							break;
						}
					}

					#endregion

					if (TJAPlayer3.ConfigIni.SimpleMode) {
						switch (TJAPlayer3.stage演奏ドラム画面.nレーン用表示コース[i]) {
							case CDTX.ECourse.eNormal:
								TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], y[i]);
								break;
							case CDTX.ECourse.eExpert:
								TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i]);
								break;
							case CDTX.ECourse.eMaster:
								TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], y[i]);
								break;
						}
					} else if (TJAPlayer3.ConfigIni.nBranchAnime == 0 && !_laneNull) {
						if (!this.stBranch[i].ct分岐アニメ進行.IsTicked) {
							switch (TJAPlayer3.stage演奏ドラム画面.nレーン用表示コース[i]) {
								case CDTX.ECourse.eNormal:
									TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
									TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], y[i]);
									break;
								case CDTX.ECourse.eExpert:
									TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i]);
									break;
								case CDTX.ECourse.eMaster:
									TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
									TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], y[i]);
									break;
							}
						}
						if (this.stBranch[i].ct分岐アニメ進行.IsTicked) {
							#region[ 普通譜面_レベルアップ ]
							//普通→玄人
							if (this.stBranch[i].nBefore == 0 && this.stBranch[i].nAfter == CDTX.ECourse.eExpert) {
								TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;

								TJAPlayer3.Tx.Lane_Text[0].Opacity = this.stBranch[i].ct分岐アニメ進行.CurrentValue > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.CurrentValue * 0xff) / 60));
								//CDTXMania.Tx.Lane_Text[1].n透明度 = this.ct分岐アニメ進行.n現在の値 > 100 ? 255 : ( ( ( this.ct分岐アニメ進行.n現在の値 * 0xff ) / 60 ) );
								if (this.stBranch[i].ct分岐アニメ進行.CurrentValue < 60) {
									this.stBranch[i].nY = this.stBranch[i].ct分岐アニメ進行.CurrentValue / 2;
									TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], y[i] + this.stBranch[i].nY);
									TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], (y[i] - 30) + this.stBranch[i].nY);
								} else {
									TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i]);
								}

							}

							//普通→達人
							if (this.stBranch[i].nBefore == 0 && this.stBranch[i].nAfter == CDTX.ECourse.eMaster) {
								TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
								if (this.stBranch[i].ct分岐アニメ進行.CurrentValue < 60) {
									this.stBranch[i].nY = this.stBranch[i].ct分岐アニメ進行.CurrentValue / 2;
									TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], (y[i] - 12) + this.stBranch[i].nY);
									TJAPlayer3.Tx.Lane_Text[0].Opacity = this.stBranch[i].ct分岐アニメ進行.CurrentValue > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.CurrentValue * 0xff) / 100));
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], (y[i] - 20) + this.stBranch[i].nY);
								}
								//if( this.stBranch[ i ].ct分岐アニメ進行.n現在の値 >= 5 && this.stBranch[ i ].ct分岐アニメ進行.n現在の値 < 60 )
								//{
								//    this.stBranch[ i ].nY = this.stBranch[ i ].ct分岐アニメ進行.n現在の値 / 2;
								//    this.tx普通譜面[ 1 ].t2D描画(CDTXMania.app.Device, 333, CDTXMania.Skin.nScrollFieldY[ i ] + this.stBranch[ i ].nY);
								//    this.tx普通譜面[ 1 ].n透明度 = this.stBranch[ i ].ct分岐アニメ進行.n現在の値 > 100 ? 0 : ( 255 - ( ( this.stBranch[ i ].ct分岐アニメ進行.n現在の値 * 0xff) / 100));
								//    this.tx玄人譜面[ 1 ].t2D描画(CDTXMania.app.Device, 333, ( CDTXMania.Skin.nScrollFieldY[ i ] - 10 ) + this.stBranch[ i ].nY);
								//}
								else if (this.stBranch[i].ct分岐アニメ進行.CurrentValue >= 60 && this.stBranch[i].ct分岐アニメ進行.CurrentValue < 150) {
									this.stBranch[i].nY = 21;
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i]);
									TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
									TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
								} else if (this.stBranch[i].ct分岐アニメ進行.CurrentValue >= 150 && this.stBranch[i].ct分岐アニメ進行.CurrentValue < 210) {
									this.stBranch[i].nY = ((this.stBranch[i].ct分岐アニメ進行.CurrentValue - 150) / 2);
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i] + this.stBranch[i].nY);
									TJAPlayer3.Tx.Lane_Text[1].Opacity = this.stBranch[i].ct分岐アニメ進行.CurrentValue > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.CurrentValue * 0xff) / 100));
									TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], (y[i] - 20) + this.stBranch[i].nY);
								} else {
									TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
									TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], y[i]);
								}
							}
							#endregion
							#region[ 玄人譜面_レベルアップ ]
							//玄人→達人
							if (this.stBranch[i].nBefore == CDTX.ECourse.eExpert && this.stBranch[i].nAfter == CDTX.ECourse.eMaster) {
								TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;

								TJAPlayer3.Tx.Lane_Text[1].Opacity = this.stBranch[i].ct分岐アニメ進行.CurrentValue > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.CurrentValue * 0xff) / 60));
								if (this.stBranch[i].ct分岐アニメ進行.CurrentValue < 60) {
									this.stBranch[i].nY = this.stBranch[i].ct分岐アニメ進行.CurrentValue / 2;
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i] + this.stBranch[i].nY);
									TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], (y[i] - 20) + this.stBranch[i].nY);
								} else {
									TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], y[i]);
								}
							}
							#endregion
							#region[ 玄人譜面_レベルダウン ]
							if (this.stBranch[i].nBefore == CDTX.ECourse.eExpert && this.stBranch[i].nAfter == CDTX.ECourse.eNormal) {
								TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;

								TJAPlayer3.Tx.Lane_Text[1].Opacity = this.stBranch[i].ct分岐アニメ進行.CurrentValue > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.CurrentValue * 0xff) / 60));
								if (this.stBranch[i].ct分岐アニメ進行.CurrentValue < 60) {
									this.stBranch[i].nY = this.stBranch[i].ct分岐アニメ進行.CurrentValue / 2;
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i] - this.stBranch[i].nY);
									TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], (y[i] + 30) - this.stBranch[i].nY);
								} else {
									TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], y[i]);
								}
							}
							#endregion
							#region[ 達人譜面_レベルダウン ]
							if (this.stBranch[i].nBefore == CDTX.ECourse.eMaster && this.stBranch[i].nAfter == CDTX.ECourse.eNormal) {
								TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;

								if (this.stBranch[i].ct分岐アニメ進行.CurrentValue < 60) {
									this.stBranch[i].nY = this.stBranch[i].ct分岐アニメ進行.CurrentValue / 2;
									TJAPlayer3.Tx.Lane_Text[2].Opacity = this.stBranch[i].ct分岐アニメ進行.CurrentValue > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.CurrentValue * 0xff) / 60));
									TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], y[i] - this.stBranch[i].nY);
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], (y[i] + 30) - this.stBranch[i].nY);
								} else if (this.stBranch[i].ct分岐アニメ進行.CurrentValue >= 60 && this.stBranch[i].ct分岐アニメ進行.CurrentValue < 150) {
									this.stBranch[i].nY = 21;
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i]);
									TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
									TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
								} else if (this.stBranch[i].ct分岐アニメ進行.CurrentValue >= 150 && this.stBranch[i].ct分岐アニメ進行.CurrentValue < 210) {
									this.stBranch[i].nY = ((this.stBranch[i].ct分岐アニメ進行.CurrentValue - 150) / 2);
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i] - this.stBranch[i].nY);
									TJAPlayer3.Tx.Lane_Text[1].Opacity = this.stBranch[i].ct分岐アニメ進行.CurrentValue > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.CurrentValue * 0xff) / 100));
									TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], (y[i] + 30) - this.stBranch[i].nY);
								} else if (this.stBranch[i].ct分岐アニメ進行.CurrentValue >= 210) {
									TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
									TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], y[i]);
								}
							}
							if (this.stBranch[i].nBefore == CDTX.ECourse.eMaster && this.stBranch[i].nAfter == CDTX.ECourse.eExpert) {
								TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
								TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;

								TJAPlayer3.Tx.Lane_Text[2].Opacity = this.stBranch[i].ct分岐アニメ進行.CurrentValue > 100 ? 0 : (255 - ((this.stBranch[i].ct分岐アニメ進行.CurrentValue * 0xff) / 60));
								if (this.stBranch[i].ct分岐アニメ進行.CurrentValue < 60) {
									this.stBranch[i].nY = this.stBranch[i].ct分岐アニメ進行.CurrentValue / 2;
									TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], y[i] - this.stBranch[i].nY);
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], (y[i] + 30) - this.stBranch[i].nY);
								} else {
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i]);
								}

							}
							#endregion
						}
					} else if (!_laneNull) {
						if (this.stBranch[i].nY座標 == 21) {
							this.stBranch[i].nY座標 = 0;
						}

						if (this.stBranch[i].nY座標 == 0) {
							switch (TJAPlayer3.stage演奏ドラム画面.nレーン用表示コース[i]) {
								case CDTX.ECourse.eNormal:
									TJAPlayer3.Tx.Lane_Text[0].Opacity = 255;
									TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], y[i]);
									break;
								case CDTX.ECourse.eExpert:
									TJAPlayer3.Tx.Lane_Text[1].Opacity = 255;
									TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i]);
									break;
								case CDTX.ECourse.eMaster:
									TJAPlayer3.Tx.Lane_Text[2].Opacity = 255;
									TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], y[i]);
									break;
							}
						}

						if (this.stBranch[i].nY座標 != 0) {
							#region[ 普通譜面_レベルアップ ]
							//普通→玄人
							if (this.stBranch[i].nBefore == CDTX.ECourse.eNormal && this.stBranch[i].nAfter == CDTX.ECourse.eExpert) {
								TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], y[i] - this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], (y[i] + 20) - this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[0].Opacity = this.stBranch[i].nBranchレイヤー透明度;
							}
							//普通→達人
							if (this.stBranch[i].nBefore == CDTX.ECourse.eNormal && this.stBranch[i].nAfter == CDTX.ECourse.eMaster) {
								TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], y[i] - this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], (y[i] + 20) - this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[0].Opacity = this.stBranch[i].nBranchレイヤー透明度;
							}
							#endregion
							#region[ 玄人譜面_レベルアップ ]
							//玄人→達人
							if (this.stBranch[i].nBefore == CDTX.ECourse.eExpert && this.stBranch[i].nAfter == CDTX.ECourse.eMaster) {
								TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i] - this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], (y[i] + 20) - this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[1].Opacity = this.stBranch[i].nBranchレイヤー透明度;
							}
							#endregion
							#region[ 玄人譜面_レベルダウン ]
							if (this.stBranch[i].nBefore == CDTX.ECourse.eExpert && this.stBranch[i].nAfter == CDTX.ECourse.eNormal) {
								TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], y[i] + this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], (y[i] - 24) + this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[1].Opacity = this.stBranch[i].nBranchレイヤー透明度;
							}
							#endregion
							#region[ 達人譜面_レベルダウン ]
							if (this.stBranch[i].nBefore == CDTX.ECourse.eMaster && this.stBranch[i].nAfter == CDTX.ECourse.eNormal) {
								TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], y[i] + this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[0].t2D描画(x[i], (y[i] - 24) + this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[2].Opacity = this.stBranch[i].nBranchレイヤー透明度;
							}
							if (this.stBranch[i].nBefore == CDTX.ECourse.eMaster && this.stBranch[i].nAfter == CDTX.ECourse.eExpert) {
								TJAPlayer3.Tx.Lane_Text[2].t2D描画(x[i], y[i] + this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[1].t2D描画(x[i], (y[i] - 24) + this.stBranch[i].nY座標);
								TJAPlayer3.Tx.Lane_Text[2].Opacity = this.stBranch[i].nBranchレイヤー透明度;
							}
							#endregion
						}
					}

				}
			}


			if (TJAPlayer3.ConfigIni.nPlayerCount <= 2) {
				if (TJAPlayer3.Tx.Lane_Background_Sub != null) {
					TJAPlayer3.Tx.Lane_Background_Sub.t2D描画(TJAPlayer3.Skin.Game_Lane_Sub_X[0], TJAPlayer3.Skin.Game_Lane_Sub_Y[0]);
					if (TJAPlayer3.stage演奏ドラム画面.bDoublePlay) {
						TJAPlayer3.Tx.Lane_Background_Sub.t2D描画(TJAPlayer3.Skin.Game_Lane_Sub_X[1], TJAPlayer3.Skin.Game_Lane_Sub_Y[1]);
					}
				}
			}


			TJAPlayer3.stage演奏ドラム画面.actTaikoLaneFlash.Draw();



			if (TJAPlayer3.Tx.Taiko_Frame[0] != null) {
				// Tower frame (without tamashii jauge) if playing a tower chart
				for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++) {
					int frame_x;
					int frame_y;
					if (TJAPlayer3.ConfigIni.nPlayerCount == 5) {
						frame_x = TJAPlayer3.Skin.Game_Taiko_Frame_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * i);
						frame_y = TJAPlayer3.Skin.Game_Taiko_Frame_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * i);
					} else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3) {
						frame_x = TJAPlayer3.Skin.Game_Taiko_Frame_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * i);
						frame_y = TJAPlayer3.Skin.Game_Taiko_Frame_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * i);
					} else {
						frame_x = TJAPlayer3.Skin.Game_Taiko_Frame_X[i];
						frame_y = TJAPlayer3.Skin.Game_Taiko_Frame_Y[i];
					}

					CTexture tex = null;

					switch (i) {
						case 0: {
								if (TJAPlayer3.ConfigIni.bTokkunMode) {
									tex = TJAPlayer3.Tx.Taiko_Frame[3];
								} else if (TJAPlayer3.ConfigIni.bAIBattleMode) {
									tex = TJAPlayer3.Tx.Taiko_Frame[5];
								} else if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
									tex = TJAPlayer3.Tx.Taiko_Frame[2];
								} else if (TJAPlayer3.ConfigIni.nPlayerCount > 2) {
									tex = TJAPlayer3.Tx.Taiko_Frame[6];
								} else {
									tex = TJAPlayer3.Tx.Taiko_Frame[0];
								}
							}
							break;
						case 1: {
								if (TJAPlayer3.ConfigIni.bAIBattleMode) {
									tex = TJAPlayer3.Tx.Taiko_Frame[4];
								} else if (TJAPlayer3.ConfigIni.nPlayerCount > 2) {
									tex = TJAPlayer3.Tx.Taiko_Frame[6];
								} else {
									tex = TJAPlayer3.Tx.Taiko_Frame[1];
								}
							}
							break;
						case 2:
							tex = TJAPlayer3.Tx.Taiko_Frame[6];
							break;
						case 3:
							tex = TJAPlayer3.Tx.Taiko_Frame[6];
							break;
						case 4:
							tex = TJAPlayer3.Tx.Taiko_Frame[6];
							break;
					}

					tex?.t2D描画(frame_x, frame_y);
				}

				/*
                if (TJAPlayer3.ConfigIni.bTokkunMode == true && TJAPlayer3.Tx.Taiko_Frame[3] != null)
                    TJAPlayer3.Tx.Taiko_Frame[3]?.t2D描画(TJAPlayer3.Skin.Game_Taiko_Frame_X[0], TJAPlayer3.Skin.Game_Taiko_Frame_Y[0]);
                else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower && TJAPlayer3.Tx.Taiko_Frame[2] != null)
                    TJAPlayer3.Tx.Taiko_Frame[2]?.t2D描画(TJAPlayer3.Skin.Game_Taiko_Frame_X[0], TJAPlayer3.Skin.Game_Taiko_Frame_Y[0]);
                else if (TJAPlayer3.ConfigIni.bAIBattleMode && TJAPlayer3.Tx.Taiko_Frame[5] != null)
                    TJAPlayer3.Tx.Taiko_Frame[5]?.t2D描画(TJAPlayer3.Skin.Game_Taiko_Frame_X[0], TJAPlayer3.Skin.Game_Taiko_Frame_Y[0]);
                else
                    TJAPlayer3.Tx.Taiko_Frame[0]?.t2D描画(TJAPlayer3.Skin.Game_Taiko_Frame_X[0], TJAPlayer3.Skin.Game_Taiko_Frame_Y[0]);

                if (TJAPlayer3.stage演奏ドラム画面.bDoublePlay)
                {
                    if (TJAPlayer3.ConfigIni.bAIBattleMode)
                        TJAPlayer3.Tx.Taiko_Frame[4]?.t2D描画(TJAPlayer3.Skin.Game_Taiko_Frame_X[1], TJAPlayer3.Skin.Game_Taiko_Frame_Y[1]);
                    else
                        TJAPlayer3.Tx.Taiko_Frame[1]?.t2D描画(TJAPlayer3.Skin.Game_Taiko_Frame_X[1], TJAPlayer3.Skin.Game_Taiko_Frame_Y[1]);
                }
                */
			}
			var nTime = (long)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed);

			for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++) {
				if (this.n総移動時間[i] != -1) {
					if (n移動方向[i] == 1) {
						TJAPlayer3.stage演奏ドラム画面.JPOSCROLLX[i] = this.n移動開始X[i] + (int)((((int)nTime - this.n移動開始時刻[i]) / (double)(this.n総移動時間[i])) * this.n移動距離px[i]);
						TJAPlayer3.stage演奏ドラム画面.JPOSCROLLY[i] = this.n移動開始Y[i] + (int)((((int)nTime - this.n移動開始時刻[i]) / (double)(this.n総移動時間[i])) * this.nVerticalJSPos[i]);
						//TJAPlayer3.stage演奏ドラム画面.FlyingNotes.StartPointX[i] = this.n移動開始X[i] + (int)((((int)nTime - this.n移動開始時刻[i]) / (double)(this.n総移動時間[i])) * this.n移動距離px[i]);
					} else {
						TJAPlayer3.stage演奏ドラム画面.JPOSCROLLX[i] = this.n移動開始X[i] - (int)((((int)nTime - this.n移動開始時刻[i]) / (double)(this.n総移動時間[i])) * this.n移動距離px[i]);
						TJAPlayer3.stage演奏ドラム画面.JPOSCROLLY[i] = this.n移動開始Y[i] - (int)((((int)nTime - this.n移動開始時刻[i]) / (double)(this.n総移動時間[i])) * this.nVerticalJSPos[i]);
						//TJAPlayer3.stage演奏ドラム画面.FlyingNotes.StartPointX[i] = this.n移動開始X[i] - (int)((((int)nTime - this.n移動開始時刻[i]) / (double)(this.n総移動時間[i])) * this.n移動距離px[i]);
					}

					if (((int)nTime) > this.n移動開始時刻[i] + this.n総移動時間[i]) {
						this.n総移動時間[i] = -1;
						TJAPlayer3.stage演奏ドラム画面.JPOSCROLLX[i] = this.n移動目的場所X[i];
						TJAPlayer3.stage演奏ドラム画面.JPOSCROLLY[i] = this.n移動目的場所Y[i];
						//TJAPlayer3.stage演奏ドラム画面.FlyingNotes.StartPointX[i] = this.n移動目的場所X[i];
					}
				}
			}




			if (TJAPlayer3.ConfigIni.bEnableAVI && TJAPlayer3.DTX.listVD.Count > 0 && TJAPlayer3.stage演奏ドラム画面.ShowVideo) {
				if (TJAPlayer3.Tx.Lane_Background_Main != null) TJAPlayer3.Tx.Lane_Background_Main.Opacity = TJAPlayer3.ConfigIni.nBGAlpha;
				if (TJAPlayer3.Tx.Lane_Background_AI != null) TJAPlayer3.Tx.Lane_Background_AI.Opacity = TJAPlayer3.ConfigIni.nBGAlpha;
				if (TJAPlayer3.Tx.Lane_Background_Sub != null) TJAPlayer3.Tx.Lane_Background_Sub.Opacity = TJAPlayer3.ConfigIni.nBGAlpha;
				if (TJAPlayer3.Tx.Lane_Background_GoGo != null) TJAPlayer3.Tx.Lane_Background_GoGo.Opacity = TJAPlayer3.ConfigIni.nBGAlpha;
			} else {
				if (TJAPlayer3.Tx.Lane_Background_Main != null) TJAPlayer3.Tx.Lane_Background_Main.Opacity = 255;
				if (TJAPlayer3.Tx.Lane_Background_AI != null) TJAPlayer3.Tx.Lane_Background_AI.Opacity = 255;
				if (TJAPlayer3.Tx.Lane_Background_Sub != null) TJAPlayer3.Tx.Lane_Background_Sub.Opacity = 255;
				if (TJAPlayer3.Tx.Lane_Background_GoGo != null) TJAPlayer3.Tx.Lane_Background_GoGo.Opacity = 255;
			}

			return base.Draw();
		}

		public void ゴーゴー炎() {
			//判定枠
			if (TJAPlayer3.Tx.Judge_Frame != null) {
				TJAPlayer3.Tx.Judge_Frame.b加算合成 = TJAPlayer3.Skin.Game_JudgeFrame_AddBlend;
				for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++) {
					TJAPlayer3.Tx.Judge_Frame.t2D描画(
						TJAPlayer3.stage演奏ドラム画面.NoteOriginX[i],
						TJAPlayer3.stage演奏ドラム画面.NoteOriginY[i], new Rectangle(0, 0, TJAPlayer3.Skin.Game_Notes_Size[0], TJAPlayer3.Skin.Game_Notes_Size[1]));
				}
			}


			#region[ ゴーゴー炎 ]
			for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++) {
				if (TJAPlayer3.stage演奏ドラム画面.bIsGOGOTIME[i] && !TJAPlayer3.ConfigIni.SimpleMode) {
					this.ctゴーゴー炎.TickLoop();

					if (TJAPlayer3.Tx.Effects_Fire != null) {
						float f倍率 = 1.0f;

						float[] ar倍率 = new float[] { 0.8f, 1.2f, 1.7f, 2.5f, 2.3f, 2.2f, 2.0f, 1.8f, 1.7f, 1.6f, 1.6f, 1.5f, 1.5f, 1.4f, 1.3f, 1.2f, 1.1f, 1.0f };

						f倍率 = ar倍率[this.ctゴーゴー.CurrentValue];

						/*
                        Matrix mat = Matrix.Identity;
                        mat *= Matrix.Scaling(f倍率, f倍率, 1.0f);
                        mat *= Matrix.Translation(TJAPlayer3.Skin.nScrollFieldX[i] - SampleFramework.GameWindowSize.Width / 2.0f, -(TJAPlayer3.Skin.nJudgePointY[i] - SampleFramework.GameWindowSize.Height / 2.0f), 0f);
                        */
						//this.txゴーゴー炎.b加算合成 = true;

						//this.ctゴーゴー.n現在の値 = 6;

						int width = TJAPlayer3.Tx.Effects_Fire.szTextureSize.Width / 7;
						int height = TJAPlayer3.Tx.Effects_Fire.szTextureSize.Height;

						float x = -(width * (f倍率 - 1.0f) / 2.0f);
						float y = -(height * (f倍率 - 1.0f) / 2.0f);

						if (TJAPlayer3.ConfigIni.nPlayerCount == 5) {
							x += TJAPlayer3.Skin.Game_Effect_Fire_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * i);
							y += TJAPlayer3.Skin.Game_Effect_Fire_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * i);
						} else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3) {
							x += TJAPlayer3.Skin.Game_Effect_Fire_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * i);
							y += TJAPlayer3.Skin.Game_Effect_Fire_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * i);
						} else {
							x += TJAPlayer3.Skin.Game_Effect_Fire_X[i];
							y += TJAPlayer3.Skin.Game_Effect_Fire_Y[i];
						}

						TJAPlayer3.Tx.Effects_Fire.vcScaleRatio.X = f倍率;
						TJAPlayer3.Tx.Effects_Fire.vcScaleRatio.Y = f倍率;

						TJAPlayer3.Tx.Effects_Fire.t2D描画(x, y,
							new Rectangle(width * (this.ctゴーゴー炎.CurrentValue), 0, width, height));
					}
				}
			}
			#endregion
			for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++) {
				if (!this.st状態[i].ct進行.IsStoped) {
					this.st状態[i].ct進行.Tick();
					if (this.st状態[i].ct進行.IsEnded) {
						this.st状態[i].ct進行.Stop();
					}
					//if( this.txアタックエフェクトLower != null )
					{
						//this.txアタックエフェクトLower.b加算合成 = true;
						int n = this.st状態[i].nIsBig == 1 ? 520 : 0;

						float x = 0;
						float y = 0;

						if (TJAPlayer3.ConfigIni.nPlayerCount == 5) {
							x = TJAPlayer3.Skin.Game_Effects_Hit_Explosion_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * i);
							y = TJAPlayer3.Skin.Game_Effects_Hit_Explosion_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * i);
						} else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3) {
							x = TJAPlayer3.Skin.Game_Effects_Hit_Explosion_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * i);
							y = TJAPlayer3.Skin.Game_Effects_Hit_Explosion_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * i);
						} else {
							x = TJAPlayer3.Skin.Game_Effects_Hit_Explosion_X[i];
							y = TJAPlayer3.Skin.Game_Effects_Hit_Explosion_Y[i];
						}
						x += TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLX(i);
						y += TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLY(i);

						switch (st状態[i].judge) {
							case ENoteJudge.Perfect:
							case ENoteJudge.Great:
							case ENoteJudge.Auto:
								if (!TJAPlayer3.ConfigIni.SimpleMode) {
									//this.txアタックエフェクトLower.t2D描画( CDTXMania.app.Device, 285, 127, new Rectangle( this.st状態[ i ].ct進行.n現在の値 * 260, n, 260, 260 ) );
									if (this.st状態[i].nIsBig == 1 && TJAPlayer3.Tx.Effects_Hit_Great_Big[this.st状態[i].ct進行.CurrentValue] != null)
										TJAPlayer3.Tx.Effects_Hit_Great_Big[this.st状態[i].ct進行.CurrentValue].t2D描画(x, y);
									else if (TJAPlayer3.Tx.Effects_Hit_Great[this.st状態[i].ct進行.CurrentValue] != null)
										TJAPlayer3.Tx.Effects_Hit_Great[this.st状態[i].ct進行.CurrentValue].t2D描画(x, y);
								}
								break;

							case ENoteJudge.Good:
								//this.txアタックエフェクトLower.t2D描画( CDTXMania.app.Device, 285, 127, new Rectangle( this.st状態[ i ].ct進行.n現在の値 * 260, n + 260, 260, 260 ) );
								if (this.st状態[i].nIsBig == 1 && TJAPlayer3.Tx.Effects_Hit_Good_Big[this.st状態[i].ct進行.CurrentValue] != null)
									TJAPlayer3.Tx.Effects_Hit_Good_Big[this.st状態[i].ct進行.CurrentValue].t2D描画(x, y);
								else if (TJAPlayer3.Tx.Effects_Hit_Good[this.st状態[i].ct進行.CurrentValue] != null)
									TJAPlayer3.Tx.Effects_Hit_Good[this.st状態[i].ct進行.CurrentValue].t2D描画(x, y);
								break;

							case ENoteJudge.Miss:
							case ENoteJudge.Bad:
								break;
						}
					}
				}
			}


		}

		public virtual void Start(int nLane, ENoteJudge judge, bool b両手入力, int nPlayer) {
			//2017.08.15 kairera0467 排他なので番地をそのまま各レーンの状態として扱う

			//for( int n = 0; n < 1; n++ )
			{
				this.st状態[nPlayer].ct進行 = new CCounter(0, 14, 20, TJAPlayer3.Timer);
				this.st状態[nPlayer].judge = judge;
				this.st状態[nPlayer].nPlayer = nPlayer;

				switch (nLane) {
					case 0x11:
					case 0x12:
						this.st状態[nPlayer].nIsBig = 0;
						break;
					case 0x13:
					case 0x14:
					case 0x1A:
					case 0x1B: {
							if (b両手入力)
								this.st状態[nPlayer].nIsBig = 1;
							else
								this.st状態[nPlayer].nIsBig = 0;
						}
						break;
				}
			}
		}


		public void GOGOSTART() {
			this.ctゴーゴー = new CCounter(0, 17, 18, TJAPlayer3.Timer);
			if (TJAPlayer3.ConfigIni.nPlayerCount == 1 && TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) TJAPlayer3.stage演奏ドラム画面.GoGoSplash.StartSplash();
		}


		public void t分岐レイヤー_コース変化(CDTX.ECourse n現在, CDTX.ECourse n次回, int nPlayer) {
			if (n現在 == n次回) {
				return;
			}
			this.stBranch[nPlayer].ct分岐アニメ進行 = new CCounter(0, 300, 2, TJAPlayer3.Timer);

			this.stBranch[nPlayer].nBranchレイヤー透明度 = 6;
			this.stBranch[nPlayer].nY座標 = 1;

			this.stBranch[nPlayer].nBefore = n現在;
			this.stBranch[nPlayer].nAfter = n次回;

			TJAPlayer3.stage演奏ドラム画面.actLane.t分岐レイヤー_コース変化(n現在, n次回, nPlayer);
		}

		public void t判定枠移動(double db移動時間, int n移動px, int n移動方向, int nPlayer, int vJs) {
			this.n移動開始時刻[nPlayer] = (int)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed);
			this.n移動開始X[nPlayer] = TJAPlayer3.stage演奏ドラム画面.JPOSCROLLX[nPlayer];
			this.n移動開始Y[nPlayer] = TJAPlayer3.stage演奏ドラム画面.JPOSCROLLY[nPlayer];
			this.n総移動時間[nPlayer] = (int)(db移動時間 * 1000);
			this.n移動方向[nPlayer] = n移動方向;
			this.n移動距離px[nPlayer] = n移動px;
			this.nVerticalJSPos[nPlayer] = vJs;
			if (n移動方向 == 0) {
				this.n移動目的場所X[nPlayer] = TJAPlayer3.stage演奏ドラム画面.JPOSCROLLX[nPlayer] - n移動px;
				this.n移動目的場所Y[nPlayer] = TJAPlayer3.stage演奏ドラム画面.JPOSCROLLY[nPlayer] - vJs;
			} else {
				this.n移動目的場所X[nPlayer] = TJAPlayer3.stage演奏ドラム画面.JPOSCROLLX[nPlayer] + n移動px;
				this.n移動目的場所Y[nPlayer] = TJAPlayer3.stage演奏ドラム画面.JPOSCROLLY[nPlayer] + vJs;
			}
		}

		#region[ private ]
		//-----------------
		//private CTexture txLane;
		//private CTexture txLaneB;
		//private CTexture tx枠線;
		//private CTexture tx判定枠;
		//private CTexture txゴーゴー;
		//private CTexture txゴーゴー炎;
		//private CTexture[] txArゴーゴー炎;
		//private CTexture[] txArアタックエフェクトLower_A;
		//private CTexture[] txArアタックエフェクトLower_B;
		//private CTexture[] txArアタックエフェクトLower_C;
		//private CTexture[] txArアタックエフェクトLower_D;

		//private CTexture[] txLaneFlush = new CTexture[3];

		//private CTexture[] tx普通譜面 = new CTexture[2];
		//private CTexture[] tx玄人譜面 = new CTexture[2];
		//private CTexture[] tx達人譜面 = new CTexture[2];

		//private CTextureAf txアタックエフェクトLower;

		protected STSTATUS[] st状態 = new STSTATUS[5];

		//private CTexture[] txゴーゴースプラッシュ;

		[StructLayout(LayoutKind.Sequential)]
		protected struct STSTATUS {
			public bool b使用中;
			public CCounter ct進行;
			public ENoteJudge judge;
			public int nIsBig;
			public int n透明度;
			public int nPlayer;
		}
		private CCounter ctゴーゴー;
		private CCounter ctゴーゴー炎;



		public STBRANCH[] stBranch = new STBRANCH[5];
		[StructLayout(LayoutKind.Sequential)]
		public struct STBRANCH {
			public CCounter ct分岐アニメ進行;
			public CDTX.ECourse nBefore;
			public CDTX.ECourse nAfter;

			public long nフラッシュ制御タイマ;
			public int nBranchレイヤー透明度;
			public int nBranch文字透明度;
			public int nY座標;
			public int nY;
		}


		private int[] n総移動時間 = new int[5];
		private int[] n移動開始X = new int[5];
		private int[] n移動開始Y = new int[5];
		private int[] n移動開始時刻 = new int[5];
		private int[] n移動距離px = new int[5];
		private int[] nVerticalJSPos = new int[5];
		private int[] n移動目的場所X = new int[5];
		private int[] n移動目的場所Y = new int[5];
		private int[] n移動方向 = new int[5];

		//-----------------
		#endregion
	}
}
