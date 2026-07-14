using System.Runtime.InteropServices;
using FDK;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko;

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
			this.stState[i].ctProgress = new CCounter();
			this.stBranch[i].ctBranchAnimeProgress = new CCounter();
			this.stBranch[i].nFlashControlTimer = -1;
			this.stBranch[i].nBranchLayerOpacity = 0;
			this.stBranch[i].nBranchTextOpacity = 0;
			this.stBranch[i].nYCoord = 0;
			this.stBranch[i].ctFadeIn = null;
			this.stBranch[i].dxFadeIn = 0;

			this.ResetPlayStates();
		}
		if (OpenTaiko.Tx.Lane_Base[0] != null)
			OpenTaiko.Tx.Lane_Base[0].Opacity = 255;
		this.ctGoGo = new CCounter();


		this.ctGoGoFlame = new CCounter(0, 6, 50, OpenTaiko.Timer);
		base.Activate();
	}

	public override void DeActivate() {
		for (int i = 0; i < 5; i++) {
			this.stState[i].ctProgress = null;
			this.stBranch[i].ctBranchAnimeProgress = null;
		}
		this.ctGoGo = null;

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
				this.stBranch[i].nFlashControlTimer = SoundManager.PlayTimer.NowTimeMs;
			base.IsFirstDraw = false;
		}

		//それぞれが独立したレイヤーでないといけないのでforループはパーツごとに分離すること。

		if (OpenTaiko.ConfigIni.nPlayerCount <= 2 && !OpenTaiko.ConfigIni.bAIBattleMode) OpenTaiko.stageGameScreen.actMtaiko.DrawBackSymbol();

		#region[ レーン本体 ]


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

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			if (i == 1 && OpenTaiko.ConfigIni.bAIBattleMode && OpenTaiko.Tx.Lane_Background_AI != null)
				OpenTaiko.Tx.Lane_Background_AI?.t2DDraw(x[i], y[i]);
			else
				OpenTaiko.Tx.Lane_Background_Main?.t2DDraw(x[i], y[i]);
		}

		#endregion

		if (OpenTaiko.ConfigIni.nPlayerCount > 2 && !OpenTaiko.ConfigIni.bAIBattleMode) OpenTaiko.stageGameScreen.actMtaiko.DrawBackSymbol();

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			#region[ 分岐アニメ制御タイマー ]
			long num = FDK.SoundManager.PlayTimer.NowTimeMs;
			if (num < this.stBranch[i].nFlashControlTimer) {
				this.stBranch[i].nFlashControlTimer = num;
			}
			while ((num - this.stBranch[i].nFlashControlTimer) >= 30) {
				if (this.stBranch[i].nBranchLayerOpacity <= 255) {
					this.stBranch[i].nBranchLayerOpacity += 10;
				}

				if (this.stBranch[i].nBranchTextOpacity >= 0) {
					this.stBranch[i].nBranchTextOpacity -= 10;
				}

				if (this.stBranch[i].nYCoord != 0 && this.stBranch[i].nYCoord <= 20) {
					this.stBranch[i].nYCoord++;
				}

				this.stBranch[i].nFlashControlTimer += 8;
			}

			if (!this.stBranch[i].ctBranchAnimeProgress.IsStopped) {
				this.stBranch[i].ctBranchAnimeProgress.Tick();
				if (this.stBranch[i].ctBranchAnimeProgress.IsEnded) {
					this.stBranch[i].ctBranchAnimeProgress.Stop();
					this.stBranch[i].nBefore = this.stBranch[i].nAfter;
				}
			}

			var ctFadeIn = this.stBranch[i].ctFadeIn;
			if (ctFadeIn?.IsUnEnded ?? false) {
				ctFadeIn.Tick();
				this.stBranch[i].dxFadeIn = (int)Easing.EaseOut(ctFadeIn, (float)(OpenTaiko.Skin.ScaleY * 100), 0, Easing.CalcType.Back);
				if (ctFadeIn.IsEnded) {
					this.stBranch[i].dxFadeIn = 0;
				}
			}
			#endregion
		}
		#region[ 分岐レイヤー ]
		this.DrawBranchBG(x, y);
		#endregion

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			#region[ ゴーゴータイムレーン背景レイヤー ]
			if (OpenTaiko.Tx.Lane_Background_GoGo != null && OpenTaiko.stageGameScreen.bIsGOGOTIME[i]) {
				if (!this.ctGoGo.IsStopped) {
					this.ctGoGo.Tick();
				}

				if (this.ctGoGo.CurrentValue <= 4) {
					OpenTaiko.Tx.Lane_Background_GoGo.vcScaleRatio.Y = 0.2f;
					OpenTaiko.Tx.Lane_Background_GoGo.t2DDraw(x[i], y[i] + 54);
				} else if (this.ctGoGo.CurrentValue <= 5) {
					OpenTaiko.Tx.Lane_Background_GoGo.vcScaleRatio.Y = 0.4f;
					OpenTaiko.Tx.Lane_Background_GoGo.t2DDraw(x[i], y[i] + 40);
				} else if (this.ctGoGo.CurrentValue <= 6) {
					OpenTaiko.Tx.Lane_Background_GoGo.vcScaleRatio.Y = 0.6f;
					OpenTaiko.Tx.Lane_Background_GoGo.t2DDraw(x[i], y[i] + 26);
				} else if (this.ctGoGo.CurrentValue <= 8) {
					OpenTaiko.Tx.Lane_Background_GoGo.vcScaleRatio.Y = 0.8f;
					OpenTaiko.Tx.Lane_Background_GoGo.t2DDraw(x[i], y[i] + 13);
				} else if (this.ctGoGo.CurrentValue >= 9) {
					OpenTaiko.Tx.Lane_Background_GoGo.vcScaleRatio.Y = 1.0f;
					OpenTaiko.Tx.Lane_Background_GoGo.t2DDraw(x[i], y[i]);
				}
			}
			#endregion
		}

		this.DrawBranchText(x, y);

		if (OpenTaiko.ConfigIni.nPlayerCount <= 2) {
			if (OpenTaiko.Tx.Lane_Background_Sub != null) {
				OpenTaiko.Tx.Lane_Background_Sub.t2DDraw(OpenTaiko.Skin.Game_Lane_Sub_X[0], OpenTaiko.Skin.Game_Lane_Sub_Y[0]);
				if (OpenTaiko.stageGameScreen.isMultiPlay) {
					OpenTaiko.Tx.Lane_Background_Sub.t2DDraw(OpenTaiko.Skin.Game_Lane_Sub_X[1], OpenTaiko.Skin.Game_Lane_Sub_Y[1]);
				}
			}
		}


		OpenTaiko.stageGameScreen.actTaikoLaneFlash.Draw();



		if (OpenTaiko.Tx.Taiko_Frame[0] != null) {
			// Tower frame (without tamashii jauge) if playing a tower chart
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				int frame_x;
				int frame_y;
				if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
					frame_x = OpenTaiko.Skin.Game_Taiko_Frame_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * i);
					frame_y = OpenTaiko.Skin.Game_Taiko_Frame_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * i);
				} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
					frame_x = OpenTaiko.Skin.Game_Taiko_Frame_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * i);
					frame_y = OpenTaiko.Skin.Game_Taiko_Frame_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * i);
				} else {
					frame_x = OpenTaiko.Skin.Game_Taiko_Frame_X[i];
					frame_y = OpenTaiko.Skin.Game_Taiko_Frame_Y[i];
				}

				CTexture tex = null;

				switch (i) {
					case 0: {
							if (OpenTaiko.ConfigIni.bTokkunMode) {
								tex = OpenTaiko.Tx.Taiko_Frame[3];
							} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
								tex = OpenTaiko.Tx.Taiko_Frame[5];
							} else if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
								tex = OpenTaiko.Tx.Taiko_Frame[2];
							} else if (OpenTaiko.ConfigIni.nPlayerCount > 2) {
								tex = OpenTaiko.Tx.Taiko_Frame[6];
							} else {
								tex = OpenTaiko.Tx.Taiko_Frame[0];
							}
						}
						break;
					case 1: {
							if (OpenTaiko.ConfigIni.bAIBattleMode) {
								tex = OpenTaiko.Tx.Taiko_Frame[4];
							} else if (OpenTaiko.ConfigIni.nPlayerCount > 2) {
								tex = OpenTaiko.Tx.Taiko_Frame[6];
							} else {
								tex = OpenTaiko.Tx.Taiko_Frame[1];
							}
						}
						break;
					case 2:
						tex = OpenTaiko.Tx.Taiko_Frame[6];
						break;
					case 3:
						tex = OpenTaiko.Tx.Taiko_Frame[6];
						break;
					case 4:
						tex = OpenTaiko.Tx.Taiko_Frame[6];
						break;
				}

				tex?.t2DDraw(frame_x, frame_y);
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

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			if (this.nTotalMoveTime[i] == -1) {
				continue;
			}
			var nTime = (int)(long)OpenTaiko.GetTJA(i)!.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs);
			if (nTime < this.nMoveStartTime[i]) { // in case of rewinding
				OpenTaiko.stageGameScreen.JPOSCROLLX[i] = this.nMoveStartX[i];
				OpenTaiko.stageGameScreen.JPOSCROLLY[i] = this.nMoveStartY[i];
			} else if (nTime < this.nMoveStartTime[i] + this.nTotalMoveTime[i]) {
				OpenTaiko.stageGameScreen.JPOSCROLLX[i] = this.nMoveStartX[i] + (((nTime - this.nMoveStartTime[i]) / (double)this.nTotalMoveTime[i]) * this.nMoveDistancepx[i]);
				OpenTaiko.stageGameScreen.JPOSCROLLY[i] = this.nMoveStartY[i] + (((nTime - this.nMoveStartTime[i]) / (double)this.nTotalMoveTime[i]) * this.nVerticalJSPos[i]);
			} else {
				this.nTotalMoveTime[i] = -1;
				OpenTaiko.stageGameScreen.JPOSCROLLX[i] = this.nMoveDestPlaceX[i];
				OpenTaiko.stageGameScreen.JPOSCROLLY[i] = this.nMoveDestPlaceY[i];
			}
		}




		if (OpenTaiko.ConfigIni.bEnableAVI && OpenTaiko.TJA.listVD.Count > 0 && OpenTaiko.stageGameScreen.ShowVideo && !OpenTaiko.ConfigIni.bTokkunMode) {
			if (OpenTaiko.Tx.Lane_Background_Main != null) OpenTaiko.Tx.Lane_Background_Main.Opacity = OpenTaiko.ConfigIni.nBGAlpha;
			if (OpenTaiko.Tx.Lane_Background_AI != null) OpenTaiko.Tx.Lane_Background_AI.Opacity = OpenTaiko.ConfigIni.nBGAlpha;
			if (OpenTaiko.Tx.Lane_Background_Sub != null) OpenTaiko.Tx.Lane_Background_Sub.Opacity = OpenTaiko.ConfigIni.nBGAlpha;
			if (OpenTaiko.Tx.Lane_Background_GoGo != null) OpenTaiko.Tx.Lane_Background_GoGo.Opacity = OpenTaiko.ConfigIni.nBGAlpha;
		} else {
			if (OpenTaiko.Tx.Lane_Background_Main != null) OpenTaiko.Tx.Lane_Background_Main.Opacity = 255;
			if (OpenTaiko.Tx.Lane_Background_AI != null) OpenTaiko.Tx.Lane_Background_AI.Opacity = 255;
			if (OpenTaiko.Tx.Lane_Background_Sub != null) OpenTaiko.Tx.Lane_Background_Sub.Opacity = 255;
			if (OpenTaiko.Tx.Lane_Background_GoGo != null) OpenTaiko.Tx.Lane_Background_GoGo.Opacity = 255;
		}

		return base.Draw();
	}

	private void DrawBranchText(int[] x, int[] y) {
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			if (!OpenTaiko.stageGameScreen.bUseBranch[i] || OpenTaiko.Tx.Lane_Text.Any(x => (x == null))) {
				continue;
			}

			if (OpenTaiko.ConfigIni.nBranchAnime == 1 && this.stBranch[i].nYCoord == 21) {
				this.stBranch[i].nYCoord = 0;
			}
			if (OpenTaiko.ConfigIni.SimpleMode || !this.stBranch[i].ctBranchAnimeProgress.IsTicked || (OpenTaiko.ConfigIni.nBranchAnime == 1 && this.stBranch[i].nYCoord == 0)) {
				OpenTaiko.Tx.Lane_Text[(int)OpenTaiko.stageGameScreen.nTargetBranch[i]].Opacity = 255;
				OpenTaiko.Tx.Lane_Text[(int)OpenTaiko.stageGameScreen.nTargetBranch[i]].t2DDraw(x[i] + this.stBranch[i].dxFadeIn, y[i]);
				continue;
			}

			int nBefore = (int)this.stBranch[i].nBefore;
			int nAfter = (int)this.stBranch[i].nAfter;

			if (OpenTaiko.ConfigIni.nBranchAnime == 0) {
				int progress = this.stBranch[i].ctBranchAnimeProgress.CurrentValue;
				if (Math.Abs(nAfter - nBefore) >= 2) {
					// 2-level change
					if (progress < 150) {
						nAfter = 1;
					} else {
						progress -= 150;
						nBefore = 1;
					}
				}
				var opacity = (Math.Min(100, progress) * 0xff) / 100;
				if (progress < 60) {
					OpenTaiko.Tx.Lane_Text[nBefore].Opacity = 255 - opacity;
					OpenTaiko.Tx.Lane_Text[nAfter].Opacity = opacity;
					double ratio = progress / 60.0;
					float max = (float)(OpenTaiko.Skin.ScaleY * 30);
					if (nAfter > nBefore) {
						// AC7~14 level up: fly down
						OpenTaiko.Tx.Lane_Text[nBefore].t2DDraw(x[i] + this.stBranch[i].dxFadeIn, y[i] + Easing.EaseIn(ratio, 0, max, Easing.CalcType.Back));
						OpenTaiko.Tx.Lane_Text[nAfter].t2DDraw(x[i] + this.stBranch[i].dxFadeIn, y[i] + Easing.EaseIn(ratio, -max, 0, Easing.CalcType.Back));
					} else {
						// AC7~14 level down: fly up
						OpenTaiko.Tx.Lane_Text[nBefore].t2DDraw(x[i] + this.stBranch[i].dxFadeIn, y[i] - Easing.EaseIn(ratio, 0, max, Easing.CalcType.Back));
						OpenTaiko.Tx.Lane_Text[nAfter].t2DDraw(x[i] + this.stBranch[i].dxFadeIn, y[i] - Easing.EaseIn(ratio, -max, 0, Easing.CalcType.Back));
					}
				} else {
					OpenTaiko.Tx.Lane_Text[nAfter].Opacity = opacity;
					OpenTaiko.Tx.Lane_Text[nAfter].t2DDraw(x[i] + this.stBranch[i].dxFadeIn, y[i]);
				}
			} else {
				var opacity = Math.Min(255, this.stBranch[i].nBranchLayerOpacity);
				OpenTaiko.Tx.Lane_Text[nBefore].Opacity = 255 - opacity;
				OpenTaiko.Tx.Lane_Text[nAfter].Opacity = opacity;
				double ratio = this.stBranch[i].nYCoord / 20.0;
				float max = (float)(OpenTaiko.Skin.ScaleY * 20);
				if (nAfter > nBefore) {
					// AC15~ level up: fly up
					OpenTaiko.Tx.Lane_Text[nBefore].t2DDraw(x[i] + this.stBranch[i].dxFadeIn, y[i] - Easing.EaseIn(ratio, 0, max, Easing.CalcType.Back));
					OpenTaiko.Tx.Lane_Text[nAfter].t2DDraw(x[i] + this.stBranch[i].dxFadeIn, y[i] - Easing.EaseIn(ratio, -max, 0, Easing.CalcType.Back));
				} else {
					// AC15~ level down: fly down
					OpenTaiko.Tx.Lane_Text[nBefore].t2DDraw(x[i] + this.stBranch[i].dxFadeIn, y[i] + Easing.EaseIn(ratio, 0, max, Easing.CalcType.Back));
					OpenTaiko.Tx.Lane_Text[nAfter].t2DDraw(x[i] + this.stBranch[i].dxFadeIn, y[i] + Easing.EaseIn(ratio, -max, 0, Easing.CalcType.Back));
				}
			}
		}
	}

	public void DrawBranchBG(int[] x, int[] y) {
		//アニメーション中の分岐レイヤー(背景)の描画を行う。
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			if (!OpenTaiko.stageGameScreen.bUseBranch[i] || OpenTaiko.Tx.Lane_Base.Any(x => (x == null))) {
				continue;
			}

			#region[ 動いていない ]
			if (OpenTaiko.ConfigIni.SimpleMode || !this.stBranch[i].ctBranchAnimeProgress.IsTicked) {
				int nBranch = (int)OpenTaiko.stageGameScreen.nTargetBranch[i];
				OpenTaiko.Tx.Lane_Base[nBranch].Opacity = 255;
				OpenTaiko.Tx.Lane_Base[nBranch].t2DDraw(x[i], y[i]);
				continue;
			}
			#endregion

			int nBranchLower = Math.Min((int)this.stBranch[i].nBefore, (int)this.stBranch[i].nAfter);
			int nBranchHigher = Math.Max((int)this.stBranch[i].nBefore, (int)this.stBranch[i].nAfter);
			bool isLevelDown = (this.stBranch[i].nAfter == (CTja.ECourse)nBranchLower);

			if (OpenTaiko.ConfigIni.nBranchAnime == 1) {
				#region[ AC15～風の背後レイヤー ]
				OpenTaiko.Tx.Lane_Base[nBranchLower].Opacity = 255;
				OpenTaiko.Tx.Lane_Base[nBranchLower].t2DDraw(x[i], y[i]);
				OpenTaiko.Tx.Lane_Base[nBranchHigher].Opacity = this.stBranch[i].nBranchLayerOpacity;
				if (isLevelDown) // level down
					OpenTaiko.Tx.Lane_Base[nBranchHigher].Opacity = 255 - OpenTaiko.Tx.Lane_Base[nBranchHigher].Opacity;
				OpenTaiko.Tx.Lane_Base[nBranchHigher].t2DDraw(x[i], y[i]);
				#endregion
			} else if (OpenTaiko.ConfigIni.nBranchAnime == 0) {
				#region[ AC7～14風の背後レイヤー ]
				var progress = this.stBranch[i].ctBranchAnimeProgress.CurrentValue;
				if (Math.Abs(nBranchHigher - nBranchLower) < 2) {
					// 1-level change
					if (isLevelDown)
						progress = Math.Max(0, 100 - progress);
				} else {
					// 2-level change
					if (isLevelDown)
						progress = Math.Max(0, 250 - progress);
					if (progress >= 150) {
						progress -= 150;
						nBranchLower = 1;
					}
				}
				if (progress < 100) {
					OpenTaiko.Tx.Lane_Base[nBranchLower].Opacity = 255;
					OpenTaiko.Tx.Lane_Base[nBranchLower].t2DDraw(x[i], y[i]);
					OpenTaiko.Tx.Lane_Base[nBranchLower + 1].Opacity = (Math.Min(100, progress) * 0xff) / 100;
					OpenTaiko.Tx.Lane_Base[nBranchLower + 1].t2DDraw(x[i], y[i]);
				} else {
					OpenTaiko.Tx.Lane_Base[nBranchLower + 1].Opacity = 255;
					OpenTaiko.Tx.Lane_Base[nBranchLower + 1].t2DDraw(x[i], y[i]);
				}
				#endregion
			}
		}
	}

	public void ResetPlayStates() {
		for (int i = 0; i < 5; ++i) {
			this.nTotalMoveTime[i] = -1;
		}
	}

	public void GoGoFlame() {
		//判定枠
		if (OpenTaiko.Tx.Judge_Frame != null) {
			OpenTaiko.Tx.Judge_Frame.bAddBlend = OpenTaiko.Skin.Game_JudgeFrame_AddBlend;
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				OpenTaiko.Tx.Judge_Frame.t2DDraw(
					OpenTaiko.stageGameScreen.GetNoteOriginX(i),
					OpenTaiko.stageGameScreen.GetNoteOriginY(i), new Rectangle(0, 0, OpenTaiko.Skin.Game_Notes_Size[0], OpenTaiko.Skin.Game_Notes_Size[1]));
			}
		}


		#region[ ゴーゴー炎 ]
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			if (OpenTaiko.stageGameScreen.bIsGOGOTIME[i] && !OpenTaiko.ConfigIni.SimpleMode) {
				this.ctGoGoFlame.TickLoop();

				if (OpenTaiko.Tx.Effects_Fire != null) {
					float fScale = 1.0f;

					float[] arScale = new float[] { 0.8f, 1.2f, 1.7f, 2.5f, 2.3f, 2.2f, 2.0f, 1.8f, 1.7f, 1.6f, 1.6f, 1.5f, 1.5f, 1.4f, 1.3f, 1.2f, 1.1f, 1.0f };

					fScale = arScale[this.ctGoGo.CurrentValue];

					/*
                    Matrix mat = Matrix.Identity;
                    mat *= Matrix.Scaling(f倍率, f倍率, 1.0f);
                    mat *= Matrix.Translation(TJAPlayer3.Skin.nScrollFieldX[i] - SampleFramework.GameWindowSize.Width / 2.0f, -(TJAPlayer3.Skin.nJudgePointY[i] - SampleFramework.GameWindowSize.Height / 2.0f), 0f);
                    */
					//this.txゴーゴー炎.b加算合成 = true;

					//this.ctゴーゴー.n現在の値 = 6;

					int width = OpenTaiko.Tx.Effects_Fire.szTextureSize.Width / 7;
					int height = OpenTaiko.Tx.Effects_Fire.szTextureSize.Height;

					float x = -(width * (fScale - 1.0f) / 2.0f);
					float y = -(height * (fScale - 1.0f) / 2.0f);

					if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
						x += OpenTaiko.Skin.Game_Effect_Fire_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * i);
						y += OpenTaiko.Skin.Game_Effect_Fire_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * i);
					} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
						x += OpenTaiko.Skin.Game_Effect_Fire_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * i);
						y += OpenTaiko.Skin.Game_Effect_Fire_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * i);
					} else {
						x += OpenTaiko.Skin.Game_Effect_Fire_X[i];
						y += OpenTaiko.Skin.Game_Effect_Fire_Y[i];
					}

					x += OpenTaiko.stageGameScreen.GetJPOSCROLLX(i);
					y += OpenTaiko.stageGameScreen.GetJPOSCROLLY(i);

					OpenTaiko.Tx.Effects_Fire.vcScaleRatio.X = fScale;
					OpenTaiko.Tx.Effects_Fire.vcScaleRatio.Y = fScale;

					OpenTaiko.Tx.Effects_Fire.t2DDraw(x, y,
						new Rectangle(width * (this.ctGoGoFlame.CurrentValue), 0, width, height));
				}
			}
		}
		#endregion
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			if (!this.stState[i].ctProgress.IsStopped) {
				this.stState[i].ctProgress.Tick();
				if (this.stState[i].ctProgress.IsEnded) {
					this.stState[i].ctProgress.Stop();
				}
				//if( this.txアタックエフェクトLower != null )
				{
					//this.txアタックエフェクトLower.b加算合成 = true;
					int n = this.stState[i].IsBig ? 520 : 0;

					float x = 0;
					float y = 0;

					if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
						x = OpenTaiko.Skin.Game_Effects_Hit_Explosion_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * i);
						y = OpenTaiko.Skin.Game_Effects_Hit_Explosion_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * i);
					} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
						x = OpenTaiko.Skin.Game_Effects_Hit_Explosion_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * i);
						y = OpenTaiko.Skin.Game_Effects_Hit_Explosion_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * i);
					} else {
						x = OpenTaiko.Skin.Game_Effects_Hit_Explosion_X[i];
						y = OpenTaiko.Skin.Game_Effects_Hit_Explosion_Y[i];
					}
					x += OpenTaiko.stageGameScreen.GetJPOSCROLLX(i);
					y += OpenTaiko.stageGameScreen.GetJPOSCROLLY(i);

					switch (stState[i].judge) {
						case ENoteJudge.Perfect:
						case ENoteJudge.Great:
						case ENoteJudge.Auto:
							if (!OpenTaiko.ConfigIni.SimpleMode) {
								//this.txアタックエフェクトLower.t2D描画( CDTXMania.app.Device, 285, 127, new Rectangle( this.st状態[ i ].ct進行.n現在の値 * 260, n, 260, 260 ) );
								if (this.stState[i].IsBig && OpenTaiko.Tx.Effects_Hit_Great_Big[this.stState[i].ctProgress.CurrentValue] != null)
									OpenTaiko.Tx.Effects_Hit_Great_Big[this.stState[i].ctProgress.CurrentValue].t2DDraw(x, y);
								else if (OpenTaiko.Tx.Effects_Hit_Great[this.stState[i].ctProgress.CurrentValue] != null)
									OpenTaiko.Tx.Effects_Hit_Great[this.stState[i].ctProgress.CurrentValue].t2DDraw(x, y);
							}
							break;

						case ENoteJudge.Good:
							//this.txアタックエフェクトLower.t2D描画( CDTXMania.app.Device, 285, 127, new Rectangle( this.st状態[ i ].ct進行.n現在の値 * 260, n + 260, 260, 260 ) );
							if (this.stState[i].IsBig && OpenTaiko.Tx.Effects_Hit_Good_Big[this.stState[i].ctProgress.CurrentValue] != null)
								OpenTaiko.Tx.Effects_Hit_Good_Big[this.stState[i].ctProgress.CurrentValue].t2DDraw(x, y);
							else if (OpenTaiko.Tx.Effects_Hit_Good[this.stState[i].ctProgress.CurrentValue] != null)
								OpenTaiko.Tx.Effects_Hit_Good[this.stState[i].ctProgress.CurrentValue].t2DDraw(x, y);
							break;

						case ENoteJudge.Miss:
						case ENoteJudge.Bad:
							break;
					}
				}
			}
		}


	}

	public virtual void Start(NotesManager.ENoteType Lane, EGameType gameType, ENoteJudge judge, bool bBothHandsInput, int nPlayer) {
		//2017.08.15 kairera0467 排他なので番地をそのまま各レーンの状態として扱う

		//for( int n = 0; n < 1; n++ )
		{
			this.stState[nPlayer].ctProgress = new CCounter(0, 14, 20, OpenTaiko.Timer);
			this.stState[nPlayer].judge = judge;
			this.stState[nPlayer].nPlayer = nPlayer;
			this.stState[nPlayer].IsBig = NotesManager.IsBigNoteTaiko(Lane, gameType) && bBothHandsInput;
		}
	}


	public void GOGOSTART() {
		this.ctGoGo = new CCounter(0, 17, 18, OpenTaiko.Timer);
		if (OpenTaiko.ConfigIni.nPlayerCount == 1 && OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) OpenTaiko.stageGameScreen.GoGoSplash.StartSplash();
	}


	public void ChangeBranch(CTja.ECourse nAfter, int nPlayer, bool stopAnime = false) {
		if (stopAnime) {
			this.stBranch[nPlayer].ctBranchAnimeProgress.Stop();
			this.stBranch[nPlayer].nBefore = this.stBranch[nPlayer].nAfter = nAfter;
			return;
		}
		if (this.stBranch[nPlayer].nAfter == nAfter) {
			return;
		}
		this.stBranch[nPlayer].ctBranchAnimeProgress = new CCounter(0, 300, 2, OpenTaiko.Timer);

		this.stBranch[nPlayer].nBranchLayerOpacity = 6;
		this.stBranch[nPlayer].nYCoord = 1;

		this.stBranch[nPlayer].nBefore = this.stBranch[nPlayer].nAfter;
		this.stBranch[nPlayer].nAfter = nAfter;
	}

	public void BranchText_FadeIn(int? msDelay, int nPlayer) {
		this.stBranch[nPlayer].dxFadeIn = 300;
		if (msDelay != null) {
			this.stBranch[nPlayer].ctFadeIn = new CCounter(-msDelay.Value, 120, 1, OpenTaiko.Timer);
		}
	}

	public void tJudgeFrameMove(int nPlayer, CTja.CJPOSSCROLL jposscroll, int msTimeNote) {
		this.nMoveStartTime[nPlayer] = msTimeNote;
		this.nMoveStartX[nPlayer] = jposscroll.pxOrigX;
		this.nMoveStartY[nPlayer] = jposscroll.pxOrigY;
		this.nTotalMoveTime[nPlayer] = (int)jposscroll.msMoveDt;
		double pxMoveDx = this.nMoveDistancepx[nPlayer] = jposscroll.pxMoveDx;
		double pxMoveDy = this.nVerticalJSPos[nPlayer] = jposscroll.pxMoveDy;
		this.nMoveDestPlaceX[nPlayer] = jposscroll.pxOrigX + pxMoveDx;
		this.nMoveDestPlaceY[nPlayer] = jposscroll.pxOrigY + pxMoveDy;
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

	protected STSTATUS[] stState = new STSTATUS[5];

	//private CTexture[] txゴーゴースプラッシュ;

	[StructLayout(LayoutKind.Sequential)]
	protected struct STSTATUS {
		public bool bUse;
		public CCounter ctProgress;
		public ENoteJudge judge;
		public bool IsBig;
		public int nOpacity;
		public int nPlayer;
	}
	private CCounter ctGoGo;
	private CCounter ctGoGoFlame;



	public STBRANCH[] stBranch = new STBRANCH[5];
	[StructLayout(LayoutKind.Sequential)]
	public struct STBRANCH {
		public CCounter ctBranchAnimeProgress;
		public CTja.ECourse nBefore;
		public CTja.ECourse nAfter;

		public long nFlashControlTimer;
		public int nBranchLayerOpacity;
		public int nBranchTextOpacity;
		public int nYCoord;
		public int dxFadeIn;
		public CCounter? ctFadeIn;
	}


	private int[] nTotalMoveTime = new int[5];
	private double[] nMoveStartX = new double[5];
	private double[] nMoveStartY = new double[5];
	private int[] nMoveStartTime = new int[5];
	private double[] nMoveDistancepx = new double[5];
	private double[] nVerticalJSPos = new double[5];
	private double[] nMoveDestPlaceX = new double[5];
	private double[] nMoveDestPlaceY = new double[5];

	//-----------------
	#endregion
}
