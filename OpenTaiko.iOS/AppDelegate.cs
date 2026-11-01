using Foundation;
using UIKit;

namespace OpenTaiko.iOS;

[Register("AppDelegate")]
public class AppDelegate : UIApplicationDelegate {
	public override UIWindow? Window { get; set; }

	public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions) {
		Window = new UIWindow(UIScreen.MainScreen.Bounds);
		Window.RootViewController = new GameViewController();
		Window.MakeKeyAndVisible();
		return true;
	}

	public override UIInterfaceOrientationMask GetSupportedInterfaceOrientations(UIApplication application, UIWindow? forWindow) {
		return UIInterfaceOrientationMask.Landscape;
	}

}
