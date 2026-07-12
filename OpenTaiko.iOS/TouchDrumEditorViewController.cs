using CoreAnimation;
using CoreGraphics;
using Foundation;
using UIKit;

namespace OpenTaiko.iOS;

/// <summary>
/// Fullscreen editor for the touch drum shapes, creating a closed ploygon from each stroke.
/// Touches inside any polygon is recognized as don, everywhere else is ka. Saving with no strokes
/// reverts to the built-in circle.
/// </summary>
internal sealed class TouchDrumEditorViewController : UIViewController {
	internal const double ScreenScale = 0.72;
	internal const double ScreenCenterYFraction = 0.55;

	private const int MaxStrokes = 8;
	private const double MinPointDistance = 6.0;
	private const double MinAreaFraction = 0.002;

	private readonly Action<bool> _onDone;
	private readonly List<List<CGPoint>> _strokes = new();
	private List<CGPoint>? _active;
	private UITouch? _activeTouch;
	private CAShapeLayer? _committedLayer;
	private CAShapeLayer? _activeLayer;
	private readonly List<(UILabel view, Action action)> _buttons = new();
	private UILabel? _hint;
	private bool _built;
	// The actual screen, drawn scaled down so strokes can extend past its edges.
	private CGRect _screenRect;

	public TouchDrumEditorViewController(Action<bool> onDone) {
		_onDone = onDone;
		ModalPresentationStyle = UIModalPresentationStyle.OverFullScreen;
		// Fade in over the game view, which shrinks to the screen rectangle in parallel.
		ModalTransitionStyle = UIModalTransitionStyle.CrossDissolve;
	}

	public override bool PrefersHomeIndicatorAutoHidden => false;
	public override UIRectEdge PreferredScreenEdgesDeferringSystemGestures => UIRectEdge.Bottom;

	public override void ViewDidAppear(bool animated) {
		base.ViewDidAppear(animated);
		SetNeedsUpdateOfHomeIndicatorAutoHidden();
		SetNeedsUpdateOfScreenEdgesDeferringSystemGestures();
	}

	public override void ViewDidLayoutSubviews() {
		base.ViewDidLayoutSubviews();
		if (_built) return;
		_built = true;

		View!.BackgroundColor = UIColor.Clear;
		var b = View.Bounds;

		// Scaled-down screen with drawing space around it. The live game view is scaled to match
		// behind this controller, so the rectangle shows the actual game.
		_screenRect = new CGRect(
			b.Width * (1 - ScreenScale) / 2,
			b.Height * ScreenCenterYFraction - b.Height * ScreenScale / 2,
			b.Width * ScreenScale,
			b.Height * ScreenScale);

		View.Layer.AddSublayer(new CAShapeLayer {
			Path = UIBezierPath.FromRect(_screenRect).CGPath,
			FillColor = UIColor.Clear.CGColor,
			StrokeColor = UIColor.White.ColorWithAlpha(0.35f).CGColor,
			LineWidth = 1.5f,
			LineDashPattern = new[] { new NSNumber(6), new NSNumber(4) },
		});
		var screenTag = new UILabel {
			Text = "Screen",
			TextColor = UIColor.White.ColorWithAlpha(0.35f),
			Font = UIFont.SystemFontOfSize(11),
		};
		screenTag.SizeToFit();
		screenTag.Frame = new CGRect(_screenRect.X + 4, _screenRect.Y - screenTag.Frame.Height - 2, screenTag.Frame.Width, screenTag.Frame.Height);
		View.AddSubview(screenTag);

		_committedLayer = new CAShapeLayer {
			FillColor = TouchDrumShape.Tint(0x30).CGColor,
			StrokeColor = TouchDrumShape.Tint(0x80).CGColor,
			LineWidth = 1.5f,
		};
		View.Layer.AddSublayer(_committedLayer);

		_activeLayer = new CAShapeLayer {
			FillColor = UIColor.Clear.CGColor,
			StrokeColor = TouchDrumShape.Tint(0xA0).CGColor,
			LineWidth = 2,
		};
		View.Layer.AddSublayer(_activeLayer);

		// Existing saved shape, mapped from screen coords into the scaled screen rect.
		foreach (var stroke in TouchDrumShape.Strokes)
			_strokes.Add(stroke.Select(p => new CGPoint(
				_screenRect.X + p.X * _screenRect.Width,
				_screenRect.Y + p.Y * _screenRect.Height)).ToList());
		RebuildCommittedLayer();

		var safe = View.SafeAreaInsets;
		_hint = new UILabel {
			Text = "Draw Don areas (everything else is Ka). The dashed rectangle is the screen; strokes may extend past it. Save with none drawn to reset to the circle.",
			TextColor = UIColor.White.ColorWithAlpha(0.9f),
			Font = UIFont.SystemFontOfSize(13),
			TextAlignment = UITextAlignment.Center,
			Lines = 3,
			Frame = new CGRect(b.Width * 0.2, safe.Top + 58, b.Width * 0.6, 56),
			BackgroundColor = UIColor.FromRGBA(0.10f, 0.10f, 0.12f, 0.85f),
		};
		_hint.Layer.CornerRadius = 10;
		_hint.ClipsToBounds = true;
		View.AddSubview(_hint);

		(string label, Action action)[] defs = {
			("Save", SaveAndClose),
			("Undo", UndoStroke),
			("Clear", ClearStrokes),
			("Cancel", Cancel),
		};
		nfloat bw = 96, bh = 40, gap = 12;
		nfloat total = defs.Length * bw + (defs.Length - 1) * gap;
		nfloat x = (b.Width - total) / 2;
		foreach (var (label, action) in defs) {
			var v = new UILabel(new CGRect(x, safe.Top + 12, bw, bh)) {
				BackgroundColor = UIColor.FromRGBA(0.10f, 0.10f, 0.12f, 0.85f),
				Text = label,
				TextColor = UIColor.White,
				TextAlignment = UITextAlignment.Center,
				Font = UIFont.BoldSystemFontOfSize(16),
			};
			v.Layer.CornerRadius = 10;
			v.Layer.BorderWidth = 1.5f;
			v.Layer.BorderColor = UIColor.White.ColorWithAlpha(0.7f).CGColor;
			v.ClipsToBounds = true;
			View.AddSubview(v);
			_buttons.Add((v, action));
			x += bw + gap;
		}
	}

