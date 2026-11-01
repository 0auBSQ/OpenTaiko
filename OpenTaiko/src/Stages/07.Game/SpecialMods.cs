namespace OpenTaiko;

// The special mods of the mod select: one choice per player among None, Auto, Flawless, Timed and Timed (Hard).
internal static class SpecialMods {
	// The mod this machine judges a player under. Autoplay, the AI battle's AI and an online remote spot are never
	// judged under Flawless or Timed, and nobody is in training mode. A remote player's own machine judges them.
	internal static ESpecialMod Judged(CConfigIni cfg, int player, bool isRemoteSpot) {
		if (cfg.bTokkunMode || isRemoteSpot || AIBattleModMirror.IsAI(cfg, player))
			return ESpecialMod.None;
		ESpecialMod mod = cfg.GetSpecialMod(player);
		return (mod == ESpecialMod.Auto) ? ESpecialMod.None : mod;
	}

	internal static bool IsTimed(ESpecialMod mod) => mod is ESpecialMod.Timed or ESpecialMod.TimedHard;
}

// Flawless: a player's first miss or mine hit fails them. One state per player.
internal sealed class FlawlessState {
	private readonly bool[] failed = new bool[OpenTaiko.MAX_PLAYERS];

	public void Reset(int player) => this.failed[player] = false;

	// a miss or a mine hit of a player judged under the given mod
	public void OnMiss(int player, ESpecialMod judgedMod) {
		if (judgedMod == ESpecialMod.Flawless)
			this.failed[player] = true;
	}

	public bool IsFailed(int player) => this.failed[player];
}
