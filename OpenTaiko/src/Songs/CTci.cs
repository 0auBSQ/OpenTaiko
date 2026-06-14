using System.Diagnostics;
using System.Drawing;
using FDK;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenTaiko;

// Parses .optktci / .tci (OpenTaiko individual chart format with osu! courses).
// Field names follow the Open Taiko Chart spec (Rev2.4).
internal class CTci {
	// ── JSON model ────────────────────────────────────────────────────────────

	private class TciJson {
		[JsonProperty("title")]    public JToken?  Title;       // string or {lang: string}
		[JsonProperty("subtitle")] public JToken?  Subtitle;
		[JsonProperty("artist")]   public JToken?  Artist;      // string or array
		[JsonProperty("creator")]  public JToken?  Creator;     // MAKER — string or array
		[JsonProperty("audio")]    public string?  Audio;
		[JsonProperty("songpreview")] public double Songpreview; // seconds
		[JsonProperty("albumart")] public string?  Albumart;
		[JsonProperty("background")] public string? Background;
		[JsonProperty("bpm")]      public double?  Bpm;
		[JsonProperty("offset")]   public double?  Offset;
		[JsonProperty("courses")]  public List<TciCourseJson>? Courses;
	}

	private class TciCourseJson {
		[JsonProperty("difficulty")] public string? Difficulty;
		[JsonProperty("level")]      public double  Level;
		[JsonProperty("single")]     public string? Single;     // .osu file
		// OpenTaiko extensions (not in spec)
		[JsonProperty("life")]  public int  Life = 5;
		[JsonProperty("side")]  public string? Side;
	}

	// ── Parsed data ───────────────────────────────────────────────────────────

	public CLocalizationData TITLE    = new();
	public CLocalizationData SUBTITLE = new();
	public string MAKER   = "";
	public string ARTIST  = "";
	public string GENRE   = "";
	public CTja.ESide SIDE     = CTja.ESide.eBoth;
	public bool   EXPLICIT     = false;
	public double SONGPREVIEW  = 0.0;   // seconds
	public string? ALBUMART;
	public string? AUDIO;
	public double? BPM;
	public double? OFFSET;
	// BPM info derived from the osu timing points (populated in Parse)
	public double FIRST_BPM = 120.0;
	public double MIN_BPM   = 120.0;
	public double MAX_BPM   = 120.0;
	public CSongUniqueID UniqueID;
	public string FolderPath;
	public string FilePath;
	public FileInfo FileInfo;

	public class CourseInfo {
		public int DifficultyIndex;
		public int Level;
		public CTja.ELevelIcon LevelIcon;
		public string NotesDesigner = "";
		public string OsuFilePath = "";
		public int Life = 5;
		public CTja.ESide Side = CTja.ESide.eEx;
	}

	public List<CourseInfo> Courses = new();

	public CTci(string filePath) {
		FilePath = Path.GetFullPath(filePath);
		FolderPath = Path.GetDirectoryName(FilePath)! + Path.DirectorySeparatorChar;
		FileInfo = new FileInfo(FilePath);
		Parse(filePath);
		UniqueID = new CSongUniqueID(FolderPath + "uniqueID.json");
	}

	// ── Song list node builder ────────────────────────────────────────────────

