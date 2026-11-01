namespace OpenTaiko;

// AI battle: rolls each upcoming note's timing zone ahead of its time, picks a hit time in that zone that lands on the
// note (the nearest one that lands when the zone has no room), and hits the note at that time. A note keeps the judge of
// its zone whatever time it is hit at (JudgeOf). Notes it cannot plan are left to the autoplay hits.
internal sealed class AIHitPlanner {
	// what the planner reads from the play and does on it
	internal interface IHost {
		List<CChip> Chips { get; }                    // the AI player's chips, in time order
		CConfigIni.CTimingZones Zones { get; }        // the AI player's timing zones, in chart ms
		int JustMode { get; }                         // the AI player's Just mod
		CConfigIni.CAIPerformances Odds { get; }      // the odds of the current AI level
		int Level { get; }                            // the current AI level
		int MsBalloonHeadWindow { get; }              // balloons take hits this long before their head, in chart ms
		double MsPlanLead { get; }                    // PlanLeadGameMs in chart ms
		bool IsNoteIfMet(CChip chip);
		bool TryHit(CChip chip, double msHitTjaTime); // hits the note as at that time; false when the hit is refused
	}

	// notes are planned this long before their earliest hit time, in game ms
	internal const int PlanLeadGameMs = 250;
	private const int MaxNeighbours = 32;

	// Replan: pending, being planned again as a branch changed the notes shown
	private enum EPlan { Pending, Replan, Done, Dropped, Unplanned }
	private readonly record struct Plan(EPlan State, int IChip, long MsTarget, ENoteJudge Zone);

	private readonly IHost host;
	private readonly Random rng;
	private readonly Dictionary<CChip, Plan> plans = new(System.Collections.Generic.ReferenceEqualityComparer.Instance);
	private readonly List<CChip> queue = new(64); // pending hits, in target order
	private readonly List<CChip> replanned = new(64);
	private readonly AIHitTiming.Neighbour[] nearestBefore = new AIHitTiming.Neighbour[MaxNeighbours];
	private readonly AIHitTiming.Neighbour[] after = new AIHitTiming.Neighbour[MaxNeighbours];
	private int iCursor;      // the first chip that a hit at or after now can still reach
	private int iQueueHead;
	private long msLastTarget = long.MinValue;
	private long msLastDispatched = long.MinValue;
	private double msDispatchedBefore = double.NegativeInfinity; // the hits planned before this chart time are made
	private int iRollScan;    // the first chip not looked at for rolls yet
	private double msRollEnd = double.NegativeInfinity; // the end of the last roll started before iRollScan
	private int rolledLevel = -1; // the AI level the zones ahead were rolled with

	internal int UnplannedCount { get; private set; } // no hit time lands on them; left to the autoplay hits
	internal int RefusedCount { get; private set; }   // planned hits the play refused

	internal AIHitPlanner(IHost host, Random rng) {
		this.host = host;
		this.rng = rng;
	}

	internal bool IsPending(CChip chip)
		=> this.plans.TryGetValue(chip, out var plan) && plan.State is EPlan.Pending;

	// the judge of a hit on the note: the one its zone gives, also when it is hit at another time; a note that was
	// never planned rolls its zone now
	internal ENoteJudge JudgeOf(CChip chip) {
		if (!this.plans.TryGetValue(chip, out var plan)) {
			plan = new Plan(EPlan.Unplanned, -1, 0, this.RollZone());
			this.plans[chip] = plan;
		}
		return JudgeWindows.NoteRule.Of(chip, this.host.Zones, this.host.JustMode).JudgeOfZone(plan.Zone);
	}

	// drops all plans and the chips they reference
	internal void Reset() {
		this.plans.Clear();
		this.queue.Clear();
		this.iCursor = 0;
		this.iQueueHead = 0;
		this.msLastTarget = this.msLastDispatched = long.MinValue;
		this.msDispatchedBefore = double.NegativeInfinity;
		this.iRollScan = 0;
		this.msRollEnd = double.NegativeInfinity;
		this.rolledLevel = -1;
		this.UnplannedCount = this.RefusedCount = 0;
	}

	// hits the notes due by now, rolls the zones ahead again when the AI level changed, then plans the upcoming ones
	internal void Update(double msTjaNowTime) {
		this.DispatchBefore(Math.Floor(msTjaNowTime) + 1);
		if (this.host.Level != this.rolledLevel) {
			this.rolledLevel = this.host.Level;
			this.RerollFrom(msTjaNowTime);
		}
		this.PlanAhead(msTjaNowTime);
	}

