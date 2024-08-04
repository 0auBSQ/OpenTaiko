using System.Drawing;
using FDK;

namespace OpenTaiko {
	internal class CActFIFOStart : CActivity {
		// メソッド

		public void tフェードアウト開始() {
			this.mode = EFIFOモード.フェードアウト;

			OpenTaiko.Skin.soundDanSelectBGM.tStop();
			if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
				this.counter = new CCounter(0, 1255, 1, OpenTaiko.Timer);
			else if (OpenTaiko.ConfigIni.bAIBattleMode) {
				this.counter = new CCounter(0, 5500, 1, OpenTaiko.Timer);
			} else {
				this.counter = new CCounter(0, 3580, 1, OpenTaiko.Timer);
			}
		}
		public void tフェードイン開始() {
			this.mode = EFIFOモード.フェードイン;

			if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan) {
				this.counter = new CCounter(0, 255, 1, OpenTaiko.Timer);

				OpenTaiko.stage演奏ドラム画面.actDan.Start(OpenTaiko.stage演奏ドラム画面.ListDan_Number);
				OpenTaiko.stage演奏ドラム画面.ListDan_Number++;
			} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
				this.counter = new CCounter(0, 3580, 1, OpenTaiko.Timer);
			} else {
				this.counter = new CCounter(0, 3580, 1, OpenTaiko.Timer);
			}
		}
		public void tフェードイン完了()     // #25406 2011.6.9 yyagi
		{
			this.counter.CurrentValue = (int)this.counter.EndValue;
		}

		// CActivity 実装

		public override void DeActivate() {
			//CDTXMania.tテクスチャの解放( ref this.tx幕 );
			base.DeActivate();
		}
		public override void CreateManagedResource() {
			//this.tx幕 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\6_FO.png" ) );
			//	this.tx幕2 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\6_FI.png" ) );
			base.CreateManagedResource();
		}
		public override int Draw() {
			if (base.IsDeActivated || (this.counter == null)) {
				return 0;
			}
			this.counter.Tick();

			if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] >= (int)Difficulty.Tower) {
				if (OpenTaiko.Tx.Tile_Black != null) {
					OpenTaiko.Tx.Tile_Black.Opacity = this.mode == EFIFOモード.フェードアウト ? -1000 + counter.CurrentValue : 255 - counter.CurrentValue;
					for (int i = 0; i <= (SampleFramework.GameWindowSize.Width / OpenTaiko.Tx.Tile_Black.szTextureSize.Width); i++)      // #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
					{
						for (int j = 0; j <= (SampleFramework.GameWindowSize.Height / OpenTaiko.Tx.Tile_Black.szTextureSize.Height); j++) // #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
						{
							OpenTaiko.Tx.Tile_Black.t2D描画(i * OpenTaiko.Tx.Tile_Black.szTextureSize.Width, j * OpenTaiko.Tx.Tile_Black.szTextureSize.Height);
						}
					}
				}
			} else if (OpenTaiko.ConfigIni.bAIBattleMode) {
				if (this.mode == EFIFOモード.フェードアウト) {
					var preTime = (this.counter.CurrentValue >= 2000 ? this.counter.CurrentValue - 2000 : 0) * 2;

					OpenTaiko.Tx.SongLoading_Fade_AI.Opacity = preTime;
					OpenTaiko.Tx.SongLoading_Fade_AI.t2D描画(0, 0);

					if (preTime > 500) {
						OpenTaiko.Tx.SongLoading_Fade_AI_Anime_Base.vcScaleRatio.X = Math.Min(((preTime - 500) / 255.0f), 1.0f);
						OpenTaiko.Tx.SongLoading_Fade_AI_Anime_Base.t2D拡大率考慮中央基準描画(OpenTaiko.Skin.Resolution[0] / 2, OpenTaiko.Skin.Resolution[1] / 2);
					}

					if (preTime > 1000) {
						OpenTaiko.Tx.SongLoading_Fade_AI_Anime_Ring.Opacity = preTime - 1000;
						OpenTaiko.Tx.SongLoading_Fade_AI_Anime_Ring.fZ軸中心回転 = preTime / 6000.0f;
						OpenTaiko.Tx.SongLoading_Fade_AI_Anime_Ring.t2D描画(OpenTaiko.Skin.SongLoading_Fade_AI_Anime_Ring[0], OpenTaiko.Skin.SongLoading_Fade_AI_Anime_Ring[1]);
						if (preTime - 1000 < 1500) {
							OpenTaiko.Tx.SongLoading_Fade_AI_Anime_NowLoading.Opacity = preTime - 1000;
							OpenTaiko.Tx.SongLoading_Fade_AI_Anime_NowLoading.t2D描画(0, 0);

							OpenTaiko.Tx.SongLoading_Fade_AI_Anime_LoadBar_Base.t2D描画(OpenTaiko.Skin.SongLoading_Fade_AI_Anime_LoadBar[0], OpenTaiko.Skin.SongLoading_Fade_AI_Anime_LoadBar[1]);

							float value = (preTime - 1000) / 1500.0f;
							value = 1.0f - (float)Math.Cos(value * Math.PI / 2.0);
							value = 1.0f - (float)Math.Cos(value * Math.PI / 2.0);
							value = 1.0f - (float)Math.Cos(value * Math.PI / 2.0);

							OpenTaiko.Tx.SongLoading_Fade_AI_Anime_LoadBar.t2D描画(OpenTaiko.Skin.SongLoading_Fade_AI_Anime_LoadBar[0], OpenTaiko.Skin.SongLoading_Fade_AI_Anime_LoadBar[1],
								new RectangleF(0, 0, OpenTaiko.Tx.SongLoading_Fade_AI_Anime_LoadBar.szTextureSize.Width * value,
								OpenTaiko.Tx.SongLoading_Fade_AI_Anime_LoadBar.szTextureSize.Height));
						} else {
							OpenTaiko.Tx.SongLoading_Fade_AI_Anime_Start.t2D描画(0, 0);
						}
					}

					var time = this.counter.CurrentValue >= 5000 ? this.counter.CurrentValue - 5000 : 0;

					OpenTaiko.Tx.SongLoading_Bg_AI.Opacity = time;
					OpenTaiko.Tx.SongLoading_Bg_AI.t2D描画(0, 0);

					OpenTaiko.Tx.SongLoading_Bg_AI_Wait.Opacity = time - 255;
					OpenTaiko.Tx.SongLoading_Bg_AI_Wait.t2D描画(0, 0);

					OpenTaiko.Tx.SongLoading_Plate_AI.Opacity = time - 255;
					if (OpenTaiko.Skin.SongLoading_Plate_ReferencePoint == CSkin.ReferencePoint.Left) {
						OpenTaiko.Tx.SongLoading_Plate_AI.t2D描画(OpenTaiko.Skin.SongLoading_Plate_X_AI, OpenTaiko.Skin.SongLoading_Plate_Y_AI - (OpenTaiko.Tx.SongLoading_Plate_AI.sz画像サイズ.Height / 2));
					} else if (OpenTaiko.Skin.SongLoading_Plate_ReferencePoint == CSkin.ReferencePoint.Right) {
						OpenTaiko.Tx.SongLoading_Plate_AI.t2D描画(OpenTaiko.Skin.SongLoading_Plate_X_AI - OpenTaiko.Tx.SongLoading_Plate_AI.sz画像サイズ.Width, OpenTaiko.Skin.SongLoading_Plate_Y_AI - (OpenTaiko.Tx.SongLoading_Plate_AI.sz画像サイズ.Height / 2));
					} else {
						OpenTaiko.Tx.SongLoading_Plate_AI.t2D描画(OpenTaiko.Skin.SongLoading_Plate_X_AI - (OpenTaiko.Tx.SongLoading_Plate_AI.sz画像サイズ.Width / 2), OpenTaiko.Skin.SongLoading_Plate_Y_AI - (OpenTaiko.Tx.SongLoading_Plate_AI.sz画像サイズ.Height / 2));
					}
				} else {
					OpenTaiko.Tx.SongLoading_Bg_AI.Opacity = 255 - counter.CurrentValue;
					OpenTaiko.Tx.SongLoading_Bg_AI.t2D描画(0, 0);
				}
			} else {
				if (this.mode == EFIFOモード.フェードアウト) {
					if (OpenTaiko.Tx.SongLoading_Fade != null) {
						// 曲開始幕アニメ。
						// 地味に横の拡大率が変動しているのが一番厄介...
						var time = this.counter.CurrentValue >= 2580 ? this.counter.CurrentValue - 2580 : 0;
						var FadeValue = (time - 670f) / 330.0f;
						if (FadeValue >= 1.0) FadeValue = 1.0f; else if (FadeValue <= 0.0) FadeValue = 0.0f;

						DrawBack(time < 500.0 ? OpenTaiko.Tx.SongLoading_Fade : OpenTaiko.Tx.SongLoading_Bg, time, 0, 500.0, false);
						DrawStar(FadeValue * 255f);
						DrawPlate(FadeValue * 255f, FadeValue);
						DrawChara(time, (time - 730f) * (255f / 270f));
					}

				} else {
					if (OpenTaiko.Tx.SongLoading_Fade != null) {
						// 曲開始幕アニメ。
						// 地味に横の拡大率が変動しているのが一番厄介...
						var time = this.counter.CurrentValue;
						var FadeValue = time / 140f;
						if (FadeValue >= 1.0) FadeValue = 1.0f; else if (FadeValue <= 0.0) FadeValue = 0.0f;

						DrawBack(time < 300.0 ? OpenTaiko.Tx.SongLoading_Bg : OpenTaiko.Tx.SongLoading_Fade, time, 300.0, 500.0, true);
						DrawStar(255f - (FadeValue * 255f));
						DrawPlate(255f - (FadeValue * 255f), 1f + (FadeValue * 0.5f), 1f - FadeValue);
						DrawChara(time, (time <= 80.0 ? 255 : 255f - (float)((Math.Pow((time - 80f), 1.5f) / Math.Pow(220f, 1.5f)) * 255f)), 250f, (time <= 80.0 ? ((time / 80f) * 30f) : 30f - (float)((Math.Pow((time - 80f), 1.5f) / Math.Pow(220f, 1.5f)) * 320f)));
					}
				}
			}

			if (this.mode == EFIFOモード.フェードアウト) {
				if (this.counter.CurrentValue != this.counter.EndValue) {
					return 0;
				}
			} else if (this.mode == EFIFOモード.フェードイン) {
				if (this.counter.CurrentValue != this.counter.EndValue) {
					return 0;
				}
			}
			return 1;
		}

		private void DrawBack(CTexture ShowTex, double time, double max, double end, bool IsExit) {
			if (ShowTex == null) return;
			if (time - max >= end) time = end + max;

			var SizeXHarf = ShowTex.szTextureSize.Width / 2f;
			var SizeY = ShowTex.szTextureSize.Height;
			var StartScaleX = 0.5f;
			var ScaleX = (float)((IsExit ? 1f - StartScaleX : 0f) - ((time >= max ? (time - max) : 0) * ((1f - StartScaleX) / end))) * (IsExit ? 1f : -1f);
			var Value = (float)((IsExit ? 1f : 0f) - ((time >= max ? (time - max) : 0) * (1f / end))) * (IsExit ? 1f : -1f);

			ShowTex.vcScaleRatio.X = StartScaleX + ScaleX;
			ShowTex.t2D描画(-(SizeXHarf * StartScaleX) + (Value * (SizeXHarf * StartScaleX)), 0, new RectangleF(0, 0, SizeXHarf, SizeY));
			ShowTex.t2D描画((SizeXHarf + (SizeXHarf * StartScaleX)) - (Value * (SizeXHarf * StartScaleX)) + ((1f - ShowTex.vcScaleRatio.X) * SizeXHarf), 0, new RectangleF(SizeXHarf, 0, SizeXHarf, SizeY));

		}
		/// <summary>
		/// キラキラ✨
		/// </summary>
		/// <param name="opacity"></param>
		private void DrawStar(float opacity) {
			if (OpenTaiko.Tx.SongLoading_BgWait is null) return;

			OpenTaiko.Tx.SongLoading_BgWait.Opacity = (int)opacity;
			OpenTaiko.Tx.SongLoading_BgWait.t2D描画(0, 0);
		}

		/// <summary>
		/// 横に伸びるプレートを描画
		/// </summary>
		/// <param name="opacity"></param>
		/// <param name="scaleX"></param>
		private void DrawPlate(float opacity, float scaleX, float scaleY = 1f) {
			if (OpenTaiko.Tx.SongLoading_Plate is null) return;
			var SizeX_Harf = OpenTaiko.Tx.SongLoading_Plate.szTextureSize.Width / 2.0f;
			var SizeY_Harf = OpenTaiko.Tx.SongLoading_Plate.szTextureSize.Height / 2.0f;

			OpenTaiko.Tx.SongLoading_Plate.Opacity = (int)opacity;
			OpenTaiko.Tx.SongLoading_Plate.vcScaleRatio.X = scaleX;
			OpenTaiko.Tx.SongLoading_Plate.vcScaleRatio.Y = scaleY;
			OpenTaiko.Tx.SongLoading_Plate.t2D描画(OpenTaiko.Skin.SongLoading_Plate_X + SizeX_Harf - (SizeX_Harf * scaleX) - SizeX_Harf, OpenTaiko.Skin.SongLoading_Plate_Y - SizeY_Harf + ((1f - scaleY) * SizeY_Harf));
		}

		private void DrawChara(double time, float opacity, float X = -1, float Y = -1) {
			if (OpenTaiko.Tx.SongLoading_Plate is null || (X == -1 && Y == -1 ? time <= 680 : false)) return;
			var SizeXHarf = OpenTaiko.Tx.SongLoading_Chara.szTextureSize.Width / 2f;
			var SizeY = OpenTaiko.Tx.SongLoading_Chara.szTextureSize.Height;

			if (X == -1 && Y == -1) {
				Y = (float)(Math.Sin((time - 680f) * (Math.PI / 320.0)) * OpenTaiko.Skin.SongLoading_Chara_Move[1]);
				X = (float)((time - 680f) / 320.0) * OpenTaiko.Skin.SongLoading_Chara_Move[0];
			}

			OpenTaiko.Tx.SongLoading_Chara.Opacity = (int)opacity;
			//左キャラ
			OpenTaiko.Tx.SongLoading_Chara.t2D描画(-OpenTaiko.Skin.SongLoading_Chara_Move[0] + X, Y, new RectangleF(0, 0, SizeXHarf, SizeY));
			//左キャラ
			OpenTaiko.Tx.SongLoading_Chara.t2D描画(SizeXHarf + OpenTaiko.Skin.SongLoading_Chara_Move[0] - X, Y, new RectangleF(SizeXHarf, 0, SizeXHarf, SizeY));
		}

		// その他

		#region [ private ]
		//-----------------
		private CCounter counter;
		private CCounter ct待機;
		private EFIFOモード mode;
		//private CTexture tx幕;
		//private CTexture tx幕2;
		//-----------------
		#endregion
	}
}
