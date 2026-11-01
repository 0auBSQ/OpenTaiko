using UIKit;

namespace OpenTaiko.iOS;

public class Application {
	static void Main(string[] args) {
		// Install native crash handlers before the run loop starts (Release builds only — see
		// CrashLog). No-op on Debug builds.
		CrashLog.Install();
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}
