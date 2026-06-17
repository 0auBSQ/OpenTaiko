namespace OpenTaiko {
	// Read-only accessors for the song the player last CONFIRMED in song select (set on Mount). Lets a custom
	// Lua stage (e.g. onlinelobby) read what the host just picked so it can broadcast it to the other players.
	public class LuaSongMountFunc {
		public string ChosenUniqueId() => OpenTaiko.SongMount?.rChoosenSong?.tGetUniqueId() ?? "";
		public int ChosenDifficulty() { var sm = OpenTaiko.SongMount; return sm != null ? sm.nChoosenSongDifficulty[0] : 0; }

		// The chosen song as a LuaSongNode (boxed as object — the type is internal). Lets the song_loading
		// transition read the song the same way the old ROActivity's activate(node, …) did: .Title, .IsSong,
		// :GetChart(diff) (dan tick/colour), etc. Returns nil if nothing is mounted.
		public object? ChosenSongNode() {
			var s = OpenTaiko.SongMount?.rChoosenSong;
			return s != null ? new LuaSongNode(s, null, false) : null;
		}
	}
}
