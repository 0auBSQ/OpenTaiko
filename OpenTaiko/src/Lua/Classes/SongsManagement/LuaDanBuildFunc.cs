using FDK;
using System.Drawing;

namespace OpenTaiko {
	/// <summary>
	/// Exposes a <c>DANBUILDER</c> global to Lua for assembling a dynamic Dan chart
	/// from arbitrary song nodes + difficulty choices + exam conditions.
	/// All chart data is built in memory — no temp files are written.
	/// </summary>
	internal class LuaDanBuildFunc {

		// ── Stored songs ──────────────────────────────────────────────────────

		private readonly List<(LuaSongNode node, int diff)> _songs = [];

		// ── Exams ─────────────────────────────────────────────────────────────

		// Global exams (slot index 0-based, up to cMaxExam)
		private readonly (string type, int red, int gold, bool lessThan)?[] _globalExams
			= new (string, int, int, bool)?[CExamInfo.cMaxExam];

		// Per-song exams  [songIndex][slot]
		private readonly List<(string type, int red, int gold, bool lessThan)?[]> _perSongExams = [];

		// ── Metadata ──────────────────────────────────────────────────────────

		private string _title = "Dynamic Dan";
		private string _subtitle = "";
		private int _danTick = 2;
		private Color _danTickColor = Color.White;

		// ── Lua-visible API ───────────────────────────────────────────────────

		public int SongCount => _songs.Count;

		/// <summary>Returns the LuaSongNode at 1-based index <paramref name="i"/>, or null.</summary>
		public LuaSongNode? GetSong(int i) {
			int idx = i - 1;
			return (idx >= 0 && idx < _songs.Count) ? _songs[idx].node : null;
		}

		/// <summary>Returns the difficulty index at 1-based song index <paramref name="i"/>, or -1.</summary>
		public int GetSongDiff(int i) {
			int idx = i - 1;
			return (idx >= 0 && idx < _songs.Count) ? _songs[idx].diff : -1;
		}

		/// <summary>Appends a song to the Dan. <paramref name="diff"/> is 0-based (0=Easy … 4=Edit).</summary>
		public void AddSong(LuaSongNode node, int diff) {
			_songs.Add((node, diff));
			_perSongExams.Add(new (string, int, int, bool)?[CExamInfo.cMaxExam]);
		}

		public void SetTitle(string title) => _title = title;
		public void SetSubtitle(string subtitle) => _subtitle = subtitle;
		public void SetDanTick(int tick) => _danTick = tick;
		/// <summary>Sets the Dan plate tick color (RGB 0–255 each).</summary>
		public void SetDanTickColor(int r, int g, int b) =>
			_danTickColor = Color.FromArgb(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));

		/// <summary>
		/// Sets a global exam (applies to the whole Dan).
		/// <paramref name="slot"/> is 1-based (1–7).
		/// </summary>
		public void SetGlobalExam(int slot, string type, int red, int gold, bool lessThan) {
			int idx = slot - 1;
			if (idx < 0 || idx >= CExamInfo.cMaxExam) return;
			_globalExams[idx] = (type, red, gold, lessThan);
		}

		/// <summary>
		/// Sets a per-song exam.
		/// <paramref name="songIndex"/> and <paramref name="slot"/> are both 1-based.
		/// </summary>
		public void SetPerSongExam(int songIndex, int slot, string type, int red, int gold, bool lessThan) {
			int sIdx = songIndex - 1;
			int eIdx = slot - 1;
			if (sIdx < 0 || sIdx >= _perSongExams.Count) return;
			if (eIdx < 0 || eIdx >= CExamInfo.cMaxExam) return;
			_perSongExams[sIdx][eIdx] = (type, red, gold, lessThan);
		}

		/// <summary>Clears all songs and exams, resets metadata.</summary>
		public void Clear() {
			_songs.Clear();
			_perSongExams.Clear();
			Array.Clear(_globalExams);
			_title = "Dynamic Dan";
			_subtitle = "";
			_danTick = 2;
			_danTickColor = Color.White;
		}

		/// <summary>
		/// Builds the Dan CTja in memory, creates a fake CSongListNode, and mounts them
		/// so the song-loading stage will pick them up.
		/// Returns true on success.
		/// </summary>
		public bool Mount() {
			if (_songs.Count == 0) return false;
			try {
				CTja outputCtja = BuildDanCtja();
				CSongListNode node = BuildDanNode(outputCtja);

				// Hand the pre-built CTja to the loading stage.
				OpenTaiko.DanBuilderPrebuiltTja = outputCtja;
				OpenTaiko.SongMount.rChoosenSong = node;
				OpenTaiko.SongMount.rChosenScore = node.score[(int)Difficulty.Dan];
				OpenTaiko.SongMount.nChoosenSongDifficulty[0] = (int)Difficulty.Dan;
				OpenTaiko.SongMount.strChosenSongGenre = "Dan";
				return true;
			} catch (Exception ex) {
				LogNotification.PopError($"[DanBuilder] Mount failed: {ex.Message}");
				return false;
			}
		}

