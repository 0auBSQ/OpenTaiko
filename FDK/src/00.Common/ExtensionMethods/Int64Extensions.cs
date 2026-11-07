namespace FDK.ExtensionMethods;

public static class Int64Extensions {
	public static long SaturatingAdd(this long x, long y) { // signed saturating addition
		unchecked {
			long res = x + y;
			bool isOverflowed = (x >= 0) == (y >= 0) && (x >= 0) != (res >= 0);
			return !isOverflowed ? res
				: (x >= 0) ? long.MaxValue : long.MinValue;
		}
	}

	public static long SaturatingSubBy(this long x, long y) { // signed saturating subtraction
		unchecked {
			long res = x - y;
			bool isOverflowed = (x >= 0) != (y >= 0) && (x >= 0) != (res >= 0);
			return !isOverflowed ? res
				: (x >= 0) ? long.MaxValue : long.MinValue;
		}
	}

	public static ulong AbsDiffTo(this long x, long y) {
		unchecked {
			return (ulong)((x >= y) ? x - y : y - x);
		}
	}
}
