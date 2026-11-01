using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenTaiko;

// Parses .optktct / .tct (OpenTaiko tower chart format): plays one course of any song, referenced by its unique ID,
// as a tower. The floors are the chart's measures, as for a tower course in a .tja.
internal class CTct {
	// ── JSON model ────────────────────────────────────────────────────────────

	private class TctJson {
		[JsonProperty("title")]      public JToken? Title { get; set; }       // string or {lang: string}; the song's when missing
		[JsonProperty("subtitle")]   public JToken? Subtitle { get; set; }
		[JsonProperty("id")]         public string? Id { get; set; }
		[JsonProperty("difficulty")] public string? Difficulty { get; set; }  // the course to play; oni when missing
		[JsonProperty("level")]      public double? Level { get; set; }       // the course's level when missing
		[JsonProperty("life")]       public int Life { get; set; } = 5;
		[JsonProperty("side")]       public string? Side { get; set; }        // normal or ex; from the course when missing
		[JsonProperty("towertype")]  public string? TowerType { get; set; }   // the tower's look (Tower/Down/<look>)
	}

	// ── Parsed data ───────────────────────────────────────────────────────────

	public CLocalizationData? TITLE;
	public CLocalizationData? SUBTITLE;
	public string SourceId = "";
	public int SourceDifficulty = (int)Difficulty.Oni;
	public double? LEVEL;
	public int LIFE = 5;
	public CTja.ESide? SIDE;
	public string? TOWERTYPE;
	public CSongUniqueID UniqueID;
	public string FolderPath;
	public string FilePath;
	public FileInfo FileInfo;

	public CTct(string filePath) {
		FilePath = Path.GetFullPath(filePath);
		FolderPath = Path.GetDirectoryName(FilePath)! + Path.DirectorySeparatorChar;
		FileInfo = new FileInfo(FilePath);
		Parse(filePath);
		UniqueID = new CSongUniqueID(FolderPath + "uniqueID.json");
	}

	// Easy and Normal courses get the easy timing of a Normal side tower, the others the Ex side
	private CTja.ESide Side => SIDE ?? (SourceDifficulty <= (int)Difficulty.Normal ? CTja.ESide.eNormal : CTja.ESide.eEx);

	// ── Song list node builder ────────────────────────────────────────────────

	// Returns null (and logs an error) when the song or its course is missing.
	public CSongListNode? BuildSongListNode() {
		var source = FindSource();
		if (source == null) return null;
		var (srcNode, srcScore) = source.Value;

		int floors;
		try {
			CTja chart = BuildCtja(0);
			floors = CountFloors(chart);
			chart.DeActivate();
		} catch (Exception ex) {
			Report($"[CTct] Could not read the chart of '{SourceId}' in {FilePath}: {ex.Message}");
			return null;
		}

		int tower = (int)Difficulty.Tower;
		int level = LEVEL.HasValue ? (int)LEVEL.Value : srcNode.nLevel[SourceDifficulty];
		double levelDecimal = LEVEL ?? srcNode.dLevel[SourceDifficulty];
		var levelIcon = LEVEL.HasValue ? LevelIconOf(LEVEL.Value) : srcNode.nLevelIcon[SourceDifficulty];

		var node = new CSongListNode();
		node.nodeType   = CSongListNode.ENodeType.SCORE;
		node.ldTitle    = TITLE ?? srcNode.ldTitle;
		node.ldSubtitle = SUBTITLE ?? srcNode.ldSubtitle;
		node.strMaker   = srcNode.strMaker;
		node.songGenre  = srcNode.songGenre;
		node.bExplicit  = srcNode.bExplicit;
		node.Compat     = srcNode.Compat;
		node.nSide      = Side;
		node.uniqueId   = UniqueID;
		node.DanSongs   = new();
		node.difficultiesCount = 1;
		node.nLife       = LIFE;
		node.nTotalFloor = floors;
		node.nTowerType  = TOWERTYPE;

		node.strNotesDesigner = Enumerable.Repeat("", (int)Difficulty.Total).ToArray();
		node.strNotesDesigner[tower] = srcNode.strNotesDesigner[SourceDifficulty];
		node.nLevel     = Enumerable.Repeat(-1, (int)Difficulty.Total).ToArray();
		node.dLevel     = Enumerable.Repeat(-1.0, (int)Difficulty.Total).ToArray();
		node.nLevelIcon = Enumerable.Repeat(CTja.ELevelIcon.eNone, (int)Difficulty.Total).ToArray();
		node.nLevel[tower]     = level;
		node.dLevel[tower]     = levelDecimal;
		node.nLevelIcon[tower] = levelIcon;

		var src = srcScore.ChartInfo;
		var score = new CScore();
		score.FileInfo.FileAbsolutePath   = FilePath;
		// the song's folder: its preimage, preview audio and background are relative to it
		score.FileInfo.FolderAbsolutePath = srcScore.FileInfo.FolderAbsolutePath;
		score.FileInfo.FileSize           = FileInfo.Length;
		score.FileInfo.LastUpdateDateTime = FileInfo.LastWriteTime;
		score.ChartInfo.Title        = node.ldTitle.GetString("");
		score.ChartInfo.strSubtitle  = node.ldSubtitle.GetString("");
		score.ChartInfo.ArtistName   = src.ArtistName;
		score.ChartInfo.Comment      = src.Comment;
		score.ChartInfo.Genre        = src.Genre;
		score.ChartInfo.Preimage     = src.Preimage;
		score.ChartInfo.Presound     = src.Presound;
		score.ChartInfo.Backgound    = src.Backgound;
		score.ChartInfo.Level        = level;
		score.ChartInfo.Bpm          = src.Bpm;
		score.ChartInfo.BaseBpm      = src.BaseBpm;
		score.ChartInfo.MinBpm       = src.MinBpm;
		score.ChartInfo.MaxBpm       = src.MaxBpm;
		score.ChartInfo.strBGMFileName       = src.strBGMFileName;
		score.ChartInfo.SongVol              = src.SongVol;
		score.ChartInfo.SongLoudnessMetadata = src.SongLoudnessMetadata;
		score.ChartInfo.nDemoBGMOffset       = src.nDemoBGMOffset;
		score.ChartInfo.nLevel       = (int[])node.nLevel.Clone();
		score.ChartInfo.nLevelIcon   = (CTja.ELevelIcon[])node.nLevelIcon.Clone();
		score.ChartInfo.bChartBranch = new bool[(int)Difficulty.Total];
		score.ChartInfo.bChartBranch[tower] = src.bChartBranch?[SourceDifficulty] ?? false;
		score.ChartInfo.nLife       = LIFE;
		score.ChartInfo.nTotalFloor = floors;
		score.ChartInfo.nTowerType  = TOWERTYPE;
		node.score[tower] = score;

		return node;
	}

