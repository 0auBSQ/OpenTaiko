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
	public CScore? rCurrentScore {
		get {
			if (rCurrentlySelectedSong == null) return null;
			var direct = rCurrentlySelectedSong.score[nCurrentSongDifficulty];
			if (direct != null) return direct;
			// The anchored difficulty has no chart for this song; find the nearest non-null score.
			for (int i = 1; i < rCurrentlySelectedSong.score.Length; i++) {
				int candidate = (nCurrentSongDifficulty + i) % rCurrentlySelectedSong.score.Length;
				var s = rCurrentlySelectedSong.score[candidate];
				if (s != null) return s;
			}
			return null;
		}
	}
}
