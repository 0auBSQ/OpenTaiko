namespace OpenTaiko {
	internal class LuaSongChart {
		private Difficulty _difficulty;
		private int _level;
		private CTja.ELevelIcon _levelIcon;
		private string _notesDesigner = "";
		private CScore _score;
		private CScore.ST譜面情報? _chartInfo;
		private LuaSongNode? _parent;
		private CSongListNode? _parentListNode;

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
					CScore.ST譜面情報 _cinfo = (CScore.ST譜面情報)_chartInfo;
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

		#endregion


		public bool Select(int player, bool init = false) {
			if (!NotNull) return false;

			if (init) OpenTaiko.stageSongSelect.rChoosenSong = _parentListNode;
			OpenTaiko.stageSongSelect.r確定されたスコア = _score;
			OpenTaiko.stageSongSelect.nChoosenSongDifficulty[player] = (int)Difficulty;
			if (init) OpenTaiko.stageSongSelect.str確定された曲のジャンル = _parent.Genre;

			return true;
		}

		public LuaSongChart(LuaSongNode? parent, CSongListNode? _from, int _chart) {
			if (_from != null) {
				_difficulty = (Difficulty)_chart;
				_level = _from.nLevel[_chart];
				_levelIcon = _from.nLevelIcon[_chart];
				_notesDesigner = _from.strNotesDesigner[_chart];
				_chartInfo = _from.score[_chart]?.譜面情報 ?? null;
				_score = _from.score[_chart];
				_parent = parent;
				_parentListNode = _from;
			}
		}
	}
}
