using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using FDK;
using System.Drawing;

namespace TJAPlayer3
{
	[Serializable]
	internal class Cスコア
	{
		// プロパティ

		public STScoreIni情報 ScoreIni情報;
		[Serializable]
		[StructLayout( LayoutKind.Sequential )]
		public struct STScoreIni情報
		{
			public DateTime 最終更新日時;
			public long ファイルサイズ;

			public STScoreIni情報( DateTime 最終更新日時, long ファイルサイズ )
			{
				this.最終更新日時 = 最終更新日時;
				this.ファイルサイズ = ファイルサイズ;
			}
		}

		public STファイル情報 ファイル情報;
		[Serializable]
		[StructLayout( LayoutKind.Sequential )]
		public struct STファイル情報
		{
			public string ファイルの絶対パス;
			public string フォルダの絶対パス;
			public DateTime 最終更新日時;
			public long ファイルサイズ;

			public STファイル情報( string ファイルの絶対パス, string フォルダの絶対パス, DateTime 最終更新日時, long ファイルサイズ )
			{
				this.ファイルの絶対パス = ファイルの絶対パス;
				this.フォルダの絶対パス = フォルダの絶対パス;
				this.最終更新日時 = 最終更新日時;
				this.ファイルサイズ = ファイルサイズ;
			}
		}

		public ST譜面情報 譜面情報;

		// Smaller version of ST譜面情報 to keep the main info for each player (High scores, clear status, score ranks
		public STGamePlayInformations[] GPInfo = new STGamePlayInformations[5];

