using System.Diagnostics;

namespace OpenTaiko;

// AI battle: the AI player's notes are hit at planned times in the timing zones rolled from the AI level's odds
// (AIHitPlanner). The AI plays with player 1's modes in its own slot (AIBattleModMirror), so its notes are judged by the
// usual rules; only a hit whose time could not be put in its zone keeps the zone's judge.
internal abstract partial class CStagePlayScreenCommon {
	private const int AIPlayer = AIBattleModMirror.AIPlayer;
	private AIHitPlanner? aiHitPlanner;

	private AIHitPlanner AIHits => this.aiHitPlanner ??= new AIHitPlanner(new AIHitHost(this), OpenTaiko.Random);

	// the AI battle's AI, whose notes the planner hits
	private static bool IsAIHitPlayer(int nPlayer)
		=> nPlayer == AIPlayer && OpenTaiko.ConfigIni.bAIBattleMode && !(LuaNetworking.Active?.IsRemoteSpot(nPlayer) ?? false);

	// the AI hits only while the play takes autoplay hits from it
	private bool CanAIHit(int nPlayer)
		=> IsAIHitPlayer(nPlayer) && !this.bPAUSE && !this.isDeniedPlaying[nPlayer] && !this.IsStageFailed_Fast();

	// the note waits for its planned hit, so the autoplay hits leave it alone
	private bool IsAIHitPending(CChip chip, int nPlayer)
		=> IsAIHitPlayer(nPlayer) && (this.aiHitPlanner?.IsPending(chip) ?? false);

	// the judge of a hit on one of the AI's notes, the one its rolled zone gives; null for the other players and for
	// rolls, balloons, ADLIB and mines, which keep the judge of their hit time
	private ENoteJudge? AIJudgeOf(CChip chip, int nPlayer)
		=> (IsAIHitPlayer(nPlayer) && NotesManager.IsMissableNote(chip)) ? this.AIHits.JudgeOf(chip) : null;

	// hits the planned notes due before a chart time, so they keep their order with the chips processed at that time
	private void DispatchAIHitsBefore(int nPlayer, double msTjaTime) {
		if (this.CanAIHit(nPlayer))
			this.aiHitPlanner?.DispatchBefore(msTjaTime);
	}

	// hits the planned notes due by now, then plans the upcoming ones
	private void UpdateAIHits(int nPlayer, double msTjaNowTime) {
		if (this.CanAIHit(nPlayer))
			this.AIHits.Update(msTjaNowTime);
	}

	// a branch showed or hid notes, so the pending hits are planned again
	private void ReplanAIHits(int nPlayer) {
		if (IsAIHitPlayer(nPlayer))
			this.aiHitPlanner?.ReplanPending();
	}

	// drops all plans; play start, retry and rewind (tValueInitialize) and leaving the play (DeActivate)
	private void ResetAIHits() => this.aiHitPlanner?.Reset();

	private sealed class AIHitHost(CStagePlayScreenCommon stage) : AIHitPlanner.IHost {
		public List<CChip> Chips => stage.listChip[AIPlayer];
		public CConfigIni.CTimingZones Zones => stage.timingZones[AIPlayer];
		public int JustMode => OpenTaiko.ConfigIni.bJust[AIPlayer];
		public CConfigIni.CAIPerformances Odds => OpenTaiko.ConfigIni.apAIPerformances[OpenTaiko.ConfigIni.nAILevel - 1];
		public int Level => OpenTaiko.ConfigIni.nAILevel;
		public int MsBalloonHeadWindow => (int)CTja.GameDurationToTjaDuration(JudgeWindows.MsBalloonHeadWindow);

		// chart time also runs faster with the Dynamic Beat speed-up, as in tProgressDraw_Chip
		public double MsPlanLead => CTja.GameDurationToTjaDuration(AIHitPlanner.PlanLeadGameMs)
			* ((OpenTaiko.ConfigIni.nFunMods[AIPlayer] == EFunMods.DynamicBeat) ? stage.dbDynamicBeatFactor : 1.0);

		public bool IsNoteIfMet(CChip chip) => stage.IsNoteIfMet(chip, AIPlayer);

		public bool TryHit(CChip chip, double msHitTjaTime) {
			if (stage.AutoplayTryHit(chip, msHitTjaTime, AIPlayer, NotesManager.GetChipGameType(chip, AIPlayer)))
				return true;
			// AIHitTiming.Lands disagrees with the hit search, or the notes changed after the plan
			Trace.TraceWarning($"AI battle: a hit at {msHitTjaTime} ms was refused by the note at {chip.dbSoundTimems} ms");
			return false;
		}
	}
}
