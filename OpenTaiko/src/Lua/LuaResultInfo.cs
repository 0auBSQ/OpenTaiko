namespace OpenTaiko;

/// <summary>
/// What a play earned, handed to the result screen's ROActivity (results_tower, results_dan, results_regular) at
/// activate. The result stage fills it and saves everything before; Lua only reads it, through the getters (the
/// fields are internal to the game). Per-player getters take the player from 0.
/// </summary>
public sealed class LuaResultInfo {
	internal string title = "";
	internal string subtitle = "";
	internal int playerCount = 1;
	internal int playerSide;

	// per player; good / ok / bad as the result screens name them
	internal readonly int[] good = new int[5];
	internal readonly int[] ok = new int[5];
	internal readonly int[] bad = new int[5];
	internal readonly int[] roll = new int[5];
	internal readonly int[] maxCombo = new int[5];
	internal readonly int[] adlib = new int[5];
	internal readonly int[] boom = new int[5];
	internal readonly long[] score = new long[5];
	internal readonly long[] previousBestScore = new long[5];
	// the play went into the player's save (not on auto, a watched replay, or a tower or dan played with mods)
	internal readonly bool[] saved = new bool[5];
	internal readonly int[] difficulty = new int[5];
	internal readonly double[] gauge = new double[5];
	internal readonly int[] gaugeType = new int[5];
	internal readonly float[] norma = new float[5];
	internal readonly float[] killZone = new float[5];
	internal readonly int[] clearType = new int[5];
	internal readonly int[] scoreRank = new int[5];
	internal readonly bool[] cleared = new bool[5];
	internal readonly string[][] speech = [[], [], [], [], []];

	// AI battle
	internal bool aiBattle;
	internal int[] aiSections = [];

	// tower
	internal string towerType = "";
	internal int towerSide;
	internal int floor;
	internal int totalFloors;
	internal int lives;
	internal int maxLives;
	internal int towerRank;
	internal int previousBestFloor;
	internal bool newBestFloor;
	internal bool newBestScore;
	internal bool tired;
	internal double bpm;

	// dan
	internal int danTick;
	internal LuaColor danTickColor = new(255, 255, 255);
	internal int danResult;
	internal int danCombo;
	internal int totalHits;
	internal int songReached;
	internal int examSong;
	internal LuaResultDanSong[] danSongs = [];
	internal LuaResultExam[] exams = [];
	internal int danGradeChange;
	internal LuaResultDanGrade danGrade = new();
	internal LuaResultDanGrade previousNameplateDan = new();

	public string Title => title;
	public string Subtitle => subtitle;
	public int PlayerCount => playerCount;
	/// <summary>The nameplate's side for player 1: 1 when the first player plays on the blue side, else 0.</summary>
	public int PlayerSide => playerSide;

	public int GetGoodCount(int player) => At(good, player);
	public int GetOkCount(int player) => At(ok, player);
	public int GetBadCount(int player) => At(bad, player);
	public int GetRollCount(int player) => At(roll, player);
	public int GetMaxCombo(int player) => At(maxCombo, player);
	public int GetADLibCount(int player) => At(adlib, player);
	public int GetBoomCount(int player) => At(boom, player);
	public long GetScore(int player) => At(score, player);
	/// <summary>The player's best score on this chart and difficulty before this play was saved, 0 when none.</summary>
	public long GetPreviousBestScore(int player) => At(previousBestScore, player);
	public bool IsSaved(int player) => At(saved, player);
	/// <summary>The difficulty played, 0 Easy to 4 Extra.</summary>
	public int GetDifficulty(int player) => At(difficulty, player);
	/// <summary>The soul gauge at the end of the play, 0 to 100.</summary>
	public double GetGauge(int player) => At(gauge, player);
	/// <summary>0 normal, 1 hard, 2 extreme.</summary>
	public int GetGaugeType(int player) => At(gaugeType, player);
	/// <summary>The gauge value a normal gauge clears from.</summary>
	public float GetNorma(int player) => At(norma, player);
	/// <summary>The extreme gauge's kill zone, 0 to 100 (0 on the other gauges).</summary>
	public float GetKillZone(int player) => At(killZone, player);
	/// <summary>0 not cleared, 1 assisted clear, 2 clear, 3 full combo, 4 perfect.</summary>
	public int GetClearType(int player) => At(clearType, player);
	/// <summary>0 none, then the score ranks from 1 to 7.</summary>
	public int GetScoreRank(int player) => At(scoreRank, player);
	/// <summary>The play counts as cleared; in an AI battle, player 1 won.</summary>
	public bool IsCleared(int player) => At(cleared, player);
	/// <summary>The player's character's result line for a mood, 0 (worst) to 5 (perfect).</summary>
	public string GetSpeechText(int player, int mood) {
		var lines = At(speech, player);
		return lines == null || lines.Length == 0 ? "" : lines[Math.Clamp(mood, 0, lines.Length - 1)];
	}

	public bool IsAIBattle => aiBattle;
	public int AISectionCount => aiSections.Length;
	/// <summary>How section i (from 0) ended: 0 not played to its end, 1 won, 2 lost.</summary>
	public int GetAISectionEnd(int section) => At(aiSections, section);