	// ── Gameplay CTja builder ─────────────────────────────────────────────────

	public CTja BuildCtja(int player) {
		var source = FindSource() ?? throw new InvalidOperationException($"[CTct] Song not found: {SourceId}");
		var (srcNode, srcScore) = source;

		string chartPath = srcScore.FileInfo.FileAbsolutePath ?? "";
		string ext = Path.GetExtension(chartPath).ToLowerInvariant();
		CTja tja = ext is ".optktci" or ".tci"
			? new CTci(chartPath).BuildCtja(SourceDifficulty)
			: new CTja(chartPath, srcNode.Compat, (Difficulty)SourceDifficulty, player, loadChart: true);

		tja.LIFE = LIFE;
		tja.TOWERTYPE = TOWERTYPE;
		tja.SIDE = Side;
		if (TITLE != null) tja.TITLE = TITLE;
		if (SUBTITLE != null) tja.SUBTITLE = SUBTITLE;
		return tja;
	}

	// ── Private helpers ───────────────────────────────────────────────────────

	// The referenced song and its course; only .tja and .tci songs, as a .tcm or .tct holds no chart of its own
	private (CSongListNode node, CScore score)? FindSource() {
		var srcNode = CSongDict.tGetNodeFromID(SourceId);
		if (srcNode == null) {
			Report($"[CTct] Song not found (id='{SourceId}') in {FilePath}");
			return null;
		}
		var srcScore = srcNode.score[SourceDifficulty];
		if (srcScore == null) {
			Report($"[CTct] Song '{SourceId}' has no difficulty {SourceDifficulty} in {FilePath}");
			return null;
		}
		string ext = Path.GetExtension(srcScore.FileInfo.FileAbsolutePath ?? "").ToLowerInvariant();
		if (ext is ".optktcm" or ".tcm" or ".optktct" or ".tct") {
			Report($"[CTct] Song '{SourceId}' is a dan or tower file, not a chart, in {FilePath}");
			return null;
		}
		return (srcNode, srcScore);
	}

	// the highest measure number plus one, as CSongManager counts a tower course's floors
	private static int CountFloors(CTja chart) {
		int floors = 0;
		foreach (CChip chip in chart.listChip) {
			if (chip.nChannelNo == 0x50 && chip.nIntValue_InternalNumber > floors)
				floors = chip.nIntValue_InternalNumber;
		}
		return floors + 1;
	}

	private static CTja.ELevelIcon LevelIconOf(double level) {
		double frac = level - (int)level;
		return frac >= 0.5 ? CTja.ELevelIcon.ePlus : (frac > 0 ? CTja.ELevelIcon.eMinus : CTja.ELevelIcon.eNone);
	}

	private static void Report(string msg) {
		Trace.TraceError(msg);
		LogNotification.PopError(msg);
	}

	private void Parse(string filePath) {
		try {
			string json = File.ReadAllText(filePath);
			var data = JsonConvert.DeserializeObject<TctJson>(json);
			if (data == null) return;

			if (data.Title != null) TITLE = CTci.ParseLocalized(data.Title);
			if (data.Subtitle != null) SUBTITLE = CTci.ParseLocalized(data.Subtitle);
			SourceId = data.Id ?? "";
			if (SourceId.Length == 0)
				Trace.TraceWarning($"[CTct] Missing 'id' in {filePath}");
			int diff = CTci.ParseDifficulty(data.Difficulty);
			SourceDifficulty = diff < 0 ? (int)Difficulty.Oni : diff;
			LEVEL = data.Level;
			LIFE = Math.Max(1, data.Life);
			SIDE = data.Side?.ToLowerInvariant() switch {
				"normal" => CTja.ESide.eNormal,
				"ex" or "extra" => CTja.ESide.eEx,
				_ => null,
			};
			TOWERTYPE = string.IsNullOrWhiteSpace(data.TowerType) ? null : data.TowerType;
		} catch (Exception ex) {
			Trace.TraceWarning($"[CTct] Failed to parse {filePath}: {ex.Message}");
		}
	}
}
