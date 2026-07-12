using System.Diagnostics;
using System.Drawing;
using FDK;
using FDK.ExtensionMethods;

namespace OpenTaiko;

/// <summary>
/// 演奏画面の共通クラス (ドラム演奏画面, ギター演奏画面の継承元)
/// </summary>
internal abstract class CStagePlayScreenCommon : CStage {
	// Properties

	// メソッド

	#region [ t演奏結果を格納する_ドラム() ]
	public void tPlayResultStore_Drums(out CScoreIni.CPlayRecord Drums) {
		Drums = new CScoreIni.CPlayRecord();

		{
			Drums.nGoodCount = OpenTaiko.ConfigIni.bAutoPlay[0] ? this.nHitCount_InclAuto.Perfect : this.nHitCount_ExclAuto.Perfect;
			Drums.nOkCount = OpenTaiko.ConfigIni.bAutoPlay[0] ? this.nHitCount_InclAuto.Great : this.nHitCount_ExclAuto.Great;
			Drums.nBadCount = OpenTaiko.ConfigIni.bAutoPlay[0] ? this.nHitCount_InclAuto.Miss : this.nHitCount_ExclAuto.Miss;

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

	/// <summary>Per-session Tower mode state. Always non-null during gameplay.</summary>
	public CFloorManagement FloorManagement { get; private set; } = new CFloorManagement(5);

	// Synchronous activate — the default for every caller. Drains the stepped build to completion, so behaviour is
	// identical to the old monolithic Activate; the iterator just adds pause points so the song-loading screen can
	// spread the build across frames (see ActivateSteps + CStageSongLoading's stepped phase).
	public override void Activate() {
		var it = ActivateSteps();
		while (it.MoveNext()) { }
	}

	// The game-screen build as a stepped iterator: identical sequential logic to the old Activate, with
	// `yield return progress` between the pre-setup, batches of child actors, and the note-state build, so the
	// song load can advance it a slice per frame (smooth bar, no freeze) instead of one blocking call.
	public virtual System.Collections.Generic.IEnumerator<float> ActivateSteps() {
		OpenTaiko.HttpEventReporter.ReportGameplayStart();

		// Initialize tower-mode life from the song node so the correct value is
		// always used regardless of what happened in the selection screens.
		int towerLife = OpenTaiko.SongMount.rChosenScore?.ChartInfo.nLife ?? 5;
		FloorManagement = new CFloorManagement(towerLife);

		listChip = new List<CChip>[5];
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			listChip[i] = OpenTaiko.GetTJA(i)!.listChip;
		}
		this.ReduceMultiplayerNotes(
			chip => NotesManager.IsKusudama(chip),
			chip => chip.nChannelNo = NotesManager.ToChannelNo(NotesManager.ENoteType.BalloonEx),
			OpenTaiko.ConfigIni.nPlayerCount);
		this.ReduceMultiplayerNotes(chip => chip.IsPartnerNote, chip => chip.IsPartnerNote = false, 2);

		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			this.CalculateGen4ShinUchiScoreParameters_Dan();
		} else {
			this.CalculateGen4ShinUchiScoreParameters();
		}


		for (int index = OpenTaiko.TJA.listChip.Count - 1; index >= 0; index--) {
			if (OpenTaiko.TJA.listChip[index].nChannelNo == 0x01) {
				this.bgmlength = OpenTaiko.TJA.listChip[index].GetDuration() + OpenTaiko.TJA.listChip[index].nSoundTimems;
				break;
			}
		}

		this.AIBattleSections = new List<AIBattleSection>();

		CChip endChip = null;
		for (int i = 0; i < listChip[0].Count; i++) {
			CChip chip = listChip[0][i];
			if (endChip == null || (chip.nSoundTimems > endChip.nSoundTimems && chip.nChannelNo is 0x50 or 0xFF)) {
				endChip = chip;
			}
			if (chip.nChannelNo == 0xFF)
				break;
		}

		int battleSectionCount = 3 + ((endChip.nSoundTimems * 2) / 100000);
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

			int endtime = endChip.nSoundTimems / battleSectionCount;

			bool isAddSection = (nowBattleSectionCount != battleSectionCount) ?
				chip.nSoundTimems >= endtime * nowBattleSectionCount :
				i == listChip[0].Count - 1;


			if (isAddSection) {
				AIBattleSection aIBattleSection = new AIBattleSection();

				aIBattleSection.StartTime = battleSectionTime;
				aIBattleSection.EndTime = chip.nSoundTimems;
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


		this.nHitCount_ExclAuto = new CHITCOUNTOFRANK();
		this.nHitCount_InclAuto = new CHITCOUNTOFRANK();
		this.rCurrentCheerChip = null;
		this.bReverse = OpenTaiko.ConfigIni.bReverse;

		yield return 0.1f;   // pre-setup done; now bring the child actors up a few per frame

		// Inline CActivity.Activate's child loop so it can yield between batches (the old base.Activate() activated
		// all ~25 actors in one synchronous call → a freeze). Pre/post setup is otherwise unchanged.
		if (!this.IsActivated) {
			this.IsDeActivated = false;   // == IsActivated = true
			int __done = 0, __total = this.ChildActivities.Count;
			foreach (var __child in this.ChildActivities) {
				__child.Activate();
				if ((++__done & 3) == 0) yield return 0.1f + 0.7f * __done / System.Math.Max(1, __total);
			}
			this.IsFirstDraw = true;
		}

		this.tPanelStringSettings();
		//this.演奏判定ライン座標();
		this.bIsGOGOTIME_Branch = new bool[5, 3];
		this.bIsGOGOTIME = new bool[] { false, false, false, false, false };
		this.bWasGOGOTIME = new bool[] { false, false, false, false, false };
		this.bIsMiss = new bool[] { false, false, false, false, false };
		this.bUseBranch = new bool[] { false, false, false, false, false };
		this.nCurrentBranch = new CTja.ECourse[5];
		this.nTargetBranch = new CTja.ECourse[5];

		for (int i = 0; i < 5; i++) {
			OpenTaiko.stageGameScreen.ChangeBranch(CTja.ECourse.eNormal, i, stopAnime: true);
		}

		for (int i = 0; i < CBranchScore.Length; i++)
			this.CBranchScore[i] = new CBRANCHSCORE();


		this.nCurrentRollCount = new int[] { 0, 0, 0, 0, 0 };
		this.idxLastBranchSection = new int[5];
		this.Chara_MissCount = new int[5];
		dbDynamicBeatFactor   = 1.0;
		dbDynBeatTjaOffset    = 0.0;
		msDynBeatRawGameTime  = 0;
		msDynBeatSectionStart = 0;
		for (int i = 0; i < 5; i++) {
			nDynBeatSectionPerfects[i] = 0;
			nDynBeatSectionBads[i]     = 0;
			nDynBeatSectionNotes[i]    = 0;
		}
		// Dynamic Beat warps the shared scroll clock, so in LOCAL co-op one player's pick is forced on
		// all (they share one screen/clock). ONLINE it is per-player: every spot but you is an auto-played
		// remote whose scroll is cosmetic, and forcing it would change YOUR scroll for a choice you never
		// made — so don't share it, and clear it off the remote spots so only spot 0 (you) can drive it.
		bool _onlineDynBeat = LuaNetworking.Active?.PlaySyncActive == true;
		if (_onlineDynBeat) {
			for (int i = 1; i < OpenTaiko.ConfigIni.nPlayerCount; i++)
				if (OpenTaiko.ConfigIni.nFunMods[i] == EFunMods.DynamicBeat)
					OpenTaiko.ConfigIni.nFunMods[i] = EFunMods.None;
		} else {
			bool anyDynBeat = false;
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (OpenTaiko.ConfigIni.nFunMods[i] == EFunMods.DynamicBeat) { anyDynBeat = true; break; }
			}
			if (anyDynBeat) {
				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++)
					OpenTaiko.ConfigIni.nFunMods[i] = EFunMods.DynamicBeat;
			}
		}
		this.bLEVELHOLD = new bool[] { false, false, false, false, false };
		this.JPOSCROLLX = new double[5];
		this.JPOSCROLLY = new double[5];
		this.timingZones = new CConfigIni.CTimingZones[5];
		eGameType = new EGameType[5];
		bSplitLane = new bool[5];


		// Double play set here
		this.isMultiPlay = OpenTaiko.ConfigIni.nPlayerCount >= 2 ? true : false;

