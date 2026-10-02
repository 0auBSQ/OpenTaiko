namespace OpenTaiko;

// AI battle: the timing zone rolled for a note from the AI level's odds, and the hit time picked for it. A zone is named
// by the judge it gives without mods (JudgeWindows).
internal static class AIHitTiming {
	// a note a hit can reach: its chart time (whole ms and exact) and its judge rule
	internal readonly record struct Neighbour(int MsNoteInt, double MsNote, JudgeWindows.NoteRule Rule);

	// hit times allowed by the notes around: strictly between the two bounds, and not before the note unless early is allowed
	internal readonly record struct HitWindow(long LoExclusive, long HiExclusive, bool AllowEarly);

	// per-mille dice: below the Bad odds is the Bad zone, the next Ok odds are the Ok zone, the rest is the Good zone
	internal static ENoteJudge RollZone(int dice1000, int badOdds, int okOdds)
		=> (dice1000 < badOdds) ? ENoteJudge.Poor
			: (dice1000 - badOdds < okOdds) ? ENoteJudge.Good
			: ENoteJudge.Perfect;

	// Whether a hit at msHit is judged on the note, rather than missing it or going to a neighbour, following the note
	// search of the gameplay hit (GetChipToJudgeIgnoringRollBody). nearestBefore: unhit ADLIB and mines before the note.
	// after: the notes after it in time order, up to the first roll.
	internal static bool Lands(long msHit, in Neighbour note, in HitWindow window,
		ReadOnlySpan<Neighbour> nearestBefore, ReadOnlySpan<Neighbour> after) {
		if (msHit <= window.LoExclusive || msHit >= window.HiExclusive)
			return false;
		bool isLate = note.MsNoteInt <= msHit; // the search counts the note as past
		if (!isLate && !window.AllowEarly)
			return false;
		var judge = JudgeWindows.JudgeAt(msHit, note.MsNote, note.Rule);
		if (judge is ENoteJudge.Miss)
			return false;

		foreach (ref readonly var nearest in nearestBefore) {
			if (JudgeWindows.JudgeAt(msHit, nearest.MsNote, nearest.Rule) is ENoteJudge.Miss)
				continue;
			// a later ADLIB or mine is found first; a Bad note gives way to an ADLIB or mine
			if (nearest.MsNoteInt > msHit || judge is ENoteJudge.Poor)
				return false;
			// an early hit goes to the nearer of the note and a past ADLIB or mine
			if (!isLate && Math.Abs(msHit - note.MsNoteInt) >= Math.Abs(msHit - nearest.MsNoteInt))
				return false;
		}

		// A late Bad gives way to the next note unless that note is Bad too. The search stops at the first next note
		// that takes the pad; which pad a next note takes is not known here, so the notes after a Bad one are checked
		// too, and an ADLIB or mine among them refuses the hit.
		if (isLate && judge is ENoteJudge.Poor) {
			foreach (ref readonly var next in after) {
				var nextJudge = JudgeWindows.JudgeAt(msHit, next.MsNote, next.Rule);
				if (nextJudge is ENoteJudge.Miss)
					break; // the search stops here
				if (nextJudge is not ENoteJudge.Poor || next.Rule.JudgedFromNearest)
					return false;
			}
		}
		return true;
	}

	// how far a whole-ms distance is outside a zone, 0 inside it
	internal static int MsOutsideZone(int msAbsDelta, ENoteJudge zone, CConfigIni.CTimingZones zones) {
		var (msLo, msHi) = zone switch {
			ENoteJudge.Perfect => (0, zones.nGoodZone),
			ENoteJudge.Good => (zones.nGoodZone + 1, zones.nOkZone),
			_ => (zones.nOkZone + 1, zones.nBadZone),
		};
		return (msAbsDelta < msLo) ? msLo - msAbsDelta : (msAbsDelta > msHi) ? msAbsDelta - msHi : 0;
	}

	// A whole-ms hit time that lands on the note, drawn uniformly among those nearest to the zone: inside it when the
	// note has room for it. Null when no hit time lands on the note.
	internal static long? PickHitTime(in Neighbour note, ENoteJudge zone, in HitWindow window,
		ReadOnlySpan<Neighbour> nearestBefore, ReadOnlySpan<Neighbour> after, Random rng) {
		var zones = note.Rule.Zones;
		long msFirst = (long)Math.Floor(note.MsNote) - zones.nBadZone - 1;
		long msLast = (long)Math.Ceiling(note.MsNote) + zones.nBadZone + 1;

		int msNearest = int.MaxValue;
		int count = 0;
		for (long t = msFirst; t <= msLast; ++t) {
			if (!Lands(t, note, window, nearestBefore, after))
				continue;
			int ms = MsOutsideZone(JudgeWindows.AbsDeltaMs(t, note.MsNote), zone, zones);
			if (ms < msNearest)
				(msNearest, count) = (ms, 0);
			if (ms == msNearest)
				++count;
		}
		if (count == 0)
			return null;

		int pick = rng.Next(count);
		for (long t = msFirst; t <= msLast; ++t) {
			if (Lands(t, note, window, nearestBefore, after)
				&& MsOutsideZone(JudgeWindows.AbsDeltaMs(t, note.MsNote), zone, zones) == msNearest
				&& pick-- == 0)
				return t;
		}
		return null;
	}
}