	public CSongListNode BuildSongListNode() {
		var node = new CSongListNode();
		node.nodeType = CSongListNode.ENodeType.SCORE;
		node.ldTitle    = TITLE;
		node.ldSubtitle = SUBTITLE;
		node.strMaker   = MAKER;
		node.songGenre  = GENRE;
		node.nSide      = SIDE;
		node.bExplicit  = EXPLICIT;
		node.uniqueId   = UniqueID;
		node.DanSongs   = new();

		var notesDesigners = new string[(int)Difficulty.Total];
		var levels         = new int[(int)Difficulty.Total];
		var levelIcons     = new CTja.ELevelIcon[(int)Difficulty.Total];
		for (int i = 0; i < (int)Difficulty.Total; i++) {
			notesDesigners[i] = "";
			levels[i]         = -1;
			levelIcons[i]     = CTja.ELevelIcon.eNone;
		}

		foreach (var course in Courses) {
			int i = course.DifficultyIndex;
			notesDesigners[i] = course.NotesDesigner;
			levels[i]         = course.Level;
			levelIcons[i]     = course.LevelIcon;
			node.difficultiesCount++;

			var score = new CScore();
			score.FileInfo.FileAbsolutePath  = FilePath;
			score.FileInfo.FolderAbsolutePath  = FolderPath;
			score.FileInfo.FileSize      = FileInfo.Length;
			score.FileInfo.LastUpdateDateTime        = FileInfo.LastWriteTime;
			score.ChartInfo.Title       = TITLE.GetString("");
			score.ChartInfo.strSubtitle = SUBTITLE.GetString("");
			score.ChartInfo.ArtistName  = ARTIST;
			score.ChartInfo.Genre       = GENRE;
			score.ChartInfo.strBGMFileName  = AUDIO ?? "";
			score.ChartInfo.Presound        = AUDIO ?? "";
			score.ChartInfo.nDemoBGMOffset = (int)(SONGPREVIEW * 1000.0);
			score.ChartInfo.Bpm     = FIRST_BPM;
			score.ChartInfo.BaseBpm = FIRST_BPM;
			score.ChartInfo.MinBpm  = MIN_BPM;
			score.ChartInfo.MaxBpm  = MAX_BPM;
			if (!string.IsNullOrEmpty(ALBUMART))
				score.ChartInfo.Preimage = ALBUMART;
			score.ChartInfo.nLevel     = Enumerable.Repeat(-1, (int)Difficulty.Total).ToArray();
			score.ChartInfo.nLevelIcon  = new CTja.ELevelIcon[(int)Difficulty.Total];
			score.ChartInfo.bChartBranch   = new bool[(int)Difficulty.Total];
			score.ChartInfo.nLevel[i]    = course.Level;
			score.ChartInfo.nLevelIcon[i] = course.LevelIcon;
			if ((Difficulty)i == Difficulty.Tower)
				score.ChartInfo.nLife = course.Life;
			node.score[i] = score;
		}

		// Propagate level info across all score slots
		foreach (var course in Courses) {
			int i = course.DifficultyIndex;
			for (int k = 0; k < (int)Difficulty.Total; k++) {
				node.score[i].ChartInfo.nLevel[k]    = levels[k];
				node.score[i].ChartInfo.nLevelIcon[k] = levelIcons[k];
			}
		}

		node.strNotesDesigner = notesDesigners;
		node.nLevel    = levels;
		node.nLevelIcon = levelIcons;
		node.nLife = Courses.FirstOrDefault(c => (Difficulty)c.DifficultyIndex == Difficulty.Tower)?.Life ?? 5;
		return node;
	}

	// ── Gameplay CTja builder ─────────────────────────────────────────────────

	public CTja BuildCtja(int difficulty) {
		var course = Courses.FirstOrDefault(c => c.DifficultyIndex == difficulty)
			?? throw new InvalidOperationException($"No TCI course for difficulty {difficulty}");

		if (!File.Exists(course.OsuFilePath))
			throw new FileNotFoundException($"osu file not found: {course.OsuFilePath}");

		var osu = new COsu(course.OsuFilePath);
		if (!osu.IsValidTaiko)
			throw new InvalidDataException($"Not a Taiko osu file: {course.OsuFilePath}");

		// strファイル名 must be relative — CTja.tWAVの読み込み prepends strFolderPath automatically.
		string bgmRelative = AUDIO ?? "";
		string bgmAbsolute = !string.IsNullOrEmpty(bgmRelative) ? FolderPath + bgmRelative : "";

		var tja = new CTja();
		tja.Activate();
		tja.strFullPath   = FilePath;
		tja.strFileName   = Path.GetFileName(FilePath);
		tja.strFolderPath = FolderPath;
		tja.uniqueID      = UniqueID;
		tja.bLoadChart    = true;
		tja.nReferenceDifficulty              = difficulty;
		tja.bChartExists[difficulty]   = true;
		tja.TITLE    = TITLE;
		tja.SUBTITLE = SUBTITLE;
		tja.MAKER    = MAKER;
		tja.GENRE    = GENRE;
		tja.SIDE     = SIDE;
		tja.nDemoBGMOffset = (int)(SONGPREVIEW * 1000.0);
		tja.strBGM_PATH = bgmRelative;
		if ((Difficulty)difficulty == Difficulty.Tower)
			tja.LIFE = course.Life;

		BuildBpmList(tja, osu);
		BuildChips(tja, osu, bgmRelative, bgmAbsolute);

		tja.SongListCourseMetadata[difficulty].LEVELtaiko      = course.Level;
		tja.SongListCourseMetadata[difficulty].LEVELtaikoIcon  = course.LevelIcon;
		tja.SongListCourseMetadata[difficulty].NOTESDESIGNER   = course.NotesDesigner;
		return tja;
	}

