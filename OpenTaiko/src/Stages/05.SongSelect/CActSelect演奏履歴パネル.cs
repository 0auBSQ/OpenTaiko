using System.Diagnostics;
using System.Runtime.InteropServices;
using FDK;
using Silk.NET.Maths;
using SkiaSharp;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko;

internal class CActSelect演奏履歴パネル : CActivity {
	// メソッド

	public CActSelect演奏履歴パネル() {
		STTextPosition[] stTextPositionArray = new STTextPosition[10];

		STTextPosition stTextPosition = new STTextPosition();
		stTextPosition.ch = '0';
		stTextPosition.pt = new Point(0, 0);
		stTextPositionArray[0] = stTextPosition;
		STTextPosition stTextPosition2 = new STTextPosition();
		stTextPosition2.ch = '1';
		stTextPosition2.pt = new Point(26, 0);
		stTextPositionArray[1] = stTextPosition2;
		STTextPosition stTextPosition3 = new STTextPosition();
		stTextPosition3.ch = '2';
		stTextPosition3.pt = new Point(52, 0);
		stTextPositionArray[2] = stTextPosition3;
		STTextPosition stTextPosition4 = new STTextPosition();
		stTextPosition4.ch = '3';
		stTextPosition4.pt = new Point(78, 0);
		stTextPositionArray[3] = stTextPosition4;
		STTextPosition stTextPosition5 = new STTextPosition();
		stTextPosition5.ch = '4';
		stTextPosition5.pt = new Point(104, 0);
		stTextPositionArray[4] = stTextPosition5;
		STTextPosition stTextPosition6 = new STTextPosition();
		stTextPosition6.ch = '5';
		stTextPosition6.pt = new Point(130, 0);
		stTextPositionArray[5] = stTextPosition6;
		STTextPosition stTextPosition7 = new STTextPosition();
		stTextPosition7.ch = '6';
		stTextPosition7.pt = new Point(156, 0);
		stTextPositionArray[6] = stTextPosition7;
		STTextPosition stTextPosition8 = new STTextPosition();
		stTextPosition8.ch = '7';
		stTextPosition8.pt = new Point(182, 0);
		stTextPositionArray[7] = stTextPosition8;
		STTextPosition stTextPosition9 = new STTextPosition();
		stTextPosition9.ch = '8';
		stTextPosition9.pt = new Point(208, 0);
		stTextPositionArray[8] = stTextPosition9;
		STTextPosition stTextPosition10 = new STTextPosition();
		stTextPosition10.ch = '9';
		stTextPosition10.pt = new Point(234, 0);
		stTextPositionArray[9] = stTextPosition10;
		this.stSmallPosition = stTextPositionArray;

		base.IsDeActivated = true;
	}
	public void t選択曲が変更された() {
		CScore cスコア = OpenTaiko.SongMount.rCurrentScore;
		if ((cスコア != null) && !OpenTaiko.stageSongSelect.bCurrentlyScrolling) {
			try {
				foreach (var item in txStringPanel) {
					item.Dispose();
				}
				txStringPanel.Clear();
				for (int i = 0; i < (int)Difficulty.Total; i++) {
					SKBitmap image = ft表示用フォント.DrawText(cスコア.ChartInfo.PlayHistory[i], Color.Yellow);
					var tex = new CTexture(image);
					tex.vcScaleRatio = new Vector3D<float>(0.5f, 0.5f, 1f);
					this.txStringPanel.Add(tex);
					image.Dispose();
				}
			} catch (CTextureCreateFailedException e) {
				Trace.TraceError(e.ToString());
				Trace.TraceError("演奏履歴文字列テクスチャの作成に失敗しました。");
				this.txStringPanel = null;
			}
		}
	}


	// CActivity 実装

