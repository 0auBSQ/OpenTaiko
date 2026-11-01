using CoreAnimation;
using CoreGraphics;
using Foundation;
using OpenGLES;
using UIKit;

namespace OpenTaiko.iOS;

/// <summary>
/// External display support for <see cref="GameViewController"/>. When an external display
/// connects, the game is presented there at the screen's highest mode and refresh
/// rate. The internal display blanks the game area and keeps only the touch controls.
/// </summary>
public partial class GameViewController {
	private UIWindow? _externalWindow;
	// Blanks the internal game view while external is active: above the Metal layer's
	// content, below the touch overlays.
	private UIView? _internalCover;
	private NSObject? _screenConnectObserver;
	private NSObject? _screenDisconnectObserver;
	private NSObject? _screenModeObserver;

	private bool ExternalDisplayActive => _externalWindow != null;

	// Hosts the external MetalView and keeps the drawable matched to the layer's actual
	// pixel size. Sizing the drawable from anything but the layer's own geometry lets
	// CoreAnimation stretch the presented image when the aspects differ.
	private sealed class ExternalViewController : UIViewController {
		public MetalPresenter? Presenter;
		public override void ViewDidLayoutSubviews() {
			base.ViewDidLayoutSubviews();
			var v = View!;
			Presenter?.UpdateDrawableSize(
				(int)(v.Bounds.Width * v.ContentScaleFactor),
				(int)(v.Bounds.Height * v.ContentScaleFactor));
		}
	}

	private void StartExternalDisplayWatch() {
		_screenConnectObserver = NSNotificationCenter.DefaultCenter.AddObserver(
			UIScreen.DidConnectNotification, n => {
				if (n.Object is UIScreen screen) ActivateExternalDisplay(screen);
			});
		_screenDisconnectObserver = NSNotificationCenter.DefaultCenter.AddObserver(
			UIScreen.DidDisconnectNotification, n => {
				if (n.Object is UIScreen screen && _externalWindow?.Screen == screen)
					DeactivateExternalDisplay();
			});
		// The bounds settle asynchronously after a mode switch. Follow them so the window,
		// layer and drawable stay consistent.
		_screenModeObserver = NSNotificationCenter.DefaultCenter.AddObserver(
			UIScreen.ModeDidChangeNotification, n => {
				if (n.Object is UIScreen screen && _externalWindow?.Screen == screen)
					_externalWindow.Frame = screen.Bounds;
			});
		// A display can already be attached at launch.
		foreach (var screen in UIScreen.Screens) {
			if (screen != UIScreen.MainScreen) {
				ActivateExternalDisplay(screen);
				break;
			}
		}
	}

	private void ActivateExternalDisplay(UIScreen screen) {
		if (ExternalDisplayActive || _glContext == null || View == null) return;

		// Choose the highest resolution mode the screen offers.
		UIScreenMode? best = null;
		foreach (var mode in screen.AvailableModes)
			if (best == null || mode.Size.Width * mode.Size.Height > best.Size.Width * best.Size.Height)
				best = mode;
		if (best != null) screen.CurrentMode = best;

		var view = new MetalView(screen.Bounds) {
			ContentScaleFactor = screen.Scale,
			AutoresizingMask = UIViewAutoresizing.FlexibleDimensions,
		};
		var controller = new ExternalViewController();
		controller.View = view;
		_externalWindow = new UIWindow(screen.Bounds) {
			Screen = screen,
			RootViewController = controller,
			// Shown but never key: input stays on the internal display.
			Hidden = false,
		};

		// Move the present boundary to the external layer; the shared render target is
		// recreated by EnsureRenderTarget on the next frame, and the drawable size is
		// maintained by ExternalViewController layout passes.
		EAGLContext.SetCurrentContext(_glContext);
		_metalPresenter?.Dispose();
		_metalPresenter = new MetalPresenter((CAMetalLayer)view.Layer, _glContext);
		controller.Presenter = _metalPresenter;
		_metalPresenter.UpdateDrawableSize(
			(int)(view.Bounds.Width * view.ContentScaleFactor),
			(int)(view.Bounds.Height * view.ContentScaleFactor));
		StartDisplayLink(screen);

		if (_internalCover == null) {
			_internalCover = new UIView(View.Bounds) {
				BackgroundColor = UIColor.Black,
				UserInteractionEnabled = false,
			};
			var notice = new UILabel {
				Text = global::OpenTaiko.CConfigOptionBuilder.L("EXTERNALDISPLAY_NOTICE",
					"The game is displayed on the external screen."),
				TextColor = UIColor.White.ColorWithAlpha(0.6f),
				Font = UIFont.SystemFontOfSize(17),
				TextAlignment = UITextAlignment.Center,
				Frame = new CGRect(0, View.Bounds.Height * 0.30, View.Bounds.Width, 24),
			};
			_internalCover.AddSubview(notice);
			View.InsertSubview(_internalCover, 0);
		}
		System.Diagnostics.Trace.TraceInformation(
			$"External display connected: {screen.Bounds.Width}x{screen.Bounds.Height} points " +
			$"at scale {screen.Scale}, up to {screen.MaximumFramesPerSecond} fps.");
	}

	private void DeactivateExternalDisplay() {
		if (!ExternalDisplayActive || _glContext == null || View == null) return;
		_externalWindow!.Hidden = true;
		_externalWindow = null;
		_internalCover?.RemoveFromSuperview();
		_internalCover = null;

		EAGLContext.SetCurrentContext(_glContext);
		_metalPresenter?.Dispose();
		_metalPresenter = new MetalPresenter((CAMetalLayer)View.Layer, _glContext);
		_metalPresenter.UpdateDrawableSize(_backingWidth, _backingHeight);
		StartDisplayLink(UIScreen.MainScreen);
		System.Diagnostics.Trace.TraceInformation(
			"External display disconnected: presenting on the internal display.");
	}

	/// <summary>
	/// (Re)creates the display link on the given screen so vsync and frame pacing follow the
	/// display actually showing the game.
	/// </summary>
	private void StartDisplayLink(UIScreen screen) {
		_displayLink?.Invalidate();
		_lastTimestamp = 0;
		_displayLink = screen.CreateDisplayLink(OnFrame);
		// Request the display's full refresh rate unless the "Frame Rate" setting caps it at 60.
		// Requires CADisableMinimumFrameDurationOnPhone=true in Info.plist to take effect on iPhone.
		bool unlimitedFps = global::OpenTaiko.OpenTaiko.ConfigIni?.biOSUnlimitedFrameRate ?? false;
		int screenMax = (int)screen.MaximumFramesPerSecond;
		if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0)) {
			float maxFps = unlimitedFps ? screenMax : Math.Min(60, screenMax);
			float minFps = Math.Min(60f, maxFps);
			_displayLink.PreferredFrameRateRange = CAFrameRateRange.Create(minFps, maxFps, maxFps);
		} else {
			_displayLink.PreferredFramesPerSecond = unlimitedFps ? screenMax : Math.Min(60, screenMax);
		}
		_displayLink.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Default);
	}
}
