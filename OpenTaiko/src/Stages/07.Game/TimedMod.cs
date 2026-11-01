namespace OpenTaiko;

// The Timed special mod for one player: a timer that runs while notes are near and fails the player when it runs out.
// Evaluations of the player's judgements give time back. Every time is in chart milliseconds.
internal sealed class TimedMod {
	// the timer counts up to this
	public const int MsLimit = 25000;
	// the timer runs when a note is due within the first range, and stops when none is within the second
	public const int MsNotesNear = 2000;
	public const int MsNotesAhead = 5000;

	private const int MsEvaluateFrom = 20000;
	private const int MsBonusFrom = 24000;
	private const int MsBetweenEvaluations = 11000;
	private const int MsJudging = 2000;
	private const int MsShowAdded = 1000;
	private const int MaxBonuses = 4;
	private const double SecondsForBreak = 15;

	internal readonly record struct Row(double Bound, double Seconds);

	internal sealed record Tables(Row[] Accuracy, Row[] MinGap, Row[] MaxGap, Row[] Combo, Row[] Miss,
		Row[] TotalAccuracy, Row[] TotalMaxGap, Row[] TotalCombo, Row[] TotalMiss, bool Strict);

	// the player's counts over the whole play
	internal readonly record struct Totals(int PerfectAndOk, int Misses, int MaxCombo);

	// the judgements since the last evaluation; a gap is the distance of a hit from its note, in ms
	internal struct Section {
		public int Perfect, Great, Good, Poor, Miss, Notes, Combo, MaxCombo;
		public int MinGap = -1, MaxGap = -1;
		public Section() { }
	}

	private const double Beyond = double.PositiveInfinity;

	// Timed: the last row of a table catches every worse value
	private static readonly Tables Normal = new(
		Accuracy: [new(90, 5), new(70, 4.5), new(60, 3), new(50, 1), new(30, 0)],
		MinGap: [new(5, 4), new(10, 3.5), new(20, 3), new(50, 1.5), new(80, -1), new(Beyond, -1)],
		MaxGap: [new(50, 2), new(60, 1.5), new(80, 0.5), new(90, 0), new(100, -1), new(Beyond, -1)],
		Combo: [new(98, 3.5), new(80, 1), new(50, 0.5), new(35, -1.5), new(0, -1.5)],
		Miss: [new(0, 2), new(20, 1), new(50, -0.5), new(Beyond, -0.5)],
		TotalAccuracy: [new(90, 3.5), new(70, 2.5), new(60, 1), new(50, 0.5), new(30, -0.5), new(0, -0.5)],
		TotalMaxGap: [new(50, 4.2), new(60, 3.6), new(80, 2), new(90, -0.5), new(100, -1), new(Beyond, -1)],
		TotalCombo: [new(98, 3), new(80, 2.5), new(50, 0.5)],
		TotalMiss: [new(0, 2), new(20, 1.5), new(50, 0.5), new(70, -0.5), new(Beyond, -0.5)],
		Strict: false);

	// Timed (Hard)
	private static readonly Tables Hard = new(
		Accuracy: [new(100, 3), new(95, 2), new(90, 1), new(70, -2), new(50, -4), new(0, -10)],
		MinGap: [new(0, 2), new(3, 1), new(5, -2), new(10, -3), new(30, -3), new(108, -4)],
		MaxGap: [new(3, 3.5), new(10, 2), new(15, 1), new(20, 0), new(50, -2), new(108, -5)],
		Combo: [new(100, 1), new(50, 0.5), new(0, -5)],
		Miss: [new(0, 1), new(100, -5)],
		TotalAccuracy: [new(100, 5), new(99, 4), new(90, 1.5), new(80, 1), new(50, -1), new(30, -3), new(0, -4.5)],
		TotalMaxGap: [new(20, 3), new(30, 1.5), new(50, 1), new(80, 0), new(108, -2.5)],
		TotalCombo: [new(100, 1), new(0, -2)],
		TotalMiss: [new(0, 1), new(100, -2)],
		Strict: false);

