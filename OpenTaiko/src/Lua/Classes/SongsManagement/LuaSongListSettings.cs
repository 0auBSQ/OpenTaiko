namespace OpenTaiko {
	internal class LuaSongListSettings {
		// Settings class used to customize which songs to include in the given song lists

		// Add a global random box within the root list
		public bool AppendMainRandomBox = true;

		// Add a Random box at the end of each folders 
		public bool AppendSubRandomBoxes = true;

		// Indicates the frequency of back boxes within folders, fe. 7 means one back box for 7 songs
		public int SubBackBoxFrequency = 7;

		// Genre folders that are not displayed in the song select menu
		public string[] ExcludedGenreFolders = [];

		// Generate the root node from a specific folder (given by Genre) instead of the list song root note (fe. Dan song select, Tower song select)
		public string? RootGenreFolder = null;

		// Only select certain difficulties to include in the songlist (fe. Dan for Dan song select, Tower for Tower song select)
		public Difficulty[]? DifficultyIncludeList = null;

		// Hide folder where there is nothing to show (No song, no locked song having a non-hidden hidden index)
		public bool HideEmptyFolders = true;

		// If true, display openned folders as flatten and get all leaves instead of just the child folder
		public bool FlattenOpennedFolders = true;

		// If true, requested pages will cycle on overflow, else overflowing element will be set as null
		public bool ModuloPagination = true;

		// If true, when moving within the song folder the cursor will loop through the page in case of overflow, else the cursor will focus the last element on both sides
		public bool ModuloMovement = true;

		public static LuaSongListSettings Generate() {
			return new LuaSongListSettings();
		}

		public LuaSongListSettings() {

		}
	}
}