		[Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct STGamePlayInformations
        {
			public int[] nHighScore;
			public int[] nClear;      //0:未クリア 1:クリア 2:フルコンボ 3:ドンダフルコンボ
			public int[] nScoreRank;  //0:未取得 1:白粋 2:銅粋 3:銀粋 4:金雅 5:桃雅 6:紫雅 7:虹極
		}

		[Serializable]
		[StructLayout( LayoutKind.Sequential )]
		public struct ST譜面情報
		{
			public string タイトル;
			public string アーティスト名;
			public string コメント;
			public string ジャンル;
			public string Preimage;
			public string Premovie;
			public string Presound;
			public string Backgound;
			public STDGBVALUE<int> レベル;
			public STRANK 最大ランク;
			public STSKILL 最大スキル;
			public STDGBVALUE<bool> フルコンボ;
			public STDGBVALUE<int> 演奏回数;
			public STHISTORY 演奏履歴;
			public bool レベルを非表示にする;
			public CDTX.E種別 曲種別;
			public double Bpm;
			public double BaseBpm;
			public double MinBpm;
			public double MaxBpm;
			public int Duration;
            public string strBGMファイル名;
            public int SongVol;
		    public LoudnessMetadata? SongLoudnessMetadata;
            public int nデモBGMオフセット;
            public bool[] b譜面分岐;
            public int ハイスコア;
            public int[] nハイスコア;
            public string strサブタイトル;
            public int[] nレベル;
			public int[] nクリア;		//0:未クリア 1:クリア 2:フルコンボ 3:ドンダフルコンボ
			public int[] nスコアランク;  //0:未取得 1:白粋 2:銅粋 3:銀粋 4:金雅 5:桃雅 6:紫雅 7:虹極
			public CDTX.ELevelIcon[] nLevelIcon;

			// Tower lifes
			public int nLife;
			public int nTotalFloor;
			public int nTowerType;

			public int nDanTick;
			public Color cDanTickColor;

			public List<int[]> nExamResult;

			[Serializable]
			[StructLayout( LayoutKind.Sequential )]
			public struct STHISTORY
			{
				public string 行1;
				public string 行2;
				public string 行3;
				public string 行4;
				public string 行5;
                public string 行6;
                public string 行7;
                public string this[ int index ]
				{
					get
					{
						switch( index )
						{
							case 0:
								return this.行1;

							case 1:
								return this.行2;

							case 2:
								return this.行3;

							case 3:
								return this.行4;

							case 4:
								return this.行5;
                            case 5:
                                return this.行6;
                            case 6:
                                return this.行7;
						}
						throw new IndexOutOfRangeException();
					}
					set
					{
						switch( index )
						{
							case 0:
								this.行1 = value;
								return;

							case 1:
								this.行2 = value;
								return;

							case 2:
								this.行3 = value;
								return;

							case 3:
								this.行4 = value;
								return;

							case 4:
								this.行5 = value;
								return;
                            case 5:
                                this.行6 = value;
                                return;
                            case 6:
                                this.行7 = value;
                                return;
						}
						throw new IndexOutOfRangeException();
					}
				}
			}

			[Serializable]
			[StructLayout( LayoutKind.Sequential )]
			public struct STRANK
			{
				public int Drums;
				public int Guitar;
				public int Bass;
				public int this[ int index ]
				{
					get
					{
						switch( index )
						{
							case 0:
								return this.Drums;

							case 1:
								return this.Guitar;

							case 2:
								return this.Bass;
						}
						throw new IndexOutOfRangeException();
					}
					set
					{
						if ( ( value < (int)CScoreIni.ERANK.SS ) || ( ( value != (int)CScoreIni.ERANK.UNKNOWN ) && ( value > (int)CScoreIni.ERANK.E ) ) )
						{
							throw new ArgumentOutOfRangeException();
						}
						switch( index )
						{
							case 0:
								this.Drums = value;
								return;

							case 1:
								this.Guitar = value;
								return;

							case 2:
								this.Bass = value;
								return;
						}
						throw new IndexOutOfRangeException();
					}
				}
			}

			[Serializable]
			[StructLayout( LayoutKind.Sequential )]
			public struct STSKILL
			{
				public double Drums;
				public double Guitar;
				public double Bass;
				public double this[ int index ]
				{
					get
					{
						switch( index )
						{
							case 0:
								return this.Drums;

							case 1:
								return this.Guitar;

							case 2:
								return this.Bass;
						}
						throw new IndexOutOfRangeException();
					}
					set
					{
						if( ( value < 0.0 ) || ( value > 100.0 ) )
						{
							throw new ArgumentOutOfRangeException();
						}
						switch( index )
						{
							case 0:
								this.Drums = value;
								return;

							case 1:
								this.Guitar = value;
								return;

							case 2:
								this.Bass = value;
								return;
						}
						throw new IndexOutOfRangeException();
					}
				}
			}
		}

		public bool bSongDBにキャッシュがあった;
		public bool bスコアが有効である
		{
			get
			{
				return ( ( ( this.譜面情報.レベル[ 0 ] + this.譜面情報.レベル[ 1 ] ) + this.譜面情報.レベル[ 2 ] ) != 0 );
			}
		}


		// コンストラクタ

		public Cスコア()
		{
			this.ScoreIni情報 = new STScoreIni情報( DateTime.MinValue, 0L );
			this.bSongDBにキャッシュがあった = false;
			this.ファイル情報 = new STファイル情報( "", "", DateTime.MinValue, 0L );
			this.譜面情報 = new ST譜面情報();
			this.譜面情報.タイトル = "";
			this.譜面情報.アーティスト名 = "";
			this.譜面情報.コメント = "";
			this.譜面情報.ジャンル = "";
			this.譜面情報.Preimage = "";
			this.譜面情報.Premovie = "";
			this.譜面情報.Presound = "";
			this.譜面情報.Backgound = "";
			this.譜面情報.レベル = new STDGBVALUE<int>();
			this.譜面情報.最大ランク = new ST譜面情報.STRANK();
			this.譜面情報.最大ランク.Drums =  (int)CScoreIni.ERANK.UNKNOWN;
			this.譜面情報.最大ランク.Guitar = (int)CScoreIni.ERANK.UNKNOWN;
			this.譜面情報.最大ランク.Bass =   (int)CScoreIni.ERANK.UNKNOWN;
			this.譜面情報.フルコンボ = new STDGBVALUE<bool>();
			this.譜面情報.演奏回数 = new STDGBVALUE<int>();
			this.譜面情報.演奏履歴 = new ST譜面情報.STHISTORY();
			this.譜面情報.演奏履歴.行1 = "";
			this.譜面情報.演奏履歴.行2 = "";
			this.譜面情報.演奏履歴.行3 = "";
			this.譜面情報.演奏履歴.行4 = "";
			this.譜面情報.演奏履歴.行5 = "";
            this.譜面情報.演奏履歴.行6 = "";
            this.譜面情報.演奏履歴.行7 = "";
            this.譜面情報.レベルを非表示にする = false;
			this.譜面情報.最大スキル = new ST譜面情報.STSKILL();
			this.譜面情報.曲種別 = CDTX.E種別.DTX;
			this.譜面情報.Bpm = 120.0;
			this.譜面情報.MinBpm = 120.0;
			this.譜面情報.MaxBpm = 120.0;
			this.譜面情報.Duration = 0;
            this.譜面情報.strBGMファイル名 = "";
            this.譜面情報.SongVol = CSound.DefaultSongVol;
            this.譜面情報.SongLoudnessMetadata = null;
            this.譜面情報.nデモBGMオフセット = 0;
            this.譜面情報.b譜面分岐 = new bool[(int)Difficulty.Total];
            this.譜面情報.ハイスコア = 0;
            this.譜面情報.nハイスコア = new int[(int)Difficulty.Total];
            this.譜面情報.strサブタイトル = "";
            this.譜面情報.nレベル = new int[(int)Difficulty.Total] { -1, -1, -1, -1, -1, -1, -1};
            this.譜面情報.nLevelIcon = new CDTX.ELevelIcon[(int)Difficulty.Total] { CDTX.ELevelIcon.eNone, CDTX.ELevelIcon.eNone, CDTX.ELevelIcon.eNone, CDTX.ELevelIcon.eNone, CDTX.ELevelIcon.eNone, CDTX.ELevelIcon.eNone, CDTX.ELevelIcon.eNone };
            this.譜面情報.nクリア = new int[5];
			this.譜面情報.nスコアランク = new int[5];
		
			for (int i = 0; i < 5; i++)
            {
				this.GPInfo[i].nHighScore = new int[(int)Difficulty.Total];
				this.GPInfo[i].nClear = new int[5];
				this.GPInfo[i].nScoreRank = new int[5];
			}

			this.譜面情報.nExamResult = new List<int[]> { };
			//for (int i = 0; i < TJAPlayer3.stage選曲.r確定された曲.DanSongs.Count; i++)
			//{
			//	譜面情報.nExamResult.Add(new int[CExamInfo.cMaxExam]);
			//}

			this.譜面情報.nLife = 5;
			this.譜面情報.nTotalFloor = 140;
		}
	}
}
