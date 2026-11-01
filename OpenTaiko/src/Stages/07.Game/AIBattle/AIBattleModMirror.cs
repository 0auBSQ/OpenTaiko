namespace OpenTaiko;

// AI battle: the AI, in player 2's slot, plays each play with player 1's play modes and seed, so both sides play the same
// chart under the same rules. Player 2's own modes are kept aside and put back when the play ends, so the mirrored ones
// never reach Config.ini. The AI also plays on the normal gauge and without puchichara effects (IsAI).
internal static class AIBattleModMirror {
	internal const int AIPlayer = 1;

	// The per-player modes of the mod select except auto, which the AI always is: scroll speed, game type, timing zones,
	// Just/Safe, stealth, random and fun mod. Scroll speed and stealth only change how the lane looks, but scroll speed
	// is also read by chart expressions, and with both the two lanes look and read alike.
	private sealed record Modes(int ScrollSpeed, EGameType GameType, int TimingZones, int Just, EStealthMode Stealth,
		ERandomMode Random, EFunMods FunMod) {
		internal static Modes Of(CConfigIni cfg, int player) => new(cfg.nScrollSpeed[player], cfg.nGameType[player],
			cfg.nTimingZones[player], cfg.bJust[player], cfg.eSTEALTH[player], cfg.eRandom[player], cfg.nFunMods[player]);

		internal void SetOn(CConfigIni cfg, int player) {
			cfg.nScrollSpeed[player] = this.ScrollSpeed;
			cfg.nGameType[player] = this.GameType;
			cfg.nTimingZones[player] = this.TimingZones;
			cfg.bJust[player] = this.Just;
			cfg.eSTEALTH[player] = this.Stealth;
			cfg.eRandom[player] = this.Random;
			cfg.nFunMods[player] = this.FunMod;
		}
	}

	// player 2's own modes while the AI plays with player 1's, null when nothing is mirrored
	private static Modes? player2Modes;

	// in AI battle, gives the AI player 1's modes and seed for the play; player 2's own modes are kept on the first call
	// only, so a second call before Restore keeps them
	internal static void Apply(CConfigIni cfg, int[] seeds) {
		if (!cfg.bAIBattleMode)
			return;
		player2Modes ??= Modes.Of(cfg, AIPlayer);
		Modes.Of(cfg, 0).SetOn(cfg, AIPlayer);
		seeds[AIPlayer] = seeds[0];
	}

	// puts player 2's own modes back; once per play, after the Dynamic Beat restore and before a watched replay's
	internal static void Restore(CConfigIni cfg) {
		if (player2Modes == null)
			return;
		player2Modes.SetOn(cfg, AIPlayer);
		player2Modes = null;
	}

	// the AI battle's AI
	internal static bool IsAI(CConfigIni cfg, int player) => cfg.bAIBattleMode && player == AIPlayer;

	// the player whose modes and chart draws a player plays with: player 1 for the AI in AI battle
	internal static int ModSourceOf(CConfigIni cfg, int player)
		=> IsAI(cfg, player) ? 0 : player;
}