	// Timed (Hard) on a chart of level 10 or more
	private static readonly Tables HardTopLevel = Hard with {
		Accuracy = [new(100, 3), new(95, 2), new(88, 1), new(80, -3), new(50, -6), new(0, -10)],
		MaxGap = [new(2, 4), new(10, 1), new(30, 0), new(50, -1), new(70, -3), new(108, -5)],
		Combo = [new(100, 1), new(0, -6)],
		Miss = [new(0, 1), new(100, -6)],
		TotalMaxGap = [new(20, 3), new(60, 1), new(108, -5)],
		TotalCombo = [new(100, 1), new(0, -5)],
		TotalMiss = [new(0, 1), new(100, -5)],
		Strict = true,
	};

	internal static Tables TablesFor(bool hard, int level)
		=> !hard ? Normal : (level >= 10) ? HardTopLevel : Hard;

	// the seconds of the first row the value reaches: a row is reached from its bound up when higher is better, from
	// its bound down otherwise; no row gives nothing
	internal static double Lookup(Row[] table, double value, bool higherIsBetter) {
		foreach (Row row in table) {
			if (higherIsBetter ? value >= row.Bound : value <= row.Bound)
				return row.Seconds;
		}
		return 0;
	}

	private static double Percent(int count, int of) => (of > 0) ? 100.0 * count / of : double.NaN;

	// the seconds a regular evaluation gives back
	internal static double RegularSeconds(Tables tables, in Section section, int passedNotes, int totalMaxGap, Totals totals) {
		if (section.Notes == 0)
			return SecondsForBreak;

		double seconds = 0;
		if (section.Perfect != 0 || section.Great != 0)
			seconds += Lookup(tables.Accuracy, Percent(section.Perfect + section.Great, section.Notes), true);
		if (section.MinGap != -1)
			seconds += Lookup(tables.MinGap, section.MinGap, false);
		if (section.MaxGap != -1)
			seconds += Lookup(tables.MaxGap, section.MaxGap, false);
		if (section.MaxCombo != 0)
			seconds += Lookup(tables.Combo, Percent(section.MaxCombo, section.Notes), true);
		seconds += Lookup(tables.Miss, Percent(section.Poor + section.Miss, section.Notes), false);

		if (totals.PerfectAndOk != 0)
			seconds += Lookup(tables.TotalAccuracy, Percent(totals.PerfectAndOk, passedNotes), true);
		if (totalMaxGap != -1)
			seconds += Lookup(tables.TotalMaxGap, totalMaxGap, false);
		if (totals.MaxCombo != 0)
			seconds += Lookup(tables.TotalCombo, Percent(totals.MaxCombo, passedNotes), true);
		seconds += Lookup(tables.TotalMiss, Percent(totals.Misses, passedNotes), false);

		return Math.Max(0, seconds);
	}

	// the seconds the last-second bonus gives back
	internal static double BonusSeconds(Tables tables, in Section section) {
		double seconds = 0;
		if ((section.Perfect != 0 || section.Great != 0)
			&& Percent(section.Perfect + section.Great, section.Notes) >= (tables.Strict ? 95.0 : 98.0))
			seconds += 6;
		if (section.MinGap != -1 && section.MinGap <= 5)
			seconds += 6;
		if (section.MaxGap != -1 && section.MaxGap <= 30)
			seconds += 6;
		if (Percent(section.Poor + section.Miss, section.Notes) >= 5.0)
			seconds -= 2;
		return Math.Max(0, seconds);
	}

	// the number the timer shows
	internal static int DisplayedSeconds(int msElapsed)
		=> (msElapsed < 1000) ? 25 : (msElapsed >= MsLimit) ? 0 : (26000 - msElapsed) / 1000;

	private readonly Tables tables;
	private readonly Func<Totals> totals;
	private Section section = new();
	private int passedNotes;
	private int totalMaxGap = -1;
	private int bonusCount;
	private bool bonusTried;
	private long msLastEvaluation;
	private long? msClock;
	private int msJudgingLeft;
	private int msAddedLeft;
	private int addedSeconds;

	public TimedMod(Tables tables, Func<Totals> totals) {
		this.tables = tables;
		this.totals = totals;
	}

