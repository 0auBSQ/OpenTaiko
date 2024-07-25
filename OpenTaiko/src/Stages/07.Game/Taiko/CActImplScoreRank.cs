using FDK;

namespace TJAPlayer3 {
	class CActImplScoreRank : CActivity {
		public override void Activate() {
			double RollTimems = 0;

			/*
            foreach (var chip in TJAPlayer3.DTX.listChip)
            {
                if (NotesManager.IsRoll(chip))
                {
                    RollTimems += (chip.nノーツ終了時刻ms - chip.n発声時刻ms) / 1000.0;
                }
            }
            */

			for (int player = 0; player < 5; player++) {
				this.ScoreRank[player] = new int[] { 500000, 600000, 700000, 800000, 900000, 950000,
				Math.Max(1000000, (int)(TJAPlayer3.stage演奏ドラム画面.nAddScoreNiji[player] * TJAPlayer3.stage演奏ドラム画面.nNoteCount[player]) + (int)(TJAPlayer3.stage演奏ドラム画面.nBalloonCount[player] * 100) + (int)(Math.Ceiling(TJAPlayer3.stage演奏ドラム画面.nRollTimeMs[player] * 16.6 / 10) * 100 * 10)) };

				for (int i = 0; i < 7; i++) {
					this.counter[player][i] = new CCounter();
				}
			}
			base.Activate();
		}

		public override void DeActivate() {
			base.DeActivate();
		}

