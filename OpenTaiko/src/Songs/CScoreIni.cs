using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using FDK;
using TJAPlayer3;

namespace TJAPlayer3
{
	[Serializable]
	public class CScoreIni
	{
		// プロパティ

		[Serializable]
		public class C演奏記録
		{
			public STAUTOPLAY bAutoPlay;
			public bool bDrums有効;
			public bool bGuitar有効;
			public STDGBVALUE<bool> bHidden;
			public STDGBVALUE<bool> bLeft;
			public STDGBVALUE<bool> bLight;
			public STDGBVALUE<bool> bReverse;
			public bool bSTAGEFAILED有効;
			public STDGBVALUE<bool> bSudden;
			public STDGBVALUE<EInvisible> eInvisible;
			public bool bTight;
			public bool b演奏にMIDI入力を使用した;
			public bool b演奏にキーボードを使用した;
			public bool b演奏にジョイパッドを使用した;
			public bool b演奏にマウスを使用した;
			public double dbゲーム型スキル値;
			public double db演奏型スキル値;
			public Eダークモード eDark;
			public STDGBVALUE<Eランダムモード> eRandom;
			public Eダメージレベル eダメージレベル;
			public STDGBVALUE<float> f譜面スクロール速度;
			public string Hash;
			public int nGoodになる範囲ms;
			public int nGood数;
			public int nGreatになる範囲ms;
			public int nGreat数;
			public int nMiss数;
			public int nPerfectになる範囲ms;
			public int nPerfect数;
			public int nPoorになる範囲ms;
			public int nPoor数;
			public int nPerfect数_Auto含まない;
			public int nGreat数_Auto含まない;
			public int nGood数_Auto含まない;
			public int nPoor数_Auto含まない;
			public int nMiss数_Auto含まない;
			public long nスコア;
            public int n連打数;
			public int n演奏速度分子;
			public int n演奏速度分母;
			public int n最大コンボ数;
			public int n全チップ数;
			public string strDTXManiaのバージョン;
			public bool レーン9モード;
			public int nRisky;		// #23559 2011.6.20 yyagi 0=OFF, 1-10=Risky
			public string 最終更新日時;
            public float fゲージ;
            public int[] n良 = new int[(int)Difficulty.Total];
            public int[] n可 = new int[(int)Difficulty.Total];
            public int[] n不可 = new int[(int)Difficulty.Total];
            public int[] n連打 = new int[(int)Difficulty.Total];
            public int[] nハイスコア = new int[(int)Difficulty.Total];
            public Dan_C[] Dan_C;
			public int[] nクリア;		//0:未クリア 1:クリア 2:フルコンボ 3:ドンダフルコンボ
			public int[] nスコアランク;  //0:未取得 1:白粋 2:銅粋 3:銀粋 4:金雅 5:桃雅 6:紫雅 7:虹極
			public List<int[]> nExamResult; //

			public C演奏記録()
			{
				this.bAutoPlay = new STAUTOPLAY();
				this.bAutoPlay.LC = false;
				this.bAutoPlay.HH = false;
				this.bAutoPlay.SD = false;
				this.bAutoPlay.BD = false;
				this.bAutoPlay.HT = false;
				this.bAutoPlay.LT = false;
				this.bAutoPlay.FT = false;
				this.bAutoPlay.CY = false;
				this.bAutoPlay.Guitar = false;
				this.bAutoPlay.Bass = false;
				this.bAutoPlay.GtR = false;
				this.bAutoPlay.GtG = false;
				this.bAutoPlay.GtB = false;
				this.bAutoPlay.GtPick = false;
				this.bAutoPlay.GtW = false;
				this.bAutoPlay.BsR = false;
				this.bAutoPlay.BsG = false;
				this.bAutoPlay.BsB = false;
				this.bAutoPlay.BsPick = false;
				this.bAutoPlay.BsW = false;

				this.bSudden = new STDGBVALUE<bool>();
				this.bSudden.Drums = false;
				this.bSudden.Guitar = false;
				this.bSudden.Bass = false;
				this.bHidden = new STDGBVALUE<bool>();
				this.bHidden.Drums = false;
				this.bHidden.Guitar = false;
				this.bHidden.Bass = false;
				this.eInvisible = new STDGBVALUE<EInvisible>();
				this.eInvisible.Drums = EInvisible.OFF;
				this.eInvisible.Guitar = EInvisible.OFF;
				this.eInvisible.Bass = EInvisible.OFF;
				this.bReverse = new STDGBVALUE<bool>();
				this.bReverse.Drums = false;
				this.bReverse.Guitar = false;
				this.bReverse.Bass = false;
				this.eRandom = new STDGBVALUE<Eランダムモード>();
				this.eRandom.Drums = Eランダムモード.OFF;
				this.eRandom.Guitar = Eランダムモード.OFF;
				this.eRandom.Bass = Eランダムモード.OFF;
				this.bLight = new STDGBVALUE<bool>();
				this.bLight.Drums = false;
				this.bLight.Guitar = false;
				this.bLight.Bass = false;
				this.bLeft = new STDGBVALUE<bool>();
				this.bLeft.Drums = false;
				this.bLeft.Guitar = false;
				this.bLeft.Bass = false;
				this.f譜面スクロール速度 = new STDGBVALUE<float>();
				this.f譜面スクロール速度.Drums = 1f;
				this.f譜面スクロール速度.Guitar = 1f;
				this.f譜面スクロール速度.Bass = 1f;
				this.n演奏速度分子 = 20;
				this.n演奏速度分母 = 20;
				this.bGuitar有効 = true;
				this.bDrums有効 = true;
				this.bSTAGEFAILED有効 = true;
				this.eダメージレベル = Eダメージレベル.普通;
				this.nPerfectになる範囲ms = 34;
				this.nGreatになる範囲ms = 67;
				this.nGoodになる範囲ms = 84;
				this.nPoorになる範囲ms = 117;
				this.strDTXManiaのバージョン = "Unknown";
				this.最終更新日時 = "";
				this.Hash = "00000000000000000000000000000000";
				this.レーン9モード = true;
				this.nRisky = 0;									// #23559 2011.6.20 yyagi
                this.fゲージ = 0.0f;
				this.nクリア = new int[5];
				this.nスコアランク = new int[5];
                Dan_C = new Dan_C[CExamInfo.cMaxExam];
				this.nExamResult = new List<int[]> { };
			}

		}

	}
}
