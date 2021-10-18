using System;
using System.Collections.Generic;
using System.Drawing;

namespace TJAPlayer3
{
	[Serializable]
	internal class C曲リストノード
	{
		// プロパティ

		public Eノード種別 eノード種別 = Eノード種別.UNKNOWN;
		public enum Eノード種別
		{
			SCORE,
			SCORE_MIDI,
			BOX,
			BACKBOX,
			RANDOM,
			UNKNOWN
		}
		public int nID { get; private set; }
		public Cスコア[] arスコア = new Cスコア[(int)Difficulty.Total];
		public string[] ar難易度ラベル = new string[(int)Difficulty.Total];
		public bool bDTXFilesで始まるフォルダ名のBOXである;
		public bool bBoxDefで作成されたBOXである
		{
			get
			{
				return !this.bDTXFilesで始まるフォルダ名のBOXである;
			}
			set
			{
				this.bDTXFilesで始まるフォルダ名のBOXである = !value;
			}
		}
		public Color col文字色 = Color.White;
        public Color ForeColor = Color.White;
        public Color BackColor = Color.Black;
		public Color BoxColor = Color.White;
        public bool IsChangedForeColor;
        public bool IsChangedBackColor;
		public bool isChangedBoxColor;
		public List<C曲リストノード> listランダム用ノードリスト;
		public List<C曲リストノード> list子リスト;
		public int nGood範囲ms = -1;
		public int nGreat範囲ms = -1;
		public int nPerfect範囲ms = -1;
		public int nPoor範囲ms = -1;
		public int nスコア数;
		public C曲リストノード r親ノード;
		public int Openindex;
		public Stack<int> stackランダム演奏番号 = new Stack<int>();
		public string strジャンル = "";
		public string str本当のジャンル = "";
		public string strタイトル = "";
		public List<CDTX.DanSongs> DanSongs;
		public Dan_C[] Dan_C;
		public string strサブタイトル = "";
		public string strBreadcrumbs = "";		// #27060 2011.2.27 yyagi; MUSIC BOXのパンくずリスト (曲リスト構造内の絶対位置捕捉のために使う)
		public string strSkinPath = "";			// #28195 2012.5.4 yyagi; box.defでのスキン切り替え対応
        public bool bBranch = false;
        public int[] nLevel = new int[(int)Difficulty.Total]{ 0, 0, 0, 0, 0, 0, 0 };
		
		// Tower Lives
		public int nLife = 5;
		public int nTotalFloor = 140;
		
		public string[] strBoxText = new string[3];
        public Eジャンル eジャンル = Eジャンル.None;
		
		// コンストラクタ

		public C曲リストノード()
		{
			this.nID = id++;
		}

		public C曲リストノード Clone()
		{
			return (C曲リストノード)MemberwiseClone();
		}


		// その他

		#region [ private ]
		//-----------------
		private static int id;
		//-----------------
		#endregion
	}
}