	// ── Private helpers ───────────────────────────────────────────────────────

	private void Parse(string filePath) {
		try {
			string json = File.ReadAllText(filePath);
			var data = JsonConvert.DeserializeObject<TciJson>(json);
			if (data == null) return;

			TITLE    = ParseLocalized(data.Title);
			SUBTITLE = ParseLocalized(data.Subtitle);
			MAKER    = ParseFirstString(data.Creator) ?? "";
			ARTIST   = ParseFirstString(data.Artist)  ?? "";
			SONGPREVIEW = data.Songpreview;
			ALBUMART    = data.Albumart;
			AUDIO       = data.Audio;
			BPM         = data.Bpm;
			OFFSET      = data.Offset;

			if (data.Courses == null) return;
			foreach (var c in data.Courses) {
				int diffIdx = ParseDifficulty(c.Difficulty);
				if (diffIdx < 0) {
					Trace.TraceWarning($"[CTci] Unknown difficulty '{c.Difficulty}' in {filePath}");
					continue;
				}
				string osuPath = FolderPath + (c.Single ?? "");
				var osuMeta = new COsu(osuPath, metadataOnly: true);
				string notesDesigner = osuMeta.Creator.Length > 0 ? osuMeta.Creator : MAKER;
				// If TCI has no audio field, fall back to the osu file's AudioFilename
				if (string.IsNullOrEmpty(AUDIO) && !string.IsNullOrEmpty(osuMeta.AudioFilename))
					AUDIO = osuMeta.AudioFilename;
				// BPM info from first course's timing points (all courses share audio/timing).
				// FIRST_BPM always comes from osu so it is guaranteed within [MIN_BPM, MAX_BPM].
				// TCI's "bpm" field is a metadata hint only; using it here would cause inconsistency
				// when it differs from the actual chart timing (e.g. 200 shown with min/max of 210).
				if (Courses.Count == 0 && osuMeta.TimingPoints.Count > 0) {
					var bpms = osuMeta.TimingPoints.Select(tp => 60000.0 / tp.MsPerBeat).ToList();
					FIRST_BPM = bpms[0];
					MIN_BPM   = bpms.Min();
					MAX_BPM   = bpms.Max();
				} else if (Courses.Count == 0 && BPM is > 0) {
					FIRST_BPM = MIN_BPM = MAX_BPM = BPM.Value;
				}
				double level = c.Level;
				Courses.Add(new CourseInfo {
					DifficultyIndex = diffIdx,
					Level     = (int)level,
					LevelIcon = (level - (int)level >= 0.5) ? CTja.ELevelIcon.ePlus : CTja.ELevelIcon.eNone,
					NotesDesigner = notesDesigner,
					OsuFilePath   = osuPath,
					Life = c.Life,
					Side = ParseSide(c.Side),
				});
			}
			// Fallback: use TCI's declared BPM if timing points weren't available
			if (BPM.HasValue && FIRST_BPM == 120.0) {
				FIRST_BPM = MIN_BPM = MAX_BPM = BPM.Value;
			}
		} catch (Exception ex) {
			Trace.TraceWarning($"[CTci] Failed to parse {filePath}: {ex.Message}");
		}
	}