	// hits the notes whose target is before the given chart time, in target order
	internal void DispatchBefore(double msTjaTime) {
		this.msDispatchedBefore = Math.Max(this.msDispatchedBefore, msTjaTime);
		for (; this.iQueueHead < this.queue.Count; ++this.iQueueHead) {
			CChip chip = this.queue[this.iQueueHead];
			Plan plan = this.plans[chip];
			if (plan.MsTarget >= msTjaTime)
				break;
			if (!IsHittable(chip) || !this.host.IsNoteIfMet(chip)) {
				// hidden by a branch, already judged, or skipped by #NOTEIF
				this.plans[chip] = plan with { State = EPlan.Dropped };
				continue;
			}
			this.plans[chip] = plan with { State = EPlan.Done };
			this.msLastDispatched = plan.MsTarget;
			if (!this.host.TryHit(chip, plan.MsTarget)) {
				// Left to the autoplay hits, keeping its zone's judge. An early target leaves it to the hit at its time;
				// a late one comes after that hit, which let the note pass for the plan, so it is hit at its time now.
				this.plans[chip] = plan with { State = EPlan.Dropped };
				++this.RefusedCount;
				if (plan.MsTarget >= chip.dbSoundTimems)
					this.host.TryHit(chip, chip.dbSoundTimems);
			}
		}
		if (this.iQueueHead == this.queue.Count) {
			this.queue.Clear();
			this.iQueueHead = 0;
		}
	}

	// A branch showed or hid notes, or the zones ahead were rolled again: the pending hits are planned again at once,
	// against the notes shown now and from the time the hits were made to, so none is pushed past the time the play has
	// reached. A note keeps its zone, and its target while that still lands in the zone.
	internal void ReplanPending() {
		this.iRollScan = 0; // the rolls shown may have changed too
		this.msRollEnd = double.NegativeInfinity;
		if (this.iQueueHead == this.queue.Count)
			return; // nothing pending; notes shown are planned by the next update
		this.replanned.Clear();
		for (int i = this.iQueueHead; i < this.queue.Count; ++i) {
			CChip chip = this.queue[i];
			Plan plan = this.plans[chip];
			this.plans[chip] = plan with { State = EPlan.Replan };
			this.iCursor = Math.Min(this.iCursor, plan.IChip);
			this.replanned.Add(chip);
		}
		this.queue.Clear();
		this.iQueueHead = 0;
		this.msLastTarget = this.msLastDispatched;

		this.PlanAhead(Math.Ceiling(this.msDispatchedBefore) - 1);
		foreach (CChip chip in this.replanned) {
			// hidden now, or out of the planning range: left to the autoplay hits
			if (this.plans[chip] is { State: EPlan.Replan } plan)
				this.plans[chip] = plan with { State = EPlan.Dropped };
		}
		this.replanned.Clear();
	}

	// The AI level changed, as an AI battle section passed: the notes from now on roll their zones again with its odds
	// and their pending hits are planned again; the notes before keep their rolls.
	private void RerollFrom(double msTjaNowTime) {
		var chips = this.host.Chips;
		for (int i = this.iCursor; i < chips.Count; ++i) {
			CChip chip = chips[i];
			if (chip.dbSoundTimems >= msTjaNowTime && this.plans.TryGetValue(chip, out var plan)
				&& plan.State is EPlan.Pending or EPlan.Unplanned)
				this.plans[chip] = plan with { Zone = this.RollZone() };
		}
		this.ReplanPending();
	}

	private static bool IsHittable(CChip chip)
		=> chip.bVisible && !chip.bHit && !chip.IsMissed && chip.eNoteState == ENoteState.None;

	private bool NeedsPlan(CChip chip)
		=> !this.plans.TryGetValue(chip, out var plan) || plan.State is EPlan.Replan;

	private ENoteJudge RollZone() {
		var odds = this.host.Odds;
		return AIHitTiming.RollZone(this.rng.Next(1000), odds.nBadOdds, odds.nGoodOdds);
	}

	private void PlanAhead(double msTjaNowTime) {
		var zones = this.host.Zones;
		var chips = this.host.Chips;
		this.plans.EnsureCapacity(chips.Count); // sized once per chart, not during the notes

		// notes no hit from now on can reach are left to the autoplay hits
		long msNow = (long)Math.Floor(msTjaNowTime);
		while (this.iCursor < chips.Count && chips[this.iCursor].dbSoundTimems <= msNow - zones.nBadZone - 1)
			++this.iCursor;

		double msPlanUntil = msTjaNowTime + zones.nBadZone + this.host.MsPlanLead;
		for (int i = this.iCursor; i < chips.Count && chips[i].dbSoundTimems <= msPlanUntil; ++i) {
			CChip chip = chips[i];
			// a hidden note is looked at again while in range, as a branch can show it
			if (NotesManager.IsMissableNote(chip) && IsHittable(chip) && this.NeedsPlan(chip))
				this.PlanNote(chips, i, msTjaNowTime, zones);
		}
	}

	private AIHitTiming.Neighbour NeighbourOf(CChip chip, CConfigIni.CTimingZones zones)
		=> new(chip.nSoundTimems, chip.dbSoundTimems, JudgeWindows.NoteRule.Of(chip, zones, this.host.JustMode));

