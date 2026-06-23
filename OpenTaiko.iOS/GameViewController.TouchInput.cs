using CoreGraphics;
using Foundation;
using UIKit;

namespace OpenTaiko.iOS;

/// <summary>
/// Touch-drum input for <see cref="GameViewController"/>: the on-screen Don/Ka/ESC overlay,
/// hit-testing of touch locations into HID key codes, and the UIKit touch-event overrides.
/// </summary>
public partial class GameViewController {

	// HID usage codes: D=0x07(Ka-left), F=0x09(Don-left), J=0x0D(Don-right), K=0x0E(Ka-right), Escape=0x29
	private const long HID_D = 0x07, HID_F = 0x09, HID_J = 0x0D, HID_K = 0x0E, HID_ESC = 0x29;

	// Escape zone (normalized coords)
	private static readonly CGRect EscapeZone = new CGRect(0, 0, 0.10, 0.15);

	// Don circle: large semicircle centered below bottom edge, top portion visible
	private const double DonCenterX = 0.5;
	private const double DonCenterY = 1.05;
	// Single radius for both visual and hit detection
	private double DonRadius => (global::OpenTaiko.OpenTaiko.ConfigIni?.nTouchDrumVisual ?? 30) / 100.0;

	private UIView? _touchOverlay;

	private void CreateTouchOverlay() {
		_touchOverlay = new UIView(View!.Bounds) {
			UserInteractionEnabled = false,
			BackgroundColor = UIColor.Clear
		};

		var bounds = View.Bounds;
		var w = bounds.Width;
		var h = bounds.Height;

		// Escape button (top-left rounded rect, inset by safe area)
		var safeInsets = View.SafeAreaInsets;
		var escRect = new CGRect(safeInsets.Left + 8, safeInsets.Top + 8, EscapeZone.Width * w - 8, EscapeZone.Height * h - 8);
		var escView = new UILabel(escRect) {
			BackgroundColor = UIColor.White.ColorWithAlpha(0.15f),
			Text = "ESC",
			TextColor = UIColor.White.ColorWithAlpha(0.5f),
			TextAlignment = UITextAlignment.Center,
			Font = UIFont.BoldSystemFontOfSize(14),
		};
		escView.Layer.CornerRadius = 10;
		escView.ClipsToBounds = true;
		_touchOverlay.AddSubview(escView);

		// Don circle — centered below bottom edge, clipped to show top portion
		var r = DonRadius * w;
		var cx = DonCenterX * w;
		var cy = DonCenterY * h;
		var donView = new UIView(new CGRect(cx - r, cy - r, r * 2, r * 2));
		donView.BackgroundColor = UIColor.FromRGBA(0xFF, 0x44, 0x44, 0x20);
		donView.Layer.CornerRadius = (nfloat)r;
		donView.Layer.BorderWidth = 1.5f;
		donView.Layer.BorderColor = UIColor.FromRGBA(0xFF, 0x44, 0x44, 0x40).CGColor;
		_touchOverlay.AddSubview(donView);

		View.AddSubview(_touchOverlay);
		View.ClipsToBounds = true;
	}

	private long HitTestTouchZone(CGPoint location) {
		var bounds = View!.Bounds;
		double w = bounds.Width;
		double h = bounds.Height;

		// Check escape zone (offset by safe area to match visual button)
		var safeInsets = View.SafeAreaInsets;
		if (location.X <= safeInsets.Left + EscapeZone.Width * w && location.Y <= safeInsets.Top + EscapeZone.Height * h) {
			return HID_ESC;
		}

		// Check Don circle in pixel space
		double dx = location.X - DonCenterX * w;
		double dy = location.Y - DonCenterY * h;
		double r = DonRadius * w;

		bool isLeft = location.X < w * 0.5;

		if (dx * dx + dy * dy <= r * r) {
			// Inside Don circle: F (left) / J (right)
			return isLeft ? HID_F : HID_J;
		}

		// Everywhere else is Ka: D (left) / K (right)
		return isLeft ? HID_D : HID_K;
	}

	public override void TouchesBegan(NSSet touches, UIEvent? evt) {
		base.TouchesBegan(touches, evt);
		foreach (UITouch touch in touches.Cast<UITouch>()) {
			var location = touch.LocationInView(View);
			long hidCode = HitTestTouchZone(location);
			if (hidCode >= 0) {
				_keyboardInput?.TouchKeyDown(hidCode);
			}
		}
	}

	public override void TouchesEnded(NSSet touches, UIEvent? evt) {
		base.TouchesEnded(touches, evt);
	}

	public override void TouchesCancelled(NSSet touches, UIEvent? evt) {
		base.TouchesCancelled(touches, evt);
	}
}
