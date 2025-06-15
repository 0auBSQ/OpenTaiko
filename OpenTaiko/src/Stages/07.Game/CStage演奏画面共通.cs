using System.Diagnostics;
using System.Drawing;
using FDK;
using FDK.ExtensionMethods;

namespace OpenTaiko;

/// <summary>
/// 演奏画面の共通クラス (ドラム演奏画面, ギター演奏画面の継承元)
/// </summary>
internal abstract class CStage演奏画面共通 : CStage {
	// Properties

	// メソッド

	#region [ t演奏結果を格納する_ドラム() ]
	public void t演奏結果を格納する_ドラム(out CScoreIni.C演奏記録 Drums) {
		Drums = new CScoreIni.C演奏記録();

		{
			Drums.nGoodCount = OpenTaiko.ConfigIni.bAutoPlay[0] ? this.nHitCount_InclAuto.Drums.Perfect : this.nHitCount_ExclAuto.Drums.Perfect;
			Drums.nOkCount = OpenTaiko.ConfigIni.bAutoPlay[0] ? this.nHitCount_InclAuto.Drums.Great : this.nHitCount_ExclAuto.Drums.Great;
			Drums.nBadCount = OpenTaiko.ConfigIni.bAutoPlay[0] ? this.nHitCount_InclAuto.Drums.Miss : this.nHitCount_ExclAuto.Drums.Miss;

			// save result, as the original will be cleaned
			// individual exams are saved to stageGameSelection
			var danC = OpenTaiko.stageGameScreen.actDan.GetExam();
			for (int i = 0; i < danC.Length; i++) {
				Drums.Dan_C[i] = danC[i];
			}
		}
	}
	#endregion

	// CStage 実装

	public int[] nNoteCount = new int[5];
	public int[] nBalloonHitCount = new int[5];
	public double[] nRollTimeMs = new double[5];
	public double[] nAddScoreGen4ShinUchi = new double[5];
	public int[] scoreMode = new int[5];

	public int[] nNoteCount_Dan = []; // [iDanSong]
	public int[] nBalloonHitCount_Dan = [];
	public double[] nRollTimeMs_Dan = [];
	public double[] nAddScoreGen4ShinUchi_Dan = [];

	public override void Activate() {
		listChip = new List<CChip>[5];
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			listChip[i] = OpenTaiko.GetTJA(i)!.listChip;
		}

