using System.Diagnostics;
using FDK;
using Silk.NET.Maths;
using Rectangle = System.Drawing.Rectangle;

namespace TJAPlayer3 {
	internal class CActSelectPreimageパネル : CActivity {
		// メソッド

		public CActSelectPreimageパネル() {
			base.IsDeActivated = true;
		}
		public void t選択曲が変更された() {
			this.ct遅延表示 = new CCounter(-TJAPlayer3.ConfigIni.n曲が選択されてからプレビュー画像が表示開始されるまでのウェイトms, 100, 1, TJAPlayer3.Timer);
			this.b新しいプレビューファイルを読み込んだ = false;
		}

		// CActivity 実装

		public override void Activate() {
			this.n本体X = 8;
			this.n本体Y = 0x39;
			this.r表示するプレビュー画像 = this.txプレビュー画像がないときの画像;
			this.str現在のファイル名 = "";
			this.b新しいプレビューファイルを読み込んだ = false;
			this.txプレビュー画像 = null;
			this.n前回描画したフレーム番号 = -1;
			this.b動画フレームを作成した = false;
			this.pAVIBmp = IntPtr.Zero;
			this.tプレビュー画像_動画の変更();
			base.Activate();
		}
		public override void DeActivate() {
			TJAPlayer3.tテクスチャの解放(ref this.txプレビュー画像);
			this.ct登場アニメ用 = null;
			this.ct遅延表示 = null;
			base.DeActivate();
		}
		public override void CreateManagedResource() {
			this.txパネル本体 = TJAPlayer3.tテクスチャの生成(CSkin.Path(@$"Graphics{Path.DirectorySeparatorChar}5_preimage panel.png"), false);
			this.txセンサ = TJAPlayer3.tテクスチャの生成(CSkin.Path(@$"Graphics{Path.DirectorySeparatorChar}5_sensor.png"), false);
			//this.txセンサ光 = CDTXMania.tテクスチャの生成( CSkin.Path( @$"Graphics{Path.DirectorySeparatorChar}5_sensor light.png" ), false );
			this.txプレビュー画像がないときの画像 = TJAPlayer3.tテクスチャの生成(CSkin.Path(@$"Graphics{Path.DirectorySeparatorChar}3_SongSelect{Path.DirectorySeparatorChar}PreImageDefault.png"), false);
			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource() {
			TJAPlayer3.tテクスチャの解放(ref this.txパネル本体);
			TJAPlayer3.tテクスチャの解放(ref this.txセンサ);
			TJAPlayer3.tテクスチャの解放(ref this.txセンサ光);
			TJAPlayer3.tテクスチャの解放(ref this.txプレビュー画像がないときの画像);
			base.ReleaseManagedResource();
		}
		public override int Draw() {
			if (!base.IsDeActivated) {
				if (base.IsFirstDraw) {
					this.ct登場アニメ用 = new CCounter(0, 100, 5, TJAPlayer3.Timer);
					this.ctセンサ光 = new CCounter(0, 100, 30, TJAPlayer3.Timer);
					this.ctセンサ光.CurrentValue = 70;
					base.IsFirstDraw = false;
				}
				this.ct登場アニメ用.Tick();
				this.ctセンサ光.TickLoop();
				if ((!TJAPlayer3.stageSongSelect.bCurrentlyScrolling && (this.ct遅延表示 != null)) && this.ct遅延表示.IsTicked) {
					this.ct遅延表示.Tick();
					if ((this.ct遅延表示.CurrentValue >= 0) && this.b新しいプレビューファイルをまだ読み込んでいない) {
						this.tプレビュー画像_動画の変更();
						TJAPlayer3.Timer.Update();
						this.ct遅延表示.NowTime = TJAPlayer3.Timer.NowTime;
						this.b新しいプレビューファイルを読み込んだ = true;
					} else if (this.ct遅延表示.IsEnded && this.ct遅延表示.IsTicked) {
						this.ct遅延表示.Stop();
					}
				}
				this.t描画処理_パネル本体();
				this.t描画処理_プレビュー画像();
			}
			return 0;
		}


		// その他

		#region [ private ]
		//-----------------
		private bool b動画フレームを作成した;
		private CCounter ctセンサ光;
		private CCounter ct遅延表示;
		private CCounter ct登場アニメ用;
		private int n前回描画したフレーム番号;
		private int n本体X;
		private int n本体Y;
		private IntPtr pAVIBmp;
		private readonly Rectangle rcセンサ光 = new Rectangle(0, 0xc0, 0x40, 0x40);
		private readonly Rectangle rcセンサ本体下半分 = new Rectangle(0x40, 0, 0x40, 0x80);
		private readonly Rectangle rcセンサ本体上半分 = new Rectangle(0, 0, 0x40, 0x80);
		private CTexture r表示するプレビュー画像;
		private string str現在のファイル名;
		private CTexture txセンサ;
		private CTexture txセンサ光;
		private CTexture txパネル本体;
		private CTexture txプレビュー画像;
		private CTexture txプレビュー画像がないときの画像;
		private bool b新しいプレビューファイルを読み込んだ;
		private bool b新しいプレビューファイルをまだ読み込んでいない {
			get {
				return !this.b新しいプレビューファイルを読み込んだ;
			}
			set {
				this.b新しいプレビューファイルを読み込んだ = !value;
			}
		}

		private void tプレビュー画像_動画の変更() {
			this.pAVIBmp = IntPtr.Zero;
			if (!TJAPlayer3.ConfigIni.bストイックモード) {
				if (this.tプレビュー画像の指定があれば構築する()) {
					return;
				}
			}
			this.r表示するプレビュー画像 = this.txプレビュー画像がないときの画像;
			this.str現在のファイル名 = "";
		}
		private bool tプレビュー画像の指定があれば構築する() {
			Cスコア cスコア = TJAPlayer3.stageSongSelect.r現在選択中のスコア;
			if ((cスコア == null) || string.IsNullOrEmpty(cスコア.譜面情報.Preimage)) return false;

			string str = ((!Path.IsPathRooted(cスコア.譜面情報.Preimage)) ? cスコア.ファイル情報.フォルダの絶対パス : "") + cスコア.譜面情報.Preimage;
			if (!str.Equals(this.str現在のファイル名)) {
				TJAPlayer3.tテクスチャの解放(ref this.txプレビュー画像);
				this.str現在のファイル名 = str;
				if (!File.Exists(this.str現在のファイル名)) {
					Trace.TraceWarning("ファイルが存在しません。({0})", new object[] { this.str現在のファイル名 });
					return false;
				}
				this.txプレビュー画像 = TJAPlayer3.tテクスチャの生成(this.str現在のファイル名, false);
				if (this.txプレビュー画像 != null) {
					this.r表示するプレビュー画像 = this.txプレビュー画像;
				} else {
					this.r表示するプレビュー画像 = this.txプレビュー画像がないときの画像;
				}
			}
			return true;
		}
		/// <summary>
		/// 一時的に使用禁止。
		/// </summary>
		private void t描画処理_ジャンル文字列() {
			CSongListNode c曲リストノード = TJAPlayer3.stageSongSelect.rNowSelectedSong;
			Cスコア cスコア = TJAPlayer3.stageSongSelect.r現在選択中のスコア;
			if ((c曲リストノード != null) && (cスコア != null)) {
				string str = "";
				switch (c曲リストノード.eノード種別) {
					case CSongListNode.ENodeType.SCORE:
						if ((c曲リストノード.strジャンル == null) || (c曲リストノード.strジャンル.Length <= 0)) {
							if ((cスコア.譜面情報.ジャンル != null) && (cスコア.譜面情報.ジャンル.Length > 0)) {
								str = cスコア.譜面情報.ジャンル;
							}
#if false  // #32644 2013.12.21 yyagi "Unknown"なジャンル表示を削除。DTX/BMSなどの種別表示もしない。                                                                     
							else
							{
								switch( cスコア.譜面情報.曲種別 )
								{
									case CDTX.E種別.DTX:
										str = "DTX";
										break;

									case CDTX.E種別.GDA:
										str = "GDA";
										break;

									case CDTX.E種別.G2D:
										str = "G2D";
										break;

									case CDTX.E種別.BMS:
										str = "BMS";
										break;

									case CDTX.E種別.BME:
										str = "BME";
										break;
								}
								str = "Unknown";
							}
#endif
							break;
						}
						str = c曲リストノード.strジャンル;
						break;

					case CSongListNode.ENodeType.SCORE_MIDI:
						str = "MIDI";
						break;

					case CSongListNode.ENodeType.BOX:
						str = "MusicBox";
						break;

					case CSongListNode.ENodeType.BACKBOX:
						str = "BackBox";
						break;

					case CSongListNode.ENodeType.RANDOM:
						str = "Random";
						break;

					default:
						str = "Unknown";
						break;
				}
				TJAPlayer3.actTextConsole.tPrint(this.n本体X + 0x12, this.n本体Y - 1, CTextConsole.EFontType.CyanSlim, str);
			}
		}
		private void t描画処理_センサ光() {
			int num = (int)this.ctセンサ光.CurrentValue;
			if (num < 12) {
				int x = this.n本体X + 0xcc;
				int y = this.n本体Y + 0x7b;
				if (this.txセンサ光 != null) {
					this.txセンサ光.vcScaleRatio = new Vector3D<float>(1f, 1f, 1f);
					this.txセンサ光.Opacity = 0xff;
					this.txセンサ光.t2D描画(x, y, new Rectangle((num % 4) * 0x40, (num / 4) * 0x40, 0x40, 0x40));
				}
			} else if (num < 0x18) {
				int num4 = num - 11;
				double num5 = ((double)num4) / 11.0;
				double num6 = 1.0 + (num5 * 0.5);
				int num7 = (int)(64.0 * num6);
				int num8 = (int)(64.0 * num6);
				int num9 = ((this.n本体X + 0xcc) + 0x20) - (num7 / 2);
				int num10 = ((this.n本体Y + 0x7b) + 0x20) - (num8 / 2);
				if (this.txセンサ光 != null) {
					this.txセンサ光.vcScaleRatio = new Vector3D<float>((float)num6, (float)num6, 1f);
					this.txセンサ光.Opacity = (int)(255.0 * (1.0 - num5));
					this.txセンサ光.t2D描画(num9, num10, this.rcセンサ光);
				}
			}
		}
		private void t描画処理_センサ本体() {
			int x = this.n本体X + 0xcd;
			int y = this.n本体Y - 4;
			if (this.txセンサ != null) {
				this.txセンサ.t2D描画(x, y, this.rcセンサ本体上半分);
				y += 0x80;
				this.txセンサ.t2D描画(x, y, this.rcセンサ本体下半分);
			}
		}
		private void t描画処理_パネル本体() {
			if (this.ct登場アニメ用.IsEnded || (this.txパネル本体 != null)) {
				this.n本体X = 16;
				this.n本体Y = 86;
			} else {
				double num = ((double)this.ct登場アニメ用.CurrentValue) / 100.0;
				double num2 = Math.Cos((1.5 + (0.5 * num)) * Math.PI);
				this.n本体X = 8;
				if (this.txパネル本体 != null)
					this.n本体Y = 0x39 - ((int)(this.txパネル本体.sz画像サイズ.Height * (1.0 - (num2 * num2))));
				else
					this.n本体Y = 8;
			}
			if (this.txパネル本体 != null) {
				this.txパネル本体.t2D描画(this.n本体X, this.n本体Y);
			}
		}
		private unsafe void t描画処理_プレビュー画像() {
			if (!TJAPlayer3.stageSongSelect.bCurrentlyScrolling && (((this.ct遅延表示 != null) && (this.ct遅延表示.CurrentValue > 0)) && !this.b新しいプレビューファイルをまだ読み込んでいない)) {
				int x = this.n本体X + 0x12;
				int y = this.n本体Y + 0x10;
				float num3 = ((float)this.ct遅延表示.CurrentValue) / 100f;
				float num4 = 0.9f + (0.1f * num3);
				if (this.r表示するプレビュー画像 != null) {
					/*
					int width = this.r表示するプレビュー画像.sz画像サイズ.Width;
					int height = this.r表示するプレビュー画像.sz画像サイズ.Height;
					if( width > 400 )
					{
						width = 400;
					}
					if( height > 400 )
					{
						height = 400;
					}
					*/

					// Placeholder
					int width = TJAPlayer3.Skin.SongSelect_Preimage_Size[0];
					int height = TJAPlayer3.Skin.SongSelect_Preimage_Size[1];

					float xRatio = width / (float)this.r表示するプレビュー画像.sz画像サイズ.Width;
					float yRatio = height / (float)this.r表示するプレビュー画像.sz画像サイズ.Height;

					x += (400 - ((int)(width * num4))) / 2;
					y += (400 - ((int)(height * num4))) / 2;

					this.r表示するプレビュー画像.Opacity = (int)(255f * num3);
					this.r表示するプレビュー画像.vcScaleRatio.X = num4 * xRatio;
					this.r表示するプレビュー画像.vcScaleRatio.Y = num4 * xRatio;

					// this.r表示するプレビュー画像.t2D描画( x + 22, y + 12, new Rectangle( 0, 0, width, height ) );

					// Temporary addition
					this.r表示するプレビュー画像.t2D拡大率考慮中央基準描画(TJAPlayer3.Skin.SongSelect_Preimage[0], TJAPlayer3.Skin.SongSelect_Preimage[1]);
				}
			}
		}
		//-----------------
		#endregion
	}
}
