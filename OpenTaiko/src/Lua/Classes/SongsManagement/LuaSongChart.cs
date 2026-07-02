namespace OpenTaiko {
	internal class LuaSongChart {
		private Difficulty _difficulty;
		private int _level;
		private CTja.ELevelIcon _levelIcon;
		private string _notesDesigner = "";
		private CScore _score;
		private CScore.STChartInfo? _chartInfo;
		private LuaSongNode? _parent;
		private CSongListNode? _parentListNode;
		private Dictionary<string, string> _customCommands = new();

		public bool NotNull {
			get {
				return _chartInfo != null && _parent != null && _parentListNode != null;
			}
		}

		public LuaSongNode? Parent {
			get {
				return _parent;
			}
		}

		#region [Player scores]

		public LuaBestScoreInfo? GetPlayerBestScore(int save) {
			return new LuaBestScoreInfo(this, save);
		}

		#endregion

		#region [General metadata]

		public int Level {
			get {
				return _level;
			}
		}

		public CTja.ELevelIcon LevelIcon {
			get {
				return _levelIcon;
			}
		}

		public bool IsPlus {
			get {
				return _levelIcon == CTja.ELevelIcon.ePlus;
			}
		}

		public Difficulty Difficulty {
			get {
				return _difficulty;
			}
		}

		public int DifficultyAsInt {
			get {
				return (int)_difficulty;
			}
		}

		public string NotesDesigner {
			get {
				return _notesDesigner;
			}
		}

		public string[] Charters {
			get {
				return _notesDesigner.SplitByCommas();
			}
		}

		public double? BPM {
			get {
				return _chartInfo?.Bpm ?? null;
			}
		}

		public double? BaseBPM {
			get {
				return _chartInfo?.BaseBpm ?? null;
			}
		}

		public double? MinBPM {
			get {
				return _chartInfo?.MinBpm ?? null;
			}
		}

		public double? MaxBPM {
			get {
				return _chartInfo?.MaxBpm ?? null;
			}
		}

		#endregion

		#region [Tower specific]

		public int? Life {
			get {
				return _chartInfo?.nLife ?? null;
			}
		}

		public int? TotalFloorCount {
			get {
				return _chartInfo?.nTotalFloor ?? null;
			}
		}

		public string? TowerType {
			get {
				return _chartInfo?.nTowerType ?? null;
			}
		}

		#endregion

		#region [Dan specific]

		public int? DanTick {
			get {
				return _chartInfo?.nDanTick ?? null;
			}
		}

		public LuaColor DanTickColor {
			get {
				LuaColorFunc lfc = new LuaColorFunc();
				if (_chartInfo != null) {
					CScore.STChartInfo _cinfo = (CScore.STChartInfo)_chartInfo;
					return lfc.CreateColorFromRGBA(_cinfo.cDanTickColor.R, _cinfo.cDanTickColor.G, _cinfo.cDanTickColor.B, _cinfo.cDanTickColor.A);
				}
				return lfc.CreateColorFromHex("#ffffff");
			}
		}

		public LuaSongDanSong[] DanSongs {
			get {
				List<LuaSongDanSong> _ds = new List<LuaSongDanSong>();
				foreach (CTja.DanSongs _danSong in _parentListNode?.DanSongs ?? new List<CTja.DanSongs>()) {
					_ds.Add(new LuaSongDanSong(_danSong));
				}
				return _ds.ToArray();
			}
		}

		public LuaSongDanExam[] DanExams {
			get {
				List<LuaSongDanExam> _de = new List<LuaSongDanExam>();
				foreach (Dan_C _danC in _parentListNode?.Dan_C.ToList() ?? new List<Dan_C>()) {
					_de.Add(new LuaSongDanExam(_danC));
				}
				return _de.ToArray();
			}
		}

		/// <summary>Returns the per-song exam for a given song and exam slot.
		/// Both <paramref name="songIdx"/> and <paramref name="examSlot"/> are 1-based.
		/// Returns an exam with IsSet=false when no per-song exam exists for that combination.</summary>
		public LuaSongDanExam GetSongExam(int songIdx, int examSlot) {
			int si = songIdx - 1;
			int ei = examSlot - 1;
			var songs = _parentListNode?.DanSongs;
			if (songs == null || si < 0 || si >= songs.Count) return new LuaSongDanExam(null);
			var danC = songs[si].Dan_C;
			if (ei < 0 || ei >= danC.Length) return new LuaSongDanExam(null);
			return new LuaSongDanExam(danC[ei]);
		}

		#endregion


		#region [Custom commands]

		/// <summary>Gets a chart-scope custom command value (commands starting with "." placed inside this chart's COURSE block).</summary>
		public string? GetCustomCommand(string key) {
			_customCommands.TryGetValue(key, out string? value);
			return value;
		}

		/// <summary>Returns all chart-scope custom commands as a Lua-accessible table (Dictionary).</summary>
		public Dictionary<string, string> GetCustomCommands() {
			return _customCommands;
		}

		#endregion

		// folder holding this chart's .tja (its Replay/ subfolder lives here) and the song's unique id — used by the
		// best-plays list to find replays for this chart
		public string SongFolder => _score != null ? (_score.FileInfo.FolderAbsolutePath ?? "") : "";
		public string ChartPath => _score != null ? (_score.FileInfo.FileAbsolutePath ?? "") : "";
		public string UniqueId => _parentListNode?.uniqueId?.data.id ?? "";

		public bool Select(int player) {
			if (!NotNull) return false;

			// Player 0 owns the global song-selection state; always sync it so song loading
			// never receives a null rChoosenSong regardless of whether Mount() was called.
			if (player == 0) {
				OpenTaiko.SongMount.rChoosenSong = _parentListNode;
				OpenTaiko.SongMount.strChosenSongGenre = _parent.Genre ?? "???";
			}
			OpenTaiko.SongMount.rChosenScore = _score;
			OpenTaiko.SongMount.nChoosenSongDifficulty[player] = (int)Difficulty;

			return true;
		}

		public LuaSongChart(LuaSongNode? parent, CSongListNode? _from, int _chart) {
			if (_from != null) {
				_difficulty = (Difficulty)_chart;
				_level = _from.nLevel[_chart];
				_levelIcon = _from.nLevelIcon[_chart];
				_notesDesigner = _from.strNotesDesigner[_chart];
				_chartInfo = _from.score[_chart]?.ChartInfo ?? null;
				_score = _from.score[_chart];
				_parent = parent;
				_parentListNode = _from;
				if (_chart >= 0 && _chart < _from.customMetadataCScope.Length)
					_customCommands = _from.customMetadataCScope[_chart];
			}
		}
	}
}