		this.nLoopCount_Clear = 1;

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			actGauge.Init(OpenTaiko.ConfigIni.nRisky, i);                                  // #23559 2011.7.28 yyagi
		}
		this.nPolyphonicSounds = OpenTaiko.ConfigIni.nPoliphonicSounds;

		OpenTaiko.Skin.tRemoveMixerAll();  // 効果音のストリームをミキサーから解除しておく

		queueMixerSound = new Queue<stmixer>(64);
		bIsDirectSound = (OpenTaiko.SoundManager.GetCurrentSoundDeviceType() == "DirectSound");
		bUseOSTimer = OpenTaiko.ConfigIni.bUseOSTimer;
		bValidScore = true;

		#region [ 演奏開始前にmixer登録しておくべきサウンド(開幕してすぐに鳴らすことになるチップ音)を登録しておく ]
		foreach (CChip pChip in listChip[0]) {
			//				Debug.WriteLine( "CH=" + pChip.nチャンネル番号.ToString( "x2" ) + ", 整数値=" + pChip.n整数値 +  ", time=" + pChip.n発声時刻ms );
			if (pChip.nSoundTimems <= 0) {
				if (pChip.nChannelNo == 0xDA) {
					pChip.bHit = true;
					//						Trace.TraceInformation( "first [DA] BAR=" + pChip.n発声位置 / 384 + " ch=" + pChip.nチャンネル番号.ToString( "x2" ) + ", wav=" + pChip.n整数値 + ", time=" + pChip.n発声時刻ms );
					if (listWAV.TryGetValue(pChip.nIntValue_InternalNumber, out CTja.CWAV wc)) {
						for (int i = 0; i < nPolyphonicSounds; i++) {
							if (wc.rSound[i] != null) {
								OpenTaiko.SoundManager.AddMixer(wc.rSound[i], OpenTaiko.ConfigIni.SongPlaybackSpeed, pChip.bPlayEndAfterPlaybackContinuesChip);
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
		if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
			this.DanSongScore = new CBRANCHSCORE[OpenTaiko.SongMount.rChoosenSong.DanSongs.Count];
			for (int i = 0; i < this.DanSongScore.Length; ++i)
				this.DanSongScore[i] = new();
		}


		this.sw = new Stopwatch();
		//          this.sw2 = new Stopwatch();
		// Reduce .NET GC hitches during the song: ask the GC to avoid blocking gen-2 collections while
		// playing. Restored to the previous mode in DeActivate. (SustainedLowLatency, not Batch — Batch
		// favors throughput and still permits the blocking gen-2 collections we're trying to avoid.)
		this.gclatencymode = System.Runtime.GCSettings.LatencyMode;
		// GCSettings.LatencyMode is unsupported on iOS (throws PlatformNotSupportedException); it's only a GC-pause tweak.
		if (!OperatingSystem.IsIOS()) System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;
		this.bIsAlreadyCleared = new bool[5];
		this.bIsAlreadyMaxed = new bool[5];

		this.ListDan_Number = 0;
		this.IsDanFailed = false;

		this.objHandlers = new();
		this.bCustomDoron = new bool[5];

		this.tBackgroundTextureCreate();

		this.nCurrentTopChip = new int[] { -1, -1, -1, -1, -1 }; // reset for new chart
		yield return 0.9f;   // children up; build the note state
		this.tValueInitialize(true, true);

		this.bPAUSE = false;
	}

	private void ReduceMultiplayerNotes(Func<CChip, bool> isTargetNoteF, Action<CChip> reduceF, int minNeighbors = 2) {
		// build filtered lists (already sorted)
		List<CChip>[,] targetNotes = new List<CChip>[OpenTaiko.ConfigIni.nPlayerCount, 3]; // [iPlayer, iBranch]

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i) {
			for (CTja.ECourse b = 0; b <= CTja.ECourse.eMaster; ++b)
				targetNotes[i, (int)b] = listChip[i].Where(chip => isTargetNoteF(chip) && chip.IsForBranch(b)).ToList();
		}

		// n-way merge, to find every almost-simultaneous note across all players and all branches
		int[,] idxNotes = new int[OpenTaiko.ConfigIni.nPlayerCount, 3]; // [iPlayer, iBranch]
		for (;;) {
			// find min of current index
			CChip? min = null;
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i) {
				for (int b = 0; b <= (int)CTja.ECourse.eMaster; ++b) {
					var now = targetNotes[i, b].ElementAtOrDefault(idxNotes[i, b]);
					if (now != null && (min == null || now.dbSoundTimems < min.dbSoundTimems))
						min = now;
				}
			}
			if (min == null) // all end reached
				break;

			// match against min
			const int msMatchErrorLimit = 100;
			CChip?[,] matchedNotes = new CChip?[OpenTaiko.ConfigIni.nPlayerCount, 3]; // [iPlayer, iBranch]
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i) {
				for (int b = 0; b <= (int)CTja.ECourse.eMaster; ++b) {
					// find same note (in case of simultaneous branched and branchless notes) or first best match
					double msErrorMin = msMatchErrorLimit;
					for (int ic = idxNotes[i, b]; ic < targetNotes[i, b].Count; ++ic) {
						var now = targetNotes[i, b][ic];
						if (now.dbSoundTimems >= min!.dbSoundTimems + msMatchErrorLimit)
							break; // would not match further
						double msError = Math.Max(Math.Abs(now.dbSoundTimems - min.dbSoundTimems), Math.Abs(now.end.dbSoundTimems - min.end.dbSoundTimems));
						if (ReferenceEquals(now, min) || msError < msErrorMin) {
							msErrorMin = msError;
							idxNotes[i, b] = ic + 1; // exclude from future match, leave previous matches unmatched
							matchedNotes[i, b] = now;
						}
					}
				}
			}

			// scan for matched multiplayer neighbors
			void matchBreak(int fromPlayer, int breakPlayer) {
				if (minNeighbors <= 1 || breakPlayer - fromPlayer >= minNeighbors)
					return; // enough neighbors
				// not enough neighbors, downgrade
				int n = Math.Min(breakPlayer + 1, OpenTaiko.ConfigIni.nPlayerCount);
				for (int i = fromPlayer; i < n; ++i) {
					for (int b = 0; b <= (int)CTja.ECourse.eMaster; ++b) {
						var note = matchedNotes[i, b];
						if (note != null) {
							reduceF(note);
							note.multiLink = null;
						}
					}
				}
			}

			int matchFrom = 0;
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i) {
				bool? isBranchless = null;
				for (int b = 0; b <= (int)CTja.ECourse.eMaster; ++b) {
					var note = matchedNotes[i, b];
					// Reject if lacking matching note in any branch
					// Matching both branched and branchless notes is considered as lacking branched note
					if (note == null || (isBranchless != null && note.IsEndedBranching != isBranchless)) {
						matchBreak(matchFrom, i);
						matchFrom = i + 1;
						break;
					}
					note.multiLink = matchedNotes;
					isBranchless = note.IsEndedBranching;
				}
			}
			matchBreak(matchFrom, OpenTaiko.ConfigIni.nPlayerCount);
		}
	}

	private void CalculateGen4ShinUchiScoreParameters() {
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			CTja _dtx = OpenTaiko.GetTJA(i)!;

			this.nNoteCount[i] = 0;
			this.nBalloonHitCount[i] = 0;
			this.nRollTimeMs[i] = 0;
			this.nAddScoreGen4ShinUchi[i] = 0;

			this.scoreMode[i] = (_dtx.PlayerSideMetadata.nScoreMode >= 0) ? _dtx.PlayerSideMetadata.nScoreMode : OpenTaiko.ConfigIni.nScoreMode;

			var _list = (_dtx.PlayerSideMetadata.bHasBranch) ? _dtx.listChip_Branch[2] : _dtx.listChip;
			CountGen4ShinUchiScoreNotes(_list, out this.nNoteCount[i], out this.nBalloonHitCount[i], out this.nRollTimeMs[i]);
			this.nAddScoreGen4ShinUchi[i] = GetAddScoreGen4ShinUchi(this.nNoteCount[i], this.nBalloonHitCount[i], this.nRollTimeMs[i]);
		}
	}

	private void CalculateGen4ShinUchiScoreParameters_Dan() {
		this.nNoteCount_Dan = new int[OpenTaiko.SongMount.rChoosenSong.DanSongs.Count];
		this.nBalloonHitCount_Dan = new int[OpenTaiko.SongMount.rChoosenSong.DanSongs.Count];
		this.nRollTimeMs_Dan = new double[OpenTaiko.SongMount.rChoosenSong.DanSongs.Count];
		this.nAddScoreGen4ShinUchi_Dan = new double[OpenTaiko.SongMount.rChoosenSong.DanSongs.Count];

		CTja tja = OpenTaiko.GetTJA(0)!;
		this.scoreMode[0] = (tja.PlayerSideMetadata.nScoreMode >= 0) ? tja.PlayerSideMetadata.nScoreMode : OpenTaiko.ConfigIni.nScoreMode;

		var _list = (tja.PlayerSideMetadata.bHasBranch) ? tja.listChip_Branch[2] : tja.listChip;
		for (int iNextSongChip = 0, iNextSongChipNext; iNextSongChip >= 0; iNextSongChip = iNextSongChipNext) {
			iNextSongChipNext = _list.FindIndex(iNextSongChip + 1, chip => (chip.nChannelNo == 0x9B));
			CChip nextSongChip = _list[iNextSongChip];
			int iDanSong = nextSongChip.nIntValue_InternalNumber;
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
				var msDuration = (_chip.end.nSoundTimems - _chip.nSoundTimems);
				var expectedHits = (int)(16.6 * msDuration / 1000);
				nBalloonHits += Math.Min(_chip.nBalloon, expectedHits);
			}

			if (NotesManager.IsRoll(_chip))
				msRollTime += (_chip.end.nSoundTimems - _chip.nSoundTimems);
		}
	}

	public int GetCeilingGen4ShinUchiScore(int player)
		=> Math.Max(1000000,
			(int)(this.nAddScoreGen4ShinUchi[player] * this.nNoteCount[player])
			+ (int)(this.nBalloonHitCount[player] * 100)
			+ (int)(Math.Ceiling(16.6 * this.nRollTimeMs[player] / 1000 / 10) * 100 * 10));

	public static double GetAddScoreGen4ShinUchi(int nSongNotes, int nSongBalloonHits, double msSongRollTime) {
		if (nSongNotes == 0)
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

		OpenTaiko.stageGameScreen.bUseBranch[0] = hasBranches;
		OpenTaiko.stageGameScreen.ChangeBranch(CTja.ECourse.eNormal, 0, stopAnime: true);
	}


	public override void DeActivate() {
		this.bgmlength = 1;
		this.ctChipPatternAnime = null;

		OpenTaiko.ResetCameraStates();

		for (int i = 0; i < 5; i++) {
			ctChipAnime[i] = null;
			ctChipAnimeLag[i] = null;
			bSplitLane[i] = false;
			this.msCurrentBarRollProgress[i] = 0;
		}

		for (int i = 0; i < this.chipCurrentProcessingRollChip.Length; ++i) {
			for (int iChip = this.chipCurrentProcessingRollChip[i].Count; iChip-- > 0;)
				this.ProcessRollEnd(i, this.chipCurrentProcessingRollChip[i][iChip], false);
			this.chipCurrentProcessingRollChip[i].Clear();
		}
		for (int i = 0; i < this.chipNowProcessingMultiHitNotes.Length; ++i)
			this.chipNowProcessingMultiHitNotes[i].Clear();

		listWAV.Clear();
		listWAV = null;
		listChip = null;
		queueMixerSound.Clear();
		queueMixerSound = null;
		if (!OperatingSystem.IsIOS()) System.Runtime.GCSettings.LatencyMode = this.gclatencymode;   // restore pre-gameplay GC mode (unsupported on iOS)

		this.actAVI.rVD = null; // Will be disposed by TJA.DeActivate() later

		var meanLag = CLagLogger.LogAndReturnMeanLag();

		this.actDan.IsAnimating = false;// IsAnimating=trueのときにそのまま選曲画面に戻ると、文字列が描画されない問題修正用。
		OpenTaiko.tTextureRelease(ref this.txBgImage);

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
		internal bool bPlayEndAfterPlaybackContinuesChip;
	};

	public class CBRANCHSCORE_Biggable {
		// is reset
		public int nRoll; // with balloon hits, but should exclude them in branch condition for TaikoJiro compatibility
		public int nGreat;
		public int nGood;
		public int nMiss;
		public int nBalloon;

		public void Reset() {
			nGreat = 0;
			nGood = 0;
			nMiss = 0;
			nRoll = 0;
			nBalloon = 0;
		}

		public double GetScore(Exam.Type type) => type switch {
			Exam.Type.Accuracy => (nGreat + nGood + nMiss == 0) ? 0 : (nGreat + nGood * 0.5) / (nGreat + nGood + nMiss) * 100.0,
			Exam.Type.PercentPerfect => (nGreat + nGood + nMiss == 0) ? 0 : (nGreat) / (nGreat + nGood + nMiss) * 100.0,
			Exam.Type.JudgePerfect => nGreat,
			Exam.Type.JudgeGood => nGood,
			Exam.Type.JudgeBad => nMiss,
			Exam.Type.Roll => nRoll,
			Exam.Type.BalloonHits => nBalloon,
			_ => 0,
		};
	}

	/// <summary>
	/// 分岐用のスコアをまとめるクラス。
	/// .2020.04.21.akasoko26
	/// </summary>
	public class CBRANCHSCORE : CBRANCHSCORE_Biggable {
		public CBRANCHSCORE_Biggable bigOnly = new();
		// no reset
		public int nScore;
		public int nADLIB;
		public int nADLIBMiss;
		public int nMine;
		public int nMineAvoid;
		// only used for dan-i
		public int nBarRollPass;
		public int nBalloonHitPass;
		public double msBarRollPass;
		public int nHighestCombo;
		public int nCombo;

		public CBRANCHSCORE_Biggable GetBiggable(bool forBigOnly) => forBigOnly ? this.bigOnly : this;

		public new void Reset() {
			base.Reset();
			bigOnly.Reset();
		}

		public double GetScore(Exam.Type type, CTja.EBranchCondBig big) => type switch {
			// unbiggable exam type
			Exam.Type.Score => nScore,
			// biggable exam type
			_ => big switch {
				CTja.EBranchCondBig.RegOnly => GetScore(type) - bigOnly.GetScore(type),
				CTja.EBranchCondBig.BigOnly => bigOnly.GetScore(type),
				CTja.EBranchCondBig.Both or _ => GetScore(type),
			},
		};

		public double GetScore((Exam.Type type, CTja.EBranchCondBig big) cond)
			=> GetScore(cond.type, cond.big);
	}

	public static void ForEachBiggable(bool isBig, Action<bool> action) {
		for (int iBig = 0, maxBig = isBig ? 1 : 0; iBig <= maxBig; ++iBig)
			action(iBig > 0);
	}

	public double[] JPOSCROLLX = new double[5];
	public int GetJPOSCROLLX(int player) {
		double screen_ratio = OpenTaiko.Skin.Resolution[0] / 1280.0;
		return (int)(JPOSCROLLX[player] * screen_ratio);
	}

	public int GetNoteOriginX(int iPlayer) {
		if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
			return OpenTaiko.Skin.nScrollField_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * iPlayer) + GetJPOSCROLLX(iPlayer);
		} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
			return OpenTaiko.Skin.nScrollField_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * iPlayer) + GetJPOSCROLLX(iPlayer);
		} else {
			return OpenTaiko.Skin.nScrollFieldX[iPlayer] + GetJPOSCROLLX(iPlayer);
		}
	}

	public double[] JPOSCROLLY = new double[5];
	public int GetJPOSCROLLY(int player) {
		double screen_ratio = OpenTaiko.Skin.Resolution[1] / 720.0;
		return (int)(JPOSCROLLY[player] * screen_ratio);
	}

	public int GetNoteOriginY(int iPlayer) {
		if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
			return OpenTaiko.Skin.nScrollField_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * iPlayer) + GetJPOSCROLLY(iPlayer);
		} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
			return OpenTaiko.Skin.nScrollField_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * iPlayer) + GetJPOSCROLLY(iPlayer);
		} else {
			return OpenTaiko.Skin.nScrollFieldY[iPlayer] + GetJPOSCROLLY(iPlayer);
		}
	}

	public CActPlayAVI actAVI;
	public Rainbow Rainbow;
	public CActPlayComboCommon actCombo;
	//protected CActFIFOBlack actFI;
	public CActFIFOStart actFI;
	protected CActFIFOBase actFO;
	protected CActFIFOBlack actFOBlack;
	protected CActFIFOResult actFOClear;
	public CActPlayGaugeCommon actGauge;

	public CActImplDancer actDancer;
	protected CActImplJudgeText actJudgeString;
	public TaikoLaneFlash actTaikoLaneFlash;
	public CActPlayPanelString actPanel;
	public CActPlayPlayInfo actPlayInfo;
	public CActPlayScoreCommon actScore;
	protected CActTaikoScrollSpeed actScrollSpeed;
	protected CActImplRoll actRoll;
	public CActImplBalloon actBalloon;
	public CActImplCharacter actChara;
	protected CActImplRollEffect actRollChara;
	protected CActImplComboBalloon actComboBalloon;
	protected CActPlayComboSound actComboVoice;
	protected CActPlayPauseMenu actPauseMenu;
	public CActImplChipEffects actChipEffects;
	public CActImplFooter actFooter;
	public CActImplRunner actRunner;
	public CActImplMob actMob;
	public Dan_Cert actDan;
	public AIBattle actAIBattle;
	public CActImplTrainingMode actTokkun;
	public bool bPAUSE;
	// Tracks time since last Resume() to enforce a 1-second anti-buffering cooldown on pause re-open.
	// Initialized with a high elapsed value so the first pause is never blocked.
	private System.Diagnostics.Stopwatch _pauseCooldown = System.Diagnostics.Stopwatch.StartNew();
	public bool[] bIsAlreadyCleared;
	public bool[] bIsAlreadyMaxed;
	protected bool bUsedMidiInputInPlay;
	protected bool bUsedKeyboardInPlay;
	protected bool bUsedJoypadInPlay;
	protected bool bUsedMouseInPlay;
	protected CCounter ctChipPatternAnime;
	public CCounter[] ctChipAnime;
	public CCounter[] ctChipAnimeLag;
	private int bgmlength = 1;

	protected EGameplayScreenReturnValue eFadeOutCompleteWhenReturnValue;
	protected readonly int[] nChannel0AtoPad08 = new int[] { 1, 2, 3, 4, 5, 7, 6, 1, 8, 0, 9, 9 };
	protected readonly int[] nChannel0AtoLane07 = new int[] { 1, 2, 3, 4, 5, 7, 6, 1, 9, 0, 8, 8 };
	//                         RD LC  LP  RD
	protected readonly int[] nPad0AtoChannel0A = new int[] { 0x11, 0x12, 0x13, 0x14, 0x15, 0x17, 0x16, 0x18, 0x19, 0x1a, 0x1b, 0x1c };
	protected readonly int[] nPad0AtoPad08 = new int[] { 1, 2, 3, 4, 5, 6, 7, 1, 8, 0, 9, 9 };// パッド画像のヒット処理用
																							  //   HH SD BD HT LT FT CY HHO RD LC LP LBD
	protected readonly int[] nPad0AtoLane07 = new int[] { 1, 2, 3, 4, 5, 6, 7, 1, 9, 0, 8, 8 };
	public CHITCOUNTOFRANK nHitCount_ExclAuto;
	public CHITCOUNTOFRANK nHitCount_InclAuto;
	public bool ShowVideo;
	public CBRANCHSCORE[] DanSongScore = [];

	// chip-played state handling
	protected bool isRewinding = false;
	public int[] nCurrentTopChip = new int[] { -1, -1, -1, -1, -1 }; // [iPlayer]; indexes of CTja.listChip
	public static bool hasChipBeenPlayedAt(int chipListIndex, int targetChipListIndex)
		=> chipListIndex < targetChipListIndex;
	public static bool hasChipBeenPlayedAt(CChip chip, double msTargetTjaTime)
		=> chip.nSoundTimems <= msTargetTjaTime;
	public bool hasChipBeenPlayed(int chipListIndex, int iPlayer)
		=> hasChipBeenPlayedAt(chipListIndex, nCurrentTopChip[iPlayer]);

	protected volatile Queue<stmixer> queueMixerSound;      // #24820 2013.1.21 yyagi まずは単純にAdd/Removeを1個のキューでまとめて管理するやり方で設計する
	protected DateTime dtLastQueueOperation;                //
	protected bool bIsDirectSound;                          //
	protected bool bValidScore;
	//		protected bool bDTXVmode;
	protected bool bReverse;

	protected CChip rCurrentCheerChip;

	protected CTexture txBgImage;

	//		protected int nRisky_InitialVar, nRiskyTime;		// #23559 2011.7.28 yyagi → CAct演奏ゲージ共通クラスに隠蔽
	protected int nPolyphonicSounds;
	protected List<CChip>[] listChip = new List<CChip>[5];
	protected Dictionary<int, CTja.CWAV> listWAV;
	protected bool bUseOSTimer;

	public CBRANCHSCORE[] CBranchScore = new CBRANCHSCORE[6];
	public CBRANCHSCORE[] CChartScore = new CBRANCHSCORE[5];
	public CBRANCHSCORE[] CSectionScore = new CBRANCHSCORE[5];
	public bool bPreviousPlayWasEndedNormally = false; // Necessary on story mode missions to see if the game has been quitted through the pause menu or not

	public bool[,] bIsGOGOTIME_Branch = new bool[5, 3]; // [iPlayer, iBranch]
	public bool[] bIsGOGOTIME = new bool[5];
	private bool[] bWasGOGOTIME = new bool[5]; // go-go time state before rewinding
	public bool[] bIsMiss = new bool[5];
	public bool[] bUseBranch = new bool[5];
	public CTja.ECourse[] nCurrentBranch = new CTja.ECourse[5]; //0:普通譜面 1:玄人譜面 2:達人譜面
	public CTja.ECourse[] nTargetBranch = new CTja.ECourse[5];
	public double[] msTargetBranchTime = new double[5];
	protected bool[] bBranchedChart = new bool[] { false, false, false, false, false };
	protected int[] idxLastBranchSection = new int[5];

	public bool[] bForcedBranch = new bool[] { false, false, false, false, false };
	public bool[] bLEVELHOLD = new bool[] { false, false, false, false, false };
	protected int nListCount;

	protected int[] nCurrentRollCount = new int[5];
	public int[] Chara_MissCount;

	// Dynamic Beat mode state (shared across all players)
	protected double dbDynamicBeatFactor    = 1.0;
	protected double dbDynBeatTjaOffset     = 0.0; // TJA-time continuity offset when factor changes mid-song (double for precision)
	protected long   msDynBeatRawGameTime   = 0;   // rawGameTime captured at the start of the current frame's chip processing
	protected long   msDynBeatSectionStart  = 0;

	/// <summary>Current chart (TJA) time for a player, including the Dynamic Beat warp (factor + offset)
	/// applied by tProgressDraw_Chip — any comparison against chip times must use this, not the raw clock.</summary>
	public long GetChartTimeNow(int nPlayer) {
		CTja tja = OpenTaiko.GetTJA(nPlayer)!;
		long rawGameTime = this.IsFailStopped() ? this.msFailedStopSystemTime : SoundManager.PlayTimer.NowTimeMs;
		double tjaTime = tja.GameTimeToTjaTime(rawGameTime);
		if (OpenTaiko.ConfigIni.nFunMods[nPlayer] == EFunMods.DynamicBeat)
			return (long)(tjaTime * dbDynamicBeatFactor + dbDynBeatTjaOffset);
		return (long)tjaTime;
	}
	protected int[]  nDynBeatSectionPerfects = new int[5];
	protected int[]  nDynBeatSectionBads     = new int[5];
	protected int[]  nDynBeatSectionNotes    = new int[5];
	protected bool[] isChartEnded = { false, false, false, false, false }; // last note of chart passed
	protected bool[] isFinishedPlaying = { false, false, false, false, false };
	protected bool[] isDeniedPlaying = { false, false, false, false, false };

	public enum EStageAbort {
		None,
		FailedFlow,
		FailedStop,
		FailedStopSkipResult,
		Max = FailedStopSkipResult
	};
	protected EStageAbort[] stageAbortType = { EStageAbort.None, EStageAbort.None, EStageAbort.None, EStageAbort.None, EStageAbort.None };

	protected long msFailedStopSystemTime;

	protected int nTimerNumber;
	protected int nCurrentNoteFaceNumber;

	protected int nWaitButton;

	protected CConfigIni.CTimingZones[] timingZones;
	public EGameType[] eGameType;
	protected bool[] bSplitLane;

	public List<CChip>[] chipNowProcessingMultiHitNotes = [[], [], [], [], []]; // [iPlayer][idxNowProcessingMultiHitNotes]
	public List<CChip>[] chipCurrentProcessingRollChip = [[], [], [], [], []]; // [iPlayer][idxNowProcessingRoll]
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
	protected System.Runtime.GCLatencyMode gclatencymode;   // saved GC latency mode, restored on DeActivate
	public int ListDan_Number;
	private bool IsDanFailed;

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

		int clearCount = 0;
		for (int i = 0; i < this.AIBattleSections.Count; i++) {
			if (this.AIBattleSections[i].End == AIBattleSection.EndType.Clear) {
				clearCount++;
			}
		}
		bIsAIBattleWin = !(this.IsStageFailed(0) || this.IsStageFailed_Fast()) && clearCount >= this.AIBattleSections.Count / 2.0;
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
		ctChipAnime[nPlayer] = new CCounter(0, 3, CTja.TjaDurationToGameDuration(60.0 / OpenTaiko.stageGameScreen.actPlayInfo.dbBPM[nPlayer] * 1 / 4), SoundManager.PlayTimer);
		OpenTaiko.stageGameScreen.PuchiChara.ChangeBPM(CTja.TjaDurationToGameDuration(60.0 / OpenTaiko.stageGameScreen.actPlayInfo.dbBPM[nPlayer]));
	}

	public void AddMixer(CSound cs, bool _bPlayEndAfterPlaybackContinuesChip) {
		stmixer stm = new stmixer() {
			bIsAdd = true,
			csound = cs,
			bPlayEndAfterPlaybackContinuesChip = _bPlayEndAfterPlaybackContinuesChip
		};
		queueMixerSound.Enqueue(stm);
		//		Debug.WriteLine( "★Queue: add " + Path.GetFileName( stm.csound.strファイル名 ));
	}
	public void RemoveMixer(CSound cs) {
		stmixer stm = new stmixer() {
			bIsAdd = false,
			csound = cs,
			bPlayEndAfterPlaybackContinuesChip = false
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
						OpenTaiko.SoundManager.AddMixer(stm.csound, OpenTaiko.ConfigIni.SongPlaybackSpeed, stm.bPlayEndAfterPlaybackContinuesChip);
					} else {
						OpenTaiko.SoundManager.RemoveMixer(stm.csound);
					}
				}
			}
		}
	}



	internal ENoteJudge eGetChipJudgeAtTime(long nTime, CChip pChip, int player = 0) {
		var eJudge = eGetChipJudgeAtTimeImpl(nTime, pChip, player).noteJudge;
		return eJudge;
	}

	private bool tEasyTimeZones(int nPlayer) {
		bool _timingzonesAreEasy = false;

		int diff = OpenTaiko.SongMount.nChoosenSongDifficulty[nPlayer];

		// Diff = Normal or Easy
		if (diff <= (int)Difficulty.Normal) {
			_timingzonesAreEasy = true;
		}

		// Diff = Dan and current song is Normal or Easy
		if (diff == (int)Difficulty.Dan) {
			int _nb = OpenTaiko.stageGameScreen.actDan.NowShowingNumber;
			var _danSongs = OpenTaiko.SongMount.rChoosenSong.DanSongs;

			if (_nb < _danSongs.Count) {
				var _currentDiff = _danSongs[_nb].Difficulty;
				if (_currentDiff <= (int)Difficulty.Normal)
					_timingzonesAreEasy = true;

			}
		}

		// Diff = Tower and SIDE is Normal
		if (diff == (int)Difficulty.Tower) {
			_timingzonesAreEasy = OpenTaiko.GetTJA(nPlayer)!.SIDE == CTja.ESide.eNormal;
		}

		return _timingzonesAreEasy;
	}

	protected CConfigIni.CTimingZones GetTimingZones(int idxPlayer) {
		// To change later to adapt to Tower Ama-kuchi
		//diff = Math.Min(diff, (int)Difficulty.Oni);

		int idxPlayerActual = idxPlayer;
		int timingShift = OpenTaiko.ConfigIni.nTimingZones[idxPlayerActual];

		bool _timingzonesAreEasy = tEasyTimeZones(idxPlayer);

		return (_timingzonesAreEasy == true) ? OpenTaiko.ConfigIni.tzLevels[timingShift] : OpenTaiko.ConfigIni.tzLevels[2 + timingShift];
	}

	private void tIncreaseComboDan(int danSong) {
		this.DanSongScore[danSong].nCombo++;
		if (this.DanSongScore[danSong].nCombo > this.DanSongScore[danSong].nHighestCombo)
			this.DanSongScore[danSong].nHighestCombo = this.DanSongScore[danSong].nCombo;
	}

	private record struct NoteJudgeWithOffset(ENoteJudge noteJudge, int? msDelta);

	private ENoteJudge evaluateNodeJudge(long msTjaTime, int msDelta, CChip pChip, int player = 0) {

		if (pChip == null) {
			return ENoteJudge.Miss;
		} else {
			//Debug.WriteLine("nAbsTime=" + (nTime - pChip.n発声時刻ms) + ", nDeltaTime=" + (nTime - pChip.n発声時刻ms));
			var nt = NotesManager.GetNoteType(pChip);
			if (NotesManager.IsRoll(nt)) {
				return (msTjaTime >= pChip.nSoundTimems && msTjaTime < pChip.end.nSoundTimems) ? ENoteJudge.Perfect : ENoteJudge.Miss;
			} else if (NotesManager.IsGenericBalloon(nt)) {
				return (msTjaTime >= pChip.nSoundTimems - 17 && msTjaTime < pChip.end.nSoundTimems) ? ENoteJudge.Perfect : ENoteJudge.Miss;
			}
			if (msDelta <= 0) // fast judge for autoplay
				return ENoteJudge.Perfect;

			CConfigIni.CTimingZones tz = this.timingZones[player];

			if (msDelta > tz.nBadZone) // fast judge for miss
				return ENoteJudge.Miss;
			if (msDelta <= tz.nGoodZone)
				return ENoteJudge.Perfect;

			int actual = player;

			if (msDelta <= tz.nOkZone) {
				if (OpenTaiko.ConfigIni.bJust[actual] == 1 && NotesManager.IsMissableNote(pChip)) // Just
					return ENoteJudge.Poor;
				return ENoteJudge.Good;
			}

			if (OpenTaiko.ConfigIni.bJust[actual] == 2 || !NotesManager.IsMissableNote(pChip)) // Safe
				return ENoteJudge.Good;
			return ENoteJudge.Poor;
		}
	}

	private NoteJudgeWithOffset eGetChipJudgeAtTimeImpl(long msTjaTime, CChip pChip, int player = 0) {
		if (pChip == null) return new NoteJudgeWithOffset(ENoteJudge.Miss, null);
		var msDelta = msTjaTime - pChip.dbSoundTimems;
		return new NoteJudgeWithOffset(
			evaluateNodeJudge(msTjaTime, (int)Math.Abs(msDelta), pChip, player),
			(int)msDelta
		);
	}

	protected abstract void ProcessPadInput(int nUsePlayer, EPad nPad, long msHitTjaTime);
	protected abstract ENoteJudge JudgePadInput(int iPlayer, CChip? chip, EPad pad, long msHitTjaTime, ENoteJudge rawJudge, bool skipHit = false);

	public static EPad[] GetAutoInput(NotesManager.ENoteType noteType, EGameType gameType, int nHand, bool isBigInput = false) {
		if (isBigInput && NotesManager.IsBigDonTaiko(noteType, gameType))
			return [EPad.LRed, EPad.RRed];
		if (isBigInput && NotesManager.IsBigKaTaiko(noteType, gameType))
			return [EPad.LBlue, EPad.RBlue];
		if (NotesManager.IsPurpleNoteTaiko(noteType, gameType))
			return (nHand == 0) ? [EPad.LBlue, EPad.RRed] : [EPad.RBlue, EPad.LRed];
		if (NotesManager.IsPinkKonga(noteType, gameType))
			return [EPad.LBlue, EPad.RRed];
		if (NotesManager.IsAcceptRed(noteType, gameType)) {
			if (gameType is EGameType.Konga && NotesManager.IsAcceptBlue(noteType, gameType))
				return (nHand == 0) ? [EPad.LBlue] : [EPad.RRed];
			return (nHand == 0) ? [EPad.LRed] : [EPad.RRed];
		}
		if (NotesManager.IsAcceptBlue(noteType, gameType))
			return (nHand == 0) ? [EPad.LBlue] : [EPad.RBlue];
		if (NotesManager.IsAcceptClap(noteType, gameType))
			return [EPad.Clap];
		return [];
	}

	private bool CanAutoplayHit(CChip chip, long msTjaTime, int iPlayer, EGameType gt) {
		if (this.isDeniedPlaying[iPlayer] || this.IsStageFailed_Fast())
			return false;
		if (this.eGetChipJudgeAtTime(msTjaTime, chip, iPlayer) is ENoteJudge.Miss) // less costly check
			return false;
		var pads = GetAutoInput(chip, gt, this.nHand[iPlayer], isBigInput: OpenTaiko.ConfigIni.bJudgeBigNotes);
		if (pads.Length == 0)
			return false;
		var (chipToJudge, judge) = this.GetChipToJudge(msTjaTime, iPlayer, pads[0]);
		return (chipToJudge == chip && judge is not ENoteJudge.Miss);
	}

	private void AutoplayDoHit(CChip chip, long msTjaTime, int iPlayer, EGameType gt) {
		if (!NotesManager.IsMine(chip) || this.CanAutoplayHitMine(iPlayer, true)) {
			this.AutoplaySwitchHand(iPlayer);
			foreach (var pad in GetAutoInput(chip, gt, this.nHand[iPlayer], isBigInput: OpenTaiko.ConfigIni.bJudgeBigNotes))
				this.ProcessPadInput(iPlayer, pad, msTjaTime);
		}
		// prevent further hit attempt (unless overridden)
		chip.msAutoLastHit = double.PositiveInfinity;
	}

	private bool AutoplayTryHit(CChip chip, long msTjaTime, int iPlayer, EGameType gt) {
		if (!this.CanAutoplayHit(chip, msTjaTime, iPlayer, gt))
			return false;
		this.AutoplayDoHit(chip, msTjaTime, iPlayer, gt);
		return true;
	}

	protected void AutoplayHit(CChip chip, long msTjaTime, int iPlayer, EGameType gt) {
		if (!chip.bVisible || chip.IsMissed || chip.bHit || this.bPAUSE || chip.msAutoLastHit > msTjaTime) {
			return;
		}
		bool bAutoPlay = OpenTaiko.ConfigIni.bAutoPlay[iPlayer] || (iPlayer == 1 && OpenTaiko.ConfigIni.bAIBattleMode);
		if (!bAutoPlay)
			return;

		bool canHitNow = this.CanAutoplayHit(chip, msTjaTime, iPlayer, gt);
		if (chip.nSoundTimems > msTjaTime) {
			if (chip.eNoteState == ENoteState.None && canHitNow)
				chip.msAutoLastHit = msTjaTime;
			return;
		}
		if (chip.eNoteState == ENoteState.None && chip.msAutoLastHit < chip.nSoundTimems) {
			if (this.AutoplayTryHit(chip, chip.nSoundTimems, iPlayer, gt)) // critical hit
				return;
			bool canHitEarly = this.CanAutoplayHit(chip, (long)chip.msAutoLastHit, iPlayer, gt);
			if (canHitEarly && (!canHitNow || Math.Abs(chip.msAutoLastHit - chip.nSoundTimems) < Math.Abs(msTjaTime - chip.nSoundTimems))) {
				this.AutoplayDoHit(chip, (long)chip.msAutoLastHit, iPlayer, gt); // early hit
				return;
			}
			// mark as attempted
			chip.msAutoLastHit = msTjaTime;
		}
		if (canHitNow) // late hit
			this.AutoplayDoHit(chip, msTjaTime, iPlayer, gt);
	}

	protected void Autoroll(CChip chip, long msTjaTime, int iPlayer, EGameType gt) {
		if (NotesManager.IsGenericBalloon(chip))
			this.AutorollBalloon(chip, msTjaTime, iPlayer, gt);
		else if (NotesManager.IsGenericRoll(chip) && !NotesManager.IsRollEnd(chip))
			this.AutorollRoll(chip, msTjaTime, iPlayer, gt);
	}

	protected void AutorollRoll(CChip pChip, long msTjaTime, int iPlayer, EGameType gt) {
		if (this.isDeniedPlaying[iPlayer] || this.IsStageFailed_Fast() || !pChip.bVisible || pChip.IsMissed || pChip.bHit || this.bPAUSE)
			return;
		bool bAutoPlay = OpenTaiko.ConfigIni.bAutoPlay[iPlayer] || (iPlayer == 1 && OpenTaiko.ConfigIni.bAIBattleMode);
		var puchichara = OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(iPlayer)];

		int rollSpeed = bAutoPlay ? OpenTaiko.ConfigIni.nRollsPerSec : puchichara.effect.Autoroll;
		if (OpenTaiko.ConfigIni.bAIBattleMode && iPlayer == 1)
			rollSpeed = OpenTaiko.ConfigIni.apAIPerformances[OpenTaiko.ConfigIni.nAILevel - 1].nRollSpeed;

		if (rollSpeed <= 0) {
			return;
		}
		long msPerRollTja = (long)CTja.GameDurationToTjaDuration(1000.0 / rollSpeed);
		if (msTjaTime >= pChip.msAutoLastHit + msPerRollTja) {
			if (this.AutoplayTryHit(pChip, msTjaTime, iPlayer, gt)) {
				pChip.msAutoLastHit = msTjaTime;
			}
		}
	}

	protected void AutorollBalloon(CChip pChip, long msTjaTime, int iPlayer, EGameType gt) {
		if (this.isDeniedPlaying[iPlayer] || this.IsStageFailed_Fast() || !pChip.bVisible || pChip.IsMissed || pChip.bHit || this.bPAUSE || pChip.msAutoLastHit > msTjaTime)
			return;

		bool bAutoPlay = OpenTaiko.ConfigIni.bAutoPlay[iPlayer] || (iPlayer == 1 && OpenTaiko.ConfigIni.bAIBattleMode);
		var puchichara = OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(iPlayer)];
		if (!(bAutoPlay || puchichara.effect.Autoroll > 0))
			return;

		int rollCount = pChip.nRollCount;
		int balloon = pChip.nBalloon;
		if (NotesManager.IsKusudama(pChip)) {
			/*
			var ts = pChip.db発声時刻ms;
			var km = TJAPlayer3.DTX.kusudaMAP;

			if (km.ContainsKey(ts))
			{
				rollCount = km[ts].nRollCount;
				balloon = km[ts].nBalloon;
			}
			*/
			rollCount = pChip.KusudamaRollCount;
			balloon = pChip.KusudamaCount;

		}

		if (balloon == 0) {
			return;
		}
		if (balloon == 1 && NotesManager.IsFuzeRoll(pChip) && this.CanAutoplayHitMine(iPlayer, true)) {
			pChip.msAutoLastHit = double.PositiveInfinity; // prevent clearing fuze
			return;
		}
		int rollSpeed = bAutoPlay ? (balloon - rollCount) : puchichara.effect.Autoroll;

		long balloonDuration = bAutoPlay ? (pChip.end.nSoundTimems - msTjaTime) : (long)CTja.GameDurationToTjaDuration(1000);

		long msPerRollTja = (long)(balloonDuration / (double)rollSpeed);
		if (msTjaTime >= pChip.msAutoLastHit + msPerRollTja) {
			if (this.AutoplayTryHit(pChip, msTjaTime, iPlayer, gt)) {
				pChip.msAutoLastHit = msTjaTime;
			}
		}
	}

	protected void PlayHitNoteSound(int iPlayer, NotesManager.EInputType input) {
		var sound = input switch {
			NotesManager.EInputType.Red or NotesManager.EInputType.RedBig => this.soundRed[iPlayer],
			NotesManager.EInputType.Blue or NotesManager.EInputType.BlueBig => this.soundBlue[iPlayer],
			NotesManager.EInputType.Clap => this.soundClap[iPlayer],
			_ => null,
		};
		sound?.PlayStart();
	}

	protected void StartHitNoteLaneFlash(int iPlayer, NotesManager.EInputType input, EGameType gt) {
		this.actTaikoLaneFlash.PlayerLane[iPlayer].Start(NotesManager.InputToLane(input), gt);
	}

	private void AutoplaySwitchHand(int iPlayer) {
		if (this.nHand[iPlayer] == 0)
			this.nHand[iPlayer]++;
		else
			this.nHand[iPlayer] = 0;
	}

	protected ENoteJudge tRollProcess(CChip pChip, EGameType gt, double msHitTjaTime, NotesManager.EInputType sort, int nPlayer) {
		if (msHitTjaTime >= pChip.nSoundTimems && msHitTjaTime < pChip.end.nSoundTimems) {
			this.actRoll.bDisplay[nPlayer] = true;
			if (pChip.nRollCount == 0) //連打カウントが0の時
				this.actRoll.tFrameDisplayTimeExtend(nPlayer, true);
			else
				this.actRoll.tFrameDisplayTimeExtend(nPlayer, false);
			if (this.actRoll.ctRollAnime[nPlayer].IsUnEnded) {
				this.actRoll.ctRollAnime[nPlayer] = new CCounter(0, 9, 14, OpenTaiko.Timer);
				this.actRoll.ctRollAnime[nPlayer].CurrentValue = 1;
			} else {
				this.actRoll.ctRollAnime[nPlayer] = new CCounter(0, 9, 14, OpenTaiko.Timer);
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

			bool isBig = NotesManager.IsBigRollTaiko(pChip, NotesManager.GetChipGameType(pChip, nPlayer));

			this.nCurrentRollCount[nPlayer] = ++pChip.nRollCount;

			ForEachBiggable(isBig, forBigOnly => {
				if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
					this.DanSongScore[actDan.NowShowingNumber].GetBiggable(forBigOnly).nRoll++;

				this.CBranchScore[nPlayer].GetBiggable(forBigOnly).nRoll++;
				this.CChartScore[nPlayer].GetBiggable(forBigOnly).nRoll++;
				this.CSectionScore[nPlayer].GetBiggable(forBigOnly).nRoll++;
			});

			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) this.actRollChara.Start(nPlayer);


			long nAddScore = 0;

			if (!OpenTaiko.ConfigIni.ShinuchiMode) {
				// 旧配点・旧筐体配点
				if (this.scoreMode[nPlayer] == 0 || this.scoreMode[nPlayer] == 1) {
					if (!isBig)
						nAddScore = 300L;
					else
						nAddScore = 360L;
				}
				// 新配点
				else {
					if (!isBig)
						nAddScore = 100L;
					else
						nAddScore = 200L;
				}
			} else {
				nAddScore = 100L;
			}

			if (!OpenTaiko.ConfigIni.ShinuchiMode && pChip.bGOGOTIME) this.actScore.Add((long)(nAddScore * 1.2f), nPlayer);
			else this.actScore.Add(nAddScore, nPlayer);


			int __score = (int)(this.actScore.Get(nPlayer));
			this.CBranchScore[nPlayer].nScore = __score;
			this.CChartScore[nPlayer].nScore = __score;
			this.CSectionScore[nPlayer].nScore = __score;

			this.PlayHitNoteSound(nPlayer, sort);
			this.StartHitNoteLaneFlash(nPlayer, sort, gt);
			//赤か青かの分岐
			if (sort is NotesManager.EInputType.Red or NotesManager.EInputType.RedBig) {
				OpenTaiko.stageGameScreen.FlyingNotes.Start(NotesManager.IsBigRollTaiko(pChip, gt) ? NotesManager.ENoteType.DonBig : NotesManager.ENoteType.Don, gt, nPlayer);
			} else if (sort is NotesManager.EInputType.Blue or NotesManager.EInputType.BlueBig) {
				OpenTaiko.stageGameScreen.FlyingNotes.Start(NotesManager.IsBigRollTaiko(pChip, gt) ? NotesManager.ENoteType.KaBig : NotesManager.ENoteType.Ka, gt, nPlayer);
			} else if (sort is NotesManager.EInputType.Clap) {
				OpenTaiko.stageGameScreen.FlyingNotes.Start(NotesManager.ENoteType.Clap, gt, nPlayer);
			}

			return ENoteJudge.Perfect;
		}

		return ENoteJudge.Miss;
	}

	protected ENoteJudge tBalloonProcess(CChip pChip, EGameType gt, double msHitTjaTime, NotesManager.EInputType sort, int player) {
		CTja tja = OpenTaiko.GetTJA(player)!;
		bool IsKusudama = NotesManager.IsKusudama(pChip);
		bool IsFuze = NotesManager.IsFuzeRoll(pChip);

		int rollCount = pChip.nRollCount;
		int balloon = pChip.nBalloon;


		if (!(msHitTjaTime < pChip.end.nSoundTimems)) {
			return ENoteJudge.Miss;
		}

		this.actChara.bBalloonRoll[player] = !IsKusudama;
		this.actChara.ReturnDefaultAnime(player);
		CCharacter character = CCharacter.GetCharacter(player);

		if (IsKusudama) {
			this.actChara.IsInKusudama = true;
			rollCount = pChip.nRollCount = ++pChip.KusudamaRollCount;
			balloon = pChip.KusudamaCount;
			if (pChip.KusudamaRollCount > 0) {
				actChara.PlayGameAction(player, CCharacter.ANIM_GAME_KUSUDAMA_BREAKING);
				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					if (this.actBalloon.ctBalloonAnime[i].IsUnEnded) {
						this.actBalloon.ctBalloonAnime[i] = new CCounter(0, 9, 14, OpenTaiko.Timer);
						this.actBalloon.ctBalloonAnime[i].CurrentValue = 1;
					} else {
						this.actBalloon.ctBalloonAnime[i] = new CCounter(0, 9, 14, OpenTaiko.Timer);
					}
				}
			}
		} else {
			actChara.PlayGameAction(player, CCharacter.ANIM_GAME_BALLOON_BREAKING);
			actChara.CharacterControllers[player].ResetCounter(player);


			if (this.actBalloon.ctBalloonAnime[player].IsUnEnded) {
				this.actBalloon.ctBalloonAnime[player] = new CCounter(0, 9, 14, OpenTaiko.Timer);
				this.actBalloon.ctBalloonAnime[player].CurrentValue = 1;
			} else {
				this.actBalloon.ctBalloonAnime[player] = new CCounter(0, 9, 14, OpenTaiko.Timer);
			}
		}


		if (!IsKusudama) {
			rollCount = ++pChip.nRollCount;
		}

		ForEachBiggable(IsKusudama, forBigOnly => {
			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
				this.DanSongScore[actDan.NowShowingNumber].GetBiggable(forBigOnly).nRoll++;
				this.DanSongScore[actDan.NowShowingNumber].GetBiggable(forBigOnly).nBalloon++;
			}
			this.CBranchScore[player].GetBiggable(forBigOnly).nRoll++;
			this.CChartScore[player].GetBiggable(forBigOnly).nRoll++; //  成績発表の連打数に風船を含めるように (AioiLight)
			this.CSectionScore[player].GetBiggable(forBigOnly).nRoll++;

			this.CBranchScore[player].GetBiggable(forBigOnly).nBalloon++;
			this.CChartScore[player].GetBiggable(forBigOnly).nBalloon++;
			this.CSectionScore[player].GetBiggable(forBigOnly).nBalloon++;
		});

		//分岐のための処理。実装してない。

		//赤か青かの分岐

		long nAddScore = 0;

		if (!OpenTaiko.ConfigIni.ShinuchiMode) {
				if (balloon == rollCount)
				nAddScore = 0; // add later
			else if (pChip.bGOGOTIME)
					nAddScore = 360L;
				else
					nAddScore = 300L;
		} else if (IsKusudama && OpenTaiko.ConfigIni.nPlayerCount > 1) {
			nAddScore = Math.Max(1, (long)Math.Floor(10.0 / OpenTaiko.ConfigIni.nPlayerCount)) * 10;
		} else {
			nAddScore = 100L;
		}

		if (nAddScore != 0) {
		this.actScore.Add(nAddScore, player);

			int __score = (int)(this.actScore.Get(player));
		this.CBranchScore[player].nScore = __score;
		this.CChartScore[player].nScore = __score;
		this.CSectionScore[player].nScore = __score;
		}

		this.StartHitNoteLaneFlash(player, sort, gt);
		if (balloon - rollCount <= 0)
			this.ProcessBalloonBroke(player, pChip, msHitTjaTime, sort);
		else
			this.PlayHitNoteSound(player, sort);
		return ENoteJudge.Perfect;
	}

	protected unsafe ENoteJudge tChipHitProcess(long msHitTjaTime, CChip pChip, EKeyConfigPart screenmode, bool bCorrectLane, NotesManager.EInputType nNowInput, int nPlayer) {
		//unsafeコードにつき、デバッグ中の変更厳禁!

		CTja tja = OpenTaiko.GetTJA(nPlayer)!;
		bool bAutoPlay = OpenTaiko.ConfigIni.bAutoPlay[nPlayer];
		bool bBombHit = false;
		bool isDeniedJudgeCount = this.isDeniedPlaying[nPlayer] || this.IsStageFailed_Fast();

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
		int msDelta = eGetChipJudgeAtTimeImpl(msHitTjaTime, pChip, nPlayer).msDelta!.Value;
		{
			//連打が短すぎると発声されない
			eJudgeResult = (bCorrectLane && !pChip.IsMissed) ? this.eGetChipJudgeAtTime(msHitTjaTime, pChip, nPlayer) : ENoteJudge.Miss;
			// for hit-type notes, check pChip.IsMissed instead to avoid repeated miss judgements

			// AI judges
			eJudgeResult = AlterJudgement(nPlayer, eJudgeResult, true);

			if (!bAutoPlay && eJudgeResult != ENoteJudge.Miss) {
				pChip.nLag = msDelta;
				CLagLogger.Add(nPlayer, pChip);
			}

			EGameType gt = NotesManager.GetChipGameType(pChip, nPlayer);

			if (NotesManager.IsRoll(pChip)) {
				eJudgeResult = this.tRollProcess(pChip, gt, msHitTjaTime, nNowInput, nPlayer);
			} else if (NotesManager.IsGenericBalloon(pChip)) {
				if (!pChip.bProcessed) // hit during pre-note window
					this.AddNowProcessingRollChip(nPlayer, pChip);
				if (!NotesManager.IsKusudama(pChip) || pChip.KusudamaCount > 0)
					eJudgeResult = this.tBalloonProcess(pChip, gt, msHitTjaTime, nNowInput, nPlayer);
				else
					eJudgeResult = ENoteJudge.Miss;
			} else if (NotesManager.IsRollEnd(pChip)) {
				/* do nothing */
			} else if (NotesManager.IsADLIB(pChip)) {
				if (eJudgeResult != ENoteJudge.Auto && eJudgeResult != ENoteJudge.Miss) {
					this.actJudgeString.Start(nPlayer, eJudgeResult != ENoteJudge.Bad ? ENoteJudge.ADLIB : ENoteJudge.Bad);
					eJudgeResult = ENoteJudge.Perfect; // Prevent ADLIB notes breaking DFC runs
					OpenTaiko.stageGameScreen.actLaneTaiko.Start(pChip, gt, eJudgeResult, false, nPlayer);
					OpenTaiko.stageGameScreen.actChipFireD.Start(pChip, gt, eJudgeResult, false, nPlayer);
					this.soundAdlib[nPlayer]?.PlayStart();
					this.StartHitNoteLaneFlash(nPlayer, nNowInput, gt);
					this.actTaikoLaneFlash.PlayerLane[nPlayer].Start(PlayerLane.FlashType.Hit, gt);
					if (!isDeniedJudgeCount) {
					this.CChartScore[nPlayer].nADLIB++;
					this.CSectionScore[nPlayer].nADLIB++;
					this.CBranchScore[nPlayer].nADLIB++;
					if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
						this.DanSongScore[actDan.NowShowingNumber].nADLIB++;
					}
				} else if (!isDeniedJudgeCount && pChip.IsMissed) {
					this.CChartScore[nPlayer].nADLIBMiss++;
					this.CSectionScore[nPlayer].nADLIBMiss++;
					this.CBranchScore[nPlayer].nADLIBMiss++;
					if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
						this.DanSongScore[actDan.NowShowingNumber].nADLIBMiss++;
				}
			} else if (NotesManager.IsMine(pChip)) {
				if (eJudgeResult != ENoteJudge.Auto && eJudgeResult != ENoteJudge.Miss) {
					this.actJudgeString.Start(nPlayer, eJudgeResult != ENoteJudge.Bad ? ENoteJudge.Mine : ENoteJudge.Bad);
					bBombHit = true;
					eJudgeResult = ENoteJudge.Bad;
					this.PlayHitNoteSound(nPlayer, nNowInput);
					this.StartHitNoteLaneFlash(nPlayer, nNowInput, gt);
					OpenTaiko.stageGameScreen.actLaneTaiko.Start(pChip, gt, eJudgeResult, false, nPlayer);
					OpenTaiko.stageGameScreen.actChipFireD.Start(pChip, gt, ENoteJudge.Mine, false, nPlayer);
					OpenTaiko.Skin.soundBomb?.tPlay();
					if (!isDeniedJudgeCount) {
					this.CChartScore[nPlayer].nMine++;
					this.CSectionScore[nPlayer].nMine++;
					this.CBranchScore[nPlayer].nMine++;
					if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
						this.DanSongScore[actDan.NowShowingNumber].nMine++;
						this.AIRegisterInput(nPlayer, 0f);
					}
				} else if (!isDeniedJudgeCount && pChip.IsMissed) {
					this.CChartScore[nPlayer].nMineAvoid++;
					this.CSectionScore[nPlayer].nMineAvoid++;
					this.CBranchScore[nPlayer].nMineAvoid++;
					if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
						this.DanSongScore[actDan.NowShowingNumber].nMineAvoid++;
					this.AIRegisterInput(nPlayer, 1f);
				}
			} else {
				if (eJudgeResult != ENoteJudge.Miss) {
					pChip.bShow = false;
				}
				if (eJudgeResult != ENoteJudge.Auto && eJudgeResult != ENoteJudge.Miss) {
					this.actJudgeString.Start(nPlayer, eJudgeResult);
					bool isBigInput = nNowInput is NotesManager.EInputType.RedBig or NotesManager.EInputType.BlueBig || !OpenTaiko.ConfigIni.bJudgeBigNotes;
					this.PlayHitNoteSound(nPlayer, nNowInput);
					this.StartHitNoteLaneFlash(nPlayer, nNowInput, gt);
					OpenTaiko.stageGameScreen.actLaneTaiko.Start(pChip, gt, eJudgeResult, isBigInput, nPlayer);
					OpenTaiko.stageGameScreen.actChipFireD.Start(pChip, gt, eJudgeResult, isBigInput, nPlayer);
					if (eJudgeResult is not ENoteJudge.Poor) {
						this.actTaikoLaneFlash.PlayerLane[nPlayer].Start(PlayerLane.FlashType.Hit, gt);
						OpenTaiko.stageGameScreen.FlyingNotes.Start(NotesManager.GetFlyNoteType(pChip, gt, isBigInput), gt, nPlayer);
					}
				}
			}
		}

		this.UpdateGauge(pChip, screenmode, nPlayer, eJudgeResult);
		if (!isDeniedJudgeCount)
		this.UpdateJudgeCount(pChip, nPlayer, bAutoPlay, bBombHit, eJudgeResult, msDelta);
		this.UpdateComboMilestone(pChip, nPlayer);
		this.AddScore(pChip, nPlayer, eJudgeResult);

		// Dynamic Beat: immediate and section tracking
		if (!isDeniedJudgeCount && OpenTaiko.ConfigIni.nFunMods[nPlayer] == EFunMods.DynamicBeat) {
			bool isAdLib = pChip != null && NotesManager.IsADLIB(pChip);
			bool isMine  = pChip != null && NotesManager.IsMine(pChip);
			if (isAdLib && eJudgeResult != ENoteJudge.Miss) {
				ApplyDynamicBeatFactor(+0.01);
			} else if (isMine && bBombHit) {
				ApplyDynamicBeatFactor(-0.05);
			} else if (pChip != null && NotesManager.IsMissableNote(pChip)) {
				nDynBeatSectionNotes[nPlayer]++;
				if (eJudgeResult == ENoteJudge.Perfect)
					nDynBeatSectionPerfects[nPlayer]++;
				else if (eJudgeResult == ENoteJudge.Miss)
					nDynBeatSectionBads[nPlayer]++;
			}
		}

		return eJudgeResult;
	}

	// Note: use ENoteJudge.Auto to simply update gauge status
	protected void UpdateGauge(CChip? pChip, EKeyConfigPart screenmode, int nPlayer, ENoteJudge eJudgeResult) {
		bool hasFailed = this.IsStageFailed(nPlayer);
		if (!hasFailed) { // prevent gauge change if song aborted
			if (eJudgeResult is ENoteJudge.Bad && (NotesManager.IsMine(pChip) || NotesManager.IsFuzeRoll(pChip))) {
				actGauge.MineDamage(nPlayer, (pChip == null || pChip.IsEndedBranching) ? null : pChip.nBranch);
			} else if (pChip == null || NotesManager.IsMissableNote(pChip)) {
				actGauge.Damage(screenmode, eJudgeResult, nPlayer, (pChip == null || pChip.IsEndedBranching) ? null : pChip.nBranch);
			}
		}

		bool cleared = HGaugeMethods.UNSAFE_FastNormaCheck(nPlayer);

		bool isIncrease = eJudgeResult is not (ENoteJudge.Poor or ENoteJudge.Bad or ENoteJudge.Miss) || eJudgeResult is ENoteJudge.Auto;
		bool isDecrease = (eJudgeResult is ENoteJudge.Poor or ENoteJudge.Bad || eJudgeResult is ENoteJudge.Auto
			|| ((pChip != null) ? (pChip.IsMissed && NotesManager.IsMissableNote(pChip)) : eJudgeResult is ENoteJudge.Miss));
		bool? isEndOfPlay = null;

		if (isIncrease) {
			// ランナー(たたけたやつ)
			if (eJudgeResult is not ENoteJudge.Auto)
				this.actRunner.Start(nPlayer, false, pChip);

			CCharacter character = CCharacter.GetCharacter(nPlayer);

			if (HGaugeMethods.UNSAFE_IsRainbow(nPlayer) && this.bIsAlreadyMaxed[nPlayer] == false) {
				this.bIsAlreadyMaxed[nPlayer] = true;

				actChara.ReturnDefaultAnime(nPlayer);
				actChara.PlayGameAction(nPlayer, CCharacter.ANIM_GAME_MAX_IN);

				if (isEndOfPlay ??= this.IsEndOfPlay())
					this.UpdateClearAnimation(nPlayer);
			}
			if (cleared && this.bIsAlreadyCleared[nPlayer] == false) {
				this.bIsAlreadyCleared[nPlayer] = true;

				actChara.ReturnDefaultAnime(nPlayer);
				actChara.PlayGameAction(nPlayer, CCharacter.ANIM_GAME_CLEAR_IN);

				OpenTaiko.stageGameScreen.actBackground.ClearIn(nPlayer);
				if (isEndOfPlay ??= this.IsEndOfPlay())
					this.UpdateClearAnimation(nPlayer);
			}
		}
		if (isDecrease) {
			int Character = this.actChara.iCurrentCharacter[nPlayer];

			// ランナー(みすったやつ)
			if (eJudgeResult is not ENoteJudge.Auto)
				this.actRunner.Start(nPlayer, true, pChip);
			if (!HGaugeMethods.UNSAFE_IsRainbow(nPlayer) && this.bIsAlreadyMaxed[nPlayer] == true) {
				this.bIsAlreadyMaxed[nPlayer] = false;
				actChara.PlayGameAction(nPlayer, CCharacter.ANIM_GAME_MAX_OUT);
				if (isEndOfPlay ??= this.IsEndOfPlay())
					this.UpdateClearAnimation(nPlayer);
			} else if (!bIsGOGOTIME[nPlayer]) {
				if (Chara_MissCount[nPlayer] == 1 - 1) {
					actChara.PlayGameAction(nPlayer, CCharacter.ANIM_GAME_MISS_IN);
				} else if (Chara_MissCount[nPlayer] == 6 - 1) {
					actChara.PlayGameAction(nPlayer, CCharacter.ANIM_GAME_MISS_DOWN_IN);
				}
			}
			if (!cleared && this.bIsAlreadyCleared[nPlayer] == true) {
				this.bIsAlreadyCleared[nPlayer] = false;
				actChara.PlayGameAction(nPlayer, CCharacter.ANIM_GAME_CLEAR_OUT);
				OpenTaiko.stageGameScreen.actBackground.ClearOut(nPlayer);
				if (isEndOfPlay ??= this.IsEndOfPlay())
					this.UpdateClearAnimation(nPlayer);

				switch (HGaugeMethods.tGetGaugeTypeEnum(nPlayer)) {
					case HGaugeMethods.EGaugeType.HARD:
					case HGaugeMethods.EGaugeType.EXTREME:
						OpenTaiko.stageGameScreen.SetStageFailed(nPlayer);
						break;
				}
			}
		}
	}

	protected virtual void UpdateClearAnimation(int iPlayer) { }

	private void UpdateJudgeCount(CChip? pChip, int nPlayer, bool bAutoPlay, bool bBombHit, ENoteJudge eJudgeResult, int? msDelta = null) {
		OpenTaiko.HttpEventReporter.ReportNoteJudgement(eJudgeResult, nPlayer, pChip, msDelta);

		void returnChara() {
			CCharacter character = CCharacter.GetCharacter(nPlayer);
			if (!bIsGOGOTIME[nPlayer]) {
				{
					// 魂ゲージMAXではない
					// ジャンプ_ノーマル
					actChara.PlayGameAction(nPlayer, CCharacter.ANIM_GAME_RETURN);
					//this.actChara.キャラクター_アクション_10コンボ();
				}
			}
		}


		switch (eJudgeResult) {
			case ENoteJudge.Perfect: {
					if (NotesManager.IsGenericRoll(pChip) || NotesManager.IsADLIB(pChip))
						break;

					this.Chara_MissCount[nPlayer] = 0;

					if (pChip != null) {
						bool isBig = NotesManager.IsBigNoteTaiko(pChip, NotesManager.GetChipGameType(pChip, nPlayer));
						ForEachBiggable(isBig, forBigOnly => {
							if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
								this.DanSongScore[actDan.NowShowingNumber].GetBiggable(forBigOnly).nGreat++;
							this.CBranchScore[nPlayer].GetBiggable(forBigOnly).nGreat++;
							this.CChartScore[nPlayer].GetBiggable(forBigOnly).nGreat++;
							this.CSectionScore[nPlayer].GetBiggable(forBigOnly).nGreat++;
						});

						if (nPlayer == 0)
							(!bAutoPlay ? this.nHitCount_ExclAuto : this.nHitCount_InclAuto).Perfect++;
						this.actCombo.nCurrentCombo[nPlayer]++;

						if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
							this.tIncreaseComboDan(actDan.NowShowingNumber);

						if (this.actCombo.ctComboAddCounter[nPlayer].IsUnEnded) {
							this.actCombo.ctComboAddCounter[nPlayer].CurrentValue = 1;
						} else {
							this.actCombo.ctComboAddCounter[nPlayer].CurrentValue = 0;
						}

						AIRegisterInput(nPlayer, 1);
					}

					OpenTaiko.stageGameScreen.actMtaiko.BackSymbolEvent(nPlayer);


					if (this.bIsMiss[nPlayer]) {
						this.bIsMiss[nPlayer] = false;
						actChara.ReturnDefaultAnime(nPlayer);
						returnChara();
					}

					// #GIANTNOTE: Perfect (良) = good grade — activates good trigger; if link=true also ok trigger
					if (pChip != null) {
						if (!string.IsNullOrEmpty(pChip.GiantNoteGoodTrigger))
							OpenTaiko.GetTJA(nPlayer)?.LocalTriggers.Store(pChip.GiantNoteGoodTrigger, "1");
						if (pChip.GiantNoteLink && !string.IsNullOrEmpty(pChip.GiantNoteOkTrigger))
							OpenTaiko.GetTJA(nPlayer)?.LocalTriggers.Store(pChip.GiantNoteOkTrigger, "1");
					}
				}
				break;
			case ENoteJudge.Great:
			case ENoteJudge.Good: {
					if (NotesManager.IsGenericRoll(pChip))
						break;

					this.Chara_MissCount[nPlayer] = 0;

					if (pChip != null) {
						bool isBig = NotesManager.IsBigNoteTaiko(pChip, NotesManager.GetChipGameType(pChip, nPlayer));
						ForEachBiggable(isBig, forBigOnly => {
							if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
								this.DanSongScore[actDan.NowShowingNumber].GetBiggable(forBigOnly).nGood++;
							this.CBranchScore[nPlayer].GetBiggable(forBigOnly).nGood++;
							this.CChartScore[nPlayer].GetBiggable(forBigOnly).nGood++;
							this.CSectionScore[nPlayer].GetBiggable(forBigOnly).nGood++;
						});

						if (nPlayer == 0)
							(!bAutoPlay ? this.nHitCount_ExclAuto : this.nHitCount_InclAuto).Great++;
						this.actCombo.nCurrentCombo[nPlayer]++;

						if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
							this.tIncreaseComboDan(actDan.NowShowingNumber);

						if (this.actCombo.ctComboAddCounter[nPlayer].IsUnEnded) {
							this.actCombo.ctComboAddCounter[nPlayer].CurrentValue = 1;
						} else {
							this.actCombo.ctComboAddCounter[nPlayer].CurrentValue = 0;
						}

						AIRegisterInput(nPlayer, 0.5f);
					}

					OpenTaiko.stageGameScreen.actMtaiko.BackSymbolEvent(nPlayer);

					if (this.bIsMiss[nPlayer]) {
						this.bIsMiss[nPlayer] = false;
						actChara.ReturnDefaultAnime(nPlayer);
						returnChara();
					}

					// #GIANTNOTE: Great/Good (可) = ok grade — activates ok trigger only
					if (pChip != null && !string.IsNullOrEmpty(pChip.GiantNoteOkTrigger))
						OpenTaiko.GetTJA(nPlayer)?.LocalTriggers.Store(pChip.GiantNoteOkTrigger, "1");
				}
				break;
			case ENoteJudge.Miss:
				if (pChip?.IsMissed ?? true)
					goto case ENoteJudge.Poor;
				break;
			case ENoteJudge.Poor:
			case ENoteJudge.Bad: {
					if (NotesManager.IsGenericRoll(pChip) || !(pChip == null || NotesManager.IsMissableNote(pChip) || bBombHit))
						break;

					if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
						FloorManagement.damage();

					this.Chara_MissCount[nPlayer]++;

					if (pChip != null) {
						if (!bBombHit) {
							bool isBig = NotesManager.IsBigNoteTaiko(pChip, NotesManager.GetChipGameType(pChip, nPlayer));
							ForEachBiggable(isBig, forBigOnly => {
								if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
									this.DanSongScore[actDan.NowShowingNumber].GetBiggable(forBigOnly).nMiss++;
								this.CBranchScore[nPlayer].GetBiggable(forBigOnly).nMiss++;
								this.CChartScore[nPlayer].GetBiggable(forBigOnly).nMiss++;
								this.CSectionScore[nPlayer].GetBiggable(forBigOnly).nMiss++;
							});

							if (nPlayer == 0)
								(!bAutoPlay ? this.nHitCount_ExclAuto : this.nHitCount_InclAuto).Miss++;
						}
						AIRegisterInput(nPlayer, 0f);
					}

					this.actCombo.nCurrentCombo[nPlayer] = 0;
					if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
						this.DanSongScore[actDan.NowShowingNumber].nCombo = 0;
					this.actComboVoice.tReset(nPlayer);


					this.bIsMiss[nPlayer] = true;

					CCharacter character = CCharacter.GetCharacter(nPlayer);
					if (!HGaugeMethods.UNSAFE_IsRainbow(nPlayer) && this.bIsAlreadyMaxed[nPlayer] == true) {
						this.bIsAlreadyMaxed[nPlayer] = false;
						actChara.ReturnDefaultAnime(nPlayer);
						actChara.PlayGameAction(nPlayer, CCharacter.ANIM_GAME_MAX_OUT);
					} else if (!bIsGOGOTIME[nPlayer]) {
						if (Chara_MissCount[nPlayer] == 1) {
							actChara.ReturnDefaultAnime(nPlayer);
							actChara.PlayGameAction(nPlayer, CCharacter.ANIM_GAME_MISS_IN);
						} else if (Chara_MissCount[nPlayer] == 6) {
							actChara.ReturnDefaultAnime(nPlayer);
							actChara.PlayGameAction(nPlayer, CCharacter.ANIM_GAME_MISS_DOWN_IN);
						}
					}

				}
				break;
			default:
				if (pChip != null)
					this.nHitCount_InclAuto[(int)eJudgeResult]++;
				break;
		}
		actDan.Update();
		if (this.IsChartEnded())
			this.UpdateClearAnimation(nPlayer);
	}

	private void UpdateComboMilestone(CChip pChip, int nPlayer) {
		if (NotesManager.IsMissableNote(pChip)) {
			if ((this.actCombo.nCurrentCombo[nPlayer] % 100 == 0 || this.actCombo.nCurrentCombo[nPlayer] == 50) && this.actCombo.nCurrentCombo[nPlayer] > 0) {
				this.actComboBalloon.Start(this.actCombo.nCurrentCombo[nPlayer], nPlayer);
			}

			// Combo voice here
			this.actComboVoice.tPlay(this.actCombo.nCurrentCombo[nPlayer], nPlayer);

			//CDTXMania.act文字コンソール.tPrint(620, 80, C文字コンソール.Eフォント種別.白, "BPM: " + dbUnit.ToString());

			for (int i = 0; i < 5; i++) {
				if (this.actCombo.nCurrentCombo[i] == 50 || this.actCombo.nCurrentCombo[i] == 300) {
					ctChipAnimeLag[i] = new CCounter(0, 664, 1, OpenTaiko.Timer);
				}
			}

			if (this.actCombo.nCurrentCombo[nPlayer] % 10 == 0 && this.actCombo.nCurrentCombo[nPlayer] > 0) {
				if (!pChip.bGOGOTIME) //2018.03.11 kairera0467 チップに埋め込んだフラグから読み取る
					actChara.PlayGameAction(nPlayer, HGaugeMethods.UNSAFE_IsRainbow(nPlayer) ? CCharacter.ANIM_GAME_10COMBO_MAX : CCharacter.ANIM_GAME_10COMBO);
			}

			this.tConfetti_Start();
		}
	}

	private void AddScore(CChip pChip, int nPlayer, ENoteJudge eJudgeResult) {
		if ((eJudgeResult != ENoteJudge.Miss) && (eJudgeResult != ENoteJudge.Bad) && (eJudgeResult != ENoteJudge.Poor) && (NotesManager.IsMissableNote(pChip))) {
			int nCombos = this.actCombo.nCurrentCombo[nPlayer];
			long nInit = OpenTaiko.TJA.PlayerSideMetadata.nScoreInit[0];
			long nDiff = OpenTaiko.TJA.PlayerSideMetadata.nScoreDiff;
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

			int __score = (int)(this.actScore.Get(nPlayer));
			this.CBranchScore[nPlayer].nScore = __score;
			this.CChartScore[nPlayer].nScore = __score;
			this.CSectionScore[nPlayer].nScore = __score;
		}
	}

	protected void tChipHitProcess_BadAndTightWhenMiss(EKeyConfigPart screenmode, ENoteJudge eJudgeResult, int nPlayer, CTja.ECourse? eCourse) {
		this.actJudgeString.Start(nPlayer, eJudgeResult);
		this.UpdateGauge(null, screenmode, nPlayer, eJudgeResult);
		this.UpdateJudgeCount(null, nPlayer, false, false, eJudgeResult);
	}

	protected bool IsInputPadConsumedByChip(int nPlayer, CChip chip, EPad pad, long msTjaTime, ENoteJudge judge)
		=> NotesManager.IsGenericBalloon(chip) || this.JudgePadInput(nPlayer, chip, pad, msTjaTime, judge, skipHit: true) is not ENoteJudge.Miss;

	protected (CChip? chip, ENoteJudge rawJudge) GetChipToJudge(long msTjaTime, int nPlayer, EPad pad) {
		var (chip, judge) = GetChipToJudgeIgnoringRollBody(msTjaTime, nPlayer, pad);
		var chipRoll = this.chipCurrentProcessingRollChip[nPlayer].FirstOrDefault(x => x.bVisible && !x.bHit);
		if (chipRoll != null && ( // note is miss or past BAD, or roll is defined earlier -> judge roll if exists
			chip == null || judge is ENoteJudge.Miss || ((chip.nSoundTimems < msTjaTime && judge is ENoteJudge.Poor) && !(NotesManager.IsADLIB(chip) || NotesManager.IsMine(chip)))
			|| chipRoll.nIntValue_InternalNumber < chip.nIntValue_InternalNumber
			)) {
			var judgeRoll = this.eGetChipJudgeAtTime(msTjaTime, chipRoll, nPlayer);
			if (judgeRoll is not ENoteJudge.Miss && this.IsInputPadConsumedByChip(nPlayer, chipRoll, pad, msTjaTime, judgeRoll))
				(chip, judge) = (chipRoll, judgeRoll);
		}
		return (chip, judge);
	}

	protected (CChip? chip, ENoteJudge rawJudge) GetChipToJudgeIgnoringRollBody(long msTjaTime, int nPlayer, EPad pad) {
		int count = listChip[nPlayer].Count;
		if (count <= 0)         // 演奏データとして1個もチップがない場合は
			return (null, ENoteJudge.Miss);

		#region [ search for the first future note chips ]
		// search backward for the top chip at given time
		int iTop = Math.Max(0, Math.Min(count, this.nCurrentTopChip[nPlayer]));
		if ((iTop < count) && (msTjaTime < this.listChip[nPlayer][iTop].nSoundTimems)) {
			CChip searchChip = new() { nSoundTimems = (int)msTjaTime, dbSoundTimems = double.PositiveInfinity }; // chip is played until this
			iTop = this.listChip[nPlayer].BinarySearch(0, iTop, searchChip, Comparer<CChip>.Default);
			if (iTop < 0)
				iTop = ~iTop;
		}

		(CChip? chip, ENoteJudge judge) futureFirstUnhit = (null, ENoteJudge.Miss);
		int iFutureFirst = count; // regardless of hit or unhit
		for (int i = iTop; i < count; ++i) {
			CChip chip = listChip[nPlayer][i];
			if (!(chip.bVisible && chip.nSoundTimems > msTjaTime && NotesManager.IsHittableNote(chip) && !NotesManager.IsRollEnd(chip)))
				continue;
			if (iFutureFirst >= count)
				iFutureFirst = i;
			var judge = this.eGetChipJudgeAtTime(msTjaTime, chip, nPlayer);
			if (judge is ENoteJudge.Miss) // not in judgement window or before a roll
				break;
			if (!chip.IsMissed && !chip.bHit && this.IsInputPadConsumedByChip(nPlayer, chip, pad, msTjaTime, judge)) {
				futureFirstUnhit = (chip, judge);
				break;
			}
		}
		#endregion

		#region [ search for the first past note chips (ignore rolls) ]
		(CChip? chip, ENoteJudge judge) pastFirstUnhit = (null, ENoteJudge.Miss);
		(CChip? chip, ENoteJudge judge) pastFirstUnhitNotBad = (null, ENoteJudge.Miss);
		(CChip? chip, ENoteJudge judge) pastFirstUnhitRoll = (null, ENoteJudge.Miss);
		var firstWaitingChip = this.chipNowProcessingMultiHitNotes[nPlayer].FirstOrDefault();
		for (int i = iFutureFirst; i-- > 0;) { // exclude past from future
			CChip chip = listChip[nPlayer][i];
			if (!chip.bVisible || !NotesManager.IsHittableNote(chip) || NotesManager.IsRollEnd(chip))
				continue;
			var judge = (chip.eNoteState is ENoteState.Wait) ? ENoteJudge.Perfect : this.eGetChipJudgeAtTime(msTjaTime, chip, nPlayer);
			if (judge is ENoteJudge.Miss && (firstWaitingChip == null || chip.nSoundTimems < firstWaitingChip.nSoundTimems)) // search over waiting notes
				break; // not in judgement window or after a roll
			if (!chip.IsMissed && !chip.bHit && this.IsInputPadConsumedByChip(nPlayer, chip, pad, msTjaTime, judge)) {
				if (NotesManager.IsGenericRoll(chip)) {
					if (pastFirstUnhitRoll.chip == null || chip.nIntValue_InternalNumber < pastFirstUnhitRoll.chip.nIntValue_InternalNumber)
						pastFirstUnhitRoll = (chip, judge);
				} else {
					pastFirstUnhit = (chip, judge);
					if (NotesManager.IsJudgedFromNearest(chip))
						break; // block search
					else if (judge is not ENoteJudge.Poor)
						pastFirstUnhitNotBad = (chip, judge);
				}
			}
		}
		#endregion
		// most past note is miss, BAD, or judged by nearest -> judge most past non-BAD note if exists
		if (pastFirstUnhitNotBad.chip != null)
			pastFirstUnhit = pastFirstUnhitNotBad;
		// past note is miss, BAD, or judged by nearest, or roll is defined earlier -> judge roll if exists
		if (pastFirstUnhitRoll.chip != null && (pastFirstUnhit.chip == null || pastFirstUnhitNotBad.chip == null || pastFirstUnhitRoll.chip.nIntValue_InternalNumber < pastFirstUnhit.chip.nIntValue_InternalNumber))
			pastFirstUnhit = pastFirstUnhitRoll;

		#region [ choose the best judgement if not both are non-BAD ]
		bool isPastNotMiss = pastFirstUnhit.judge is not ENoteJudge.Miss;
		bool isFutureNotMiss = futureFirstUnhit.judge is not ENoteJudge.Miss;
		if (!(isPastNotMiss && isFutureNotMiss))
			return isFutureNotMiss ? futureFirstUnhit : pastFirstUnhit;

		// past note is roll, future note is not miss, note is within roll and defined earlier -> judge note
		if (NotesManager.IsGenericRoll(pastFirstUnhit.chip) && !NotesManager.IsRollEnd(pastFirstUnhit.chip)
			&& futureFirstUnhit.chip!.nSoundTimems <= pastFirstUnhit.chip!.end.nSoundTimems && (futureFirstUnhit.chip!.nIntValue_InternalNumber < pastFirstUnhit.chip!.nIntValue_InternalNumber)
			)
			return futureFirstUnhit;

		bool isPastNotBad = pastFirstUnhit.judge is not ENoteJudge.Poor || NotesManager.IsADLIB(pastFirstUnhit.chip) || NotesManager.IsMine(pastFirstUnhit.chip);
		bool isFutureNotBad = futureFirstUnhit.judge is not ENoteJudge.Poor || NotesManager.IsADLIB(futureFirstUnhit.chip) || NotesManager.IsMine(futureFirstUnhit.chip);
		if (!(isPastNotBad && isFutureNotBad))
			return isFutureNotBad ? futureFirstUnhit : pastFirstUnhit;
		#endregion

		// for balloon-type head judgement window
		if (NotesManager.IsGenericRoll(futureFirstUnhit.chip) && !NotesManager.IsRollEnd(futureFirstUnhit.chip)
			&& (futureFirstUnhit.chip!.nIntValue_InternalNumber < pastFirstUnhit.chip!.nIntValue_InternalNumber)
			)
			return futureFirstUnhit;

		if (!NotesManager.IsJudgedFromNearest(pastFirstUnhit.chip))
			return pastFirstUnhit;

		// past note is judged from nearest
		int msTjaDTime_Future = Math.Abs((int)(msTjaTime - futureFirstUnhit.chip!.nSoundTimems));
		int msTjaDTime_Past = Math.Abs((int)(msTjaTime - pastFirstUnhit.chip!.nSoundTimems));
		return (msTjaDTime_Future < msTjaDTime_Past) ? futureFirstUnhit : pastFirstUnhit;
	}

	public bool rIsChipInSearchRange(long nTime, int nSearchRangeTimems, int nPlayer) {
		for (int i = 0; i < listChip[nPlayer].Count; i++) {
			CChip chip = listChip[nPlayer][i];
			if (chip.bVisible && !chip.bHit) {
				if (NotesManager.IsMissableNote(chip)) {
					if (chip.nSoundTimems < nTime + nSearchRangeTimems) {
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

	protected abstract void tInputProcess_Drums();
	protected abstract void DrumsScrollSpeedUp();
	protected abstract void DrumsScrollSpeedDown();
	protected void tKeyInput() {
		// Inputs
		if (this.IsQuittingStage())
			return;

		IInputDevice keyboard = OpenTaiko.InputManager.Keyboard;

		if (!this.bPAUSE) {
			this.tInputProcess_Drums();

			CTja tja = OpenTaiko.TJA;

			// Individual offset
			if (keyboard.KeyPressed((int)SlimDXKeys.Key.UpArrow) && (keyboard.KeyPressing((int)SlimDXKeys.Key.RightShift) || keyboard.KeyPressing((int)SlimDXKeys.Key.LeftShift))) {    // shift (+ctrl) + UpArrow (BGMAdjust)
				OpenTaiko.TJA.tEachAutoPlaySoundChipPlaybackTimeChange((keyboard.KeyPressing((int)SlimDXKeys.Key.LeftControl) || keyboard.KeyPressing((int)SlimDXKeys.Key.RightControl)) ? 1 : 10);
				OpenTaiko.TJA.tWavePlaybackPositionAutoCorrection();
			} else if (keyboard.KeyPressed((int)SlimDXKeys.Key.DownArrow) && (keyboard.KeyPressing((int)SlimDXKeys.Key.RightShift) || keyboard.KeyPressing((int)SlimDXKeys.Key.LeftShift))) {   // shift + DownArrow (BGMAdjust)
				OpenTaiko.TJA.tEachAutoPlaySoundChipPlaybackTimeChange((keyboard.KeyPressing((int)SlimDXKeys.Key.LeftControl) || keyboard.KeyPressing((int)SlimDXKeys.Key.RightControl)) ? -1 : -10);
				OpenTaiko.TJA.tWavePlaybackPositionAutoCorrection();
			}
			// Tokkun only
			else if (OpenTaiko.ConfigIni.bTokkunMode &&
					 OpenTaiko.ConfigIni.KeyAssign.Taiko.TrainingIncreaseScrollSpeed.IsPressed()) {  // UpArrow(scrollspeed up)
				DrumsScrollSpeedUp();
			} else if (OpenTaiko.ConfigIni.bTokkunMode &&
					   OpenTaiko.ConfigIni.KeyAssign.Taiko.TrainingDecreaseScrollSpeed.IsPressed()) {  // DownArrow (scrollspeed down)
				DrumsScrollSpeedDown();
			}
			// Debug mode
			else if (OpenTaiko.ConfigIni.KeyAssign.System.DisplayDebug.IsPressed()) {   // del (debug info)
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
			else if (keyboard.KeyPressed([(int)SlimDXKeys.Key.Escape, (int)SlimDXKeys.Key.F1])) {    // escape (exit)
				if (LuaNetworking.Active?.PlaySyncActive == true) {
					// online play: pause (and the restart that lives in the pause menu) is disabled for fairness
					LogNotification.PopInfo("Pause is disabled during online play.");
				} else if (!this.actPauseMenu.bIsActivePopupMenu && this.bPAUSE == false) {
					long cooldownRemaining = 1000 - _pauseCooldown.ElapsedMilliseconds;
					if (cooldownRemaining > 0) {
						LogNotification.PopInfo($"Pause on cooldown. Please wait {cooldownRemaining / 1000.0:F1}s.");
					} else {
						OpenTaiko.Skin.soundChangeSFX.tPlay();
						this.Pause();
						this.actPauseMenu.tActivatePopupMenu(0);
					}
				}
				// this.t演奏中止();
			} else if (OpenTaiko.ConfigIni.KeyAssign.Taiko.TrainingBranchNormal.IsPressed()) {
				this.TrainingSwitchBranch(CTja.ECourse.eNormal);
			} else if (OpenTaiko.ConfigIni.KeyAssign.Taiko.TrainingBranchExpert.IsPressed()) {
				this.TrainingSwitchBranch(CTja.ECourse.eExpert);
			} else if (OpenTaiko.ConfigIni.KeyAssign.Taiko.TrainingBranchMaster.IsPressed()) {
				this.TrainingSwitchBranch(CTja.ECourse.eMaster);
			}

			if (OpenTaiko.ConfigIni.KeyAssign.System.DisplayHits.IsPressed()) {
				if (OpenTaiko.ConfigIni.bJudgeCountDisplay == false)
					OpenTaiko.ConfigIni.bJudgeCountDisplay = true;
				else
					OpenTaiko.ConfigIni.bJudgeCountDisplay = false;
			}

			if (OpenTaiko.ConfigIni.KeyAssign.System.CycleVideoDisplayMode.IsPressed()) {
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

			if (OpenTaiko.ConfigIni.bTokkunMode && OpenTaiko.ConfigIni.KeyAssign.Taiko.TrainingToggleAuto.IsPressed()) {
				OpenTaiko.ConfigIni.bAutoPlay[0] = !OpenTaiko.ConfigIni.bAutoPlay[0];
			}
		}

#if DEBUG

		if (keyboard.KeyPressed((int)SlimDXKeys.Key.F7)) {
			OpenTaiko.ConfigIni.bAutoPlay[1] = !OpenTaiko.ConfigIni.bAutoPlay[1];
		}
#endif
		if (!this.actPauseMenu.bIsActivePopupMenu && this.bPAUSE) {
			if (keyboard.KeyPressed((int)SlimDXKeys.Key.UpArrow)) { // UpArrow(scrollspeed up)
				DrumsScrollSpeedUp();
			} else if (keyboard.KeyPressed((int)SlimDXKeys.Key.DownArrow)) {    // DownArrow (scrollspeed down)
				DrumsScrollSpeedDown();
			} else if (OpenTaiko.ConfigIni.KeyAssign.System.DisplayDebug.IsPressed()) {   // del (debug info)
				OpenTaiko.ConfigIni.bDisplayDebugInfo = !OpenTaiko.ConfigIni.bDisplayDebugInfo;
			} else if ((keyboard.KeyPressed((int)SlimDXKeys.Key.Escape))) {   // escape (exit)
				this.tPlayAbort();
			}
		}

		#region [ Minus & Equals Sound Group Level ]
		KeyboardSoundGroupLevelControlHandler.Handle(
			keyboard, OpenTaiko.SoundGroupLevelController, OpenTaiko.Skin, false);
		#endregion
	}

	// ── BGM drift watchdog ─────────────────────────────────────────────────────────
	// The play timer free-runs on the OS clock (bUseOSTimer default), so a long frame stall (GC, driver)
	// that starves the audio feed leaves the music permanently behind the chart — previously only a manual
	// pause→resume re-seeked it back. Watch the BGM position against the timer and, when a large drift
	// persists over two consecutive checks, re-seek through the same correction the BGMAdjust keys use.
	private long msLastBgmDriftCheck = 0;
	private int nBgmDriftStrikes = 0;
	private const long msBgmDriftCheckInterval = 500;
	private const long msBgmDriftThreshold = 100;

	protected void tCheckBgmDrift() {
		if (this.bPAUSE || this.isRewinding || this.IsFailStopped()) {
			this.nBgmDriftStrikes = 0;
			return;
		}
		// Dynamic Beat retunes the music speed mid-song (SetSpeedWhilePlaying) — the constant-speed mapping
		// between timer time and stream position no longer holds, so any "drift" measured here is phantom
		// and correcting it repeatedly seeks the music ahead of the chart.
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			if (OpenTaiko.ConfigIni.nFunMods[i] == EFunMods.DynamicBeat) {
				this.nBgmDriftStrikes = 0;
				return;
			}
		}
		long now = SoundManager.PlayTimer.SystemTimeMs;
		if (now - this.msLastBgmDriftCheck < msBgmDriftCheckInterval) return;
		this.msLastBgmDriftCheck = now;

		CTja tja = OpenTaiko.TJA;
		if (tja?.listWAV == null) return;

		long worst = 0;
		foreach (CTja.CWAV wc in tja.listWAV.Values) {
			// same "long sound" gate as tWavePlaybackPositionAutoCorrection
			if (wc.rSound[0] == null || wc.rSound[0].TotalPlayTime < 5000) continue;
			for (int i = 0; i < wc.rSound.Length; i++) {
				CSound snd = wc.rSound[i];
				if (snd == null || !snd.IsPlaying) continue;
				long expected = now - wc.nPlaybackStartTime[i] + wc.nInitialSeekMs;
				long timelineLength = (long)(snd.TotalPlayTime / Math.Max(0.01, snd.Frequency * snd.PlaySpeed));
				if (expected < 1000 || expected > timelineLength - 1000) continue;   // start settle-in / near end
				long actual = snd.tGetPositionOnTimelineMs();
				if (actual < 0) continue;
				long drift = actual - expected;
				if (Math.Abs(drift) > Math.Abs(worst)) worst = drift;
			}
		}

		if (Math.Abs(worst) > msBgmDriftThreshold) {
			if (++this.nBgmDriftStrikes >= 2) {
				tja.tWavePlaybackPositionAutoCorrection();
				Trace.TraceWarning($"[BGMDrift] music drifted {worst}ms from the play timer (frame stall?) — re-seeked to sync.");
				this.nBgmDriftStrikes = 0;
			}
		} else {
			this.nBgmDriftStrikes = 0;
		}
	}

	public void Pause() {
		this.bPAUSE = true;
		SoundManager.PlayTimer.Pause();
		OpenTaiko.Timer.Pause();
		OpenTaiko.TJA.tAllChipPlaybackPause();
		this.actAVI.Pause();
	}

	public void Resume(long? msStartGameTime = null) {
		OpenTaiko.TJA.tAllChipPlaybackReOpen();
		OpenTaiko.Timer.Resume();
		msStartGameTime ??= OpenTaiko.Timer.NowTimeMs; // target time defaults to pre-pause time
		OpenTaiko.Timer.Reset(); // reset internal pause timer
		OpenTaiko.Timer.NowTimeMs = msStartGameTime.Value; // jump to target time
		SoundManager.PlayTimer.Resume();
		SoundManager.PlayTimer.Reset();
		SoundManager.PlayTimer.NowTimeMs = OpenTaiko.Timer.NowTimeMs; // sync with game time

		this.actAVI.Resume();
		this.actPanel.Start();
		this.bPAUSE = false;                                // システムがPAUSE状態だったら、強制解除
		_pauseCooldown.Restart();
	}

	private void TrainingSwitchBranch(CTja.ECourse branch) {
		if (!(OpenTaiko.ConfigIni.bTokkunMode || OpenTaiko.ConfigIni.bAutoPlay[0]))
			return;

		CTja tja = OpenTaiko.TJA!;
		if (!tja.PlayerSideMetadata.bHasBranch) return;

		// use last reached measure
		var measure = tja.listChip.ElementAtOrDefault(tja.GetListChipIndexOfMeasure(this.actPlayInfo.NowMeasure[0], this.nCurrentBranch[0]));
		double dbOneMeasureAfter = 0.0;
		if (measure != null)
			dbOneMeasureAfter = Math.Max(0, ((15000.0 / measure.dbBPM * (measure.fNow_Measure_s / measure.fNow_Measure_m)) * 16.0));
		double msBranchPoint = tja.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs) + dbOneMeasureAfter;

		if (!this.bUseBranch[0]) {
			this.bUseBranch[0] = true;
			OpenTaiko.stageGameScreen.actLaneTaiko.BranchText_FadeIn(0, 0);
		}
		this.tBranchProcess(branch, 0, msBranchPoint, measure?.idxBranchSection ?? this.idxLastBranchSection[0]);
		OpenTaiko.stageGameScreen.ChangeBranch(branch, 0, msBranchPoint);
		this.bForcedBranch[0] = true;
	}

	protected void tInputMethodStore(EKeyConfigPart part) {
		if (OpenTaiko.Pad.detectedDevice.Keyboard) {
			this.bUsedKeyboardInPlay = true;
		}
		if (OpenTaiko.Pad.detectedDevice.Joypad) {
			this.bUsedJoypadInPlay = true;
		}
		if (OpenTaiko.Pad.detectedDevice.MIDIIN) {
			this.bUsedMidiInputInPlay = true;
		}
		if (OpenTaiko.Pad.detectedDevice.Mouse) {
			this.bUsedMouseInPlay = true;
		}
	}

	public virtual void SetStageFailed(int iPlayer, EStageAbort failType = EStageAbort.FailedFlow) {
		if (OpenTaiko.ConfigIni.bTokkunMode)
			return;
		if (!OpenTaiko.ConfigIni.bAIBattleMode) { // allowing play to end in AI battle mode
			isFinishedPlaying[iPlayer] = true;
			isDeniedPlaying[iPlayer] = true; // Prevents the player to ever be able to hit the drum, without freezing the whole game
		}
		if (stageAbortType[iPlayer] < failType) {
			if (stageAbortType[iPlayer] < EStageAbort.FailedStop && failType >= EStageAbort.FailedStop)
				msFailedStopSystemTime = SoundManager.PlayTimer.NowTimeMs;
			stageAbortType[iPlayer] = failType;
		}
	}
	public bool IsStageFailed(int iPlayer) => stageAbortType[iPlayer] != EStageAbort.None;
	public EStageAbort MinStageAbortType => stageAbortType.Take(OpenTaiko.ConfigIni.nPlayerCount).Min();
	public bool IsStageFailed() => MinStageAbortType != EStageAbort.None;
	public bool IsFailStopped() => !OpenTaiko.ConfigIni.bAIBattleMode && MinStageAbortType >= EStageAbort.FailedStop;
	public bool IsChartEnded() => isChartEnded.Take(OpenTaiko.ConfigIni.nPlayerCount).All(x => x);
	public bool IsChartEnded(int iPlayer) => isChartEnded[iPlayer];
	public bool IsFinishedPlaying() => isFinishedPlaying.Take(OpenTaiko.ConfigIni.nPlayerCount).All(x => x);
	public bool IsFinishedPlaying(int iPlayer) => isFinishedPlaying[iPlayer];
	public virtual bool IsEndOfPlay(bool? isChartEnded = null, bool? isFinishedPlaying = null)
		=> (isChartEnded ?? IsChartEnded()) || (isFinishedPlaying ?? IsFinishedPlaying());
	public bool IsStageFailed_Fast()
		=> ePhaseID == CStage.EPhase.Game_STAGE_FAILED || ((ePhaseID is CStage.EPhase.Game_EndStage_FadeOut or CStage.EPhase.Game_EndStage_Quit_FadeOut) && IsStageFailed());
	public bool IsStageCompleted() => ePhaseID is CStage.EPhase.Game_EndChart or CStage.EPhase.Game_EndStage or CStage.EPhase.Game_EndStage_FadeOut or CStage.EPhase.Game_EndStage_Quit_FadeOut;
	public bool IsQuittingStage() => ePhaseID is CStage.EPhase.Common_FADEOUT or CStage.EPhase.Game_EndStage_Quit_FadeOut;

	protected bool tProgressDraw_AVI() {
		if (this.IsStageFailed_Fast() && (this.actAVI?.rVD.bPlaying ?? false)) {
			this.actAVI.Pause(); // paused but still shown
		}
		if (OpenTaiko.ConfigIni.bEnableAVI) {
			this.actAVI.Draw();
			return true;
		}
		return false;
	}


	protected void tProgressDraw_PanelString() {
			this.actPanel.Draw();
		}
	protected void tPanelStringSettings() {
		var panelString = string.IsNullOrEmpty(OpenTaiko.TJA.PANEL) ? OpenTaiko.TJA.TITLE.GetString("") : OpenTaiko.TJA.PANEL;

		this.actPanel.SetPanelString(panelString,
			OpenTaiko.SongMount.rChoosenSong.songGenrePanel,
			OpenTaiko.Skin.Game_StageText,
			songNode: OpenTaiko.SongMount.rChoosenSong);
	}


	protected void tProgressDraw_Gauge() {
			this.actGauge.Draw();
		}
	protected void tProgressDraw_Combo() {
		this.actCombo.Draw();
	}
	protected void tProgressDraw_Score() {
		this.actScore.Draw();
	}

	// Per-player scan time for the replay precise auto-miss/timeout (see tReplayAutoMissBefore). Reset per play.
	protected readonly double[] msReplayTjaTime = new double[5];
	protected double msMaxPlayedTjaTime(int nPlayer)
		=> OpenTaiko.bReplayMode[nPlayer] ? this.msReplayTjaTime[nPlayer] : double.PositiveInfinity;

	protected bool tProgressDraw_Chip(EKeyConfigPart ePlayMode, int nPlayer) {
		bool drawOnly = this.IsFailStopped() || (this.nCurrentTopChip[nPlayer] == -1) || IsDanFailed;

		CTja tja = OpenTaiko.GetTJA(nPlayer)!;
		bool isDynBeat = OpenTaiko.ConfigIni.nFunMods[nPlayer] == EFunMods.DynamicBeat;
		long rawGameTime = this.IsFailStopped() ? this.msFailedStopSystemTime : SoundManager.PlayTimer.NowTimeMs;
		// Store for ApplyDynamicBeatFactor so the offset is computed against the same time used here
		if (nPlayer == 0) msDynBeatRawGameTime = rawGameTime;
		long nCurrentTimems = isDynBeat
			? (long)(tja.GameTimeToTjaTime(rawGameTime) * dbDynamicBeatFactor + dbDynBeatTjaOffset)
			: (long)tja.GameTimeToTjaTime(rawGameTime);

		NowAIBattleSectionTime = (int)nCurrentTimems - NowAIBattleSection.StartTime;

		var scrollRate = this.GetScrollRate(nPlayer);

		// Dynamic Beat: tick section timer and evaluate every 2 seconds (player 0 drives the shared timer)
		if (!drawOnly && !this.bPAUSE && isDynBeat && nPlayer == 0) {
			if (msDynBeatSectionStart == 0)
				msDynBeatSectionStart = rawGameTime;
			else if (rawGameTime - msDynBeatSectionStart >= 2000)
				EvaluateDynamicBeat();
		}

		CConfigIni configIni = OpenTaiko.ConfigIni;

		CTja dTX = OpenTaiko.GetTJA(nPlayer)!;
		bool bAutoPlay = configIni.bAutoPlay[nPlayer];
		if (nPlayer == 1)
			bAutoPlay = bAutoPlay || OpenTaiko.ConfigIni.bAIBattleMode;

		if (dTX.PlayerSideMetadata.bHasBranch && nCurrentTimems >= this.msTargetBranchTime[nPlayer]) {
			this.nCurrentBranch[nPlayer] = this.nTargetBranch[nPlayer];
			this.msTargetBranchTime[nPlayer] = double.MaxValue;
		}

		//CDTXMania.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.灰, this.nLoopCount_Clear.ToString()  );

		double play_time = tja.TjaTimeToRawTjaTimeNote(nCurrentTimems);
		var play_bpm_points = new[] {
			GetNowPBPMPoint(dTX, play_time, CTja.ECourse.eNormal),
			GetNowPBPMPoint(dTX, play_time, CTja.ECourse.eExpert),
			GetNowPBPMPoint(dTX, play_time, CTja.ECourse.eMaster),
		};
		double[] th16NowBeats = play_bpm_points.Select(bp => GetNowPBMTime(bp, play_time)).ToArray();

		#region [update phase, process forward for correct order of non-note events]
		for (; this.nCurrentTopChip[nPlayer] < dTX.listChip.Count; ++this.nCurrentTopChip[nPlayer]) {
			if (drawOnly)
				break;

			CChip pChip = dTX.listChip[this.nCurrentTopChip[nPlayer]];
			//Debug.WriteLine( "nCurrentTopChip=" + nCurrentTopChip + ", ch=" + pChip.nチャンネル番号.ToString("x2") + ", 発音位置=" + pChip.n発声位置 + ", 発声時刻ms=" + pChip.n発声時刻ms );
			if (!hasChipBeenPlayedAt(pChip, nCurrentTimems)) // not processed yet
				break;

			// handle last chip status of dan-i exams
			if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
				if (dTX.pDan_LastChip.Contains(pChip)) {
					this.actDan.Update();
					if (this.IsChartEnded())
						this.UpdateClearAnimation(nPlayer);
				}
			}

			switch (pChip.nChannelNo) {
				#region [ 01: BGM ]
				case 0x01:  // BGM
					if (!this.bPAUSE && !pChip.bHit) { // can't play while paused
						pChip.bHit = true;
						if (configIni.bBGMPlayVoiceSound) {
							dTX.tChipPlayback(pChip, SoundManager.PlayTimer.GameTimeToSystemTime((long)tja.TjaTimeToGameTime(pChip.nSoundTimems)));
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
				case 0x1D:
					// draw later
					break;
				case 0x18:
					// draw later
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
							this.UpdateAIBattleSection(nPlayer, nCurrentTimems);

							if (this.actPlayInfo.NowMeasure[nPlayer] == 0) {
								UpdateCharaCounter(nPlayer);
							}
							actPlayInfo.NowMeasure[nPlayer] = pChip.nIntValue_InternalNumber;
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
							if ((dTX.listVD.TryGetValue(pChip.nIntValue_InternalNumber, out CVideoDecoder vd))) {
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
							if ((dTX.listVD.TryGetValue(pChip.nIntValue_InternalNumber, out CVideoDecoder vd))) {
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
						this.ListDan_Number = pChip.nIntValue_InternalNumber;
						this.actPanel.tLyricsTextureRemove();
						this.actDan.Update();
						if (this.IsChartEnded())
							this.UpdateClearAnimation(nPlayer);
						if (ListDan_Number != 0 && actDan.FirstSectionAnime) {
							if (this.actDan.GetFailedAllChallenges(OpenTaiko.SongMount.rChoosenSong.DanSongs)) {
								this.nCurrentTopChip[nPlayer] = tja.listChip.Count - 1;   // 終端にシーク
								IsDanFailed = true;
								return true;
							}

							// Play next song here
							this.actDan.Start(this.ListDan_Number);
							this.timingZones[nPlayer] = CTja.GameDurationToTjaDuration(this.GetTimingZones(nPlayer));
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
							if (dTX.listBPM.ElementAtOrDefault(pChip.nIntValue_InternalNumber) is CTja.CBPM cBPM) {
								this.actPlayInfo.dbBPM[nPlayer] = cBPM.dbBPMValue;// + dTX.BASEBPM;
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
						pChip.ForEachTargetBranch(branch => this.bIsGOGOTIME_Branch[nPlayer, (int)branch] = true);
						if (true /* TJAP3/OOS */ || pChip.IsForBranch(this.nTargetBranch[nPlayer])) {
							this.bIsGOGOTIME[nPlayer] = true;
							if (!this.isRewinding)
								this.StartGoGoTimeEffect(nPlayer);
						}
					}
					break;
				case 0x9F: //ゴーゴータイム
					if (!pChip.bHit) {
						pChip.bHit = true;
						pChip.ForEachTargetBranch(branch => this.bIsGOGOTIME_Branch[nPlayer, (int)branch] = false);
						if (true /* TJAP3/OOS */ || pChip.IsForBranch(this.nTargetBranch[nPlayer])) {
							this.bIsGOGOTIME[nPlayer] = false;
							if (!this.isRewinding)
								actChara.ReturnDefaultAnime(nPlayer);
						}
					}
					break;
				#endregion

				#region [ EXTENDED COMMANDS ]
				case 0xa0: //camera vertical move start
				case 0xa2: //camera horizontal move start
				case 0xa4: //camera zoom start
				case 0xa6: //camera rotation start
				case 0xa8: //camera vertical scaling start
				case 0xb0: //camera horizontal scaling start
					if (!pChip.bHit) {
						pChip.bHit = true;
						this.objHandlers[GetObjHandlerKeys(pChip)[0]] = (pChip, new CCounter(0, pChip.fObjTimeMs, CTja.TjaDurationToGameDuration(1), OpenTaiko.Timer), GetObjHandlerSetter(pChip));
					}
					break;
				case 0xa1: //camera vertical move end
				case 0xa3: //camera horizontal move end
				case 0xa5: //camera zoom end
				case 0xa7: //camera rotation end
				case 0xa9: //camera vertical scaling end
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
				case 0xb4: //set camera y offset
				case 0xb5: //set camera zoom factor
				case 0xb6: //set camera rotation
				case 0xb7: //set camera x scale
				case 0xb8: //set camera y scale
					if (!pChip.bHit) {
						pChip.bHit = true;
						this.objHandlers[GetObjHandlerKeys(pChip)[0]] = (pChip, new CCounter(0, 0, 0, OpenTaiko.Timer), GetObjHandlerSetter(pChip));
					}
					break;
				case 0xb9: //reset camera
					if (!pChip.bHit) {
						pChip.bHit = true;

						foreach (var key in GetObjHandlerKeys(pChip)) {
							this.objHandlers.Remove(key);
						}
						OpenTaiko.ResetCameraStates();
					}
					break;
				case 0xba: //enable doron
					if (!pChip.bHit) {
						pChip.bHit = true;
						bCustomDoron[nPlayer] = true;
					}
					break;
				case 0xbb: //disable doron
					if (!pChip.bHit) {
						pChip.bHit = true;
						bCustomDoron[nPlayer] = false;
					}
					break;
				case 0xbc: //add object
					if (!pChip.bHit) {
						pChip.bHit = true;

						if (dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj)) {
							pChip.obj.x = pChip.fObjX;
							pChip.obj.y = pChip.fObjY;
							pChip.obj.isVisible = true;
						}
					}
					break;
				case 0xbd: //remove object
					if (!pChip.bHit) {
						pChip.bHit = true;

						if (dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj)) {
							pChip.obj.isVisible = false;
						}
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

						if (dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj))
							objHandlers[GetObjHandlerKeys(pChip)[0]] = (pChip, new CCounter(0, pChip.fObjTimeMs, CTja.TjaDurationToGameDuration(1), OpenTaiko.Timer), GetObjHandlerSetter(pChip));
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

						if (dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj)) {
							pChip.obj.color = pChip.borderColor;
						}
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

						if (dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj)) {
							this.objHandlers[GetObjHandlerKeys(pChip)[0]] = (pChip, new CCounter(0, 0, 0, OpenTaiko.Timer), GetObjHandlerSetter(pChip));
						}
					}
					break;
				case 0xd1: //change texture
					if (!pChip.bHit) {
						pChip.bHit = true;

						if (OpenTaiko.Tx.trackedTextures.ContainsKey(pChip.strTargetTxName)) {
							OpenTaiko.Tx.trackedTextures.TryGetValue(pChip.strTargetTxName, out CTexture oldTx);
							dTX.listTextures.TryGetValue(pChip.strNewPath, out CTexture newTx);

							newTx.Opacity = oldTx.Opacity;
							newTx.fZAxisCenterRotate = oldTx.fZAxisCenterRotate;
							newTx.vcScaleRatio = oldTx.vcScaleRatio;

							oldTx.UpdateTexture(newTx, newTx.szImageSize.Width, newTx.szImageSize.Height);
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
							originalTx.fZAxisCenterRotate = oldTx.fZAxisCenterRotate;
							originalTx.vcScaleRatio = oldTx.vcScaleRatio;

							oldTx.UpdateTexture(originalTx, originalTx.szImageSize.Width, originalTx.szImageSize.Height);
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
						if (dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj)) {
							pChip.obj.tStartAnimation(CTja.TjaDurationToGameDuration(pChip.dbAnimInterval), false);
						}
					}
					break;
				case 0xd5: //start object animation (looping)
					if (!pChip.bHit) {
						pChip.bHit = true;
						if (dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj)) {
							pChip.obj.tStartAnimation(CTja.TjaDurationToGameDuration(pChip.dbAnimInterval), true);
						}
					}
					break;
				case 0xd6: //end object animation
					if (!pChip.bHit) {
						pChip.bHit = true;
						if (dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj)) {
							pChip.obj.tStopAnimation();
						}
					}
					break;
				case 0xd7: //set object frame
					if (!pChip.bHit) {
						pChip.bHit = true;
						if (dTX.listObj.TryGetValue(pChip.strObjName, out pChip.obj)) {
							pChip.obj.frame = pChip.intFrame;
						}
					}
					break;
				#endregion

				#region [ d8-d9: EXTENDED2 ]
				case 0xd8:
					if (!pChip.bHit) {
						if (pChip.eGameType != null)
							this.eGameType[nPlayer] = pChip.eGameType.Value;
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
						if (listWAV.TryGetValue(pChip.nIntValue_InternalNumber, out CTja.CWAV wc)) // 参照が遠いので後日最適化する
						{
							for (int i = 0; i < nPolyphonicSounds; i++) {
								if (wc.rSound[i] != null) {
									//CDTXMania.Sound管理.AddMixer( wc.rSound[ i ] );
									AddMixer(wc.rSound[i], pChip.bPlayEndAfterPlaybackContinuesChip);
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
						if (listWAV.TryGetValue(pChip.nIntValue_InternalNumber, out CTja.CWAV wc)) // 参照が遠いので後日最適化する
						{
							for (int i = 0; i < nPolyphonicSounds; i++) {
								if (wc.rSound[i] != null) {
									//CDTXMania.Sound管理.RemoveMixer( wc.rSound[ i ] );
									if (!wc.rSound[i].bPlayEndAfterPlaybackContinuesChip)   // #32248 2013.10.16 yyagi
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
						if (pChip.idxBranchSection > this.idxLastBranchSection[nPlayer]) {
							if (!this.bUseBranch[nPlayer]) {
								this.bUseBranch[nPlayer] = true;
								OpenTaiko.stageGameScreen.actLaneTaiko.BranchText_FadeIn(0, nPlayer);
							}

							if (!this.bLEVELHOLD[nPlayer]) {
								CTja.ECourse targetBranch = this.tBranchJudge(nPlayer, pChip);
								this.tBranchProcess(targetBranch, nPlayer, idxBranchSection: pChip.idxBranchSection);
								OpenTaiko.stageGameScreen.ChangeBranch(targetBranch, nPlayer, pChip.nBranchTimems);
								if (pChip.hasLevelHold[(int)targetBranch])
									this.bLEVELHOLD[nPlayer] = true;
							}
							this.idxLastBranchSection[nPlayer] = pChip.idxBranchSection;
						}
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
						if (pChip.hasLevelHold[0]) // lock up branch at chip
							this.bLEVELHOLD[nPlayer] = true;
					}
					break;
				case 0xE2:
					if (!pChip.bHit) {
						CTja.CJPOSSCROLL jposs = dTX.listJPOSSCROLL[pChip.nIntValue_InternalNumber];
						OpenTaiko.stageGameScreen.actLaneTaiko.tJudgeFrameMove(nPlayer, jposs, pChip.nSoundTimems);
						pChip.bHit = true;
					}
					break;
				case 0xEA: // #STOREC
					if (!pChip.bHit) {
						if (IsCommandIfMet(pChip, dTX) && !string.IsNullOrEmpty(pChip.StoreCKey) && !string.IsNullOrEmpty(pChip.StoreCExpression))
							dTX.LocalCounters.Store(pChip.StoreCKey, pChip.StoreCExpression);
						pChip.bHit = true;
					}
					break;
				case 0xEB: // #STORET
					if (!pChip.bHit) {
						if (IsCommandIfMet(pChip, dTX) && !string.IsNullOrEmpty(pChip.StoreTKey) && !string.IsNullOrEmpty(pChip.StoreTExpression))
							dTX.LocalTriggers.Store(pChip.StoreTKey, pChip.StoreTExpression);
						pChip.bHit = true;
					}
					break;
				case 0xEC: // #ELEVATEC
					if (!pChip.bHit) {
						if (IsCommandIfMet(pChip, dTX) && !string.IsNullOrEmpty(pChip.ElevateCKey))
							dTX.LocalCounters.Elevate(pChip.ElevateCKey);
						pChip.bHit = true;
					}
					break;
				case 0xED: // #ELEVATET
					if (!pChip.bHit) {
						if (IsCommandIfMet(pChip, dTX) && !string.IsNullOrEmpty(pChip.ElevateTKey))
							dTX.LocalTriggers.Elevate(pChip.ElevateTKey);
						pChip.bHit = true;
					}
					break;
				case 0xEE: // #SONGJUMP
					if (!pChip.bHit) {
						if (IsCommandIfMet(pChip, dTX) && !string.IsNullOrEmpty(pChip.SongJumpUniqueId)) {
							var targetNode = CSongDict.tGetNodeFromID(pChip.SongJumpUniqueId);
						if (targetNode != null) {
								int jumpDiff = CSongMount.FindClosestDifficulty(targetNode, pChip.SongJumpDifficulty);
								OpenTaiko.SongMount.rChoosenSong = targetNode;
								for (int p = 0; p < OpenTaiko.ConfigIni.nPlayerCount; p++)
									OpenTaiko.SongMount.nChoosenSongDifficulty[p] = jumpDiff;
								OpenTaiko.SongMount.rChosenScore = targetNode.score[jumpDiff];
								OpenTaiko.SongMount.bIsAfterSongJump = true;
								OpenTaiko.SongMount.bSongJumpPending = true;
								for (int p = 0; p < OpenTaiko.ConfigIni.nPlayerCount; p++) {
									OpenTaiko.GetTJA(p)?.tStopAllChips();
									this.isChartEnded[p] = true;
								}
							}
						}
						pChip.bHit = true;
					}
					break;
				#endregion
				#region[ f1: 歌詞 ]
				case 0xF1:
					if (!pChip.bHit) {
						if (OpenTaiko.ConfigIni.nPlayerCount == 1) {
							if (pChip.nIntValue_InternalNumber >= 0 && pChip.nIntValue_InternalNumber < dTX.listLyric.Count) {
								this.actPanel.tLyricsTextureCreate(dTX.listLyric[pChip.nIntValue_InternalNumber]);
							}
						}
						pChip.bHit = true;
					}
					break;
				#endregion
				#region[ ff: 譜面の強制終了 ]
				//バグで譜面がとてつもないことになっているため、#ENDがきたらこれを差し込む。
				case 0xFF:
					if (!this.bPAUSE && !pChip.bHit) { // prevent infinity pause in training mode
						this.UpdateAIBattleSection(nPlayer, nCurrentTimems, endOfPlay: true);
						this.isChartEnded[nPlayer] = true;
						if (pChip.nIntValue != 0) { // 0: last note past, 0xFF: song end
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

		if (false /* Jiro1 */)
			this.bIsGOGOTIME[nPlayer] = this.bIsGOGOTIME_Branch[nPlayer, (int)this.nTargetBranch[nPlayer]];
		if (this.isRewinding) {
			this.isRewinding = false;
			if (this.bIsGOGOTIME[nPlayer] && this.bIsGOGOTIME[nPlayer] != this.bWasGOGOTIME[nPlayer]) {
				this.StartGoGoTimeEffect(nPlayer);
			}
			this.bWasGOGOTIME[nPlayer] = this.bIsGOGOTIME[nPlayer];
		}
		#endregion

		#region [update phase (bar lines' position)]
		foreach (var pChip in dTX.listBarLineChip) {
			if (drawOnly)
				break;
			if (!pChip.bVisible)
				continue;

			tja.UpdateScrolledChipPosition(pChip, play_bpm_points[(int)pChip.nBranch], nCurrentTimems, th16NowBeats[(int)pChip.nBranch], scrollRate);
		}
		#endregion

		#region [update phase (notes' position & auto judgement)]
		foreach (var pChip in dTX.listNoteChip) {
			if (drawOnly)
				break;
			if (NotesManager.IsGenericRoll(pChip) && pChip.nSoundTimems <= nCurrentTimems) {
				if (!pChip.bProcessed) {
					if (NotesManager.IsRollEnd(pChip))
						this.ProcessRollEnd(nPlayer, pChip, false);
					else if (pChip.bVisible)
						this.AddNowProcessingRollChip(nPlayer, pChip);
				}
			}

			if (!pChip.bVisible)
				continue;

			tja.UpdateScrolledChipPosition(pChip, play_bpm_points[(int)pChip.nBranch], nCurrentTimems, th16NowBeats[(int)pChip.nBranch], scrollRate);

			if (!this.bPAUSE && !this.isRewinding) {
				this.AutoJudge(nPlayer, nCurrentTimems, pChip, msMaxPlayedTjaTime: this.msMaxPlayedTjaTime(nPlayer));
			}
		}
		#endregion

		#region [draw phase (bar line), backward for correct stack order]
		for (int iChip = dTX.listBarLineChip.Count; iChip-- > 0;) {
			CChip pChip = dTX.listBarLineChip[iChip];
			switch (pChip.nChannelNo) {
				case 0x50: // 小節線
				case 0xe4: // #BARLINE
					this.tProgressDraw_Chip_MeasureLine(configIni, ref dTX, ref pChip, nPlayer, nCurrentTimems);
					break;
			}
		}
		#endregion

		#region [draw phase (note), backward for correct stack order]
		for (int iChip = dTX.listNoteChip.Count; iChip-- > 0;) {
			CChip pChip = dTX.listNoteChip[iChip];
				this.tProgressDraw_Chip_Taiko(configIni, ref dTX, ref pChip, nPlayer, nCurrentTimems);
		}
		#endregion

		#region [ EXTENDED CONTROLS ]
		List<string> keysToRemove = new();
		foreach (var (key, (chip, counter, setter)) in this.objHandlers) {
			counter.Tick();

			float value = 0.0f;
			if (counter.IsEnded) {
				value = chip.fObjEnd;
				keysToRemove.Add(key);
			} else {
				if (chip.strObjEaseType.Equals("IN")) value = Easing.EaseIn(counter, chip.fObjStart, chip.fObjEnd, chip.objCalcType);
				if (chip.strObjEaseType.Equals("OUT")) value = Easing.EaseOut(counter, chip.fObjStart, chip.fObjEnd, chip.objCalcType);
				if (chip.strObjEaseType.Equals("IN_OUT")) value = Easing.EaseInOut(counter, chip.fObjStart, chip.fObjEnd, chip.objCalcType);
				value = float.IsNaN(value) ? chip.fObjStart : value;
			}


			setter(value);
		}
		foreach (var key in keysToRemove) {
			this.objHandlers.Remove(key);
		}
		#endregion

		return false;
	}

	private void AutoJudge(int nPlayer, long msTjaHitTime, CChip pChip, bool doAutoInput = true, double msMaxPlayedTjaTime = double.PositiveInfinity) {
		if (!pChip.IsMissed && !pChip.bHit) {
			if (NotesManager.IsGenericRoll(pChip) && !NotesManager.IsRollEnd(pChip)) {
				if (pChip.end.nSoundTimems <= msTjaHitTime) {
					var msJudgeTjaTime = (long)Math.Max(pChip.end.nSoundTimems, Math.Min(msTjaHitTime, msMaxPlayedTjaTime));
					if (this.eGetChipJudgeAtTime(msJudgeTjaTime, pChip, nPlayer) == ENoteJudge.Miss) {
						pChip.bHit = true;
					}
				} else if (pChip.nSoundTimems <= msTjaHitTime) {
					//時間内でかつ0x9Aじゃないならならヒット処理
					if (doAutoInput)
						this.Autoroll(pChip, msTjaHitTime, nPlayer, NotesManager.GetChipGameType(pChip, nPlayer));
				}
			} else if (NotesManager.IsHittableNote(pChip) && pChip.eNoteState != ENoteState.Wait) {
				//こっちのほうが適格と考えたためフラグを変更.2020.04.20 Akasoko26
				if (doAutoInput && !NotesManager.IsMine(pChip))
					this.AutoplayHit(pChip, msTjaHitTime, nPlayer, NotesManager.GetChipGameType(pChip, nPlayer));
				if (pChip.nSoundTimems <= msTjaHitTime) {
					var msJudgeTjaTime = (long)Math.Max(pChip.nSoundTimems, Math.Min(msTjaHitTime, msMaxPlayedTjaTime));
					if (!this.IsNoteIfMet(pChip, nPlayer)) {
						pChip.bHit = true; // skip silently — trigger condition not met
					} else if (this.eGetChipJudgeAtTime(msJudgeTjaTime, pChip, nPlayer) == ENoteJudge.Miss) {
						pChip.IsMissed = true;
						this.tChipHitProcess(msTjaHitTime, pChip, EKeyConfigPart.Taiko, false, 0, nPlayer);
						pChip.eNoteState = ENoteState.Bad; // set after hit processing for detecting duplicated misses
					}
				}
			}
		}
	}

	protected abstract void MultiHitNoteTimeout(int iPlayer, CChip chip, double msTjaNowTime, double msMaxPlayedTjaTime = double.PositiveInfinity);

	private void UpdateAIBattleSection(int nPlayer, long nCurrentTimems, bool endOfPlay = false) {
		if (nPlayer != 0)
			return;
		bool anySectionPassed = false;
		while (AIBattleSections.Count > NowAIBattleSectionCount && (endOfPlay || NowAIBattleSectionTime >= NowAIBattleSection.Length)) {
			if (NowAIBattleSection.End == AIBattleSection.EndType.None)
				PassAIBattleSection();

			actAIBattle.BatchAnimeCounter.CurrentValue = 0;
			_AIBattleState = 0;
			OpenTaiko.NamePlate?.tNamePlateRefreshTitles(1);

			NowAIBattleSectionCount++;
			anySectionPassed = true;

			NowAIBattleSectionTime = (int)nCurrentTimems - NowAIBattleSection.StartTime;
		}
		if (anySectionPassed && AIBattleSections.Count > NowAIBattleSectionCount) {
			for (int i = 0; i < 5; i++) {
				this.CSectionScore[i] = new CBRANCHSCORE();
			}
		}
	}

	private double GetScrollRate(int iPlayer)
		=> (this.actScrollSpeed.dbConfigScrollSpeed[iPlayer] + 1.0) / 10.0;

	private static double GetDynamicBeatAccelerationThreshold(double factor) => factor switch {
		< 1.05 => 50.0,
		< 1.10 => 70.0,
		< 1.20 => 80.0,
		< 1.30 => 90.0,
		< 1.40 => 92.0,
		< 1.50 => 95.0,
		_      => 98.0
	};

	protected void ApplyDynamicBeatFactor(double delta) {
		int playerCount = Math.Max(1, OpenTaiko.ConfigIni.nPlayerCount);
		double newFactor = Math.Max(1.0, dbDynamicBeatFactor + delta / playerCount);
		if (newFactor == dbDynamicBeatFactor) return;
		// Offset must cancel the jump that multiplying by the new factor would introduce.
		// n現在時刻ms = GameTimeToTjaTime(rawGameTime) * F + offset
		// For continuity: offset -= GameTimeToTjaTime(rawGameTime) * (newF - oldF)
		// Using rawGameTime * SongPlaybackSpeed here would be WRONG because GameTimeToTjaTime
		// also subtracts MusicPreTimeMs (~2500ms), causing a large jump per factor change.
		double tjaBase = OpenTaiko.GetTJA(0)?.GameTimeToTjaTime(msDynBeatRawGameTime)
		                 ?? (msDynBeatRawGameTime * OpenTaiko.ConfigIni.SongPlaybackSpeed);
		dbDynBeatTjaOffset -= tjaBase * (newFactor - dbDynamicBeatFactor);
		dbDynamicBeatFactor = newFactor;
		OpenTaiko.TJA?.tUpdateDynamicBeatSpeed(OpenTaiko.ConfigIni.SongPlaybackSpeed * newFactor);
	}

	private void EvaluateDynamicBeat() {
		int totalNotes = 0, totalPerfects = 0, totalBads = 0;
		for (int p = 0; p < OpenTaiko.ConfigIni.nPlayerCount; p++) {
			totalNotes    += nDynBeatSectionNotes[p];
			totalPerfects += nDynBeatSectionPerfects[p];
			totalBads     += nDynBeatSectionBads[p];
		}
		if (totalNotes > 0) {
			if (totalBads > 0) {
				ApplyDynamicBeatFactor(-0.05);
			} else {
				double accuracy = totalPerfects / (double)totalNotes * 100.0;
				if (accuracy >= GetDynamicBeatAccelerationThreshold(dbDynamicBeatFactor))
					ApplyDynamicBeatFactor(+0.05);
			}
		}
		for (int p = 0; p < OpenTaiko.ConfigIni.nPlayerCount; p++) {
			nDynBeatSectionPerfects[p] = 0;
			nDynBeatSectionBads[p]     = 0;
			nDynBeatSectionNotes[p]    = 0;
		}
		msDynBeatSectionStart = SoundManager.PlayTimer.NowTimeMs;
	}

	private static Comparer<CChip> NowProcessingRollComparer = Comparer<CChip>.Create((a, b) => a.nIntValue_InternalNumber.CompareTo(b.nIntValue_InternalNumber));

	private static Action<float> GetObjHandlerSetter(CChip chip)
		=> chip.nChannelNo switch {
			0xA0 or 0xB4 => (value) => OpenTaiko.fCamYOffset = value,
			0xA2 or 0xB3 => (value) => OpenTaiko.fCamXOffset = value,
			0xA4 or 0xB5 => (value) => OpenTaiko.fCamZoomFactor = value,
			0xA6 or 0xB6 => (value) => OpenTaiko.fCamRotation = value,
			0xA8 or 0xB8 => (value) => OpenTaiko.fCamYScale = value,
			0xB0 or 0xB7 => (value) => OpenTaiko.fCamXScale = value,
			0xB9 => (value) => OpenTaiko.ResetCameraStates(),
			0xBE or 0xCB => (value) => chip.obj!.y = value,
			0xC0 or 0xCC => (value) => chip.obj!.x = value,
			0xC2 or 0xCD => (value) => chip.obj!.yScale = value,
			0xC4 or 0xCE => (value) => chip.obj!.xScale = value,
			0xC6 or 0xCF => (value) => chip.obj!.rotation = value,
			0xC8 or 0xD0 => (value) => chip.obj!.opacity = (int)value,
			_ => throw new ArgumentOutOfRangeException(nameof(chip)),
		};

	private static string[] GetObjHandlerKeys(CChip chip)
		=> chip.nChannelNo switch {
			0xA0 or 0xB4 => ["cam_y"],
			0xA2 or 0xB3 => ["cam_x"],
			0xA4 or 0xB5 => ["cam_zoom"],
			0xA6 or 0xB6 => ["cam_rotation"],
			0xA8 or 0xB8 => ["cam_yScale"],
			0xB0 or 0xB7 => ["cam_xScale"],
			0xB9 => ["cam_x", "cam_y", "cam_zoom", "cam_rotation", "cam_xScale", "cam_yScale"],
			0xBE or 0xCB => [$"obj_{chip.strObjName}_y"],
			0xC0 or 0xCC => [$"obj_{chip.strObjName}_x"],
			0xC2 or 0xCD => [$"obj_{chip.strObjName}_yScale"],
			0xC4 or 0xCE => [$"obj_{chip.strObjName}_xScale"],
			0xC6 or 0xCF => [$"obj_{chip.strObjName}_rotation"],
			0xC8 or 0xD0 => [$"obj_{chip.strObjName}_opacity"],
			_ => throw new ArgumentOutOfRangeException(nameof(chip)),
		};

	private void AddNowProcessingRollChip(int iPlayer, CChip chip) {
		//if( this.n現在のコース == pChip.nコース )
		bool alreadyProcessing = false;
		int idx = this.chipCurrentProcessingRollChip[iPlayer].BinarySearch(chip, NowProcessingRollComparer);
		if (idx < 0)
			idx = ~idx;
		if (ReferenceEquals(this.chipCurrentProcessingRollChip[iPlayer].ElementAtOrDefault(idx), chip))
			alreadyProcessing = true;
		else
			this.chipCurrentProcessingRollChip[iPlayer].Insert(idx, chip);
		if (chip.bVisible && !chip.IsHitted) {
			if (NotesManager.IsKusudama(chip) && !alreadyProcessing) {
				chip.KusudamaCount += chip.nBalloon;
			}
			if (!this.bPAUSE && !this.isRewinding) {
				this.ProcessRollHeadEffects(iPlayer, chip);
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
			if (iPlayer == 0) {
				actBalloon.KusuIn();
				actChara.KusuIn();
				this.actChara.IsInKusudama = true;
				for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
					if (chip.multiLink != null) {
						for (int b = 0; b <= (int)CTja.ECourse.eMaster; ++b) {
							var linkedChip = chip.multiLink[i, b];
							if (!ReferenceEquals(linkedChip, chip) && (linkedChip?.bVisible ?? false))
								this.AddNowProcessingRollChip(i, linkedChip);
						}
					}
				}
			}
			this.actChara.bBalloonRoll[iPlayer] = false; // prevent normal balloon animation
		}
	}

	public void ProcessRollEnd(int iPlayer, CChip chip, bool resetStates) {
		if (NotesManager.IsRollEnd(chip))
			chip = chip.start;
		if (!NotesManager.IsGenericRoll(chip))
			return;

		if (!resetStates)
			chip.bHit = true;
		if (chip.bVisible && !chip.IsHitted) {
			if (NotesManager.IsGenericBalloon(chip)) {
				if (NotesManager.IsKusudama(chip)) {
					if (iPlayer == 0) {
						if (!this.bPAUSE && !this.isRewinding) {
							actBalloon.KusuMiss();
							OpenTaiko.Skin.soundKusudamaMiss.tPlay();
							for (int p = 0; p < OpenTaiko.ConfigIni.nPlayerCount; p++) {
								if (chip.multiLink != null) {
									for (int b = 0; b <= (int)CTja.ECourse.eMaster; ++b) {
										var linkedChip = chip.multiLink[p, b];
										if (!ReferenceEquals(linkedChip, chip) && (linkedChip?.bVisible ?? false))
											this.ProcessRollEnd(p, linkedChip, resetStates);
									}
								}
							}
						}
						chip.KusudamaRollCount = 0;
						chip.KusudamaCount = 0;
					}
				}
				if (NotesManager.IsFuzeRoll(chip)) {
					if (!this.bPAUSE && !this.isRewinding) {
						EGameType gt = NotesManager.GetChipGameType(chip, iPlayer);
						this.actJudgeString.Start(iPlayer, ENoteJudge.Mine);
						OpenTaiko.stageGameScreen.actLaneTaiko.Start(chip, gt, ENoteJudge.Bad, false, iPlayer);
						OpenTaiko.stageGameScreen.actChipFireD.Start(chip, gt, ENoteJudge.Mine, false, iPlayer);
						OpenTaiko.Skin.soundBomb?.tPlay();
						chip.bVisible = false;
						this.Chara_MissCount[iPlayer]++;
						if (!(this.isDeniedPlaying[iPlayer] || this.IsStageFailed_Fast())) {
						this.CChartScore[iPlayer].nMine++;
						this.CSectionScore[iPlayer].nMine++;
						this.CBranchScore[iPlayer].nMine++;
						if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
							this.DanSongScore[actDan.NowShowingNumber].nMine++;
							this.AIRegisterInput(iPlayer, 0f);

							this.actDan.Update();
							if (this.IsChartEnded())
								this.UpdateClearAnimation(iPlayer);
						}
						if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
							FloorManagement.damage();
						this.actCombo.nCurrentCombo[iPlayer] = 0;
						if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
							this.DanSongScore[actDan.NowShowingNumber].nCombo = 0;
						this.actComboVoice.tReset(iPlayer);
						this.bIsMiss[iPlayer] = true;

						UpdateGauge(chip, EKeyConfigPart.Taiko, iPlayer, ENoteJudge.Bad);
						if (OpenTaiko.ConfigIni.nFunMods[iPlayer] == EFunMods.DynamicBeat)
							ApplyDynamicBeatFactor(-0.05);
					}
				}
			}
		}
		this.RemoveNowProcessingRollChip(iPlayer, chip, resetStates);

		// process here for the correct default animation
		if (!this.bPAUSE && !this.isRewinding) {
			if (NotesManager.IsKusudama(chip)) {
				actChara.KusuMiss(iPlayer);
			} else if (NotesManager.IsGenericBalloon(chip)) {
				if (chip.nRollCount > 0)
					actChara.PlayGameAction(iPlayer, CCharacter.ANIM_GAME_BALLOON_MISS);
			}
		}
	}

	public void ProcessBalloonBroke(int iPlayer, CChip chip, double msHitTjaTime, NotesManager.EInputType input) {
		if (NotesManager.IsRollEnd(chip))
			chip = chip.start;
		if (!NotesManager.IsGenericBalloon(chip))
			return;

		if (!OpenTaiko.ConfigIni.ShinuchiMode) {
			bool pastKusudamaBorder = NotesManager.IsKusudama(chip) && (msHitTjaTime >= chip.msKusudamaBonusBorder);

			long nAddScore = 0;
			if (chip.bGOGOTIME) {
				nAddScore = (pastKusudamaBorder ? 1200L : 6000L);
			} else {
				nAddScore = (pastKusudamaBorder ? 1000L : 5000L);
			}

			this.actScore.Add(nAddScore, iPlayer);

			int __score = (int)(this.actScore.Get(iPlayer));
			this.CBranchScore[iPlayer].nScore = __score;
			this.CChartScore[iPlayer].nScore = __score;
			this.CSectionScore[iPlayer].nScore = __score;
		}

		if (NotesManager.IsKusudama(chip)) {
			if (input != NotesManager.EInputType.Unknown) { // finished from this player
			OpenTaiko.Skin.soundKusudama.tPlay();
			if (!OpenTaiko.Skin.soundKusudama.bIsPlaying)
				this.PlayHitNoteSound(iPlayer, input); // fallback sound
				actBalloon.KusuBroke();
				chip.KusudamaRollCount = 0;
				chip.KusudamaCount = 0;
			}
			chip.bHit = true;
			chip.IsHitted = true;
			chip.bVisible = false;

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (chip.multiLink != null) {
					for (int b = 0; b <= (int)CTja.ECourse.eMaster; ++b) {
						var linkedChip = chip.multiLink[i, b];
						if (!ReferenceEquals(linkedChip, chip) && (linkedChip?.bVisible ?? false))
							this.ProcessBalloonBroke(i, linkedChip, msHitTjaTime, NotesManager.EInputType.Unknown);
					}
				}
			}
		} else {
			//ﾊﾟｧｰﾝ
			OpenTaiko.Skin.soundBalloon.tPlay();
			if (!OpenTaiko.Skin.soundBalloon.bIsPlaying)
				this.PlayHitNoteSound(iPlayer, input); // fallback sound
			OpenTaiko.stageGameScreen.FlyingNotes.Start(NotesManager.ENoteType.DonBig, NotesManager.GetChipGameType(chip, iPlayer), iPlayer, forceFirework: true);
			OpenTaiko.stageGameScreen.Rainbow.Start(iPlayer);
			//CDTXMania.stage演奏ドラム画面.actChipFireD.Start( 0, player );
			chip.bHit = true;
			chip.IsHitted = true;
			chip.bVisible = false;
			if (NotesManager.IsFuzeRoll(chip)) {
				this.CChartScore[iPlayer].nMineAvoid++;
				this.CSectionScore[iPlayer].nMineAvoid++;
				this.CBranchScore[iPlayer].nMineAvoid++;
				if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
					this.DanSongScore[actDan.NowShowingNumber].nMineAvoid++;
				this.AIRegisterInput(iPlayer, 1f);
			}
		}
		this.RemoveNowProcessingRollChip(iPlayer, chip, false);

		// process here for the correct default animation
		if (NotesManager.IsKusudama(chip)) {
			actChara.KusuSuccess(iPlayer);
		} else if (NotesManager.IsGenericBalloon(chip)) {
			actChara.PlayGameAction(iPlayer, CCharacter.ANIM_GAME_BALLOON_BROKE);
		}
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
				if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
					this.DanSongScore[actDan.NowShowingNumber].nBalloonHitPass += chip.nBalloon;
			} else {
				this.CChartScore[iPlayer].nBarRollPass++;
				this.CSectionScore[iPlayer].nBarRollPass++;
				this.CBranchScore[iPlayer].nBarRollPass++;
				if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
					this.DanSongScore[actDan.NowShowingNumber].nBarRollPass++;

				double msRollLength = chip.end.nSoundTimems - chip.nSoundTimems;
				this.CChartScore[iPlayer].msBarRollPass += msRollLength;
				this.CSectionScore[iPlayer].msBarRollPass += msRollLength;
				this.CBranchScore[iPlayer].msBarRollPass += msRollLength;
				if (OpenTaiko.SongMount.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
					this.DanSongScore[actDan.NowShowingNumber].msBarRollPass += msRollLength;
			}
			this.actDan.Update();
			if (this.IsChartEnded())
				this.UpdateClearAnimation(iPlayer);
		}

		this.chipCurrentProcessingRollChip[iPlayer].Remove(chip);
		this.UpdateRollStateAfterRemove(iPlayer);
		if (resetStates || (!this.bPAUSE && !this.isRewinding)) {
			chip.bProcessed = !resetStates;
			chip.end.bProcessed = !resetStates;
		}
	}

	private void UpdateRollStateAfterRemove(int iPlayer) {
		if (!this.chipCurrentProcessingRollChip[iPlayer].Any(x => x.bVisible && NotesManager.IsGenericBalloon(x))) {
			this.actChara.bBalloonRoll[iPlayer] = false;
			this.actChara.IsInKusudama = false;
			this.actChara.ReturnDefaultAnime(iPlayer);
		} else if (!this.chipCurrentProcessingRollChip[iPlayer].Any(x => x.bVisible && NotesManager.IsKusudama(x))) {
			this.actChara.IsInKusudama = false;
			this.actChara.ReturnDefaultAnime(iPlayer);
		}
	}

	public void StartGoGoTimeEffect(int iPlayer) {
		CCharacter character = CCharacter.GetCharacter(iPlayer);

		actChara.ReturnDefaultAnime(iPlayer);

		{
			if (!HGaugeMethods.UNSAFE_IsRainbow(iPlayer) && (!HGaugeMethods.UNSAFE_FastNormaCheck(iPlayer))) {
				// 魂ゲージMAXではない
				// ゴーゴースタート_ノーマル
				actChara.PlayGameAction(iPlayer, CCharacter.ANIM_GAME_GOGOSTART);
				//this.actChara.キャラクター_アクション_10コンボ();
			}
			if (!HGaugeMethods.UNSAFE_IsRainbow(iPlayer) && HGaugeMethods.UNSAFE_FastNormaCheck(iPlayer)) {
				actChara.PlayGameAction(iPlayer, CCharacter.ANIM_GAME_GOGOSTART_CLEAR);
			}
			if (HGaugeMethods.UNSAFE_IsRainbow(iPlayer)) {
				// 魂ゲージMAX
				// ゴーゴースタート_MAX
				actChara.PlayGameAction(iPlayer, CCharacter.ANIM_GAME_GOGOSTART_MAX);
			}

		}
		OpenTaiko.stageGameScreen.actLaneTaiko.GOGOSTART();
	}

	public void tBranchReset(int player) {
		if (player != -1) {
			this.CBranchScore[player].Reset();
		} else {
			for (int i = 0; i < CBranchScore.Length; i++) {
				this.CBranchScore[i].Reset();
			}
		}
	}

	public CTja.ECourse tBranchJudge(int nPlayer, CChip pChip) {
		CBRANCHSCORE branchScore = this.CBranchScore[OpenTaiko.ConfigIni.bAIBattleMode ? 0 : nPlayer];
		return this.tBranchJudge(nPlayer, pChip, branchScore);
	}

	public CTja.ECourse tBranchJudge(int nPlayer, CChip pChip, CBRANCHSCORE branchScore) {
		// Branch check score here
		if (this.bLEVELHOLD[nPlayer] || this.bForcedBranch[nPlayer])
			return this.nTargetBranch[nPlayer];

		if (pChip.eBranchCondition.type == Exam.Type.None)
			return this.nTargetBranch[nPlayer]; // keep current branch

		// Local counter branch (#BRANCHSTART lc:key,v1,v2[,op])
		if (pChip.eBranchCondition.type == Exam.Type.LocalCounter) {
			CTja? tja = OpenTaiko.GetTJA(nPlayer);
			if (tja == null || string.IsNullOrEmpty(pChip.BranchLcKey))
				return this.nTargetBranch[nPlayer];
			double val = tja.LocalCounters.Get(pChip.BranchLcKey);
			if (CTExprRangeHelper.Compare(val, pChip.nBranchCondition2_Master,       pChip.eBranchConditionRange)) return CTja.ECourse.eMaster;
			if (CTExprRangeHelper.Compare(val, pChip.nBranchCondition1_Professional, pChip.eBranchConditionRange)) return CTja.ECourse.eExpert;
			return CTja.ECourse.eNormal;
		}

		// Local trigger branch (#BRANCHSTART lt,expertTrigger,masterTrigger)
		if (pChip.eBranchCondition.type == Exam.Type.LocalTrigger) {
			CTja? tja = OpenTaiko.GetTJA(nPlayer);
			if (tja == null) return this.nTargetBranch[nPlayer];
			bool t2 = !string.IsNullOrEmpty(pChip.BranchLt2Key) && tja.LocalTriggers.Get(pChip.BranchLt2Key);
			bool t1 = !string.IsNullOrEmpty(pChip.BranchLt1Key) && tja.LocalTriggers.Get(pChip.BranchLt1Key);
			if (t2) return CTja.ECourse.eMaster;
			if (t1) return CTja.ECourse.eExpert;
			return CTja.ECourse.eNormal;
		}

		double dbRate = branchScore.GetScore(pChip.eBranchCondition);

		if (CTExprRangeHelper.Compare(dbRate, pChip.nBranchCondition2_Master,       pChip.eBranchConditionRange)) return CTja.ECourse.eMaster;
		if (CTExprRangeHelper.Compare(dbRate, pChip.nBranchCondition1_Professional, pChip.eBranchConditionRange)) return CTja.ECourse.eExpert;
		return CTja.ECourse.eNormal;
	}

	private static bool IsCommandIfMet(CChip chip, CTja tja) {
		if (string.IsNullOrEmpty(chip.CommandIfTrigger)) return true;
		return tja.LocalTriggers.Get(chip.CommandIfTrigger);
	}

	private bool IsNoteIfMet(CChip chip, int nPlayer) {
		if (string.IsNullOrEmpty(chip.NoteIfTrigger)) return true;
		CTja? tja = OpenTaiko.GetTJA(nPlayer);
		return tja == null || tja.LocalTriggers.Get(chip.NoteIfTrigger);
	}

	public double GetBranchConditionScore(int nPlayer, (Exam.Type, CTja.EBranchCondBig) cond) {
		CBRANCHSCORE branchScore = this.CBranchScore[OpenTaiko.ConfigIni.bAIBattleMode ? 0 : nPlayer];
		return branchScore.GetScore(cond);
	}

	public void tBranchProcess(CTja.ECourse branch, int nPlayer, double msBranchPoint = double.MinValue, int idxBranchSection = -1) {
		CTja dTX = OpenTaiko.GetTJA(nPlayer)!;
		bool isAfterBranchPoint(double ms, int idx)
			=> (ms >= msBranchPoint && (idxBranchSection < 0 || idx >= idxBranchSection));

		// For `#BRANCHSTART`, skip processing earlier defined notes and bar lines
		bool rollingNotesMadeHidden = false;
		for (int i = 0; i < dTX.listChip.Count; i++) {
			var chip = dTX.listChip[i];
			bool isBarLine = (chip.nChannelNo == 0x50);
			bool isScrollable = (NotesManager.IsHittableNote(chip) || isBarLine || chip.nChannelNo == 0xE4);
			bool isRollEnd = NotesManager.IsRollEnd(chip);
			if (!(isScrollable && isAfterBranchPoint(chip.nSoundTimems, chip.idxBranchSection)))
				continue;

			// real bar line is inserted per-branch even in common branch
			// branched roll head + non-branched end is treated as branched head + end
			if (chip.IsEndedBranching && !isBarLine && (!isRollEnd || chip.start.IsEndedBranching))
				continue;

			if (chip.nBranch == branch) {
				// non-branched head + branched end
				if (isRollEnd && chip.start.IsEndedBranching && branch != CTja.ECourse.eNormal)
					continue; // Currently treated as non-branch roll with the Normal branch end being the end; do not show the non-Normal end

				chip.bVisible = true;
				if (isRollEnd && !chip.start.bHit && !isAfterBranchPoint(chip.start.nSoundTimems, chip.start.idxBranchSection)) {
					chip.start.bVisible = true; // show roll head before branch point and made hittable if end is shown
					if (!chip.start.bProcessed)
						this.AddNowProcessingRollChip(nPlayer, chip.start);
				}
			} else {
				// non-branched head + branched end
				if (isRollEnd && chip.start.IsEndedBranching)
					continue; // Currently treated as non-branch roll with the end being Normal branch end; not hidable

				chip.bVisible = false;
				chip.eNoteState = ENoteState.None; // cancel input
				if (isRollEnd && !isAfterBranchPoint(chip.start.nSoundTimems, chip.start.idxBranchSection)) {
					chip.start.bVisible = false; // hide hidable roll head before branch point if end is hide
					rollingNotesMadeHidden = true;
				}
			}

		}
		if (rollingNotesMadeHidden) {
			this.UpdateRollStateAfterRemove(nPlayer);
		}
	}

	public int GetRoll(int player) {
		return this.CChartScore[player].nRoll;
	}

	public static CTja.CBPM GetNowPBPMPoint(CTja tja, double play_time, CTja.ECourse branch) {
		var last_match = (int)branch; // Initial 3 for each branch
		for (int i = 3, iNext; i < tja.listBPM.Count; i = iNext) {
			//BPMCHANGEの数越えた
			for (iNext = i + 1; iNext < tja.listBPM.Count; ++iNext) {
				if (tja.listBPM[iNext].bpm_change_course == branch)
					break;
			}
			CTja.CBPM bpm = tja.listBPM[i];
			if (bpm.bpm_change_course != branch)
				continue;
			CTja.CBPM? bpm_next = (iNext < tja.listBPM.Count) ? tja.listBPM[iNext] : null;
			bool afterHead = ((int)play_time >= (int)bpm.bpm_change_time);
			bool beforeEnd = (bpm_next == null || ((int)play_time < (int)bpm_next.bpm_change_time));
			if (afterHead && beforeEnd) {
				last_match = i;
			}
		}
		return tja.listBPM[last_match];
	}

	public static double GetNowPBMTime(CTja.CBPM cBPM, double play_time) {
		return cBPM.bpm_change_bmscroll_time + (play_time - cBPM.bpm_change_time) * cBPM.dbBPMValue / 15000.0;
	}

	public void tReload() {
		OpenTaiko.TJA.tStopAllChipsAndRemoveFromMixer();
		this.eFadeOutCompleteWhenReturnValue = EGameplayScreenReturnValue.ReloadAndReplay;
		base.ePhaseID = CStage.EPhase.Game_Reload;
		this.bPAUSE = false;
	}

	public void tPlayRetry() {
		OpenTaiko.HttpEventReporter.ReportGameplayStart();
		OpenTaiko.TJA.tStopAllChipsAndRemoveFromMixer();
		//this.actAVI.Stop();
		foreach (var vd in OpenTaiko.TJA.listVD) {
			vd.Value.Stop();
		}
		this.actAVI.Stop();
		this.actPanel.tLyricsTextureRemove();
		var cleared = (bool[])bIsAlreadyCleared.Clone();
		this.tValueInitialize(true, true);
		var (idxStartChip, msStartGameTime) = this.tPlayPositionChange(0);
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			if (!bIsAlreadyCleared[i] && cleared[i]) {
				OpenTaiko.stageGameScreen.actBackground.ClearOut(i);
			}
		}
		this.Resume(msStartGameTime);
	}

	public void tStop() {
		OpenTaiko.TJA.tStopAllChipsAndRemoveFromMixer();
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

	public virtual void tValueInitialize(bool bPlayRecord, bool bPlayState) {
		this.isRewinding = true;

		if (bPlayRecord) {
			this.bUsedKeyboardInPlay = false;
			this.bUsedJoypadInPlay = false;
			this.bUsedMidiInputInPlay = false;
			this.bUsedMouseInPlay = false;

			this.nHitCount_InclAuto.Perfect = 0;
			this.nHitCount_InclAuto.Great = 0;
			this.nHitCount_InclAuto.Good = 0;
			this.nHitCount_InclAuto.Poor = 0;
			this.nHitCount_InclAuto.Miss = 0;

			this.nHitCount_ExclAuto.Perfect = 0;
			this.nHitCount_ExclAuto.Great = 0;
			this.nHitCount_ExclAuto.Good = 0;
			this.nHitCount_ExclAuto.Poor = 0;
			this.nHitCount_ExclAuto.Miss = 0;

			this.actCombo.Activate();
			this.actScore.Activate();
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				this.actGauge.Init(OpenTaiko.ConfigIni.nRisky, i);
			}
		}
		if (bPlayState) {
			_AIBattleStateBatch = new Queue<float>[] { new Queue<float>(), new Queue<float>() };
			bIsAIBattleWin = false;
			this.dbDynamicBeatFactor   = 1.0;
			this.dbDynBeatTjaOffset    = 0.0;
			this.msDynBeatRawGameTime  = 0;
			this.msDynBeatSectionStart = 0;

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				CCharacter character = CCharacter.GetCharacter(i);

				this.Chara_MissCount[i] = 0;
				this.bIsMiss[i] = false;
				this.bUseBranch[i] = false;
				this.nDynBeatSectionPerfects[i] = 0;
				this.nDynBeatSectionBads[i]     = 0;
				this.nDynBeatSectionNotes[i]    = 0;
				this.bLEVELHOLD[i] = false;
				this.bForcedBranch[i] = false;
				OpenTaiko.stageGameScreen.ChangeBranch(CTja.ECourse.eNormal, i, stopAnime: true);
				this.nCurrentRollCount[i] = 0;

				OpenTaiko.GetTJA(i)?.tInitLocalStores(i);

				switch (HGaugeMethods.tGetGaugeTypeEnum(i)) {
					default:
					case HGaugeMethods.EGaugeType.NORMAL:
						bIsAlreadyMaxed[i] = bIsAlreadyCleared[i] = false;
						break;
					case HGaugeMethods.EGaugeType.HARD:
					case HGaugeMethods.EGaugeType.EXTREME:
						bIsAlreadyCleared[i] = true;
						bIsAlreadyMaxed[i] = false;
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
					chip.msAutoLastHit = double.NegativeInfinity;
					chip.padStoredHit = EPad.Unknown;
					chip.nRollCount = 0;
					if (NotesManager.IsRollEnd(chip.end))
						chip.end.nBalloon = 0; // Kusudama balloon count
					chip.ResetRollEffect();
				}
				#endregion
			}
			for (int i = 0; i < 5; i++) {
				this.CChartScore[i] = new CBRANCHSCORE();
				this.CSectionScore[i] = new CBRANCHSCORE();

				this.actComboVoice.tReset(i);
				this.isChartEnded[i] = false;
				this.isFinishedPlaying[i] = false;
				this.isDeniedPlaying[i] = false;
				this.stageAbortType[i] = EStageAbort.None;
			}

			this.tBranchReset(-1);

			this.ePhaseID = CStage.EPhase.Common_NORMAL;//初期化すれば、リザルト変遷は止まる。
			this.eFadeOutCompleteWhenReturnValue = EGameplayScreenReturnValue.Continue;

			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i) {
				CTja tja = OpenTaiko.GetTJA(i)!;
				this.ReSetScore(tja.PlayerSideMetadata.nScoreInit[0], tja.PlayerSideMetadata.nScoreDiff, i);
			}
			this.nHand = new int[] { 0, 0, 0, 0, 0 };
		}

		// rewind nCurrentTopChip
		int[] iPrevTopChip = this.nCurrentTopChip.Copy();
		int iPrevTopChipMax = this.nCurrentTopChip.Max();
		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; ++i)
			this.nCurrentTopChip[i] = (this.listChip[i].Count > 0) ? 0 : -1;

		if (!bPlayState && iPrevTopChipMax <= 0)
			return; // no needs to reset

		// reset accumulated chip state
		_AIBattleState = 0;

		NowAIBattleSectionCount = 0;
		NowAIBattleSectionTime = 0;

		FloorManagement.reload();

		for (int i = 0; i < AIBattleSections.Count; i++) {
			AIBattleSections[i].End = AIBattleSection.EndType.None;
			AIBattleSections[i].IsAnimated = false;
		}

		OpenTaiko.ResetCameraStates();

		for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			CTja tja = OpenTaiko.GetTJA(i)!;

			this.eGameType[i] = tja.PlayerSideMetadata.GameType ?? OpenTaiko.ConfigIni.nGameType[i];

			for (CTja.ECourse b = CTja.ECourse.eNormal; b <= CTja.ECourse.eMaster; ++b)
				this.bIsGOGOTIME_Branch[i, (int)b] = false;
			this.bWasGOGOTIME[i] = this.bIsGOGOTIME[i] = false;
			this.bBranchedChart[i] = false;
			this.idxLastBranchSection[i] = 0;
			this.bUseBranch[i] = tja.PlayerSideMetadata.bHasBranch && !tja.PlayerSideMetadata.bHIDDENBRANCH;

			if (tja.PlayerSideMetadata.bHasBranch)
				this.tBranchProcess(CTja.ECourse.eNormal, i);

			this.actPlayInfo.dbBPM[i] = tja.BASEBPM;
			this.UpdateCharaCounter(i);

			this.actPlayInfo.NowMeasure[i] = 0;
			this.JPOSCROLLX[i] = 0;
			this.JPOSCROLLY[i] = 0;

			this.timingZones[i] = CTja.GameDurationToTjaDuration(this.GetTimingZones(i));
			this.bSplitLane[i] = false;
			this.msCurrentBarRollProgress[i] = 0;

			for (int iChip = this.chipCurrentProcessingRollChip[i].Count; iChip-- > 0;) {
				var chip = this.chipCurrentProcessingRollChip[i][iChip];
				this.ProcessRollEnd(i, chip, true);
				chip.bProcessed = false;
			}
			this.chipCurrentProcessingRollChip[i].Clear();
			this.actChara.ReturnDefaultAnime(i);

			for (int iChip = 0; iChip < iPrevTopChip[i]; ++iChip) {
				CChip chip = tja.listChip[iChip];
				if (!NotesManager.IsHittableNote(chip))
					chip.bHit = false;
				chip.obj?.ResetStates();
			}

			for (int iChip = this.chipNowProcessingMultiHitNotes[i].Count; iChip-- > 0;) {
				var chip = this.chipNowProcessingMultiHitNotes[i][iChip];
				if (chip.eNoteState == ENoteState.Wait)
					chip.eNoteState = ENoteState.None;
			}
			this.chipNowProcessingMultiHitNotes[i].Clear();

			this.bCustomDoron[i] = false;
		}

		this.objHandlers.Clear();

		this.actAVI.rVD = null;
		if ((OpenTaiko.TJA.listVD.TryGetValue(1, out CVideoDecoder vd2))) {
			ShowVideo = true;
		} else {
			ShowVideo = false;
		}

		this.actPanel.tLyricsTextureRemove();

		dtLastQueueOperation = DateTime.MinValue;
	}

	// returns the chip index at the target measure of the first player
	public (int idxChip, int msStartGameTime) tPlayPositionChange(int nStartBar) {
		// まず全サウンドオフにする
		OpenTaiko.TJA.tStopAllChips();
		this.actAVI.Stop();
		if (OpenTaiko.TJA == null) return (0, 0); //CDTXがnullの場合はプレイヤーが居ないのでその場で処理終了

		#region [ 再生開始小節の変更 ]
		//nStartBar++;									// +1が必要

		CTja dTX = OpenTaiko.TJA;
		#region [ 処理を開始するチップの特定 ]
		int iTargetChip = dTX.GetListChipIndexOfMeasure(nStartBar);
		#endregion
		#region [ 演奏開始の発声時刻msを取得し、タイマに設定 ]
		int nStartTime = (nStartBar == 0) ? 0
			: ((int)dTX.TjaTimeToGameTime(dTX.listChip[iTargetChip].nSoundTimems) - OpenTaiko.ConfigIni.MusicPreTimeMs);

		int[] iLastChipAtStart = new int[OpenTaiko.MAX_PLAYERS];

		iLastChipAtStart[0] = iTargetChip;
		for (int nPlayer = 0; nPlayer < OpenTaiko.ConfigIni.nPlayerCount; ++nPlayer) {
			CTja tjai = OpenTaiko.GetTJA(nPlayer)!;
			int msStartTjaTime = (int)tjai.GameTimeToTjaTime(nStartTime);
			if (nPlayer != 0) {
				CChip targetDummy = new() { nChannelNo = CChip.nChannelNoLeastPrior, nSoundTimems = msStartTjaTime };
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
				OpenTaiko.stageGameScreen.tValueInitialize(false, false); // rewind
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
				long nSoundTimems = (long)tjai.TjaTimeToGameTime(pChip.nSoundTimems);
				if (nSoundTimems <= nStartTime) {
					if (pChip.nChannelNo == 0x01 && (pChip.nChannelNo >> 4) != 0xB) // wav系チャンネル、且つ、空打ちチップではない
					{
						pChip.bHit = true;
						if (!((nDuration > 0) && (nStartTime <= nSoundTimems + nDuration)))
							continue;

						CTja.CWAV wc;
						bool b = tjai.listWAV.TryGetValue(pChip.nIntValue_InternalNumber, out wc);
						if (!b) continue;

						if ((wc.bIsBGMSound && OpenTaiko.ConfigIni.bBGMPlayVoiceSound) || (!wc.bIsBGMSound)) {
							tjai.tChipPlayback(pChip, SoundManager.PlayTimer.GameTimeToSystemTime((long)tjai.TjaTimeToGameTime(pChip.nSoundTimems)));
							#region [ PAUSEする ]
							int j = wc.nCurrentPlaybackSoundNumber;
							if (wc.rSound[j] != null) {
								wc.rSound[j].Pause();
								wc.rSound[j].tSetPositonToBegin(nStartTime - nSoundTimems);
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
		this.Resume(nStartTime);
		#endregion

		return (iTargetChip, nStartTime);
	}

	public void tPlayAbort() {
		bPreviousPlayWasEndedNormally = false;
		// resume for playing fading out
		SoundManager.PlayTimer.Resume();
		OpenTaiko.Timer.Resume();
		if (this.IsQuittingStage())
			return;
		this.actFO = this.actFOBlack;
		this.actFO.tFadeOutStart();
		base.ePhaseID = (this.IsStageFailed_Fast() || this.IsStageCompleted()) ?
			CStage.EPhase.Game_EndStage_Quit_FadeOut // keep end-of-chart animation
			: CStage.EPhase.Common_FADEOUT;
		this.eFadeOutCompleteWhenReturnValue = EGameplayScreenReturnValue.PerformanceInterrupted;
	}

	/// <summary>
	/// DTXV用の設定をする。(全AUTOなど)
	/// 元の設定のバックアップなどはしないので、あとでConfig.iniを上書き保存しないこと。
	/// </summary>
	protected void tDTXVSettings() {

	}

	protected abstract void tProgressDraw_Chip_Drums(CConfigIni configIni, ref CTja dTX, ref CChip pChip, long nowTime);
	protected abstract void tProgressDraw_ChipBody_Drums(CConfigIni configIni, ref CTja dTX, ref CChip pChip, long nowTime);
	protected abstract void tProgressDraw_Chip_Taiko(CConfigIni configIni, ref CTja dTX, ref CChip pChip, int nPlayer, long nowTime);
	protected abstract void tProgressDraw_Chip_TaikoRoll(CConfigIni configIni, ref CTja dTX, ref CChip pChip, int nPlayer, long nowTime, NotesManager.ENoteType nt, EGameType _gt);

	protected abstract void tProgressDraw_Chip_FillIn(CConfigIni configIni, ref CTja dTX, ref CChip pChip, long nowTime);
	protected abstract void tProgressDraw_Chip_MeasureLine(CConfigIni configIni, ref CTja dTX, ref CChip pChip, int nPlayer, long nowTime);
	protected void tProgressDraw_ChipAnime() {
		for (int i = 0; i < 5; i++) {
			ctChipAnime[i].TickLoopDB();
			ctChipAnimeLag[i].Tick();
		}
	}

	protected bool tProgressDraw_FadeInOut() {
		switch (base.ePhaseID) {
			case CStage.EPhase.Common_FADEIN:
				if (this.actFI.Draw() != 0) {
					base.ePhaseID = CStage.EPhase.Common_NORMAL;
				}
				break;

			case CStage.EPhase.Common_FADEOUT:
			case CStage.EPhase.Game_EndStage_Quit_FadeOut:
			case CStage.EPhase.Game_EndStage_FadeOut:
				if (this.actFO.Draw() != 0) {
					return true;
				}
				break;

		}
		return false;
	}

	protected void tProgressDraw_PlayInfo() {
		if (!OpenTaiko.ConfigIni.bDoNotDisplayPerformanceInfos) {
			this.actPlayInfo.Draw();
		}
	}
	protected bool tProgressDraw_Background() {
		if (this.txBgImage != null) {
			this.txBgImage.t2DDraw(0, 0);
			return true;
		}
		return false;
	}

	protected void tProgressDraw_JudgeString1_NormalPositionSpecifiedCase() {
		if (((EJudgeTextDisplayPosition)OpenTaiko.ConfigIni.JudgeTextDisplayPosition) != EJudgeTextDisplayPosition.BelowCombo)    // 判定ライン上または横
		{
			this.actJudgeString.Draw();
		}
	}

	protected void tProgressDraw_ChartScrollSpeed() {
		this.actScrollSpeed.Draw();
	}
	protected abstract void tConfetti_Start();
	protected abstract void tBackgroundTextureCreate();
	protected void tBackgroundTextureCreate(string DefaultBgFilename, Rectangle bgrect, string bgfilename) {
		try {
			if (!String.IsNullOrEmpty(bgfilename))
				this.txBgImage = OpenTaiko.tTextureCreate(OpenTaiko.SongMount.rChosenScore.FileInfo.FolderAbsolutePath + bgfilename);
			else
				this.txBgImage = OpenTaiko.tTextureCreate(CSkin.Path(DefaultBgFilename));
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
		// Online VS: a REMOTE player's auto-hit judge is sampled to match THEIR broadcast good/ok/bad rates (per
		// mille), so their lane shows the chart being hit with a realistic judge mix — AI-battle-style, driven by
		// the wire instead of an AI level. Only applies during an online play round (IsRemoteSpot is else false).
		var _onl = LuaNetworking.Active;
		if (_onl != null && _onl.IsRemoteSpot(player)) {
			if (reroll) nDice = OpenTaiko.Random.Next(1000);
			int _bad = _onl.GetSpotBadOdds(player), _good = _onl.GetSpotGoodOdds(player);
			if (nDice < _bad) return ENoteJudge.Poor;
			else if (nDice - _bad < _good) return ENoteJudge.Good;
		}
		return judgement;
	}

	// Online VS: freeze a player spot whose remote player disconnected mid-play — stops its auto-hits so it stops
	// updating, while staying flagged auto (still excluded from saving). Called each frame by OnlinePlaySync.
	public void OnlineFreezeSpot(int spot) { if (spot >= 0 && spot < this.isDeniedPlaying.Length) this.isDeniedPlaying[spot] = true; }

	public bool CanAutoplayHitMine(int player, bool reroll) {
		if (this.isDeniedPlaying[player] || this.IsStageFailed_Fast())
			return false;
		int AILevel = OpenTaiko.ConfigIni.nAILevel;
		if (OpenTaiko.ConfigIni.bAIBattleMode && player == 1) {
			if (reroll)
				nDice = OpenTaiko.Random.Next(1000);

			if (nDice < OpenTaiko.ConfigIni.apAIPerformances[AILevel - 1].nMineHitOdds)
				return true;
		}
		return false;
	}

	public void ReSetScore(int scoreInit, int scoreDiff, int iPlayer) {
		//一打目の処理落ちがひどいので、あらかじめここで点数の計算をしておく。
		// -1だった場合、その前を引き継ぐ。
		int nInit = scoreInit != -1 ? scoreInit : this.nScore[iPlayer, 0];
		int nDiff = scoreDiff != -1 ? scoreDiff : this.nScore[iPlayer, 1] - this.nScore[iPlayer, 0];
		int nAddScore = 0;
		int[] nScale = { 0, 1, 2, 4, 8 };

		if (this.scoreMode[iPlayer] == 1) {
			for (int i = 0; i < 11; i++) {
				this.nScore[iPlayer, i] = (int)(nInit + (nDiff * (i)));
			}
		} else if (this.scoreMode[iPlayer] == 2) {
			for (int i = 0; i < 5; i++) {
				this.nScore[iPlayer, i] = (int)(nInit + (nDiff * nScale[i]));

				this.nScore[iPlayer, i] = (int)(this.nScore[iPlayer, i] / 10.0);
				this.nScore[iPlayer, i] = this.nScore[iPlayer, i] * 10;

			}
		}
	}


	#region [EXTENDED COMMANDS]
	private Dictionary<string, (CChip chip, CCounter counter, Action<float> setter)> objHandlers;

	public bool[] bCustomDoron;
	private bool bConfigUpdated = false;
	#endregion
}
