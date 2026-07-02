using System.Collections.Generic;

namespace OpenTaiko {
	// Lua global REPLAY: list a chart's saved replays and start watching one (used by the best-plays list in
	// single-player difficulty select).
	class LuaReplayFunc {
		// Regular replays for a chart+difficulty, ranked by score, capped to topN. Returns a C# ARRAY (not a List)
		// so Lua can use the proven array pattern: list.Length + list[i] (0-based). Each row exposes (read in Lua):
		// .Score .PlayerName .ClearStatus .ScoreRank .ModFlags .Date .FilePath .Watchable .ChartDifficulty
		public CSongReplay.ReplayHeader[] ListReplays(string songFolder, string uniqueId, int difficulty, int topN) {
			return CSongReplay.tListReplays(songFolder ?? "", uniqueId ?? "", difficulty, topN).ToArray();
		}

		// Load a replay and arm replay-playback for the next play. Returns false if it can't be watched (load
		// failure or unreproducible RNG mods). Applies the replay's mods in memory only (restored after the play).
		public bool Watch(string filepath) {
			if (string.IsNullOrEmpty(filepath)) return false;
			var rep = new CSongReplay();
			rep.tLoadReplayFile(filepath);
			if (rep.Inputs == null || rep.Inputs.Count == 0) return false;
			if (!CSongReplay.tIsReplayWatchable(rep.ModFlags, rep.RandomSeed)) return false;
			rep.tApplyVirtualMods();              // snapshot the real mods + apply the replay's, and set ReplaySeed[0]
			OpenTaiko.PendingReplay = rep;
			OpenTaiko.ReplayWatchArmed = true;    // song loading consumes this → sets bReplayMode for exactly this play
			return true;
		}
	}
}
