namespace OpenTaiko {
	internal class CTExprVariables {
		public static double ResolveVariable(CTExpression expr, string name, List<string> args) {
			CTja? _chart = OpenTaiko.GetTJA(expr._player);
			if (_chart == null) return 0;

			double value = 0;

			switch (name) {
				case "cd": {
						return _chart.nInstanceDifficulty;
					}
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