	public override void Activate() {
		this.n本体X = 810;
		this.n本体Y = 558;
		base.Activate();
	}
	public override void DeActivate() {
		this.ct登場アニメ用 = null;
		base.DeActivate();
	}
	public override void CreateManagedResource() {
		this.ft表示用フォント = new CCachedFontRenderer("Arial", 30, CFontRenderer.FontStyle.Bold);

		//this.txパネル本体 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\5_play history panel.png" ) );
		//this.txスコアボード[0] = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\5_scoreboard_0.png" ) );
		//this.txスコアボード[1] = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\5_scoreboard_1.png" ) );
		//this.txスコアボード[2] = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\5_scoreboard_2.png" ) );
		//this.txスコアボード[3] = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\5_scoreboard_3.png" ) );
		//this.tx文字 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\5_scoreboard_number.png" ) );

		this.t選択曲が変更された();
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		if (this.ft表示用フォント != null) {
			this.ft表示用フォント.Dispose();
			this.ft表示用フォント = null;
		}
		//CDTXMania.tテクスチャの解放( ref this.txパネル本体 );
		//CDTXMania.tテクスチャの解放( ref this.tx文字列パネル );
		//            CDTXMania.tテクスチャの解放( ref this.txスコアボード[0] );
		//            CDTXMania.tテクスチャの解放( ref this.txスコアボード[1] );
		//            CDTXMania.tテクスチャの解放( ref this.txスコアボード[2] );
		//            CDTXMania.tテクスチャの解放( ref this.txスコアボード[3] );
		//            CDTXMania.tテクスチャの解放( ref this.tx文字 );
		base.ReleaseManagedResource();
	}
	public override int Draw() {
		if (!base.IsDeActivated) {
			if (base.IsFirstDraw) {
				this.ct登場アニメ用 = new CCounter(0, 3000, 1, OpenTaiko.Timer);
				base.IsFirstDraw = false;
			}
			this.ct登場アニメ用.Tick();
			int x = 980;
			int y = 350;
			if (OpenTaiko.SongMount.rCurrentScore != null && this.ct登場アニメ用.CurrentValue >= 2000 && OpenTaiko.SongMount.rCurrentlySelectedSong.nodeType == CSongListNode.ENodeType.SCORE) {
				//CDTXMania.Tx.SongSelect_ScoreWindow_Text.n透明度 = ct登場アニメ用.n現在の値 - 1745;
				if (OpenTaiko.Tx.SongSelect_ScoreWindow[OpenTaiko.SongMount.nCurrentSongDifficulty] != null) {
					//CDTXMania.Tx.SongSelect_ScoreWindow[CDTXMania.stage選曲.n現在選択中の曲の難易度].n透明度 = ct登場アニメ用.n現在の値 - 1745;
					OpenTaiko.Tx.SongSelect_ScoreWindow[OpenTaiko.SongMount.nCurrentSongDifficulty].t2DDraw(x, y);
					this.tSmallDisplay(x + 56, y + 160, string.Format("{0,7:######0}", OpenTaiko.SongMount.rCurrentScore.ChartInfo.nHighScore[OpenTaiko.SongMount.nCurrentSongDifficulty].ToString()));
					OpenTaiko.Tx.SongSelect_ScoreWindow_Text.t2DDraw(x + 236, y + 166, new Rectangle(0, 36, 32, 30));
				}
			}
		}
		return 0;
	}


	// その他

	#region [ private ]
	//-----------------
	private CCounter ct登場アニメ用;
	private CCounter ctスコアボード登場アニメ;
	private CCachedFontRenderer ft表示用フォント;
	private int n本体X;
	private int n本体Y;
	//private CTexture txパネル本体;
	private List<CTexture> txStringPanel = new();
	//      private CTexture[] txスコアボード = new CTexture[4];
	//      private CTexture tx文字;
	//-----------------

	[StructLayout(LayoutKind.Sequential)]
	private struct STTextPosition {
		public char ch;
		public Point pt;
	}
	private readonly STTextPosition[] stSmallPosition;
	private void tSmallDisplay(int x, int y, string str) {
		foreach (char ch in str) {
			for (int i = 0; i < this.stSmallPosition.Length; i++) {
				if (this.stSmallPosition[i].ch == ch) {
					Rectangle rectangle = new Rectangle(this.stSmallPosition[i].pt.X, this.stSmallPosition[i].pt.Y, 26, 36);
					if (OpenTaiko.Tx.SongSelect_ScoreWindow_Text != null) {
						OpenTaiko.Tx.SongSelect_ScoreWindow_Text.t2DDraw(x, y, rectangle);
					}
					break;
				}
			}
			x += 26;
		}
	}

	public void tSongChange() {
		this.ct登場アニメ用 = new CCounter(0, 3000, 1, OpenTaiko.Timer);
	}


	#endregion
}
