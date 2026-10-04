namespace OpenTaiko {
	// Accessors for the song the player last CONFIRMED in song select (set on Mount). Lets a custom
	// Lua stage (e.g. onlinelobby) read what the host just picked so it can broadcast it to the other players.
	public class LuaSongMountFunc {
		private readonly string _dir;

		public LuaSongMountFunc(string dir) => _dir = dir;

		public string ChosenUniqueId() => OpenTaiko.SongMount?.rChoosenSong?.tGetUniqueId() ?? "";
		public int ChosenDifficulty() { var sm = OpenTaiko.SongMount; return sm != null ? sm.nChoosenSongDifficulty[0] : 0; }

		// The chosen song as a LuaSongNode (boxed as object — the type is internal). Lets the song_loading
		// transition read the song the same way the old ROActivity's activate(node, …) did: .Title, .IsSong,
		// :GetChart(diff) (dan tick/colour), etc. Returns nil if nothing is mounted.
		public object? ChosenSongNode() {
			var s = OpenTaiko.SongMount?.rChoosenSong;
			return s != null ? new LuaSongNode(s, null, false) : null;
		}

		// Forces the next play's upper and lower gameplay backgrounds: folders holding a background Script.lua, relative
		// to this script's folder or absolute, nil or "" keeping the usual pick. The next play takes them when it starts;
		// a new call replaces both. Returns false when a given folder has no Script.lua (that layer keeps the usual pick).
		public bool SetBackgrounds(string? up = null, string? down = null) {
			CActImplBackground.NextUpDir = BackgroundDir(up, out bool upFound);
			CActImplBackground.NextDownDir = BackgroundDir(down, out bool downFound);
			return upFound && downFound;
		}

		private string? BackgroundDir(string? dir, out bool found) {
			found = true;
			if (string.IsNullOrEmpty(dir)) return null;
			try {
				string full = Path.GetFullPath(Path.Combine(_dir, dir));
				found = File.Exists(Path.Combine(full, "Script.lua"));
				return found ? full : null;
			} catch {
				found = false;
				return null;
			}
		}
	}
}