	public override void TouchesBegan(NSSet touches, UIEvent? evt) {
		if (_activeTouch != null) return;
		var touch = (UITouch)touches.AnyObject;
		var loc = touch.LocationInView(View);

		foreach (var (view, action) in _buttons) {
			if (view.Frame.Contains(loc)) {
				action();
				return;
			}
		}

		if (_strokes.Count >= MaxStrokes) {
			ShowHint($"Up to {MaxStrokes} areas.");
			return;
		}
		_activeTouch = touch;
		_active = new List<CGPoint> { loc };
	}

	public override void TouchesMoved(NSSet touches, UIEvent? evt) {
		if (_activeTouch == null || _active == null || !touches.Contains(_activeTouch)) return;
		var loc = _activeTouch.LocationInView(View);
		var last = _active[^1];
		double dx = loc.X - last.X, dy = loc.Y - last.Y;
		if (dx * dx + dy * dy < MinPointDistance * MinPointDistance) return;
		_active.Add(loc);
		_activeLayer!.Path = BuildPath(_active, close: false);
	}

	public override void TouchesEnded(NSSet touches, UIEvent? evt) => FinishStroke(touches);
	public override void TouchesCancelled(NSSet touches, UIEvent? evt) => FinishStroke(touches);

	private void FinishStroke(NSSet touches) {
		if (_activeTouch == null || !touches.Contains(_activeTouch)) return;
		var stroke = _active;
		_activeTouch = null;
		_active = null;
		_activeLayer!.Path = null;
		if (stroke == null) return;

		if (stroke.Count < 3 || PolygonArea(stroke) < MinAreaFraction * _screenRect.Width * _screenRect.Height) {
			ShowHint("Too small; draw a larger area.");
			return;
		}
		_strokes.Add(stroke);
		RebuildCommittedLayer();
	}

	// One layer for all committed strokes so overlaps display as a single merged shape.
	private void RebuildCommittedLayer() {
		_committedLayer!.Path = TouchDrumShape.BuildUnionPath(_strokes);
	}

	private void SaveAndClose() {
		TouchDrumShape.Save(_strokes
			.Select(s => s.Select(p => new CGPoint(
				(p.X - _screenRect.X) / _screenRect.Width,
				(p.Y - _screenRect.Y) / _screenRect.Height)).ToList())
			.ToList());
		DismissViewController(true, () => _onDone(true));
	}

	private void Cancel() => DismissViewController(true, () => _onDone(false));

	private void UndoStroke() {
		if (_strokes.Count == 0) return;
		_strokes.RemoveAt(_strokes.Count - 1);
		RebuildCommittedLayer();
	}

	private void ClearStrokes() {
		_strokes.Clear();
		RebuildCommittedLayer();
	}

	private void ShowHint(string text) {
		if (_hint != null) _hint.Text = text;
	}

	private static CGPath BuildPath(List<CGPoint> points, bool close) {
		var path = new UIBezierPath();
		path.MoveTo(points[0]);
		for (int i = 1; i < points.Count; i++) path.AddLineTo(points[i]);
		if (close) path.ClosePath();
		return path.CGPath!;
	}

	private static double PolygonArea(List<CGPoint> points) {
		double area = 0;
		for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
			area += (double)(points[j].X * points[i].Y - points[i].X * points[j].Y);
		return Math.Abs(area) / 2;
	}
}
