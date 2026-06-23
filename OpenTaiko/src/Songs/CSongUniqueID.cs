using System.Diagnostics;

namespace OpenTaiko;

[Serializable()]
internal class CSongUniqueID {
	public CSongUniqueID(string path) {
		filePath = path;

		if (File.Exists(filePath)) {
			tLoadFile();
		} else {
			tGenerateUniqueID();
			tSaveFile();
		}
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
		try {
			ConfigManager.SaveConfig(data, filePath);
		} catch (IOException ex) {
			// Log instead of swallowing: the file may already exist with different casing on case-sensitive filesystems.
			Trace.TraceWarning($"CSongUniqueID: could not save {filePath}: {ex.Message}");
		}
	}

	private void tLoadFile() {
		data = ConfigManager.GetConfig<Data>(filePath);
	}

	#endregion
}
