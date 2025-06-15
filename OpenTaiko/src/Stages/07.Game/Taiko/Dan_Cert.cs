using System.Runtime.InteropServices;
using FDK;
using static OpenTaiko.CActSelect曲リスト;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using RectangleF = System.Drawing.RectangleF;

namespace OpenTaiko;

static internal class CExamInfo {
	// Includes the gauge exam, DanCert max number of exams is 6
	public static readonly int cMaxExam = 7;

	// Max number of songs for a Dan chart
	public static readonly int cExamMaxSongs = 9;
}


internal class Dan_Cert : CActivity {
	/// <summary>
	/// 段位認定
	/// </summary>
	public Dan_Cert() {
		base.IsDeActivated = true;
	}

	//
	Dan_C[] Challenge = new Dan_C[CExamInfo.cMaxExam];
	//

	public void Start(int number) {
		NowShowingNumber = number;
		if (number == 0) {
			Counter_Wait = new CCounter(0, 2299, 1, OpenTaiko.Timer);
		} else {
			Counter_In = new CCounter(0, 999, 1, OpenTaiko.Timer);
		}
		bExamChangeCheck = false;

		if (number == 0) {
			for (int i = 0; i < CExamInfo.cMaxExam; i++)
				ExamChange[i] = false;

			for (int j = 0; j < CExamInfo.cMaxExam; j++) {
				if (OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[0].Dan_C[j] != null) {
					Challenge[j] = OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[NowShowingNumber].Dan_C[j];
					if (OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count - 1].Dan_C[j] != null
						&& OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count > 1) // Individual exams, not counted if dan is only a single song
					{
						OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[NowShowingNumber].Dan_C[j].Amount = 0;
						ExamChange[j] = true;
					}
				}
			}
		}

		ScreenPoint = [ScreenPointAnchor(0), ScreenPointAnchor(2)];

		OpenTaiko.stageGameScreen.ReSetScore(OpenTaiko.TJA.List_DanSongs[NowShowingNumber].ScoreInit, OpenTaiko.TJA.List_DanSongs[NowShowingNumber].ScoreDiff, 0);

		OpenTaiko.stageGameScreen.ftDanReSetScoreGen4ShinUchi(NowShowingNumber);
		OpenTaiko.stageGameScreen.ftDanReSetBranches(OpenTaiko.TJA.bHasBranchDan[NowShowingNumber]);

		IsAnimating = true;

		//段位道場
		//TJAPlayer3.stage演奏ドラム画面.actPanel.SetPanelString(TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].Title, TJAPlayer3.DTX.List_DanSongs[NowShowingNumber].Genre, 1 + NowShowingNumber + "曲目");
		OpenTaiko.stageGameScreen.actPanel.SetPanelString(OpenTaiko.TJA.List_DanSongs[NowShowingNumber].Title,
			CLangManager.LangInstance.GetString("TITLE_MODE_DAN"),
			1 + NowShowingNumber + "曲目");

