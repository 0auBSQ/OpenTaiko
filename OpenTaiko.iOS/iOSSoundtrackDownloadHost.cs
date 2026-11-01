using Foundation;
using UIKit;

namespace OpenTaiko.iOS;

internal sealed class iOSSoundtrackDownloadHost : global::OpenTaiko.ISoundtrackDownloadHost {
	private readonly UIViewController _host;

	internal iOSSoundtrackDownloadHost(UIViewController host) {
		_host = host;
	}

	public string UserAgent => "OpenTaiko-iOS";

	public global::OpenTaiko.SoundtrackDownloadChoice AskUser(int songCount, long bytes) {
		var picked = global::OpenTaiko.SoundtrackDownloadChoice.Later;
		using var done = new ManualResetEventSlim(false);
		_host.InvokeOnMainThread(() => {
			var alert = UIAlertController.Create(
				"OpenTaiko Soundtrack",
				$"Download the official soundtrack now?\n\n{songCount} songs, {GB(bytes)} — Wi-Fi recommended. An interrupted download resumes on the next launch. You can also add songs yourself through the Files app (OpenTaiko/Songs).",
				UIAlertControllerStyle.Alert);
			alert.AddAction(UIAlertAction.Create("Download", UIAlertActionStyle.Default, _ => {
				picked = global::OpenTaiko.SoundtrackDownloadChoice.Download;
				done.Set();
			}));
			alert.AddAction(UIAlertAction.Create("Not now", UIAlertActionStyle.Default, _ => done.Set()));
			alert.AddAction(UIAlertAction.Create("Never", UIAlertActionStyle.Destructive, _ => {
				picked = global::OpenTaiko.SoundtrackDownloadChoice.Never;
				done.Set();
			}));
			_host.PresentViewController(alert, true, null);
		});
		done.Wait();
		return picked;
	}

	public long GetFreeBytes(string dataRoot) {
		var attrs = NSFileManager.DefaultManager.GetFileSystemAttributes(dataRoot);
		return attrs == null ? long.MaxValue : (long)attrs.FreeSize;
	}

	public void LogInformation(string message) => System.Diagnostics.Trace.TraceInformation(message);

	public void LogWarning(string message) => System.Diagnostics.Trace.TraceWarning(message);

	private static string GB(long bytes) => $"{bytes / 1e9:0.00} GB";
}
