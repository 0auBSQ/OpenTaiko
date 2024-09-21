namespace OpenTaiko {
	internal class RecentlyPlayedSongs {
		public void tRecentlyPlayedSongs() {
			if (!File.Exists("RecentlyPlayedSongs.json"))
				tSaveFile();

			tLoadFile();
		}

		#region [Auxiliary methods]

		public void tAddChart(string chartID) {
			if (!data.recentlyplayedsongs[OpenTaiko.SaveFile].Contains(chartID))
				data.recentlyplayedsongs[OpenTaiko.SaveFile].Enqueue(chartID);

			while (data.recentlyplayedsongs[OpenTaiko.SaveFile].Count > OpenTaiko.ConfigIni.nRecentlyPlayedMax)
				data.recentlyplayedsongs[OpenTaiko.SaveFile].Dequeue();

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
			data = ConfigManager.GetConfig<Data>(@"RecentlyPlayedSongs.json");
		}

		#endregion
	}

}
