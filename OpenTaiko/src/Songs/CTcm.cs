using System.Diagnostics;
using System.Drawing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenTaiko;

// Parses .optktcm / .tcm (OpenTaiko dan course builder format).
// Field names follow the Open Taiko Chart spec (Rev2.4) where applicable.
internal class CTcm {
	// ── JSON model ────────────────────────────────────────────────────────────

	private class TcmJson {
		[JsonProperty("title")]    public JToken? Title;
		[JsonProperty("subtitle")] public JToken? Subtitle;
		[JsonProperty("albumart")] public string? Albumart;
		// OpenTaiko extensions
		[JsonProperty("dan_tick")]       public int   DanTick;
		[JsonProperty("dan_tick_color")] public int[]? DanTickColor;
		[JsonProperty("exams")]  public List<TcmExamJson?>? Exams;
		[JsonProperty("charts")] public List<TcmChartJson?>? Charts;
	}

	private class TcmExamJson {
		[JsonProperty("type")]  public string? Type;
		[JsonProperty("range")] public string? Range;
		// value: [int, int]  OR  [[int,int], [int,int], null, ...]
		[JsonProperty("value")] public JToken? Value;
	}

	private class TcmChartJson {
		// "id" replaces spec's "file" — references a song by its OpenTaiko unique ID
		[JsonProperty("id")]         public string? Id;
		[JsonProperty("difficulty")] public string? Difficulty;
		// OpenTaiko extension
		[JsonProperty("title_show")] public bool TitleShow;
	}

	// ── Parsed data ───────────────────────────────────────────────────────────

	public CLocalizationData TITLE    = new();
	public CLocalizationData SUBTITLE = new();
	public string ALBUMART  = "";
	public string GENRE     = "段位道場";
	public int    DAN_TICK  = 2;
	public Color  DAN_TICK_COLOR = Color.White;
	public CSongUniqueID UniqueID;
	public string FolderPath;
	public string FilePath;
	public FileInfo FileInfo;

	public class SongRef {
		public string UniqueId = "";
		public int    DifficultyIndex;
		public bool   TitleShow;
	}

	public List<SongRef> Songs = new();

	// Global exam per slot (flat value exam)
	public Dan_C?[] GlobalExams = new Dan_C?[CExamInfo.cMaxExam];
	// Per-chart exam per slot: [examSlot][chartIndex] = Dan_C or null
	public Dan_C?[]?[] PerChartExams = new Dan_C?[]?[CExamInfo.cMaxExam];

	public CTcm(string filePath) {
		FilePath = Path.GetFullPath(filePath);
		FolderPath = Path.GetDirectoryName(FilePath)! + Path.DirectorySeparatorChar;
		FileInfo = new FileInfo(FilePath);
		Parse(filePath);
		UniqueID = new CSongUniqueID(FolderPath + "uniqueID.json");
	}

	// ── Song list node builder ────────────────────────────────────────────────

