namespace OpenTaiko;

// The timing-zone rule that turns a hit's distance from a missable, ADLIB or mine note into a judge. A zone is named by
// the judge it gives without mods: Perfect (Good zone), Good (Ok zone) or Poor (Bad zone).
internal static class JudgeWindows {
	// balloons take hits this long before their head, in game ms
	internal const int MsBalloonHeadWindow = 17;

	// what the rule reads for one note and player: zones in chart ms, the Just mod (0 off, 1 Just, 2 Safe) and the note kind
	internal readonly record struct NoteRule(CConfigIni.CTimingZones Zones, int JustMode, bool Missable, bool JudgedFromNearest) {
		internal static NoteRule Of(NotesManager.ENoteType noteType, CConfigIni.CTimingZones zones, int justMode)
			=> new(zones, justMode, NotesManager.IsMissableNote(noteType), NotesManager.IsJudgedFromNearest(noteType));

		// the judge of a hit in a zone: Just turns the Ok zone into Bad on missable notes, and Safe, or an ADLIB or mine,
		// turns the Bad zone into Ok
		internal ENoteJudge JudgeOfZone(ENoteJudge zone) => zone switch {
			ENoteJudge.Good when this.JustMode == 1 && this.Missable => ENoteJudge.Poor, // Just
			ENoteJudge.Poor when this.JustMode == 2 || this.JudgedFromNearest => ENoteJudge.Good, // Safe
			_ => zone,
		};
	}

	// whole-ms distance of a hit from a note, truncated; out of int range it is negative on x64 and int.MaxValue on
	// ARM64, both judged Miss
	internal static int AbsDeltaMs(long msHitTjaTime, double msNoteTjaTime)
		=> (int)Math.Abs(msHitTjaTime - msNoteTjaTime);

	// the zone of a whole-ms distance, Miss past the Bad zone; the bounds are inclusive
	internal static ENoteJudge ZoneOf(int msAbsDelta, CConfigIni.CTimingZones zones)
		=> (msAbsDelta > zones.nBadZone) ? ENoteJudge.Miss
			: (msAbsDelta <= zones.nGoodZone) ? ENoteJudge.Perfect
			: (msAbsDelta <= zones.nOkZone) ? ENoteJudge.Good
			: ENoteJudge.Poor;

	// judge of a whole-ms distance
	internal static ENoteJudge Classify(int msAbsDelta, in NoteRule rule)
		=> rule.JudgeOfZone(ZoneOf(msAbsDelta, rule.Zones));

	// judge of a hit at a whole-ms chart time, as the gameplay judge gives it
	internal static ENoteJudge JudgeAt(long msHitTjaTime, double msNoteTjaTime, in NoteRule rule) {
		int msAbsDelta = AbsDeltaMs(msHitTjaTime, msNoteTjaTime);
		if (msAbsDelta < 0) // too large and overflowed
			return ENoteJudge.Miss;
		if (msAbsDelta == 0)
			return ENoteJudge.Perfect;
		return Classify(msAbsDelta, rule);
	}
}