	private bool IsPlanned(CChip chip)
		=> this.plans.TryGetValue(chip, out var plan) && plan.State is EPlan.Pending or EPlan.Done;

	// the end time of the last shown roll that starts before the chip
	private double RollEndBefore(List<CChip> chips, int iChip) {
		if (iChip < this.iRollScan) { // a chip behind the scan, shown late: scan again from the start
			this.iRollScan = 0;
			this.msRollEnd = double.NegativeInfinity;
		}
		for (; this.iRollScan < iChip; ++this.iRollScan) {
			CChip c = chips[this.iRollScan];
			if (c.bVisible && NotesManager.IsGenericRoll(c) && !NotesManager.IsRollEnd(c))
				this.msRollEnd = Math.Max(this.msRollEnd, c.end.dbSoundTimems);
		}
		return this.msRollEnd;
	}

	private void PlanNote(List<CChip> chips, int iChip, double msTjaNowTime, CConfigIni.CTimingZones zones) {
		CChip chip = chips[iChip];
		// a note planned again keeps its zone, and its target while that still lands in the zone
		bool isReplan = this.plans.TryGetValue(chip, out var replanned) && replanned.State is EPlan.Replan;
		var zone = isReplan ? replanned.Zone : this.RollZone();
		double msReach = 2.0 * zones.nBadZone + 2; // farther chips cannot be judged at any hit time of this note
		long msLo = Math.Max(this.msLastTarget, (long)Math.Floor(msTjaNowTime));
		long msHi = long.MaxValue;
		int nBefore = 0, nAfter = 0;

		double msRollEnd = this.RollEndBefore(chips, iChip);
		if (msRollEnd > chip.dbSoundTimems) { // the note is inside a roll
			this.Unplan(chip, iChip, zone);
			return;
		}
		if (!double.IsNegativeInfinity(msRollEnd))
			msLo = Math.Max(msLo, (long)Math.Floor(msRollEnd) - 1); // the roll takes hits until its end

		for (int i = iChip; i-- > 0;) {
			CChip c = chips[i];
			if (c.dbSoundTimems < chip.dbSoundTimems - msReach)
				break;
			if (!c.bVisible || !NotesManager.IsHittableNote(c) || NotesManager.IsGenericRoll(c) || c.bHit || c.IsMissed)
				continue;
			if (NotesManager.IsJudgedFromNearest(c)) {
				if (nBefore == MaxNeighbours) {
					this.Unplan(chip, iChip, zone);
					return;
				}
				this.nearestBefore[nBefore++] = this.NeighbourOf(c, zones);
			} else if (!this.IsPlanned(c)) {
				msLo = Math.Max(msLo, (long)Math.Ceiling(c.dbSoundTimems) - 1); // hit at its own time by the autoplay hits
			}
		}

		for (int i = iChip + 1; i < chips.Count; ++i) {
			CChip c = chips[i];
			if (c.dbSoundTimems > chip.dbSoundTimems + msReach)
				break;
			if (!c.bVisible || !NotesManager.IsHittableNote(c) || NotesManager.IsRollEnd(c))
				continue;
			if (NotesManager.IsGenericRoll(c)) {
				long msHeadWindow = NotesManager.IsGenericBalloon(c) ? this.host.MsBalloonHeadWindow : 0;
				msHi = Math.Min(msHi, c.nSoundTimems - msHeadWindow); // the roll takes hits from its head
				break;
			}
			msHi = Math.Min(msHi, c.nSoundTimems);
			if (nAfter == MaxNeighbours) {
				this.Unplan(chip, iChip, zone);
				return;
			}
			this.after[nAfter++] = this.NeighbourOf(c, zones);
		}

		var note = this.NeighbourOf(chip, zones);
		var window = new AIHitTiming.HitWindow(msLo, msHi, AllowEarly: string.IsNullOrEmpty(chip.NoteIfTrigger));
		var before = this.nearestBefore.AsSpan(0, nBefore);
		var next = this.after.AsSpan(0, nAfter);
		bool keepsTarget = isReplan && AIHitTiming.Lands(replanned.MsTarget, note, window, before, next)
			&& AIHitTiming.MsOutsideZone(JudgeWindows.AbsDeltaMs(replanned.MsTarget, note.MsNote), zone, zones) == 0;
		var msHit = keepsTarget ? replanned.MsTarget : AIHitTiming.PickHitTime(note, zone, window, before, next, this.rng);
		if (msHit is not { } msTarget) {
			this.Unplan(chip, iChip, zone);
			return;
		}
		this.plans[chip] = new Plan(EPlan.Pending, iChip, msTarget, zone);
		this.queue.Add(chip);
		this.msLastTarget = msTarget;
	}

	// left to the autoplay hits, which hit it at its time; it keeps its zone
	private void Unplan(CChip chip, int iChip, ENoteJudge zone) {
		this.plans[chip] = new Plan(EPlan.Unplanned, iChip, 0, zone);
		++this.UnplannedCount;
	}
}
