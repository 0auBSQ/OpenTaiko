using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
	internal class CActFIFOStart : CActivity
	{
		// メソッド

		public void tフェードアウト開始()
		{
			this.mode = EFIFOモード.フェードアウト;

			TJAPlayer3.Skin.soundDanSelectBGM.t停止する();
			if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
				this.counter = new CCounter(0, 1255, 1, TJAPlayer3.Timer);
			else
				this.counter = new CCounter(0, 3580, 1, TJAPlayer3.Timer);
		}
		public void tフェードイン開始()
		{
			this.mode = EFIFOモード.フェードイン;

			if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
			{
				this.counter = new CCounter(0, 255, 1, TJAPlayer3.Timer);

				TJAPlayer3.stage演奏ドラム画面.actDan.Start(TJAPlayer3.stage演奏ドラム画面.ListDan_Number);
				TJAPlayer3.stage演奏ドラム画面.ListDan_Number++;
			}
			else
				this.counter = new CCounter(0, 3580, 1, TJAPlayer3.Timer);
		}
		public void tフェードイン完了()     // #25406 2011.6.9 yyagi
		{
			this.counter.n現在の値 = (int)this.counter.n終了値;
		}

		// CActivity 実装

		public override void On非活性化()
		{
			if (!base.b活性化してない)
			{
				//CDTXMania.tテクスチャの解放( ref this.tx幕 );
				base.On非活性化();
			}
		}
		public override void OnManagedリソースの作成()
		{
			if (!base.b活性化してない)
			{
				//this.tx幕 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\6_FO.png" ) );
				//	this.tx幕2 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\6_FI.png" ) );
				base.OnManagedリソースの作成();
			}
		}
		public override int On進行描画()
		{
			if (base.b活性化してない || (this.counter == null))
			{
				return 0;
			}
			this.counter.t進行();

			if(TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
            {
				if (TJAPlayer3.Tx.Tile_Black != null)
				{
					TJAPlayer3.Tx.Tile_Black.Opacity = this.mode == EFIFOモード.フェードアウト ? -1000 + counter.n現在の値 : 255 - counter.n現在の値;
					for (int i = 0; i <= (SampleFramework.GameWindowSize.Width / 64); i++)      // #23510 2010.10.31 yyagi: change "clientSize.Width" to "640" to fix FIFO drawing size
					{
						for (int j = 0; j <= (SampleFramework.GameWindowSize.Height / 64); j++) // #23510 2010.10.31 yyagi: change "clientSize.Height" to "480" to fix FIFO drawing size
						{
							TJAPlayer3.Tx.Tile_Black.t2D描画(TJAPlayer3.app.Device, i * 64, j * 64);
						}
					}
				}
			}
            else
			{
				if (this.mode == EFIFOモード.フェードアウト)
				{
					if (TJAPlayer3.Tx.SongLoading_Fade != null)
					{
						// 曲開始幕アニメ。
						// 地味に横の拡大率が変動しているのが一番厄介...
						var time = this.counter.n現在の値 >= 2580 ? this.counter.n現在の値 - 2580 : 0;
						var FadeValue = (time - 670f) / 330.0f;
						if (FadeValue >= 1.0) FadeValue = 1.0f; else if (FadeValue <= 0.0) FadeValue = 0.0f;

						DrawBack(time < 500.0 ? TJAPlayer3.Tx.SongLoading_Fade : TJAPlayer3.Tx.SongLoading_Bg, time, 0, 500.0, false);
						DrawStar(FadeValue * 255f);
						DrawPlate(FadeValue * 255f, FadeValue);
						DrawChara(time, (time - 730f) * (255f / 270f));
					}

				}
				else
				{
					if (TJAPlayer3.Tx.SongLoading_Fade != null)
					{
						// 曲開始幕アニメ。
						// 地味に横の拡大率が変動しているのが一番厄介...
						var time = this.counter.n現在の値;
						var FadeValue = time / 140f;
						if (FadeValue >= 1.0) FadeValue = 1.0f; else if (FadeValue <= 0.0) FadeValue = 0.0f;

						DrawBack(time < 300.0 ? TJAPlayer3.Tx.SongLoading_Bg : TJAPlayer3.Tx.SongLoading_Fade, time, 300.0, 500.0, true);
						DrawStar(255f - (FadeValue * 255f));
						DrawPlate(255f - (FadeValue * 255f), 1f + (FadeValue * 0.5f), 1f - FadeValue);
						DrawChara(time, (time <= 80.0 ? 255 : 255f - (float)((Math.Pow((time - 80f), 1.5f) / Math.Pow(220f, 1.5f)) * 255f)), 250f, (time <= 80.0 ? ((time / 80f) * 30f) : 30f - (float)((Math.Pow((time - 80f), 1.5f) / Math.Pow(220f, 1.5f)) * 320f)));
					}
				}
			}

			if (this.mode == EFIFOモード.フェードアウト)
			{
				if (this.counter.n現在の値 != this.counter.n終了値)
				{
					return 0;
				}
			}
			else if (this.mode == EFIFOモード.フェードイン)
			{
				if (this.counter.n現在の値 != this.counter.n終了値)
				{
					return 0;
				}
			}
			return 1;
		}

		public void DrawBack(CTexture ShowTex, double time, double max, double end, bool IsExit)
		{
			if (ShowTex == null) return;
			if (time - max >= end) time = end + max;

			var SizeXHarf = ShowTex.szテクスチャサイズ.Width / 2f;
			var SizeY = ShowTex.szテクスチャサイズ.Height;
			var StartScaleX = 0.5f;
			var ScaleX = (float)((IsExit ? 1f - StartScaleX : 0f) - ((time >= max ? (time - max) : 0) * ((1f - StartScaleX) / end))) * (IsExit ? 1f : -1f);
			var Value = (float)((IsExit ? 1f : 0f) - ((time >= max ? (time - max) : 0) * (1f / end))) * (IsExit ? 1f : -1f);

			ShowTex.vc拡大縮小倍率.X = StartScaleX + ScaleX;
			ShowTex.t2D描画(TJAPlayer3.app.Device, -(SizeXHarf * StartScaleX) + (Value * (SizeXHarf * StartScaleX)), 0, new RectangleF(0, 0, SizeXHarf, SizeY));
			ShowTex.t2D描画(TJAPlayer3.app.Device, (SizeXHarf + (SizeXHarf * StartScaleX)) - (Value * (SizeXHarf * StartScaleX)) + ((1f - ShowTex.vc拡大縮小倍率.X) * SizeXHarf), 0, new RectangleF(SizeXHarf, 0, SizeXHarf, SizeY));

		}
		/// <summary>
		/// キラキラ✨
		/// </summary>
		/// <param name="opacity"></param>
		public void DrawStar(float opacity)
		{
			if (TJAPlayer3.Tx.SongLoading_BgWait is null) return;

			TJAPlayer3.Tx.SongLoading_BgWait.Opacity = (int)opacity;
			TJAPlayer3.Tx.SongLoading_BgWait.t2D描画(TJAPlayer3.app.Device, 0, 0);
		}

		/// <summary>
		/// 横に伸びるプレートを描画
		/// </summary>
		/// <param name="opacity"></param>
		/// <param name="scaleX"></param>
		public void DrawPlate(float opacity, float scaleX, float scaleY = 1f)
		{
			if (TJAPlayer3.Tx.SongLoading_Plate is null) return;
			var SizeX_Harf = TJAPlayer3.Tx.SongLoading_Plate.szテクスチャサイズ.Width / 2.0f;
			var SizeY_Harf = TJAPlayer3.Tx.SongLoading_Plate.szテクスチャサイズ.Height / 2.0f;

			TJAPlayer3.Tx.SongLoading_Plate.Opacity = (int)opacity;
			TJAPlayer3.Tx.SongLoading_Plate.vc拡大縮小倍率.X = scaleX;
			TJAPlayer3.Tx.SongLoading_Plate.vc拡大縮小倍率.Y = scaleY;
			TJAPlayer3.Tx.SongLoading_Plate.t2D描画(TJAPlayer3.app.Device, SizeX_Harf - (SizeX_Harf * scaleX) + (1280.0f / 2.0f) - SizeX_Harf, TJAPlayer3.Skin.SongLoading_Plate_Y - SizeY_Harf + ((1f - scaleY) * SizeY_Harf));
		}

		public void DrawChara(double time, float opacity, float X = -1, float Y = -1)
		{
			if (TJAPlayer3.Tx.SongLoading_Plate is null || (X == -1 && Y == -1 ? time <= 680 : false)) return;
			var SizeXHarf = TJAPlayer3.Tx.SongLoading_Chara.szテクスチャサイズ.Width / 2f;
			var SizeY = TJAPlayer3.Tx.SongLoading_Chara.szテクスチャサイズ.Height;

			if (X == -1 && Y == -1)
			{
				Y = -(float)(Math.Sin((time - 680f) * (Math.PI / 320.0)) * 80f);
				X = (float)((time - 680f) / 320.0) * 250f;
			}

			TJAPlayer3.Tx.SongLoading_Chara.Opacity = (int)opacity;
			//左どんちゃん
			TJAPlayer3.Tx.SongLoading_Chara.t2D描画(TJAPlayer3.app.Device, -250f + X, Y, new RectangleF(0, 0, SizeXHarf, SizeY));
			//左どんちゃん
			TJAPlayer3.Tx.SongLoading_Chara.t2D描画(TJAPlayer3.app.Device, SizeXHarf + 250f - X, Y, new RectangleF(SizeXHarf, 0, SizeXHarf, SizeY));
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