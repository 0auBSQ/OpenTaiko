using FDK;

namespace OpenTaiko {
	internal class CActImplLane : CActivity {
		public CActImplLane() {
			base.IsDeActivated = true;
		}

		public override void Activate() {
			this.ct分岐アニメ進行 = new CCounter[5];
			this.nBefore = new CDTX.ECourse[5];
			this.nAfter = new CDTX.ECourse[5];
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				this.ct分岐アニメ進行[i] = new CCounter();
				this.nBefore = new CDTX.ECourse[5];
				this.nAfter = new CDTX.ECourse[5];
				this.bState[i] = false;
			}
			if (OpenTaiko.Tx.Lane_Base[0] != null)
				OpenTaiko.Tx.Lane_Base[0].Opacity = 255;

			base.Activate();
		}

		public override void DeActivate() {
			OpenTaiko.tDisposeSafely(ref this.ct分岐アニメ進行);
			base.DeActivate();
		}

		public override void CreateManagedResource() {
			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {


			base.ReleaseManagedResource();
		}

		public override int Draw() {
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (!this.ct分岐アニメ進行[i].IsStoped) {
					this.ct分岐アニメ進行[i].Tick();
					if (this.ct分岐アニメ進行[i].IsEnded) {
						this.bState[i] = false;
						this.ct分岐アニメ進行[i].Stop();
					}
				}
			}

			int[] x = new int[5];
			int[] y = new int[5];

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
					x[i] = OpenTaiko.Skin.Game_Lane_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * i);
					y[i] = OpenTaiko.Skin.Game_Lane_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * i);
				} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
					x[i] = OpenTaiko.Skin.Game_Lane_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * i);
					y[i] = OpenTaiko.Skin.Game_Lane_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * i);
				} else {
					x[i] = OpenTaiko.Skin.Game_Lane_X[i];
					y[i] = OpenTaiko.Skin.Game_Lane_Y[i];
				}
			}

			//アニメーション中の分岐レイヤー(背景)の描画を行う。
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (OpenTaiko.stage演奏ドラム画面.bUseBranch[i] == true) {

					#region NullCheck

					bool _laneNull = false;

					for (int j = 0; j < OpenTaiko.Tx.Lane_Base.Length; j++) {
						if (OpenTaiko.Tx.Lane_Base[j] == null) {
							_laneNull = true;
							break;
						}
					}

					#endregion

					if (OpenTaiko.ConfigIni.SimpleMode) {
						OpenTaiko.Tx.Lane_Base[(int)nAfter[i]].t2D描画(x[i], y[i]);
					} else if (this.ct分岐アニメ進行[i].IsTicked && !_laneNull) {
						#region[ 普通譜面_レベルアップ ]
						//普通→玄人
						if (nBefore[i] == 0 && nAfter[i] == CDTX.ECourse.eNormal) {
							OpenTaiko.Tx.Lane_Base[1].Opacity = this.ct分岐アニメ進行[i].CurrentValue > 100 ? 255 : ((this.ct分岐アニメ進行[i].CurrentValue * 0xff) / 100);
							OpenTaiko.Tx.Lane_Base[0].t2D描画(x[i], y[i]);
							OpenTaiko.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
						}
						//普通→達人
						if (nBefore[i] == 0 && nAfter[i] == CDTX.ECourse.eMaster) {
							OpenTaiko.Tx.Lane_Base[0].t2D描画(x[i], y[i]);
							if (this.ct分岐アニメ進行[i].CurrentValue < 100) {
								OpenTaiko.Tx.Lane_Base[1].Opacity = this.ct分岐アニメ進行[i].CurrentValue > 100 ? 255 : ((this.ct分岐アニメ進行[i].CurrentValue * 0xff) / 100);
								OpenTaiko.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
							} else if (this.ct分岐アニメ進行[i].CurrentValue >= 100 && this.ct分岐アニメ進行[i].CurrentValue < 150) {
								OpenTaiko.Tx.Lane_Base[1].Opacity = 255;
								OpenTaiko.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
							} else if (this.ct分岐アニメ進行[i].CurrentValue >= 150) {
								OpenTaiko.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
								OpenTaiko.Tx.Lane_Base[2].Opacity = this.ct分岐アニメ進行[i].CurrentValue > 250 ? 255 : (((this.ct分岐アニメ進行[i].CurrentValue - 150) * 0xff) / 100);
								OpenTaiko.Tx.Lane_Base[2].t2D描画(x[i], y[i]);
							}
						}
						#endregion

						#region[ 玄人譜面_レベルアップ ]
						if (nBefore[i] == CDTX.ECourse.eExpert && nAfter[i] == CDTX.ECourse.eMaster) {
							OpenTaiko.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
							OpenTaiko.Tx.Lane_Base[2].Opacity = this.ct分岐アニメ進行[i].CurrentValue > 100 ? 255 : ((this.ct分岐アニメ進行[i].CurrentValue * 0xff) / 100);
							OpenTaiko.Tx.Lane_Base[2].t2D描画(x[i], y[i]);
						}
						#endregion

						#region[ 玄人譜面_レベルダウン ]
						if (nBefore[i] == CDTX.ECourse.eExpert && nAfter[i] == CDTX.ECourse.eNormal) {
							OpenTaiko.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
							OpenTaiko.Tx.Lane_Base[0].Opacity = this.ct分岐アニメ進行[i].CurrentValue > 100 ? 255 : ((this.ct分岐アニメ進行[i].CurrentValue * 0xff) / 100);
							OpenTaiko.Tx.Lane_Base[0].t2D描画(x[i], y[i]);
						}
						#endregion

						#region[ 達人譜面_レベルダウン ]
						if (nBefore[i] == CDTX.ECourse.eMaster && nAfter[i] == CDTX.ECourse.eNormal) {
							OpenTaiko.Tx.Lane_Base[2].t2D描画(x[i], y[i]);
							if (this.ct分岐アニメ進行[i].CurrentValue < 100) {
								OpenTaiko.Tx.Lane_Base[1].Opacity = this.ct分岐アニメ進行[i].CurrentValue > 100 ? 255 : ((this.ct分岐アニメ進行[i].CurrentValue * 0xff) / 100);
								OpenTaiko.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
							} else if (this.ct分岐アニメ進行[i].CurrentValue >= 100 && this.ct分岐アニメ進行[i].CurrentValue < 150) {
								OpenTaiko.Tx.Lane_Base[1].Opacity = 255;
								OpenTaiko.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
							} else if (this.ct分岐アニメ進行[i].CurrentValue >= 150) {
								OpenTaiko.Tx.Lane_Base[1].t2D描画(x[i], y[i]);
								OpenTaiko.Tx.Lane_Base[0].Opacity = this.ct分岐アニメ進行[i].CurrentValue > 250 ? 255 : (((this.ct分岐アニメ進行[i].CurrentValue - 150) * 0xff) / 100);
								OpenTaiko.Tx.Lane_Base[0].t2D描画(x[i], y[i]);
							}
						}
						if (nBefore[i] == CDTX.ECourse.eMaster && nAfter[i] == CDTX.ECourse.eExpert) {
							OpenTaiko.Tx.Lane_Base[2].t2D描画(x[i], y[i]);
							OpenTaiko.Tx.Lane_Base[2].Opacity = this.ct分岐アニメ進行[i].CurrentValue > 100 ? 255 : ((this.ct分岐アニメ進行[i].CurrentValue * 0xff) / 100);
							OpenTaiko.Tx.Lane_Base[2].t2D描画(x[i], y[i]);
						}
						#endregion
					}
				}
			}
			return base.Draw();
		}

		public virtual void t分岐レイヤー_コース変化(CDTX.ECourse n現在, CDTX.ECourse n次回, int player) {
			if (n現在 == n次回) {
				return;
			}
			this.ct分岐アニメ進行[player] = new CCounter(0, 300, 2, OpenTaiko.Timer);
			this.bState[player] = true;

			this.nBefore[player] = n現在;
			this.nAfter[player] = n次回;

		}

		#region[ private ]
		//-----------------
		public bool[] bState = new bool[5];
		public CCounter[] ct分岐アニメ進行 = new CCounter[5];
		private CDTX.ECourse[] nBefore;
		private CDTX.ECourse[] nAfter;
		private int[] n透明度 = new int[5];
		//private CTexture[] tx普通譜面 = new CTexture[2];
		//private CTexture[] tx玄人譜面 = new CTexture[2];
		//private CTexture[] tx達人譜面 = new CTexture[2];
		//-----------------
		#endregion
	}
}