		if (number == 0) {
			Sound_Section_First?.PlayStart();
			this.Update(); // resolve Unknown exam reach status
		} else {
			Sound_Section?.PlayStart();
		}
	}

	public override void Activate() {
		for (int i = 0; i < CExamInfo.cMaxExam; i++) {
			if (OpenTaiko.TJA.Dan_C[i] != null) Challenge[i] = new Dan_C(OpenTaiko.TJA.Dan_C[i]);

			for (int j = 0; j < OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count; j++) {
				if (OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[j].Dan_C[i] != null) {
					OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[j].Dan_C[i] = new Dan_C(OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[j].Dan_C[i]);
				}
			}
		}

		if (OpenTaiko.stageGameScreen.ListDan_Number >= 1 && FirstSectionAnime)
			OpenTaiko.stageGameScreen.ListDan_Number = 0;

		FirstSectionAnime = false;
		// 始点を決定する。
		// ExamCount = 0;

		songsnotesremain = new int[OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count];
		this.ct虹アニメ = new CCounter(0, OpenTaiko.Skin.Game_Gauge_Dan_Rainbow_Ptn - 1, 30, OpenTaiko.Timer);
		this.ct虹透明度 = new CCounter(0, OpenTaiko.Skin.Game_Gauge_Rainbow_Timer - 1, 1, OpenTaiko.Timer);

		this.pfExamFont = HPrivateFastFont.tInstantiateMainFont(OpenTaiko.Skin.Game_DanC_ExamFont_Size);

		this.ttkExams = new TitleTextureKey[(int)Exam.Type.Total];
		for (int i = 0; i < this.ttkExams.Length; i++) {
			this.ttkExams[i] = new TitleTextureKey(CLangManager.LangInstance.GetExamName(i), this.pfExamFont, Color.White, Color.SaddleBrown, 1000);
		}

		NowCymbolShowingNumber = 0;
		bExamChangeCheck = false;

		for (int i = 0; i < CExamInfo.cMaxExam; i++) {
			Status[i] = new ChallengeStatus();
			Status[i].Timer_Amount = new CCounter();
			Status[i].Timer_Gauge = new CCounter();
			Status[i].Timer_Gauge!.EndValue = 400;
			Status[i].ctGaugeFlashLoop = new CCounter();
			Status[i].ctGaugeFlashLoop.EndValue = 1000;
		}

		IsEnded = new bool[OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count];

		if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) IsAnimating = true;

		Dan_Plate = OpenTaiko.tテクスチャの生成(Path.GetDirectoryName(OpenTaiko.TJA.strFullPath) + @$"{Path.DirectorySeparatorChar}Dan_Plate.png");

		base.Activate();
	}

	private class DanExamScore {
		public CStage演奏画面共通.CBRANCHSCORE? judges;
		public int nCombo, nHighestCombo, nNotesMax, nNotesRemainMax;
		public double msBarRollMax;
		public int nBarRollMax, nBalloonHitMax, nAdLibMax, nMineMax;
		public CChip? lastChip;
		public bool hasBranch;

		public int GetUpdatedNNotesPast() => (judges!.nGreat + judges.nGood + judges.nMiss);
		public double GetUpdatedAccuracy() => (judges!.nGreat! * 100 + judges.nGood * 50) / (double)GetUpdatedNNotesPast();
		public int GetUpdatedNNotesRemainMax() => nNotesMax - GetUpdatedNNotesPast();
	}

	public void Update() {
		DanExamScore? individual = null;
		DanExamScore? total = null;

		DanExamScore getIndividual()
			=> individual ??= this.GetIndividualExamScore(NowShowingNumber);
		DanExamScore getTotal()
			=> total ??= this.GetTotalExamScore();

		for (int i = 0; i < CExamInfo.cMaxExam; i++) {
			if (Challenge[i] == null || !Challenge[i].ExamIsEnable) continue;
			if (ExamChange[i] && Challenge[i] != OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[NowShowingNumber].Dan_C[i]) continue;

			var oldReachedStatus = Challenge[i].ReachStatus;
			var isChangedAmount = false;

			DanExamScore score = ExamChange[i] ? getIndividual() : getTotal();
			double examAmount = Challenge[i].ExamType switch {
				Exam.Type.Gauge => OpenTaiko.stageGameScreen.actGauge.db現在のゲージ値[0],
				Exam.Type.JudgePerfect => score.judges!.nGreat,
				Exam.Type.JudgeGood => score.judges!.nGood,
				Exam.Type.JudgeBad => score.judges!.nMiss,
				Exam.Type.JudgeADLIB => score.judges!.nADLIB,
				Exam.Type.JudgeMine => score.judges!.nMine,
				Exam.Type.Score => OpenTaiko.stageGameScreen.actScore.GetScore(0),
				Exam.Type.Roll => score.judges!.nRoll,
				Exam.Type.Hit => score.judges!.nGreat + score.judges.nGood + score.judges.nRoll,
				Exam.Type.Combo => score.nHighestCombo,
				Exam.Type.Accuracy => score.GetUpdatedAccuracy(),
				_ => 0,
			};
			isChangedAmount = Challenge[i].Update((int)examAmount);

			// 値が変更されていたらアニメーションを行う。
			if (isChangedAmount) {
				if (Status[i].Timer_Amount != null && Status[i].Timer_Amount.IsUnEnded) {
					Status[i].Timer_Amount = new CCounter(0, 11, 12, OpenTaiko.Timer);
					Status[i].Timer_Amount.CurrentValue = 1;
				} else {
					Status[i].Timer_Amount = new CCounter(0, 11, 12, OpenTaiko.Timer);
				}
			}
			this.UpdateReachStatus(NowShowingNumber, i, score, ExamChange[i]);
			if (Challenge[i].ReachStatus != oldReachedStatus && oldReachedStatus != Exam.ReachStatus.Unknown) {
				if (Challenge[i].ReachStatus == Exam.ReachStatus.Failure) {
					Sound_Failed?.PlayStart();
				}
				this.Status[i].Timer_Gauge.Start(0, this.Status[i].Timer_Gauge.EndValue, 1, OpenTaiko.Timer);
				SetFlashSpeed(this.Status[i], this.Challenge[i].ReachStatus);
			}
		}
	}

	private DanExamScore GetIndividualExamScore(int iSong) {
		DanExamScore individual = new() {
			judges = OpenTaiko.stageGameScreen.DanSongScore[iSong],
			nCombo = OpenTaiko.stageGameScreen.DanSongScore[iSong].nCombo,
			nHighestCombo = OpenTaiko.stageGameScreen.DanSongScore[iSong].nHighestCombo,
			msBarRollMax = OpenTaiko.stageGameScreen.nRollTimeMs_Dan[iSong],

			nNotesMax = OpenTaiko.TJA!.nDan_NotesCount[iSong],
			nBarRollMax = OpenTaiko.TJA.nDan_BarRollCount[iSong],
			nBalloonHitMax = OpenTaiko.TJA.nDan_BalloonHitCount[iSong],
			nAdLibMax = OpenTaiko.TJA.nDan_AdLibCount[iSong],
			nMineMax = OpenTaiko.TJA.nDan_MineCount[iSong],

			lastChip = OpenTaiko.TJA.pDan_LastChip[iSong],
			hasBranch = OpenTaiko.TJA.bHasBranchDan[iSong],
		};
		individual.nNotesRemainMax = this.songsnotesremain[iSong] = individual.GetUpdatedNNotesRemainMax();
		return individual;
	}

	private DanExamScore GetTotalExamScore() {
		DanExamScore total = new() {
			judges = OpenTaiko.stageGameScreen.CChartScore[0],
			nCombo = OpenTaiko.stageGameScreen.actCombo.nCurrentCombo.P1,
			nHighestCombo = OpenTaiko.stageGameScreen.actCombo.nCurrentCombo.最高値[0],
			msBarRollMax = OpenTaiko.stageGameScreen.nRollTimeMs_Dan.Sum(),

			nNotesMax = OpenTaiko.TJA!.nノーツ数[3],
			nBarRollMax = OpenTaiko.TJA.nDan_BarRollCount.Sum(),
			nBalloonHitMax = OpenTaiko.TJA.nDan_BalloonHitCount.Sum(),
			nAdLibMax = OpenTaiko.TJA.nDan_AdLibCount.Sum(),
			nMineMax = OpenTaiko.TJA.nDan_MineCount.Sum(),

			lastChip = !(OpenTaiko.TJA.listChip.Count > 0) ? null : OpenTaiko.TJA.listChip[OpenTaiko.TJA.listChip.Count - 1],
			hasBranch = OpenTaiko.TJA.bチップがある.Branch,
		};
		total.nNotesRemainMax = this.notesremain = total.GetUpdatedNNotesRemainMax();
		return total;
	}

	private static void SetFlashSpeed(ChallengeStatus challengeStatus, Exam.ReachStatus reachStatus) {
		int flashSpeed = GetExamGaugeFlashSpeed(reachStatus);
		if (flashSpeed > 0) {
			// extra delay before "grey" blinking
			int msDelay = (reachStatus == Exam.ReachStatus.Danger) ? (int)challengeStatus.Timer_Gauge!.EndValue : 0;
			challengeStatus.ctGaugeFlashLoop.Start(-msDelay, challengeStatus.ctGaugeFlashLoop.EndValue, 1.0 / flashSpeed, OpenTaiko.Timer);
		} else {
			challengeStatus.ctGaugeFlashLoop.Stop();
			challengeStatus.ctGaugeFlashLoop.CurrentValue = 0;
		}
	}

	public static int GetIdxExamGaugeTexture(Exam.ReachStatus reachStatus, Exam.Range rangeType) => reachStatus switch {
		Exam.ReachStatus.Better_Success => 2, // unused
		>= Exam.ReachStatus.Success_Or_Better => 2,
		Exam.ReachStatus.Unknown when rangeType is Exam.Range.Less => 2,
		>= Exam.ReachStatus.High => 1,
		(Exam.ReachStatus.Low or Exam.ReachStatus.Unknown) when rangeType is Exam.Range.More => 0, // TODO: add dedicated texture for more-type Low
		_ => 0, // ... or for red
	};

	public static int GetExamGaugeFlashSpeed(Exam.ReachStatus reachStatus) => reachStatus switch {
		Exam.ReachStatus.Nearer_Success => 2,
		Exam.ReachStatus.Nearer_Better_Success => 2,
		Exam.ReachStatus.Danger => 1,
		Exam.ReachStatus.Near_Success => 1,
		Exam.ReachStatus.Near_Better_Success => 1,
		_ => 0, // no flashing
	};

	private void UpdateReachStatus(int iSong, int iExam, DanExamScore score, bool isIndividualExam) {
		// 条件の達成見込みがあるかどうか判断する。
		var dan_C = this.Challenge[iExam];

		// 残り音符数が0になったときに判断されるやつ
		// Challenges that are only judged when there are no remaining notes
		bool judgeOnlyAfterLastNote = dan_C.ExamType is Exam.Type.Gauge or Exam.Type.Accuracy;

		// 残り音符数ゼロ
		bool isAfterLastNote = (!score.hasBranch && score.nNotesRemainMax <= 0);
		// 音源が終了したやつの分岐。
		bool isAfterLastChip = (score.lastChip?.bHit ?? true)
			|| ((!score.lastChip.bVisible || !NotesManager.IsHittableNote(score.lastChip))
				&& score.lastChip.n発声時刻ms <= OpenTaiko.TJA.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs));

		// returns whether the final judge has been done
		static bool doFinalJudge(Dan_C dan_C) {
			var clearStatus = dan_C.GetExamStatus();
			dan_C.ReachStatus = Exam.ToReachStatus(clearStatus); // also reset danger status
			return (clearStatus != Exam.Status.Success); // return false for further checking reach status
		}

		if (isAfterLastChip && doFinalJudge(dan_C))
			return;

		if (dan_C.ReachStatus == Exam.ReachStatus.Failure)
			return;

		if (!judgeOnlyAfterLastNote) {
			if (dan_C.ExamRange != Exam.Range.Less) {
				if (dan_C.GetExamStatus() == Exam.Status.Better_Success) {
					dan_C.ReachStatus = Exam.ReachStatus.Better_Success;
					return;
				}
			} else {
				if (dan_C.GetExamStatus() == Exam.Status.Failure) {
					dan_C.ReachStatus = Exam.ReachStatus.Failure;
					return;
				}
			}
		}

		// Challenges that are monitored in live
		bool judgeFailureEveryTime = dan_C.ExamType is Exam.Type.JudgePerfect or Exam.Type.JudgeGood or Exam.Type.JudgeBad or Exam.Type.Combo
			or Exam.Type.JudgeADLIB or Exam.Type.JudgeMine or Exam.Type.Roll or Exam.Type.Hit or Exam.Type.Accuracy;
		// Other challenges: Check challenge fails at the end of each songs

		bool judgeFailure = (judgeFailureEveryTime && !score.hasBranch) || isAfterLastChip; // workaround: prevent judging too early for branched charts

		static void resetDangerStatusIfSuccess(Dan_C dan_C) {
			if (dan_C.GetExamStatus() >= Exam.Status.Success)
				dan_C.ReachStatus = Exam.ToReachStatus(dan_C.GetExamStatus()); // reset danger status
		}

		// returns whether the final judge has been done
		static bool judgeGenericMore(DanExamScore score, Dan_C dan_C, bool judgeFailure, double amountRemainMax) {
			if (!score.hasBranch && amountRemainMax <= 0 && doFinalJudge(dan_C))
				return true;
			if (dan_C.GetExamStatus() < Exam.Status.Success) {
				if (judgeFailure && amountRemainMax < dan_C.GetValue()[0] - dan_C.Amount) {
					dan_C.ReachStatus = Exam.ReachStatus.Failure;
					return true;
				}
				if (amountRemainMax - 0.02 * amountRemainMax < dan_C.GetValue()[0] - dan_C.Amount) {
					dan_C.ReachStatus = Exam.ReachStatus.Danger;
					return true;
				}
			}
			resetDangerStatusIfSuccess(dan_C);
			return false;
		}

		static bool judgeGenericLess(DanExamScore score, Dan_C dan_C, double amountRemainMax, double dangerWeight = 1) {
			if (!score.hasBranch && amountRemainMax <= 0 && doFinalJudge(dan_C))
				return true;
			if (dan_C.Amount + 4 * dangerWeight >= dan_C.GetValue()[0] && dan_C.Amount > 0) {
				dan_C.ReachStatus = Exam.ReachStatus.Danger;
				return true;
			}
			resetDangerStatusIfSuccess(dan_C);
			return false;
		}

		static bool judgeGeneric(DanExamScore score, Dan_C dan_C, bool judgeFailure, double amountRemainMax) {
			if (dan_C.ExamRange != Exam.Range.Less) {
				if (judgeGenericMore(score, dan_C, judgeFailure, amountRemainMax))
					return true;
			} else {
				if (judgeGenericLess(score, dan_C, amountRemainMax))
					return true;
			}
			return false;
		}

		static double getExpectedMaxBarRollHits(DanExamScore score) {
			double msRemainBarRoll = CTja.TjaDurationToGameDuration(score.msBarRollMax - (score.judges!.msBarRollPass + OpenTaiko.stageGameScreen.msCurrentBarRollProgress[0]));
			return 20.0 * msRemainBarRoll / 1000;
		}

		static bool judgeRoll(DanExamScore score, Dan_C dan_C, bool judgeFailure, double judgedNotesRemainMax, int noteHitWeight) {
			if (dan_C.ExamRange != Exam.Range.Less) {
				if (!score.hasBranch && judgedNotesRemainMax <= 0 && doFinalJudge(dan_C))
					return true;
				if (dan_C.GetExamStatus() < Exam.Status.Success) {
					int balloonHitRemainMax = score.nBalloonHitMax - score.judges!.nBalloonHitPass;
					double maxHitsNoBarRoll = dan_C.Amount + (noteHitWeight * score.nNotesRemainMax) + balloonHitRemainMax;
					if (judgeFailure && (maxHitsNoBarRoll < dan_C.GetValue()[0])
						&& (score.nBarRollMax <= score.judges!.nBarRollPass)
						) {
						dan_C.ReachStatus = Exam.ReachStatus.Failure;
						return true;
					}
					double expectedMaxHits = maxHitsNoBarRoll + getExpectedMaxBarRollHits(score);
					if (expectedMaxHits < dan_C.GetValue()[0]) {
						dan_C.ReachStatus = Exam.ReachStatus.Danger;
						return true;
					}
				}
				resetDangerStatusIfSuccess(dan_C);
			} else {
				if (judgeGenericLess(score, dan_C, judgedNotesRemainMax))
					return true;
			}
			return false;
		}

		double amountMax = score.nNotesMax;
		double amountRemainMax = score.nNotesRemainMax;
		switch (dan_C.ExamType) {
			case Exam.Type.JudgePerfect:
			case Exam.Type.JudgeGood:
			case Exam.Type.JudgeBad:
				if (judgeGeneric(score, dan_C, judgeFailure, amountRemainMax))
					return;
				break;
			case Exam.Type.JudgeADLIB:
				amountMax = score.nAdLibMax;
				amountRemainMax = amountMax - score.judges!.nADLIBMiss;
				if (judgeGeneric(score, dan_C, judgeFailure, amountRemainMax))
					return;
				break;
			case Exam.Type.JudgeMine:
				amountMax = score.nMineMax;
				amountRemainMax = score.nMineMax - score.judges!.nMineAvoid;
				if (judgeGeneric(score, dan_C, judgeFailure, amountRemainMax))
					return;
				break;
			case Exam.Type.Combo:
				if (dan_C.ExamRange != Exam.Range.Less) {
					if (!score.hasBranch && score.nNotesRemainMax <= 0 && doFinalJudge(dan_C))
						return;
					if (dan_C.GetExamStatus() < Exam.Status.Success) {
						if (judgeFailure && score.nCombo + score.nNotesRemainMax < dan_C.GetValue()[0]) {
							dan_C.ReachStatus = Exam.ReachStatus.Failure;
							return;
						}
						if (score.nNotesRemainMax - 50 <= dan_C.GetValue()[0]) {
							dan_C.ReachStatus = Exam.ReachStatus.Danger;
							return;
						}
					}
					resetDangerStatusIfSuccess(dan_C);
				} else {
					if (judgeGenericLess(score, dan_C, amountRemainMax))
						return;
				}
				break;
			case Exam.Type.Roll:
				amountMax = score.nBarRollMax + score.nBalloonHitMax;
				amountRemainMax = (score.nBarRollMax - score.judges!.nBarRollPass) + (score.nBalloonHitMax - score.judges.nBalloonHitPass);
				if (judgeRoll(score, dan_C, judgeFailure, amountRemainMax, 0))
					return;
				break;
			case Exam.Type.Hit:
				amountMax = score.nNotesMax + score.nBarRollMax + score.nBalloonHitMax;
				amountRemainMax = score.nNotesRemainMax + (score.nBarRollMax - score.judges!.nBarRollPass) + (score.nBalloonHitMax - score.judges.nBalloonHitPass);
				if (judgeRoll(score, dan_C, judgeFailure, amountRemainMax, 1))
					return;
				break;
			case Exam.Type.Accuracy:
				if (!score.hasBranch && amountRemainMax <= 0 && doFinalJudge(dan_C))
					return;
				double accPointSuccess = dan_C.GetValue()[0] * score.nNotesMax;
				double accPointBetterSuccess = dan_C.GetValue()[1] * score.nNotesMax;
				double accPointMax = (score.judges!.nGreat + score.nNotesRemainMax) * 100 + score.judges.nGood * 50;
				double accPoint = score.judges.nGreat * 100 + score.judges.nGood * 50;
				if (dan_C.ExamRange != Exam.Range.Less) {
					if (dan_C.GetExamStatus() >= Exam.Status.Success) {
						// reuse less-type rules for blinking status
						dan_C.ReachStatus = getGenericSuccessStatusLess(dan_C, amountMax, amountRemainMax,
							(accPointMax >= accPointBetterSuccess) ? Exam.Status.Better_Success : Exam.Status.Success);
						return;
					}
					if (judgeFailure && accPointMax < accPointSuccess) {
						dan_C.ReachStatus = Exam.ReachStatus.Failure;
						return;
					}
					if (accPointMax - 0.02 * score.nNotesRemainMax * 100 < accPointSuccess && (score.nNotesRemainMax < score.nNotesMax)) {
						dan_C.ReachStatus = Exam.ReachStatus.Danger;
						return;
					}
					// else do not blink
					dan_C.ReachStatus = (dan_C.GetAmountToPercent() < 50) ? Exam.ReachStatus.Low : Exam.ReachStatus.High;
					return;
				} else {
					if (judgeFailure && accPoint >= accPointSuccess) {
						dan_C.ReachStatus = Exam.ReachStatus.Failure;
						return;
					}
					if (accPoint + 4 * 100 >= accPointSuccess && accPoint > 0) {
						dan_C.ReachStatus = Exam.ReachStatus.Danger;
						return;
					}
				}
				break;
			case Exam.Type.Score:
				// Currently calculated with gen-4 Shin-uchi score for danger and failure status
				amountMax = score.nNotesMax + score.nBarRollMax + score.nBalloonHitMax;
				amountRemainMax = score.nNotesRemainMax + (score.nBarRollMax - score.judges!.nBarRollPass) + (score.nBalloonHitMax - score.judges.nBalloonHitPass);
				if (dan_C.ExamRange != Exam.Range.Less) {
					if (!score.hasBranch && amountRemainMax <= 0 && doFinalJudge(dan_C))
						return;
					double ptsMaxRemainingNote = 0;
					for (int i = iSong; i < (isIndividualExam ? iSong + 1 : OpenTaiko.stageGameScreen.nAddScoreGen4ShinUchi_Dan.Length); ++i)
						ptsMaxRemainingNote += OpenTaiko.stageGameScreen.nAddScoreGen4ShinUchi_Dan[i] * this.GetIndividualExamScore(i).nNotesRemainMax;

					if (dan_C.GetExamStatus() < Exam.Status.Success) {
						int balloonHitRemainMax = score.nBalloonHitMax - score.judges!.nBalloonHitPass;
						double ptsMaxNoBarRoll = dan_C.Amount + ptsMaxRemainingNote + 100 * balloonHitRemainMax;
						if (judgeFailure && OpenTaiko.ConfigIni.ShinuchiMode && (ptsMaxNoBarRoll < dan_C.GetValue()[0])
							&& (score.nBarRollMax <= score.judges!.nBarRollPass)
							) {
							dan_C.ReachStatus = Exam.ReachStatus.Failure;
							return;
						}
						double ptsMaxExpected = ptsMaxNoBarRoll + 100 * getExpectedMaxBarRollHits(score);
						if (ptsMaxExpected < dan_C.GetValue()[0]) {
							dan_C.ReachStatus = Exam.ReachStatus.Danger;
							return;
						}
					}
					resetDangerStatusIfSuccess(dan_C);
				} else {
					double ptsPerHitMax = OpenTaiko.stageGameScreen.nAddScoreGen4ShinUchi_Dan.Skip(iSong).Append(100).Max();
					if (judgeGenericLess(score, dan_C, amountRemainMax, ptsPerHitMax))
						return;
				}
				break;
			default:
				if (!score.hasBranch && amountRemainMax <= 0 && doFinalJudge(dan_C))
					return;
				if (judgeFailure && dan_C.GetExamStatus() < Exam.Status.Success) {
					dan_C.ReachStatus = Exam.ReachStatus.Failure;
					return;
				}
				resetDangerStatusIfSuccess(dan_C);
				break;
		}

		if (dan_C.ReachStatus == Exam.ReachStatus.Danger)
			return; // keep danger status

		Exam.ReachStatus getGenericSuccessStatusLess(Dan_C dan_C, double amountMax, double amountRemainMax, Exam.Status status)
			=> ((amountMax < 0) ? 0 : 100 * amountRemainMax / amountMax) switch {
				_ when isAfterLastChip => (dan_C.GetExamStatus() == Exam.Status.Better_Success) ? Exam.ReachStatus.Success_Or_Better : Exam.ReachStatus.High, // no white blinking at exam end
				>= 10 => (dan_C.GetExamStatus() == Exam.Status.Better_Success) ? Exam.ReachStatus.Success_Or_Better : Exam.ReachStatus.High,
				>= 5 => (dan_C.GetExamStatus() == Exam.Status.Better_Success) ? Exam.ReachStatus.Near_Better_Success : Exam.ReachStatus.Near_Success,
				_ => (status == Exam.Status.Better_Success) ? Exam.ReachStatus.Nearer_Better_Success : Exam.ReachStatus.Nearer_Success,
			};

		dan_C.ReachStatus = (dan_C.ExamRange != Exam.Range.Less) ?
			dan_C.GetAmountToPercent() switch {
				< 50 => Exam.ReachStatus.Low,
				< 95 => Exam.ReachStatus.High,
				< 100 => Exam.ReachStatus.Near_Success,
				_ => dan_C.GetBetterAmountToPercent() switch {
					_ when isAfterLastChip => Exam.ReachStatus.Success_Or_Better, // no white blinking at exam end
					< 100.0 / 3 => Exam.ReachStatus.Success_Or_Better,
					< 200.0 / 3 => Exam.ReachStatus.Near_Better_Success,
					_ => Exam.ReachStatus.Nearer_Better_Success,
				},
			}
			: dan_C.GetAmountToPercent() switch {
				< 20 => Exam.ReachStatus.Danger,
				< 30 => Exam.ReachStatus.Low,
				_ => getGenericSuccessStatusLess(dan_C, amountMax, amountRemainMax, dan_C.GetExamStatus())
			};

	}

	public override void DeActivate() {
		// clear out ongoing animation
		Counter_In = null;
		Counter_Wait = null;
		Counter_Out = null;
		Counter_Text = null;

		for (int i = 0; i < CExamInfo.cMaxExam; i++) {
			Challenge[i] = null;
		}

		for (int i = 0; i < CExamInfo.cMaxExam; i++) {
			Status[i].Timer_Amount = null;
			Status[i].Timer_Gauge = null;
			Status[i].ctGaugeFlashLoop.CurrentValue = 0; // sync, otherwise kept as-is for displaying result
		}
		for (int i = 0; i < IsEnded.Length; i++)
			IsEnded[i] = false;

		OpenTaiko.tDisposeSafely(ref this.pfExamFont);

		Dan_Plate?.Dispose();

		base.DeActivate();
	}

	public override void CreateManagedResource() {
		Sound_Section = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Section.ogg"), ESoundGroup.SoundEffect);
		Sound_Section_First = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Section_First.wav"), ESoundGroup.SoundEffect);
		Sound_Failed = OpenTaiko.SoundManager.tCreateSound(CSkin.Path(@$"Sounds{Path.DirectorySeparatorChar}Dan{Path.DirectorySeparatorChar}Failed.ogg"), ESoundGroup.SoundEffect);
		base.CreateManagedResource();
	}

	public override void ReleaseManagedResource() {
		Sound_Section_First?.Dispose();
		Sound_Section?.tDispose();
		Sound_Failed?.tDispose();
		base.ReleaseManagedResource();
	}

	public override int Draw() {
		if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) return base.Draw();
		Counter_In?.Tick();
		Counter_Wait?.Tick();
		Counter_Out?.Tick();
		Counter_Text?.Tick();

		for (int i = 0; i < CExamInfo.cMaxExam; i++) {
			Status[i].Timer_Amount?.Tick();
		}

		// 背景を描画する。

		OpenTaiko.Tx.DanC_Background?.t2D描画(0, 0);

		DrawExam(this.Challenge, OpenTaiko.stageSongSelect.rChoosenSong.DanSongs);

		// 幕のアニメーション
		if (Counter_In != null) {
			if (Counter_In.IsUnEnded) {
				const double smoothFactor = 1 - 1 / 180.0;
				const int msLinearDuration = 150;
				int msLinearOffset = (int)(Counter_In.EndValue + 1) - msLinearDuration;
				double endValueRatio = 1 - Math.Pow(smoothFactor, Math.Min(msLinearOffset, Counter_In.CurrentValue));
				if (Counter_In.CurrentValue > msLinearOffset) {
					endValueRatio += (1 - endValueRatio) * (Counter_In.CurrentValue - msLinearOffset) / msLinearDuration;
				}
				ScreenPoint[0] = endValueRatio * ScreenPointAnchor(1) + (1 - endValueRatio) * ScreenPointAnchor(0);
				ScreenPoint[1] = endValueRatio * ScreenPointAnchor(1) + (1 - endValueRatio) * ScreenPointAnchor(2);
				OpenTaiko.Tx.DanC_Screen?.t2D描画((int)Math.Ceiling(ScreenPoint[0]) - OpenTaiko.Tx.DanC_Screen.szTextureSize.Width / 2, OpenTaiko.Skin.Game_Lane_Y[0], new Rectangle(0, 0, OpenTaiko.Tx.DanC_Screen.szTextureSize.Width / 2, OpenTaiko.Tx.DanC_Screen.szTextureSize.Height));
				OpenTaiko.Tx.DanC_Screen?.t2D描画((int)Math.Floor(ScreenPoint[1]), OpenTaiko.Skin.Game_Lane_Y[0], new Rectangle(OpenTaiko.Tx.DanC_Screen.szTextureSize.Width / 2, 0, OpenTaiko.Tx.DanC_Screen.szTextureSize.Width / 2, OpenTaiko.Tx.DanC_Screen.szTextureSize.Height));
				//CDTXMania.act文字コンソール.tPrint(0, 420, C文字コンソール.Eフォント種別.白, String.Format("{0} : {1}", ScreenPoint[0], ScreenPoint[1]));
			}
			if (Counter_In.IsEnded) {
				Counter_In = null;
				Counter_Wait = new CCounter(0, 2299, 1, OpenTaiko.Timer);
			}
		}

		if (Counter_Wait != null) {
			if (Counter_Wait.IsUnEnded) {
				// clear out ongoing animation from last song
				Counter_In = null;
				OpenTaiko.Tx.DanC_Screen?.t2D描画(OpenTaiko.Skin.Game_Lane_X[0], OpenTaiko.Skin.Game_Lane_Y[0]);

				if (NowShowingNumber != 0) {
					if (Counter_Wait.CurrentValue >= 800) {
						if (!bExamChangeCheck) {
							for (int i = 0; i < CExamInfo.cMaxExam; i++)
								ExamChange[i] = false;

							for (int j = 0; j < CExamInfo.cMaxExam; j++) {
								if (OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[0].Dan_C[j] != null) {
									if (OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count - 1].Dan_C[j] != null) //個別の条件がありますよー
									{
										Challenge[j] = OpenTaiko.stageSongSelect.rChoosenSong.DanSongs[NowShowingNumber].Dan_C[j];
										SetFlashSpeed(this.Status[j], this.Challenge[j].ReachStatus);
										ExamChange[j] = true;
									}
								}
							}
							NowCymbolShowingNumber = NowShowingNumber;
							bExamChangeCheck = true;
							this.Update(); // refresh reach status
							// clear out ongoing animation from last song
							Counter_Out = null;
							Counter_Text = null;
						}
					}
				}
			}
			if (Counter_Wait.IsEnded) {
				Counter_Wait = null;
				Counter_Out = new CCounter(0, 90, 3, OpenTaiko.Timer);
				Counter_Text = new CCounter(0, 2899, 1, OpenTaiko.Timer);
			}
		}
		if (Counter_Text != null) {
			if (Counter_Text.CurrentValue >= 2000) {
				if (OpenTaiko.TJA.List_DanSongs[NowCymbolShowingNumber].TitleTex != null) {
					OpenTaiko.TJA.List_DanSongs[NowCymbolShowingNumber].TitleTex.Opacity = Math.Max(0, 255 - (Counter_Text.CurrentValue - 2000) / 2);
				}
				if (OpenTaiko.TJA.List_DanSongs[NowCymbolShowingNumber].SubTitleTex != null) {
					OpenTaiko.TJA.List_DanSongs[NowCymbolShowingNumber].SubTitleTex.Opacity = Math.Max(0, 255 - (Counter_Text.CurrentValue - 2000) / 2);
				}
			} else {
				if (OpenTaiko.TJA.List_DanSongs[NowCymbolShowingNumber].TitleTex != null) {
					OpenTaiko.TJA.List_DanSongs[NowCymbolShowingNumber].TitleTex.Opacity = 255;
				}
				if (OpenTaiko.TJA.List_DanSongs[NowCymbolShowingNumber].SubTitleTex != null) {
					OpenTaiko.TJA.List_DanSongs[NowCymbolShowingNumber].SubTitleTex.Opacity = 255;
				}
			}
			if (Counter_Text.IsUnEnded) {

				var title = OpenTaiko.TJA.List_DanSongs[NowCymbolShowingNumber].TitleTex;
				var subTitle = OpenTaiko.TJA.List_DanSongs[NowCymbolShowingNumber].SubTitleTex;
				if (subTitle == null)
					title?.t2D拡大率考慮中央基準描画(OpenTaiko.Skin.Game_DanC_Title_X[0], OpenTaiko.Skin.Game_DanC_Title_Y[0]);
				else {
					title?.t2D拡大率考慮中央基準描画(OpenTaiko.Skin.Game_DanC_Title_X[1], OpenTaiko.Skin.Game_DanC_Title_Y[1]);
					subTitle?.t2D拡大率考慮中央基準描画(OpenTaiko.Skin.Game_DanC_SubTitle[0], OpenTaiko.Skin.Game_DanC_SubTitle[1]);
				}
			}
			if (Counter_Text.IsEnded) {
				Counter_Text = null;
				IsAnimating = false;
			}
		}
		if (Counter_Out != null) {
			if (Counter_Out.IsUnEnded) {
				double laneCoverOpenAmount = Math.Sin(Counter_Out.CurrentValue * (Math.PI / 180)) * (ScreenPointAnchor(2) - ScreenPointAnchor(0)) / 2;
				ScreenPoint[0] = ScreenPointAnchor(1) - laneCoverOpenAmount;
				ScreenPoint[1] = ScreenPointAnchor(1) + laneCoverOpenAmount;
				OpenTaiko.Tx.DanC_Screen?.t2D描画((int)Math.Ceiling(ScreenPoint[0]) - OpenTaiko.Tx.DanC_Screen.szTextureSize.Width / 2, OpenTaiko.Skin.Game_Lane_Y[0], new Rectangle(0, 0, OpenTaiko.Tx.DanC_Screen.szTextureSize.Width / 2, OpenTaiko.Tx.DanC_Screen.szTextureSize.Height));
				OpenTaiko.Tx.DanC_Screen?.t2D描画((int)Math.Floor(ScreenPoint[1]), OpenTaiko.Skin.Game_Lane_Y[0], new Rectangle(OpenTaiko.Tx.DanC_Screen.szTextureSize.Width / 2, 0, OpenTaiko.Tx.DanC_Screen.szTextureSize.Width / 2, OpenTaiko.Tx.DanC_Screen.szTextureSize.Height));
				//CDTXMania.act文字コンソール.tPrint(0, 420, C文字コンソール.Eフォント種別.白, String.Format("{0} : {1}", ScreenPoint[0], ScreenPoint[1]));
			}
			if (Counter_Out.IsEnded) {
				Counter_Out = null;
			}
		}

		#region [Dan Plate]

		CActSelect段位リスト.tDisplayDanPlate(Dan_Plate,
			null,
			OpenTaiko.Skin.Game_DanC_Dan_Plate[0],
			OpenTaiko.Skin.Game_DanC_Dan_Plate[1]);

		#endregion

		/*
        TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, TJAPlayer3.DTX.pDan_LastChip[NowShowingNumber].n発声時刻ms + " / " + CSound管理.rc演奏用タイマ.n現在時刻);

        TJAPlayer3.act文字コンソール.tPrint(100, 20, C文字コンソール.Eフォント種別.白, TJAPlayer3.DTX.pDan_LastChip[NowShowingNumber].n発声時刻ms.ToString());
        TJAPlayer3.act文字コンソール.tPrint(100, 40, C文字コンソール.Eフォント種別.白, TJAPlayer3.DTX.listChip[TJAPlayer3.DTX.listChip.Count - 1].n発声時刻ms.ToString());
        TJAPlayer3.act文字コンソール.tPrint(100, 60, C文字コンソール.Eフォント種別.白, TJAPlayer3.Timer.n現在時刻.ToString());
        */

		// Challenges that are judged when the song stops

		return base.Draw();
	}

	// Regular ingame exams draw
	public void DrawExam(ReadOnlySpan<Dan_C> dan_C, List<CTja.DanSongs> danSongs, bool isResult = false, int offX = 0) {
		int count = 0;
		int countNoGauge = 0;

		// Count exams, both with and without gauge
		for (int i = 0; i < CExamInfo.cMaxExam; i++) {
			if (dan_C[i] != null && dan_C[i].ExamIsEnable == true) {
				count++;
				if (dan_C[i].ExamType != Exam.Type.Gauge)
					countNoGauge++;
			}

		}

		// Bar position on the cert
		int currentPosition = -1;

		for (int i = 0; i < CExamInfo.cMaxExam; i++) {
			if (dan_C[i] == null || dan_C[i].ExamIsEnable != true)
				continue;

			if (dan_C[i].ExamType != Exam.Type.Gauge
				|| isResult) {
				if (dan_C[i].ExamType != Exam.Type.Gauge)
					currentPosition++;

				// Determines if a small bar will be used to optimise the display layout
				bool isSmallGauge = currentPosition >= 3 || (countNoGauge > 3 && countNoGauge % 3 > currentPosition) || countNoGauge == 6;

				// Y index of the gauge
				int yIndex = (currentPosition % 3) + 1;

				// Specific case for gauge
				if (dan_C[i].ExamType == Exam.Type.Gauge) {
					yIndex = 0;
					isSmallGauge = false;
				}


				// Panel origin
				int xOrigin = (isResult) ? OpenTaiko.Skin.DanResult_Exam[0] + offX : OpenTaiko.Skin.Game_DanC_X[1];
				int yOrigin = (isResult) ? OpenTaiko.Skin.DanResult_Exam[1] : OpenTaiko.Skin.Game_DanC_Y[1];

				// Origin position which will be used as a reference for bar elements
				int barXOffset = xOrigin + (currentPosition >= 3 ? OpenTaiko.Skin.Game_DanC_Base_Offset_X[1] : OpenTaiko.Skin.Game_DanC_Base_Offset_X[0]);
				int barYOffset = yOrigin + (currentPosition >= 3 ? OpenTaiko.Skin.Game_DanC_Base_Offset_Y[1] : OpenTaiko.Skin.Game_DanC_Base_Offset_Y[0]) + OpenTaiko.Skin.Game_DanC_Size[1] * yIndex + (yIndex * OpenTaiko.Skin.Game_DanC_Padding);

				// Small bar
				int lowerBarYOffset = barYOffset + OpenTaiko.Skin.Game_DanC_Size[1] + OpenTaiko.Skin.Game_DanC_Padding;

				// Skin X : 70
				// Skin Y : 292


				#region [Gauge base]

				if (!isSmallGauge)
					OpenTaiko.Tx.DanC_Base?.t2D描画(barXOffset, barYOffset, new RectangleF(0, ExamChange[i] ? OpenTaiko.Tx.DanC_Base.szTextureSize.Height / 2 : 0, OpenTaiko.Tx.DanC_Base.szTextureSize.Width, OpenTaiko.Tx.DanC_Base.szTextureSize.Height / 2));
				else
					OpenTaiko.Tx.DanC_Base_Small?.t2D描画(barXOffset, barYOffset, new RectangleF(0, ExamChange[i] ? OpenTaiko.Tx.DanC_Base_Small.szTextureSize.Height / 2 : 0, OpenTaiko.Tx.DanC_Base_Small.szTextureSize.Width, OpenTaiko.Tx.DanC_Base_Small.szTextureSize.Height / 2));

				#endregion

				#region [Counter wait variables]

				int examChangeFadeInOpacity = (Counter_Wait != null ? Counter_Wait.CurrentValue - 800 : 0);
				int examChangeFadeOutOpacity = (Counter_Wait != null ? 255 - (Counter_Wait.CurrentValue - (800 - 255)) : 0);

				#endregion

				#region [Small bars]

				if (ExamChange[i] == true) {
					for (int j = 0; j < danSongs.Count - 1; j++) {
						Dan_C dan_CJ = danSongs[j].Dan_C[i];
						if (!(dan_CJ != null && danSongs[this.NowCymbolShowingNumber].Dan_C[i] != null))
							continue;

						#region [Success type variables]
						int idxExamGaugeTextureJ = GetIdxExamGaugeTexture(dan_CJ.ReachStatus, dan_CJ.ExamRange);
						#endregion

						// Small bar elements base opacity

						// Bars starting from the song N
						int opacityJ = (j > this.NowShowingNumber - 3 && j <= this.NowShowingNumber - 1) ? 255 : 0;
						if (j == this.NowShowingNumber - 1) {
							// Currently showing song parameters
							if (Counter_In != null || Counter_Wait?.CurrentValue < 800) {
								opacityJ = 0;
							} else if (Counter_Wait != null) {
								opacityJ = examChangeFadeInOpacity;
							}
						} else if (j == this.NowShowingNumber - 3) {
							// Currently hiding song parameters
							if (Counter_In != null) {
								opacityJ = 255;
							} else if (Counter_Wait?.CurrentValue < 800) {
								opacityJ = examChangeFadeOutOpacity;
							}
						}
						if (opacityJ <= 0)
							continue;

						OpenTaiko.Tx.DanC_SmallBase.Opacity = opacityJ;
						OpenTaiko.Tx.DanC_Small_ExamCymbol.Opacity = opacityJ;

						OpenTaiko.Tx.Gauge_Dan_Rainbow[0].Opacity = opacityJ;
						OpenTaiko.Tx.DanC_MiniNumber.Opacity = opacityJ;

						OpenTaiko.Tx.DanC_Gauge[idxExamGaugeTextureJ].Opacity = opacityJ;

						int miniIconOpacity = opacityJ;

						// Determine bars width
						OpenTaiko.Tx.DanC_SmallBase.vcScaleRatio.X = isSmallGauge ? 0.34f : 1f;

						int smallBarGap = (int)(33f * OpenTaiko.Skin.Resolution[1] / 720f);

						// 815 : Small base (70 + 745)
						int miniBarPositionX = barXOffset + (isSmallGauge ? OpenTaiko.Skin.Game_DanC_SmallBase_Offset_X[1] : OpenTaiko.Skin.Game_DanC_SmallBase_Offset_X[0]);

						// 613 + j * 33 : Small base (barYoffset for 3rd exam : 494 + 119 + Local song offset j * 33)
						int miniBarPositionY = (barYOffset + (isSmallGauge ? OpenTaiko.Skin.Game_DanC_SmallBase_Offset_Y[1] : OpenTaiko.Skin.Game_DanC_SmallBase_Offset_Y[0])) + (j % 2) * smallBarGap - (OpenTaiko.Skin.Game_DanC_Size[1] + (OpenTaiko.Skin.Game_DanC_Padding));

						// Display bars
						#region [Displayables]

						// Display mini-bar base and small symbol
						OpenTaiko.Tx.DanC_SmallBase?.t2D描画(miniBarPositionX, miniBarPositionY);
						OpenTaiko.Tx.DanC_Small_ExamCymbol?.t2D描画(miniBarPositionX - 30, miniBarPositionY - 3, new RectangleF(0, j * 28, 30, 28));

						// Display bar content
						if (dan_CJ.ReachStatus == Exam.ReachStatus.Better_Success) {
							OpenTaiko.Tx.Gauge_Dan_Rainbow[0].vcScaleRatio.X = 0.23875f * OpenTaiko.Tx.DanC_SmallBase.vcScaleRatio.X * (isSmallGauge ? 0.94f : 1f);
							OpenTaiko.Tx.Gauge_Dan_Rainbow[0].vcScaleRatio.Y = 0.35185f;

							OpenTaiko.Tx.Gauge_Dan_Rainbow[0]?.t2D描画(miniBarPositionX + 3, miniBarPositionY + 2,
								new Rectangle(0, 0, (int)(dan_CJ.GetAmountToPercent() * (OpenTaiko.Tx.Gauge_Dan_Rainbow[0].szTextureSize.Width / 100.0)), OpenTaiko.Tx.Gauge_Dan_Rainbow[0].szTextureSize.Height));
						} else {
							OpenTaiko.Tx.DanC_Gauge[idxExamGaugeTextureJ].vcScaleRatio.X = 0.23875f * OpenTaiko.Tx.DanC_SmallBase.vcScaleRatio.X * (isSmallGauge ? 0.94f : 1f);
							OpenTaiko.Tx.DanC_Gauge[idxExamGaugeTextureJ].vcScaleRatio.Y = 0.35185f;

							OpenTaiko.Tx.DanC_Gauge[idxExamGaugeTextureJ]?.t2D描画(miniBarPositionX + 3, miniBarPositionY + 2,
								new Rectangle(0, 0, (int)(dan_CJ.GetAmountToPercent() * (OpenTaiko.Tx.DanC_Gauge[idxExamGaugeTextureJ].szTextureSize.Width / 100.0)), OpenTaiko.Tx.DanC_Gauge[idxExamGaugeTextureJ].szTextureSize.Height));
						}

						int _tmpMiniPadding = (int)(14f * OpenTaiko.Skin.Resolution[0] / 1280f);

						// Usually +23 for gold and +17 for white, to test
						DrawMiniNumber(
							dan_CJ.GetDisplayedAmount(),
							miniBarPositionX + 11,
							miniBarPositionY + 20,
							_tmpMiniPadding,
							dan_CJ.ReachStatus);

						CActSelect段位リスト.tDisplayDanIcon(j + 1, miniBarPositionX + OpenTaiko.Skin.Game_DanC_DanIcon_Offset_Mini[0], miniBarPositionY + OpenTaiko.Skin.Game_DanC_DanIcon_Offset_Mini[1], miniIconOpacity, 0.5f, false);

						#endregion
					}
				}

				#endregion

				#region [Currently playing song icons]

				OpenTaiko.Tx.DanC_ExamCymbol.Opacity = 255;

				if (ExamChange[i] && NowShowingNumber != 0) {
					if (Counter_Wait != null) {
						if (Counter_Wait.CurrentValue >= 800)
							OpenTaiko.Tx.DanC_ExamCymbol.Opacity = examChangeFadeInOpacity;
						else if (Counter_Wait.CurrentValue >= 800 - 255)
							OpenTaiko.Tx.DanC_ExamCymbol.Opacity = examChangeFadeOutOpacity;
					}
				}

				//75, 418
				// 292 - 228 = 64
				if (ExamChange[i]) {
					OpenTaiko.Tx.DanC_ExamCymbol.t2D描画(barXOffset + 5, lowerBarYOffset - 64, new RectangleF(0, 41 * NowCymbolShowingNumber, 197, 41));
				}

				#endregion

				#region [Large bars]

				#region [Success type variables]
				int idxExamGaugeTexture = GetIdxExamGaugeTexture(dan_C[i].ReachStatus, dan_C[i].ExamRange);
				#endregion

				// rainbowIndex : Rainbow bar texture to display (int), rainbowBase : same as rainbowIndex, but 0 if the counter is maxed
				#region [Rainbow gauge counter]

				int rainbowIndex = 0;
				int rainbowBase = 0;
				if (dan_C[i].ReachStatus == Exam.ReachStatus.Better_Success) {
					this.ct虹アニメ.TickLoop();
					this.ct虹透明度.TickLoop();

					rainbowIndex = this.ct虹アニメ.CurrentValue;

					rainbowBase = rainbowIndex;
					if (rainbowBase == ct虹アニメ.EndValue) rainbowBase = 0;
				}
				this.Status[i].ctGaugeFlashLoop.TickLoop();
				if (!isResult) {
					this.Status[i].Timer_Gauge!.Tick();
					if (this.Status[i].Timer_Gauge!.IsTicked && this.Status[i].Timer_Gauge!.IsEnded) {
						this.Status[i].Timer_Gauge!.Stop();
						this.Status[i].Timer_Gauge!.CurrentValue = (int)this.Status[i].Timer_Gauge!.EndValue;
					}
				}

				#endregion

				// Default opacity
				int opacity = 255;

				if (ExamChange[i] && NowShowingNumber != 0 && Counter_Wait != null) {
					if (Counter_Wait.CurrentValue >= 800) {
						opacity = examChangeFadeInOpacity;
					} else if (Counter_Wait.CurrentValue >= 800 - 255) {
						opacity = examChangeFadeOutOpacity;
					}
				}

				// Flash value
				float whiteFlashValue = (isResult || !this.Status[i].Timer_Gauge!.IsTicked) ? 0
					: Math.Min(1, ((int)this.Status[i].Timer_Gauge!.EndValue - this.Status[i].Timer_Gauge!.CurrentValue) / 315f);
				int gaugeOpacity = opacity;
				if (dan_C[i].ReachStatus == Exam.ReachStatus.Failure) {
					gaugeOpacity = 0; // "grey"
				} else if (GetExamGaugeFlashSpeed(dan_C[i].ReachStatus) != 0) {
					float loopFlashValue = (1 + MathF.Cos(
						2 * MathF.PI
						* Math.Max(0, this.Status[i].ctGaugeFlashLoop.CurrentValue) / (float)this.Status[i].ctGaugeFlashLoop.EndValue
					)) / 2;
					if (dan_C[i].ReachStatus == Exam.ReachStatus.Danger) {
						gaugeOpacity = (int)(opacity * loopFlashValue); // "grey" flash
					} else {
						whiteFlashValue = Math.Max(whiteFlashValue, loopFlashValue); // white flash
					}
				}

				OpenTaiko.Tx.DanC_Gauge[idxExamGaugeTexture].Opacity = gaugeOpacity;

				OpenTaiko.Tx.Gauge_Dan_Rainbow[rainbowIndex].Opacity = gaugeOpacity;

				OpenTaiko.Tx.DanC_Number.Opacity = opacity;
				OpenTaiko.Tx.DanC_ExamRange.Opacity = opacity;
				OpenTaiko.Tx.DanC_Small_Number.Opacity = opacity;

				int iconOpacity = opacity;

				#region [Displayables]

				// Non individual : 209 / 650 : 0.32154f
				// Individual : 97 / 432 : 0.22454f

				float xExtend = ExamChange[i] ? (isSmallGauge ? 0.215f * 0.663333333f : 0.663333333f) : (isSmallGauge ? 0.32154f : 1.0f);

				void drawGauge(CTexture gaugeTexture, ReadOnlySpan<Dan_C> dan_C) {
					if (gaugeTexture == null)
						return;

					gaugeTexture.vcScaleRatio.X = xExtend;
					gaugeTexture.vcScaleRatio.Y = 1.0f;
					gaugeTexture.t2D拡大率考慮下基準描画(
						barXOffset + OpenTaiko.Skin.Game_DanC_Offset[0], lowerBarYOffset - OpenTaiko.Skin.Game_DanC_Offset[1],
						new Rectangle(0, 0,
							(int)(dan_C[i].GetAmountToPercent() * (gaugeTexture.szTextureSize.Width / 100.0)),
							gaugeTexture.szTextureSize.Height));

					// flash layer
					int opacityLast = gaugeTexture.Opacity;
					gaugeTexture.color4 = new(255.0f, 255.0f, 255.0f, 1.0f); // hack: make the texture white
					gaugeTexture.Opacity = (int)(255 * whiteFlashValue);
					gaugeTexture.t2D拡大率考慮下基準描画(
						barXOffset + OpenTaiko.Skin.Game_DanC_Offset[0], lowerBarYOffset - OpenTaiko.Skin.Game_DanC_Offset[1],
						new Rectangle(0, 0,
							(int)(dan_C[i].GetAmountToPercent() * (gaugeTexture.szTextureSize.Width / 100.0)),
							gaugeTexture.szTextureSize.Height));
					gaugeTexture.color4 = new(1.0f, 1.0f, 1.0f, 1.0f);
					gaugeTexture.Opacity = opacityLast;
				}

				if (dan_C[i].ReachStatus == Exam.ReachStatus.Better_Success) {
					#region [Rainbow gauge display]
					var gaugeTexture = OpenTaiko.Tx.Gauge_Dan_Rainbow[rainbowIndex];
					if (Counter_Wait != null && !(Counter_Wait.CurrentValue <= 1055 && Counter_Wait.CurrentValue >= 800 - 255)) {
						gaugeTexture.Opacity = 255;
					}
					drawGauge(gaugeTexture, dan_C);

					gaugeTexture = OpenTaiko.Tx.Gauge_Dan_Rainbow[rainbowBase];
					if (Counter_Wait != null && !(Counter_Wait.CurrentValue <= 1055 && Counter_Wait.CurrentValue >= 800 - 255)) {
						gaugeTexture.Opacity = (ct虹透明度.CurrentValue * 255 / (int)ct虹透明度.EndValue) / 1;
					}
					drawGauge(gaugeTexture, dan_C);
					#endregion
				} else {
					// Regular gauge display
					drawGauge(OpenTaiko.Tx.DanC_Gauge[idxExamGaugeTexture], dan_C);
				}

				#endregion


				#endregion

				#region [Print the current value number]
				float numberXScale = isSmallGauge ? OpenTaiko.Skin.Game_DanC_Number_Small_Scale * 0.6f : OpenTaiko.Skin.Game_DanC_Number_Small_Scale;
				float numberYScale = isSmallGauge ? OpenTaiko.Skin.Game_DanC_Number_Small_Scale * 0.8f : OpenTaiko.Skin.Game_DanC_Number_Small_Scale;
				int numberPadding = (int)(OpenTaiko.Skin.Game_DanC_Number_Padding * (isSmallGauge ? 0.6f : 1f));

				DrawNumber(dan_C[i].GetDisplayedAmount(),
					barXOffset + OpenTaiko.Skin.Game_DanC_Number_Small_Number_Offset[0],
					lowerBarYOffset - OpenTaiko.Skin.Game_DanC_Number_Small_Number_Offset[1],
					numberPadding,
					true,
					dan_C[i].ReachStatus,
					numberXScale,
					numberYScale,
					(Status[i].Timer_Amount != null ? ScoreScale[Status[i].Timer_Amount.CurrentValue] : 0f));

				#endregion

				if (ExamChange[i]) {
					CActSelect段位リスト.tDisplayDanIcon(this.NowCymbolShowingNumber + 1, barXOffset + OpenTaiko.Skin.Game_DanC_DanIcon_Offset[0], barYOffset + OpenTaiko.Skin.Game_DanC_DanIcon_Offset[1], iconOpacity, 0.6f, true);
				}


				#region [Dan conditions display]

				int offset = OpenTaiko.Skin.Game_DanC_Exam_Offset[0];

				OpenTaiko.Tx.DanC_ExamType.vcScaleRatio.X = 1.0f;
				OpenTaiko.Tx.DanC_ExamType.vcScaleRatio.Y = 1.0f;

				// Exam range (Less than/More)
				OpenTaiko.Tx.DanC_ExamRange?.t2D拡大率考慮下基準描画(
					barXOffset + offset - OpenTaiko.Tx.DanC_ExamRange.szTextureSize.Width,
					lowerBarYOffset - OpenTaiko.Skin.Game_DanC_Exam_Offset[1],
					new Rectangle(0, OpenTaiko.Skin.Game_DanC_ExamRange_Size[1] * (int)dan_C[i].ExamRange, OpenTaiko.Skin.Game_DanC_ExamRange_Size[0], OpenTaiko.Skin.Game_DanC_ExamRange_Size[1]));

				offset -= OpenTaiko.Skin.Game_DanC_ExamRange_Padding;

				// Condition number
				DrawNumber(
					dan_C[i].GetValue()[0],
					barXOffset + offset - dan_C[i].GetValue()[0].ToString().Length * (int)(OpenTaiko.Skin.Game_DanC_Number_Small_Padding * OpenTaiko.Skin.Game_DanC_Exam_Number_Scale),
					lowerBarYOffset - OpenTaiko.Skin.Game_DanC_Exam_Offset[1] - 1,
					(int)(OpenTaiko.Skin.Game_DanC_Number_Small_Padding * OpenTaiko.Skin.Game_DanC_Exam_Number_Scale),
					false,
					dan_C[i].ReachStatus);

				int _offexX = (int)(22f * OpenTaiko.Skin.Resolution[0] / 1280f);
				int _offexY = (int)(48f * OpenTaiko.Skin.Resolution[1] / 720f);
				int _examX = barXOffset + OpenTaiko.Skin.Game_DanC_Exam_Offset[0] - OpenTaiko.Tx.DanC_ExamType.szTextureSize.Width + _offexX;
				int _examY = lowerBarYOffset - OpenTaiko.Skin.Game_DanC_Exam_Offset[1] - _offexY;

				// Exam type flag
				OpenTaiko.Tx.DanC_ExamType?.t2D拡大率考慮下基準描画(
					_examX,
					_examY,
					new Rectangle(0, 0, OpenTaiko.Skin.Game_DanC_ExamType_Size[0], OpenTaiko.Skin.Game_DanC_ExamType_Size[1]));

				if ((int)dan_C[i].ExamType < this.ttkExams.Length)
					TitleTextureKey.ResolveTitleTexture(this.ttkExams[(int)dan_C[i].ExamType]).t2D拡大率考慮中央基準描画(
						_examX + OpenTaiko.Skin.Game_DanC_ExamType_Size[0] / 2,
						_examY - OpenTaiko.Skin.Game_DanC_ExamType_Size[1] / 2);


				/*
                TJAPlayer3.Tx.DanC_ExamType?.t2D拡大率考慮下基準描画(
                    barXOffset + TJAPlayer3.Skin.Game_DanC_Exam_Offset[0] - TJAPlayer3.Tx.DanC_ExamType.szテクスチャサイズ.Width + 22,
                    lowerBarYOffset - TJAPlayer3.Skin.Game_DanC_Exam_Offset[1] - 48,
                    new Rectangle(0, TJAPlayer3.Skin.Game_DanC_ExamType_Size[1] * (int)dan_C[i].GetExamType(), TJAPlayer3.Skin.Game_DanC_ExamType_Size[0], TJAPlayer3.Skin.Game_DanC_ExamType_Size[1]));
                */

				#endregion

				#region [Failed condition box]

				OpenTaiko.Tx.DanC_Failed.vcScaleRatio.X = isSmallGauge ? 0.33f : 1f;

				if (dan_C[i].ReachStatus == Exam.ReachStatus.Failure) {
					OpenTaiko.Tx.DanC_Failed.Opacity = (isResult ? 255 : Math.Min(255, 255 * this.Status[i].Timer_Gauge.CurrentValue / 85));
					OpenTaiko.Tx.DanC_Failed.t2D拡大率考慮下基準描画(
						barXOffset + OpenTaiko.Skin.Game_DanC_Offset[0],
						lowerBarYOffset - OpenTaiko.Skin.Game_DanC_Offset[1]);
				}

				#endregion

			} else {
				#region [Gauge dan condition]

				int _scale = (int)(14f * OpenTaiko.Skin.Resolution[0] / 1280f);
				int _nbX = (int)(292f * OpenTaiko.Skin.Resolution[0] / 1280f);
				int _nbY = (int)(64f * OpenTaiko.Skin.Resolution[0] / 1280f);
				int _offexX = (int)(104f * OpenTaiko.Skin.Resolution[0] / 1280f);
				int _offexY = (int)(21f * OpenTaiko.Skin.Resolution[1] / 720f);

				OpenTaiko.Tx.DanC_Gauge_Base?.t2D描画(
					OpenTaiko.Skin.Game_DanC_X[0] - ((50 - dan_C[i].GetValue()[0] / 2) * _scale) + 4,
					OpenTaiko.Skin.Game_DanC_Y[0]);

				TitleTextureKey.ResolveTitleTexture(this.ttkExams[(int)Exam.Type.Gauge]).t2D拡大率考慮中央基準描画(
					OpenTaiko.Skin.Game_DanC_X[0] - ((50 - dan_C[i].GetValue()[0] / 2) * _scale) + _offexX,
					OpenTaiko.Skin.Game_DanC_Y[0] + _offexY);

				// Display percentage here
				DrawNumber(
					dan_C[i].GetValue()[0],
					OpenTaiko.Skin.Game_DanC_X[0] - ((50 - dan_C[i].GetValue()[0] / 2) * _scale) + _nbX - dan_C[i].GetValue()[0].ToString().Length * (int)(OpenTaiko.Skin.Game_DanC_Number_Small_Padding * OpenTaiko.Skin.Game_DanC_Exam_Number_Scale),
					OpenTaiko.Skin.Game_DanC_Y[0] - OpenTaiko.Skin.Game_DanC_Exam_Offset[1] + _nbY,
					(int)(OpenTaiko.Skin.Game_DanC_Number_Small_Padding * OpenTaiko.Skin.Game_DanC_Exam_Number_Scale),
					false,
					dan_C[i].ReachStatus);

				#endregion
			}
		}
	}

	/// <summary>
	/// 段位チャレンジの数字フォントで数字を描画します。
	/// </summary>
	/// <param name="value">値。</param>
	/// <param name="x">一桁目のX座標。</param>
	/// <param name="y">一桁目のY座標</param>
	/// <param name="padding">桁数間の字間</param>
	/// <param name="scaleX">拡大率X</param>
	/// <param name="scaleY">拡大率Y</param>
	/// <param name="scaleJump">アニメーション用拡大率(Yに加算される)。</param>
	private static void DrawNumber(int value, int x, int y, int padding, bool bBig, Exam.ReachStatus reachStatus, float scaleX = 1.0f, float scaleY = 1.0f, float scaleJump = 0.0f) {
		if (OpenTaiko.Tx.DanC_Number == null || OpenTaiko.Tx.DanC_Small_Number == null || value < 0)
			return;

		if (value == 0 || reachStatus == Exam.ReachStatus.Failure) {
			OpenTaiko.Tx.DanC_Number.color4 = CConversion.ColorToColor4(Color.Gray);
			OpenTaiko.Tx.DanC_Small_Number.color4 = CConversion.ColorToColor4(Color.Gray);
		} else {
			OpenTaiko.Tx.DanC_Number.color4 = CConversion.ColorToColor4(Color.White);
			OpenTaiko.Tx.DanC_Small_Number.color4 = CConversion.ColorToColor4(Color.White);
		}

		if (bBig) {
			var notesRemainDigit = 0;
			for (int i = 0; i < value.ToString().Length; i++) {
				var number = Convert.ToInt32(value.ToString()[i].ToString());
				Rectangle rectangle = new Rectangle(OpenTaiko.Skin.Game_DanC_Number_Size[0] * number - 1, (reachStatus == Exam.ReachStatus.Better_Success) ? OpenTaiko.Skin.Game_DanC_Number_Size[1] : 0, OpenTaiko.Skin.Game_DanC_Number_Size[0], OpenTaiko.Skin.Game_DanC_Number_Size[1]);
				if (OpenTaiko.Tx.DanC_Number != null) {
					OpenTaiko.Tx.DanC_Number.vcScaleRatio.X = scaleX;
					OpenTaiko.Tx.DanC_Number.vcScaleRatio.Y = scaleY + scaleJump;
				}
				OpenTaiko.Tx.DanC_Number?.t2D拡大率考慮下中心基準描画(x - (notesRemainDigit * padding), y, rectangle);
				notesRemainDigit--;
			}
		} else {
			var notesRemainDigit = 0;
			for (int i = 0; i < value.ToString().Length; i++) {
				var number = Convert.ToInt32(value.ToString()[i].ToString());
				Rectangle rectangle = new Rectangle(OpenTaiko.Skin.Game_DanC_Small_Number_Size[0] * number - 1, 0, OpenTaiko.Skin.Game_DanC_Small_Number_Size[0], OpenTaiko.Skin.Game_DanC_Small_Number_Size[1]);
				if (OpenTaiko.Tx.DanC_Small_Number != null) {
					OpenTaiko.Tx.DanC_Small_Number.vcScaleRatio.X = scaleX;
					OpenTaiko.Tx.DanC_Small_Number.vcScaleRatio.Y = scaleY + scaleJump;
				}
				OpenTaiko.Tx.DanC_Small_Number?.t2D拡大率考慮下中心基準描画(x - (notesRemainDigit * padding), y, rectangle);
				notesRemainDigit--;
			}
		}
	}

	public static void DrawMiniNumber(int value, int x, int y, int padding, Exam.ReachStatus reachStatus) {
		if (OpenTaiko.Tx.DanC_MiniNumber == null || value < 0)
			return;

		var notesRemainDigit = 0;
		if (value < 0)
			return;
		for (int i = 0; i < value.ToString().Length; i++) {
			var number = Convert.ToInt32(value.ToString()[i].ToString());
			Rectangle rectangle = new Rectangle(OpenTaiko.Skin.Game_DanC_MiniNumber_Size[0] * number - 1, (reachStatus == Exam.ReachStatus.Better_Success) ? OpenTaiko.Skin.Game_DanC_MiniNumber_Size[1] : 0, OpenTaiko.Skin.Game_DanC_MiniNumber_Size[0], OpenTaiko.Skin.Game_DanC_MiniNumber_Size[1]);
			OpenTaiko.Tx.DanC_MiniNumber.t2D拡大率考慮下中心基準描画(x - (notesRemainDigit * padding), y, rectangle);
			notesRemainDigit--;
		}
	}

	/// <summary>
	/// n個の条件がひとつ以上達成失敗しているかどうかを返します。
	/// </summary>
	/// <returns>n個の条件がひとつ以上達成失敗しているか。</returns>
	public static bool GetFailedAllChallenges(ReadOnlySpan<Dan_C> dan_C, List<CTja.DanSongs> danSongs) {
		for (int i = 0; i < CExamInfo.cMaxExam; i++) {
			if (dan_C[i] == null)
				continue;
			for (int j = 0; j < danSongs.Count; j++) {
				if (danSongs[j].Dan_C[i]?.ReachStatus == Exam.ReachStatus.Failure)
					return true;
			}
			if (dan_C[i].ReachStatus == Exam.ReachStatus.Failure)
				return true;
		}
		return false;
	}

	/// <summary>
	/// n個の条件で段位認定モードのステータスを返します。
	/// </summary>
	/// <param name="dan_C">条件。</param>
	/// <returns>ExamStatus。</returns>
	public Exam.Status GetResultExamStatus(ReadOnlySpan<Dan_C> dan_C, List<CTja.DanSongs> danSongs) {
		var status = Exam.Status.Better_Success;

		for (int i = 0; i < CExamInfo.cMaxExam; i++) {
			if (ExamChange[i] && status == Exam.Status.Better_Success) {
				for (int j = 0; j < danSongs.Count - 1; j++) { // last song already being checked
					if (danSongs[j].Dan_C[i]?.GetExamStatus() < status) {
						status = danSongs[j].Dan_C[i].GetExamStatus();
						break;
					}
				}
			}

			if (dan_C[i] == null || !dan_C[i].ExamIsEnable)
				continue;

			if (dan_C[i].GetExamStatus() < status)
				status = dan_C[i].GetExamStatus();
			if (status < Exam.Status.Success) // only possible for the last reached song
				return Exam.Status.Failure;
		}

		return status;
	}

	public ReadOnlySpan<Dan_C> GetExam() => this.Challenge;

	private readonly float[] ScoreScale = new float[]
	{
		0.000f,
		0.111f, // リピート
		0.222f,
		0.185f,
		0.148f,
		0.129f,
		0.111f,
		0.074f,
		0.065f,
		0.033f,
		0.015f,
		0.000f
	};

	struct ChallengeStatus {
		public CCounter ctGaugeFlashLoop;
		public CCounter? Timer_Gauge;
		public CCounter? Timer_Amount;
	}

	#region[ private ]
	//-----------------

	private bool bExamChangeCheck;
	private int notesremain;
	private int[] songsnotesremain;
	private bool[] ExamChange = new bool[CExamInfo.cMaxExam];
	private int ExamCount;
	private ChallengeStatus[] Status = new ChallengeStatus[CExamInfo.cMaxExam];
	private CTexture Dan_Plate;
	private bool[] IsEnded;
	public bool FirstSectionAnime;

	// アニメ関連
	public int NowShowingNumber;
	public int NowCymbolShowingNumber;
	private CCounter? Counter_In, Counter_Wait, Counter_Out, Counter_Text;
	private double[] ScreenPoint;
	public bool IsAnimating;

	private static double ScreenPointAnchor(int i) => i switch {
		0 => OpenTaiko.Skin.Game_Lane_X[0],
		1 => OpenTaiko.Skin.Game_Lane_X[0] + (OpenTaiko.Skin.Resolution[0] - OpenTaiko.Skin.Game_Lane_X[0]) / 2.0,
		_ => OpenTaiko.Skin.Resolution[0],
	};

	//音声関連
	private CSound Sound_Section;
	private CSound Sound_Section_First;
	private CSound Sound_Failed;

	private CCounter ct虹アニメ;
	private CCounter ct虹透明度;

	private CCachedFontRenderer pfExamFont;
	private TitleTextureKey[] ttkExams;

	//-----------------
	#endregion
}