	private static void BuildBpmList(CTja tja, COsu osu) {
		double firstBpm = osu.TimingPoints.Count > 0
			? 60000.0 / osu.TimingPoints[0].MsPerBeat
			: 120.0;

		for (int ib = 0; ib < 3; ib++) {
			tja.listBPM.Add(new CTja.CBPM {
				nInternalNumber = ib, nNotationTopNumber = ib,
				dbBPMValue = firstBpm,
				bpm_change_time = 0.0, bpm_change_bmscroll_time = 0.0,
				bpm_change_course = (CTja.ECourse)ib,
			});
		}
		tja.BPM = firstBpm; tja.BASEBPM = firstBpm;
		tja.MinBPM = firstBpm; tja.MaxBPM = firstBpm;

		double bmscroll = 0.0, prevTime = 0.0, prevBpm = firstBpm;
		for (int i = 0; i < osu.TimingPoints.Count; i++) {
			var tp = osu.TimingPoints[i];
			double bpm = 60000.0 / tp.MsPerBeat;
			if (i == 0) { prevBpm = bpm; continue; }
			bmscroll += (tp.OffsetMs - prevTime) * prevBpm / 15000.0;
			int idx = tja.listBPM.Count;
			tja.listBPM.Add(new CTja.CBPM {
				nInternalNumber = idx, nNotationTopNumber = idx,
				dbBPMValue = bpm,
				bpm_change_time = tp.OffsetMs,
				bpm_change_bmscroll_time = bmscroll,
				bpm_change_course = CTja.ECourse.eNormal,
			});
			tja.MinBPM = Math.Min(tja.MinBPM, bpm);
			tja.MaxBPM = Math.Max(tja.MaxBPM, bpm);
			prevTime = tp.OffsetMs; prevBpm = bpm;
		}
	}

	private static void BuildChips(CTja tja, COsu osu, string bgmRelative, string bgmAbsolute) {
		int bgmWavId = 1;
		var bgmChip = MakeChip(0x01, 0, bgmWavId);
		ApplyChipState(bgmChip, osu, 0);
		var cwav = new CTja.CWAV {
			nInternalNumber = bgmWavId, nNotationTopNumber = bgmWavId,
			bIsBGMSound = true,
			strFileName = bgmRelative, // relative — CTja prepends strFolderPath on load
			strCommentText = "BGM",
			PlayChip = bgmChip,
			SongVol = CSound.DefaultSongVol,
		};
		cwav.listThisWAVUseChannelNumberSet.Add(0x01);
		if (!string.IsNullOrEmpty(bgmAbsolute))
			cwav.SongLoudnessMetadata = LoudnessMetadataScanner.LoadForAudioPath(bgmAbsolute);
		tja.listWAV.Add(bgmWavId, cwav);
		AddToAllBranches(tja, bgmChip);

		GenerateBarLines(tja, osu);

		var rollStack = new Stack<CChip>();
		int noteIdx = 0;
		foreach (var note in osu.Notes) {
			var chip = MakeChip(note.ChannelNo, note.TimeMs, 0);
			ApplyChipState(chip, osu, note.TimeMs);
			if (note.ChannelNo == 0x17 && note.Duration > 0)
				chip.nBalloon = osu.GetBalloonHits(note.Duration);
			bool isRollStart = note.ChannelNo is 0x15 or 0x16 or 0x17;
			bool isRollEnd   = note.ChannelNo == 0x18;

			if (isRollStart) {
				rollStack.Push(chip);
			} else if (isRollEnd && rollStack.Count > 0) {
				var head = rollStack.Pop();
				head.end = chip; chip.start = head; chip.end = chip;
			}

			AddToAllBranches(tja, chip);

			// All taiko note chips go into listNoteChip (mirrors CTja.InsertNoteAtDefCursor).
			// Critically this includes 0x18 (EndRoll) — without it the engine never fires
			// the roll-end chip and rolls run indefinitely on screen.
			if (note.ChannelNo is >= 0x11 and <= 0x1F) {
				chip.nIntValue_InternalNumber = tja.listNoteChip.Count;
				tja.listNoteChip.Add(chip);
			}
			if (NotesManager.IsMissableNote((NotesManager.ENoteType)note.ChannelNo)) noteIdx++;
		}

		tja.nNotesCount_Common = noteIdx;
		for (int ib = 0; ib < 3; ib++) tja.nNotesCount_Branch[ib] = noteIdx;

		int lastMs = osu.Notes.Count > 0 ? osu.Notes.Max(n => n.TimeMs) : 0;
		AddToAllBranches(tja, MakeChip(0xFF, lastMs + 2000, 0));
		AddToAllBranches(tja, MakeChip(0xFF, lastMs + 3000, 0xFF));

		tja.listChip.Sort();
		for (int ib = 0; ib < 3; ib++) tja.listChip_Branch[ib].Sort();
	}

