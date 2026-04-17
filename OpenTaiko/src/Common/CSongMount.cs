namespace OpenTaiko;

/// <summary>
/// Holds the currently and previously selected song, decoupled from CStageSongSelect.
/// This allows future stages and systems to access song selection state without depending
/// on the song select stage being active.
/// </summary>
internal class CSongMount {
	public CSongListNode? rPrevSelectedSong {
		get;
		private set;
	}

	private CSongListNode? _rCurrentlySelectedSong;
	public CSongListNode? rCurrentlySelectedSong {
		get => _rCurrentlySelectedSong;
		set {
			rPrevSelectedSong = _rCurrentlySelectedSong;
			_rCurrentlySelectedSong = value;
		}
	}

	// Chosen song state (set when the player confirms a song)
	public int[] nChoosenSongDifficulty = new int[5];
	public string? strChosenSongGenre { get; set; }
	public CScore? rChosenScore { get; set; }
	public CSongListNode? rChoosenSong { get; set; }

	// Current selection state (updated live during song select navigation)
	public int nCurrentSongDifficulty { get; set; }
	public CScore? rCurrentScore => rCurrentlySelectedSong?.score[nCurrentSongDifficulty];
}
