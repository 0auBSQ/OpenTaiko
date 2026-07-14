using Android.Content;
using Android.Graphics;
using Android.Views;

namespace OpenTaiko.Android;

/// <summary>
/// The on-screen control overlay — a faithful port of the iOS port's touch zones: a large Don
/// semicircle rising from the bottom edge (split left/right), Ka everywhere else (split left/right),
/// an ESC button top-left, and a software D-pad (bottom-right) while the Config stage is active.
/// The same geometry drives both the visuals (OnDraw) and the hit-testing (HitTest).
/// </summary>
public class TouchOverlayView : View {
	// HID usage codes, identical to the iOS mapping:
	// D=0x07(Ka-left), F=0x09(Don-left), J=0x0D(Don-right), K=0x0E(Ka-right), Escape=0x29
	public const long HID_D = 0x07, HID_F = 0x09, HID_J = 0x0D, HID_K = 0x0E, HID_ESC = 0x29;
	public const long HID_RIGHT = 0x4F, HID_LEFT = 0x50, HID_DOWN = 0x51, HID_UP = 0x52, HID_RETURN = 0x28;

	private const double EscZoneW = 0.10, EscZoneH = 0.15;   // normalized, matches iOS EscapeZone
	private const double DonCenterX = 0.5, DonCenterY = 1.05;

	/// <summary>Don radius as a fraction of the view width (fed from ConfigIni.nTouchDrumVisual).</summary>
	public double DonRadiusPercent { get; set; } = 30;

	/// <summary>While true (Config stage) the D-pad replaces the drum for hit-testing + drawing.</summary>
	public bool ArrowNavMode { get; set; }

	private readonly Paint _fill = new() { AntiAlias = true };
	private readonly Paint _stroke = new() { AntiAlias = true, StrokeWidth = 3 };
	private readonly Paint _text = new() { AntiAlias = true, TextAlign = Paint.Align.Center };

	public TouchOverlayView(Context context) : base(context) {
		SetWillNotDraw(false);
	}

	private double DonRadius => DonRadiusPercent / 100.0;

	/// <summary>Map a touch position (view pixels) to a HID usage code; -1 = no zone (arrow mode only).</summary>
	public long HitTest(float x, float y) {
		float w = Width, h = Height;
		if (x <= EscZoneW * w && y <= EscZoneH * h)
			return HID_ESC;
		if (ArrowNavMode) {
			foreach (var (rect, hid) in ArrowButtons())
				if (rect.Contains(x, y)) return hid;
			return -1;
		}
		double dx = x - DonCenterX * w;
		double dy = y - DonCenterY * h;
		double r = DonRadius * w;
		bool isLeft = x < w * 0.5;
		if (dx * dx + dy * dy <= r * r)
			return isLeft ? HID_F : HID_J;    // Don
		return isLeft ? HID_D : HID_K;        // Ka
	}

	private (RectF rect, long hid)[] ArrowButtons() {
		float w = Width, h = Height;
		float sz = Math.Max(96f, h * 0.11f), gap = 8f;
		float step = sz + gap;
		float cx = w - 32 - step - sz / 2;
		float cy = h - 32 - step - sz / 2;
		return new (RectF, long)[] {
			(new RectF(cx - sz / 2, cy - sz / 2 - step, cx + sz / 2, cy + sz / 2 - step), HID_UP),
			(new RectF(cx - sz / 2, cy - sz / 2 + step, cx + sz / 2, cy + sz / 2 + step), HID_DOWN),
			(new RectF(cx - sz / 2 - step, cy - sz / 2, cx + sz / 2 - step, cy + sz / 2), HID_LEFT),
			(new RectF(cx - sz / 2 + step, cy - sz / 2, cx + sz / 2 + step, cy + sz / 2), HID_RIGHT),
			(new RectF(cx - sz / 2, cy - sz / 2, cx + sz / 2, cy + sz / 2), HID_RETURN),
		};
	}

	protected override void OnDraw(Canvas canvas) {
		base.OnDraw(canvas);
		float w = Width, h = Height;

		// ESC button (top-left)
		var esc = new RectF(8, 8, (float)(EscZoneW * w) - 8, (float)(EscZoneH * h) - 8);
		_fill.Color = new Color(255, 255, 255, 38);
		_fill.SetStyle(Paint.Style.Fill);
		canvas.DrawRoundRect(esc, 16, 16, _fill);
		_text.Color = new Color(255, 255, 255, 128);
		_text.TextSize = esc.Height() * 0.4f;
		canvas.DrawText("ESC", esc.CenterX(), esc.CenterY() + _text.TextSize * 0.35f, _text);

		if (ArrowNavMode) {
			foreach (var (rect, hid) in ArrowButtons()) {
				_fill.Color = new Color(26, 26, 31, 184);
				canvas.DrawRoundRect(rect, 16, 16, _fill);
				_stroke.Color = new Color(255, 255, 255, 178);
				_stroke.SetStyle(Paint.Style.Stroke);
				canvas.DrawRoundRect(rect, 16, 16, _stroke);
				string label = hid == HID_UP ? "▲" : hid == HID_DOWN ? "▼" : hid == HID_LEFT ? "◄" : hid == HID_RIGHT ? "►" : "OK";
				_text.Color = Color.White;
				_text.TextSize = rect.Height() * (hid == HID_RETURN ? 0.30f : 0.40f);
				canvas.DrawText(label, rect.CenterX(), rect.CenterY() + _text.TextSize * 0.35f, _text);
			}
			return;
		}

		// Don circle — centered below the bottom edge, top arc visible
		float r = (float)(DonRadius * w);
		float cx = (float)(DonCenterX * w);
		float cy = (float)(DonCenterY * h);
		_fill.Color = new Color(255, 68, 68, 32);
		_fill.SetStyle(Paint.Style.Fill);
		canvas.DrawCircle(cx, cy, r, _fill);
		_stroke.Color = new Color(255, 68, 68, 64);
		_stroke.SetStyle(Paint.Style.Stroke);
		canvas.DrawCircle(cx, cy, r, _stroke);
	}
}
