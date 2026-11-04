namespace OpenTaiko {
	internal class LuaSongDanSong {
		private CTja.DanSongs _dsInfo;

		public LuaSongDanSong(CTja.DanSongs _ds) {
			_dsInfo = _ds;
		}

		#region [General Metadata]

		public string Title {
			get {
				return _dsInfo.Title;
			}
		}

		public string SubTitle {
			get {
				return _dsInfo.SubTitle;
			}
		}

		public string Genre {
			get {
				return _dsInfo.Genre;
			}
		}

		public int Level {
			get {
				return _dsInfo.Level;
			}
		}

		/// <summary>The level with its decimals (e.g. 10.5); the whole Level when the dan gives none.</summary>
		public double LevelDecimal {
			get {
				return _dsInfo.LevelDecimal >= 0 ? _dsInfo.LevelDecimal : _dsInfo.Level;
			}
		}

		public Difficulty Difficulty {
			get {
				return (Difficulty)_dsInfo.Difficulty;
			}
		}

		public int DifficultyAsInt {
			get {
				return _dsInfo.Difficulty;
			}
		}

		#endregion
	}
}
