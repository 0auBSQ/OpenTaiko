using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenTaiko.AviUtl2 {
	public enum Aup2Motion {
		None,         // a constant
		Linear,       // 直線移動
		Smooth,       // 補間移動, 曲線移動
		Instant,      // 瞬間移動
		Step,         // 移動量指定
		Repeat,       // 反復移動
		Rotation,     // 回転
		Random,       // ランダム移動
		Accelerate,   // 加減速移動
		PlayRange,    // 再生範囲
		Unknown,
	}

	// A setting's value over an object's life: "start,end,movement,parameters", or one constant.
	// With middle points there is one value per point. The parameters are "flags|setting|curve": the
	// setting is the movement's own number (a period, an angle), the curve is the time control.
	public sealed class Aup2Track {
		public double[] Values { get; private set; } = { 0 };
		public Aup2Motion Motion { get; private set; } = Aup2Motion.None;
		public string MotionName { get; private set; } = "";
		public bool TimeControl { get; private set; }
		public string Param { get; private set; } = "";
		public int Flags { get; private set; }
		public double? Setting { get; private set; }
		public double[] Curve { get; private set; } = Array.Empty<double>();
		// the text of a setting that is not a number
		public string? Text { get; private set; }

		public bool IsAnimated => Motion != Aup2Motion.None;

		private const string TimeControlSuffix = "(時間制御)";

		public static Aup2Track Parse(string? value, double fallback = 0) {
			var t = new Aup2Track { Values = new[] { fallback } };
			if (value == null) return t;
			var parts = value.Split(',');
			var numbers = new List<double>();
			int i = 0;
			for (; i < parts.Length; i++) {
				if (!double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) break;
				numbers.Add(v);
			}
			if (i == parts.Length) {
				// numbers only: the first is the value
				if (numbers.Count > 0) t.Values = new[] { numbers[0] };
				return t;
			}
			if (numbers.Count < 2) {
				t.Text = value;
				return t;
			}
			t.Values = numbers.ToArray();
			string name = parts[i];
			t.MotionName = name;
			if (name.EndsWith(TimeControlSuffix, StringComparison.Ordinal)) {
				t.TimeControl = true;
				name = name[..^TimeControlSuffix.Length];
			}
			t.Motion = name switch {
				"直線移動" => Aup2Motion.Linear,
				"補間移動" => Aup2Motion.Smooth,
				"曲線移動" => Aup2Motion.Smooth,
				"瞬間移動" => Aup2Motion.Instant,
				"移動量指定" => Aup2Motion.Step,
				"反復移動" => Aup2Motion.Repeat,
				"回転" => Aup2Motion.Rotation,
				"ランダム移動" => Aup2Motion.Random,
				"加減速移動" => Aup2Motion.Accelerate,
				"再生範囲" => Aup2Motion.PlayRange,
				_ => Aup2Motion.Unknown,
			};
			t.Param = string.Join(",", parts, i + 1, parts.Length - i - 1);
			var fields = t.Param.Split('|');
			if (fields.Length > 0 && int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int flags)) t.Flags = flags;
			for (int k = 1; k < fields.Length; k++) {
				if (fields[k].Contains(',')) {
					var curve = new List<double>();
					foreach (var c in fields[k].Split(','))
						if (double.TryParse(c, NumberStyles.Float, CultureInfo.InvariantCulture, out double cv)) curve.Add(cv);
					t.Curve = curve.ToArray();
				} else if (double.TryParse(fields[k], NumberStyles.Float, CultureInfo.InvariantCulture, out double setting)) {
					t.Setting = setting;
				}
			}
			return t;
		}

		// true when the time control curve is one this player reads: four numbers for the object or four
		// per section (the two handles), or eight (both ends and both handles, as older versions wrote it)
		// no curve written is the untouched one, a straight line
		public bool CurveKnown(int sections) => Curve.Length == 0 || Curve.Length == 4 || Curve.Length == 8 || (sections > 0 && Curve.Length == 4 * sections);

		// The value at `frame` frames after the object's first frame. `points` are the frames of the object's
		// first frame, of its middle points and of its last frame, counted from the first; `seed` tells the
		// random movement of one setting from another's.
		public double Evaluate(double frame, int[] points, int seed = 0) {
			if (Motion == Aup2Motion.None || Values.Length < 2) return Values[0];
			int sections = Math.Max(1, points.Length - 1);
			double total = Math.Max(1, points[^1] - points[0]);
			double f = Math.Clamp(frame, 0, total);

			// a value per point follows the sections; two values on a longer object span all of it
			bool perSection = Values.Length == sections + 1 && sections > 1;
			int k = 0;
			double start = 0, length = total;
			if (perSection) {
				while (k < sections - 1 && f >= points[k + 1] - points[0]) k++;
				start = points[k] - points[0];
				length = Math.Max(1, points[k + 1] - points[k]);
			}
			double t = Math.Clamp((f - start) / length, 0, 1);
			if (TimeControl) t = Eased(t, perSection ? k : 0, sections);
			double a = Values[perSection ? k : 0], b = Values[perSection ? k + 1 : Values.Length - 1];

			switch (Motion) {
				case Aup2Motion.Instant:
					return a;
				case Aup2Motion.PlayRange:
					// the part of a file that plays: from the first value to the second over the object
					return a + (b - a) * t;
				case Aup2Motion.Smooth:
					return perSection ? Spline(k, t) : a + (b - a) * t;
				case Aup2Motion.Accelerate:
					t = t < 0.5 ? 2 * t * t : 1 - 2 * (1 - t) * (1 - t);
					return a + (b - a) * t;
				case Aup2Motion.Step:
					// the second value is the change per frame
					return Values[0] + Values[1] * f;
				case Aup2Motion.Repeat: {
					// there and back: the setting is the frames of one way
					double period = Setting is > 0 ? Setting.Value : total;
					double phase = f / period;
					double tri = phase % 2.0;
					if (tri > 1) tri = 2 - tri;
					return Values[0] + (Values[^1] - Values[0]) * tri;
				}
				case Aup2Motion.Rotation: {
					// one axis of a turn of `setting` degrees between the two values
					double angle = (Setting ?? 360) * Math.PI / 180 * (f / total);
					return Values[0] + (Values[^1] - Values[0]) * (1 - Math.Cos(angle)) / 2;
				}
				case Aup2Motion.Random: {
					// towards a new value every `setting` frames, in a straight line from the last one
					double every = Setting is > 0 ? Setting.Value : 1;
					long step = (long)Math.Floor(f / every);
					double part = f / every - step;
					double from = Hash01(seed, step), to = Hash01(seed, step + 1);
					return Values[0] + (Values[^1] - Values[0]) * (from + (to - from) * part);
				}
				default:
					return a + (b - a) * t;
			}
		}

		// The time control: a cubic curve from (0,0) to (1,1). Its four numbers are the two handles, each
		// counted from its own end of the curve: (x1,y1) from the start, (x2,y2) back from the end.
		private double Eased(double t, int section, int sections) {
			int at;
			if (Curve.Length == 8 && sections != 2) {
				// start point, its handle, end point, its handle, all in the curve's own square
				double ax = Curve[0], ay = Curve[1], bx = Curve[4], by = Curve[5];
				double span = Math.Max(1e-9, bx - ax);
				double h1 = Math.Clamp((Curve[2] - ax) / span, 0, 1), h2 = Math.Clamp((Curve[6] - ax) / span, 0, 1);
				double target = Math.Clamp((t - ax) / span, 0, 1), a = 0, b = 1;
				for (int i = 0; i < 32; i++) {
					double mid = (a + b) / 2;
					if (Bezier(mid, h1, h2) < target) a = mid; else b = mid;
				}
				double u = (a + b) / 2, v = 1 - u;
				return v * v * v * ay + 3 * v * v * u * Curve[3] + 3 * v * u * u * Curve[7] + u * u * u * by;
			}
			if (Curve.Length == 4) at = 0;
			else if (sections > 0 && Curve.Length == 4 * sections) at = 4 * section;
			else return t;
			double x1 = Math.Clamp(Curve[at], 0, 1), y1 = Curve[at + 1];
			double x2 = Math.Clamp(1 - Curve[at + 2], 0, 1), y2 = 1 - Curve[at + 3];
			double lo = 0, hi = 1;
			for (int i = 0; i < 32; i++) {
				double mid = (lo + hi) / 2;
				if (Bezier(mid, x1, x2) < t) lo = mid; else hi = mid;
			}
			return Bezier((lo + hi) / 2, y1, y2);
		}

		private static double Bezier(double u, double p1, double p2) {
			double v = 1 - u;
			return 3 * v * v * u * p1 + 3 * v * u * u * p2 + u * u * u;
		}

		// a smooth curve through every point's value
		private double Spline(int k, double t) {
			double p1 = Values[k], p2 = Values[k + 1];
			double p0 = k > 0 ? Values[k - 1] : p1 - (p2 - p1);
			double p3 = k + 2 < Values.Length ? Values[k + 2] : p2 + (p2 - p1);
			double t2 = t * t, t3 = t2 * t;
			return 0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
		}

		private static double Hash01(int seed, long step) {
			unchecked {
				ulong x = (ulong)step * 0x9E3779B97F4A7C15UL + (ulong)(uint)seed * 0xC2B2AE3D27D4EB4FUL + 0x165667B19E3779F9UL;
				x ^= x >> 31; x *= 0xD6E8FEB86659FD93UL; x ^= x >> 32;
				return (x >> 11) / (double)(1UL << 53);
			}
		}
	}
}