		// ─────────────────────────────────────────────────────────────────────
		// Core builders
		// ─────────────────────────────────────────────────────────────────────

		/// <summary>
		/// Constructs a <see cref="CTja"/> whose chip lists, WAV map, DanSongs list,
		/// and exam arrays are assembled entirely in memory from the source songs.
		/// </summary>
		private CTja BuildDanCtja() {
			var output = new CTja();
			output.Activate();

			// Metadata
			output.TITLE.SetString("default", _title);
			output.SUBTITLE.SetString("default", _subtitle);
			output.DANTICK = _danTick;
			output.DANTICKCOLOR = _danTickColor;
			output.strFileName = "dynamic_dan.tja";
			output.strFullPath = CTja.DanBuilderSentinelPath;
			output.strFolderPath = "";           // all WAV strファイル名 will be absolute
			output.bLoadChart = true;
			output.n参照中の難易度 = (int)Difficulty.Dan;
			output.b譜面が存在する[(int)Difficulty.Dan] = true;

			// Global exams
			for (int i = 0; i < CExamInfo.cMaxExam; i++) {
				if (_globalExams[i] is { } ge)
					output.Dan_C[i] = MakeDanC(ge.type, ge.red, ge.gold, ge.lessThan);
			}

			// Resize per-song stat arrays to the correct song count upfront
			int n = _songs.Count;
			Array.Resize(ref output.nDan_NotesCount, n);
			Array.Resize(ref output.nDan_AdLibCount, n);
			Array.Resize(ref output.nDan_MineCount, n);
			Array.Resize(ref output.nDan_BalloonHitCount, n);
			Array.Resize(ref output.nDan_BarRollCount, n);
			Array.Resize(ref output.bHasBranchDan, n);
			Array.Resize(ref output.pDan_LastChip, n);

			// Dan timing mirrors the real TJA parser:
			//   nextsongTime  = time at which the 0x9B (NEXTSONG) chip fires
			//   accum         = time at which the BGM chip (and notes) start
			//                 = nextsongTime + msDanNextSongDelay + MusicPreTimeMs
			// For the first song nextsongTime is 0, so the gate fires at 0 and the
			// music starts after the full pre-delay.
			double nextsongTime = 0.0;
			double accum = nextsongTime + CTja.msDanNextSongDelay + OpenTaiko.ConfigIni.MusicPreTimeMs;

			// listBPM[0..2] must always be present as per-branch initial BPM sentinels
			// (GetNowPBPMPoint starts with last_match = (int)branch which indexes into [0..2]).
			// We seed them from the first valid song; BPM-change entries follow from index 3 onward.
			bool bpmSeeded = false;
			bool baseBpmSet = false;

			for (int si = 0; si < _songs.Count; si++) {
				double songBoundaryTime = nextsongTime;  // saved before any advancement this iteration
				var (luaNode, diff) = _songs[si];
				var internalNode = luaNode.InternalNode;
				if (internalNode == null) continue;

				CScore? score = internalNode.score[diff];
				if (score == null) continue;

				string tjaPath = score.ファイル情報.ファイルの絶対パス ?? "";
				if (!File.Exists(tjaPath)) continue;

				// ── Parse source song ──────────────────────────────────────────
				var src = new CTja(tjaPath, diff, 0, loadChart: true);

				// BGM chip in source is the channel-0x01 chip that plays the audio.
				CChip? srcBgmChip = src.listChip.FirstOrDefault(c => c.nChannelNo == 0x01);
				double srcBgmTimeDb = srcBgmChip?.db発声時刻ms ?? 0.0;
				// Initial BPM of this source chart — used for the animation-BPM chip emitted
				// at the song boundary so character/puchichara animations run at the right speed.
				double srcInitialBpm = src.listBPM.Count > 0 ? src.listBPM[0].dbBPM値 : 120.0;

				// Anchor the shift on the first note in the source chart (not the BGM chip).
				// This ensures the first note always lands at `accum` in the Dan chart,
				// matching the real TJA Dan parser where notes start exactly
				// msDanNextSongDelay + MusicPreTimeMs after each #NEXTSONG marker.
				//
				// Why not anchor on srcBgmTimeDb?
				//   • Negative OFFSET (common): BGM chip is before notes; using it would push
				//     notes past `accum` by msOFFSET_Abs ms → correct for audio sync but the
				//     inter-song gap becomes inconsistent.
				//   • Positive OFFSET: BGM chip is AFTER notes; using
				//     it would place the earliest notes before `accum` (into the transition gap)
				//     causing them to appear on screen too early and out of sync with audio.
				//
				// With firstNoteTimeDb as anchor:
				//   • The first note always hits the screen at `accum`.
				//   • For negative OFFSET: the BGM chip falls into the transition gap
				//     (accum - msOFFSET_Abs), giving the player the song's intro music.
				//   • For positive OFFSET: the BGM chip fires after `accum`, so the first
				//     notes play in silence — the same behaviour as the standalone chart.
				double firstNoteTimeDb = (src.listNoteChip.Count > 0)
					? src.listNoteChip.Min(c => c.db発声時刻ms)
					: srcBgmTimeDb;

				double offsetDb = accum - firstNoteTimeDb;
				int offsetMs = (int)Math.Round(offsetDb);

				// ── Populate listBPM ───────────────────────────────────────────────
				// Build a source-index → output-index map for remapping BPM chips later.
				var bpmIdxMap = new Dictionary<int, int>();

				if (!bpmSeeded) {
					// Seed output.listBPM[0..2]: one sentinel per branch with this song's initial BPM.
					for (int ib = 0; ib < 3; ib++) {
						double branchBpm = (ib < src.listBPM.Count) ? src.listBPM[ib].dbBPM値 : 120.0;
						output.listBPM.Add(new CTja.CBPM {
							n内部番号 = ib,
							n表記上の番号 = ib,
							dbBPM値 = branchBpm,
							bpm_change_time = 0.0,
							bpm_change_bmscroll_time = 0.0,
							bpm_change_course = (CTja.ECourse)ib,
						});
						bpmIdxMap[ib] = ib;
					}
					bpmSeeded = true;
				} else {
					// Initial indices 0-2 already exist in output; just alias them.
					bpmIdxMap[0] = 0;
					bpmIdxMap[1] = 1;
					bpmIdxMap[2] = 2;
				}

				// Copy actual BPM-change entries (index 3+) with the time offset applied.
				// IMPORTANT: bpm_change_bmscroll_time is in 16th-beat units (not ms) for HBSCROLL,
				// so adding offsetDb (ms) to it would corrupt HBSCROLL note positions. Instead we
				// derive it from bpm_change_time (which IS in ms) so the Dan always uses normal-
				// scroll positioning regardless of the source chart's scroll mode.
				for (int bi = 3; bi < src.listBPM.Count; bi++) {
					var srcBpm = src.listBPM[bi];
					int newIdx = output.listBPM.Count;
					output.listBPM.Add(new CTja.CBPM {
						n内部番号 = newIdx,
						n表記上の番号 = srcBpm.n表記上の番号,
						dbBPM値 = srcBpm.dbBPM値,
						bpm_change_time = srcBpm.bpm_change_time + offsetDb,
						bpm_change_bmscroll_time = srcBpm.bpm_change_time + offsetDb,
						bpm_change_course = srcBpm.bpm_change_course,
					});
					bpmIdxMap[bi] = newIdx;
				}

				// ── Populate listJPOSSCROLL ───────────────────────────────────────────────
				// JPOSSCROLL chips (0xE2) index into listJPOSSCROLL by n整数値_内部番号.
				// Copy every entry from the source into the output and remap the index.
				var jposIdxMap = new Dictionary<int, int>();
				for (int ji = 0; ji < src.listJPOSSCROLL.Count; ji++) {
					var srcJpos = src.listJPOSSCROLL[ji];
					int newIdx = output.listJPOSSCROLL.Count;
					output.listJPOSSCROLL.Add(new CTja.CJPOSSCROLL {
						n内部番号 = newIdx,
						n表記上の番号 = srcJpos.n表記上の番号,
						msMoveDt = srcJpos.msMoveDt,
						pxOrigX = srcJpos.pxOrigX,
						pxOrigY = srcJpos.pxOrigY,
						pxMoveDx = srcJpos.pxMoveDx,
						pxMoveDy = srcJpos.pxMoveDy,
						// chip back-ref is patched after chipMap is built
					});
					jposIdxMap[ji] = newIdx;
				}

				// ── BASEBPM: grab from the first song so character animations start correctly ──
				if (!baseBpmSet) {
					output.BASEBPM = src.BASEBPM > 0 ? src.BASEBPM : (src.listBPM.Count > 0 ? src.listBPM[0].dbBPM値 : 120.0);
					baseBpmSet = true;
				}

				// ── 0x9B chip: Dan song-start marker used by the engine to trigger ──
				// song transitions and note-count partitioning for scoring.
				// Placed at nextsongTime (BEFORE the 6200ms + MusicPreTimeMs gap),
				// exactly mirroring how the real TJA parser emits it at the current
				// cursor time before advancing by msDanNextSongDelay + MusicPreTimeMs.
				var nextsongChip = new CChip();
				nextsongChip.t初期化();
				nextsongChip.nChannelNo = 0x9B;
				nextsongChip.n整数値_内部番号 = si;
				nextsongChip.n発声時刻ms = (int)Math.Round(nextsongTime);
				nextsongChip.db発声時刻ms = nextsongTime;
				nextsongChip.nBranch = CTja.ECourse.eNormal;
				nextsongChip.start = nextsongChip;
				nextsongChip.end = nextsongChip;
				output.listChip.Add(nextsongChip);
				for (int ib = 0; ib < 3; ib++)
					output.listChip_Branch[ib].Add(nextsongChip);

				// ── 0x9C animation-BPM chip for this song's transition ─────────────
				// Character and puchichara animations are driven by the 0x9C chip's BPM.
				// Without a chip at each song boundary the animation system keeps using the
				// previous song's BPM throughout the transition gap, making the dons/kats
				// appear to walk at the wrong speed. We emit one 1 ms after the 0x9B gate
				// so it fires during the transition window before any notes arrive.
				// (Not needed for the very first song — the initial listBPM[0] value covers it.)
				if (si > 0) {
					int animBpmIdx = output.listBPM.Count;
					output.listBPM.Add(new CTja.CBPM {
						n内部番号 = animBpmIdx,
						n表記上の番号 = animBpmIdx,
						dbBPM値 = srcInitialBpm,
						bpm_change_time = songBoundaryTime + 1.0,
						bpm_change_bmscroll_time = songBoundaryTime + 1.0,
						bpm_change_course = CTja.ECourse.eNormal,
					});
					var animBpmChip = new CChip();
					animBpmChip.t初期化();
					animBpmChip.nChannelNo = 0x9C;
					animBpmChip.n整数値_内部番号 = animBpmIdx;
					animBpmChip.n発声時刻ms = (int)Math.Round(songBoundaryTime + 1.0);
					animBpmChip.db発声時刻ms = songBoundaryTime + 1.0;
					animBpmChip.start = animBpmChip;
					animBpmChip.end = animBpmChip;
					output.listChip.Add(animBpmChip);
				}

				// Absolute path of the source audio file.
				string absoluteBgmPath = "";
				if (srcBgmChip != null &&
					src.listWAV.TryGetValue(srcBgmChip.n整数値_内部番号, out var srcBgmWav) &&
					!string.IsNullOrEmpty(srcBgmWav.strファイル名)) {
					absoluteBgmPath = Path.Combine(src.strFolderPath, srcBgmWav.strファイル名);
				}

				// ── Create output BGM chip ─────────────────────────────────────
				int wavId = si + 1;
				var bgmChip = new CChip();
				bgmChip.t初期化();
				bgmChip.nChannelNo = 0x01;
				bgmChip.n整数値 = wavId;
				bgmChip.n整数値_内部番号 = wavId;
				// BGM chip fires at srcBgmTimeDb shifted into Dan time.
				// For negative OFFSET: this is before `accum` (intro music during the gap).
				// For positive OFFSET: this is after `accum` (silence until BGM kicks in).
				double bgmChipTimeDb = srcBgmTimeDb + offsetDb;

				// If the shifted BGM chip would fire before nextsongTime (the song-boundary
				// marker), the audio would play during the PREVIOUS song's playback.  This
				// happens when a chart has a large gap between the BGM and the first note
				// (many empty bars or a very large OFFSET value).
				//
				// Fix: clamp the chip to nextsongTime so the audio never starts before the
				// transition, then tell the audio system to begin playback at an initial seek
				// position so the first note still aligns with the correct position in the
				// audio file.  The seek position is:
				//
				//   nInitialSeekMs = (firstNoteTimeDb − srcBgmTimeDb) − (accum − nextsongTime)
				//
				// which ensures that at Dan time `accum` the audio is exactly
				// (firstNoteTimeDb − srcBgmTimeDb) ms from the start of the file — the same
				// offset a standalone play-through of the chart would have.
				long bgmInitialSeekMs = 0;
				if (bgmChipTimeDb < nextsongTime) {
					bgmInitialSeekMs = (long)Math.Round((firstNoteTimeDb - srcBgmTimeDb) - (accum - nextsongTime));
					bgmInitialSeekMs = Math.Max(0L, bgmInitialSeekMs);
					bgmChipTimeDb = nextsongTime;
				}
				bgmChip.n発声時刻ms = (int)Math.Round(bgmChipTimeDb);
				bgmChip.db発声時刻ms = bgmChipTimeDb;
				bgmChip.start = bgmChip;
				bgmChip.end = bgmChip;

				// ── Create CWAV for this song's BGM ───────────────────────────
				var cwav = new CTja.CWAV {
					n内部番号 = wavId,
					n表記上の番号 = wavId,
					bIsBGMSound = true,
					strファイル名 = absoluteBgmPath,
					strコメント文 = $"DanBuilder BGM [{si + 1}]",
					PlayChip = bgmChip,
					SongVol = CSound.DefaultSongVol,
					nInitialSeekMs = bgmInitialSeekMs,
				};
				cwav.listこのWAVを使用するチャンネル番号の集合.Add(0x01);
				if (!string.IsNullOrEmpty(absoluteBgmPath))
					cwav.SongLoudnessMetadata = LoudnessMetadataScanner.LoadForAudioPath(absoluteBgmPath);

				output.listWAV.Add(wavId, cwav);
				output.listChip.Add(bgmChip);
				for (int ib = 0; ib < 3; ib++)
					output.listChip_Branch[ib].Add(bgmChip);

				// ── Clone note chips from source ───────────────────────────────
				// We build a source→clone map so that start/end links can be re-established.
				var chipMap = new Dictionary<CChip, CChip>();  // CChip has no Equals override → reference equality by default
				var clonedInOrder = new List<CChip>(src.listChip.Count);

				foreach (var srcChip in src.listChip) {
					if (srcChip.nChannelNo == 0x01) continue; // BGM already handled above
					if (srcChip.nChannelNo == 0xFF) continue; // end-of-chart sentinel — one is appended after the last song

					var newChip = (CChip)srcChip.Clone();
					newChip.n発声時刻ms = srcChip.n発声時刻ms + offsetMs; // setter updates db too
					newChip.db発声時刻ms = srcChip.db発声時刻ms + offsetDb;  // restore double precision

					// Remap BPM-referencing chips (0x08 = extended BPM, 0x9C = animation BPM)
					// so they point to the correct entries in the output listBPM table.
					if ((newChip.nChannelNo == 0x08 || newChip.nChannelNo == 0x9C) &&
						bpmIdxMap.TryGetValue(newChip.n整数値_内部番号, out int remappedBpmIdx))
						newChip.n整数値_内部番号 = remappedBpmIdx;

					// Remap JPOSSCROLL chips (0xE2) so they point to the correct entry in
					// output.listJPOSSCROLL. Without this the engine crashes with an out-of-range
					// exception when the chip fires.
					if (newChip.nChannelNo == 0xE2 &&
						jposIdxMap.TryGetValue(newChip.n整数値_内部番号, out int remappedJposIdx))
						newChip.n整数値_内部番号 = remappedJposIdx;

					chipMap[srcChip] = newChip;
					clonedInOrder.Add(newChip);
					output.listChip.Add(newChip);
					// Bar-line chips (0x50 = auto bar line, 0xE4 = explicit #BARLINE) must
					// also be registered in listBarLineChip, which is what the renderer reads.
					// The output's listBarLineChip is never built automatically, so without
					// this step no bar lines are visible during Dan play.
					if (newChip.nChannelNo == 0x50 || newChip.nChannelNo == 0xE4)
						output.listBarLineChip.Add(newChip);
				}

				// Patch CJPOSSCROLL.chip back-refs to point at the cloned 0xE2 chips.
				foreach (var (srcJposIdx, outJposIdx) in jposIdxMap) {
					var srcJpos = src.listJPOSSCROLL[srcJposIdx];
					if (srcJpos.chip != null && chipMap.TryGetValue(srcJpos.chip, out var clonedJposChip))
						output.listJPOSSCROLL[outJposIdx].chip = clonedJposChip;
				}

				// Re-link start/end within this song segment.
				// For single notes:  start==self, end==self  → maps to cloned self
				// For roll heads:    start==self, end==rollEnd → maps to cloned rollEnd
				// For roll ends:     start==rollHead, end==self → maps to cloned rollHead / self
				foreach (var (srcChip, newChip) in chipMap) {
					newChip.start = chipMap.TryGetValue(srcChip.start, out var ms) ? ms : newChip;
					newChip.end = chipMap.TryGetValue(srcChip.end, out var me) ? me : newChip;
				}

				// Populate branch lists using the same chipMap.
				// For non-branching songs all three branches contain the same chips.
				for (int ib = 0; ib < 3; ib++) {
					foreach (var srcChip in src.listChip_Branch[ib]) {
						if (srcChip.nChannelNo == 0x01) continue;
						if (chipMap.TryGetValue(srcChip, out var mapped))
							output.listChip_Branch[ib].Add(mapped);
					}
				}

				// ── Populate listNoteChip ──────────────────────────────────────────
				// listNoteChip is the authoritative list used by the engine for:
				//   • UpdateScrolledChipPosition  (sets nHorizontalChipDistance so notes scroll)
				//   • Miss detection in the update loop
				//   • Roll auto-processing
				//   • Per-song note counting for Dan exam tracking
				// Every chip that was in src.listNoteChip maps to a cloned chip; we
				// re-index n整数値_内部番号 to be the 0-based position in the OUTPUT list.
				//
				// msShowOffset controls when a note becomes visible on screen:
				//   bShowSudden = (msTjaNowTime >= chip.n発声時刻ms - chip.msShowOffset)
				// Setting msShowOffset = chip.n発声時刻ms - nextsongTime gives:
				//   bShowSudden = (msTjaNowTime >= nextsongTime)
				// so notes from song si are hidden until its 0x9B gate fires.
				// For roll ends the engine uses the roll HEAD's msShowOffset, so
				// setting it on the head chip is sufficient to gate the whole roll.
				foreach (var srcNoteChip in src.listNoteChip) {
					if (chipMap.TryGetValue(srcNoteChip, out var clonedNoteChip)) {
						clonedNoteChip.n整数値_内部番号 = output.listNoteChip.Count;
						clonedNoteChip.msShowOffset = clonedNoteChip.n発声時刻ms - nextsongTime;
						clonedNoteChip.msMoveOffset = double.PositiveInfinity; // always move at natural speed
						clonedNoteChip.IsSuddenHideRoll = false;                  // don't hide roll bodies
						output.listNoteChip.Add(clonedNoteChip);
					}
				}

				// ── Count notes and find the last hittable chip ────────────────
				// Forward pass: accumulate per-song stats
				foreach (var chip in clonedInOrder) {
					if (NotesManager.IsMissableNote(chip)) {
						output.nDan_NotesCount[si]++;
					} else if (NotesManager.IsADLIB(chip)) {
						output.nDan_AdLibCount[si]++;
					} else if (NotesManager.IsMine(chip)) {
						output.nDan_MineCount[si]++;
					} else if (NotesManager.IsGenericBalloon(chip)) {
						output.nDan_BalloonHitCount[si] += chip.nBalloon;
						if (NotesManager.IsFuzeRoll(chip))
							output.nDan_MineCount[si]++;
					} else if (NotesManager.IsGenericRoll(chip) && !NotesManager.IsRollEnd(chip)) {
						output.nDan_BarRollCount[si]++;
					}
				}

				// Backward pass: find last hittable chip (mirrors FindLastHittableOrChip)
				CChip? lastHittable = null;
				for (int ci = clonedInOrder.Count - 1; ci >= 0; ci--) {
					var chip = clonedInOrder[ci];
					var chipStart = chip.start; // for roll ends this gives the roll head
					if (NotesManager.IsHittableNote(chipStart)) {
						lastHittable = chipStart.end; // end time of the note/roll
						break;
					}
				}
				output.pDan_LastChip[si] = lastHittable ?? bgmChip;
				output.bHasBranchDan[si] = false;

				// ── Build DanSongs entry ───────────────────────────────────────
				var danSong = new CTja.DanSongs {
					Title = luaNode.Title ?? "",
					SubTitle = luaNode.Subtitle ?? "",
					Genre = luaNode.Genre ?? "",
					FileName = absoluteBgmPath,
					Level = internalNode.nLevel[diff],
					Difficulty = diff,
					ScoreInit = src.nScoreInit[0, diff],
					ScoreDiff = src.nScoreDiff[diff],
					bTitleShow = false,
					Wave = cwav,
				};

				// Per-song exams
				if (si < _perSongExams.Count) {
					for (int ei = 0; ei < CExamInfo.cMaxExam; ei++) {
						if (_perSongExams[si][ei] is { } pe)
							danSong.Dan_C[ei] = MakeDanC(pe.type, pe.red, pe.gold, pe.lessThan);
					}
				}

				output.List_DanSongs.Add(danSong);

				// ── Advance time accumulator ───────────────────────────────────
				// The 0x9B for song N+1 fires right after the last chip of song N.
				// Because we anchored the shift on firstNoteTimeDb, the last chip of
				// song N in the Dan is at (srcLastTimeDb + offsetDb), which equals
				// accum + (srcLastTimeDb - firstNoteTimeDb).
				double srcLastTimeDb = src.listChip.Count > 0
					? src.listChip.Max(c => c.db発声時刻ms)
					: srcBgmTimeDb;
				double srcDuration = srcLastTimeDb - firstNoteTimeDb;
				nextsongTime = accum + srcDuration;
				accum = nextsongTime + CTja.msDanNextSongDelay + OpenTaiko.ConfigIni.MusicPreTimeMs;

				// ── Inter-song gimmick resets ─────────────────────────────────────
				// Placed 1 ms after nextsongTime (the 0x9B boundary chip), so they
				// fire during the ~6 s transition gap before the next song's notes begin.
				// This prevents gimmick state from one song bleeding into the next.
				// Not emitted after the final song (no next song to protect).
				if (si < _songs.Count - 1) {
					double resetTime = nextsongTime + 1.0;
					int resetTimeMs = (int)Math.Round(resetTime);

					CChip MakeReset(int channel) {
						var c = new CChip();
						c.t初期化();
						c.nChannelNo = channel;
						c.n発声時刻ms = resetTimeMs;
						c.db発声時刻ms = resetTime;
						c.start = c; c.end = c;
						return c;
					}

					// 0x9F  GOGOEND — ensure Gogo Time is off for the next song
					var gogoEnd = MakeReset(0x9F);
					gogoEnd.n整数値 = 1;
					output.listChip.Add(gogoEnd);

					// 0xE0  BARLINEON (n整数値 = 2) — restore bar lines if a song hid them
					var barlineOn = MakeReset(0xE0);
					barlineOn.n整数値 = 2;
					output.listChip.Add(barlineOn);

					// 0x09  NMSCROLL — reset scroll mode to Normal.
					// If any song used HBSCROLL (or BMSCROLL), the scroll mode carries over
					// into the next song and corrupts note positioning. Channel 0x09 is the
					// #NMSCROLL directive which resets eScrollMode to Normal.
					var nmscrollReset = MakeReset(0x09);
					output.listChip.Add(nmscrollReset);

					// 0x9D  SCROLL = 1.0 — restore normal scroll speed
					var scrollReset = MakeReset(0x9D);
					scrollReset.dbSCROLL = 1.0;
					scrollReset.dbSCROLL_Y = 0.0;
					output.listChip.Add(scrollReset);

					// 0xF2  DIRECTION = 0 — restore normal (left-to-right) scroll direction
					var dirReset = MakeReset(0xF2);
					dirReset.nScrollDirection = 0;
					output.listChip.Add(dirReset);

					// 0xE2  JPOSSCROLL — snap judgment frame back to default centre position
					int jposResetIdx = output.listJPOSSCROLL.Count;
					output.listJPOSSCROLL.Add(new CTja.CJPOSSCROLL {
						n内部番号 = jposResetIdx,
						n表記上の番号 = jposResetIdx,
						pxOrigX = 0, pxOrigY = 0,
						pxMoveDx = 0, pxMoveDy = 0,
						msMoveDt = 0,
					});
					var jposReset = MakeReset(0xE2);
					jposReset.n整数値 = jposResetIdx;
					jposReset.n整数値_内部番号 = jposResetIdx;
					output.listJPOSSCROLL[jposResetIdx].chip = jposReset;
					output.listChip.Add(jposReset);
				}

				src.DeActivate();
			}

			// nノーツ数_Branch is used by CAct演奏ゲージ共通.Init() to compute the
			// gauge gain/loss per note. InsertNoteAtDefCursor normally increments it
			// during TJA parsing, but our builder never calls that. Set all three
			// branch slots to the total note count across all Dan songs so the gauge
			// initialises correctly instead of falling back to +1.2%/hit, -2.4%/miss.
			int totalDanNotes = output.nDan_NotesCount.Sum();
			output.nノーツ数_Branch[0] = totalDanNotes;
			output.nノーツ数_Branch[1] = totalDanNotes;
			output.nノーツ数_Branch[2] = totalDanNotes;
			// nノーツ数[3] is the total missable-note count for the whole Dan chart.
			// GetTotalExamScore() reads this as nNotesMax for global exams; if it stays 0,
			// amountRemainMax == 0 at game start → doFinalJudge fires immediately with
			// gauge=0% → Failure before the player hits a single note.
			output.nノーツ数[3] = totalDanNotes;

			// Add the two end-of-chart sentinel chips that the engine uses to know the Dan is done.
			//   0xFF with n整数値=0    → sets isChartEnded[player] = true, continues chip loop
			//   0xFF with n整数値=0xFF → sets isChartEnded[player] = true, then exits the chip loop (result transition)
			// Each source TJA normally has its own pair; we stripped those (they would fire after song 0
			// and prematurely end the entire Dan). Now we add a single pair after the last song.
			int lastDanChipMs = (output.pDan_LastChip.Length > 0 && output.pDan_LastChip[^1] != null)
				? output.pDan_LastChip[^1]!.n発声時刻ms
				: (int)Math.Round(nextsongTime);

			var chartEndChip = new CChip();
			chartEndChip.t初期化();
			chartEndChip.nChannelNo = 0xFF;
			chartEndChip.n整数値 = 0;
			chartEndChip.n発声時刻ms = lastDanChipMs + 2000;
			chartEndChip.db発声時刻ms = lastDanChipMs + 2000.0;
			chartEndChip.start = chartEndChip;
			chartEndChip.end = chartEndChip;
			output.listChip.Add(chartEndChip);

			var gameFadeOutChip = new CChip();
			gameFadeOutChip.t初期化();
			gameFadeOutChip.nChannelNo = 0xFF;
			gameFadeOutChip.n整数値 = 0xFF;
			gameFadeOutChip.n発声時刻ms = lastDanChipMs + 3000;
			gameFadeOutChip.db発声時刻ms = lastDanChipMs + 3000.0;
			gameFadeOutChip.start = gameFadeOutChip;
			gameFadeOutChip.end = gameFadeOutChip;
			output.listChip.Add(gameFadeOutChip);

			// Sort the combined chip lists (required by the engine).
			output.listChip.Sort();
			for (int ib = 0; ib < 3; ib++)
				output.listChip_Branch[ib].Sort();

			return output;
		}

