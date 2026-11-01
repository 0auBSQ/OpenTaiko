using CoreGraphics;
using UIKit;

namespace OpenTaiko.iOS;

/// <summary>
/// Player drawn shapes for the touch drum.
/// </summary>
internal static class TouchDrumShape {
	/// <summary>The Don accent color used by the overlay and the editor, at the given alpha.</summary>
	public static UIColor Tint(byte alpha) => UIColor.FromRGBA((byte)0xFF, (byte)0x44, (byte)0x44, alpha);

	// One stroke per line: "x,y x,y x,y ..." with 0..1 coordinates.
	private static string FilePath =>
		Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TouchDrumShape.txt");

	public static List<List<CGPoint>> Strokes { get; private set; } = new();

	public static bool HasCustomShape => Strokes.Count > 0;

	public static void Load() {
		var strokes = new List<List<CGPoint>>();
		try {
			if (File.Exists(FilePath)) {
				foreach (string line in File.ReadAllLines(FilePath)) {
					var points = new List<CGPoint>();
					foreach (string pair in line.Split(' ', StringSplitOptions.RemoveEmptyEntries)) {
						string[] xy = pair.Split(',');
						if (xy.Length == 2
								&& float.TryParse(xy[0], System.Globalization.CultureInfo.InvariantCulture, out float x)
								&& float.TryParse(xy[1], System.Globalization.CultureInfo.InvariantCulture, out float y))
							points.Add(new CGPoint(x, y));
					}
					if (points.Count >= 3)
						strokes.Add(points);
				}
			}
		} catch (Exception e) {
			System.Diagnostics.Trace.TraceWarning($"TouchDrumShape: failed to load ({e.Message}); using the default circle.");
			strokes.Clear();
		}
		Strokes = strokes;
	}

	/// <summary>Saves the strokes (empty list reverts to the built-in circle).</summary>
	public static void Save(List<List<CGPoint>> strokes) {
		Strokes = strokes.Where(s => s.Count >= 3).ToList();
		try {
			if (Strokes.Count == 0) {
				File.Delete(FilePath);
				return;
			}
			var lines = Strokes.Select(s => string.Join(' ',
				s.Select(p => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{p.X:F4},{p.Y:F4}"))));
			File.WriteAllLines(FilePath, lines);
		} catch (Exception e) {
			System.Diagnostics.Trace.TraceWarning($"TouchDrumShape: failed to save ({e.Message}).");
		}
	}

	/// <summary>
	/// Builds one path for the polygons (in the caller's coordinate space). On iOS 16+, overlapping
	/// polygons are unioned into a single merged shape. Older systems fall back to one path with
	/// multiple subpaths.
	/// </summary>
	public static CGPath? BuildUnionPath(IEnumerable<IReadOnlyList<CGPoint>> polygons) {
		bool canUnion = OperatingSystem.IsIOSVersionAtLeast(16);
		CGPath? result = null;
		double baseSign = 0;
		foreach (var poly in polygons) {
			if (poly.Count < 3)
				continue;
			IReadOnlyList<CGPoint> pts = poly;
			if (!canUnion) {
				double sign = Math.Sign(SignedArea(poly));
				if (baseSign == 0) baseSign = sign;
				else if (sign != 0 && sign != baseSign) pts = poly.Reverse().ToList();
			}
			var bp = new UIBezierPath();
			bp.MoveTo(pts[0]);
			for (int i = 1; i < pts.Count; i++) bp.AddLineTo(pts[i]);
			bp.ClosePath();
			CGPath p = bp.CGPath!;
			if (result == null)
				result = p;
			else if (canUnion)
				result = result.CreateByUnioningPath(p, false) ?? result;
			else {
				var merged = new CGPath();
				merged.AddPath(result);
				merged.AddPath(p);
				result = merged;
			}
		}
		return result;
	}

	private static double SignedArea(IReadOnlyList<CGPoint> points) {
		double area = 0;
		for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
			area += (double)(points[j].X * points[i].Y - points[i].X * points[j].Y);
		return area / 2;
	}

	/// <summary>True when the normalized point lies inside any stroke.</summary>
	public static bool HitTest(double nx, double ny) {
		foreach (var stroke in Strokes) {
			bool inside = false;
			for (int i = 0, j = stroke.Count - 1; i < stroke.Count; j = i++) {
				double xi = stroke[i].X, yi = stroke[i].Y;
				double xj = stroke[j].X, yj = stroke[j].Y;
				if ((yi > ny) != (yj > ny) && nx < (xj - xi) * (ny - yi) / (yj - yi) + xi)
					inside = !inside;
			}
			if (inside)
				return true;
		}
		return false;
	}
}
