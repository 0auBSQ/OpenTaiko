namespace OpenTaiko;

/// <summary>
/// What a play earned, handed to the result screen's ROActivity (results_tower, results_dan, results_regular) at
/// activate. The result stage fills it and saves everything before; Lua only reads it, through the getters (the
/// fields are internal to the game).
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
	// the play went into the player's save (not on auto, a watched replay, or a tower or dan played with mods)
	internal readonly bool[] saved = new bool[5];

	// tower
	internal string towerType = "";
	internal int towerSide;
	internal int floor;
	internal int totalFloors;
	internal int lives;
	internal int maxLives;
	internal int towerRank;
	internal int previousBestFloor;
	internal long previousBestScore;
	internal bool newBestFloor;
	internal bool newBestScore;
	internal bool tired;
	internal double bpm;

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
	public bool IsSaved(int player) => At(saved, player);

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
	/// <summary>The bests before this play was saved.</summary>
	public int PreviousBestFloor => previousBestFloor;
	public long PreviousBestScore => previousBestScore;
	public bool IsNewBestFloor => newBestFloor;
	public bool IsNewBestScore => newBestScore;
	/// <summary>The play ended with the tower character on its tired animations (few lives left).</summary>
	public bool IsTired => tired;
	/// <summary>The BPM the play ended on, which times the tower character's animations as in play.</summary>
	public double Bpm => bpm;

	private static T At<T>(T[] values, int player) => player >= 0 && player < values.Length ? values[player] : default!;
}
