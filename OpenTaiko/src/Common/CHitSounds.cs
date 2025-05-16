using Newtonsoft.Json;

namespace OpenTaiko;

class CHitSounds {
	public CHitSounds(string path) {
		tLoadFile(path);
		for (int i = 0; i < 5; i++) {
			tReloadHitSounds(OpenTaiko.ConfigIni.nHitSounds[i], i);
		}
	}

	public bool tReloadHitSounds(int id, int player) {
		if (id >= names.Length || id >= data.Length)
			return false;

		string fileExtension(string file) {
			string path = Path.Combine(data[id].path, file);
			return File.Exists(path + ".ogg") ? path + ".ogg" : path + ".wav";
		}

		don[player] = fileExtension("dong");
		ka[player] = fileExtension("ka");
		adlib[player] = fileExtension("Adlib");
		clap[player] = fileExtension("clap");

		return true;
	}

	public CLocalizationData[] names;

	public string[] don = new string[5];
	public string[] ka = new string[5];
	public string[] adlib = new string[5];
	public string[] clap = new string[5];

	#region [private]

	private class HitSoundsData {
		[JsonProperty("name")]
		[JsonConverter(typeof(LocalizedStringConverter<CLocalizationData>))]
		public CLocalizationData name = new();

		[JsonIgnore]
		public string path = $"Global{Path.DirectorySeparatorChar}HitSounds{Path.DirectorySeparatorChar}_fallback{Path.DirectorySeparatorChar}";

		public HitSoundsData() { name.SetString("default", "Unknown Hitsound"); }
		public HitSoundsData(string path) : this() {
			name.SetString("default", Path.GetRelativePath($"Global{Path.DirectorySeparatorChar}HitSounds{Path.DirectorySeparatorChar}", path));
			this.path = path;
		}
	}

	private HitSoundsData[] data;

	private void tLoadFile(string path) {
		string[] directories = Directory.GetDirectories(path);
		data = new HitSoundsData[directories.Length];
		names = new CLocalizationData[data.Length];

		for (int i = 0; i < data.Length; i++) {
			string dir_path = Path.Combine(directories[i], "HitSounds.json");
			data[i] = File.Exists(dir_path) ? ConfigManager.GetConfig<HitSoundsData>(dir_path) : new(directories[i]);
			data[i].path = directories[i];
			names[i] = data[i].name;

			if (!File.Exists(dir_path)) { ConfigManager.SaveConfig(data[i], dir_path); }
		}
	}

	#endregion
}
