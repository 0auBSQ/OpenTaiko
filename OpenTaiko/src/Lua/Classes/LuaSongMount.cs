namespace OpenTaiko {
	// Read-only accessors for the song the player last CONFIRMED in song select (set on Mount). Lets a custom
	// Lua stage (e.g. onlinelobby) read what the host just picked so it can broadcast it to the other players.
	public class LuaSongMountFunc {
		public string ChosenUniqueId() => OpenTaiko.SongMount?.rChoosenSong?.tGetUniqueId() ?? "";
		public int ChosenDifficulty() { var sm = OpenTaiko.SongMount; return sm != null ? sm.nChoosenSongDifficulty[0] : 0; }
	}
}