	// Returns null (and logs an error) if any referenced song is missing.
	public CSongListNode? BuildSongListNode() {
		if (Songs.Count == 0) return null;

		var danSongsList = new List<CTja.DanSongs>();
		for (int si = 0; si < Songs.Count; si++) {
			var songRef = Songs[si];
			var refNode = CSongDict.tGetNodeFromID(songRef.UniqueId);
			if (refNode == null) {
				string msg = $"[CTcm] Song not found (id='{songRef.UniqueId}') in {FilePath}";
				Trace.TraceError(msg);
				LogNotification.PopError(msg);
				return null;
			}
			int diff = songRef.DifficultyIndex;
			if (refNode.score[diff] == null) {
				string msg = $"[CTcm] Song '{songRef.UniqueId}' has no difficulty {diff} in {FilePath}";
				Trace.TraceError(msg);
				LogNotification.PopError(msg);
				return null;
			}
			var danSong = new CTja.DanSongs {
				Title    = refNode.ldTitle.GetString(""),
				SubTitle = refNode.ldSubtitle.GetString(""),
				Genre    = refNode.songGenre,
				FileName = refNode.score[diff].ファイル情報.ファイルの絶対パス ?? "",
				Level      = refNode.nLevel[diff],
				Difficulty = diff,
				ScoreInit  = 300,
				ScoreDiff  = 120,
				bTitleShow = songRef.TitleShow,
			};
			for (int slot = 0; slot < CExamInfo.cMaxExam; slot++) {
				var perChart = PerChartExams[slot];
				if (perChart != null && si < perChart.Length && perChart[si] != null)
					danSong.Dan_C[slot] = perChart[si]!;
			}
			danSongsList.Add(danSong);
		}

		var node = new CSongListNode();
		node.nodeType   = CSongListNode.ENodeType.SCORE;
		node.ldTitle    = TITLE;
		node.ldSubtitle = SUBTITLE;
		node.songGenre  = GENRE;
		node.nDanTick   = DAN_TICK;
		node.cDanTickColor = DAN_TICK_COLOR;
		node.uniqueId   = UniqueID;
		node.DanSongs   = danSongsList;
		node.difficultiesCount = 1;

		var danC = new Dan_C[CExamInfo.cMaxExam];
		for (int i = 0; i < CExamInfo.cMaxExam; i++)
			if (GlobalExams[i] != null) danC[i] = GlobalExams[i]!;
		node.Dan_C = danC;

		var score = new CScore();
		score.ファイル情報.ファイルの絶対パス  = FilePath;
		score.ファイル情報.フォルダの絶対パス  = FolderPath;
		score.ファイル情報.ファイルサイズ      = FileInfo.Length;
		score.ファイル情報.最終更新日時        = FileInfo.LastWriteTime;
		score.譜面情報.タイトル       = TITLE.GetString("");
		score.譜面情報.strサブタイトル = SUBTITLE.GetString("");
		score.譜面情報.nレベル        = Enumerable.Repeat(-1, (int)Difficulty.Total).ToArray();
		score.譜面情報.nレベル[(int)Difficulty.Dan] = 10;
		score.譜面情報.nLevelIcon  = new CTja.ELevelIcon[(int)Difficulty.Total];
		score.譜面情報.b譜面分岐    = new bool[(int)Difficulty.Total];
		score.譜面情報.nDanTick    = DAN_TICK;
		score.譜面情報.cDanTickColor = DAN_TICK_COLOR;
		node.score[(int)Difficulty.Dan] = score;
		node.nLevel[(int)Difficulty.Dan] = 10;

		return node;
	}

	// ── Gameplay CTja builder ─────────────────────────────────────────────────

	public CTja BuildDanCtja() {
		var builder = LuaDanBuildFunc.Generate();
		builder.SetTitle(TITLE.GetString(""));
		builder.SetSubtitle(SUBTITLE.GetString(""));
		builder.SetDanTick(DAN_TICK);
		builder.SetDanTickColor(DAN_TICK_COLOR.R, DAN_TICK_COLOR.G, DAN_TICK_COLOR.B);

		for (int slot = 0; slot < CExamInfo.cMaxExam; slot++) {
			if (GlobalExams[slot] is { } ge) {
				var v = ge.GetValue();
				builder.SetGlobalExam(slot + 1, ExamTypeToString(ge.ExamType), v[0], v[1], ge.ExamRange == Exam.Range.Less);
			}
		}

		for (int si = 0; si < Songs.Count; si++) {
			var songRef = Songs[si];
			var refNode = CSongDict.tGetNodeFromID(songRef.UniqueId)
				?? throw new InvalidOperationException($"[CTcm] Song not found: {songRef.UniqueId}");
			builder.AddSong(new LuaSongNode(refNode), songRef.DifficultyIndex);
			for (int slot = 0; slot < CExamInfo.cMaxExam; slot++) {
				var perChart = PerChartExams[slot];
				if (perChart != null && si < perChart.Length && perChart[si] is { } pe) {
					var v = pe.GetValue();
					builder.SetPerSongExam(si + 1, slot + 1, ExamTypeToString(pe.ExamType), v[0], v[1], pe.ExamRange == Exam.Range.Less);
				}
			}
		}

		return builder.BuildDanCtjaInternal();
	}

	// ── Private helpers ───────────────────────────────────────────────────────