		if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			this.CalculateGen4ShinUchiScoreParameters_Dan();
		} else {
			this.CalculateGen4ShinUchiScoreParameters();
		}


		for (int index = OpenTaiko.TJA.listChip.Count - 1; index >= 0; index--) {
			if (OpenTaiko.TJA.listChip[index].nChannelNo == 0x01) {
				this.bgmlength = OpenTaiko.TJA.listChip[index].GetDuration() + OpenTaiko.TJA.listChip[index].n発声時刻ms;
				break;
			}
		}

		this.AIBattleSections = new List<AIBattleSection>();

		CChip endChip = null;
		for (int i = 0; i < listChip[0].Count; i++) {
			CChip chip = listChip[0][i];
			if (endChip == null || (chip.n発声時刻ms > endChip.n発声時刻ms && chip.nChannelNo == 0x50)) {
				endChip = chip;
			}
		}

		int battleSectionCount = 3 + ((endChip.n発声時刻ms * 2) / 100000);
		// Avoid single section
		if (battleSectionCount <= 1)
			battleSectionCount = 3;
		// Avoid ties
		if (battleSectionCount % 2 == 0)
			battleSectionCount -= 1;


		int battleSectionTime = 0;

		int nowBattleSectionCount = 1;

		for (int i = 0; i < listChip[0].Count; i++) {
			CChip chip = listChip[0][i];

			if (nowBattleSectionCount == battleSectionCount) {
				chip = endChip;
				i = listChip[0].Count - 1;
			}

			int endtime = endChip.n発声時刻ms / battleSectionCount;

			bool isAddSection = (nowBattleSectionCount != battleSectionCount) ?
				chip.n発声時刻ms >= endtime * nowBattleSectionCount :
				i == listChip[0].Count - 1;


			if (isAddSection) {
				AIBattleSection aIBattleSection = new AIBattleSection();

				aIBattleSection.StartTime = battleSectionTime;
				aIBattleSection.EndTime = chip.n発声時刻ms;
				aIBattleSection.Length = aIBattleSection.EndTime - aIBattleSection.StartTime;

				this.AIBattleSections.Add(aIBattleSection);

				battleSectionTime = aIBattleSection.EndTime;
				nowBattleSectionCount++;
			}
		}

		ctChipAnime = new CCounter[5];
		ctChipAnimeLag = new CCounter[5];
		for (int i = 0; i < 5; i++) {
			ctChipAnime[i] = new CCounter();
			ctChipAnimeLag[i] = new CCounter();
		}

		listWAV = OpenTaiko.TJA.listWAV;


		for (int k = 0; k < 4; k++) {
			//for ( int n = 0; n < 5; n++ )
			//{
			this.nHitCount_ExclAuto[k] = new CHITCOUNTOFRANK();
			this.nHitCount_InclAuto[k] = new CHITCOUNTOFRANK();
			//}
			this.r現在の歓声Chip[k] = null;
			this.bReverse[k] = OpenTaiko.ConfigIni.bReverse[k];

		}

		base.Activate();
		this.tパネル文字列の設定();
		//this.演奏判定ライン座標();
		this.bIsGOGOTIME = new bool[] { false, false, false, false, false };
		this.bWasGOGOTIME = new bool[] { false, false, false, false, false };
		this.bIsMiss = new bool[] { false, false, false, false, false };
		this.bUseBranch = new bool[] { false, false, false, false, false };
		this.nCurrentBranch = new CTja.ECourse[5];
		this.nNextBranch = new CTja.ECourse[5];

		for (int i = 0; i < 5; i++) {
			OpenTaiko.stageGameScreen.actMtaiko.After[i] = CTja.ECourse.eNormal;
			OpenTaiko.stageGameScreen.actLaneTaiko.stBranch[i].nAfter = CTja.ECourse.eNormal;
			OpenTaiko.stageGameScreen.actMtaiko.Before[i] = CTja.ECourse.eNormal;
			OpenTaiko.stageGameScreen.actLaneTaiko.stBranch[i].nBefore = CTja.ECourse.eNormal;
		}

		for (int i = 0; i < CBranchScore.Length; i++) {
			this.CBranchScore[i] = new CBRANCHSCORE();

			//大音符分岐時の情報をまとめるため
			this.CBranchScore[i].cBigNotes = new CBRANCHSCORE();
		}


		this.nDisplayedBranchLane = new CTja.ECourse[5];
		this.bCurrentlyDrumRoll = new bool[] { false, false, false, false, false };
		this.nCurrentRollCount = new int[] { 0, 0, 0, 0, 0 };
		this.n分岐した回数 = new int[5];
		this.Chara_MissCount = new int[5];
		this.bLEVELHOLD = new bool[] { false, false, false, false, false };
		this.JPOSCROLLX = new double[5];
		this.JPOSCROLLY = new double[5];
		eFirstGameType = new EGameType[5];
		bSplitLane = new bool[5];


		// Double play set here
		this.isMultiPlay = OpenTaiko.ConfigIni.nPlayerCount >= 2 ? true : false;

		this.nLoopCount_Clear = 1;

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			actGauge.Init(OpenTaiko.ConfigIni.nRisky, i);                                  // #23559 2011.7.28 yyagi
			eFirstGameType[i] = OpenTaiko.ConfigIni.nGameType[i];
		}
		this.nPolyphonicSounds = OpenTaiko.ConfigIni.nPoliphonicSounds;

		OpenTaiko.Skin.tRemoveMixerAll();  // 効果音のストリームをミキサーから解除しておく

		queueMixerSound = new Queue<stmixer>(64);
		bIsDirectSound = (OpenTaiko.SoundManager.GetCurrentSoundDeviceType() == "DirectSound");
		bUseOSTimer = OpenTaiko.ConfigIni.bUseOSTimer;
		this.bPAUSE = false;
		dbSongPlaybackSpeed = OpenTaiko.ConfigIni.SongPlaybackSpeed;
		bValidScore = true;

		#region [ 演奏開始前にmixer登録しておくべきサウンド(開幕してすぐに鳴らすことになるチップ音)を登録しておく ]
		foreach (CChip pChip in listChip[0]) {
			//				Debug.WriteLine( "CH=" + pChip.nチャンネル番号.ToString( "x2" ) + ", 整数値=" + pChip.n整数値 +  ", time=" + pChip.n発声時刻ms );
			if (pChip.n発声時刻ms <= 0) {
				if (pChip.nChannelNo == 0xDA) {
					pChip.bHit = true;
					//						Trace.TraceInformation( "first [DA] BAR=" + pChip.n発声位置 / 384 + " ch=" + pChip.nチャンネル番号.ToString( "x2" ) + ", wav=" + pChip.n整数値 + ", time=" + pChip.n発声時刻ms );
					if (listWAV.TryGetValue(pChip.n整数値_内部番号, out CTja.CWAV wc)) {
						for (int i = 0; i < nPolyphonicSounds; i++) {
							if (wc.rSound[i] != null) {
								OpenTaiko.SoundManager.AddMixer(wc.rSound[i], dbSongPlaybackSpeed, pChip.b演奏終了後も再生が続くチップである);
								//AddMixer( wc.rSound[ i ] );		// 最初はqueueを介さず直接ミキサー登録する
							}
						}
					}
				}
			} else {
				break;
			}
		}
		#endregion

		// Note
		if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			this.DanSongScore = new CBRANCHSCORE[OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count];
			for (int i = 0; i < this.DanSongScore.Length; ++i)
				this.DanSongScore[i] = new();
		}


		this.sw = new Stopwatch();
		//          this.sw2 = new Stopwatch();
		//			this.gclatencymode = GCSettings.LatencyMode;
		//			GCSettings.LatencyMode = GCLatencyMode.Batch;	// 演奏画面中はGCを抑止する
		this.bIsAlreadyCleared = new bool[5];
		this.bIsAlreadyMaxed = new bool[5];

		this.ListDan_Number = 0;
		this.IsDanFailed = false;

		this.objHandlers = new Dictionary<CChip, CCounter>();

		this.t背景テクスチャの生成();

		this.nCurrentTopChip = new int[] { -1, -1, -1, -1, -1 }; // reset for new chart
		this.t数値の初期化(true, true);
	}

	private void CalculateGen4ShinUchiScoreParameters() {
		List<CChip>[] balloonChips = new List<CChip>[5];

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			this.nNoteCount[i] = 0;
			this.nBalloonHitCount[i] = 0;
			this.nRollTimeMs[i] = 0;
			this.nAddScoreGen4ShinUchi[i] = 0;

			if (OpenTaiko.ConfigIni.nPlayerCount >= 2) {
				balloonChips[i] = new();
				for (int j = 0; j < listChip[i].Count; j++) {
					var chip = listChip[i][j];

					if (NotesManager.IsGenericBalloon(chip)) {
						balloonChips[i].Add(chip);
					}
				}
			}
		}

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			CTja _dtx = OpenTaiko.GetTJA(i)!;

			this.scoreMode[i] = (_dtx.nScoreMode >= 0) ? _dtx.nScoreMode : OpenTaiko.ConfigIni.nScoreMode;

			if (OpenTaiko.ConfigIni.nPlayerCount >= 2) {
				for (int j = 0; j < balloonChips[i].Count; j++) {
					var chip = balloonChips[i][j];
					if (NotesManager.IsKusudama(chip)) {
						for (int p = 0; p < OpenTaiko.ConfigIni.nPlayerCount; p++) {
							if (p == i) continue;
							var chip2 = balloonChips[p].Find(x => Math.Abs(x.db発声時刻ms - chip.db発声時刻ms) < 100);

							if (chip2 == null) {
								var chip3 = listChip[p].Find(x => Math.Abs(x.db発声時刻ms - chip.db発声時刻ms) < 100);
								if (!NotesManager.IsKusudama(chip3)) {
									chip.nChannelNo = 0x17;
								}
							} else if (!NotesManager.IsKusudama(chip2)) {
								chip.nChannelNo = 0x17;
							}
						}
					}
				}
			}

			var _list = (_dtx.bチップがある.Branch) ? _dtx.listChip_Branch[2] : _dtx.listChip;
			CountGen4ShinUchiScoreNotes(_list, out this.nNoteCount[i], out this.nBalloonHitCount[i], out this.nRollTimeMs[i]);
			this.nAddScoreGen4ShinUchi[i] = GetAddScoreGen4ShinUchi(this.nNoteCount[i], this.nBalloonHitCount[i], this.nRollTimeMs[i]);
		}
	}

	private void CalculateGen4ShinUchiScoreParameters_Dan() {
		this.nNoteCount_Dan = new int[OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count];
		this.nBalloonHitCount_Dan = new int[OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count];
		this.nRollTimeMs_Dan = new double[OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count];
		this.nAddScoreGen4ShinUchi_Dan = new double[OpenTaiko.stageSongSelect.rChoosenSong.DanSongs.Count];

		CTja tja = OpenTaiko.GetTJA(0)!;
		this.scoreMode[0] = (tja.nScoreMode >= 0) ? tja.nScoreMode : OpenTaiko.ConfigIni.nScoreMode;

		var _list = (tja.bチップがある.Branch) ? tja.listChip_Branch[2] : tja.listChip;
		for (int iNextSongChip = 0, iNextSongChipNext; iNextSongChip >= 0; iNextSongChip = iNextSongChipNext) {
			iNextSongChipNext = _list.FindIndex(iNextSongChip + 1, chip => (chip.nChannelNo == 0x9B));
			CChip nextSongChip = _list[iNextSongChip];
			int iDanSong = nextSongChip.n整数値_内部番号;
			if ((nextSongChip.nChannelNo == 0x9B) && iDanSong >= 0) {
				CountGen4ShinUchiScoreNotes(_list, out this.nNoteCount_Dan[iDanSong], out this.nBalloonHitCount_Dan[iDanSong], out this.nRollTimeMs_Dan[iDanSong], iNextSongChip, iNextSongChipNext);
				this.nAddScoreGen4ShinUchi_Dan[iDanSong] = GetAddScoreGen4ShinUchi(this.nNoteCount_Dan[iDanSong], this.nBalloonHitCount_Dan[iDanSong], this.nRollTimeMs_Dan[iDanSong]);
			}
		}
	}

	private static void CountGen4ShinUchiScoreNotes(List<CChip> listChip, out int nNotes, out int nBalloonHits, out double msRollTime, int startIdx = 0, int endIdx = -1) {
		nNotes = 0;
		nBalloonHits = 0;
		msRollTime = 0;

		if (endIdx < 0)
			endIdx = listChip.Count;
		for (int i = startIdx; i < endIdx; ++i) {
			var _chip = listChip[i];
			if (NotesManager.IsMissableNote(_chip))
				++nNotes;

			if (NotesManager.IsGenericBalloon(_chip)) {
				var msDuration = (_chip.end.n発声時刻ms - _chip.n発声時刻ms);
				var expectedHits = (int)(msDuration / 1000 / 16.6f);
				nBalloonHits += Math.Min(_chip.nBalloon, expectedHits);
			}

			if (NotesManager.IsRoll(_chip) || NotesManager.IsFuzeRoll(_chip))
				msRollTime += (_chip.end.n発声時刻ms - _chip.n発声時刻ms);
		}
	}

	public int GetCeilingGen4ShinUchiScore(int player)
		=> Math.Max(1000000,
			(int)(this.nAddScoreGen4ShinUchi[player] * this.nNoteCount[player])
			+ (int)(this.nBalloonHitCount[player] * 100)
			+ (int)(Math.Ceiling(16.6 * this.nRollTimeMs[player] / 1000 / 10) * 100 * 10));

	public static double GetAddScoreGen4ShinUchi(int nSongNotes, int nSongBalloonHits, double msSongRollTime) {
		if (nSongNotes == 0 && nSongBalloonHits == 0)
			return 1000000;
		return (double)Math.Ceiling((decimal)(
			1000000 - (nSongBalloonHits * 100) - (16.6 * msSongRollTime / 1000 * 100)
		) / nSongNotes / 10) * 10;
	}

	public void ftDanReSetScoreGen4ShinUchi(int iDanSong) {
		this.nAddScoreGen4ShinUchi[0] = this.nAddScoreGen4ShinUchi_Dan[iDanSong];
	}

	public void ftDanReSetBranches(bool hasBranches) {
		this.tBranchReset(0);

		OpenTaiko.stageGameScreen.nDisplayedBranchLane[0] = CTja.ECourse.eNormal;
		OpenTaiko.stageGameScreen.bUseBranch[0] = hasBranches;

		// TJAPlayer3.stage選曲.r確定されたスコア.譜面情報.b譜面分岐[(int)Difficulty.Dan] = hasBranches;
	}


	public override void DeActivate() {
		this.bgmlength = 1;
		this.ctチップ模様アニメ.Drums = null;

		this.ctCamHMove = null;
		this.ctCamVMove = null;
		this.ctCamHScale = null;
		this.ctCamVScale = null;
		this.ctCamRotation = null;
		this.ctCamZoom = null;

		OpenTaiko.borderColor = new Color4(0f, 0f, 0f, 0f);
		OpenTaiko.fCamXOffset = 0.0f;
		OpenTaiko.fCamYOffset = 0.0f;
		OpenTaiko.fCamXScale = 1.0f;
		OpenTaiko.fCamYScale = 1.0f;
		OpenTaiko.fCamRotation = 0.0f;
		OpenTaiko.fCamZoomFactor = 1.0f;

		for (int i = 0; i < 5; i++) {
			ctChipAnime[i] = null;
			ctChipAnimeLag[i] = null;
			OpenTaiko.ConfigIni.nGameType[i] = eFirstGameType[i];
			bSplitLane[i] = false;
			this.msCurrentBarRollProgress[i] = 0;
		}

		this.nowProcessingKusudama = null;

		for (int i = 0; i < this.chip現在処理中の連打チップ.Length; ++i) {
			for (int iChip = this.chip現在処理中の連打チップ[i].Count; iChip-- > 0;)
				this.ProcessRollEnd(i, this.chip現在処理中の連打チップ[i][iChip], false);
		}

		listWAV.Clear();
		listWAV = null;
		listChip = null;
		queueMixerSound.Clear();
		queueMixerSound = null;
		//			GCSettings.LatencyMode = this.gclatencymode;

		this.actAVI.rVD = null; // Will be disposed by TJA.DeActivate() later

		var meanLag = CLagLogger.LogAndReturnMeanLag();

		this.actDan.IsAnimating = false;// IsAnimating=trueのときにそのまま選曲画面に戻ると、文字列が描画されない問題修正用。
		OpenTaiko.tテクスチャの解放(ref this.txBgImage);

		base.DeActivate();
	}
	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		Trace.TraceInformation("CStage演奏画面共通 リソースの開放");
		base.ReleaseManagedResource();
	}

	// その他


	//-----------------
	public class CHITCOUNTOFRANK {
		// Fields
		public int Good;
		public int Great;
		public int Miss;
		public int Perfect;
		public int Poor;

		// Properties
		public int this[int index] {
			get {
				switch (index) {
					case 0:
						return this.Perfect;

					case 1:
						return this.Great;

					case 2:
						return this.Good;

					case 3:
						return this.Poor;

					case 4:
						return this.Miss;
				}
				throw new IndexOutOfRangeException();
			}
			set {
				switch (index) {
					case 0:
						this.Perfect = value;
						return;

					case 1:
						this.Great = value;
						return;

					case 2:
						this.Good = value;
						return;

					case 3:
						this.Poor = value;
						return;

					case 4:
						this.Miss = value;
						return;
				}
				throw new IndexOutOfRangeException();
			}
		}
	}



	protected struct stmixer {
		internal bool bIsAdd;
		internal CSound csound;
		internal bool b演奏終了後も再生が続くチップである;
	};

	/// <summary>
	/// 分岐用のスコアをまとめるクラス。
	/// .2020.04.21.akasoko26
	/// </summary>
	public class CBRANCHSCORE {
		// unused
		public CBRANCHSCORE cBigNotes;//大音符分岐時の情報をまとめるため
		// is reset
		public int nRoll; // with balloon hits, but should exclude them in branch condition for TaikoJiro compatibility
		public int nGreat;
		public int nGood;
		public int nMiss;
		// no reset
		public int nScore;
		public int nADLIB;
		public int nADLIBMiss;
		public int nMine;
		public int nMineAvoid;
		public int nBarRollPass;
		public int nBalloonHitPass;
		public double msBarRollPass;
		// only used for dan-i
		public int nHighestCombo;
		public int nCombo;
	}

	public double[] JPOSCROLLX = new double[5];
	public int GetJPOSCROLLX(int player) {
		double screen_ratio = OpenTaiko.Skin.Resolution[0] / 1280.0;
		return (int)(JPOSCROLLX[player] * screen_ratio);
	}
	public int[] NoteOriginX {
		get {
			if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
				return new int[] {
					OpenTaiko.Skin.nScrollField_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * 0) + GetJPOSCROLLX(0),
					OpenTaiko.Skin.nScrollField_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * 1) + GetJPOSCROLLX(1),
					OpenTaiko.Skin.nScrollField_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * 2) + GetJPOSCROLLX(2),
					OpenTaiko.Skin.nScrollField_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * 3) + GetJPOSCROLLX(3),
					OpenTaiko.Skin.nScrollField_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * 4) + GetJPOSCROLLX(4)
				};
			} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
				return new int[] {
					OpenTaiko.Skin.nScrollField_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * 0) + GetJPOSCROLLX(0),
					OpenTaiko.Skin.nScrollField_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * 1) + GetJPOSCROLLX(1),
					OpenTaiko.Skin.nScrollField_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * 2) + GetJPOSCROLLX(2),
					OpenTaiko.Skin.nScrollField_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * 3) + GetJPOSCROLLX(3)
				};
			} else {
				return new int[] {
					OpenTaiko.Skin.nScrollFieldX[0] + GetJPOSCROLLX(0),
					OpenTaiko.Skin.nScrollFieldX[1] + GetJPOSCROLLX(1)
				};
			}
		}
	}

	public double[] JPOSCROLLY = new double[5];
	public int GetJPOSCROLLY(int player) {
		double screen_ratio = OpenTaiko.Skin.Resolution[1] / 720.0;
		return (int)(JPOSCROLLY[player] * screen_ratio);
	}
	public int[] NoteOriginY {
		get {
			if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
				return new int[] {
					OpenTaiko.Skin.nScrollField_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * 0) + GetJPOSCROLLY(0),
					OpenTaiko.Skin.nScrollField_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * 1) + GetJPOSCROLLY(1),
					OpenTaiko.Skin.nScrollField_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * 2) + GetJPOSCROLLY(2),
					OpenTaiko.Skin.nScrollField_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * 3) + GetJPOSCROLLY(3),
					OpenTaiko.Skin.nScrollField_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * 4) + GetJPOSCROLLY(4)
				};
			} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
				return new int[] {
					OpenTaiko.Skin.nScrollField_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * 0) + GetJPOSCROLLY(0),
					OpenTaiko.Skin.nScrollField_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * 1) + GetJPOSCROLLY(1),
					OpenTaiko.Skin.nScrollField_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * 2) + GetJPOSCROLLY(2),
					OpenTaiko.Skin.nScrollField_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * 3) + GetJPOSCROLLY(3)
				};
			} else {
				return new int[] {
					OpenTaiko.Skin.nScrollFieldY[0] + GetJPOSCROLLY(0),
					OpenTaiko.Skin.nScrollFieldY[1] + GetJPOSCROLLY(1)
				};
			}
		}
	}

	public CAct演奏AVI actAVI;
	public Rainbow Rainbow;
	public CAct演奏Combo共通 actCombo;
	//protected CActFIFOBlack actFI;
	public CActFIFOStart actFI;
	protected CActFIFOBlack actFO;
	protected CActFIFOResult actFOClear;
	public CAct演奏ゲージ共通 actGauge;

	public CActImplDancer actDancer;
	protected CActImplJudgeText actJudgeString;
	public TaikoLaneFlash actTaikoLaneFlash;
	public CAct演奏パネル文字列 actPanel;
	public CAct演奏演奏情報 actPlayInfo;
	public CAct演奏スコア共通 actScore;
	public CAct演奏ステージ失敗 actStageFailed;
	protected CActTaikoScrollSpeed actScrollSpeed;
	protected CActImplRoll actRoll;
	public CActImplBalloon actBalloon;
	public CActImplCharacter actChara;
	protected CActImplRollEffect actRollChara;
	protected CActImplComboBalloon actComboBalloon;
	protected CAct演奏Combo音声 actComboVoice;
	protected CAct演奏PauseMenu actPauseMenu;
	public CActImplChipEffects actChipEffects;
	public CActImplFooter actFooter;
	public CActImplRunner actRunner;
	public CActImplMob actMob;
	public Dan_Cert actDan;
	public AIBattle actAIBattle;
	public CActImplTrainingMode actTokkun;
	public bool bPAUSE;
	public bool[] bIsAlreadyCleared;
	public bool[] bIsAlreadyMaxed;
	protected bool b演奏にMIDI入力を使った;
	protected bool b演奏にキーボードを使った;
	protected bool b演奏にジョイパッドを使った;
	protected bool b演奏にマウスを使った;
	protected STDGBVALUE<CCounter> ctチップ模様アニメ;
	public CCounter[] ctChipAnime;
	public CCounter[] ctChipAnimeLag;
	private int bgmlength = 1;

	protected EGameplayScreenReturnValue eフェードアウト完了時の戻り値;
	protected readonly int[] nチャンネル0Atoパッド08 = new int[] { 1, 2, 3, 4, 5, 7, 6, 1, 8, 0, 9, 9 };
	protected readonly int[] nチャンネル0Atoレーン07 = new int[] { 1, 2, 3, 4, 5, 7, 6, 1, 9, 0, 8, 8 };
	//                         RD LC  LP  RD
	protected readonly int[] nパッド0Atoチャンネル0A = new int[] { 0x11, 0x12, 0x13, 0x14, 0x15, 0x17, 0x16, 0x18, 0x19, 0x1a, 0x1b, 0x1c };
	protected readonly int[] nパッド0Atoパッド08 = new int[] { 1, 2, 3, 4, 5, 6, 7, 1, 8, 0, 9, 9 };// パッド画像のヒット処理用
																							  //   HH SD BD HT LT FT CY HHO RD LC LP LBD
	protected readonly int[] nパッド0Atoレーン07 = new int[] { 1, 2, 3, 4, 5, 6, 7, 1, 9, 0, 8, 8 };
	public STDGBVALUE<CHITCOUNTOFRANK> nHitCount_ExclAuto;
	public STDGBVALUE<CHITCOUNTOFRANK> nHitCount_InclAuto;
	public bool ShowVideo;
	public CBRANCHSCORE[] DanSongScore = [];

	// chip-played state handling
	protected bool isRewinding = false;
	public int[] nCurrentTopChip = new int[] { -1, -1, -1, -1, -1 }; // [iPlayer]; indexes of CTja.listChip
	public static bool hasChipBeenPlayedAt(int chipListIndex, int targetChipListIndex)
		=> chipListIndex < targetChipListIndex;
	public static bool hasChipBeenPlayedAt(CChip chip, double msTargetTjaTime)
		=> chip.n発声時刻ms <= msTargetTjaTime;
	public bool hasChipBeenPlayed(int chipListIndex, int iPlayer)
		=> hasChipBeenPlayedAt(chipListIndex, nCurrentTopChip[iPlayer]);

	protected volatile Queue<stmixer> queueMixerSound;      // #24820 2013.1.21 yyagi まずは単純にAdd/Removeを1個のキューでまとめて管理するやり方で設計する
	protected DateTime dtLastQueueOperation;                //
	protected bool bIsDirectSound;                          //
	protected double dbSongPlaybackSpeed;
	protected bool bValidScore;
	//		protected bool bDTXVmode;
	protected STDGBVALUE<bool> bReverse;

	protected STDGBVALUE<CChip> r現在の歓声Chip;

	protected CTexture txBgImage;

	//		protected int nRisky_InitialVar, nRiskyTime;		// #23559 2011.7.28 yyagi → CAct演奏ゲージ共通クラスに隠蔽
	protected int nPolyphonicSounds;
	protected List<CChip>[] listChip = new List<CChip>[5];
	protected Dictionary<int, CTja.CWAV> listWAV;
	protected bool bUseOSTimer;

	public CBRANCHSCORE[] CBranchScore = new CBRANCHSCORE[6];
	public CBRANCHSCORE[] CChartScore = new CBRANCHSCORE[5];
	public CBRANCHSCORE[] CSectionScore = new CBRANCHSCORE[5];

	public bool[] bIsGOGOTIME = new bool[5];
	private bool[] bWasGOGOTIME = new bool[5]; // go-go time state before rewinding
	public bool[] bIsMiss = new bool[5];
	public bool[] bUseBranch = new bool[5];
	public CTja.ECourse[] nCurrentBranch = new CTja.ECourse[5]; //0:普通譜面 1:玄人譜面 2:達人譜面
	public CTja.ECourse[] nNextBranch = new CTja.ECourse[5];
	public CTja.ECourse[] nDisplayedBranchLane = new CTja.ECourse[5];
	protected bool[] bBranchedChart = new bool[] { false, false, false, false, false };
	protected int[] n分岐した回数 = new int[5];

	public bool[] b強制的に分岐させた = new bool[] { false, false, false, false, false };
	public bool[] bLEVELHOLD = new bool[] { false, false, false, false, false };
	protected int nListCount;

	public bool[] bCurrentlyDrumRoll = new bool[] { false, false, false, false, false }; //奥の手
	protected int[] nCurrentRollCount = new int[5];
	public int[] Chara_MissCount;
	protected ERollState eRollState;
	protected bool[] ifp = { false, false, false, false, false };
	protected bool[] isDeniedPlaying = { false, false, false, false, false };

	protected int nタイマ番号;
	protected int n現在の音符の顔番号;

	protected int nWaitButton;

	protected int[] nStoredHit;
	private EGameType[] eFirstGameType;
	protected bool[] bSplitLane;

	private CChip? nowProcessingKusudama = null;
	public List<CChip>[] chip現在処理中の連打チップ = [[], [], [], [], []]; // [iPlayer][idxNowProcessingRoll]
	public double[] msCurrentBarRollProgress = [0, 0, 0, 0, 0]; // [iPlayer]

	protected const int NOTE_GAP = 25;
	public int nLoopCount_Clear;
	protected int[,] nScore = new int[5, 11]; // [iPlayer, comboLevel]
	protected int[] nHand = new int[5];
	protected CSound[] soundRed = new CSound[5];
	protected CSound[] soundBlue = new CSound[5];
	protected CSound[] soundAdlib = new CSound[5];
	protected CSound[] soundClap = new CSound[5];
	public bool isMultiPlay; // 2016.08.21 kairera0467 表示だけ。
	protected Stopwatch sw;     // 2011.6.13 最適化検討用のストップウォッチ
	public int ListDan_Number;
	private bool IsDanFailed;
	private bool[] b強制分岐譜面 = new bool[5];
	private CTja.EBranchConditionType eBranch種類;
	public double nBranch条件数値A;
	public double nBranch条件数値B;
	protected int nCurrentKusudamaRollCount;
	protected int nCurrentKusudamaCount;

	private float _AIBattleState;
	private Queue<float>[] _AIBattleStateBatch;
	public int AIBattleState {
		get {
			return (int)_AIBattleState;
		}
	}
	public bool bIsAIBattleWin {
		get;
		private set;
	}

	public class AIBattleSection {
		public enum EndType {
			None,
			Clear,
			Lose
		}

		public int Length;
		public int StartTime;
		public int EndTime;

		public EndType End;
		public bool IsAnimated;
	}

	public List<AIBattleSection> AIBattleSections;

	public int NowAIBattleSectionCount;
	public int NowAIBattleSectionTime;
	public AIBattleSection NowAIBattleSection {
		get {
			return AIBattleSections[Math.Min(NowAIBattleSectionCount, AIBattleSections.Count - 1)];
		}
	}

	private void PassAIBattleSection() {
		if (AIBattleState >= 0) {
			NowAIBattleSection.End = AIBattleSection.EndType.Clear;
			if (OpenTaiko.ConfigIni.nAILevel < 10)
				OpenTaiko.ConfigIni.nAILevel++;
		} else {
			NowAIBattleSection.End = AIBattleSection.EndType.Lose;
			if (OpenTaiko.ConfigIni.nAILevel > 1)
				OpenTaiko.ConfigIni.nAILevel--;
		}
		actAIBattle.BatchAnimeCounter.CurrentValue = 0;
		_AIBattleState = 0;

		for (int i = 0; i < 5; i++) {
			this.CSectionScore[i] = new CBRANCHSCORE();
		}

		int clearCount = 0;
		for (int i = 0; i < OpenTaiko.stageGameScreen.AIBattleSections.Count; i++) {
			if (OpenTaiko.stageGameScreen.AIBattleSections[i].End == CStage演奏画面共通.AIBattleSection.EndType.Clear) {
				clearCount++;
			}
		}
		bIsAIBattleWin = clearCount >= OpenTaiko.stageGameScreen.AIBattleSections.Count / 2.0;
	}

	private void AIRegisterInput(int nPlayer, float move) {
		if (nPlayer < 2 && nPlayer >= 0) {
			_AIBattleStateBatch[nPlayer].Enqueue(move);
			while (_AIBattleStateBatch[0].Count > 0 && _AIBattleStateBatch[1].Count > 0) {
				_AIBattleState += _AIBattleStateBatch[0].Dequeue() - _AIBattleStateBatch[1].Dequeue();
				_AIBattleState = Math.Max(Math.Min(_AIBattleState, 9), -9);
			}
		}
	}

	private void UpdateCharaCounter(int nPlayer) {
		for (int i = 0; i < 5; i++) {
			ctChipAnime[i] = new CCounter(0, 3, CTja.TjaDurationToGameDuration(60.0 / OpenTaiko.stageGameScreen.actPlayInfo.dbBPM[i] * 1 / 4), SoundManager.PlayTimer);
		}

		OpenTaiko.stageGameScreen.PuchiChara.ChangeBPM(CTja.TjaDurationToGameDuration(60.0 / OpenTaiko.stageGameScreen.actPlayInfo.dbBPM[nPlayer]));
	}

	public void AddMixer(CSound cs, bool _b演奏終了後も再生が続くチップである) {
		stmixer stm = new stmixer() {
			bIsAdd = true,
			csound = cs,
			b演奏終了後も再生が続くチップである = _b演奏終了後も再生が続くチップである
		};
		queueMixerSound.Enqueue(stm);
		//		Debug.WriteLine( "★Queue: add " + Path.GetFileName( stm.csound.strファイル名 ));
	}
	public void RemoveMixer(CSound cs) {
		stmixer stm = new stmixer() {
			bIsAdd = false,
			csound = cs,
			b演奏終了後も再生が続くチップである = false
		};
		queueMixerSound.Enqueue(stm);
		//		Debug.WriteLine( "★Queue: remove " + Path.GetFileName( stm.csound.strファイル名 ));
	}
	public void ManageMixerQueue() {
		// もしサウンドの登録/削除が必要なら、実行する
		if (queueMixerSound.Count > 0) {
			//Debug.WriteLine( "☆queueLength=" + queueMixerSound.Count );
			DateTime dtnow = DateTime.Now;
			TimeSpan ts = dtnow - dtLastQueueOperation;
			if (ts.Milliseconds > 7) {
				for (int i = 0; i < 2 && queueMixerSound.Count > 0; i++) {
					dtLastQueueOperation = dtnow;
					stmixer stm = queueMixerSound.Dequeue();
					if (stm.bIsAdd) {
						OpenTaiko.SoundManager.AddMixer(stm.csound, dbSongPlaybackSpeed, stm.b演奏終了後も再生が続くチップである);
					} else {
						OpenTaiko.SoundManager.RemoveMixer(stm.csound);
					}
				}
			}
		}
	}



	internal ENoteJudge e指定時刻からChipのJUDGEを返す(long nTime, CChip pChip, int player = 0) {
		var e判定 = e指定時刻からChipのJUDGEを返すImpl(nTime, pChip, player);
		return e判定;
	}

	private bool tEasyTimeZones(int nPlayer) {
		bool _timingzonesAreEasy = false;

		int diff = OpenTaiko.stageSongSelect.nChoosenSongDifficulty[nPlayer];

		// Diff = Normal or Easy
		if (diff <= (int)Difficulty.Normal) {
			_timingzonesAreEasy = true;
		}

		// Diff = Dan and current song is Normal or Easy
		if (diff == (int)Difficulty.Dan) {
			int _nb = OpenTaiko.stageGameScreen.actDan.NowShowingNumber;
			var _danSongs = OpenTaiko.stageSongSelect.rChoosenSong.DanSongs;

			if (_nb < _danSongs.Count) {
				var _currentDiff = _danSongs[_nb].Difficulty;
				if (_currentDiff <= (int)Difficulty.Normal)
					_timingzonesAreEasy = true;

			}
		}

		// Diff = Tower and SIDE is Normal
		if (diff == (int)Difficulty.Tower) {
			_timingzonesAreEasy = OpenTaiko.stageSongSelect.rChoosenSong.nSide == CTja.ESide.eNormal;
		}

		return _timingzonesAreEasy;
	}

	protected CConfigIni.CTimingZones GetTimingZones(int idxPlayerActual) {
		// To change later to adapt to Tower Ama-kuchi
		//diff = Math.Min(diff, (int)Difficulty.Oni);

		int timingShift = OpenTaiko.ConfigIni.nTimingZones[idxPlayerActual];

		bool _timingzonesAreEasy = tEasyTimeZones(idxPlayerActual);

		return (_timingzonesAreEasy == true) ? OpenTaiko.ConfigIni.tzLevels[timingShift] : OpenTaiko.ConfigIni.tzLevels[2 + timingShift];
	}

	private void tIncreaseComboDan(int danSong) {
		this.DanSongScore[danSong].nCombo++;
		if (this.DanSongScore[danSong].nCombo > this.DanSongScore[danSong].nHighestCombo)
			this.DanSongScore[danSong].nHighestCombo = this.DanSongScore[danSong].nCombo;
	}

	private ENoteJudge e指定時刻からChipのJUDGEを返すImpl(long nTime, CChip pChip, int player = 0) {

		if (pChip != null) {
			CTja tja = OpenTaiko.GetTJA(player)!;
			pChip.nLag = (int)(nTime - pChip.n発声時刻ms);
			int nDeltaTime = Math.Abs(pChip.nLag);
			//Debug.WriteLine("nAbsTime=" + (nTime - pChip.n発声時刻ms) + ", nDeltaTime=" + (nTime - pChip.n発声時刻ms));
			if (NotesManager.IsRoll(pChip) || NotesManager.IsFuzeRoll(pChip)) {
				if (tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs) >= pChip.n発声時刻ms && tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs) < pChip.end.n発声時刻ms) {
					return ENoteJudge.Perfect;
				}
			} else if (NotesManager.IsGenericBalloon(pChip)) {
				if (tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs) >= pChip.n発声時刻ms - 17 && tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs) < pChip.end.n発声時刻ms) {
					return ENoteJudge.Perfect;
				}
			}

			int actual = OpenTaiko.GetActualPlayer(player);
			CConfigIni.CTimingZones tz = GetTimingZones(actual);

			if (nDeltaTime <= CTja.GameDurationToTjaDuration(tz.nGoodZone)) {
				return ENoteJudge.Perfect;
			}
			if (nDeltaTime <= CTja.GameDurationToTjaDuration(tz.nOkZone)) {
				if (OpenTaiko.ConfigIni.bJust[actual] == 1 && NotesManager.IsMissableNote(pChip)) // Just
					return ENoteJudge.Poor;
				return ENoteJudge.Good;
			}


			if (nDeltaTime <= CTja.GameDurationToTjaDuration(tz.nBadZone)) {
				if (OpenTaiko.ConfigIni.bJust[actual] == 2 || !NotesManager.IsMissableNote(pChip)) // Safe
					return ENoteJudge.Good;
				return ENoteJudge.Poor;
			}

		}
		return ENoteJudge.Miss;
	}

	protected void tサウンド再生(CChip pChip, int nPlayer) {
		var _gt = OpenTaiko.ConfigIni.nGameType[OpenTaiko.GetActualPlayer(nPlayer)];
		int index = pChip.nChannelNo;

		if (index == 0x11 || index == 0x13 || index == 0x1A || index == 0x101) {
			this.soundRed[pChip.nPlayerSide]?.PlayStart();
			if ((index == 0x13 && _gt == EGameType.Konga) || index == 0x101) {
				this.soundBlue[pChip.nPlayerSide]?.PlayStart();
			}
		} else if (index == 0x12 || index == 0x14 || index == 0x1B) {
			if (index == 0x14 && _gt == EGameType.Konga) {
				this.soundClap[pChip.nPlayerSide]?.PlayStart();
			} else {
				this.soundBlue[pChip.nPlayerSide]?.PlayStart();
			}


		} else if (index == 0x1F) {
			this.soundAdlib[pChip.nPlayerSide]?.PlayStart();
		}

		if (this.nHand[nPlayer] == 0)
			this.nHand[nPlayer]++;
		else
			this.nHand[nPlayer] = 0;
	}

	protected bool tRollProcess(CChip pChip, double dbProcess_time, int num, int sort, int Input, int nPlayer) {
		if (dbProcess_time >= pChip.n発声時刻ms && dbProcess_time < pChip.end.n発声時刻ms) {
			this.bCurrentlyDrumRoll[nPlayer] = true;

			if (pChip.nRollCount == 0) //連打カウントが0の時
			{
				this.actRoll.b表示[nPlayer] = true;
				this.nCurrentRollCount[nPlayer] = 0;
				this.actRoll.t枠表示時間延長(nPlayer, true);
			} else {
				this.actRoll.t枠表示時間延長(nPlayer, false);
			}
			if (this.actRoll.ct連打アニメ[nPlayer].IsUnEnded) {
				this.actRoll.ct連打アニメ[nPlayer] = new CCounter(0, 9, 14, OpenTaiko.Timer);
				this.actRoll.ct連打アニメ[nPlayer].CurrentValue = 1;
			} else {
				this.actRoll.ct連打アニメ[nPlayer] = new CCounter(0, 9, 14, OpenTaiko.Timer);
			}


			pChip.RollEffectLevel += 10;
			if (pChip.RollEffectLevel >= 100) {
				pChip.RollEffectLevel = 100;
				pChip.RollInputTime = new CCounter(0, 1500, 1, OpenTaiko.Timer);
				pChip.RollDelay?.Stop();
			} else {
				pChip.RollInputTime = new CCounter(0, 150, 1, OpenTaiko.Timer);
				pChip.RollDelay?.Stop();
			}

			if (pChip.nChannelNo == 0x15)
				this.eRollState = ERollState.Roll;
			else
				this.eRollState = ERollState.RollB;

			pChip.nRollCount++;

			if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
				this.DanSongScore[actDan.NowShowingNumber].nRoll++;

			this.nCurrentRollCount[nPlayer]++;

			this.CBranchScore[nPlayer].nRoll++;
			this.CChartScore[nPlayer].nRoll++;
			this.CSectionScore[nPlayer].nRoll++;

			if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) this.actRollChara.Start(nPlayer);


			long nAddScore = 0;

			if (!OpenTaiko.ConfigIni.ShinuchiMode) {
				// 旧配点・旧筐体配点
				if (this.scoreMode[nPlayer] == 0 || this.scoreMode[nPlayer] == 1) {
					if (pChip.nChannelNo == 0x15)
						nAddScore = 300L;
					else
						nAddScore = 360L;
				}
				// 新配点
				else {
					if (pChip.nChannelNo == 0x15)
						nAddScore = 100L;
					else
						nAddScore = 200L;
				}
			} else {
				nAddScore = 100L;
			}

			if (!OpenTaiko.ConfigIni.ShinuchiMode && pChip.bGOGOTIME) this.actScore.Add((long)(nAddScore * 1.2f), nPlayer);
			else this.actScore.Add(nAddScore, nPlayer);


			// Refresh scores after roll hits as well
			int __score = (int)(this.actScore.GetScore(nPlayer) + nAddScore);
			this.CBranchScore[nPlayer].nScore = __score;
			this.CChartScore[nPlayer].nScore = __score;
			this.CSectionScore[nPlayer].nScore = __score;

			EGameType _gt = OpenTaiko.ConfigIni.nGameType[OpenTaiko.GetActualPlayer(nPlayer)];

			//赤か青かの分岐
			if (sort == 0 || sort == 2) {
				this.soundRed[pChip.nPlayerSide]?.PlayStart();

				if (pChip.nChannelNo == 0x15 || _gt == EGameType.Konga || (_gt == EGameType.Taiko && pChip.nChannelNo == 0x21)) {
					//CDTXMania.Skin.soundRed.t再生する();
					//CDTXMania.stage演奏ドラム画面.actChipFireTaiko.Start( 1, nPlayer );
					OpenTaiko.stageGameScreen.FlyingNotes.Start(1, nPlayer, true);
				} else {
					//CDTXMania.Skin.soundRed.t再生する();
					//CDTXMania.stage演奏ドラム画面.actChipFireTaiko.Start( 3, nPlayer );
					OpenTaiko.stageGameScreen.FlyingNotes.Start(3, nPlayer, true);
				}
			} else if (sort == 1 || sort == 3) {
				this.soundBlue[pChip.nPlayerSide]?.PlayStart();

				if (pChip.nChannelNo == 0x15 || _gt == EGameType.Konga || (_gt == EGameType.Taiko && pChip.nChannelNo == 0x21)) {
					//CDTXMania.Skin.soundBlue.t再生する();
					//CDTXMania.stage演奏ドラム画面.actChipFireTaiko.Start( 2, nPlayer );
					OpenTaiko.stageGameScreen.FlyingNotes.Start(2, nPlayer, true);
				} else {
					//CDTXMania.Skin.soundBlue.t再生する();
					//CDTXMania.stage演奏ドラム画面.actChipFireTaiko.Start( 4, nPlayer );
					OpenTaiko.stageGameScreen.FlyingNotes.Start(4, nPlayer, true);
				}
			} else if (sort == 4) {
				this.soundClap[pChip.nPlayerSide]?.PlayStart();
				OpenTaiko.stageGameScreen.FlyingNotes.Start(4, nPlayer, true);
			}

			//TJAPlayer3.stage演奏ドラム画面.actTaikoLaneFlash.PlayerLane[nPlayer].Start(PlayerLane.FlashType.Hit);
		} else {
			return true;
		}

		return false;
	}

	protected bool tBalloonProcess(CChip pChip, double dbProcess_time, int player) {
		CTja tja = OpenTaiko.GetTJA(player)!;
		//if( dbProcess_time >= pChip.n発声時刻ms && dbProcess_time < pChip.nノーツ終了時刻ms )
		long nowTime = (long)tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs);
		bool IsKusudama = NotesManager.IsKusudama(pChip);
		bool IsFuze = NotesManager.IsFuzeRoll(pChip);

		int rollCount = pChip.nRollCount;
		int balloon = pChip.nBalloon;


		if (!((int)nowTime < pChip.end.n発声時刻ms)) {
			return false;
		}

		if (IsKusudama) {
			rollCount = pChip.nRollCount = ++nCurrentKusudamaRollCount;
			balloon = nCurrentKusudamaCount;
			if (nCurrentKusudamaCount > 0) {
				actChara.ChangeAnime(player, CActImplCharacter.Anime.Kusudama_Breaking, true);
				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					if (this.actBalloon.ct風船アニメ[i].IsUnEnded) {
						this.actBalloon.ct風船アニメ[i] = new CCounter(0, 9, 14, OpenTaiko.Timer);
						this.actBalloon.ct風船アニメ[i].CurrentValue = 1;
					} else {
						this.actBalloon.ct風船アニメ[i] = new CCounter(0, 9, 14, OpenTaiko.Timer);
					}
				}
			}
		} else {
			this.bCurrentlyDrumRoll[player] = true;
			this.actChara.b風船連打中[player] = true;
			actChara.ChangeAnime(player, CActImplCharacter.Anime.Balloon_Breaking, true);


			if (this.actBalloon.ct風船アニメ[player].IsUnEnded) {
				this.actBalloon.ct風船アニメ[player] = new CCounter(0, 9, 14, OpenTaiko.Timer);
				this.actBalloon.ct風船アニメ[player].CurrentValue = 1;
			} else {
				this.actBalloon.ct風船アニメ[player] = new CCounter(0, 9, 14, OpenTaiko.Timer);
			}
		}

		this.eRollState = ERollState.Balloon;



		if (!IsKusudama) {
			rollCount = ++pChip.nRollCount;
		}

		if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
			this.DanSongScore[actDan.NowShowingNumber].nRoll++;
		this.CBranchScore[player].nRoll++;
		this.CChartScore[player].nRoll++; //  成績発表の連打数に風船を含めるように (AioiLight)
		this.CSectionScore[player].nRoll++;

		//分岐のための処理。実装してない。

		//赤か青かの分岐

		long nAddScore = 0;

		if (!OpenTaiko.ConfigIni.ShinuchiMode) {
			if (pChip.bGOGOTIME) {
				if (balloon == rollCount)
					nAddScore = 6000L;
				else
					nAddScore = 360L;
			} else {
				if (balloon == rollCount)
					nAddScore = 5000L;
				else
					nAddScore = 300L;
			}
		} else {
			nAddScore = 100L;
		}

		this.actScore.Add(nAddScore, player);

		// Refresh scores after roll hits as well
		int __score = (int)(this.actScore.GetScore(player) + nAddScore);
		this.CBranchScore[player].nScore = __score;
		this.CChartScore[player].nScore = __score;
		this.CSectionScore[player].nScore = __score;

		this.soundRed[pChip.nPlayerSide]?.PlayStart();


		if (balloon - rollCount <= 0) {
			this.ProcessBalloonBroke(player, pChip);
		}
		return true;
	}

	protected abstract ENoteJudge tチップのヒット処理(long nHitTime, CChip pChip, bool bCorrectLane);

	protected ENoteJudge tチップのヒット処理(long nHitTime, CChip pChip, EInstrumentPad screenmode, bool bCorrectLane, int nNowInput) {
		return tチップのヒット処理(nHitTime, pChip, screenmode, bCorrectLane, nNowInput, 0);
	}
	protected unsafe ENoteJudge tチップのヒット処理(long nHitTime, CChip pChip, EInstrumentPad screenmode, bool bCorrectLane, int nNowInput, int nPlayer, bool rollEffectHit = false) {
		//unsafeコードにつき、デバッグ中の変更厳禁!

		CTja tja = OpenTaiko.GetTJA(nPlayer)!;
		bool bAutoPlay = OpenTaiko.ConfigIni.bAutoPlay[nPlayer];
		bool bBombHit = false;

		switch (nPlayer) {
			case 1:
				bAutoPlay = OpenTaiko.ConfigIni.bAutoPlay[nPlayer] || OpenTaiko.ConfigIni.bAIBattleMode;
				break;
		}

		if (!pChip.bVisible)
			return ENoteJudge.Auto;

		if (!NotesManager.IsGenericRoll(pChip)) {
			if (pChip.IsHitted || (pChip.IsMissed && (pChip.eNoteState == ENoteState.Bad)))
				return ENoteJudge.Auto; // no repeated judgements
			if (!pChip.IsMissed)//通り越したチップでなければ判定！
			{
				pChip.bHit = true;
				pChip.IsHitted = true;
			}
		}

		ENoteJudge eJudgeResult = ENoteJudge.Auto;
		{
			//連打が短すぎると発声されない
			eJudgeResult = (bCorrectLane && !pChip.IsMissed) ? this.e指定時刻からChipのJUDGEを返す(nHitTime, pChip, nPlayer) : ENoteJudge.Miss;
			// for hit-type notes, check pChip.IsMissed instead to avoid repeated miss judgements

			// AI judges
			eJudgeResult = AlterJudgement(nPlayer, eJudgeResult, true);

			if (!bAutoPlay && eJudgeResult != ENoteJudge.Miss) {
				CLagLogger.Add(nPlayer, pChip);
			}

			var puchichara = OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(OpenTaiko.GetActualPlayer(nPlayer))];

			if (NotesManager.IsRoll(pChip)) {
				#region[ Drumroll ]
				//---------------------------
				if (bAutoPlay || rollEffectHit) {
					int rollSpeed = bAutoPlay ? OpenTaiko.ConfigIni.nRollsPerSec : puchichara.effect.Autoroll;
					if (OpenTaiko.ConfigIni.bAIBattleMode && nPlayer == 1)
						rollSpeed = OpenTaiko.ConfigIni.apAIPerformances[OpenTaiko.ConfigIni.nAILevel - 1].nRollSpeed;

					if (this.bPAUSE == false && rollSpeed > 0) // && TJAPlayer3.ConfigIni.bAuto先生の連打)
					{
						double msPerRollTja = CTja.GameDurationToTjaDuration(1000.0 / rollSpeed);
						if (tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs)
							> (pChip.n発声時刻ms + msPerRollTja * pChip.nRollCount)) {
							EGameType _gt = OpenTaiko.ConfigIni.nGameType[OpenTaiko.GetActualPlayer(nPlayer)];
							int nLane = 0;

							if (this.nHand[nPlayer] == 0)
								this.nHand[nPlayer]++;
							else
								this.nHand[nPlayer] = 0;

							OpenTaiko.stageGameScreen.actTaikoLaneFlash.PlayerLane[nPlayer].Start(PlayerLane.FlashType.Red);
							//CDTXMania.stage演奏ドラム画面.actChipFireTaiko.Start( pChip.nチャンネル番号 == 0x15 ? 1 : 3, nPlayer );
							OpenTaiko.stageGameScreen.FlyingNotes.Start(pChip.nChannelNo == 0x15 ? 1 : 3, nPlayer, true);
							OpenTaiko.stageGameScreen.actMtaiko.tMtaikoEvent(pChip.nChannelNo, this.nHand[nPlayer], nPlayer);


							if (pChip.nChannelNo == 0x20 && _gt == EGameType.Konga) nLane = 4;
							else if (pChip.nChannelNo == 0x21 && _gt == EGameType.Konga) nLane = 1;

							this.tRollProcess(pChip, tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs), 1, nLane, 0, nPlayer);
						}
					}
				}
				if (!bAutoPlay && !rollEffectHit) {
					this.eRollState = ERollState.Roll;
					this.tRollProcess(pChip, tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs), 1, nNowInput, 0, nPlayer);
				}
				//---------------------------
				#endregion
			} else if (NotesManager.IsGenericBalloon(pChip)) {
				#region [ Balloon ]

				bool IsKusudama = NotesManager.IsKusudama(pChip);

				if (!pChip.bProcessed) { // hit during pre-note window
					this.AddNowProcessingRollChip(nPlayer, pChip);
				}


				if (bAutoPlay || rollEffectHit) {

					int rollCount = pChip.nRollCount;
					int balloon = pChip.nBalloon;
					if (IsKusudama) {
						/*
						var ts = pChip.db発声時刻ms;
						var km = TJAPlayer3.DTX.kusudaMAP;

						if (km.ContainsKey(ts))
						{
							rollCount = km[ts].nRollCount;
							balloon = km[ts].nBalloon;
						}
						*/
						rollCount = nCurrentKusudamaRollCount;
						balloon = nCurrentKusudamaCount;

					}

					if (balloon != 0 && this.bPAUSE == false) {
						int rollSpeed = bAutoPlay ? balloon : puchichara.effect.Autoroll;

						int balloonDuration = bAutoPlay ? (pChip.end.n発声時刻ms - pChip.n発声時刻ms) : 1000;

						if (tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs) >
							(pChip.n発声時刻ms + (balloonDuration / (double)rollSpeed) * rollCount)) {
							if (this.nHand[nPlayer] == 0)
								this.nHand[nPlayer]++;
							else
								this.nHand[nPlayer] = 0;

							OpenTaiko.stageGameScreen.actTaikoLaneFlash.PlayerLane[nPlayer].Start(PlayerLane.FlashType.Red);
							OpenTaiko.stageGameScreen.actMtaiko.tMtaikoEvent(pChip.nChannelNo, this.nHand[nPlayer], nPlayer);

							this.tBalloonProcess(pChip, tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs), nPlayer);
						}
					}
				}
				if (!bAutoPlay && !rollEffectHit) {
					if (!IsKusudama || nCurrentKusudamaCount > 0) {
						this.tBalloonProcess(pChip, tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs), nPlayer);
					}
				}
				#endregion
			} else if (NotesManager.IsRollEnd(pChip)) {
				/* do nothing */
			} else if (NotesManager.IsADLIB(pChip)) {
				if (eJudgeResult != ENoteJudge.Auto && eJudgeResult != ENoteJudge.Miss) {
					this.actJudgeString.Start(nPlayer, eJudgeResult != ENoteJudge.Bad ? ENoteJudge.ADLIB : ENoteJudge.Bad);
					eJudgeResult = ENoteJudge.Perfect; // Prevent ADLIB notes breaking DFC runs
					OpenTaiko.stageGameScreen.actLaneTaiko.Start(0x11, eJudgeResult, true, nPlayer);
					OpenTaiko.stageGameScreen.actChipFireD.Start(0x11, eJudgeResult, nPlayer);
					this.CChartScore[nPlayer].nADLIB++;
					this.CSectionScore[nPlayer].nADLIB++;
					this.CBranchScore[nPlayer].nADLIB++;
					if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
						this.DanSongScore[actDan.NowShowingNumber].nADLIB++;
				} else if (pChip.IsMissed) {
					this.CChartScore[nPlayer].nADLIBMiss++;
					this.CSectionScore[nPlayer].nADLIBMiss++;
					this.CBranchScore[nPlayer].nADLIBMiss++;
					if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
						this.DanSongScore[actDan.NowShowingNumber].nADLIBMiss++;
				}
			} else if (NotesManager.IsMine(pChip)) {
				if (eJudgeResult != ENoteJudge.Auto && eJudgeResult != ENoteJudge.Miss) {
					this.actJudgeString.Start(nPlayer, eJudgeResult != ENoteJudge.Bad ? ENoteJudge.Mine : ENoteJudge.Bad);
					bBombHit = true;
					eJudgeResult = ENoteJudge.Bad;
					OpenTaiko.stageGameScreen.actLaneTaiko.Start(0x11, eJudgeResult, true, nPlayer);
					OpenTaiko.stageGameScreen.actChipFireD.Start(0x11, ENoteJudge.Mine, nPlayer);
					OpenTaiko.Skin.soundBomb?.tPlay();
					actGauge.MineDamage(nPlayer);
					this.CChartScore[nPlayer].nMine++;
					this.CSectionScore[nPlayer].nMine++;
					this.CBranchScore[nPlayer].nMine++;
					if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
						this.DanSongScore[actDan.NowShowingNumber].nMine++;
				} else if (pChip.IsMissed) {
					this.CChartScore[nPlayer].nMineAvoid++;
					this.CSectionScore[nPlayer].nMineAvoid++;
					this.CBranchScore[nPlayer].nMineAvoid++;
					if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
						this.DanSongScore[actDan.NowShowingNumber].nMineAvoid++;
				}
			} else {
				if (eJudgeResult != ENoteJudge.Miss) {
					pChip.bShow = false;
				}
				if (eJudgeResult != ENoteJudge.Auto && eJudgeResult != ENoteJudge.Miss) {

					this.actJudgeString.Start(nPlayer, (bAutoPlay && !OpenTaiko.ConfigIni.bAIBattleMode) ? ENoteJudge.Auto : eJudgeResult);
					OpenTaiko.stageGameScreen.actLaneTaiko.Start(pChip.nChannelNo, eJudgeResult, true, nPlayer);
					OpenTaiko.stageGameScreen.actChipFireD.Start(pChip.nChannelNo, eJudgeResult, nPlayer);
				}
			}



		}


		if (NotesManager.IsMissableNote(pChip)) {
			actGauge.Damage(screenmode, eJudgeResult, nPlayer);
		}






		var chara = OpenTaiko.Tx.Characters[OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(nPlayer)].data.Character];
		bool cleared = HGaugeMethods.UNSAFE_FastNormaCheck(nPlayer);

		if (eJudgeResult != ENoteJudge.Poor && eJudgeResult != ENoteJudge.Miss) {
			double dbUnit = (((60.0 / (OpenTaiko.stageGameScreen.actPlayInfo.dbBPM[nPlayer]))));

			// ランナー(たたけたやつ)
			this.actRunner.Start(nPlayer, false, pChip);

			int Character = this.actChara.iCurrentCharacter[nPlayer];

			if (HGaugeMethods.UNSAFE_IsRainbow(nPlayer) && this.bIsAlreadyMaxed[nPlayer] == false) {
				if (OpenTaiko.Skin.Characters_Become_Maxed_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded) {
					this.actChara.ChangeAnime(nPlayer, CActImplCharacter.Anime.Become_Maxed, true);
				}
				this.bIsAlreadyMaxed[nPlayer] = true;
			}
			if (cleared && this.bIsAlreadyCleared[nPlayer] == false) {
				if (OpenTaiko.Skin.Characters_Become_Cleared_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded) {
					this.actChara.ChangeAnime(nPlayer, CActImplCharacter.Anime.Become_Cleared, true);
				}
				this.bIsAlreadyCleared[nPlayer] = true;
				OpenTaiko.stageGameScreen.actBackground.ClearIn(nPlayer);
			}
		}

		if (eJudgeResult == ENoteJudge.Poor || pChip.IsMissed || eJudgeResult == ENoteJudge.Bad) {
			int Character = this.actChara.iCurrentCharacter[nPlayer];

			// ランナー(みすったやつ)
			this.actRunner.Start(nPlayer, true, pChip);
			if (!HGaugeMethods.UNSAFE_IsRainbow(nPlayer) && this.bIsAlreadyMaxed[nPlayer] == true) {
				this.bIsAlreadyMaxed[nPlayer] = false;
				if (OpenTaiko.Skin.Characters_SoulOut_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded) {
					this.actChara.ChangeAnime(nPlayer, CActImplCharacter.Anime.SoulOut, true);
				}
			} else if (!bIsGOGOTIME[nPlayer]) {
				if (Chara_MissCount[nPlayer] == 1 - 1) {
					if (OpenTaiko.Skin.Characters_MissIn_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded) {
						this.actChara.ChangeAnime(nPlayer, CActImplCharacter.Anime.MissIn, true);
					}
				} else if (Chara_MissCount[nPlayer] == 6 - 1) {
					if (OpenTaiko.Skin.Characters_MissDownIn_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded) {
						this.actChara.ChangeAnime(nPlayer, CActImplCharacter.Anime.MissDownIn, true);
					}
				}
			}
			if (!cleared && this.bIsAlreadyCleared[nPlayer] == true) {
				this.bIsAlreadyCleared[nPlayer] = false;
				if (OpenTaiko.Skin.Characters_ClearOut_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded) {
					this.actChara.ChangeAnime(nPlayer, CActImplCharacter.Anime.ClearOut, true);
				}
				OpenTaiko.stageGameScreen.actBackground.ClearOut(nPlayer);

				switch (chara.effect.tGetGaugeType()) {
					case "Hard":
					case "Extreme": {
							ifp[nPlayer] = true;
							isDeniedPlaying[nPlayer] = true; // Prevents the player to ever be able to hit the drum, without freezing the whole game

							bool allDeniedPlaying = true;
							for (int p = 0; p < OpenTaiko.ConfigIni.nPlayerCount; p++) {
								if (!isDeniedPlaying[p]) {
									allDeniedPlaying = false;
									break;
								}
							}
							if (allDeniedPlaying) {
								for (int p = 0; p < OpenTaiko.ConfigIni.nPlayerCount; p++) {
									OpenTaiko.GetTJA(p)!.tStopAllChips(); // Stop playing song
								}
							}

							// Stop timer : Pauses the whole game (to remove once is denied playing will work)
							//CSound管理.rc演奏用タイマ.t一時停止();
						}
						break;
				}
			}
		}

		void returnChara() {
			int Character = this.actChara.iCurrentCharacter[nPlayer];

			double dbUnit = (((60.0 / (OpenTaiko.stageGameScreen.actPlayInfo.dbBPM[nPlayer]))));
			dbUnit = (((60.0 / pChip.dbBPM)));

			if (OpenTaiko.Skin.Characters_Return_Ptn[Character] != 0 && !bIsGOGOTIME[nPlayer] && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded) {
				{
					// 魂ゲージMAXではない
					// ジャンプ_ノーマル
					this.actChara.ChangeAnime(nPlayer, CActImplCharacter.Anime.Return, true);
					//this.actChara.キャラクター_アクション_10コンボ();
				}
			}
		}


		switch (eJudgeResult) {
			case ENoteJudge.Perfect: {
					if (NotesManager.IsGenericRoll(pChip) || NotesManager.IsADLIB(pChip))
						break;

					this.CBranchScore[nPlayer].nGreat++;
					this.CChartScore[nPlayer].nGreat++;
					this.CSectionScore[nPlayer].nGreat++;
					this.Chara_MissCount[nPlayer] = 0;

					if (nPlayer == 0)
						(!bAutoPlay ? this.nHitCount_ExclAuto : this.nHitCount_InclAuto).Drums.Perfect++;
					this.actCombo.nCurrentCombo[nPlayer]++;

					if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
						this.DanSongScore[actDan.NowShowingNumber].nGreat++;
						this.tIncreaseComboDan(actDan.NowShowingNumber);
					}

					if (this.actCombo.ctComboAddCounter[nPlayer].IsUnEnded) {
						this.actCombo.ctComboAddCounter[nPlayer].CurrentValue = 1;
					} else {
						this.actCombo.ctComboAddCounter[nPlayer].CurrentValue = 0;
					}

					AIRegisterInput(nPlayer, 1);

					OpenTaiko.stageGameScreen.actMtaiko.BackSymbolEvent(nPlayer);


					if (this.bIsMiss[nPlayer]) {
						returnChara();
					}

					this.bIsMiss[nPlayer] = false;
				}
				break;
			case ENoteJudge.Great:
			case ENoteJudge.Good: {
					if (NotesManager.IsGenericRoll(pChip))
						break;

					this.CBranchScore[nPlayer].nGood++;
					this.CChartScore[nPlayer].nGood++;
					this.CSectionScore[nPlayer].nGood++;
					this.Chara_MissCount[nPlayer] = 0;

					if (nPlayer == 0)
						(!bAutoPlay ? this.nHitCount_ExclAuto : this.nHitCount_InclAuto).Drums.Great++;
					this.actCombo.nCurrentCombo[nPlayer]++;

					if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
						this.DanSongScore[actDan.NowShowingNumber].nGood++;
						this.tIncreaseComboDan(actDan.NowShowingNumber);
					}

					if (this.actCombo.ctComboAddCounter[nPlayer].IsUnEnded) {
						this.actCombo.ctComboAddCounter[nPlayer].CurrentValue = 1;
					} else {
						this.actCombo.ctComboAddCounter[nPlayer].CurrentValue = 0;
					}

					AIRegisterInput(nPlayer, 0.5f);

					OpenTaiko.stageGameScreen.actMtaiko.BackSymbolEvent(nPlayer);

					if (this.bIsMiss[nPlayer]) {
						returnChara();
					}

					this.bIsMiss[nPlayer] = false;
				}
				break;
			case ENoteJudge.Miss:
				if (pChip.IsMissed)
					goto case ENoteJudge.Poor;
				break;
			case ENoteJudge.Poor:
			case ENoteJudge.Bad: {
					if (NotesManager.IsGenericRoll(pChip) || !(NotesManager.IsMissableNote(pChip) || bBombHit))
						break;

					if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
						CFloorManagement.damage();

					if (!bBombHit) {
						if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
							this.DanSongScore[actDan.NowShowingNumber].nMiss++;

						this.CBranchScore[nPlayer].nMiss++;
						this.CChartScore[nPlayer].nMiss++;
						this.CSectionScore[nPlayer].nMiss++;
						this.Chara_MissCount[nPlayer]++;

						if (nPlayer == 0)
							(!bAutoPlay ? this.nHitCount_ExclAuto : this.nHitCount_InclAuto).Drums.Miss++;
					}

					this.actCombo.nCurrentCombo[nPlayer] = 0;
					if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
						this.DanSongScore[actDan.NowShowingNumber].nCombo = 0;
					this.actComboVoice.tReset(nPlayer);

					AIRegisterInput(nPlayer, 0f);

					this.bIsMiss[nPlayer] = true;
				}
				break;
			default:
				this.nHitCount_InclAuto.Drums[(int)eJudgeResult]++;
				break;
		}
		actDan.Update();

		#region[ Combo voice ]

		if (!NotesManager.IsGenericRoll(pChip)) {
			if ((this.actCombo.nCurrentCombo[nPlayer] % 100 == 0 || this.actCombo.nCurrentCombo[nPlayer] == 50) && this.actCombo.nCurrentCombo[nPlayer] > 0) {
				this.actComboBalloon.Start(this.actCombo.nCurrentCombo[nPlayer], nPlayer);
			}

			// Combo voice here
			this.actComboVoice.tPlay(this.actCombo.nCurrentCombo[nPlayer], nPlayer);

			double dbUnit = (((60.0 / (OpenTaiko.stageGameScreen.actPlayInfo.dbBPM[nPlayer]))));
			dbUnit = (((60.0 / pChip.dbBPM)));

			//CDTXMania.act文字コンソール.tPrint(620, 80, C文字コンソール.Eフォント種別.白, "BPM: " + dbUnit.ToString());

			for (int i = 0; i < 5; i++) {
				if (this.actCombo.nCurrentCombo[i] == 50 || this.actCombo.nCurrentCombo[i] == 300) {
					ctChipAnimeLag[i] = new CCounter(0, 664, 1, OpenTaiko.Timer);
				}
			}

			if (this.actCombo.nCurrentCombo[nPlayer] % 10 == 0 && this.actCombo.nCurrentCombo[nPlayer] > 0) {
				//if (this.actChara.bキャラクターアクション中 == false)
				//{
				int Character = this.actChara.iCurrentCharacter[nPlayer];
				// Edit character values here
				if (!pChip.bGOGOTIME) //2018.03.11 kairera0467 チップに埋め込んだフラグから読み取る
				{
					if (OpenTaiko.Skin.Characters_10Combo_Ptn[Character] != 0 && this.actChara.eNowAnime[nPlayer] != CActImplCharacter.Anime.Combo10 && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded) {
						if (!HGaugeMethods.UNSAFE_IsRainbow(nPlayer)) {
							// 魂ゲージMAXではない
							// ジャンプ_ノーマル
							this.actChara.ChangeAnime(nPlayer, CActImplCharacter.Anime.Combo10, true);
						}
					}
					if (OpenTaiko.Skin.Characters_10Combo_Maxed_Ptn[Character] != 0 && this.actChara.eNowAnime[nPlayer] != CActImplCharacter.Anime.Combo10_Max && actChara.CharaAction_Balloon_Delay[nPlayer].IsEnded) {
						if (HGaugeMethods.UNSAFE_IsRainbow(nPlayer)) {
							// 魂ゲージMAX
							// ジャンプ_MAX
							this.actChara.ChangeAnime(nPlayer, CActImplCharacter.Anime.Combo10_Max, true);
						}
					}
				}


			}

			this.t紙吹雪_開始();
		}
		#endregion




		if ((eJudgeResult != ENoteJudge.Miss) && (eJudgeResult != ENoteJudge.Bad) && (eJudgeResult != ENoteJudge.Poor) && (NotesManager.IsMissableNote(pChip))) {
			int nCombos = this.actCombo.nCurrentCombo[nPlayer];
			long nInit = OpenTaiko.TJA.nScoreInit[0, OpenTaiko.stageSongSelect.nChoosenSongDifficulty[nPlayer]];
			long nDiff = OpenTaiko.TJA.nScoreDiff[OpenTaiko.stageSongSelect.nChoosenSongDifficulty[nPlayer]];
			long nAddScore = 0;

			if (OpenTaiko.ConfigIni.ShinuchiMode)  //2016.07.04 kairera0467 真打モード。
			{
				nAddScore = (long)nAddScoreGen4ShinUchi[nPlayer];

				if (eJudgeResult == ENoteJudge.Great || eJudgeResult == ENoteJudge.Good) {
					nAddScore = (long)nAddScoreGen4ShinUchi[nPlayer] / 20;
					nAddScore = (long)nAddScore * 10;
				}

				this.actScore.Add((long)nAddScore, nPlayer);
			} else if (this.scoreMode[nPlayer] == 2) {
				if (nCombos < 10) {
					nAddScore = this.nScore[nPlayer, 0];
				} else if (nCombos >= 10 && nCombos <= 29) {
					nAddScore = this.nScore[nPlayer, 1];
				} else if (nCombos >= 30 && nCombos <= 49) {
					nAddScore = this.nScore[nPlayer, 2];
				} else if (nCombos >= 50 && nCombos <= 99) {
					nAddScore = this.nScore[nPlayer, 3];
				} else if (nCombos >= 100) {
					nAddScore = this.nScore[nPlayer, 4];
				}

				if (eJudgeResult == ENoteJudge.Great || eJudgeResult == ENoteJudge.Good) {
					nAddScore = nAddScore / 2;
				}

				if (pChip.bGOGOTIME) //2018.03.11 kairera0467 チップに埋め込んだフラグから読み取る
				{
					nAddScore = (int)(nAddScore * 1.2f);
				}

				//100コンボ毎のボーナス
				if (nCombos % 100 == 0 && nCombos > 99) {
					if (this.actScore.ctBonusAddTimer[nPlayer].IsTicked) {
						this.actScore.ctBonusAddTimer[nPlayer].Stop();
						this.actScore.BonusAdd(nPlayer);
					}
					this.actScore.ctBonusAddTimer[nPlayer].CurrentValue = 0;
					this.actScore.ctBonusAddTimer[nPlayer] = new CCounter(0, 2, 1000, OpenTaiko.Timer);
				}

				nAddScore = (int)(nAddScore / 10);
				nAddScore = (int)(nAddScore * 10);

				//大音符のボーナス
				if (pChip.nChannelNo == 0x13 || pChip.nChannelNo == 0x14 || pChip.nChannelNo == 0x1A || pChip.nChannelNo == 0x1B) {
					nAddScore = nAddScore * 2;
				}

				this.actScore.Add(nAddScore, nPlayer);
			} else if (this.scoreMode[nPlayer] == 1) {
				if (nCombos < 10) {
					nAddScore = this.nScore[nPlayer, 0];
				} else if (nCombos >= 10 && nCombos <= 19) {
					nAddScore = this.nScore[nPlayer, 1];
				} else if (nCombos >= 20 && nCombos <= 29) {
					nAddScore = this.nScore[nPlayer, 2];
				} else if (nCombos >= 30 && nCombos <= 39) {
					nAddScore = this.nScore[nPlayer, 3];
				} else if (nCombos >= 40 && nCombos <= 49) {
					nAddScore = this.nScore[nPlayer, 4];
				} else if (nCombos >= 50 && nCombos <= 59) {
					nAddScore = this.nScore[nPlayer, 5];
				} else if (nCombos >= 60 && nCombos <= 69) {
					nAddScore = this.nScore[nPlayer, 6];
				} else if (nCombos >= 70 && nCombos <= 79) {
					nAddScore = this.nScore[nPlayer, 7];
				} else if (nCombos >= 80 && nCombos <= 89) {
					nAddScore = this.nScore[nPlayer, 8];
				} else if (nCombos >= 90 && nCombos <= 99) {
					nAddScore = this.nScore[nPlayer, 9];
				} else if (nCombos >= 100) {
					nAddScore = this.nScore[nPlayer, 10];
				}

				if (eJudgeResult == ENoteJudge.Great || eJudgeResult == ENoteJudge.Good) {
					nAddScore = nAddScore / 2;
				}

				if (pChip.bGOGOTIME) //2018.03.11 kairera0467 チップに埋め込んだフラグから読み取る
					nAddScore = (int)(nAddScore * 1.2f);

				nAddScore = (int)(nAddScore / 10.0);
				nAddScore = (int)(nAddScore * 10);

				//大音符のボーナス
				if (pChip.nChannelNo == 0x13 || pChip.nChannelNo == 0x14 || pChip.nChannelNo == 0x1A || pChip.nChannelNo == 0x1B) {
					nAddScore = nAddScore * 2;
				}

				this.actScore.Add(nAddScore, nPlayer);
			} else {
				if (eJudgeResult == ENoteJudge.Perfect) {
					if (nCombos < 200) {
						nAddScore = 1000;
					} else {
						nAddScore = 2000;
					}
				} else if (eJudgeResult == ENoteJudge.Great || eJudgeResult == ENoteJudge.Good) {
					if (nCombos < 200) {
						nAddScore = 500;
					} else {
						nAddScore = 1000;
					}
				}

				if (pChip.bGOGOTIME) //2018.03.11 kairera0467 チップに埋め込んだフラグから読み取る
					nAddScore = (int)(nAddScore * 1.2f);


				//大音符のボーナス
				if (pChip.nChannelNo == 0x13 || pChip.nChannelNo == 0x25) {
					nAddScore = nAddScore * 2;
				}

				this.actScore.Add(nAddScore, nPlayer);
			}

			//キーを押したときにスコア情報 + nAddScoreを置き換える様に
			int __score = (int)(this.actScore.GetScore(nPlayer) + nAddScore);
			this.CBranchScore[nPlayer].nScore = __score;
			this.CChartScore[nPlayer].nScore = __score;
			this.CSectionScore[nPlayer].nScore = __score;
		}


		return ENoteJudge.Auto;
	}

	protected abstract void tチップのヒット処理_BadならびにTight時のMiss(CTja.ECourse eCourse, EInstrumentPad part);
	protected abstract void tチップのヒット処理_BadならびにTight時のMiss(CTja.ECourse eCourse, EInstrumentPad part, int nLane);

	protected void tチップのヒット処理_BadならびにTight時のMiss(CTja.ECourse eCourse, EInstrumentPad part, int nLane, EInstrumentPad screenmode) {
		// Something looks wrong with this (Notelock mode)
		actGauge.Damage(screenmode, ENoteJudge.Miss, 0);
		this.actCombo.nCurrentCombo.P1 = 0;
		this.actCombo.nCurrentCombo.P2 = 0;
		this.actCombo.nCurrentCombo.P3 = 0;
		this.actCombo.nCurrentCombo.P4 = 0;
		this.actCombo.nCurrentCombo.P5 = 0;
	}

	protected CChip r指定時刻に一番近い未ヒットChipを過去方向優先で検索する(long nTime, int nPlayer) {
		//sw2.Start();

		int nTimeDiff;
		int count = listChip[nPlayer].Count;
		if (count <= 0)         // 演奏データとして1個もチップがない場合は
		{
			//sw2.Stop();
			return null;
		}

		int nIndex_NearestChip_Future = this.nCurrentTopChip[nPlayer];
		int nIndex_InitialPositionSearchingToPast = nIndex_NearestChip_Future - 1; // exclude past from future
		if (this.nCurrentTopChip[nPlayer] >= count)      // その時点で演奏すべきチップが既に全部無くなっていたら
		{
			nIndex_NearestChip_Future = nIndex_InitialPositionSearchingToPast = count - 1;
		}


		// int nIndex_NearestChip_Future = nIndex_InitialPositionSearchingToFuture;
		//			while ( nIndex_NearestChip_Future < count )	// 未来方向への検索
		for (; nIndex_NearestChip_Future < count; nIndex_NearestChip_Future++) {

			if (nIndex_NearestChip_Future < 0)
				continue;


			CChip chip = listChip[nPlayer][nIndex_NearestChip_Future];
			if (!chip.bHit && chip.bVisible) {
				if (NotesManager.IsHittableNote(chip) && !NotesManager.IsRollEnd(chip)) {
					if (chip.n発声時刻ms > nTime) {
						break;
					}
					nIndex_InitialPositionSearchingToPast = nIndex_NearestChip_Future;
					if (NotesManager.IsGenericRoll(chip) && !NotesManager.IsRollEnd(chip)) {
						if (chip.end.n発声時刻ms > nTime) {
							break;
						}
					}
				}
			}
			//				nIndex_NearestChip_Future++;
		}


		int nIndex_NearestChip_Past = nIndex_InitialPositionSearchingToPast;
		//			while ( nIndex_NearestChip_Past >= 0 )		// 過去方向への検索
		for (; nIndex_NearestChip_Past >= 0; nIndex_NearestChip_Past--) {
			CChip chip = listChip[nPlayer][nIndex_NearestChip_Past];
			//if ( (!chip.bHit && chip.b可視 ) && ( (  0x93 <= chip.nチャンネル番号 ) && ( chip.nチャンネル番号 <= 0x99 ) ) )

			if (chip.bVisible && !NotesManager.IsRollEnd(chip)
				&& (!chip.bHit && NotesManager.IsHittableNote(chip) || chip.bProcessed && NotesManager.IsGenericRoll(chip))
				) {
				break;
			}

			//				nIndex_NearestChip_Past--;
		}
		if ((nIndex_NearestChip_Future >= count) && (nIndex_NearestChip_Past < 0))  // 検索対象が過去未来どちらにも見つからなかった場合
		{
			//sw2.Stop();
			return null;
		}
		CChip nearestChip; // = null;	// 以下のifブロックのいずれかで必ずnearestChipには非nullが代入されるので、null初期化を削除
		if (nIndex_NearestChip_Future >= count)                                         // 検索対象が未来方向には見つからなかった(しかし過去方向には見つかった)場合
		{
			nearestChip = listChip[nPlayer][nIndex_NearestChip_Past];
			//				nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
		} else if (nIndex_NearestChip_Past < 0)                                             // 検索対象が過去方向には見つからなかった(しかし未来方向には見つかった)場合
		{
			nearestChip = listChip[nPlayer][nIndex_NearestChip_Future];
			//				nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
		} else {
			int nTimeDiff_Future = Math.Abs((int)(nTime - listChip[nPlayer][nIndex_NearestChip_Future].n発声時刻ms));
			int nTimeDiff_Past = Math.Abs((int)(nTime - listChip[nPlayer][nIndex_NearestChip_Past].n発声時刻ms));

			if (nTimeDiff_Future < nTimeDiff_Past) {
				if (!listChip[nPlayer][nIndex_NearestChip_Past].bHit
					&& listChip[nPlayer][nIndex_NearestChip_Past].n発声時刻ms + 108 >= nTime
					&& NotesManager.IsMissableNote(listChip[nPlayer][nIndex_NearestChip_Past])
				   ) {
					nearestChip = listChip[nPlayer][nIndex_NearestChip_Past];
				} else
					nearestChip = listChip[nPlayer][nIndex_NearestChip_Future];

				//					nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
			} else {
				nearestChip = listChip[nPlayer][nIndex_NearestChip_Past];
				//					nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
			}

			var __tmpchp = listChip[nPlayer][nIndex_NearestChip_Future];

			//2015.11.5 kairera0467　連打音符の判定
			if (NotesManager.IsGenericRoll(__tmpchp) && !NotesManager.IsRollEnd(__tmpchp)) {
				if (listChip[nPlayer][nIndex_NearestChip_Future].n発声時刻ms <= nTime && listChip[nPlayer][nIndex_NearestChip_Future].end.n発声時刻ms >= nTime) {
					nearestChip = listChip[nPlayer][nIndex_NearestChip_Future];
				}
			}
		}
		nTimeDiff = Math.Abs((int)(nTime - nearestChip.n発声時刻ms));
		int n検索範囲時間ms = 0;
		if ((n検索範囲時間ms > 0) && (nTimeDiff > n検索範囲時間ms))                 // チップは見つかったが、検索範囲時間外だった場合
		{
			//sw2.Stop();
			return null;
		}
		//sw2.Stop();
		return nearestChip;
	}

	/// <summary>
	/// 最も判定枠に近いドンカツを返します。
	/// </summary>
	/// <param name="nowTime">判定時の時間。</param>
	/// <param name="player">プレイヤー。</param>
	/// <param name="don">ドンかどうか。</param>
	/// <returns>最も判定枠に近いノーツ。</returns>
	/*
    protected CDTX.CChip GetChipOfNearest(long nowTime, int player, bool don)
    {
        var nearestChip = new CDTX.CChip();
        var count = listChip[player].Count;
        var chips = listChip[player];
        var startPosision = NowProcessingChip[player];
        CDTX.CChip pastChip; // 判定されるべき過去ノート
        CDTX.CChip futureChip; // 判定されるべき未来ノート
        var pastJudge = E判定.Miss;
        var futureJudge = E判定.Miss;

        bool GetDon(CDTX.CChip note)
        {
            return note.nチャンネル番号 == 0x11 || note.nチャンネル番号 == 0x13 || note.nチャンネル番号 == 0x1A || note.nチャンネル番号 == 0x1F;
        }
        bool GetKatsu(CDTX.CChip note)
        {
            return note.nチャンネル番号 == 0x12 || note.nチャンネル番号 == 0x14 || note.nチャンネル番号 == 0x1B || note.nチャンネル番号 == 0x1F;
        }

        if (count <= 0)
        {
            return null;
        }

        if (startPosision >= count)
        {
            startPosision -= 1;
        }

        #region 過去のノーツで、かつ可判定以上のノーツの決定
        CDTX.CChip afterChip = null;
        for (int pastNote = startPosision - 1; ; pastNote--)
        {
            if (pastNote < 0)
            {
                pastChip = afterChip != null ? afterChip : null; // afterChipに過去の判定があるかもしれないので
                break;
            }
            var processingChip = chips[pastNote];
            if (!processingChip.IsHitted && processingChip.nコース == n現在のコース[player]) // まだ判定されてない音符
            {
                if (don ? GetDon(processingChip) : GetKatsu(processingChip)) // 音符のチャンネルである
                {
                    var thisChipJudge = pastJudge = e指定時刻からChipのJUDGEを返すImpl(nowTime, processingChip, player);
                    if (thisChipJudge != E判定.Miss)
                    {
                        // 判定が見過ごし不可ではない(=たたいて不可以上)
                        // その前のノートがもしかしたら存在して、可以上の判定かもしれないからまだ処理を続行する。
                        afterChip = processingChip;
                        continue;
                    }
                    else
                    {
                        // 判定が不可だった
                        // その前のノーツを過去で可以上のノート(つまり判定されるべきノート)とする。
                        pastChip = afterChip;
                        break; // 検索終わり
                    }
                }
            }
            if (processingChip.IsHitted && processingChip.nコース == n現在のコース[player]) // 連打
            {
                if ((0x15 <= processingChip.nチャンネル番号) && (processingChip.nチャンネル番号 <= 0x17))
                {
                    if (processingChip.nノーツ終了時刻ms > nowTime)
                    {
                        pastChip = processingChip;
                        break;
                    }
                }
            }
        }
        #endregion

        #region 未来のノーツで、かつ可判定以上のノーツの決定
        for (int futureNote = startPosision; ; futureNote++)
        {
            if (futureNote >= count)
            {
                futureChip = null;
                break;
            }
            var processingChip = chips[futureNote];
            if (!processingChip.IsHitted && processingChip.nコース == n現在のコース[player]) // まだ判定されてない音符
            {
                if (don ? GetDon(processingChip) : GetKatsu(processingChip)) // 音符のチャンネルである
                {
                    var thisChipJudge = futureJudge = e指定時刻からChipのJUDGEを返すImpl(nowTime, processingChip, player);
                    if (thisChipJudge != E判定.Miss)
                    {
                        // 判定が見過ごし不可ではない(=たたいて不可以上)
                        // そのノートを処理すべきなので、検索終わり。
                        futureChip = processingChip;
                        break; // 検索終わり
                    }
                    else
                    {
                        // 判定が不可だった
                        // つまり未来に処理すべきノートはないので、検索終わり。
                        futureChip = null; // 今処理中のノート
                        break; // 検索終わり
                    }
                }
            }
        }
        #endregion

        #region 過去のノーツが見つかったらそれを返却、そうでなければ未来のノーツを返却
        if ((pastJudge == E判定.Miss || pastJudge == E判定.Poor) && (futureJudge != E判定.Miss && futureJudge != E判定.Poor))
        {
            // 過去の判定が不可で、未来の判定が可以上なら未来を返却。
            nearestChip = futureChip;
        }
        else if (futureChip == null && pastChip != null)
        {
            // 未来に処理するべきノートがなかったので、過去の処理すべきノートを返す。
            nearestChip = pastChip;
        }
        else if (pastChip == null && futureChip != null)
        {
            // 過去の検索が該当なしだったので、未来のノートを返す。
            nearestChip = futureChip;
        }
        else
        {
            // 基本的には過去のノートを返す。
            nearestChip = pastChip;
        }
        #endregion

        return nearestChip;
    }
    */


	protected CChip r指定時刻に一番近い未ヒットChip(long nTime, int nChannel, int n検索範囲時間ms, int nPlayer) {
		//sw2.Start();
		//Trace.TraceInformation( "nTime={0}, nChannel={1:x2}, 現在のTop={2}", nTime, nChannel,CDTXMania.DTX.listChip[ this.n現在のトップChip ].n発声時刻ms );

		int nTimeDiff;
		if (this.nCurrentTopChip[nPlayer] == -1)         // 演奏データとして1個もチップがない場合は
		{
			//sw2.Stop();
			return null;
		}
		int count = listChip[nPlayer].Count;
		int nIndex_NearestChip_Future = this.nCurrentTopChip[nPlayer];
		int nIndex_InitialPositionSearchingToPast = nIndex_NearestChip_Future - 1; // exclude past from future
		if (this.nCurrentTopChip[nPlayer] >= count)      // その時点で演奏すべきチップが既に全部無くなっていたら
		{
			nIndex_NearestChip_Future = nIndex_InitialPositionSearchingToPast = count - 1;
		}
		// int nIndex_NearestChip_Future = nIndex_InitialPositionSearchingToFuture;
		//			while ( nIndex_NearestChip_Future < count )	// 未来方向への検索
		for (; nIndex_NearestChip_Future < count; nIndex_NearestChip_Future++) {
			CChip chip = listChip[nPlayer][nIndex_NearestChip_Future];
			if (!chip.bHit) {
				if ((0x11 <= nChannel) && (nChannel <= 0x1F)) {
					if ((chip.nChannelNo == nChannel) || (chip.nChannelNo == (nChannel + 0x20))) {
						if (chip.n発声時刻ms > nTime) {
							break;
						}
						nIndex_InitialPositionSearchingToPast = nIndex_NearestChip_Future;
					}
					continue;
				}

				//if ( ( ( 0xDE <= nChannel ) && ( nChannel <= 0xDF ) ) )
				if (((0xDF == nChannel))) {
					if (chip.nChannelNo == nChannel) {
						if (chip.n発声時刻ms > nTime) {
							break;
						}
						nIndex_InitialPositionSearchingToPast = nIndex_NearestChip_Future;
					}
				}

				if (((0x50 == nChannel))) {
					if (chip.nChannelNo == nChannel) {
						if (chip.n発声時刻ms > nTime) {
							break;
						}
						nIndex_InitialPositionSearchingToPast = nIndex_NearestChip_Future;
					}
				}

			}
			//				nIndex_NearestChip_Future++;
		}

		// Channel is always 50, following code is unreachable

		int nIndex_NearestChip_Past = nIndex_InitialPositionSearchingToPast;
		//			while ( nIndex_NearestChip_Past >= 0 )		// 過去方向への検索
		for (; nIndex_NearestChip_Past >= 0; nIndex_NearestChip_Past--) {
			CChip chip = listChip[nPlayer][nIndex_NearestChip_Past];
			if ((!chip.bHit) &&
				(
					(
						((((nChannel >= 0x11) && (nChannel <= 0x14)) || nChannel == 0x1A || nChannel == 0x1B || nChannel == 0x1F) && (chip.nChannelNo == nChannel))
					)
					||
					(
						//	( ( ( nChannel >= 0xDE ) && ( nChannel <= 0xDF ) ) && ( chip.nチャンネル番号 == nChannel ) )
						(((nChannel == 0xDF)) && (chip.nChannelNo == nChannel))
					)
					||
					(
						//	( ( ( nChannel >= 0xDE ) && ( nChannel <= 0xDF ) ) && ( chip.nチャンネル番号 == nChannel ) )
						(((nChannel == 0x50)) && (chip.nChannelNo == nChannel))
					)
				)
			   ) {
				break;
			}
			//				nIndex_NearestChip_Past--;
		}
		if ((nIndex_NearestChip_Future >= count) && (nIndex_NearestChip_Past < 0))  // 検索対象が過去未来どちらにも見つからなかった場合
		{
			//sw2.Stop();
			return null;
		}
		CChip nearestChip; // = null;	// 以下のifブロックのいずれかで必ずnearestChipには非nullが代入されるので、null初期化を削除
		if (nIndex_NearestChip_Future >= count)                                         // 検索対象が未来方向には見つからなかった(しかし過去方向には見つかった)場合
		{
			nearestChip = listChip[nPlayer][nIndex_NearestChip_Past];
			//				nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
		} else if (nIndex_NearestChip_Past < 0)                                             // 検索対象が過去方向には見つからなかった(しかし未来方向には見つかった)場合
		{
			nearestChip = listChip[nPlayer][nIndex_NearestChip_Future];
			//				nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
		} else {
			int nTimeDiff_Future = Math.Abs((int)(nTime - listChip[nPlayer][nIndex_NearestChip_Future].n発声時刻ms));
			int nTimeDiff_Past = Math.Abs((int)(nTime - listChip[nPlayer][nIndex_NearestChip_Past].n発声時刻ms));

			if (nChannel == 0xDF) //0xDFの場合は過去方向への検索をしない
			{
				return listChip[nPlayer][nIndex_NearestChip_Future];
			}

			if (nTimeDiff_Future < nTimeDiff_Past) {
				nearestChip = listChip[nPlayer][nIndex_NearestChip_Future];
				//					nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
			} else {
				nearestChip = listChip[nPlayer][nIndex_NearestChip_Past];
				//					nTimeDiff = Math.Abs( (int) ( nTime - nearestChip.n発声時刻ms ) );
			}
		}
		nTimeDiff = Math.Abs((int)(nTime - nearestChip.n発声時刻ms));
		if ((n検索範囲時間ms > 0) && (nTimeDiff > n検索範囲時間ms))                 // チップは見つかったが、検索範囲時間外だった場合
		{
			//sw2.Stop();
			return null;
		}
		//sw2.Stop();
		return nearestChip;
	}
	public bool r検索範囲内にチップがあるか調べる(long nTime, int n検索範囲時間ms, int nPlayer) {
		for (int i = 0; i < listChip[nPlayer].Count; i++) {
			CChip chip = listChip[nPlayer][i];
			if (!chip.bHit) {
				if (NotesManager.IsMissableNote(chip)) {
					if (chip.n発声時刻ms < nTime + n検索範囲時間ms) {
						if (chip.nBranch == this.nCurrentBranch[nPlayer]) //2016.06.14 kairera0467 譜面分岐も考慮するようにしてみる。
							return true;
					}
				}
			}
		}

		return false;
	}

	protected void ChangeInputAdjustTimeInPlaying(IInputDevice keyboard, int plusminus)     // #23580 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
	{
		int offset;
		if (keyboard.KeyPressing((int)SlimDXKeys.Key.LeftControl) ||
			keyboard.KeyPressing((int)SlimDXKeys.Key.RightControl)) {
			offset = plusminus;
		} else {
			offset = plusminus * 10;
		}

		var newInputAdjustTimeMs = (OpenTaiko.ConfigIni.nInputAdjustTimeMs + offset).Clamp(-99, 99);
		OpenTaiko.ConfigIni.nInputAdjustTimeMs = newInputAdjustTimeMs;
	}

	protected abstract void t入力処理_ドラム();
	protected abstract void ドラムスクロール速度アップ();
	protected abstract void ドラムスクロール速度ダウン();
	protected void tキー入力() {
		// Inputs

		IInputDevice keyboard = OpenTaiko.InputManager.Keyboard;

		if ((!this.bPAUSE && (base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED)) && (base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED_FadeOut)) {
			this.t入力処理_ドラム();

			CTja tja = OpenTaiko.TJA;

			// Individual offset
			if (keyboard.KeyPressed((int)SlimDXKeys.Key.UpArrow) && (keyboard.KeyPressing((int)SlimDXKeys.Key.RightShift) || keyboard.KeyPressing((int)SlimDXKeys.Key.LeftShift))) {    // shift (+ctrl) + UpArrow (BGMAdjust)
				OpenTaiko.TJA.t各自動再生音チップの再生時刻を変更する((keyboard.KeyPressing((int)SlimDXKeys.Key.LeftControl) || keyboard.KeyPressing((int)SlimDXKeys.Key.RightControl)) ? 1 : 10);
				OpenTaiko.TJA.tWave再生位置自動補正();
			} else if (keyboard.KeyPressed((int)SlimDXKeys.Key.DownArrow) && (keyboard.KeyPressing((int)SlimDXKeys.Key.RightShift) || keyboard.KeyPressing((int)SlimDXKeys.Key.LeftShift))) {   // shift + DownArrow (BGMAdjust)
				OpenTaiko.TJA.t各自動再生音チップの再生時刻を変更する((keyboard.KeyPressing((int)SlimDXKeys.Key.LeftControl) || keyboard.KeyPressing((int)SlimDXKeys.Key.RightControl)) ? -1 : -10);
				OpenTaiko.TJA.tWave再生位置自動補正();
			}
			// Tokkun only
			else if (OpenTaiko.ConfigIni.bTokkunMode &&
					 OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingIncreaseScrollSpeed)) {  // UpArrow(scrollspeed up)
				ドラムスクロール速度アップ();
			} else if (OpenTaiko.ConfigIni.bTokkunMode &&
					   OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingDecreaseScrollSpeed)) {  // DownArrow (scrollspeed down)
				ドラムスクロール速度ダウン();
			}
			// Debug mode
			else if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.System.DisplayDebug)) {   // del (debug info)
				OpenTaiko.ConfigIni.bDisplayDebugInfo = !OpenTaiko.ConfigIni.bDisplayDebugInfo;
			}


			/*
			else if ( keyboard.bキーが押された( (int)SlimDXKeys.Key.LeftArrow ) )		// #24243 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
			{
				ChangeInputAdjustTimeInPlaying( keyboard, -1 );
			}
			else if ( keyboard.bキーが押された( (int)SlimDXKeys.Key.RightArrow ) )		// #24243 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
			{
				ChangeInputAdjustTimeInPlaying( keyboard, +1 );
			}
			*/

			else if ((base.ePhaseID == CStage.EPhase.Common_NORMAL) && (keyboard.KeyPressed((int)SlimDXKeys.Key.Escape) || OpenTaiko.Pad.bPressedGB(EPad.FT)) && !this.actPauseMenu.bIsActivePopupMenu) {    // escape (exit)
				if (!this.actPauseMenu.bIsActivePopupMenu && this.bPAUSE == false) {
					OpenTaiko.Skin.soundChangeSFX.tPlay();

					SoundManager.PlayTimer.Pause();
					OpenTaiko.Timer.Pause();
					OpenTaiko.TJA.t全チップの再生一時停止();
					this.actAVI.Pause();

					this.bPAUSE = true;
					this.actPauseMenu.tActivatePopupMenu(0);
				}
				// this.t演奏中止();
			} else if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingBranchNormal) &&
					   (OpenTaiko.ConfigIni.bTokkunMode || OpenTaiko.ConfigIni.bAutoPlay[0])) {
				if (!OpenTaiko.TJA.bHasBranch[OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0]]) return;

				//listBRANCHを廃止したため強制分岐の開始値を
				//rc演奏用タイマ.n現在時刻msから引っ張ることに

				//判定枠に一番近いチップの情報を元に一小節分の値を計算する. 2020.04.21 akasoko26

				var p判定枠に最も近いチップ = r指定時刻に一番近い未ヒットChipを過去方向優先で検索する((long)tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs), 0);
				double db一小節後 = 0.0;
				if (p判定枠に最も近いチップ != null)
					db一小節後 = ((15000.0 / p判定枠に最も近いチップ.dbBPM * (p判定枠に最も近いチップ.fNow_Measure_s / p判定枠に最も近いチップ.fNow_Measure_m)) * 16.0);

				this.t分岐処理(CTja.ECourse.eNormal, 0, tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs) + db一小節後);

				OpenTaiko.stageGameScreen.actLaneTaiko.t分岐レイヤー_コース変化(OpenTaiko.stageGameScreen.actLaneTaiko.stBranch[0].nAfter, CTja.ECourse.eNormal, 0);
				OpenTaiko.stageGameScreen.actMtaiko.tBranchEvent(OpenTaiko.stageGameScreen.actMtaiko.After[0], CTja.ECourse.eNormal, 0);

				this.nCurrentBranch[0] = CTja.ECourse.eNormal;
				this.nNextBranch[0] = CTja.ECourse.eNormal;
				this.nDisplayedBranchLane[0] = CTja.ECourse.eNormal;


				this.b強制的に分岐させた[0] = true;
			} else if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingBranchExpert) &&
					   (OpenTaiko.ConfigIni.bTokkunMode || OpenTaiko.ConfigIni.bAutoPlay[0]))      // #24243 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
			{
				if (!OpenTaiko.TJA.bHasBranch[OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0]]) return;

				//listBRANCHを廃止したため強制分岐の開始値を
				//rc演奏用タイマ.n現在時刻msから引っ張ることに

				//判定枠に一番近いチップの情報を元に一小節分の値を計算する. 2020.04.21 akasoko26
				var p判定枠に最も近いチップ = r指定時刻に一番近い未ヒットChipを過去方向優先で検索する((long)tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs), 0);

				double db一小節後 = 0.0;
				if (p判定枠に最も近いチップ != null)
					db一小節後 = ((15000.0 / p判定枠に最も近いチップ.dbBPM * (p判定枠に最も近いチップ.fNow_Measure_s / p判定枠に最も近いチップ.fNow_Measure_m)) * 16.0);

				this.t分岐処理(CTja.ECourse.eExpert, 0, tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs) + db一小節後);

				OpenTaiko.stageGameScreen.actLaneTaiko.t分岐レイヤー_コース変化(OpenTaiko.stageGameScreen.actLaneTaiko.stBranch[0].nAfter, CTja.ECourse.eExpert, 0);
				OpenTaiko.stageGameScreen.actMtaiko.tBranchEvent(OpenTaiko.stageGameScreen.actMtaiko.After[0], CTja.ECourse.eExpert, 0);


				this.nCurrentBranch[0] = CTja.ECourse.eExpert;
				this.nNextBranch[0] = CTja.ECourse.eExpert;
				this.nDisplayedBranchLane[0] = CTja.ECourse.eExpert;

				this.b強制的に分岐させた[0] = true;
			} else if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingBranchMaster) &&
					   (OpenTaiko.ConfigIni.bTokkunMode || OpenTaiko.ConfigIni.bAutoPlay[0]))      // #24243 2011.1.16 yyagi UI for InputAdjustTime in playing screen.
			{
				if (!OpenTaiko.TJA.bHasBranch[OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0]]) return;

				//listBRANCHを廃止したため強制分岐の開始値を
				//rc演奏用タイマ.n現在時刻msから引っ張ることに

				//判定枠に一番近いチップの情報を元に一小節分の値を計算する. 2020.04.21 akasoko26
				var p判定枠に最も近いチップ = r指定時刻に一番近い未ヒットChipを過去方向優先で検索する((long)tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs), 0);

				double db一小節後 = 0.0;
				if (p判定枠に最も近いチップ != null)
					db一小節後 = ((15000.0 / p判定枠に最も近いチップ.dbBPM * (p判定枠に最も近いチップ.fNow_Measure_s / p判定枠に最も近いチップ.fNow_Measure_m)) * 16.0);

				this.t分岐処理(CTja.ECourse.eMaster, 0, tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs) + db一小節後);

				OpenTaiko.stageGameScreen.actLaneTaiko.t分岐レイヤー_コース変化(OpenTaiko.stageGameScreen.actLaneTaiko.stBranch[0].nAfter, CTja.ECourse.eMaster, 0);
				OpenTaiko.stageGameScreen.actMtaiko.tBranchEvent(OpenTaiko.stageGameScreen.actMtaiko.After[0], CTja.ECourse.eMaster, 0);

				this.nCurrentBranch[0] = CTja.ECourse.eMaster;
				this.nNextBranch[0] = CTja.ECourse.eMaster;
				this.nDisplayedBranchLane[0] = CTja.ECourse.eMaster;

				this.b強制的に分岐させた[0] = true;
			}

			if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.System.DisplayHits)) {
				if (OpenTaiko.ConfigIni.bJudgeCountDisplay == false)
					OpenTaiko.ConfigIni.bJudgeCountDisplay = true;
				else
					OpenTaiko.ConfigIni.bJudgeCountDisplay = false;
			}

			if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.System.CycleVideoDisplayMode)) {
				switch (OpenTaiko.ConfigIni.eClipDispType) {
					case EClipDispType.Off:
						OpenTaiko.ConfigIni.eClipDispType = EClipDispType.BackgroundOnly;
						break;
					case EClipDispType.BackgroundOnly:
						OpenTaiko.ConfigIni.eClipDispType = EClipDispType.WindowOnly;
						break;
					case EClipDispType.WindowOnly:
						OpenTaiko.ConfigIni.eClipDispType = EClipDispType.Both;
						break;
					case EClipDispType.Both:
						OpenTaiko.ConfigIni.eClipDispType = EClipDispType.Off;
						break;
				}
			}

			if (OpenTaiko.ConfigIni.bTokkunMode && OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.Drums.TrainingToggleAuto)) {
				OpenTaiko.ConfigIni.bAutoPlay[0] = !OpenTaiko.ConfigIni.bAutoPlay[0];
			}
		}

