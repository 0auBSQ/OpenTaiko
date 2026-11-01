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
