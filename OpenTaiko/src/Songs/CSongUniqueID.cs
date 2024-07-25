namespace TJAPlayer3 {
	[Serializable()]
	internal class CSongUniqueID {
		public CSongUniqueID(string path) {
			filePath = path;

			tGenerateUniqueID();
			tSongUniqueID();
		}

		public void tSongUniqueID() {
			if (!File.Exists(filePath))
				tSaveFile();

			tLoadFile();
		}

		#region [Auxiliary methods]

		public void tAttachOnlineAddress(string url) {
			data.url = url;
			tSaveFile();
		}

		public void tGenerateUniqueID() {
			data.id = CCrypto.GetUniqueKey(64);
		}

		#endregion

		[Serializable]
		public class Data {
			public string id = "";
			public string url = "";
		}

		public Data data = new Data();

		public string filePath;

		#region [private]

		private void tSaveFile() {
			ConfigManager.SaveConfig(data, filePath);
		}

		private void tLoadFile() {
			data = ConfigManager.GetConfig<Data>(filePath);
		}

		#endregion
	}
}