	private void Parse(string filePath) {
		try {
			string json = File.ReadAllText(filePath);
			var data = JsonConvert.DeserializeObject<TcmJson>(json);
			if (data == null) return;

			TITLE    = CTci.ParseLocalized(data.Title);
			SUBTITLE = CTci.ParseLocalized(data.Subtitle);
			ALBUMART = data.Albumart ?? "";
			DAN_TICK = data.DanTick;
			if (data.DanTickColor is { Length: >= 3 })
				DAN_TICK_COLOR = Color.FromArgb(
					Math.Clamp(data.DanTickColor[0], 0, 255),
					Math.Clamp(data.DanTickColor[1], 0, 255),
					Math.Clamp(data.DanTickColor[2], 0, 255));

			// Parse charts first so we know the count when processing per-chart exam values
			if (data.Charts != null) {
				foreach (var chart in data.Charts) {
					if (chart == null || string.IsNullOrEmpty(chart.Id)) {
						Trace.TraceWarning($"[CTcm] Chart entry missing 'id' in {filePath}");
						continue;
					}
					int diffIdx = CTci.ParseDifficulty(chart.Difficulty);
					if (diffIdx < 0) diffIdx = (int)Difficulty.Oni;
					Songs.Add(new SongRef {
						UniqueId = chart.Id,
						DifficultyIndex = diffIdx,
						TitleShow = chart.TitleShow,
					});
				}
			}

			// Parse exams (after charts so per-chart count is known)
			if (data.Exams != null) {
				for (int slot = 0; slot < data.Exams.Count && slot < CExamInfo.cMaxExam; slot++) {
					var examJson = data.Exams[slot];
					if (examJson?.Type == null || examJson.Value == null) continue;

					var type  = ExamTypeFromString(examJson.Type);
					var range = string.Equals(examJson.Range, "less", StringComparison.OrdinalIgnoreCase)
						? Exam.Range.Less : Exam.Range.More;

					if (examJson.Value is not JArray valueArr || valueArr.Count == 0) continue;

					// Determine per-chart vs global: if ANY element is an array or null → per-chart
					bool isPerChart = valueArr.Any(t => t.Type is JTokenType.Array or JTokenType.Null);

					if (isPerChart) {
						int chartCount = Songs.Count;
						if (valueArr.Count != chartCount) {
							string msg = $"[CTcm] Exam slot {slot}: value count ({valueArr.Count}) ≠ chart count ({chartCount}) in {FilePath}";
							Trace.TraceWarning(msg);
							LogNotification.PopWarning(msg);
						}
						var perChartArr = new Dan_C?[chartCount];
						for (int ci = 0; ci < chartCount; ci++) {
							if (ci >= valueArr.Count) break; // null-pad (already null by default)
							var entry = valueArr[ci];
							if (entry.Type == JTokenType.Null) continue;
							if (entry is JArray pair && pair.Count >= 2) {
								if (int.TryParse(pair[0].ToString(), out int r) && int.TryParse(pair[1].ToString(), out int g))
									perChartArr[ci] = new Dan_C(type, new int[] { r, g }, range);
							}
						}
						PerChartExams[slot] = perChartArr;
					} else {
						// Flat [red, gold]
						if (valueArr.Count >= 2 &&
							int.TryParse(valueArr[0].ToString(), out int red) &&
							int.TryParse(valueArr[1].ToString(), out int gold)) {
							GlobalExams[slot] = new Dan_C(type, new int[] { red, gold }, range);
						} else if (valueArr.Count == 1 &&
							int.TryParse(valueArr[0].ToString(), out int single)) {
							GlobalExams[slot] = new Dan_C(type, new int[] { single, single }, range);
						}
					}
				}
			}
		} catch (Exception ex) {
			Trace.TraceWarning($"[CTcm] Failed to parse {filePath}: {ex.Message}");
		}
	}

	internal static Exam.Type ExamTypeFromString(string type) => type.ToLowerInvariant() switch {
		"judgeperfect" or "jp" => Exam.Type.JudgePerfect,
		"judgegood"    or "jg" => Exam.Type.JudgeGood,
		"judgebad"     or "jb" => Exam.Type.JudgeBad,
		"score"        or "s"  => Exam.Type.Score,
		"roll"         or "r"  => Exam.Type.Roll,
		"hit"          or "h"  => Exam.Type.Hit,
		"combo"        or "c"  => Exam.Type.Combo,
		"accuracy"     or "a"  => Exam.Type.Accuracy,
		"judgeadlib"   or "ja" => Exam.Type.JudgeADLIB,
		"judgemine"    or "jm" => Exam.Type.JudgeMine,
		_ => Exam.Type.Gauge,
	};

	private static string ExamTypeToString(Exam.Type type) => type switch {
		Exam.Type.JudgePerfect => "judgeperfect",
		Exam.Type.JudgeGood    => "judgegood",
		Exam.Type.JudgeBad     => "judgebad",
		Exam.Type.Score        => "score",
		Exam.Type.Roll         => "roll",
		Exam.Type.Hit          => "hit",
		Exam.Type.Combo        => "combo",
		Exam.Type.Accuracy     => "accuracy",
		Exam.Type.JudgeADLIB   => "judgeadlib",
		Exam.Type.JudgeMine    => "judgemine",
		_ => "gauge",
	};
}