		/// <summary>
		/// Builds a <see cref="CSongListNode"/> that points to the pre-built CTja
		/// via the sentinel file path, so the song-loading stage can recognise it.
		/// </summary>
		private CSongListNode BuildDanNode(CTja outputCtja) {
			var node = new CSongListNode();
			node.nodeType = CSongListNode.ENodeType.SCORE;
			node.ldTitle.SetString("default", _title);
			node.ldSubtitle.SetString("default", _subtitle);
			node.songGenre = "Dan";
			node.nDanTick = _danTick;
			node.cDanTickColor = _danTickColor;
			node.Dan_C = outputCtja.Dan_C;
			node.DanSongs = outputCtja.List_DanSongs;

			// Fake CScore — the sentinel path tells the loading stage to use the
			// pre-built CTja stored in OpenTaiko.DanBuilderPrebuiltTja.
			var fakeScore = new CScore();
			fakeScore.ファイル情報 = new CScore.STファイル情報(
				CTja.DanBuilderSentinelPath,
				"",
				DateTime.Now,
				0L
			);
			fakeScore.譜面情報.タイトル = _title;
			fakeScore.譜面情報.strサブタイトル = _subtitle;
			fakeScore.譜面情報.nレベル = new int[(int)Difficulty.Total] { -1, -1, -1, -1, -1, -1, 10 };
			fakeScore.譜面情報.cDanTickColor = _danTickColor;

			node.score[(int)Difficulty.Dan] = fakeScore;
			node.nLevel[(int)Difficulty.Dan] = 10;

			return node;
		}

		// ─────────────────────────────────────────────────────────────────────
		// Helpers
		// ─────────────────────────────────────────────────────────────────────

		private static Dan_C MakeDanC(string typeStr, int red, int gold, bool lessThan) =>
			new Dan_C(
				ExamTypeFromString(typeStr),
				new int[] { red, gold },
				lessThan ? Exam.Range.Less : Exam.Range.More
			);

		private static Exam.Type ExamTypeFromString(string type) => type switch {
			"jp" => Exam.Type.JudgePerfect,
			"jg" => Exam.Type.JudgeGood,
			"jb" => Exam.Type.JudgeBad,
			"s" => Exam.Type.Score,
			"r" => Exam.Type.Roll,
			"h" => Exam.Type.Hit,
			"c" => Exam.Type.Combo,
			"a" => Exam.Type.Accuracy,
			"ja" => Exam.Type.JudgeADLIB,
			"jm" => Exam.Type.JudgeMine,
			_ => Exam.Type.Gauge,
		};

		public static LuaDanBuildFunc Generate() => new LuaDanBuildFunc();
	}
}
