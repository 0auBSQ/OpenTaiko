using System.Diagnostics;
using System.Runtime.InteropServices;
using FDK;
using Silk.NET.Maths;
using SkiaSharp;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace TJAPlayer3 {
	internal class CActSelect演奏履歴パネル : CActivity {
		// メソッド

		public CActSelect演奏履歴パネル() {
			ST文字位置[] st文字位置Array = new ST文字位置[10];

			ST文字位置 st文字位置 = new ST文字位置();
			st文字位置.ch = '0';
			st文字位置.pt = new Point(0, 0);
			st文字位置Array[0] = st文字位置;
			ST文字位置 st文字位置2 = new ST文字位置();
			st文字位置2.ch = '1';
			st文字位置2.pt = new Point(26, 0);
			st文字位置Array[1] = st文字位置2;
			ST文字位置 st文字位置3 = new ST文字位置();
			st文字位置3.ch = '2';
			st文字位置3.pt = new Point(52, 0);
			st文字位置Array[2] = st文字位置3;
			ST文字位置 st文字位置4 = new ST文字位置();
			st文字位置4.ch = '3';
			st文字位置4.pt = new Point(78, 0);
			st文字位置Array[3] = st文字位置4;
			ST文字位置 st文字位置5 = new ST文字位置();
			st文字位置5.ch = '4';
			st文字位置5.pt = new Point(104, 0);
			st文字位置Array[4] = st文字位置5;
			ST文字位置 st文字位置6 = new ST文字位置();
			st文字位置6.ch = '5';
			st文字位置6.pt = new Point(130, 0);
			st文字位置Array[5] = st文字位置6;
			ST文字位置 st文字位置7 = new ST文字位置();
			st文字位置7.ch = '6';
			st文字位置7.pt = new Point(156, 0);
			st文字位置Array[6] = st文字位置7;
			ST文字位置 st文字位置8 = new ST文字位置();
			st文字位置8.ch = '7';
			st文字位置8.pt = new Point(182, 0);
			st文字位置Array[7] = st文字位置8;
			ST文字位置 st文字位置9 = new ST文字位置();
			st文字位置9.ch = '8';
			st文字位置9.pt = new Point(208, 0);
			st文字位置Array[8] = st文字位置9;
			ST文字位置 st文字位置10 = new ST文字位置();
			st文字位置10.ch = '9';
			st文字位置10.pt = new Point(234, 0);
			st文字位置Array[9] = st文字位置10;
			this.st小文字位置 = st文字位置Array;

			base.IsDeActivated = true;
		}
		public void t選択曲が変更された() {
			Cスコア cスコア = TJAPlayer3.stageSongSelect.r現在選択中のスコア;
			if ((cスコア != null) && !TJAPlayer3.stageSongSelect.bCurrentlyScrolling) {
				try {
					foreach (var item in tx文字列パネル) {
						item.Dispose();
					}
					tx文字列パネル.Clear();
					for (int i = 0; i < (int)Difficulty.Total; i++) {
						SKBitmap image = ft表示用フォント.DrawText(cスコア.譜面情報.演奏履歴[i], Color.Yellow);
						var tex = new CTexture(image);
						tex.vcScaleRatio = new Vector3D<float>(0.5f, 0.5f, 1f);
						this.tx文字列パネル.Add(tex);
						image.Dispose();
					}
				} catch (CTextureCreateFailedException e) {
					Trace.TraceError(e.ToString());
					Trace.TraceError("演奏履歴文字列テクスチャの作成に失敗しました。");
					this.tx文字列パネル = null;
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
					this.ct登場アニメ用 = new CCounter(0, 3000, 1, TJAPlayer3.Timer);
					base.IsFirstDraw = false;
				}
				this.ct登場アニメ用.Tick();
				int x = 980;
				int y = 350;
				if (TJAPlayer3.stageSongSelect.r現在選択中のスコア != null && this.ct登場アニメ用.CurrentValue >= 2000 && TJAPlayer3.stageSongSelect.rNowSelectedSong.eノード種別 == CSongListNode.ENodeType.SCORE) {
					//CDTXMania.Tx.SongSelect_ScoreWindow_Text.n透明度 = ct登場アニメ用.n現在の値 - 1745;
					if (TJAPlayer3.Tx.SongSelect_ScoreWindow[TJAPlayer3.stageSongSelect.n現在選択中の曲の難易度] != null) {
						//CDTXMania.Tx.SongSelect_ScoreWindow[CDTXMania.stage選曲.n現在選択中の曲の難易度].n透明度 = ct登場アニメ用.n現在の値 - 1745;
						TJAPlayer3.Tx.SongSelect_ScoreWindow[TJAPlayer3.stageSongSelect.n現在選択中の曲の難易度].t2D描画(x, y);
						this.t小文字表示(x + 56, y + 160, string.Format("{0,7:######0}", TJAPlayer3.stageSongSelect.r現在選択中のスコア.譜面情報.nハイスコア[TJAPlayer3.stageSongSelect.n現在選択中の曲の難易度].ToString()));
						TJAPlayer3.Tx.SongSelect_ScoreWindow_Text.t2D描画(x + 236, y + 166, new Rectangle(0, 36, 32, 30));
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
		private List<CTexture> tx文字列パネル = new();
		//      private CTexture[] txスコアボード = new CTexture[4];
		//      private CTexture tx文字;
		//-----------------

		[StructLayout(LayoutKind.Sequential)]
		private struct ST文字位置 {
			public char ch;
			public Point pt;
		}
		private readonly ST文字位置[] st小文字位置;
		private void t小文字表示(int x, int y, string str) {
			foreach (char ch in str) {
				for (int i = 0; i < this.st小文字位置.Length; i++) {
					if (this.st小文字位置[i].ch == ch) {
						Rectangle rectangle = new Rectangle(this.st小文字位置[i].pt.X, this.st小文字位置[i].pt.Y, 26, 36);
						if (TJAPlayer3.Tx.SongSelect_ScoreWindow_Text != null) {
							TJAPlayer3.Tx.SongSelect_ScoreWindow_Text.t2D描画(x, y, rectangle);
						}
						break;
					}
				}
				x += 26;
			}
		}

		public void tSongChange() {
			this.ct登場アニメ用 = new CCounter(0, 3000, 1, TJAPlayer3.Timer);
		}


		#endregion
	}
}
