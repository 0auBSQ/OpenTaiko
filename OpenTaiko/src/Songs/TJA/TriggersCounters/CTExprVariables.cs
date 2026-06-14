namespace OpenTaiko {
	internal class CTExprVariables {
		public static double ResolveVariable(CTExpression expr, string name, List<string> args) {
			CTja? _chart = OpenTaiko.GetTJA(expr._player);
			if (_chart == null) return 0;

			double value = 0;

			switch (name) {
				// ── Chart main ────────────────────────────────────────────────────────────────────────────────────────────────────
				case "cl": {
						return _chart.PlayerSideMetadata.LEVELtaiko;
					}
				case "cd": {
						return _chart.nInstanceDifficulty;
					}

				// ── Play options ──────────────────────────────────────────────────────────────────────────────────────────────────
				case "pc": {
						return OpenTaiko.ConfigIni.nPlayerCount;
					}
				case "ss": {
						return OpenTaiko.ConfigIni.SongPlaybackSpeed; // 1.0 = normal
					}
				case "sc": {
						if (expr._player >= 0) return OpenTaiko.ConfigIni.nScrollSpeed[expr._player] / 9.0; // 1.0 = default
						break;
					}

				// ── Gameplay — judge counts ────────────────────────────────────────────────────────────────────────────────────────
				case "jp": {
						var gs = OpenTaiko.stageGameScreen;
						if (gs != null && expr._player >= 0) return gs.CChartScore[expr._player].nGreat;
						break;
					}
				case "jg": {
						var gs = OpenTaiko.stageGameScreen;
						if (gs != null && expr._player >= 0) return gs.CChartScore[expr._player].nGood;
						break;
					}
				case "jb": {
						var gs = OpenTaiko.stageGameScreen;
						if (gs != null && expr._player >= 0) return gs.CChartScore[expr._player].nMiss;
						break;
					}
				case "ja": {
						var gs = OpenTaiko.stageGameScreen;
						if (gs != null && expr._player >= 0) return gs.CChartScore[expr._player].nADLIB;
						break;
					}
				case "jm": {
						var gs = OpenTaiko.stageGameScreen;
						if (gs != null && expr._player >= 0) return gs.CChartScore[expr._player].nMine;
						break;
					}

				// ── Gameplay — accuracy / gauge / combo ───────────────────────────────────────────────────────────────────────────
				case "a":
				case "p": {
						var gs = OpenTaiko.stageGameScreen;
						if (gs != null && expr._player >= 0) return gs.CChartScore[expr._player].GetScore(Exam.Type.Accuracy);
						break;
					}
				case "g":
				case "gauge": {
						var gs = OpenTaiko.stageGameScreen;
						if (gs != null && expr._player >= 0) return gs.actGauge.dbCurrentGaugeValue[expr._player];
						break;
					}
				case "cc":
				case "combo": {
						var gs = OpenTaiko.stageGameScreen;
						if (gs != null && expr._player >= 0) return gs.actCombo.nCurrentCombo[expr._player];
						break;
					}
				case "c":
				case "mc": {
						var gs = OpenTaiko.stageGameScreen;
						if (gs != null && expr._player >= 0) return gs.actCombo.nCurrentCombo.MaxValue[expr._player];
						break;
					}

				// ── Gameplay — totals ─────────────────────────────────────────────────────────────────────────────────────────────
				case "tn": {
						var gs = OpenTaiko.stageGameScreen;
						if (gs != null && expr._player >= 0) {
							int branch = (int)gs.nCurrentBranch[expr._player];
							if (branch < _chart.nNotesCount_Branch.Length) return _chart.nNotesCount_Branch[branch];
						}
						break;
					}
				case "ta": {
						return _chart.nTotalAdLib;
					}
				case "tm": {
						return _chart.nTotalMine;
					}

				// ── Gameplay — branch / gamemode ──────────────────────────────────────────────────────────────────────────────────
				case "cb": {
						var gs = OpenTaiko.stageGameScreen;
						if (gs != null && expr._player >= 0) return (int)gs.nCurrentBranch[expr._player];
						break;
					}
				case "cg": {
						return (int?)_chart.PlayerSideMetadata.GameType ?? 0; // 0=Taiko, 1=Konga
					}

				// ── Counters / Triggers ────────────────────────────────────────────────────────────────────────────────────────────
				case "lc": {
						if (args.Count > 0) return _chart.LocalCounters.Get(args[0]);
						break;
					}
				case "lt": {
						if (args.Count > 0) return _chart.LocalTriggers.Get(args[0]) ? 1 : 0;
						break;
					}
				case "gc": {
						if (args.Count > 0) return expr._sfref?.tGetGlobalCounter(args[0]) ?? 0;
						break;
					}
				case "gt": {
						if (args.Count > 0) return (expr._sfref?.tGetGlobalTrigger(args[0]) ?? false) ? 1 : 0;
						break;
					}
			}

			// Insert error notification if necessary here, wrong arg count or unexisting variable
			return value;
		}
	}
}