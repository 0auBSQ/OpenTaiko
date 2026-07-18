using Android.App;
using Activity = Android.App.Activity;

namespace OpenTaiko.Android;

internal sealed class AndroidSoundtrackDownloadHost : global::OpenTaiko.ISoundtrackDownloadHost {
	private readonly Activity _activity;

	internal AndroidSoundtrackDownloadHost(Activity activity) {
		_activity = activity;
	}

	public string UserAgent => "OpenTaiko-Android";

	public global::OpenTaiko.SoundtrackDownloadChoice AskUser(int songCount, long bytes) {
		var picked = global::OpenTaiko.SoundtrackDownloadChoice.Later;
		using var done = new ManualResetEventSlim(false);
		_activity.RunOnUiThread(() => {
			new AlertDialog.Builder(_activity)
				.SetTitle("OpenTaiko Soundtrack")!
				.SetMessage($"Download the official soundtrack now?\n\n{songCount} songs, {GB(bytes)} — Wi-Fi recommended. An interrupted download resumes on the next launch. You can also add songs yourself over USB (files/Songs).")!
				.SetPositiveButton("Download", (_, _) => {
					picked = global::OpenTaiko.SoundtrackDownloadChoice.Download;
					done.Set();
				})!
				.SetNeutralButton("Not now", (_, _) => done.Set())!
				.SetNegativeButton("Never", (_, _) => {
					picked = global::OpenTaiko.SoundtrackDownloadChoice.Never;
					done.Set();
				})!
				.SetCancelable(false)!
				.Show();
		});
		done.Wait();
		return picked;
	}

	public long GetFreeBytes(string dataRoot) => new global::Android.OS.StatFs(dataRoot).AvailableBytes;

	public void LogInformation(string message) => global::Android.Util.Log.Info("OpenTaiko", message);

	public void LogWarning(string message) => global::Android.Util.Log.Warn("OpenTaiko", message);

	private static string GB(long bytes) => $"{bytes / 1e9:0.00} GB";
}