	// below 0 when more time was given back than the timer had counted
	public int MsElapsed { get; private set; }
	// the first note was reached: the timer may run
	public bool IsStarted { get; private set; }
	public bool IsRunning { get; private set; }
	// held after an extension, with its digits hidden
	public bool IsJudging { get; private set; }
	public bool IsTimeUp => this.MsElapsed >= MsLimit;
	// the seconds of the last extension while they show, else null
	public int? AddedSeconds => (this.msAddedLeft > 0) ? this.addedSeconds : null;

	public void Start() => this.IsStarted = true;

	// moves the clock to a chart time; a time before the clock changes nothing
	public void Advance(long msNow) {
		long msBefore = this.msClock ?? msNow;
		this.msClock = Math.Max(msNow, msBefore);
		int ms = (int)Math.Min(this.msClock.Value - msBefore, MsLimit);

		if (this.IsJudging) {
			this.msJudgingLeft -= ms;
			if (this.msJudgingLeft <= 0) {
				this.IsJudging = false;
				this.msAddedLeft = MsShowAdded;
			}
		} else {
			this.msAddedLeft = Math.Max(0, this.msAddedLeft - ms);
			if (this.IsRunning)
				this.MsElapsed = Math.Min(MsLimit, this.MsElapsed + ms);
		}

		if (this.MsElapsed >= MsEvaluateFrom && !this.IsTimeUp)
			this.Evaluate(this.msClock.Value);
	}

	// starts and stops the timer from the notes still to play
	public void SetNotes(bool anyNear, bool anyAhead) {
		if (this.IsRunning && (!anyAhead || this.IsJudging))
			this.IsRunning = false;
		if (!this.IsRunning && !this.IsJudging && this.IsStarted && anyNear)
			this.IsRunning = true;
	}

	// counts a judged note, at its chart time
	public void Judge(ENoteJudge judge, int msLag, long msNow) {
		this.Advance(msNow);
		this.IsStarted = true;
		this.section.Notes++;
		this.passedNotes++;
		switch (judge) {
			case ENoteJudge.Perfect:
				this.section.Perfect++;
				break;
			case ENoteJudge.Great:
				this.section.Great++;
				break;
			case ENoteJudge.Good:
				this.section.Good++;
				break;
			case ENoteJudge.Poor:
				this.section.Poor++;
				break;
			case ENoteJudge.Miss:
				this.section.Miss++;
				break;
		}
		if (judge is ENoteJudge.Perfect or ENoteJudge.Great or ENoteJudge.Good) {
			this.section.Combo++;
			this.section.MaxCombo = Math.Max(this.section.MaxCombo, this.section.Combo);
			int gap = Math.Abs(msLag);
			this.section.MaxGap = Math.Max(this.section.MaxGap, gap);
			this.totalMaxGap = Math.Max(this.totalMaxGap, gap);
			this.section.MinGap = (this.section.MinGap == -1) ? gap : Math.Min(this.section.MinGap, gap);
		} else {
			this.section.Combo = 0;
		}
	}

	private void Evaluate(long msNow) {
		if (this.msLastEvaluation + MsBetweenEvaluations <= msNow) {
			double seconds = RegularSeconds(this.tables, this.section, this.passedNotes, this.totalMaxGap, this.totals());
			this.msLastEvaluation = msNow;
			this.bonusTried = false;
			this.section = new();
			this.Extend(seconds);
		} else if (this.MsElapsed >= MsBonusFrom) {
			// one try between two regular evaluations
			if (this.bonusCount >= MaxBonuses || this.bonusTried)
				return;
			this.bonusTried = true;
			if (this.tables.Strict && this.section.Poor + this.section.Miss > 0)
				return;
			this.bonusCount++;
			double seconds = BonusSeconds(this.tables, this.section);
			this.msLastEvaluation = msNow;
			this.section = new();
			if (seconds > 5)
				this.Extend(seconds);
		}
	}

	private void Extend(double seconds) {
		if (seconds <= 0)
			return;
		this.addedSeconds = (int)seconds;
		this.msAddedLeft = 0;
		this.IsJudging = true;
		this.msJudgingLeft = MsJudging;
		this.MsElapsed -= (int)(seconds * 1000);
	}
}