	/// <summary>The chart's TOWERTYPE, the look of its tower ("" when it names none).</summary>
	public string TowerType => towerType;
	/// <summary>0 Sweet, 1 Spicy.</summary>
	public int TowerSide => towerSide;
	public int ReachedFloor => floor;
	public int TotalFloors => totalFloors;
	public int Lives => lives;
	public int MaxLives => maxLives;
	/// <summary>0 none, then the cells of 8_TowerResult/ScoreRankEffect.png from 1: 10, 25, 50 and 75 %, clear, full
	/// combo, perfect.</summary>
	public int TowerRank => towerRank;
	/// <summary>The best floor before this play was saved.</summary>
	public int PreviousBestFloor => previousBestFloor;
	public bool IsNewBestFloor => newBestFloor;
	public bool IsNewBestScore => newBestScore;
	/// <summary>The play ended with the tower character on its tired animations (few lives left).</summary>
	public bool IsTired => tired;
	/// <summary>The BPM the play ended on, which times the tower character's animations as in play.</summary>
	public double Bpm => bpm;

	/// <summary>The dan plate's ticks and their colour (the chart's DANTICK and DANTICKCOLOR).</summary>
	public int DanTick => danTick;
	public LuaColor DanTickColor => danTickColor;
	/// <summary>0 failed, 1 passed (red), 2 passed (gold).</summary>
	public int DanResult => danResult;
	/// <summary>0, 1 no bad (full combo), 2 no bad and no ok (perfect).</summary>
	public int DanCombo => danCombo;
	/// <summary>Good, ok and roll hits together.</summary>
	public int TotalHits => totalHits;
	public int DanSongCount => danSongs.Length;
	/// <summary>The song the play ended on, from 0.</summary>
	public int DanSongReached => songReached;
	/// <summary>The song whose per-song exams were shown last, from 0.</summary>
	public int DanExamSong => examSong;
	/// <summary>Song i of the dan (from 0), or nil.</summary>
	public LuaResultDanSong? GetDanSong(int song) => At(danSongs, song);
	public int ExamCount => exams.Length;
	/// <summary>Exam i (from 0, the chart's order), or nil.</summary>
	public LuaResultExam? GetExam(int exam) => At(exams, exam);
	/// <summary>0 no change, 1 a new dan grade, 2 a better grade on a dan passed before (gold, or a better clear).
	/// A new or better grade goes onto the player's nameplate.</summary>
	public int DanGradeChange => danGradeChange;
	/// <summary>The dan's grade in the save after this play; its Title is "" when the dan was not passed.</summary>
	public LuaResultDanGrade DanGrade => danGrade;
	/// <summary>The dan grade the nameplate showed before this play.</summary>
	public LuaResultDanGrade PreviousNameplateDan => previousNameplateDan;

	private static T At<T>(T[] values, int player) => player >= 0 && player < values.Length ? values[player] : default!;
}

/// <summary>A song of a dan play.</summary>
public sealed class LuaResultDanSong {
	internal string title = "";
	internal string subtitle = "";
	internal int difficulty, level, good, ok, bad, roll;

	/// <summary>"???" for a song the chart keeps hidden.</summary>
	public string Title => title;
	/// <summary>"" for a song the chart keeps hidden.</summary>
	public string Subtitle => subtitle;
	/// <summary>0 Easy to 4 Extra.</summary>
	public int Difficulty => difficulty;
	public int Level => level;
	public int Good => good;
	public int Ok => ok;
	public int Bad => bad;
	public int Roll => roll;
}

/// <summary>A dan grade: the dan's title (as the nameplate shows it), gold or red, and its clear.</summary>
public sealed class LuaResultDanGrade {
	internal string title = "";
	internal bool gold;
	internal int tier;

	public string Title => title;
	public bool Gold => gold;
	/// <summary>0 clear, 1 full combo, 2 perfect.</summary>
	public int Tier => tier;
}

/// <summary>An exam of a dan play, as it ended.</summary>
public sealed class LuaResultExam {
	internal int type, range, value, amount, percent, status;
	internal int previousBestAmount = -1;
	internal LuaResultExam?[] songs = [];

	/// <summary>0 gauge, 1 good, 2 ok, 3 bad, 4 score, 5 rolls, 6 hits, 7 combo, 8 accuracy, 9 AD-LIB, 10 BOOM.</summary>
	public int Type => type;
	/// <summary>0 at least Value, 1 less than Value.</summary>
	public int Range => range;
	/// <summary>The value to pass (red).</summary>
	public int Value => value;
	/// <summary>The count shown; for a "less than" exam, what was left before failing it.</summary>
	public int Amount => amount;
	/// <summary>The player's best Amount on this exam before this play was saved, in the same terms (larger is better);
	/// -1 when none. A per-song exam's songs (GetSong) each have their own.</summary>
	public int PreviousBestAmount => previousBestAmount;
	/// <summary>Amount against Value, 0 to 100.</summary>
	public int Percent => percent;
	/// <summary>0 failed, 1 passed, 2 passed gold.</summary>
	public int Status => status;
	/// <summary>The exam is judged song by song; this one is the last song reached.</summary>
	public bool IsPerSong => songs.Length > 0;
	/// <summary>A per-song exam's exam on song i (from 0), or nil.</summary>
	public LuaResultExam? GetSong(int song) => song >= 0 && song < songs.Length ? songs[song] : null;
}
