using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

[Serializable]
internal class CScore {
	// Old DTX class, to deprecate ASAP and handle everything on CSongListNode


	// Properties

	public STScoreIniInfo ScoreIniInfo;
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct STScoreIniInfo {
		public DateTime LastUpdateDateTime;
		public long FileSize;

		public STScoreIniInfo(DateTime LastUpdateDateTime, long FileSize) {
			this.LastUpdateDateTime = LastUpdateDateTime;
			this.FileSize = FileSize;
		}
	}

	public STFileInfo FileInfo;
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct STFileInfo {
		public string FileAbsolutePath;
		public string FolderAbsolutePath;
		public DateTime LastUpdateDateTime;
		public long FileSize;

		public STFileInfo(string FileAbsolutePath, string FolderAbsolutePath, DateTime LastUpdateDateTime, long FileSize) {
			this.FileAbsolutePath = FileAbsolutePath;
			this.FolderAbsolutePath = FolderAbsolutePath;
			this.LastUpdateDateTime = LastUpdateDateTime;
			this.FileSize = FileSize;
		}
	}

	public STChartInfo ChartInfo;

	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct STChartInfo {
		public string Title;
		public string ArtistName;
		public string Comment;
		public string Genre;
		public string Preimage;
		public string Premovie;
		public string Presound;
		public string Backgound;
		public int Level;
		public STSKILL MaxSkill;
		public bool FullCombo;
		public int PlayCount;
		public STHISTORY PlayHistory;
		public bool LevelHide;
		public double Bpm;
		public double BaseBpm;
		public double MinBpm;
		public double MaxBpm;
		public int Duration;
		public string strBGMFileName;
		public int SongVol;
		public LoudnessMetadata? SongLoudnessMetadata;
		public int nDemoBGMOffset;
		public bool[] bChartBranch;
		public int HighScore;
		public int[] nHighScore;
		public string strSubtitle;
		public int[] nLevel;
		public int[] nClear;      //0:未クリア 1:クリア 2:フルコンボ 3:ドンダフルコンボ
		public int[] nScoreRank;  //0:未取得 1:白粋 2:銅粋 3:銀粋 4:金雅 5:桃雅 6:紫雅 7:虹極
		public CTja.ELevelIcon[] nLevelIcon;

		// Tower lifes
		public int nLife;
		public int nTotalFloor;
		public string nTowerType;

		public int nDanTick;
		public Color cDanTickColor;

		public List<int[]> nExamResult;

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct STHISTORY {
			public string Line1;
			public string Line2;
			public string Line3;
			public string Line4;
			public string Line5;
			public string Line6;
			public string Line7;
			public string this[int index] {
				get {
					switch (index) {
						case 0:
							return this.Line1;

						case 1:
							return this.Line2;

						case 2:
							return this.Line3;

						case 3:
							return this.Line4;

						case 4:
							return this.Line5;
						case 5:
							return this.Line6;
						case 6:
							return this.Line7;
					}
					throw new IndexOutOfRangeException();
				}
				set {
					switch (index) {
						case 0:
							this.Line1 = value;
							return;

						case 1:
							this.Line2 = value;
							return;

						case 2:
							this.Line3 = value;
							return;

						case 3:
							this.Line4 = value;
							return;

						case 4:
							this.Line5 = value;
							return;
						case 5:
							this.Line6 = value;
							return;
						case 6:
							this.Line7 = value;
							return;
					}
					throw new IndexOutOfRangeException();
				}
			}
		}

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct STSKILL {
			public double Drums;
			public double this[int index] {
				get {
					if (index == 0) {
						return this.Drums;
					}
					throw new IndexOutOfRangeException();
				}
				set {
					if ((value < 0.0) || (value > 100.0)) {
						throw new ArgumentOutOfRangeException();
					}
					if (index == 0) {
						this.Drums = value;
						return;
					}
					throw new IndexOutOfRangeException();
				}
			}
		}
	}

	public bool bHadCacheInSongDB;
	public bool bScoreEnabled {
		get {
			return (this.ChartInfo.Level != 0);
		}
	}


	// Constructor

	public CScore() {
		this.ScoreIniInfo = new STScoreIniInfo(DateTime.MinValue, 0L);
		this.bHadCacheInSongDB = false;
		this.FileInfo = new STFileInfo("", "", DateTime.MinValue, 0L);
		this.ChartInfo = new STChartInfo();
		this.ChartInfo.Title = "";
		this.ChartInfo.ArtistName = "";
		this.ChartInfo.Comment = "";
		this.ChartInfo.Genre = "";
		this.ChartInfo.Preimage = "";
		this.ChartInfo.Premovie = "";
		this.ChartInfo.Presound = "";
		this.ChartInfo.Backgound = "";
		this.ChartInfo.Level = 0;
		this.ChartInfo.FullCombo = false;
		this.ChartInfo.PlayCount = 0;
		this.ChartInfo.PlayHistory = new STChartInfo.STHISTORY();
		this.ChartInfo.PlayHistory.Line1 = "";
		this.ChartInfo.PlayHistory.Line2 = "";
		this.ChartInfo.PlayHistory.Line3 = "";
		this.ChartInfo.PlayHistory.Line4 = "";
		this.ChartInfo.PlayHistory.Line5 = "";
		this.ChartInfo.PlayHistory.Line6 = "";
		this.ChartInfo.PlayHistory.Line7 = "";
		this.ChartInfo.LevelHide = false;
		this.ChartInfo.MaxSkill = new STChartInfo.STSKILL();
		this.ChartInfo.Bpm = 120.0;
		this.ChartInfo.MinBpm = 120.0;
		this.ChartInfo.MaxBpm = 120.0;
		this.ChartInfo.Duration = 0;
		this.ChartInfo.strBGMFileName = "";
		this.ChartInfo.SongVol = CSound.DefaultSongVol;
		this.ChartInfo.SongLoudnessMetadata = null;
		this.ChartInfo.nDemoBGMOffset = 0;
		this.ChartInfo.bChartBranch = new bool[(int)Difficulty.Total];
		this.ChartInfo.HighScore = 0;
		this.ChartInfo.nHighScore = new int[(int)Difficulty.Total];
		this.ChartInfo.strSubtitle = "";
		this.ChartInfo.nLevel = new int[(int)Difficulty.Total] { -1, -1, -1, -1, -1, -1, -1 };
		this.ChartInfo.nLevelIcon = new CTja.ELevelIcon[(int)Difficulty.Total] { CTja.ELevelIcon.eNone, CTja.ELevelIcon.eNone, CTja.ELevelIcon.eNone, CTja.ELevelIcon.eNone, CTja.ELevelIcon.eNone, CTja.ELevelIcon.eNone, CTja.ELevelIcon.eNone };
		this.ChartInfo.nClear = new int[5];
		this.ChartInfo.nScoreRank = new int[5];
		this.ChartInfo.nExamResult = new List<int[]> { };
		this.ChartInfo.nLife = 5;
		this.ChartInfo.nTotalFloor = 140;
	}
}
