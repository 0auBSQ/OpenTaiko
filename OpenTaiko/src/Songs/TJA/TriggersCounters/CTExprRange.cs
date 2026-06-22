namespace OpenTaiko;

public enum CTExprRange {
	MoreOrEqual, // >= (default)
	Less,        // <
	Equal,       // ==
	NotEqual,    // !=
	LessOrEqual, // <=
	More,        // >
}

internal static class CTExprRangeHelper {
	public static bool Compare(double val, double threshold, CTExprRange op) => op switch {
		CTExprRange.MoreOrEqual => val >= threshold,
		CTExprRange.Less        => val <  threshold,
		CTExprRange.Equal       => val == threshold,
		CTExprRange.NotEqual    => val != threshold,
		CTExprRange.LessOrEqual => val <= threshold,
		CTExprRange.More        => val >  threshold,
		_ => val >= threshold,
	};
}
