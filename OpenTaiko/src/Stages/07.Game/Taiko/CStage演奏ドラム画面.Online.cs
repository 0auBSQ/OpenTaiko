using FDK;

namespace OpenTaiko;

// ── Online VS end-of-play / results sync (partial of CStage演奏ドラム画面) ───────────────────────────────
// Split out of the main drum-screen file so the online concern stays in one place. The gameplay loop in
// CStage演奏ドラム画面.cs calls these at the finish barrier:
//   • ReportFinished(BuildOnlineFinishJson())  — broadcast THIS client's authoritative final tally;
//   • ApplyOnlineRemoteResults()               — once everyone has reported, overwrite each remote spot's
//                                                 locally-auto-played tally with that player's real counts
//                                                 so the results screen (CStage結果, which reads CChartScore[i])
//                                                 shows their actual great/good/bad, rolls, balloons, adlibs,
//                                                 combo and score — not this machine's simulation of them.
// Nothing here runs offline (every entry guards on the spot being a live remote player).
internal partial class CStage演奏ドラム画面 {
	// guards the one-shot end-anim pass in the finish barrier (see Draw())
	private bool _onlEndAnimsDone = false;

	/// <summary>This client's final result, broadcast on "fn" for the other players' results screens.</summary>
	private string BuildOnlineFinishJson() {
		var cs = this.CChartScore[0];
		bool clear = !this.IsStageFailed(0) && HGaugeMethods.UNSAFE_FastNormaCheck(0);
		bool rainbow = HGaugeMethods.UNSAFE_IsRainbow(0);
		bool fc = clear && cs != null && cs.nMiss == 0;
		bool pf = fc && cs.nGood == 0;
		int gr = cs?.nGreat ?? 0, gd = cs?.nGood ?? 0, ms = cs?.nMiss ?? 0;
		int rl = cs?.nRoll ?? 0, bl = cs?.nBalloon ?? 0, ad = cs?.nADLIB ?? 0, hc = cs?.nHighestCombo ?? 0, sc = (int)this.actScore.GetDisplayedScore(0);
		return string.Format("{{\"cl\":{0},\"fc\":{1},\"pf\":{2},\"mx\":{3},\"gr\":{4},\"gd\":{5},\"ms\":{6},\"rl\":{7},\"bl\":{8},\"ad\":{9},\"hc\":{10},\"sc\":{11}}}",
			clear ? "true" : "false", fc ? "true" : "false", pf ? "true" : "false", rainbow ? "true" : "false", gr, gd, ms, rl, bl, ad, hc, sc);
	}

	/// <summary>Overwrite every remote spot's judge tallies with the values that player broadcast at finish,
	/// so the results screen reflects the real player rather than this client's local auto-play.</summary>
	private void ApplyOnlineRemoteResults() {
		var net = LuaNetworking.Active;
		if (net == null) return;
		for (int i = 1; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
			if (!net.IsRemoteSpot(i)) continue;
			var cs = this.CChartScore[i];
			if (cs == null) continue;
			int gr = net.GetSpotJudge(i, "gr"), gd = net.GetSpotJudge(i, "gd"), ms = net.GetSpotJudge(i, "ms");
			int rl = net.GetSpotJudge(i, "rl"), bl = net.GetSpotJudge(i, "bl"), ad = net.GetSpotJudge(i, "ad"), hc = net.GetSpotJudge(i, "hc"), sc = net.GetSpotJudge(i, "sc");
			if (gr >= 0) cs.nGreat = gr;
			if (gd >= 0) cs.nGood = gd;
			if (ms >= 0) cs.nMiss = ms;
			if (rl >= 0) cs.nRoll = rl;
			if (bl >= 0) cs.nBalloon = bl;
			if (ad >= 0) cs.nADLIB = ad;
			if (hc >= 0) { cs.nHighestCombo = hc; cs.nCombo = hc; }
			if (sc >= 0) this.actScore.Set(sc, i);
		}
	}
}
