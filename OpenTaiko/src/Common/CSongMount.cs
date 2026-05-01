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
			nCurrentSongDifficulty = FindClosestDifficulty(rCurrentlySelectedSong, nCurrentAnchorDifficulty);
		}
	}

	// Chosen song state (set when the player confirms a song)
	public int[] nChoosenSongDifficulty = new int[5];
	public string? strChosenSongGenre { get; set; }
	public CScore? rChosenScore { get; set; }
	public CSongListNode? rChoosenSong { get; set; }

	// Current selection state (updated live during song select navigation)
	private int _nCurrentAnchorDifficulty;
	public int nCurrentAnchorDifficulty {
		get => _nCurrentAnchorDifficulty;
		set {
			_nCurrentAnchorDifficulty = value;
			nCurrentSongDifficulty = FindClosestDifficulty(rCurrentlySelectedSong, value);
		}
	}

	public int nCurrentSongDifficulty { get; set; }
	public CScore? rCurrentScore => rCurrentlySelectedSong?.score[nCurrentSongDifficulty]; // require difficulty to be valid to not silence desyncing of other status


	// Closest level
	public static int FindClosestDifficulty(CSongListNode? song, int difficulty) {
		// 事前チェック。
		if (song == null)
			return difficulty;  // 曲がまったくないよ

		if (song.score[difficulty] != null)
			return difficulty;  // 難易度ぴったりの曲があったよ

		if ((song.nodeType == CSongListNode.ENodeType.BOX) || (song.nodeType == CSongListNode.ENodeType.BACKBOX))
			return 0;                               // BOX と BACKBOX は関係無いよ


		// 現在のアンカレベルから、難易度上向きに検索開始。

		int n最も近いレベル = difficulty;

		for (int i = 0; i < (int)Difficulty.Total; i++) {
			if (song.score[n最も近いレベル] != null)
				break;  // 曲があった。

			n最も近いレベル = (n最も近いレベル + 1) % (int)Difficulty.Total;  // 曲がなかったので次の難易度レベルへGo。（5以上になったら0に戻る。）
		}


		// 見つかった曲がアンカより下のレベルだった場合……
		// アンカから下向きに検索すれば、もっとアンカに近い曲があるんじゃね？

		if (n最も近いレベル < difficulty) {
			// 現在のアンカレベルから、難易度下向きに検索開始。

			n最も近いレベル = difficulty;

			for (int i = 0; i < (int)Difficulty.Total; i++) {
				if (song.score[n最も近いレベル] != null)
					break;  // 曲があった。

				n最も近いレベル = ((n最も近いレベル - 1) + (int)Difficulty.Total) % (int)Difficulty.Total;    // 曲がなかったので次の難易度レベルへGo。（0未満になったら4に戻る。）
			}
		}

		return n最も近いレベル;
	}

	public int FindClosestDifficultyToAnchor(CSongListNode? song)
		=> FindClosestDifficulty(song, nCurrentAnchorDifficulty);
}