	private static void GenerateBarLines(CTja tja, COsu osu) {
		if (osu.TimingPoints.Count == 0) return;
		int maxTime = osu.Notes.Count > 0 ? osu.Notes.Max(n => n.TimeMs) + 5000 : 30000;
		int measure = 0;
		for (int ti = 0; ti < osu.TimingPoints.Count; ti++) {
			var tp = osu.TimingPoints[ti];
			int nextOffset = (ti + 1 < osu.TimingPoints.Count) ? osu.TimingPoints[ti + 1].OffsetMs : maxTime;
			double measureMs = tp.MsPerBeat * tp.Meter;
			double t = tp.OffsetMs;
			while (t < nextOffset && t <= maxTime) {
				int timeMs = (int)Math.Round(t);
				var bl = MakeChip(0x50, timeMs, 0);
				bl.nIntValue_InternalNumber = measure++;
				ApplyChipState(bl, osu, timeMs);
				tja.listChip.Add(bl);
				tja.listBarLineChip.Add(bl);
				for (int ib = 0; ib < 3; ib++) tja.listChip_Branch[ib].Add(bl);
				t += measureMs;
			}
		}
	}

	private static void ApplyChipState(CChip chip, COsu osu, int timeMs) {
		chip.dbBPM         = osu.GetBPMAt(timeMs);
		chip.dbSCROLL      = osu.GetSvMult(timeMs);
		chip.fBMSCROLLTime = osu.GetFBMSCROLLAt(timeMs);
	}

	private static CChip MakeChip(int channel, int timeMs, int intVal) {
		var c = new CChip(); c.tInitialize();
		c.nChannelNo = channel; c.nSoundTimems = timeMs; c.dbSoundTimems = timeMs;
		c.nIntValue = intVal; c.nIntValue_InternalNumber = intVal; c.start = c; c.end = c;
		c.bHideBarLine = false; // default is true; must be false or bar lines are never drawn
		return c;
	}

	private static void AddToAllBranches(CTja tja, CChip chip) {
		tja.listChip.Add(chip);
		for (int ib = 0; ib < 3; ib++) tja.listChip_Branch[ib].Add(chip);
	}

	// ── Shared parsers ────────────────────────────────────────────────────────

	internal static CLocalizationData ParseLocalized(JToken? token) {
		var ld = new CLocalizationData();
		if (token == null) return ld;
		if (token.Type == JTokenType.String) {
			ld.SetString("default", token.ToString());
		} else if (token is JObject obj) {
			foreach (var kv in obj)
				ld.SetString(kv.Key, kv.Value?.ToString() ?? "");
		}
		return ld;
	}

	private static string? ParseFirstString(JToken? token) {
		if (token == null) return null;
		if (token.Type == JTokenType.String) return token.ToString();
		if (token is JArray arr && arr.Count > 0) return arr[0].ToString();
		return null;
	}

	internal static int ParseDifficulty(string? s) => s?.ToLowerInvariant() switch {
		"easy"   or "0" => (int)Difficulty.Easy,
		"normal" or "1" => (int)Difficulty.Normal,
		"hard"   or "2" => (int)Difficulty.Hard,
		"oni"    or "3" => (int)Difficulty.Oni,
		"edit"   or "4" => (int)Difficulty.Edit,
		"tower"  or "5" => (int)Difficulty.Tower,
		_ => -1
	};

	private static CTja.ESide ParseSide(string? s) => s?.ToLowerInvariant() switch {
		"normal" => CTja.ESide.eNormal,
		"ex" or "extra" => CTja.ESide.eEx,
		_ => CTja.ESide.eBoth
	};
}
