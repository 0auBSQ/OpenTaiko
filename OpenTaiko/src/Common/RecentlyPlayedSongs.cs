namespace OpenTaiko;

internal class RecentlyPlayedSongs {
	public void tRecentlyPlayedSongs() {
		if (!File.Exists("RecentlyPlayedSongs.json"))
			tSaveFile();

		tLoadFile();
	}

	#region [Auxiliary methods]

	public void tAddChart(string chartID) {
		if (!data.recentlyplayedsongs[0].Contains(chartID))
			data.recentlyplayedsongs[0].Enqueue(chartID);

		while (data.recentlyplayedsongs[0].Count > OpenTaiko.ConfigIni.nRecentlyPlayedMax)
			data.recentlyplayedsongs[0].Dequeue();

		tSaveFile();
	}

	#endregion

	public class Data {
		public Queue<string>[] recentlyplayedsongs = new Queue<string>[2] { new Queue<string>(), new Queue<string>() };
	}

	public Data data = new Data();

	#region [private]

	private void tSaveFile() {
		ConfigManager.SaveConfig(data, "RecentlyPlayedSongs.json");
	}

	private void tLoadFile() {
		try {
			data = ConfigManager.GetConfig<Data>(@"RecentlyPlayedSongs.json");
		} catch {
			data = new Data();
		}
	}

	#endregion
}