		public override void CreateManagedResource() {
			TowerResult_ScoreRankEffect = TJAPlayer3.tテクスチャの生成(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.TOWERRESULT}ScoreRankEffect.png"));

			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {

			TJAPlayer3.tDisposeSafely(ref TowerResult_ScoreRankEffect);

			base.ReleaseManagedResource();
		}

		private void displayScoreRank(int i, int player, float x, float y, int mode = 0) {
			CCounter cct = this.counter[player][i];

			CTexture tex = TJAPlayer3.Tx.ScoreRank;
			if (mode == 1) // tower
				tex = TowerResult_ScoreRankEffect;

			if (tex == null)
				return;

			if (!cct.IsTicked) {
				cct.Start(0, 3000, 1, TJAPlayer3.Timer);
			}
			if (cct.CurrentValue <= 255) {
				tex.Opacity = cct.CurrentValue;
				x = ((cct.CurrentValue / 255.0f) - 1.0f) * (player == 0 ? -TJAPlayer3.Skin.Game_Judge_Move[0] : TJAPlayer3.Skin.Game_Judge_Move[0]);
				y = ((cct.CurrentValue / 255.0f) - 1.0f) * (player == 0 ? -TJAPlayer3.Skin.Game_Judge_Move[1] : TJAPlayer3.Skin.Game_Judge_Move[1]);
			}
			if (cct.CurrentValue > 255 && cct.CurrentValue <= 255 + 180) {
				tex.Opacity = 255;

				float newSize = 1.0f + (float)Math.Sin((cct.CurrentValue - 255) * (Math.PI / 180)) * 0.2f;
				tex.vcScaleRatio.X = newSize;
				tex.vcScaleRatio.Y = newSize;
				x = 0;
				y = 0;
			}
			if (cct.CurrentValue > 255 + 180 && cct.CurrentValue <= 2745) {
				tex.Opacity = 255;
				tex.vcScaleRatio.X = 1.0f;
				tex.vcScaleRatio.Y = 1.0f;
				x = 0;
				y = 0;
			}
			if (cct.CurrentValue >= 2745 && cct.CurrentValue <= 3000) {
				tex.Opacity = 255 - ((cct.CurrentValue - 2745));
				x = ((cct.CurrentValue - 2745) / 255.0f) * (player == 0 || TJAPlayer3.ConfigIni.nPlayerCount >= 2 ? -TJAPlayer3.Skin.Game_Judge_Move[0] : TJAPlayer3.Skin.Game_Judge_Move[0]);
				y = ((cct.CurrentValue - 2745) / 255.0f) * (player == 0 || TJAPlayer3.ConfigIni.nPlayerCount >= 2 ? -TJAPlayer3.Skin.Game_Judge_Move[1] : TJAPlayer3.Skin.Game_Judge_Move[1]);
			}

			var xpos = 0;
			var ypos = 0;
			if (TJAPlayer3.ConfigIni.nPlayerCount == 5) {
				xpos = TJAPlayer3.Skin.Game_ScoreRank_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * player);
				ypos = TJAPlayer3.Skin.Game_ScoreRank_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * player);
			} else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3) {
				xpos = TJAPlayer3.Skin.Game_ScoreRank_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * player);
				ypos = TJAPlayer3.Skin.Game_ScoreRank_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * player);
			} else {
				xpos = TJAPlayer3.Skin.Game_ScoreRank_X[player];
				ypos = TJAPlayer3.Skin.Game_ScoreRank_Y[player];
			}
			xpos += (int)x;
			ypos += (int)y;

			int width;
			int height;

			switch (mode) {
				case 1:
					width = tex.szTextureSize.Width / 7;
					height = tex.szTextureSize.Height;
					break;
				default:
					width = tex.szTextureSize.Width;
					height = tex.szTextureSize.Height / 7;
					break;
			}

			if (mode == 0)
				tex.t2D拡大率考慮中央基準描画(xpos, ypos, new System.Drawing.Rectangle(0, height * i, width, height));
			else if (mode == 1 && player == 0)
				tex.t2D拡大率考慮中央基準描画(xpos, ypos, new System.Drawing.Rectangle(width * i, 0, width, height));
		}

		public override int Draw() {
			if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) {
				float x = 0;
				float y = 0;

				for (int i = 0; i < 7; i++) {
					if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Tower) {

						for (int player = 0; player < 5; player++) {
							#region [Ensou score ranks]

							counter[player][i].Tick();
							if (TJAPlayer3.stage演奏ドラム画面.actScore.GetScore(player) >= ScoreRank[player][i]) {
								displayScoreRank(i, player, x, y);

								#region [Legacy]

								/*
                                if (!this.counter[i].b進行中)
                                {
                                    this.counter[i].t開始(0, 3000, 1, TJAPlayer3.Timer);
                                }
                                if (counter[i].n現在の値 <= 255)
                                {
                                    TJAPlayer3.Tx.ScoreRank.Opacity = counter[i].n現在の値;
                                    x = 51 - (counter[i].n現在の値 / 5.0f);
                                }
                                if (counter[i].n現在の値 > 255 && counter[i].n現在の値 <= 255 + 180)
                                {
                                    TJAPlayer3.Tx.ScoreRank.Opacity = 255;
                                    TJAPlayer3.Tx.ScoreRank.vc拡大縮小倍率.X = 1.0f + (float)Math.Sin((counter[i].n現在の値 - 255) * (Math.PI / 180)) * 0.2f;
                                    TJAPlayer3.Tx.ScoreRank.vc拡大縮小倍率.Y = 1.0f + (float)Math.Sin((counter[i].n現在の値 - 255) * (Math.PI / 180)) * 0.2f;
                                    x = 0;
                                }
                                if (counter[i].n現在の値 > 255 + 180 && counter[i].n現在の値 <= 2745)
                                {
                                    TJAPlayer3.Tx.ScoreRank.Opacity = 255;
                                    TJAPlayer3.Tx.ScoreRank.vc拡大縮小倍率.X = 1.0f;
                                    TJAPlayer3.Tx.ScoreRank.vc拡大縮小倍率.Y = 1.0f;
                                    x = 0;
                                }
                                if (counter[i].n現在の値 >= 2745 && counter[i].n現在の値 <= 3000)
                                {
                                    TJAPlayer3.Tx.ScoreRank.Opacity = 255 - ((counter[i].n現在の値 - 2745));
                                    x = -((counter[i].n現在の値 - 2745) / 5.0f);
                                }

                                TJAPlayer3.Tx.ScoreRank.t2D拡大率考慮中央基準描画(87, 98 + (int)x, new System.Drawing.Rectangle(0, i == 0 ? i * 114 : i * 120, 140, i == 0 ? 114 : 120));
                                */

								#endregion
							}

							x = 0;
						}
						#endregion
					} else if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
						#region [Tower score ranks]

						double progress = CFloorManagement.LastRegisteredFloor / ((double)TJAPlayer3.stageSongSelect.rChoosenSong.arスコア[5].譜面情報.nTotalFloor);

						bool[] conditions =
						{
							progress >= 0.1,
							progress >= 0.25,
							progress >= 0.5,
							progress >= 0.75,
							progress == 1 && CFloorManagement.CurrentNumberOfLives > 0,
							TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nMiss == 0 && TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nMine == 0,
							TJAPlayer3.stage演奏ドラム画面.CChartScore[0].nGood == 0
						};

						counter[0][i].Tick();

						bool satisfied = true;
						for (int j = 0; j <= i; j++)
							if (conditions[j] == false) {
								satisfied = false;
								break;
							}


						if (satisfied == true) {
							displayScoreRank(i, 0, x, y, 1);
						}

						#endregion
					}
				}


			}

			//TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, ScoreRank[6].ToString());
			//TJAPlayer3.act文字コンソール.tPrint(0, 10, C文字コンソール.Eフォント種別.白, ScoreRank2P[6].ToString());

			return base.Draw();
		}

		private CTexture TowerResult_ScoreRankEffect;

		public int[][] ScoreRank = new int[5][];
		private CCounter[][] counter = new CCounter[5][] {
			new CCounter[7],
			new CCounter[7],
			new CCounter[7],
			new CCounter[7],
			new CCounter[7]
		};
	}
}