#if DEBUG

		if (keyboard.KeyPressed((int)SlimDXKeys.Key.F7)) {
			OpenTaiko.ConfigIni.bAutoPlay[1] = !OpenTaiko.ConfigIni.bAutoPlay[1];
		}
#endif
		if (!this.actPauseMenu.bIsActivePopupMenu && this.bPAUSE && ((base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED)) && (base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED_FadeOut)) {
			if (keyboard.KeyPressed((int)SlimDXKeys.Key.UpArrow)) { // UpArrow(scrollspeed up)
				ドラムスクロール速度アップ();
			} else if (keyboard.KeyPressed((int)SlimDXKeys.Key.DownArrow)) {    // DownArrow (scrollspeed down)
				ドラムスクロール速度ダウン();
			} else if (OpenTaiko.ConfigIni.KeyAssign.KeyIsPressed(OpenTaiko.ConfigIni.KeyAssign.System.DisplayDebug)) {   // del (debug info)
				OpenTaiko.ConfigIni.bDisplayDebugInfo = !OpenTaiko.ConfigIni.bDisplayDebugInfo;
			} else if ((keyboard.KeyPressed((int)SlimDXKeys.Key.Escape))) {   // escape (exit)
				SoundManager.PlayTimer.Resume();
				OpenTaiko.Timer.Resume();
				this.t演奏中止();
			}
		}

		#region [ Minus & Equals Sound Group Level ]
		KeyboardSoundGroupLevelControlHandler.Handle(
			keyboard, OpenTaiko.SoundGroupLevelController, OpenTaiko.Skin, false);
		#endregion
	}

	protected void t入力メソッド記憶(EInstrumentPad part) {
		if (OpenTaiko.Pad.detectedDevice.Keyboard) {
			this.b演奏にキーボードを使った = true;
		}
		if (OpenTaiko.Pad.detectedDevice.Joypad) {
			this.b演奏にジョイパッドを使った = true;
		}
		if (OpenTaiko.Pad.detectedDevice.MIDIIN) {
			this.b演奏にMIDI入力を使った = true;
		}
		if (OpenTaiko.Pad.detectedDevice.Mouse) {
			this.b演奏にマウスを使った = true;
		}
	}


	protected bool t進行描画_AVI() {
		if (((base.ePhaseID == CStage.EPhase.Game_STAGE_FAILED) || (base.ePhaseID == CStage.EPhase.Game_STAGE_FAILED_FadeOut))
			&& (this.actAVI?.rVD.bPlaying ?? false)
			) {
			this.actAVI.Pause(); // paused but still shown
		}
		if (OpenTaiko.ConfigIni.bEnableAVI) {
			this.actAVI.Draw();
			return true;
		}
		return false;
	}
	protected void t進行描画_STAGEFAILED() {
		// Transition for failed games
		if (((base.ePhaseID == CStage.EPhase.Game_STAGE_FAILED)
			 || (base.ePhaseID == CStage.EPhase.Game_STAGE_FAILED_FadeOut))
			&& ((this.actStageFailed.Draw() != 0)
				&& (base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED_FadeOut))) {
			if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower) {
				this.eフェードアウト完了時の戻り値 = EGameplayScreenReturnValue.StageCleared;
			} else {
				this.eフェードアウト完了時の戻り値 = EGameplayScreenReturnValue.StageFailed;

			}
			base.ePhaseID = CStage.EPhase.Game_STAGE_FAILED_FadeOut;
			this.actFO.tフェードアウト開始();
		}
	}

	protected void t進行描画_パネル文字列() {
		if ((base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED) && (base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED_FadeOut)) {
			this.actPanel.Draw();
		}
	}
	protected void tパネル文字列の設定() {
		var panelString = string.IsNullOrEmpty(OpenTaiko.TJA.PANEL) ? OpenTaiko.TJA.TITLE.GetString("") : OpenTaiko.TJA.PANEL;

		this.actPanel.SetPanelString(panelString,
			OpenTaiko.stageSongSelect.rChoosenSong.songGenrePanel,
			OpenTaiko.Skin.Game_StageText,
			songNode: OpenTaiko.stageSongSelect.rChoosenSong);
	}


	protected void t進行描画_ゲージ() {
		if ((((base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED) && (base.ePhaseID != CStage.EPhase.Game_STAGE_FAILED_FadeOut)))) {
			this.actGauge.Draw();
		}
	}
	protected void t進行描画_コンボ() {
		this.actCombo.Draw();
	}
	protected void t進行描画_スコア() {
		this.actScore.Draw();
	}

	protected bool t進行描画_チップ(EInstrumentPad ePlayMode, int nPlayer) {
		if ((base.ePhaseID == CStage.EPhase.Game_STAGE_FAILED) || (base.ePhaseID == CStage.EPhase.Game_STAGE_FAILED_FadeOut)) {
			return true;
		}
		if ((this.nCurrentTopChip[nPlayer] == -1) || (this.nCurrentTopChip[nPlayer] >= listChip[nPlayer].Count)) {
			return true;
		}
		if (IsDanFailed) {
			return true;
		}

		CTja tja = OpenTaiko.GetTJA(nPlayer)!;

		var n現在時刻ms = (long)tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs);

		NowAIBattleSectionTime = (int)n現在時刻ms - NowAIBattleSection.StartTime;

		if (this.r指定時刻に一番近い未ヒットChip((long)n現在時刻ms, 0x50, 1000000, nPlayer) == null) {
			this.actChara.b演奏中[nPlayer] = false;
		}

		var dbCurrentScrollSpeed = this.actScrollSpeed.dbConfigScrollSpeed;

		//double speed = 264.0;	// BPM150の時の1小節の長さ[dot]
		const double speed = 324.0; // BPM150の時の1小節の長さ[dot]

		double ScrollSpeedTaiko = ((dbCurrentScrollSpeed[nPlayer] + 1.0) * speed) * 0.5 * 37.5 / 60000.0;

		CConfigIni configIni = OpenTaiko.ConfigIni;

		CTja dTX = OpenTaiko.GetTJA(nPlayer)!;
		bool bAutoPlay = configIni.bAutoPlay[nPlayer];
		if (nPlayer == 1)
			bAutoPlay = bAutoPlay || OpenTaiko.ConfigIni.bAIBattleMode;

		if (this.n分岐した回数[nPlayer] == 0) {
			this.bUseBranch[nPlayer] = dTX.bHIDDENBRANCH ? false : dTX.bチップがある.Branch;
		}


		//CDTXMania.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.灰, this.nLoopCount_Clear.ToString()  );

		float play_bpm_time = this.GetNowPBMTime(dTX, 0);

		#region [update phase (bar lines' position)]
		foreach (var pChip in dTX.listBarLineChip) {
			long time = pChip.n発声時刻ms - n現在時刻ms;
			long msDTime_end = time;
			double th16DBeat = pChip.fBMSCROLLTime - play_bpm_time;
			double _scroll_rate = (dbCurrentScrollSpeed[nPlayer] + 1.0) / 10.0;

			double _scrollSpeed = pChip.dbSCROLL * _scroll_rate;
			double _scrollSpeed_Y = pChip.dbSCROLL_Y * _scroll_rate;
			pChip.nHorizontalChipDistance = NotesManager.GetNoteX(time, th16DBeat, pChip.dbBPM, _scrollSpeed, pChip.eScrollMode);
			pChip.nVerticalChipDistance = NotesManager.GetNoteY(time, th16DBeat, pChip.dbBPM, _scrollSpeed_Y, pChip.eScrollMode);
		}
		#endregion

		#region [update phase (notes' position & auto judgement)]
		foreach (var pChip in dTX.listNoteChip) {
			long time = pChip.n発声時刻ms - n現在時刻ms;
			double th16DBeat = pChip.fBMSCROLLTime - play_bpm_time;
			double _scroll_rate = (dbCurrentScrollSpeed[nPlayer] + 1.0) / 10.0;

			CChip velocityRefChip = (NotesManager.IsRollEnd(pChip)) ? pChip.start : pChip; // && !StretchRoll
			double _scrollSpeed = velocityRefChip.dbSCROLL * _scroll_rate;
			double _scrollSpeed_Y = velocityRefChip.dbSCROLL_Y * _scroll_rate;
			pChip.nHorizontalChipDistance = NotesManager.GetNoteX(time, th16DBeat, velocityRefChip.dbBPM, _scrollSpeed, velocityRefChip.eScrollMode);
			pChip.nVerticalChipDistance = NotesManager.GetNoteY(time, th16DBeat, velocityRefChip.dbBPM, _scrollSpeed_Y, velocityRefChip.eScrollMode);

			if (!this.bPAUSE && !this.isRewinding) {
				if (!pChip.IsMissed && !pChip.bHit) {
					if (NotesManager.IsMissableNote(pChip))//|| pChip.nチャンネル番号 == 0x9A )
					{
						//こっちのほうが適格と考えたためフラグを変更.2020.04.20 Akasoko26
						if (time <= 0) {
							if (this.e指定時刻からChipのJUDGEを返す(n現在時刻ms, pChip, nPlayer) == ENoteJudge.Miss) {
								pChip.IsMissed = true;
								this.tチップのヒット処理(n現在時刻ms, pChip, EInstrumentPad.Taiko, false, 0, nPlayer);
								pChip.eNoteState = ENoteState.Bad; // set after hit processing for detecting duplicated misses
							}
						}
					}
				} else if (NotesManager.IsGenericRoll(pChip)) {
					if (pChip.end.n発声時刻ms <= n現在時刻ms) {
						if (this.e指定時刻からChipのJUDGEを返す(n現在時刻ms, pChip, nPlayer) == ENoteJudge.Miss) {
							pChip.bHit = true;
						}
					}
				}
			}
		}
		#endregion

		#region [update phase, process forward for correct order of non-note events]
		for (; this.nCurrentTopChip[nPlayer] < dTX.listChip.Count; ++this.nCurrentTopChip[nPlayer]) {
			CChip pChip = dTX.listChip[this.nCurrentTopChip[nPlayer]];
			//Debug.WriteLine( "nCurrentTopChip=" + nCurrentTopChip + ", ch=" + pChip.nチャンネル番号.ToString("x2") + ", 発音位置=" + pChip.n発声位置 + ", 発声時刻ms=" + pChip.n発声時刻ms );
			if (!hasChipBeenPlayedAt(pChip, n現在時刻ms)) // not processed yet
				break;

			switch (pChip.nChannelNo) {
				#region [ 01: BGM ]
				case 0x01:  // BGM
					if (!this.bPAUSE && !pChip.bHit) { // can't play while paused
						pChip.bHit = true;
						if (configIni.bBGMPlayVoiceSound) {
							dTX.tチップの再生(pChip, SoundManager.PlayTimer.GameTimeToSystemTime((long)tja.TjaTimeToGameTime(pChip.n発声時刻ms)));
						}
					}
					break;
				#endregion
				#region [ 03: BPM変更 ]
				case 0x03:  // Initial BPM
					if (!pChip.bHit) {
						pChip.bHit = true;
						// this.actPlayInfo.dbBPM[nPlayer] has already been initialized
						// Alternative behavior: Start with 120 BPM chara speed, switch to initial BPM chara speed at this chip?
					}
					break;
				#endregion
				#region [ 08: BPM変更(拡張) ]
				case 0x08:  // BPM変更(拡張)
							//CDTXMania.act文字コンソール.tPrint( 414 + pChip.nバーからの距離dot.Drums + 4, 192, C文字コンソール.Eフォント種別.白, "BRANCH START" + "  " + pChip.n整数値.ToString() );
					if (!pChip.bHit) {
						pChip.bHit = true;
						//if( pChip.nコース == this.n現在のコース[ nPlayer ] )
						//{
						//double bpm = ( dTX.listBPM[ pChip.n整数値_内部番号 ].dbBPM値 * ( ( (double) configIni.n演奏速度 ) / 20.0 ) );
						//int nUnit = (int)((60.0 / ( bpm ) / this.actChara.nキャラクター通常モーション枚数 ) * 1000 );
						//int nUnit_gogo = (int)((60.0 / ( bpm ) / this.actChara.nキャラクターゴーゴーモーション枚数 ) * 1000 );
						//this.actChara.ct通常モーション = new CCounter( 0, this.actChara.nキャラクター通常モーション枚数 - 1, nUnit, CDTXMania.Timer );
						//this.actChara.ctゴーゴーモーション = new CCounter(0, this.actChara.nキャラクターゴーゴーモーション枚数 - 1, nUnit_gogo * 2, CDTXMania.Timer);

						//}
					}
					break;
				#endregion

				#region [ 11-1f & 101-: Taiko ]
				case 0x11:
				case 0x12:
				case 0x13:
				case 0x14:
				case 0x1C:
				case 0x101:
					// draw later
					break;

				case 0x15:
				case 0x16:
				case 0x17:
				case 0x19:
				case 0x1D: {
						if (!pChip.bProcessed) {
							this.AddNowProcessingRollChip(nPlayer, pChip);
						}
						// draw later
					}

					break;
				case 0x18: {
						if (!pChip.bProcessed) {
							this.ProcessRollEnd(nPlayer, pChip, false);
						}
						// draw later
					}

					break;

				case 0x1e:
					break;

				case 0x1a:
				case 0x1b:
				case 0x1f:
					// draw later
					break;
				#endregion
				#region [ 20-2F: EmptySlot ]
				case 0x20:
				case 0x21:
					// draw later
					break;

				case 0x22:
				case 0x23:
				case 0x24:
				case 0x25:
				case 0x26:
				case 0x27:
				case 0x28:
				case 0x29:
				case 0x2a:
				case 0x2b:
				case 0x2c:
				case 0x2d:
				case 0x2e:
				case 0x2f:
					break;
				#endregion
				#region [ 31-3f: EmptySlot ]
				case 0x31:
				case 0x32:
				case 0x33:
				case 0x34:
				case 0x35:
				case 0x36:
				case 0x37:
				case 0x38:
				case 0x39:
				case 0x3a:
				case 0x3b:
				case 0x3c:
				case 0x3d:
				case 0x3e:
				case 0x3f:
					break;
				#endregion

				#region [ 50: 小節線 ]
				case 0x50:  // 小節線
				{

						if (!this.bPAUSE && !pChip.bHit) { // can't update while paused
														   //if (nPlayer == 0) TJAPlayer3.BeatScaling = new CCounter(0, 1000, 120.0 / pChip.dbBPM / 2.0, TJAPlayer3.Timer);
							if (NowAIBattleSectionTime >= NowAIBattleSection.Length && NowAIBattleSection.End == AIBattleSection.EndType.None && nPlayer == 0) {
								PassAIBattleSection();

								NowAIBattleSectionCount++;

								if (AIBattleSections.Count > NowAIBattleSectionCount) {
									NowAIBattleSectionTime = 0;
								}
								NowAIBattleSectionTime = (int)n現在時刻ms - NowAIBattleSection.StartTime;
							}


							this.actChara.b演奏中[nPlayer] = true;
							if (this.actPlayInfo.NowMeasure[nPlayer] == 0) {
								UpdateCharaCounter(nPlayer);
							}
							actPlayInfo.NowMeasure[nPlayer] = pChip.n整数値_内部番号;
							pChip.bHit = true;
						}
						// draw later
						break;
					}
				#endregion
				#region [ 54: 動画再生 ]
				case 0x54:  // 動画再生
					if (!this.bPAUSE && !pChip.bHit) { // can't play while paused
						pChip.bHit = true;
						if (configIni.bEnableAVI) {
							if ((dTX.listVD.TryGetValue(pChip.n整数値_内部番号, out CVideoDecoder vd))) {
								ShowVideo = true;
								if (OpenTaiko.ConfigIni.bEnableAVI && vd != null) {
									this.actAVI.Start(vd);
									this.actAVI.Seek(pChip.VideoStartTimeMs);
								}
							}
						}
					}
					break;
				case 0x55:
					if (!this.bPAUSE && !pChip.bHit) { // can't play while paused
						pChip.bHit = true;
						if (configIni.bEnableAVI) {
							if ((dTX.listVD.TryGetValue(pChip.n整数値_内部番号, out CVideoDecoder vd))) {
								ShowVideo = false;
								if (OpenTaiko.ConfigIni.bEnableAVI && vd != null) {
									this.actAVI.Stop();
								}
							}

							if ((dTX.listVD.TryGetValue(1, out CVideoDecoder vd2))) {
								ShowVideo = true;
								if (OpenTaiko.ConfigIni.bEnableAVI && vd != null) {
									this.actAVI.Start(vd);
								}
							}
						}
					}
					break;
				#endregion
				#region[ 55-60: EmptySlot ]
				case 0x56:
				case 0x57:
				case 0x58:
				case 0x59:
					break;
				#endregion
				#region [ 61-89: EmptySlot ]
				case 0x60:
				case 0x61:
				case 0x62:
				case 0x63:
				case 0x64:
				case 0x65:
				case 0x66:
				case 0x67:
				case 0x68:
				case 0x69:
				case 0x70:
				case 0x71:
				case 0x72:
				case 0x73:
				case 0x74:
				case 0x75:
				case 0x76:
				case 0x77:
				case 0x78:
				case 0x79:
				case 0x80:
				case 0x81:
				case 0x82:
				case 0x83:
				case 0x84:
				case 0x85:
				case 0x86:
				case 0x87:
				case 0x88:
				case 0x89:
					break;
				#endregion

				#region[ 90-9A: EmptySlot ]
				case 0x90:
				case 0x91:
				case 0x92:
				case 0x93:
				case 0x94:
				case 0x95:
				case 0x96:
				case 0x97:
				case 0x98:
				case 0x99:
				case 0x9A:
					break;
				#endregion

				#region[ 9B-9F: 太鼓 ]
				case 0x9B:
					// 段位認定モードの幕アニメーション
					if (!pChip.bHit) {
						pChip.bHit = true;
						this.ListDan_Number = pChip.n整数値_内部番号;
						this.actPanel.t歌詞テクスチャを削除する();
						this.actDan.Update();
						if (ListDan_Number != 0 && actDan.FirstSectionAnime) {
							if (Dan_Cert.GetFailedAllChallenges(this.actDan.GetExam(), OpenTaiko.stageSongSelect.rChoosenSong.DanSongs)) {
								this.nCurrentTopChip[nPlayer] = tja.listChip.Count - 1;   // 終端にシーク
								IsDanFailed = true;
								return true;
							}

							// Play next song here
							this.actDan.Start(this.ListDan_Number);
						} else {
							actDan.FirstSectionAnime = true;
						}
					}
					break;
				//0x9C BPM変化(アニメーション用)
				case 0x9C:
					//CDTXMania.act文字コンソール.tPrint( 414 + pChip.nバーからの距離dot.Taiko + 8, 192, C文字コンソール.Eフォント種別.白, "BPMCHANGE" );
					if (!pChip.bHit) {
						pChip.bHit = true;
						if (pChip.nBranch == this.nCurrentBranch[nPlayer]) {
							if (dTX.listBPM.TryGetValue(pChip.n整数値_内部番号, out CTja.CBPM cBPM)) {
								this.actPlayInfo.dbBPM[nPlayer] = cBPM.dbBPM値;// + dTX.BASEBPM;
							}

							UpdateCharaCounter(nPlayer);
							//this.actDancer.ct踊り子モーション = new CCounter(0, this.actDancer.ar踊り子モーション番号.Length - 1, (dbUnit * CDTXMania.Skin.Game_Dancer_Beat) / this.actDancer.ar踊り子モーション番号.Length, CSound管理.rc演奏用タイマ);
							//this.actChara.ctモブモーション = new CCounter(0, this.actChara.arモブモーション番号.Length - 1, (dbUnit) / this.actChara.arモブモーション番号.Length, CSound管理.rc演奏用タイマ);
							//#if C_82D982F182AF82CD82A282AF82A2
							/*
                             * for( int dancer = 0; dancer < 5; dancer++ )
                                this.actDancer.st投げ上げ[ dancer ].ct進行 = new CCounter( 0, this.actDancer.arモーション番号_登場.Length - 1, dbUnit / this.actDancer.arモーション番号_登場.Length, CSound管理.rc演奏用タイマ );

                            this.actDancer.ct通常モーション = new CCounter( 0, this.actDancer.arモーション番号_通常.Length - 1, ( dbUnit * 4 ) / this.actDancer.arモーション番号_通常.Length, CSound管理.rc演奏用タイマ );
                            this.actDancer.ctモブ = new CCounter( 1.0, 16.0, (int)((60.0 / bpm / 16.0 ) * 1000 ), CSound管理.rc演奏用タイマ );
//#endif
                           */
						}

					}
					break;

				case 0x9D: //SCROLL
					if (!pChip.bHit) {
						pChip.bHit = true;
						//if ( dTX.listSCROLL.ContainsKey( pChip.n整数値_内部番号 ) )
						//{
						//this.actPlayInfo.dbBPM = ( dTX.listBPM[ pChip.n整数値_内部番号 ].dbBPM値 * ( ( (double) configIni.n演奏速度 ) / 20.0 ) );// + dTX.BASEBPM;
						//}
					}
					break;

				case 0x9E: //ゴーゴータイム
					if (!pChip.bHit) {
						pChip.bHit = true;
						this.bIsGOGOTIME[nPlayer] = true;
						if (!this.isRewinding)
							this.StartGoGoTimeEffect(nPlayer);
					}
					break;
				case 0x9F: //ゴーゴータイム
					if (!pChip.bHit) {
						pChip.bHit = true;
						this.bIsGOGOTIME[nPlayer] = false;
					}
					break;
				#endregion

				#region [ EXTENDED COMMANDS ]
				case 0xa0: //camera vertical move start
					if (!pChip.bHit) {
						pChip.bHit = true;
						this.currentCamVMoveChip = pChip;
						this.ctCamVMove = new CCounter(0, pChip.fCamTimeMs, 1, OpenTaiko.Timer);
					}
					break;
				case 0xa1: //camera vertical move end
					if (!pChip.bHit) {
						pChip.bHit = true;
					}
					break;
				case 0xa2: //camera horizontal move start
					if (!pChip.bHit) {
						pChip.bHit = true;
						this.currentCamHMoveChip = pChip;
						this.ctCamHMove = new CCounter(0, pChip.fCamTimeMs, 1, OpenTaiko.Timer);
					}
					break;
				case 0xa3: //camera horizontal move end
					if (!pChip.bHit) {
						pChip.bHit = true;
					}
					break;
				case 0xa4: //camera zoom start
					if (!pChip.bHit) {
						pChip.bHit = true;
						this.currentCamZoomChip = pChip;
						this.ctCamZoom = new CCounter(0, pChip.fCamTimeMs, 1, OpenTaiko.Timer);
					}
					break;
				case 0xa5: //camera zoom end
					if (!pChip.bHit) {
						pChip.bHit = true;
					}
					break;
				case 0xa6: //camera rotation start
					if (!pChip.bHit) {
						pChip.bHit = true;
						this.currentCamRotateChip = pChip;
						this.ctCamRotation = new CCounter(0, pChip.fCamTimeMs, 1, OpenTaiko.Timer);
					}
					break;
				case 0xa7: //camera rotation end
					if (!pChip.bHit) {
						pChip.bHit = true;
					}
					break;
				case 0xa8: //camera vertical scaling start
					if (!pChip.bHit) {
						pChip.bHit = true;
						this.currentCamVScaleChip = pChip;
						this.ctCamVScale = new CCounter(0, pChip.fCamTimeMs, 1, OpenTaiko.Timer);
					}
					break;
				case 0xa9: //camera vertical scaling end
					if (!pChip.bHit) {
						pChip.bHit = true;
					}
					break;
				case 0xb0: //camera horizontal scaling start
					if (!pChip.bHit) {
						pChip.bHit = true;
						this.currentCamHScaleChip = pChip;
						this.ctCamHScale = new CCounter(0, pChip.fCamTimeMs, 1, OpenTaiko.Timer);
					}
					break;
				case 0xb1: //camera horizontal scaling end
					if (!pChip.bHit) {
						pChip.bHit = true;
					}
					break;
				case 0xb2: //change border color
					if (!pChip.bHit) {
						pChip.bHit = true;
						OpenTaiko.borderColor = pChip.borderColor;
					}
					break;
				case 0xb3: //set camera x offset
					if (!pChip.bHit) {
						pChip.bHit = true;

						this.currentCamHMoveChip = pChip;
						this.ctCamHMove = new CCounter(0, 0, 1, OpenTaiko.Timer);
					}
					break;
				case 0xb4: //set camera y offset
					if (!pChip.bHit) {
						pChip.bHit = true;

						this.currentCamVMoveChip = pChip;
						this.ctCamVMove = new CCounter(0, 0, 1, OpenTaiko.Timer);
					}
					break;
				case 0xb5: //set camera zoom factor
					if (!pChip.bHit) {
						pChip.bHit = true;

						this.currentCamZoomChip = pChip;
						this.ctCamZoom = new CCounter(0, 0, 1, OpenTaiko.Timer);
					}
					break;
				case 0xb6: //set camera rotation
					if (!pChip.bHit) {
						pChip.bHit = true;

						this.currentCamRotateChip = pChip;
						this.ctCamRotation = new CCounter(0, 0, 1, OpenTaiko.Timer);
					}
					break;
				case 0xb7: //set camera x scale
					if (!pChip.bHit) {
						pChip.bHit = true;

						this.currentCamHScaleChip = pChip;
						this.ctCamHScale = new CCounter(0, 0, 1, OpenTaiko.Timer);
					}
					break;
				case 0xb8: //set camera y scale
					if (!pChip.bHit) {
						pChip.bHit = true;

						this.currentCamVScaleChip = pChip;
						this.ctCamVScale = new CCounter(0, 0, 1, OpenTaiko.Timer);
					}
					break;
				case 0xb9: //reset camera
					if (!pChip.bHit) {
						pChip.bHit = true;

						OpenTaiko.borderColor = new Color4(0f, 0f, 0f, 0f);

						this.currentCamVMoveChip = pChip;
						this.currentCamHMoveChip = pChip;

						this.currentCamZoomChip = pChip;
						this.currentCamRotateChip = pChip;

						this.currentCamVScaleChip = pChip;
						this.currentCamHScaleChip = pChip;

						this.ctCamVMove = new CCounter(0, 0, 1, OpenTaiko.Timer);
						this.ctCamHMove = new CCounter(0, 0, 1, OpenTaiko.Timer);

						this.ctCamZoom = new CCounter(0, 0, 1, OpenTaiko.Timer);
						this.ctCamRotation = new CCounter(0, 0, 1, OpenTaiko.Timer);

						this.ctCamVScale = new CCounter(0, 0, 1, OpenTaiko.Timer);
						this.ctCamHScale = new CCounter(0, 0, 1, OpenTaiko.Timer);
					}
					break;
				case 0xba: //enable doron
					if (!pChip.bHit) {
						pChip.bHit = true;
						bCustomDoron = true;
					}
					break;
				case 0xbb: //disable doron
					if (!pChip.bHit) {
						pChip.bHit = true;
						bCustomDoron = false;
					}
					break;
				case 0xbc: //add object
					if (!pChip.bHit) {
						pChip.bHit = true;

						dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);
						obj.x = pChip.fObjX;
						obj.y = pChip.fObjY;
						obj.isVisible = true;
					}
					break;
				case 0xbd: //remove object
					if (!pChip.bHit) {
						pChip.bHit = true;

						dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);
						obj.isVisible = false;
					}
					break;
				case 0xbe: //object animation start
				case 0xc0:
				case 0xc2:
				case 0xc4:
				case 0xc6:
				case 0xc8:
					if (!pChip.bHit) {
						pChip.bHit = true;

						dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj);
						objHandlers.Add(pChip, new CCounter(0, pChip.fObjTimeMs, 1, OpenTaiko.Timer));
					}
					break;
				case 0xbf: //object animation end
				case 0xc1:
				case 0xc3:
				case 0xc5:
				case 0xc7:
				case 0xc9:
					if (!pChip.bHit) {
						pChip.bHit = true;
					}
					break;
				case 0xca: //set object color
					if (!pChip.bHit) {
						pChip.bHit = true;

						dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);
						obj.color = pChip.borderColor;
					}
					break;
				case 0xcb: //set object y
				case 0xcc: //set object x
				case 0xcd: //set object vertical scale
				case 0xce: //set object horizontal scale
				case 0xcf: //set object rotation
				case 0xd0: //set object opacity
					if (!pChip.bHit) {
						pChip.bHit = true;

						dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj);
						objHandlers.Add(pChip, new CCounter(0, 0, 1, OpenTaiko.Timer));
					}
					break;
				case 0xd1: //change texture
					if (!pChip.bHit) {
						pChip.bHit = true;

						if (OpenTaiko.Tx.trackedTextures.ContainsKey(pChip.strTargetTxName)) {
							OpenTaiko.Tx.trackedTextures.TryGetValue(pChip.strTargetTxName, out CTexture oldTx);
							dTX.listTextures.TryGetValue(pChip.strNewPath, out CTexture newTx);

							newTx.Opacity = oldTx.Opacity;
							newTx.fZ軸中心回転 = oldTx.fZ軸中心回転;
							newTx.vcScaleRatio = oldTx.vcScaleRatio;

							oldTx.UpdateTexture(newTx, newTx.sz画像サイズ.Width, newTx.sz画像サイズ.Height);
						}
					}
					break;
				case 0xd2: //reset texture
					if (!pChip.bHit) {
						pChip.bHit = true;

						if (OpenTaiko.Tx.trackedTextures.ContainsKey(pChip.strTargetTxName)) {
							OpenTaiko.Tx.trackedTextures.TryGetValue(pChip.strTargetTxName, out CTexture oldTx);
							dTX.listOriginalTextures.TryGetValue(pChip.strTargetTxName, out CTexture originalTx);

							originalTx.Opacity = oldTx.Opacity;
							originalTx.fZ軸中心回転 = oldTx.fZ軸中心回転;
							originalTx.vcScaleRatio = oldTx.vcScaleRatio;

							oldTx.UpdateTexture(originalTx, originalTx.sz画像サイズ.Width, originalTx.sz画像サイズ.Height);
						}
					}
					break;
				case 0xd3: //set config
					if (!pChip.bHit) {
						pChip.bHit = true;
						string[] split = pChip.strConfigValue.Split('=');

						//TJAPlayer3.Skin.t文字列から読み込み(pChip.strConfigValue, split[0]);
						bConfigUpdated = true;
					}
					break;
				case 0xd4: //start object animation
					if (!pChip.bHit) {
						pChip.bHit = true;
						dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);

						obj.tStartAnimation(pChip.dbAnimInterval, false);
					}
					break;
				case 0xd5: //start object animation (looping)
					if (!pChip.bHit) {
						pChip.bHit = true;
						dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);

						obj.tStartAnimation(pChip.dbAnimInterval, true);
					}
					break;
				case 0xd6: //end object animation
					if (!pChip.bHit) {
						pChip.bHit = true;
						dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);

						obj.tStopAnimation();
					}
					break;
				case 0xd7: //set object frame
					if (!pChip.bHit) {
						pChip.bHit = true;
						dTX.listObj.TryGetValue(pChip.strObjName, out CSongObject obj);

						obj.frame = pChip.intFrame;
					}
					break;
				#endregion

				#region [ d8-d9: EXTENDED2 ]
				case 0xd8:
					if (!pChip.bHit) {
						OpenTaiko.ConfigIni.nGameType[nPlayer] = pChip.eGameType;
						pChip.bHit = true;
					}
					break;
				case 0xd9:
					if (!pChip.bHit) {
						bSplitLane[nPlayer] = true;
						pChip.bHit = true;
					}
					break;
				#endregion

				#region [ da: ミキサーへチップ音追加 ]
				case 0xDA:
					if (!pChip.bHit) {
						//Debug.WriteLine( "[DA(AddMixer)] BAR=" + pChip.n発声位置 / 384 + " ch=" + pChip.nチャンネル番号.ToString( "x2" ) + ", wav=" + pChip.n整数値.ToString( "x2" ) + ", time=" + pChip.n発声時刻ms );
						pChip.bHit = true;
						if (listWAV.TryGetValue(pChip.n整数値_内部番号, out CTja.CWAV wc)) // 参照が遠いので後日最適化する
						{
							for (int i = 0; i < nPolyphonicSounds; i++) {
								if (wc.rSound[i] != null) {
									//CDTXMania.Sound管理.AddMixer( wc.rSound[ i ] );
									AddMixer(wc.rSound[i], pChip.b演奏終了後も再生が続くチップである);
								}
							}
						}
					}
					break;
				#endregion
				#region [ db: ミキサーからチップ音削除 ]
				case 0xDB:
					if (!pChip.bHit) {
						//Debug.WriteLine( "[DB(RemoveMixer)] BAR=" + pChip.n発声位置 / 384 + " ch=" + pChip.nチャンネル番号.ToString( "x2" ) + ", wav=" + pChip.n整数値.ToString( "x2" ) + ", time=" + pChip.n発声時刻ms );
						pChip.bHit = true;
						if (listWAV.TryGetValue(pChip.n整数値_内部番号, out CTja.CWAV wc)) // 参照が遠いので後日最適化する
						{
							for (int i = 0; i < nPolyphonicSounds; i++) {
								if (wc.rSound[i] != null) {
									//CDTXMania.Sound管理.RemoveMixer( wc.rSound[ i ] );
									if (!wc.rSound[i].b演奏終了後も再生が続くチップである)   // #32248 2013.10.16 yyagi
									{                                                           // DTX終了後も再生が続くチップの0xDB登録をなくすことはできず。
										RemoveMixer(wc.rSound[i]);                          // (ミキサー解除のタイミングが遅延する場合の対応が面倒なので。)
									}                                                           // そこで、代わりにフラグをチェックしてミキサー削除ロジックへの遷移をカットする。
								}
							}
						}
					}
					break;
				#endregion

				#region[ dc-df:太鼓(特殊命令) ]
				case 0xDC: //DELAY
					if (!pChip.bHit) {
						pChip.bHit = true;
						//if ( dTX.listDELAY.ContainsKey( pChip.n整数値_内部番号 ) )
						//{
						//this.actPlayInfo.dbBPM = ( dTX.listBPM[ pChip.n整数値_内部番号 ].dbBPM値 * ( ( (double) configIni.n演奏速度 ) / 20.0 ) );// + dTX.BASEBPM;
						//}
					}
					break;
				case 0xDD: //SECTION
					if (!pChip.bHit) {
						// 分岐毎にリセットしていたのでSECTIONの命令が来たらリセットする。
						this.tBranchReset(nPlayer);
						pChip.bHit = true;
					}
					break;

				case 0xDE: //Judgeに応じたCourseを取得
					if (!pChip.bHit) {
						this.b強制分岐譜面[nPlayer] = false;
						//分岐の種類はプレイヤー関係ないと思う
						this.eBranch種類 = pChip.eBranchCondition;
						this.nBranch条件数値A = pChip.nBranchCondition1_Professional;
						this.nBranch条件数値B = pChip.nBranchCondition2_Master;
						if (!this.bLEVELHOLD[nPlayer]) {
							//成仏2000にある-2,-1だったら達人に強制分岐みたいな。
							this.t強制用条件かを判断する(pChip.nBranchCondition1_Professional, pChip.nBranchCondition2_Master, nPlayer);

							OpenTaiko.stageGameScreen.bUseBranch[nPlayer] = true;

							CBRANCHSCORE branchScore;
							if (OpenTaiko.ConfigIni.bAIBattleMode) {
								branchScore = this.CBranchScore[0];
							} else {
								branchScore = this.CBranchScore[nPlayer];
							}
							this.tBranchJudge(pChip, branchScore.cBigNotes, branchScore.nScore, branchScore.nRoll, branchScore.nGreat, branchScore.nGood, branchScore.nMiss, nPlayer);

							if (this.b強制分岐譜面[nPlayer])//強制分岐譜面だったら次回コースをそのコースにセット
								this.nNextBranch[nPlayer] = this.E強制コース[nPlayer];

							this.t分岐処理(this.nNextBranch[nPlayer], nPlayer, pChip.n分岐時刻ms, pChip.eBranchCondition);

							OpenTaiko.stageGameScreen.actLaneTaiko.t分岐レイヤー_コース変化(OpenTaiko.stageGameScreen.actLaneTaiko.stBranch[nPlayer].nAfter, this.nNextBranch[nPlayer], nPlayer);
							OpenTaiko.stageGameScreen.actMtaiko.tBranchEvent(OpenTaiko.stageGameScreen.actMtaiko.After[nPlayer], this.nNextBranch[nPlayer], nPlayer);
							this.nCurrentBranch[nPlayer] = this.nNextBranch[nPlayer];
						}
						this.n分岐した回数[nPlayer]++;
						pChip.bHit = true;
					}
					break;
				case 0x52://End処理
					if (!pChip.bHit) {

						pChip.bHit = true;
					}

					break;
				case 0xE0:
					//if( !pChip.bHit )
					//{
					//#BARLINEONと#BARLINEOFF
					//演奏中は使用しません。
					//}
					break;
				case 0xE1:
					if (!pChip.bHit) {
						//LEVELHOLD
						this.bLEVELHOLD[nPlayer] = true;
					}
					break;
				case 0xE2:
					if (!pChip.bHit) {
						CTja.CJPOSSCROLL jposs = dTX.listJPOSSCROLL[pChip.n整数値_内部番号];
						OpenTaiko.stageGameScreen.actLaneTaiko.t判定枠移動(nPlayer, jposs, pChip.n発声時刻ms);
						pChip.bHit = true;
					}
					break;
				#endregion
				#region[ f1: 歌詞 ]
				case 0xF1:
					if (!pChip.bHit) {
						if (OpenTaiko.ConfigIni.nPlayerCount == 1) {
							if (pChip.n整数値_内部番号 >= 0 && pChip.n整数値_内部番号 < dTX.listLyric.Count) {
								this.actPanel.t歌詞テクスチャを生成する(dTX.listLyric[pChip.n整数値_内部番号]);
							}
						}
						pChip.bHit = true;
					}
					break;
				#endregion
				#region[ ff: 譜面の強制終了 ]
				//バグで譜面がとてつもないことになっているため、#ENDがきたらこれを差し込む。
				case 0xFF:
					if (!pChip.bHit) {
						if (OpenTaiko.ConfigIni.bTokkunMode) {
							foreach (CTja.CWAV cwav in OpenTaiko.TJA.listWAV.Values) {
								for (int i = 0; i < nPolyphonicSounds; i++) {
									if ((cwav.rSound[i] != null) && cwav.rSound[i].IsPlaying) {
										return false;
									}
								}
							}
						}
						pChip.bHit = true;
						return true;
					}
					break;
				#endregion

				#region [ d8-d9: EXTENDED2 ]
				case 0xe3:
					if (!pChip.bHit) {
						bSplitLane[nPlayer] = false;
						pChip.bHit = true;
					}
					break;
				case 0xe4:
					if (!pChip.bHit) {
						pChip.bHit = true;
					}
					// draw later
					break;
				case 0x09:
					if (!pChip.bHit) {

						pChip.bHit = true;
					}
					break;
				case 0x0A:
					if (!pChip.bHit) {

						pChip.bHit = true;
					}
					break;
				case 0x0B:
					if (!pChip.bHit) {

						pChip.bHit = true;
					}
					break;
					#endregion
			}
		}

		if (this.isRewinding) {
			this.isRewinding = false;
			if (this.bIsGOGOTIME[nPlayer] && this.bIsGOGOTIME[nPlayer] != this.bWasGOGOTIME[nPlayer]) {
				this.StartGoGoTimeEffect(nPlayer);
			}
			this.bWasGOGOTIME[nPlayer] = this.bIsGOGOTIME[nPlayer];
		}

		if (!this.bPAUSE) {
			foreach (var cChipCurrentlyInProcess in chip現在処理中の連打チップ[nPlayer]) {
				if (cChipCurrentlyInProcess.bHit)
					continue;
				//if( cChipCurrentlyInProcess.nチャンネル番号 >= 0x13 && cChipCurrentlyInProcess.nチャンネル番号 <= 0x15 )//|| pChip.nチャンネル番号 == 0x9A )
				if (NotesManager.IsBigNote(cChipCurrentlyInProcess)) {
					if ((cChipCurrentlyInProcess.n発声時刻ms - n現在時刻ms) < -CTja.GameDurationToTjaDuration(OpenTaiko.ConfigIni.nBigNoteWaitTimems)
						&& (cChipCurrentlyInProcess.n発声時刻ms <= n現在時刻ms && cChipCurrentlyInProcess.end.n発声時刻ms >= n現在時刻ms))
					//( ( chip現在処理中の連打チップ.nバーからのノーツ末端距離dot.Taiko < -500 ) && ( chip現在処理中の連打チップ.n発声時刻ms <= CSound管理.rc演奏用タイマ.n現在時刻ms && chip現在処理中の連打チップ.nノーツ終了時刻ms >= CSound管理.rc演奏用タイマ.n現在時刻ms ) ) )
					//( ( pChip.n発声時刻ms <= CSound管理.rc演奏用タイマ.n現在時刻ms && pChip.nノーツ終了時刻ms >= CSound管理.rc演奏用タイマ.n現在時刻ms ) ) )
					{
						if (bAutoPlay)
							this.tチップのヒット処理(n現在時刻ms, cChipCurrentlyInProcess, EInstrumentPad.Taiko, false, 0, nPlayer);
					}
				}
			}
		}
		#endregion

		#region [draw phase (bar line), backward for correct stack order]
		for (int iChip = dTX.listBarLineChip.Count; iChip-- > 0;) {
			CChip pChip = dTX.listBarLineChip[iChip];
			switch (pChip.nChannelNo) {
				case 0x50: // 小節線
				case 0xe4: // #BARLINE
					this.t進行描画_チップ_小節線(configIni, ref dTX, ref pChip, nPlayer);
					break;
			}
		}
		#endregion

		#region [draw phase (note), backward for correct stack order]
		for (int iChip = dTX.listNoteChip.Count; iChip-- > 0;) {
			CChip pChip = dTX.listNoteChip[iChip];

			switch (pChip.nChannelNo) {
				#region [ 11-1f & 101-: Taiko ]
				case 0x11:
				case 0x12:
				case 0x13:
				case 0x14:
				case 0x1C:
				case 0x101: {
						this.t進行描画_チップ_Taiko(configIni, ref dTX, ref pChip, nPlayer);
					}
					break;

				case 0x15:
				case 0x16:
				case 0x17:
				case 0x19:
				case 0x1D: {
						this.t進行描画_チップ_Taiko連打(configIni, ref dTX, ref pChip, nPlayer);
					}

					break;
				case 0x18: {
						this.t進行描画_チップ_Taiko連打(configIni, ref dTX, ref pChip, nPlayer);
					}

					break;

				case 0x1e:
					break;

				case 0x1a:
				case 0x1b:
				case 0x1f: {
						this.t進行描画_チップ_Taiko(configIni, ref dTX, ref pChip, nPlayer);
					}
					break;
				#endregion
				#region [ 20-2F: EmptySlot ]
				case 0x20:
				case 0x21: {
						this.t進行描画_チップ_Taiko連打(configIni, ref dTX, ref pChip, nPlayer);
					}
					break;
					#endregion
			}
		}
		#endregion

		#region [ EXTENDED CONTROLS ]
		if (ctCamVMove != null) //vertical camera move
		{
			ctCamVMove.Tick();
			float value = 0.0f;
			if (currentCamVMoveChip.strCamEaseType.Equals("IN")) value = easing.EaseIn(ctCamVMove, currentCamVMoveChip.fCamScrollStartY, currentCamVMoveChip.fCamScrollEndY, currentCamVMoveChip.fCamMoveType);
			if (currentCamVMoveChip.strCamEaseType.Equals("OUT")) value = easing.EaseOut(ctCamVMove, currentCamVMoveChip.fCamScrollStartY, currentCamVMoveChip.fCamScrollEndY, currentCamVMoveChip.fCamMoveType);
			if (currentCamVMoveChip.strCamEaseType.Equals("IN_OUT")) value = easing.EaseInOut(ctCamVMove, currentCamVMoveChip.fCamScrollStartY, currentCamVMoveChip.fCamScrollEndY, currentCamVMoveChip.fCamMoveType);
			OpenTaiko.fCamYOffset = float.IsNaN(value) ? currentCamVMoveChip.fCamScrollStartY : value;

			if (ctCamVMove.IsEnded) {
				ctCamVMove = null;
				OpenTaiko.fCamYOffset = currentCamVMoveChip.fCamScrollEndY;
			}
		}

		if (ctCamHMove != null) //horizontal camera move
		{
			ctCamHMove.Tick();
			float value = 0.0f;
			if (currentCamHMoveChip.strCamEaseType.Equals("IN")) value = easing.EaseIn(ctCamHMove, currentCamHMoveChip.fCamScrollStartX, currentCamHMoveChip.fCamScrollEndX, currentCamHMoveChip.fCamMoveType);
			if (currentCamHMoveChip.strCamEaseType.Equals("OUT")) value = easing.EaseOut(ctCamHMove, currentCamHMoveChip.fCamScrollStartX, currentCamHMoveChip.fCamScrollEndX, currentCamHMoveChip.fCamMoveType);
			if (currentCamHMoveChip.strCamEaseType.Equals("IN_OUT")) value = easing.EaseInOut(ctCamHMove, currentCamHMoveChip.fCamScrollStartX, currentCamHMoveChip.fCamScrollEndX, currentCamHMoveChip.fCamMoveType);
			OpenTaiko.fCamXOffset = float.IsNaN(value) ? currentCamHMoveChip.fCamScrollStartX : value;

			if (ctCamHMove.IsEnded) {
				ctCamHMove = null;
				OpenTaiko.fCamXOffset = currentCamHMoveChip.fCamScrollEndX;
			}
		}

		if (ctCamZoom != null) //camera zoom
		{
			ctCamZoom.Tick();
			float value = 0.0f;
			if (currentCamZoomChip.strCamEaseType.Equals("IN")) value = easing.EaseIn(ctCamZoom, currentCamZoomChip.fCamZoomStart, currentCamZoomChip.fCamZoomEnd, currentCamZoomChip.fCamMoveType);
			if (currentCamZoomChip.strCamEaseType.Equals("OUT")) value = easing.EaseOut(ctCamZoom, currentCamZoomChip.fCamZoomStart, currentCamZoomChip.fCamZoomEnd, currentCamZoomChip.fCamMoveType);
			if (currentCamZoomChip.strCamEaseType.Equals("IN_OUT")) value = easing.EaseInOut(ctCamZoom, currentCamZoomChip.fCamZoomStart, currentCamZoomChip.fCamZoomEnd, currentCamZoomChip.fCamMoveType);
			OpenTaiko.fCamZoomFactor = float.IsNaN(value) ? currentCamZoomChip.fCamZoomStart : value;

			if (ctCamZoom.IsEnded) {
				ctCamZoom = null;
				OpenTaiko.fCamZoomFactor = currentCamZoomChip.fCamZoomEnd;
			}
		}

		if (ctCamRotation != null) //camera rotation
		{
			ctCamRotation.Tick();
			float value = 0.0f;
			if (currentCamRotateChip.strCamEaseType.Equals("IN")) value = easing.EaseIn(ctCamRotation, currentCamRotateChip.fCamRotationStart, currentCamRotateChip.fCamRotationEnd, currentCamRotateChip.fCamMoveType);
			if (currentCamRotateChip.strCamEaseType.Equals("OUT")) value = easing.EaseOut(ctCamRotation, currentCamRotateChip.fCamRotationStart, currentCamRotateChip.fCamRotationEnd, currentCamRotateChip.fCamMoveType);
			if (currentCamRotateChip.strCamEaseType.Equals("IN_OUT")) value = easing.EaseInOut(ctCamRotation, currentCamRotateChip.fCamRotationStart, currentCamRotateChip.fCamRotationEnd, currentCamRotateChip.fCamMoveType);
			OpenTaiko.fCamRotation = float.IsNaN(value) ? currentCamRotateChip.fCamRotationStart : value;

			if (ctCamRotation.IsEnded) {
				ctCamRotation = null;
				OpenTaiko.fCamRotation = currentCamRotateChip.fCamRotationEnd;
			}
		}

		if (ctCamVScale != null) //vertical camera scaling
		{
			ctCamVScale.Tick();
			float value = 0.0f;
			if (currentCamVScaleChip.strCamEaseType.Equals("IN")) value = easing.EaseIn(ctCamVScale, currentCamVScaleChip.fCamScaleStartY, currentCamVScaleChip.fCamScaleEndY, currentCamVScaleChip.fCamMoveType);
			if (currentCamVScaleChip.strCamEaseType.Equals("OUT")) value = easing.EaseOut(ctCamVScale, currentCamVScaleChip.fCamScaleStartY, currentCamVScaleChip.fCamScaleEndY, currentCamVScaleChip.fCamMoveType);
			if (currentCamVScaleChip.strCamEaseType.Equals("IN_OUT")) value = easing.EaseInOut(ctCamVScale, currentCamVScaleChip.fCamScaleStartY, currentCamVScaleChip.fCamScaleEndY, currentCamVScaleChip.fCamMoveType);
			OpenTaiko.fCamYScale = float.IsNaN(value) ? currentCamVScaleChip.fCamScaleStartY : value;

			if (ctCamVScale.IsEnded) {
				ctCamVScale = null;
				OpenTaiko.fCamYScale = currentCamVScaleChip.fCamScaleEndY;
			}
		}

		if (ctCamHScale != null) //horizontal camera scaling
		{
			ctCamHScale.Tick();
			float value = 0.0f;
			if (currentCamHScaleChip.strCamEaseType.Equals("IN")) value = easing.EaseIn(ctCamHScale, currentCamHScaleChip.fCamScaleStartX, currentCamHScaleChip.fCamScaleEndX, currentCamHScaleChip.fCamMoveType);
			if (currentCamHScaleChip.strCamEaseType.Equals("OUT")) value = easing.EaseOut(ctCamHScale, currentCamHScaleChip.fCamScaleStartX, currentCamHScaleChip.fCamScaleEndX, currentCamHScaleChip.fCamMoveType);
			if (currentCamHScaleChip.strCamEaseType.Equals("IN_OUT")) value = easing.EaseInOut(ctCamHScale, currentCamHScaleChip.fCamScaleStartX, currentCamHScaleChip.fCamScaleEndX, currentCamHScaleChip.fCamMoveType);
			OpenTaiko.fCamXScale = float.IsNaN(value) ? currentCamHScaleChip.fCamScaleStartX : value;

			if (ctCamHScale.IsEnded) {
				ctCamHScale = null;
				OpenTaiko.fCamXScale = currentCamHScaleChip.fCamScaleEndX;
			}
		}

		foreach (KeyValuePair<CChip, CCounter> pair in objHandlers) {
			CChip chip = pair.Key;
			CCounter counter = pair.Value;

			if (counter != null) {
				counter.Tick();

				float value = 0.0f;
				if (counter.IsEnded) {
					value = chip.fObjEnd;
					counter = null;
				} else {
					if (chip.strObjEaseType.Equals("IN")) value = easing.EaseIn(counter, chip.fObjStart, chip.fObjEnd, chip.objCalcType);
					if (chip.strObjEaseType.Equals("OUT")) value = easing.EaseOut(counter, chip.fObjStart, chip.fObjEnd, chip.objCalcType);
					if (chip.strObjEaseType.Equals("IN_OUT")) value = easing.EaseInOut(counter, chip.fObjStart, chip.fObjEnd, chip.objCalcType);
					value = float.IsNaN(value) ? chip.fObjStart : value;
				}

				if (chip.nChannelNo == 0xBE) chip.obj.y = value;
				if (chip.nChannelNo == 0xC0) chip.obj.x = value;
				if (chip.nChannelNo == 0xC2) chip.obj.yScale = value;
				if (chip.nChannelNo == 0xC4) chip.obj.xScale = value;
				if (chip.nChannelNo == 0xC6) chip.obj.rotation = value;
				if (chip.nChannelNo == 0xC8) chip.obj.opacity = (int)value;

				if (chip.nChannelNo == 0xCB) chip.obj.y = value;
				if (chip.nChannelNo == 0xCC) chip.obj.x = value;
				if (chip.nChannelNo == 0xCD) chip.obj.yScale = value;
				if (chip.nChannelNo == 0xCE) chip.obj.xScale = value;
				if (chip.nChannelNo == 0xCF) chip.obj.rotation = value;
				if (chip.nChannelNo == 0xD0) chip.obj.opacity = (int)value;
			}
		}
		#endregion

		return false;
	}

	private void AddNowProcessingRollChip(int iPlayer, CChip chip) {
		//if( this.n現在のコース == pChip.nコース )
		if (chip.bVisible == true) {
			int idx = this.chip現在処理中の連打チップ[iPlayer].BinarySearch(chip);
			if (idx < 0) {
				this.chip現在処理中の連打チップ[iPlayer].Insert(~idx, chip);
			}
			if (!chip.IsHitted) {
				if (NotesManager.IsKusudama(chip)) {
					nCurrentKusudamaRollCount = 0;
					nCurrentKusudamaCount += chip.nBalloon;
				}
				if (!this.bPAUSE && !this.isRewinding) {
					this.ProcessRollHeadEffects(iPlayer, chip);
				}
			}
		}
		if (chip.end.bProcessed) { // handle negative-length rolls
			this.ProcessRollEnd(iPlayer, chip, false);
		}
	}

	public void ProcessRollHeadEffects(int iPlayer, CChip chip) {
		if (chip.bProcessed)
			return;
		chip.bProcessed = true;
		if (NotesManager.IsKusudama(chip)) {
			if (this.nowProcessingKusudama == null && iPlayer == 0) {
				this.nowProcessingKusudama = chip;
				actBalloon.KusuIn();
				actChara.KusuIn();
				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					this.bCurrentlyDrumRoll[i] = true;
					this.actChara.b風船連打中[i] = true;
				}
			}
		}
	}

	public void ProcessRollEnd(int iPlayer, CChip chip, bool resetStates) {
		if (NotesManager.IsRollEnd(chip))
			chip = chip.start;
		if (!NotesManager.IsGenericRoll(chip))
			return;

		if (!resetStates)
			chip.bHit = true;
		if (!chip.IsHitted) {
			if (NotesManager.IsGenericBalloon(chip)) {
				if (NotesManager.IsKusudama(chip)) {
					if (iPlayer == 0) {
						if (!this.bPAUSE && !this.isRewinding && actBalloon.KusudamaIsActive) {
							actBalloon.KusuMiss();
							OpenTaiko.Skin.soundKusudamaMiss.tPlay();
							for (int p = 0; p < OpenTaiko.ConfigIni.nPlayerCount; p++) {
								this.actChara.ChangeAnime(p, CActImplCharacter.Anime.Kusudama_Miss, true);

								if (actChara.CharaAction_Balloon_Delay[p] != null) actChara.CharaAction_Balloon_Delay[p] = new CCounter(0,
									OpenTaiko.Skin.Characters_Balloon_Delay[actChara.iCurrentCharacter[p]] - 1,
									1,
									OpenTaiko.Timer);
							}
						}
						nCurrentKusudamaRollCount = 0;
						nCurrentKusudamaCount = 0;
						this.nowProcessingKusudama = null;
					}
				} else {
					if (!this.bPAUSE && !this.isRewinding) {
						if (chip.nRollCount > 0) {
							this.actChara.ChangeAnime(iPlayer, CActImplCharacter.Anime.Balloon_Miss, true);

							if (actChara.CharaAction_Balloon_Delay[iPlayer] != null) actChara.CharaAction_Balloon_Delay[iPlayer] = new CCounter(0,
								OpenTaiko.Skin.Characters_Balloon_Delay[actChara.iCurrentCharacter[iPlayer]] - 1,
								1,
								OpenTaiko.Timer);
						}
					}
				}
				if (NotesManager.IsFuzeRoll(chip)) {
					if (!this.bPAUSE && !this.isRewinding) {
						this.actJudgeString.Start(iPlayer, ENoteJudge.Mine);
						OpenTaiko.stageGameScreen.actLaneTaiko.Start(0x11, ENoteJudge.Bad, true, iPlayer);
						OpenTaiko.stageGameScreen.actChipFireD.Start(0x11, ENoteJudge.Mine, iPlayer);
						actGauge.MineDamage(iPlayer);
						OpenTaiko.Skin.soundBomb?.tPlay();
						this.CChartScore[iPlayer].nMine++;
						this.CSectionScore[iPlayer].nMine++;
						this.CBranchScore[iPlayer].nMine++;
						if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
							CFloorManagement.damage();
						if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
							this.DanSongScore[actDan.NowShowingNumber].nMine++;
						this.actCombo.nCurrentCombo[iPlayer] = 0;
						if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
							this.DanSongScore[actDan.NowShowingNumber].nCombo = 0;
						this.actComboVoice.tReset(iPlayer);
						this.bIsMiss[iPlayer] = true;
					}
				}
			}
		}
		this.RemoveNowProcessingRollChip(iPlayer, chip, resetStates);
	}

	public void ProcessBalloonBroke(int iPlayer, CChip chip) {
		if (NotesManager.IsRollEnd(chip))
			chip = chip.start;
		if (!NotesManager.IsGenericBalloon(chip))
			return;

		if (NotesManager.IsKusudama(chip)) {
			OpenTaiko.Skin.soundKusudama.tPlay();
			chip.bHit = true;
			chip.IsHitted = true;
			chip.bVisible = false;
			nCurrentKusudamaRollCount = 0;
			nCurrentKusudamaCount = 0;
			this.nowProcessingKusudama = null;

			actBalloon.KusuBroke();
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				actChara.ChangeAnime(i, CActImplCharacter.Anime.Kusudama_Broke, true);
				if (actChara.CharaAction_Balloon_Delay[i] != null)
					actChara.CharaAction_Balloon_Delay[i] = new CCounter(0, OpenTaiko.Skin.Characters_Balloon_Delay[actChara.iCurrentCharacter[i]] - 1, 1, OpenTaiko.Timer);
			}
		} else {
			//ﾊﾟｧｰﾝ
			OpenTaiko.Skin.soundBalloon.tPlay();
			//CDTXMania.stage演奏ドラム画面.actChipFireTaiko.Start( 3, player ); //ここで飛ばす。飛ばされるのは大音符のみ。
			OpenTaiko.stageGameScreen.FlyingNotes.Start(3, iPlayer);
			OpenTaiko.stageGameScreen.Rainbow.Start(iPlayer);
			//CDTXMania.stage演奏ドラム画面.actChipFireD.Start( 0, player );
			chip.bHit = true;
			chip.IsHitted = true;
			//this.b連打中 = false;
			//this.actChara.b風船連打中 = false;
			chip.bVisible = false;
			{
				actChara.ChangeAnime(iPlayer, CActImplCharacter.Anime.Balloon_Broke, true);
				if (actChara.CharaAction_Balloon_Delay[iPlayer] != null)
					actChara.CharaAction_Balloon_Delay[iPlayer] = new CCounter(0, OpenTaiko.Skin.Characters_Balloon_Delay[actChara.iCurrentCharacter[iPlayer]] - 1, 1, OpenTaiko.Timer);
			}
			if (NotesManager.IsFuzeRoll(chip)) {
				this.CChartScore[iPlayer].nMineAvoid++;
				this.CSectionScore[iPlayer].nMineAvoid++;
				this.CBranchScore[iPlayer].nMineAvoid++;
				if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
					this.DanSongScore[actDan.NowShowingNumber].nMineAvoid++;
			}
		}
		this.RemoveNowProcessingRollChip(iPlayer, chip, false);
	}

	private void RemoveNowProcessingRollChip(int iPlayer, CChip chip, bool resetStates) {
		if (NotesManager.IsRollEnd(chip))
			chip = chip.start;

		if (NotesManager.IsKusudama(chip) && this.actBalloon.KusudamaIsActive) {
			this.actBalloon.KusuMiss();
		}

		if (!resetStates && !chip.end.bProcessed && chip.end.bVisible) {
			if (NotesManager.IsGenericBalloon(chip)) {
				this.CChartScore[iPlayer].nBalloonHitPass += chip.nBalloon;
				this.CSectionScore[iPlayer].nBalloonHitPass += chip.nBalloon;
				this.CBranchScore[iPlayer].nBalloonHitPass += chip.nBalloon;
				if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
					this.DanSongScore[actDan.NowShowingNumber].nBalloonHitPass += chip.nBalloon;
			} else {
				this.CChartScore[iPlayer].nBarRollPass++;
				this.CSectionScore[iPlayer].nBarRollPass++;
				this.CBranchScore[iPlayer].nBarRollPass++;
				if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
					this.DanSongScore[actDan.NowShowingNumber].nBarRollPass++;

				double msRollLength = chip.end.n発声時刻ms - chip.n発声時刻ms;
				this.CChartScore[iPlayer].msBarRollPass += msRollLength;
				this.CSectionScore[iPlayer].msBarRollPass += msRollLength;
				this.CBranchScore[iPlayer].msBarRollPass += msRollLength;
				if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
					this.DanSongScore[actDan.NowShowingNumber].msBarRollPass += msRollLength;
			}
			this.actDan.Update();
		}

		this.chip現在処理中の連打チップ[iPlayer].Remove(chip);
		if (this.chip現在処理中の連打チップ[iPlayer].Count == 0) {
			this.bCurrentlyDrumRoll[iPlayer] = false;
			this.eRollState = ERollState.None;
		} else if (!this.chip現在処理中の連打チップ[iPlayer].Any(x => NotesManager.IsGenericBalloon(x))) {
			this.actChara.b風船連打中[iPlayer] = false;
		}
		if (resetStates || (!this.bPAUSE && !this.isRewinding)) {
			chip.bProcessed = !resetStates;
			chip.end.bProcessed = !resetStates;
		}
	}

	public void StartGoGoTimeEffect(int iPlayer) {
		int Character = this.actChara.iCurrentCharacter[iPlayer];

		{
			if (OpenTaiko.Skin.Characters_GoGoStart_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[iPlayer].IsEnded) {
				if (!HGaugeMethods.UNSAFE_IsRainbow(iPlayer) && (!HGaugeMethods.UNSAFE_FastNormaCheck(iPlayer) || OpenTaiko.Skin.Characters_GoGoStart_Clear_Ptn[Character] == 0)) {
					// 魂ゲージMAXではない
					// ゴーゴースタート_ノーマル
					this.actChara.ChangeAnime(iPlayer, CActImplCharacter.Anime.GoGoStart, true);
					//this.actChara.キャラクター_アクション_10コンボ();
				}
			}
			if (OpenTaiko.Skin.Characters_GoGoStart_Clear_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[iPlayer].IsEnded) {
				if (!HGaugeMethods.UNSAFE_IsRainbow(iPlayer) && HGaugeMethods.UNSAFE_FastNormaCheck(iPlayer)) {
					this.actChara.ChangeAnime(iPlayer, CActImplCharacter.Anime.GoGoStart_Clear, true);
				}
			}
			if (OpenTaiko.Skin.Characters_GoGoStart_Maxed_Ptn[Character] != 0 && actChara.CharaAction_Balloon_Delay[iPlayer].IsEnded) {
				if (HGaugeMethods.UNSAFE_IsRainbow(iPlayer)) {
					// 魂ゲージMAX
					// ゴーゴースタート_MAX
					this.actChara.ChangeAnime(iPlayer, CActImplCharacter.Anime.GoGoStart_Max, true);
				}
			}

		}
		OpenTaiko.stageGameScreen.actLaneTaiko.GOGOSTART();
	}

	public void tBranchReset(int player) {
		if (player != -1) {
			this.CBranchScore[player].cBigNotes.nGreat = 0;
			this.CBranchScore[player].cBigNotes.nGood = 0;
			this.CBranchScore[player].cBigNotes.nMiss = 0;
			this.CBranchScore[player].cBigNotes.nRoll = 0;

			this.CBranchScore[player].nGreat = 0;
			this.CBranchScore[player].nGood = 0;
			this.CBranchScore[player].nMiss = 0;
			this.CBranchScore[player].nRoll = 0;
		} else {
			for (int i = 0; i < CBranchScore.Length; i++) {
				this.CBranchScore[i].cBigNotes.nGreat = 0;
				this.CBranchScore[i].cBigNotes.nGood = 0;
				this.CBranchScore[i].cBigNotes.nMiss = 0;
				this.CBranchScore[i].cBigNotes.nRoll = 0;

				this.CBranchScore[i].nGreat = 0;
				this.CBranchScore[i].nGood = 0;
				this.CBranchScore[i].nMiss = 0;
				this.CBranchScore[i].nRoll = 0;
			}
		}
	}

	public void tBranchJudge(CChip pChip, CBRANCHSCORE cBRANCHSCORE, int nスコア, int n連打数, int n良, int n可, int n不可, int nPlayer) {
		// Branch check score here

		if (this.b強制的に分岐させた[nPlayer]) return;

		var e種類 = pChip.eBranchCondition;

		//分岐の仕方が同じなので一緒にしていいと思う。
		var b分岐種類が一致 = e種類 == CTja.EBranchConditionType.Accuracy || e種類 == CTja.EBranchConditionType.Score;


		double dbRate = 0;

		if (e種類 == CTja.EBranchConditionType.Accuracy) {
			if ((n良 + n可 + n不可) != 0) {
				dbRate = (((double)n良 + (double)n可 * 0.5) / (double)(n良 + n可 + n不可)) * 100.0;
			}
		} else if (e種類 == CTja.EBranchConditionType.Score) {
			dbRate = nスコア;
		} else if (e種類 == CTja.EBranchConditionType.Drumroll) {
			dbRate = n連打数;
		} else if (e種類 == CTja.EBranchConditionType.Accuracy_BigNotesOnly) {
			dbRate = cBRANCHSCORE.nGreat;
		}


		if (b分岐種類が一致) {
			if (dbRate < pChip.nBranchCondition1_Professional) {
				this.nDisplayedBranchLane[nPlayer] = CTja.ECourse.eNormal;
				this.nNextBranch[nPlayer] = CTja.ECourse.eNormal;
			} else if (dbRate >= pChip.nBranchCondition1_Professional && dbRate < pChip.nBranchCondition2_Master) {
				this.nDisplayedBranchLane[nPlayer] = CTja.ECourse.eExpert;
				this.nNextBranch[nPlayer] = CTja.ECourse.eExpert;
			} else if (dbRate >= pChip.nBranchCondition2_Master) {
				this.nDisplayedBranchLane[nPlayer] = CTja.ECourse.eMaster;
				this.nNextBranch[nPlayer] = CTja.ECourse.eMaster;
			}

		} else if (e種類 == CTja.EBranchConditionType.Drumroll) {
			if (!(pChip.nBranchCondition1_Professional == 0 && pChip.nBranchCondition2_Master == 0)) {
				if (dbRate < pChip.nBranchCondition1_Professional) {
					this.nDisplayedBranchLane[nPlayer] = CTja.ECourse.eNormal;
					this.nNextBranch[nPlayer] = CTja.ECourse.eNormal;
				} else if (dbRate >= pChip.nBranchCondition1_Professional && dbRate < pChip.nBranchCondition2_Master) {
					this.nDisplayedBranchLane[nPlayer] = CTja.ECourse.eExpert;
					this.nNextBranch[nPlayer] = CTja.ECourse.eExpert;
				} else if (dbRate >= pChip.nBranchCondition2_Master) {
					this.nDisplayedBranchLane[nPlayer] = CTja.ECourse.eMaster;
					this.nNextBranch[nPlayer] = CTja.ECourse.eMaster;
				}
			}
		} else if (e種類 == CTja.EBranchConditionType.Accuracy_BigNotesOnly) {
			if (!(pChip.nBranchCondition1_Professional == 0 && pChip.nBranchCondition2_Master == 0)) {
				if (dbRate < pChip.nBranchCondition1_Professional) {
					this.nDisplayedBranchLane[nPlayer] = CTja.ECourse.eNormal;
					this.nNextBranch[nPlayer] = CTja.ECourse.eNormal;
				} else if (dbRate >= pChip.nBranchCondition1_Professional && dbRate < pChip.nBranchCondition2_Master) {
					this.nDisplayedBranchLane[nPlayer] = CTja.ECourse.eExpert;
					this.nNextBranch[nPlayer] = CTja.ECourse.eExpert;
				} else if (dbRate >= pChip.nBranchCondition2_Master) {
					this.nDisplayedBranchLane[nPlayer] = CTja.ECourse.eMaster;
					this.nNextBranch[nPlayer] = CTja.ECourse.eMaster;
				}
			}
		}
	}

	private CTja.ECourse[] E強制コース = new CTja.ECourse[5];
	private void t強制用条件かを判断する(double db条件A, double db条件B, int nPlayer) {
		//Wiki参考
		//成仏

		if (db条件A == 101 && db条件B == 102) //強制普通譜面
		{
			E強制コース[nPlayer] = CTja.ECourse.eNormal;
			this.b強制分岐譜面[nPlayer] = true;
		} else if (db条件A == -1 && db条件B == 101)  //強制玄人譜面
		{
			E強制コース[nPlayer] = CTja.ECourse.eExpert;
			this.b強制分岐譜面[nPlayer] = true;
		} else if (db条件A == -2 && db条件B == -1)   //強制達人譜面
		{
			E強制コース[nPlayer] = CTja.ECourse.eMaster;
			this.b強制分岐譜面[nPlayer] = true;
		}
	}

	public void t分岐処理(CTja.ECourse n分岐先, int nPlayer, double n発声位置, CTja.EBranchConditionType e分岐種類 = CTja.EBranchConditionType.Accuracy) {

		CTja dTX = OpenTaiko.GetTJA(nPlayer)!;

		for (int A = 0; A < dTX.listChip.Count; A++) {
			var Chip = dTX.listChip[A].nChannelNo;
			var _chip = dTX.listChip[A];

			var bDontDeleteFlag = NotesManager.IsHittableNote(_chip);// Chip >= 0x11 && Chip <= 0x19;
			var bRollAllFlag = NotesManager.IsGenericRoll(_chip);//Chip >= 0x15 && Chip <= 0x19;
			var bBalloonOnlyFlag = NotesManager.IsGenericBalloon(_chip);//Chip == 0x17;
			var bRollOnlyFlag = NotesManager.IsRoll(_chip);//Chip >= 0x15 && Chip <= 0x16;

			if (bDontDeleteFlag) {
				if (dTX.listChip[A].n発声時刻ms > n発声位置) {
					if (dTX.listChip[A].nBranch == n分岐先) {
						dTX.listChip[A].bVisible = true;

						if (dTX.listChip[A].IsEndedBranching) {
							if (bRollAllFlag)//共通譜面時かつ、連打譜面だったら非可視化
							{
								dTX.listChip[A].bHit = true;
								dTX.listChip[A].bShow = false;
								dTX.listChip[A].bVisible = false;
							}
						}
					} else {
						if (!dTX.listChip[A].IsEndedBranching)
							dTX.listChip[A].bVisible = false;
					}
					//共通なため分岐させない.
					dTX.listChip[A].eNoteState = ENoteState.None;

					if (dTX.listChip[A].IsEndedBranching && (dTX.listChip[A].nBranch == CTja.ECourse.eNormal)) {
						if (bRollOnlyFlag)//共通譜面時かつ、連打譜面だったら可視化
						{
							dTX.listChip[A].bHit = false;
							dTX.listChip[A].bShow = true;
							dTX.listChip[A].bVisible = true;
						} else {
							if (bBalloonOnlyFlag)//共通譜面時かつ、風船譜面だったら可視化
							{
								dTX.listChip[A].bShow = true;
								dTX.listChip[A].bVisible = true;
							}
						}
					}
				}
			}
		}
	}

	public int GetRoll(int player) {
		return this.CChartScore[player].nRoll;
	}

	protected float GetNowPBMTime(CTja tja, float play_time) {
		float bpm_time = 0;
		int last_input = 0;
		float last_bpm_change_time;
		play_time = (float)tja.TjaTimeToRawTjaTimeNote(tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs));

		for (int i = 1; ; i++) {
			//BPMCHANGEの数越えた
			if (i >= tja.listBPM.Count) {
				CTja.CBPM cBPM = tja.listBPM[last_input];
				bpm_time = (float)cBPM.bpm_change_bmscroll_time + ((play_time - (float)cBPM.bpm_change_time) * (float)cBPM.dbBPM値 / 15000.0f);
				last_bpm_change_time = (float)cBPM.bpm_change_time;
				break;
			}
			for (; i < tja.listBPM.Count; i++) {
				CTja.CBPM cBPM = tja.listBPM[i];
				if (cBPM.bpm_change_time == 0 || cBPM.bpm_change_course == this.nCurrentBranch[0]) {
					break;
				}
			}
			if (i == tja.listBPM.Count) {
				i = tja.listBPM.Count - 1;
				continue;
			}

			if (play_time < tja.listBPM[i].bpm_change_time) {
				CTja.CBPM cBPM = tja.listBPM[last_input];
				bpm_time = (float)cBPM.bpm_change_bmscroll_time + ((play_time - (float)cBPM.bpm_change_time) * (float)cBPM.dbBPM値 / 15000.0f);
				last_bpm_change_time = (float)cBPM.bpm_change_time;
				break;
			} else {
				last_input = i;
			}
		}

		return bpm_time;
	}

	public void t再読込() {
		OpenTaiko.TJA.t全チップの再生停止とミキサーからの削除();
		this.eフェードアウト完了時の戻り値 = EGameplayScreenReturnValue.ReloadAndReplay;
		base.ePhaseID = CStage.EPhase.Game_Reload;
		this.bPAUSE = false;
	}

	public void t演奏やりなおし() {
		OpenTaiko.TJA.t全チップの再生停止とミキサーからの削除();
		//this.actAVI.Stop();
		foreach (var vd in OpenTaiko.TJA.listVD) {
			vd.Value.Stop();
		}
		this.actAVI.Stop();
		this.actPanel.t歌詞テクスチャを削除する();
		var cleared = (bool[])bIsAlreadyCleared.Clone();
		this.t数値の初期化(true, true);
		this.t演奏位置の変更(0);
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			if (!bIsAlreadyCleared[i] && cleared[i]) {
				OpenTaiko.stageGameScreen.actBackground.ClearOut(i);
			}
		}
		this.bPAUSE = false;
	}

	public void t停止() {
		OpenTaiko.TJA.t全チップの再生停止とミキサーからの削除();
		foreach (var vd in OpenTaiko.TJA.listVD) {
			vd.Value.Stop();
		}
		this.actAVI.Stop();
		this.actPanel.Stop();               // PANEL表示停止
		OpenTaiko.Timer.Pause();       // 再生時刻カウンタ停止

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i)
			this.nCurrentTopChip[i] = OpenTaiko.GetTJA(i)!.listChip.Count - 1;   // 終端にシーク

		// 自分自身のOn活性化()相当の処理もすべき。
	}

	public virtual void t数値の初期化(bool b演奏記録, bool b演奏状態) {
		this.isRewinding = true;

		if (b演奏記録) {
			this.b演奏にキーボードを使った = false;
			this.b演奏にジョイパッドを使った = false;
			this.b演奏にMIDI入力を使った = false;
			this.b演奏にマウスを使った = false;

			this.nHitCount_InclAuto.Taiko.Perfect = 0;
			this.nHitCount_InclAuto.Taiko.Great = 0;
			this.nHitCount_InclAuto.Taiko.Good = 0;
			this.nHitCount_InclAuto.Taiko.Poor = 0;
			this.nHitCount_InclAuto.Taiko.Miss = 0;

			this.nHitCount_ExclAuto.Taiko.Perfect = 0;
			this.nHitCount_ExclAuto.Taiko.Great = 0;
			this.nHitCount_ExclAuto.Taiko.Good = 0;
			this.nHitCount_ExclAuto.Taiko.Poor = 0;
			this.nHitCount_ExclAuto.Taiko.Miss = 0;

			this.actCombo.Activate();
			this.actScore.Activate();
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				this.actGauge.Init(OpenTaiko.ConfigIni.nRisky, i);
			}
		}
		if (b演奏状態) {
			_AIBattleStateBatch = new Queue<float>[] { new Queue<float>(), new Queue<float>() };
			bIsAIBattleWin = false;

			nCurrentKusudamaCount = 0;
			nCurrentKusudamaRollCount = 0;

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				this.Chara_MissCount[i] = 0;
				this.bIsMiss[i] = false;
				this.bUseBranch[i] = false;
				this.bLEVELHOLD[i] = false;
				this.b強制的に分岐させた[i] = false;
				this.nCurrentBranch[i] = CTja.ECourse.eNormal;
				this.nNextBranch[i] = CTja.ECourse.eNormal;
				this.nDisplayedBranchLane[0] = CTja.ECourse.eNormal;
				this.nCurrentRollCount[i] = 0;

				OpenTaiko.GetTJA(i)?.tInitLocalStores(i);

				var chara = OpenTaiko.Tx.Characters[OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(i)].data.Character];
				switch (chara.effect.tGetGaugeType()) {
					default:
					case "Normal":
						bIsAlreadyCleared[i] = false;
						break;
					case "Hard":
					case "Extreme":
						bIsAlreadyCleared[i] = true;
						break;
				}

				#region [ 演奏済みフラグのついたChipをリセットする ]
				foreach (var chip in OpenTaiko.GetTJA(i)!.listNoteChip) {
					chip.bHit = false;
					chip.bShow = true;
					chip.bShowRoll = true;
					chip.bProcessed = false;
					chip.bVisible = true;
					chip.IsHitted = false;
					chip.IsMissed = false;
					chip.eNoteState = ENoteState.None;
					chip.nProcessTime = 0;
					chip.nRollCount = 0;
					chip.nRollCount = 0;
					chip.ResetRollEffect();
				}
				#endregion
			}
			for (int i = 0; i < 5; i++) {
				this.CChartScore[i] = new CBRANCHSCORE();
				this.CSectionScore[i] = new CBRANCHSCORE();

				this.actComboVoice.tReset(i);
				this.ifp[i] = false;
				this.isDeniedPlaying[i] = false;
			}

			this.tBranchReset(-1);

			this.nBranch条件数値A = 0;
			this.nBranch条件数値B = 0;

			this.ePhaseID = CStage.EPhase.Common_NORMAL;//初期化すれば、リザルト変遷は止まる。
			this.eフェードアウト完了時の戻り値 = EGameplayScreenReturnValue.Continue;

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i) {
				CTja tja = OpenTaiko.GetTJA(i)!;
				this.ReSetScore(tja.nScoreInit[0, OpenTaiko.stageSongSelect.nChoosenSongDifficulty[i]], tja.nScoreDiff[OpenTaiko.stageSongSelect.nChoosenSongDifficulty[i]], i);
			}
			this.nHand = new int[] { 0, 0, 0, 0, 0 };
		}

		// rewind nCurrentTopChip
		int[] iPrevTopChip = this.nCurrentTopChip.Copy();
		int iPrevTopChipMax = this.nCurrentTopChip.Max();
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i)
			this.nCurrentTopChip[i] = (this.listChip[i].Count > 0) ? 0 : -1;

		if (!b演奏状態 && iPrevTopChipMax <= 0)
			return; // no needs to reset

		// reset accumulated chip state
		_AIBattleState = 0;

		NowAIBattleSectionCount = 0;
		NowAIBattleSectionTime = 0;

		CFloorManagement.reload();

		for (int i = 0; i < AIBattleSections.Count; i++) {
			AIBattleSections[i].End = AIBattleSection.EndType.None;
			AIBattleSections[i].IsAnimated = false;
		}

		OpenTaiko.fCamXOffset = 0;

		OpenTaiko.fCamYOffset = 0;

		OpenTaiko.fCamZoomFactor = 1.0f;
		OpenTaiko.fCamRotation = 0;

		OpenTaiko.fCamXScale = 1.0f;
		OpenTaiko.fCamYScale = 1.0f;

		OpenTaiko.borderColor = new Color4(1f, 0f, 0f, 0f);

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			CTja tja = OpenTaiko.GetTJA(i)!;

			this.bWasGOGOTIME[i] = this.bIsGOGOTIME[i];
			this.bIsGOGOTIME[i] = false;
			this.bBranchedChart[i] = false;
			this.n分岐した回数[i] = 0;

			this.actPlayInfo.dbBPM[i] = tja.BASEBPM;
			this.UpdateCharaCounter(i);

			this.actPlayInfo.NowMeasure[i] = 0;
			this.JPOSCROLLX[i] = 0;
			this.JPOSCROLLY[i] = 0;

			OpenTaiko.ConfigIni.nGameType[i] = this.eFirstGameType[i];
			this.bSplitLane[i] = false;
			this.msCurrentBarRollProgress[i] = 0;

			for (int iChip = this.chip現在処理中の連打チップ[i].Count; iChip-- > 0;) {
				var chip = this.chip現在処理中の連打チップ[i][iChip];
				this.ProcessRollEnd(i, chip, true);
				chip.bProcessed = false;
			}
			this.bCurrentlyDrumRoll[i] = false;
			this.actChara.ReturnDefaultAnime(i, true);

			for (int iChip = 0; iChip < iPrevTopChip[i]; ++iChip) {
				CChip chip = tja.listChip[iChip];
				if (!NotesManager.IsHittableNote(chip))
					chip.bHit = false;
			}
		}

		foreach (var chip in this.objHandlers.Keys) {
			if (chip.obj == null) continue;
			chip.obj.isVisible = false;
			chip.obj.yScale = 1.0f;
			chip.obj.xScale = 1.0f;
			chip.obj.rotation = 0.0f;
			chip.obj.opacity = 255;
			chip.obj.frame = 0;
		}
		this.objHandlers.Clear();

		this.actAVI.rVD = null;
		if ((OpenTaiko.TJA.listVD.TryGetValue(1, out CVideoDecoder vd2))) {
			ShowVideo = true;
		} else {
			ShowVideo = false;
		}

		dtLastQueueOperation = DateTime.MinValue;

		this.nStoredHit = new int[OpenTaiko.ConfigIni.nPlayerCount];
	}

	// returns the chip index at the target measure of the first player
	public int t演奏位置の変更(int nStartBar) {
		// まず全サウンドオフにする
		OpenTaiko.TJA.tStopAllChips();
		this.actAVI.Stop();
		if (OpenTaiko.TJA == null) return 0; //CDTXがnullの場合はプレイヤーが居ないのでその場で処理終了

		#region [ 再生開始小節の変更 ]
		//nStartBar++;									// +1が必要

		CTja dTX = OpenTaiko.TJA;
		#region [ 処理を開始するチップの特定 ]
		int iTargetChip = dTX.GetListChipIndexOfMeasure(nStartBar);
		#endregion
		#region [ 演奏開始の発声時刻msを取得し、タイマに設定 ]
		int nStartTime = (nStartBar == 0) ? 0
			: ((int)dTX.TjaTimeToGameTime(dTX.listChip[iTargetChip].n発声時刻ms) - OpenTaiko.ConfigIni.MusicPreTimeMs);

		int[] iLastChipAtStart = new int[OpenTaiko.MAX_PLAYERS];

		iLastChipAtStart[0] = iTargetChip;
		for (int nPlayer = 0; nPlayer < OpenTaiko.ConfigIni.nPlayerCount; ++nPlayer) {
			CTja tjai = OpenTaiko.GetTJA(nPlayer)!;
			int msStartTjaTime = (int)tjai.GameTimeToTjaTime(nStartTime);
			if (nPlayer != 0) {
				CChip targetDummy = new() { nChannelNo = CChip.nChannelNoLeastPrior, n発声時刻ms = msStartTjaTime };
				iLastChipAtStart[nPlayer] = tjai.listChip.BinarySearch(targetDummy);
				if (iLastChipAtStart[nPlayer] < 0)
					iLastChipAtStart[nPlayer] = int.Max(0, ~iLastChipAtStart[nPlayer] - 1);
			}
			// re-seek for the correct last-played chip at target time
			while (iLastChipAtStart[nPlayer] > 0 && !hasChipBeenPlayedAt(tjai.listChip[iLastChipAtStart[nPlayer]], msStartTjaTime))
				iLastChipAtStart[nPlayer]--;
			// forward to cover simultaneous chips
			while (iLastChipAtStart[nPlayer] + 1 < tjai.listChip.Count && hasChipBeenPlayedAt(tjai.listChip[iLastChipAtStart[nPlayer] + 1], msStartTjaTime))
				iLastChipAtStart[nPlayer]++;
		}

		for (int nPlayer = 0; nPlayer < OpenTaiko.ConfigIni.nPlayerCount; ++nPlayer) {
			CTja tjai = OpenTaiko.GetTJA(nPlayer)!;
			CChip? lastChipAtNow = tjai.listChip.ElementAtOrDefault(OpenTaiko.stageGameScreen.nCurrentTopChip[nPlayer] - 1);
			if (lastChipAtNow != null && !hasChipBeenPlayedAt(lastChipAtNow, tjai.GameTimeToTjaTime(nStartTime))) {
				OpenTaiko.stageGameScreen.t数値の初期化(false, false); // rewind
				break;
			}
		}

		SoundManager.PlayTimer.Reset(); // これでPAUSE解除されるので、次のPAUSEチェックは不要
										//if ( !this.bPAUSE )
										//{
		SoundManager.PlayTimer.Pause();
		//}
		SoundManager.PlayTimer.NowTimeMs = nStartTime;
		#endregion

		List<CSound> pausedCSound = new List<CSound>();

		#region [ BGMやギターなど、演奏開始のタイミングで再生がかかっているサウンドのの途中再生開始 ] // (CDTXのt入力_行解析_チップ配置()で小節番号が+1されているのを削っておくこと)
		for (int nPlayer = 0; nPlayer < OpenTaiko.ConfigIni.nPlayerCount; ++nPlayer) {
			CTja tjai = OpenTaiko.GetTJA(nPlayer)!;
			for (int i = 0; i <= iLastChipAtStart[nPlayer]; ++i) {
				CChip pChip = tjai.listChip[i];
				int nDuration = (int)CTja.TjaDurationToGameDuration(pChip.GetDuration());
				long n発声時刻ms = (long)tjai.TjaTimeToGameTime(pChip.n発声時刻ms);
				if (n発声時刻ms <= nStartTime) {
					if (pChip.nChannelNo == 0x01 && (pChip.nChannelNo >> 4) != 0xB) // wav系チャンネル、且つ、空打ちチップではない
					{
						pChip.bHit = true;
						if (!((nDuration > 0) && (nStartTime <= n発声時刻ms + nDuration)))
							continue;

						CTja.CWAV wc;
						bool b = tjai.listWAV.TryGetValue(pChip.n整数値_内部番号, out wc);
						if (!b) continue;

						if ((wc.bIsBGMSound && OpenTaiko.ConfigIni.bBGMPlayVoiceSound) || (!wc.bIsBGMSound)) {
							tjai.tチップの再生(pChip, SoundManager.PlayTimer.GameTimeToSystemTime((long)tjai.TjaTimeToGameTime(pChip.n発声時刻ms)));
							#region [ PAUSEする ]
							int j = wc.n現在再生中のサウンド番号;
							if (wc.rSound[j] != null) {
								wc.rSound[j].Pause();
								wc.rSound[j].tSetPositonToBegin(nStartTime - n発声時刻ms);
								pausedCSound.Add(wc.rSound[j]);
							}
							#endregion
						}
					}
				}
			}
			#endregion
		}
		#region [ PAUSEしていたサウンドを一斉に再生再開する(ただしタイマを止めているので、ここではまだ再生開始しない) ]

		if (!(OpenTaiko.ConfigIni.bNoAudioIfNot1xSpeed && OpenTaiko.ConfigIni.nSongSpeed != 20))
			foreach (CSound cs in pausedCSound) {
				cs.tPlaySound();
			}
		#endregion
		pausedCSound.Clear();
		#region [ タイマを再開して、PAUSEから復帰する ]
		SoundManager.PlayTimer.NowTimeMs = nStartTime;
		OpenTaiko.Timer.Reset();                       // これでPAUSE解除されるので、3行先の再開()は不要
		OpenTaiko.Timer.NowTimeMs = nStartTime;              // Debug表示のTime: 表記を正しくするために必要
		SoundManager.PlayTimer.Resume();
		//CDTXMania.Timer.t再開();
		this.bPAUSE = false;                                // システムがPAUSE状態だったら、強制解除
		this.actPanel.Start();
		#endregion
		#endregion

		return iTargetChip;
	}

	public void t演奏中止() {
		this.actFO.tフェードアウト開始();
		base.ePhaseID = CStage.EPhase.Common_FADEOUT;
		this.eフェードアウト完了時の戻り値 = EGameplayScreenReturnValue.PerformanceInterrupted;
	}

	/// <summary>
	/// DTXV用の設定をする。(全AUTOなど)
	/// 元の設定のバックアップなどはしないので、あとでConfig.iniを上書き保存しないこと。
	/// </summary>
	protected void tDTXV用の設定() {

	}

	protected abstract void t進行描画_チップ_ドラムス(CConfigIni configIni, ref CTja dTX, ref CChip pChip);
	protected abstract void t進行描画_チップ本体_ドラムス(CConfigIni configIni, ref CTja dTX, ref CChip pChip);
	protected abstract void t進行描画_チップ_Taiko(CConfigIni configIni, ref CTja dTX, ref CChip pChip, int nPlayer);
	protected abstract void t進行描画_チップ_Taiko連打(CConfigIni configIni, ref CTja dTX, ref CChip pChip, int nPlayer);

	protected abstract void t進行描画_チップ_フィルイン(CConfigIni configIni, ref CTja dTX, ref CChip pChip);
	protected abstract void t進行描画_チップ_小節線(CConfigIni configIni, ref CTja dTX, ref CChip pChip, int nPlayer);
	protected void t進行描画_チップアニメ() {
		for (int i = 0; i < 5; i++) {
			ctChipAnime[i].TickLoopDB();
			ctChipAnimeLag[i].Tick();
		}
	}

	protected bool t進行描画_フェードイン_アウト() {
		switch (base.ePhaseID) {
			case CStage.EPhase.Common_FADEIN:
				if (this.actFI.Draw() != 0) {
					base.ePhaseID = CStage.EPhase.Common_NORMAL;
				}
				break;

			case CStage.EPhase.Common_FADEOUT:
			case CStage.EPhase.Game_STAGE_FAILED_FadeOut:
				if (this.actFO.Draw() != 0) {
					return true;
				}
				break;

			case CStage.EPhase.Game_STAGE_CLEAR_FadeOut:
				if (this.actFOClear.Draw() == 0) {
					break;
				}
				return true;

		}
		return false;
	}

	protected void t進行描画_演奏情報() {
		if (!OpenTaiko.ConfigIni.bDoNotDisplayPerformanceInfos) {
			this.actPlayInfo.Draw();
		}
	}
	protected bool t進行描画_背景() {
		if (this.txBgImage != null) {
			this.txBgImage.t2D描画(0, 0);
			return true;
		}
		return false;
	}

	protected void t進行描画_判定文字列1_通常位置指定の場合() {
		if (((EJudgeTextDisplayPosition)OpenTaiko.ConfigIni.JudgeTextDisplayPosition.Drums) != EJudgeTextDisplayPosition.BelowCombo)    // 判定ライン上または横
		{
			this.actJudgeString.Draw();
		}
	}

	protected void t進行描画_譜面スクロール速度() {
		this.actScrollSpeed.Draw();
	}
	protected abstract void t紙吹雪_開始();
	protected abstract void t背景テクスチャの生成();
	protected void t背景テクスチャの生成(string DefaultBgFilename, Rectangle bgrect, string bgfilename) {
		try {
			if (!String.IsNullOrEmpty(bgfilename))
				this.txBgImage = OpenTaiko.tテクスチャの生成(OpenTaiko.stageSongSelect.r確定されたスコア.ファイル情報.フォルダの絶対パス + bgfilename);
			else
				this.txBgImage = OpenTaiko.tテクスチャの生成(CSkin.Path(DefaultBgFilename));
		} catch (Exception e) {
			Trace.TraceError(e.ToString());
			Trace.TraceError("例外が発生しましたが処理を継続します。 (a80767e1-4de7-4fec-b072-d078b3659e62)");
			this.txBgImage = null;
		}
	}

	private int nDice = 0;

	public ENoteJudge AlterJudgement(int player, ENoteJudge judgement, bool reroll) {
		int AILevel = OpenTaiko.ConfigIni.nAILevel;
		if (OpenTaiko.ConfigIni.bAIBattleMode && player == 1) {
			if (reroll)
				nDice = OpenTaiko.Random.Next(1000);

			if (nDice < OpenTaiko.ConfigIni.apAIPerformances[AILevel - 1].nBadOdds)
				return ENoteJudge.Poor;
			else if (nDice - OpenTaiko.ConfigIni.apAIPerformances[AILevel - 1].nBadOdds
					 < OpenTaiko.ConfigIni.apAIPerformances[AILevel - 1].nGoodOdds)
				return ENoteJudge.Good;
		}
		return judgement;
	}

	public void ReSetScore(int scoreInit, int scoreDiff, int iPlayer) {
		//一打目の処理落ちがひどいので、あらかじめここで点数の計算をしておく。
		// -1だった場合、その前を引き継ぐ。
		int nInit = scoreInit != -1 ? scoreInit : this.nScore[iPlayer, 0];
		int nDiff = scoreDiff != -1 ? scoreDiff : this.nScore[iPlayer, 1] - this.nScore[iPlayer, 0];
		int nAddScore = 0;
		int[] n倍率 = { 0, 1, 2, 4, 8 };

		if (this.scoreMode[iPlayer] == 1) {
			for (int i = 0; i < 11; i++) {
				this.nScore[iPlayer, i] = (int)(nInit + (nDiff * (i)));
			}
		} else if (this.scoreMode[iPlayer] == 2) {
			for (int i = 0; i < 5; i++) {
				this.nScore[iPlayer, i] = (int)(nInit + (nDiff * n倍率[i]));

				this.nScore[iPlayer, i] = (int)(this.nScore[iPlayer, i] / 10.0);
				this.nScore[iPlayer, i] = this.nScore[iPlayer, i] * 10;

			}
		}
	}


	#region [EXTENDED COMMANDS]
	private CCounter ctCamVMove;
	private CCounter ctCamHMove;
	private CCounter ctCamZoom;
	private CCounter ctCamRotation;
	private CCounter ctCamVScale;
	private CCounter ctCamHScale;

	private CChip currentCamVMoveChip;
	private CChip currentCamHMoveChip;
	private CChip currentCamZoomChip;
	private CChip currentCamRotateChip;
	private CChip currentCamVScaleChip;
	private CChip currentCamHScaleChip;

	private Dictionary<CChip, CCounter> camHandlers;
	private Dictionary<CChip, CCounter> objHandlers;

	private Easing easing = new Easing();

	public bool bCustomDoron = false;
	private bool bConfigUpdated = false;
	#endregion
}
