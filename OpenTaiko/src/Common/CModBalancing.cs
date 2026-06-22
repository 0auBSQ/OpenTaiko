namespace OpenTaiko;

// Extracted from CActPlayOption — mod multiplier helpers that read directly from ConfigIni.
internal static class CModBalancing {
	public enum EBalancingType { SCORE, COINS }

	public static float tGetSongSpeedFactor(EBalancingType ebt, int player = 0) {
		var _compare = Math.Min(2.0, OpenTaiko.ConfigIni.nSongSpeed / 20f);
		if (ebt == EBalancingType.SCORE || _compare <= 1f)
			return Math.Min(1f, (float)Math.Pow(_compare, 1.3));
		return Math.Max(1f, (float)Math.Pow(_compare, 0.7));
	}

	public static float tGetJustFactor(EBalancingType ebt, int player = 0) {
		var _compare = OpenTaiko.ConfigIni.bJust[player];
		if (ebt == EBalancingType.SCORE)
			return (_compare == 2) ? 0.6f : 1f;
		return (_compare > 0) ? ((_compare > 1) ? 0.5f : 1.3f) : 1f;
	}

	public static float tGetTimingFactor(EBalancingType ebt, int player = 0) {
		var _compare = OpenTaiko.ConfigIni.nTimingZones[player] - 2;
		if (ebt == EBalancingType.SCORE)
			return (_compare < 0) ? (1f + 0.2f * _compare) : 1f;
		return 1f + 0.2f * _compare;
	}

	public static float tGetDoronFactor(EBalancingType ebt, int player = 0) {
		var _compare = (int)OpenTaiko.ConfigIni.eSTEALTH[player];
		if (ebt == EBalancingType.SCORE || _compare == 0) return 1f;
		return 1f + 0.1f * (float)Math.Pow(_compare, 2);
	}

	public static float tGetModMultiplier(EBalancingType ebt, int player = 0) {
		float factor = 1f;
		factor *= tGetSongSpeedFactor(ebt, player);
		factor *= tGetJustFactor(ebt, player);
		factor *= tGetTimingFactor(ebt, player);
		factor *= tGetDoronFactor(ebt, player);
		return ebt == EBalancingType.SCORE ? Math.Min(factor, 1f) : factor;
	}
}
