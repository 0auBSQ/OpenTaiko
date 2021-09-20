using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using FDK;
using System.Reflection;

namespace TJAPlayer3
{
	internal class CStageタイトル : CStage
	{
		// コンストラクタ

		public CStageタイトル()
		{
			base.eステージID = CStage.Eステージ.タイトル;
			base.b活性化してない = true;
			base.list子Activities.Add( this.actFIfromSetup = new CActFIFOBlack() );
			base.list子Activities.Add( this.actFI = new CActFIFOBlack() );
			base.list子Activities.Add( this.actFO = new CActFIFOBlack() );

		}


		// CStage 実装

		public override void On活性化()
		{
			Trace.TraceInformation( "タイトルステージを活性化します。" );
			Trace.Indent();
			try
			{
				this.ctバナパス読み込み待機 = new CCounter();

				this.ctコインイン待機 = new CCounter(0, 2000, 1, TJAPlayer3.Timer);

				this.ctバナパス読み込み成功 = new CCounter();
				this.ctバナパス読み込み失敗 = new CCounter();

				this.ctエントリーバー点滅 = new CCounter(0, 510, 2, TJAPlayer3.Timer);
				this.ctエントリーバー決定点滅 = new CCounter();

				this.ctどんちゃんエントリーループ = new CCounter();
				this.ctどんちゃんイン = new CCounter();
				this.ctどんちゃんループ = new CCounter(0, TJAPlayer3.Tx.Entry_Donchan_Normal.Length - 1, 1000 / 30, TJAPlayer3.Timer);

				this.ctBarAnimeIn = new CCounter();
				this.ctBarMove = new CCounter();
				this.ctBarMove.n現在の値 = 250;

				this.bバナパス読み込み = false;
				this.bバナパス読み込み失敗 = false;
				this.bプレイヤーエントリー = false;
				this.bプレイヤーエントリー決定 = false;
				this.bモード選択 = false;
				this.bどんちゃんカウンター初期化 = false;

				this.n現在の選択行プレイヤーエントリー = 1;

				for (int i = 0; i < this.nbModes; i++)
				{
					this.stModeBar[i].BarTexture = TJAPlayer3.Tx.ModeSelect_Bar[i];
					this.stModeBar[i].n現在存在している行 = i + 1;
				}

				TJAPlayer3.Skin.soundEntry.t再生する();
				TJAPlayer3.Skin.SoundBanapas.bPlayed = false;
				if (TJAPlayer3.ConfigIni.bBGM音を発声する)
					TJAPlayer3.Skin.bgmタイトルイン.t再生する();
				b音声再生 = false;
				base.On活性化();
			}
			finally
			{
				Trace.TraceInformation( "タイトルステージの活性化を完了しました。" );
				Trace.Unindent();
			}
		}
		public override void On非活性化()
		{
			Trace.TraceInformation( "タイトルステージを非活性化します。" );
			Trace.Indent();
			try
			{

			}
			finally
			{
				Trace.TraceInformation( "タイトルステージの非活性化を完了しました。" );
				Trace.Unindent();
			}
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{

		}
		public override void OnManagedリソースの解放()
		{

		}
		public override int On進行描画()
		{
			if( !base.b活性化してない )
			{
				#region [ 初めての進行描画 ]
				//---------------------
				if ( base.b初めての進行描画 )
				{
					if( TJAPlayer3.r直前のステージ == TJAPlayer3.stage起動 )
					{
						this.actFIfromSetup.tフェードイン開始();
						base.eフェーズID = CStage.Eフェーズ.タイトル_起動画面からのフェードイン;
					}
					else
					{
						this.actFI.tフェードイン開始();
						base.eフェーズID = CStage.Eフェーズ.共通_フェードイン;
					}
					base.b初めての進行描画 = false;
                }
				//---------------------
				#endregion

				this.ctコインイン待機.t進行Loop();
				this.ctバナパス読み込み成功.t進行();
				this.ctバナパス読み込み失敗.t進行();
				this.ctエントリーバー点滅.t進行Loop();
				this.ctエントリーバー決定点滅.t進行();
				this.ctどんちゃんイン.t進行();
				this.ctどんちゃんループ.t進行Loop();
				this.ctどんちゃんエントリーループ.t進行Loop();
				this.ctBarMove.t進行();

				if (!TJAPlayer3.Skin.bgmタイトルイン.b再生中)
                {
                    if (TJAPlayer3.ConfigIni.bBGM音を発声する && !b音声再生)
					{
						TJAPlayer3.Skin.bgmタイトル.t再生する();
						b音声再生 = true;
					}
                }

                // 進行

                #region [ キー関係 ]

                if (base.eフェーズID == CStage.Eフェーズ.共通_通常状態        // 通常状態、かつ
					&& TJAPlayer3.act現在入力を占有中のプラグイン == null)    // プラグインの入力占有がない
				{
					if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Escape))
						return (int)E戻り値.EXIT;

					if ((TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.RightShift) || TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.LeftShift)) && TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.F1))
					{
						TJAPlayer3.Skin.soundEntry.t停止する();
						n現在の選択行モード選択 = (int)E戻り値.CONFIG - 1;
						this.actFO.tフェードアウト開始();
						base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
						TJAPlayer3.Skin.sound取消音.t再生する();
					}

					if (!bバナパス読み込み && !bバナパス読み込み失敗)
					{
						if (TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.P))
							this.ctバナパス読み込み待機.t開始(0, 600, 1, TJAPlayer3.Timer);
						if (TJAPlayer3.Input管理.Keyboard.bキーが押されている((int)SlimDXKeys.Key.P))
							ctバナパス読み込み待機.t進行();
						if (TJAPlayer3.Input管理.Keyboard.bキーが離された((int)SlimDXKeys.Key.P))
						{
							this.ctバナパス読み込み待機.t停止();
							if (this.ctバナパス読み込み待機.n現在の値 < 600 && !bバナパス読み込み失敗)
							{
								ctバナパス読み込み失敗.t開始(0, 1128, 1, TJAPlayer3.Timer);
								bバナパス読み込み失敗 = true;
							}
						}
					}

					if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RBlue) || TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.RightArrow))
                    {
						if(bプレイヤーエントリー && !bプレイヤーエントリー決定)
                        {
							if(n現在の選択行プレイヤーエントリー + 1 <= 2)
							{
								TJAPlayer3.Skin.sound変更音.t再生する();
								n現在の選択行プレイヤーエントリー += 1;
							}
						}

						if (bモード選択)
						{
							if (n現在の選択行モード選択 < this.nbModes - 1)
							{
								TJAPlayer3.Skin.sound変更音.t再生する();
								ctBarMove.t開始(0, 250, 1.2f, TJAPlayer3.Timer);
								n現在の選択行モード選択++;
								for (int i = 0; i < this.nbModes; i++)
									this.stModeBar[i].n現在存在している行 = Math.Max(0, i + 1 - n現在の選択行モード選択);
							}
						}
					}

					if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LBlue)||TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.LeftArrow))
                    {
						if(bプレイヤーエントリー && !bプレイヤーエントリー決定)
                        {
							if(n現在の選択行プレイヤーエントリー - 1 >= 0)
							{
								TJAPlayer3.Skin.sound変更音.t再生する();
								n現在の選択行プレイヤーエントリー -= 1;
							}
                        }

						if (bモード選択)
						{
							if(n現在の選択行モード選択 > 0)
							{
								TJAPlayer3.Skin.sound変更音.t再生する();
								ctBarMove.t開始(0, 250, 1.2f, TJAPlayer3.Timer);
								n現在の選択行モード選択--;
								for (int i = 0; i < this.nbModes; i++)
									this.stModeBar[i].n現在存在している行 = Math.Max(0, i + 1 - n現在の選択行モード選択);
							}
						}
                    }


					if (TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.RRed) || TJAPlayer3.Pad.b押された(E楽器パート.DRUMS, Eパッド.LRed) || TJAPlayer3.Input管理.Keyboard.bキーが押された((int)SlimDXKeys.Key.Return))
					{
						if (bプレイヤーエントリー)
						{
							if (n現在の選択行プレイヤーエントリー == 0 || n現在の選択行プレイヤーエントリー == 2)
							{
								if (!bプレイヤーエントリー決定)
								{
									TJAPlayer3.Skin.sound決定音.t再生する();
									ctエントリーバー決定点滅.t開始(0, 1055, 1, TJAPlayer3.Timer);
									bプレイヤーエントリー決定 = true;
								}
							}
							else
							{
								TJAPlayer3.Skin.sound決定音.t再生する();
								bプレイヤーエントリー = false;
								bバナパス読み込み = false;
								TJAPlayer3.Skin.SoundBanapas.bPlayed = false;
								ctバナパス読み込み成功 = new CCounter();
								ctバナパス読み込み待機 = new CCounter();
							}
						}
						if (bモード選択)
						{
							if (this.n現在の選択行モード選択 == (int)E戻り値.CONFIG - 1)
							{
								TJAPlayer3.Skin.sound決定音.t再生する();
								n現在の選択行モード選択 = (int)E戻り値.CONFIG - 1;
								this.actFO.tフェードアウト開始(0, 500);
								base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
							}
							else if (this.n現在の選択行モード選択 == (int)E戻り値.DANGAMESTART - 1)
							{
								if (TJAPlayer3.Songs管理.list曲ルート_Dan.Count > 0 && TJAPlayer3.ConfigIni.nPlayerCount != 2)
								{
									TJAPlayer3.Skin.sound決定音.t再生する();
									n現在の選択行モード選択 = (int)E戻り値.DANGAMESTART - 1;
									this.actFO.tフェードアウト開始(0, 500);
									base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
								}
								else
								{
									TJAPlayer3.Skin.soundError.t再生する();
								}
							}
							else if (this.n現在の選択行モード選択 == (int)E戻り値.GAMESTART - 1)
							{
								TJAPlayer3.Skin.sound決定音.t再生する();
								n現在の選択行モード選択 = (int)E戻り値.GAMESTART - 1;
								this.actFO.tフェードアウト開始(0, 500);
								base.eフェーズID = CStage.Eフェーズ.共通_フェードアウト;
							}
							else
                            {
								TJAPlayer3.Skin.soundError.t再生する();
							}
						}
					}

					if (ctバナパス読み込み待機.n現在の値 >= 500)
					{
                        if (!bバナパス読み込み)
						{
							TJAPlayer3.Skin.soundEntry.t停止する();
							ctバナパス読み込み成功.t開始(0, 3655, 1, TJAPlayer3.Timer);
							bバナパス読み込み = true;
						}
					}

					if (ctエントリーバー決定点滅.n現在の値 >= 1055)
                    {
                        if (!bモード選択)
                        {
							if (!TJAPlayer3.Skin.soundsanka.bPlayed)
								TJAPlayer3.Skin.soundsanka.t再生する();

							ctどんちゃんイン.t開始(0, 180, 2, TJAPlayer3.Timer);
							ctBarAnimeIn.t開始(0, 1295, 1, TJAPlayer3.Timer);
							bモード選択 = true;
                        }
                    }
				}

				#endregion

				#region [ 背景描画 ]

				if (TJAPlayer3.Tx.Title_Background != null )
                    TJAPlayer3.Tx.Title_Background.t2D描画( TJAPlayer3.app.Device, 0, 0 );
				
                #endregion

                #region [ バナパス読み込み ]

                if (!bバナパス読み込み && !bバナパス読み込み失敗)
                {
					TJAPlayer3.Tx.Entry_Bar.t2D描画(TJAPlayer3.app.Device, 0, 0);

					if (this.ctコインイン待機.n現在の値 <= 255)
						TJAPlayer3.Tx.Entry_Bar_Text.Opacity = this.ctコインイン待機.n現在の値;
					else if (this.ctコインイン待機.n現在の値 <= 2000 - 355)
						TJAPlayer3.Tx.Entry_Bar_Text.Opacity = 255;
					else
						TJAPlayer3.Tx.Entry_Bar_Text.Opacity = 255 - (this.ctコインイン待機.n現在の値 - (2000 - 355));

					TJAPlayer3.Tx.Entry_Bar_Text.t2D描画(TJAPlayer3.app.Device, 563, 312, new RectangleF(0, 0, 395, 50));
					TJAPlayer3.Tx.Entry_Bar_Text.t2D描画(TJAPlayer3.app.Device, 563, 430, new RectangleF(0, 50, 395, 50));
				}
                else
				{
					if (this.ctバナパス読み込み成功.n現在の値 <= 1000 && this.ctバナパス読み込み失敗.n現在の値 <= 1128)
					{
						if (bバナパス読み込み)
						{
							TJAPlayer3.Tx.Tile_Black.Opacity = this.ctバナパス読み込み成功.n現在の値 <= 2972 ? 128 : 128 - (this.ctバナパス読み込み成功.n現在の値 - 2972);

							for (int i = 0; i < 1280 / TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Width + 1; i++)
								for (int j = 0; j < 720 / TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Height + 1; j++)
									TJAPlayer3.Tx.Tile_Black.t2D描画(TJAPlayer3.app.Device, i * TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Width, j * TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Height);

							TJAPlayer3.Tx.Banapas_Load[0].Opacity = ctバナパス読み込み成功.n現在の値 >= 872 ? 255 - (ctバナパス読み込み成功.n現在の値 - 872) * 2 : ctバナパス読み込み成功.n現在の値 * 2;
							TJAPlayer3.Tx.Banapas_Load[0].vc拡大縮小倍率.Y = ctバナパス読み込み成功.n現在の値 <= 100 ? ctバナパス読み込み成功.n現在の値 * 0.01f : 1.0f;
							TJAPlayer3.Tx.Banapas_Load[0].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360);

							TJAPlayer3.Tx.Banapas_Load[1].Opacity = ctバナパス読み込み成功.n現在の値 >= 872 ? 255 - (ctバナパス読み込み成功.n現在の値 - 872) * 2 : ctバナパス読み込み成功.n現在の値 <= 96 ? (int)((ctバナパス読み込み成功.n現在の値 - 96) * 7.96875f) : 255;
							TJAPlayer3.Tx.Banapas_Load[1].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360);

							for (int i = 0; i < 5; i++)
							{
								TJAPlayer3.Tx.Banapas_Load[2].Opacity = ctバナパス読み込み成功.n現在の値 >= 872 ? 255 - (ctバナパス読み込み成功.n現在の値 - 872) * 2 : ctバナパス読み込み成功.n現在の値 <= 96 ? (int)((ctバナパス読み込み成功.n現在の値 - 96) * 7.96875f) : 255;
								TJAPlayer3.Tx.Banapas_Load[2].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 480 + 90 * i, 410, new Rectangle(0 + 72 * (ctバナパス読み込み成功.n現在の値 >= 200 + (i - 1) * 320 ? ctバナパス読み込み成功.n現在の値 <= (200 + i * 320) ? (ctバナパス読み込み成功.n現在の値 - (200 + i * 320)) / 40 : 0 : 0), 0, 72, 72));
							}
						}
						if (bバナパス読み込み失敗)
						{
							TJAPlayer3.Tx.Tile_Black.Opacity = this.ctバナパス読み込み失敗.n現在の値 <= 1000 ? 128 : 128 - (this.ctバナパス読み込み失敗.n現在の値 - 1000);

							for (int i = 0; i < 1280 / TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Width + 1; i++)
								for (int j = 0; j < 720 / TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Height + 1; j++)
									TJAPlayer3.Tx.Tile_Black.t2D描画(TJAPlayer3.app.Device, i * TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Width, j * TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Height);

							if (!TJAPlayer3.Skin.soundError.bPlayed)
								TJAPlayer3.Skin.soundError.t再生する();

							int count = this.ctバナパス読み込み失敗.n現在の値;
							TJAPlayer3.Tx.Banapas_Load_Failure[0].Opacity = count >= 872 ? 255 - (count - 872) * 2 : count * 2;
							TJAPlayer3.Tx.Banapas_Load_Failure[0].vc拡大縮小倍率.Y = count <= 100 ? count * 0.01f : 1.0f;
							TJAPlayer3.Tx.Banapas_Load_Failure[0].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360);

							if (ctバナパス読み込み失敗.n現在の値 >= 1128)
							{
								bバナパス読み込み失敗 = false;
								TJAPlayer3.Skin.soundError.bPlayed = false;
							}
						}
					}
                    else
					{
                        if (bバナパス読み込み)
						{
							TJAPlayer3.Tx.Tile_Black.Opacity = this.ctバナパス読み込み成功.n現在の値 <= 2972 ? 128 : 128 - (this.ctバナパス読み込み成功.n現在の値 - 2972);

							for (int i = 0; i < 1280 / TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Width + 1; i++)
								for (int j = 0; j < 720 / TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Height + 1; j++)
									TJAPlayer3.Tx.Tile_Black.t2D描画(TJAPlayer3.app.Device, i * TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Width, j * TJAPlayer3.Tx.Tile_Black.szテクスチャサイズ.Height);

							if (!TJAPlayer3.Skin.SoundBanapas.bPlayed)
								TJAPlayer3.Skin.SoundBanapas.t再生する();

							int count = this.ctバナパス読み込み成功.n現在の値 - 1000;
							TJAPlayer3.Tx.Banapas_Load_Clear[0].Opacity = count >= 1872 ? 255 - (count - 1872) * 2 : count * 2;
							TJAPlayer3.Tx.Banapas_Load_Clear[0].vc拡大縮小倍率.Y = count <= 100 ? count * 0.01f : 1.0f;
							TJAPlayer3.Tx.Banapas_Load_Clear[0].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360);

							float anime = 0f;
							float scalex = 0f;
							float scaley = 0f;

							if (count >= 300)
							{
								if (count <= 300 + 270)
								{
									anime = (float)Math.Sin((float)(count - 300) / 1.5f * (Math.PI / 180)) * 95f;
									scalex = -(float)Math.Sin((float)(count - 300) / 1.5f * (Math.PI / 180)) * 0.15f;
									scaley = (float)Math.Sin((float)(count - 300) / 1.5f * (Math.PI / 180)) * 0.2f;
								}
								else if (count <= 300 + 270 + 100)
								{
									scalex = (float)Math.Sin((float)(count - (300 + 270)) * 1.8f * (Math.PI / 180)) * 0.13f;
									scaley = -(float)Math.Sin((float)(count - (300 + 270)) * 1.8f * (Math.PI / 180)) * 0.1f;
									anime = 0;
								}
								else if (count <= 300 + 540 + 100)
								{
									anime = (float)Math.Sin((float)(count - (300 + 270 + 100)) / 1.5f * (Math.PI / 180)) * 95f;
									scalex = -(float)Math.Sin((float)(count - (300 + 270 + 100)) / 1.5f * (Math.PI / 180)) * 0.15f;
									scaley = (float)Math.Sin((float)(count - (300 + 270 + 100)) / 1.5f * (Math.PI / 180)) * 0.2f;
								}
								else if (count <= 300 + 540 + 100 + 100)
								{
									scalex = (float)Math.Sin((float)(count - (300 + 540 + 100)) * 1.8f * (Math.PI / 180)) * 0.13f;
									scaley = -(float)Math.Sin((float)(count - (300 + 540 + 100)) * 1.8f * (Math.PI / 180)) * 0.1f;
								}
							}

							TJAPlayer3.Tx.Banapas_Load_Clear[1].vc拡大縮小倍率.X = 1.0f + scalex;
							TJAPlayer3.Tx.Banapas_Load_Clear[1].vc拡大縮小倍率.Y = 1.0f + scaley;
							TJAPlayer3.Tx.Banapas_Load_Clear[1].Opacity = count >= 1872 ? 255 - (count - 1872) * 2 : count * 2;
							TJAPlayer3.Tx.Banapas_Load_Clear[1].t2D拡大率考慮下中心基準描画(TJAPlayer3.app.Device, 198, 514 - anime);

							if (ctバナパス読み込み成功.n現在の値 >= 2000)
							{
								bプレイヤーエントリー = true;
							}
						}
					}
				}

                #endregion

                #region [ プレイヤーエントリー ]

                if (bプレイヤーエントリー)
                {
                    if (!this.bどんちゃんカウンター初期化)
					{
						this.ctどんちゃんエントリーループ = new CCounter(0, TJAPlayer3.Tx.Donchan_Entry.Length - 1, 1000 / 60, TJAPlayer3.Timer);
						this.bどんちゃんカウンター初期化 = true;
					}

					TJAPlayer3.Tx.Entry_Player[0].Opacity = ctエントリーバー決定点滅.n現在の値 >= 800 ? 255 - (ctエントリーバー決定点滅.n現在の値 - 800) : (this.ctバナパス読み込み成功.n現在の値 - 3400); 
					TJAPlayer3.Tx.Entry_Player[1].Opacity = ctエントリーバー決定点滅.n現在の値 >= 800 ? 255 - (ctエントリーバー決定点滅.n現在の値 - 800) : (this.ctバナパス読み込み成功.n現在の値 - 3400);
					TJAPlayer3.Tx.Donchan_Entry[this.ctどんちゃんエントリーループ.n現在の値].Opacity = ctエントリーバー決定点滅.n現在の値 >= 800 ? 255 - (ctエントリーバー決定点滅.n現在の値 - 800) : (this.ctバナパス読み込み成功.n現在の値 - 3400);

					TJAPlayer3.Tx.Entry_Player[0].t2D描画(TJAPlayer3.app.Device, 0, 0);

					TJAPlayer3.Tx.Donchan_Entry[this.ctどんちゃんエントリーループ.n現在の値].t2D描画(TJAPlayer3.app.Device, 485, 140);

					TJAPlayer3.Tx.Entry_Player[2].Opacity = ctエントリーバー決定点滅.n現在の値 >= 800 ? 255 - (ctエントリーバー決定点滅.n現在の値 - 800 ) : (this.ctバナパス読み込み成功.n現在の値 - 3400) - (this.ctエントリーバー点滅.n現在の値 <= 255 ? this.ctエントリーバー点滅.n現在の値 : 255 - (this.ctエントリーバー点滅.n現在の値 - 255));
					TJAPlayer3.Tx.Entry_Player[2].t2D描画(TJAPlayer3.app.Device, ptプレイヤーエントリーバー座標[n現在の選択行プレイヤーエントリー].X, ptプレイヤーエントリーバー座標[n現在の選択行プレイヤーエントリー].Y,
						new RectangleF(n現在の選択行プレイヤーエントリー == 1 ? 199 : 0, 0, n現在の選択行プレイヤーエントリー == 1 ? 224 : 199, 92));

					TJAPlayer3.Tx.Entry_Player[2].Opacity = ctエントリーバー決定点滅.n現在の値 >= 800 ? 255 - (ctエントリーバー決定点滅.n現在の値 - 800) : (this.ctバナパス読み込み成功.n現在の値 - 3400);
					TJAPlayer3.Tx.Entry_Player[2].t2D描画(TJAPlayer3.app.Device, ptプレイヤーエントリーバー座標[n現在の選択行プレイヤーエントリー].X, ptプレイヤーエントリーバー座標[n現在の選択行プレイヤーエントリー].Y,
						new RectangleF(n現在の選択行プレイヤーエントリー == 1 ? 199 : 0, 92, n現在の選択行プレイヤーエントリー == 1 ? 224 : 199, 92));

					TJAPlayer3.Tx.Entry_Player[1].t2D描画(TJAPlayer3.app.Device, 0, 0);

					#region [ 透明度 ]

					int Opacity = 0;

					if (ctエントリーバー決定点滅.n現在の値 <= 100)
						Opacity = (int)(ctエントリーバー決定点滅.n現在の値 * 2.55f);
					else if (ctエントリーバー決定点滅.n現在の値 <= 200)
						Opacity = 255 - (int)((ctエントリーバー決定点滅.n現在の値 - 100) * 2.55f);
					else if (ctエントリーバー決定点滅.n現在の値 <= 300)
						Opacity = (int)((ctエントリーバー決定点滅.n現在の値 - 200) * 2.55f);
					else if (ctエントリーバー決定点滅.n現在の値 <= 400)
						Opacity = 255 - (int)((ctエントリーバー決定点滅.n現在の値 - 300) * 2.55f);
					else if (ctエントリーバー決定点滅.n現在の値 <= 500)
						Opacity = (int)((ctエントリーバー決定点滅.n現在の値 - 400) * 2.55f);
					else if (ctエントリーバー決定点滅.n現在の値 <= 600)
						Opacity = 255 - (int)((ctエントリーバー決定点滅.n現在の値 - 500) * 2.55f);

					#endregion

					TJAPlayer3.Tx.Entry_Player[2].Opacity = Opacity;
					TJAPlayer3.Tx.Entry_Player[2].t2D描画(TJAPlayer3.app.Device, ptプレイヤーエントリーバー座標[n現在の選択行プレイヤーエントリー].X, ptプレイヤーエントリーバー座標[n現在の選択行プレイヤーエントリー].Y,
						new RectangleF(n現在の選択行プレイヤーエントリー == 1 ? 199 : 0, 92 * 2, n現在の選択行プレイヤーエントリー == 1 ? 224 : 199, 92));

					TJAPlayer3.NamePlate.tNamePlateDraw(530, 385, 0, true, ctエントリーバー決定点滅.n現在の値 >= 800 ? 255 - (ctエントリーバー決定点滅.n現在の値 - 800) : (this.ctバナパス読み込み成功.n現在の値 - 3400));
				}

                #endregion

                #region [ モード選択 ]

                if (bモード選択)
				{
					this.ctBarAnimeIn.t進行();

					#region [ どんちゃん描画 ]

					float DonchanX = 0f, DonchanY = 0f;

					DonchanX = (float)Math.Sin(ctどんちゃんイン.n現在の値 / 2 * (Math.PI / 180)) * 200f;
					DonchanY = ( (float)Math.Sin((90 + (ctどんちゃんイン.n現在の値 / 2)) * (Math.PI / 180)) * 150f);

					TJAPlayer3.Tx.Entry_Donchan_Normal[ctどんちゃんループ.n現在の値].t2D描画(TJAPlayer3.app.Device, -200 + DonchanX, 341 - DonchanY);

					#endregion

					if(ctBarAnimeIn.n現在の値 >= (int)(16 * 16.6f))
					{
						TJAPlayer3.act文字コンソール.tPrint(0, 0, C文字コンソール.Eフォント種別.白, ctBarMove.n現在の値.ToString());

						for (int i = 0; i < this.nbModes; i++)
                        {
							if(this.stModeBar[i].n現在存在している行 == 1 && ctBarMove.n現在の値 >= 150)
							{
								int BarAnime = ctBarAnimeIn.n現在の値 >= (int)(26 * 16.6f) + 100 ? 0 : ctBarAnimeIn.n現在の値 >= (int)(26 * 16.6f) && ctBarAnimeIn.n現在の値 <= (int)(26 * 16.6f) + 100 ? 40 + (int)((ctBarAnimeIn.n現在の値 - (26 * 16.6)) / 100f * 71f) : ctBarAnimeIn.n現在の値 < (int)(26 * 16.6f) ? 40 : 111;
								int BarAnime1 = BarAnime == 0 ? ctBarMove.n現在の値 >= 150 ? 40 + (int)((ctBarMove.n現在の値 - 150) / 100f * 71f) : ctBarMove.n現在の値 < 150 ? 40 : 111 : 0;

								this.stModeBar[i].BarTexture.Opacity = (int)((ctBarAnimeIn.n現在の値 - (16 * 16.6f)) * 1.23f);

								this.stModeBar[i].BarTexture.vc拡大縮小倍率.Y = 1.0f;
								this.stModeBar[i].BarTexture.t2D描画(TJAPlayer3.app.Device, 320, 347 - BarAnime - BarAnime1, new Rectangle(0, 0, 641, 27));
								this.stModeBar[i].BarTexture.t2D描画(TJAPlayer3.app.Device, 320, 346 + BarAnime + BarAnime1, new Rectangle(0, 76, 641, 30));

								this.stModeBar[i].BarTexture.vc拡大縮小倍率.Y = BarAnime / 25.7f + BarAnime1 / 25.7f;
								this.stModeBar[i].BarTexture.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, 640, 360, new Rectangle(0, 27, 641, 45));

								TJAPlayer3.Tx.ModeSelect_Bar[this.nbModes].vc拡大縮小倍率.Y = 1.0f;
								TJAPlayer3.Tx.ModeSelect_Bar[this.nbModes].t2D描画(TJAPlayer3.app.Device, 320, 306, new Rectangle(0, 0, 641, 27));
								TJAPlayer3.Tx.ModeSelect_Bar[this.nbModes].t2D描画(TJAPlayer3.app.Device, 320, 334 + (BarAnime + BarAnime1) / 0.95238f, new Rectangle(0, 71, 641, 35));

								TJAPlayer3.Tx.ModeSelect_Bar[this.nbModes].vc拡大縮小倍率.Y = (BarAnime + BarAnime1) / 0.95238f;
								TJAPlayer3.Tx.ModeSelect_Bar[this.nbModes].t2D拡大率考慮上中央基準描画(TJAPlayer3.app.Device, 640, 333, new Rectangle(0, 27, 641, 1));

								float anime = 0;
								float BarAnimeCount = this.ctBarMove.n現在の値 - 150;

								if (BarAnimeCount <= 45)
									anime = BarAnimeCount * 3.333333333f;
								else
									anime = 150 - (BarAnimeCount - 45) * 0.61764705f;

								TJAPlayer3.Tx.ModeSelect_Bar_Chara[i].Opacity = (int)(BarAnimeCount * 2.55f) + (int)(BarAnime * 2.5f);
								//130
								TJAPlayer3.Tx.ModeSelect_Bar_Chara[i].t2D中心基準描画(TJAPlayer3.app.Device, 640 - TJAPlayer3.Tx.ModeSelect_Bar_Chara[i].szテクスチャサイズ.Width / 4 + 114 - anime, 360,
									new Rectangle(0, 0, TJAPlayer3.Tx.ModeSelect_Bar_Chara[i].szテクスチャサイズ.Width / 2, TJAPlayer3.Tx.ModeSelect_Bar_Chara[i].szテクスチャサイズ.Height));

								TJAPlayer3.Tx.ModeSelect_Bar_Chara[i].t2D中心基準描画(TJAPlayer3.app.Device, 640 + TJAPlayer3.Tx.ModeSelect_Bar_Chara[i].szテクスチャサイズ.Width / 4 - 114 + anime, 360,
									new Rectangle(TJAPlayer3.Tx.ModeSelect_Bar_Chara[i].szテクスチャサイズ.Width / 2, 0, TJAPlayer3.Tx.ModeSelect_Bar_Chara[i].szテクスチャサイズ.Width / 2, TJAPlayer3.Tx.ModeSelect_Bar_Chara[i].szテクスチャサイズ.Height));

								TJAPlayer3.Tx.ModeSelect_Bar_Text[i].Opacity = 255;
								TJAPlayer3.Tx.ModeSelect_Bar_Text[i]?.t2D中心基準描画(TJAPlayer3.app.Device, 640, 355 - BarAnimeCount / 1.5f, new Rectangle(0, 0, 642, 122));

								TJAPlayer3.Tx.ModeSelect_Bar_Text[i].Opacity = (int)(BarAnimeCount * 2.55f);
								TJAPlayer3.Tx.ModeSelect_Bar_Text[i]?.t2D中心基準描画(TJAPlayer3.app.Device, 640, 355 + 132 / 2, new Rectangle(0, 122, 642, 148));

							}
							else if (this.stModeBar[i].n現在存在している行 <= 2)
							{
								int BarAnimeY = ctBarAnimeIn.n現在の値 >= (int)(26 * 16.6f) + 100 && ctBarAnimeIn.n現在の値 <= (int)(26 * 16.6f) + 299 ? 600 - (ctBarAnimeIn.n現在の値 - (int)(26 * 16.6f + 100)) * 3 : ctBarAnimeIn.n現在の値 >= (int)(26 * 16.6f) + 100 ? 0 : 600;
								int BarAnimeX = ctBarAnimeIn.n現在の値 >= (int)(26 * 16.6f) + 100 && ctBarAnimeIn.n現在の値 <= (int)(26 * 16.6f) + 299 ? 100 - (int)((ctBarAnimeIn.n現在の値 - (int)(26 * 16.6f + 100)) * 0.5f) : ctBarAnimeIn.n現在の値 >= (int)(26 * 16.6f) + 100 ? 0 : 100;

								int BarMoveX = 0;
								int BarMoveY = 0;

								if (this.stModeBar[i].n現在存在している行 - n現在の選択行モード選択 != 0)
								{
									if (this.stModeBar[i].n現在存在している行 - this.n現在の選択行モード選択 != this.nbModes)
									{
										BarMoveX = ctBarMove.n現在の値 <= 100 ? (int)(this.ptモード選択バー座標[this.stModeBar[i].n現在存在している行].X - this.ptモード選択バー座標[this.n現在の選択行モード選択].X) - (int)(ctBarMove.n現在の値 / 100f * (this.ptモード選択バー座標[this.stModeBar[i].n現在存在している行].X - this.ptモード選択バー座標[this.n現在の選択行モード選択].X)) : 0;
										BarMoveY = ctBarMove.n現在の値 <= 100 ? (int)(this.ptモード選択バー座標[this.stModeBar[i].n現在存在している行].Y - this.ptモード選択バー座標[this.n現在の選択行モード選択].Y) - (int)(ctBarMove.n現在の値 / 100f * (this.ptモード選択バー座標[this.stModeBar[i].n現在存在している行].Y - this.ptモード選択バー座標[this.n現在の選択行モード選択].Y)) : 0;
									}
									else
                                    {
										BarMoveX = ctBarMove.n現在の値 <= 100 ? (int)(this.ptモード選択バー座標[this.stModeBar[i].n現在存在している行].X - this.ptモード選択バー座標[this.n現在の選択行モード選択 + 1].X) - (int)(ctBarMove.n現在の値 / 100f * (this.ptモード選択バー座標[this.stModeBar[i].n現在存在している行].X - this.ptモード選択バー座標[this.n現在の選択行モード選択 + 1].X)) : 0;
										BarMoveY = ctBarMove.n現在の値 <= 100 ? (int)(this.ptモード選択バー座標[this.stModeBar[i].n現在存在している行].Y - this.ptモード選択バー座標[this.n現在の選択行モード選択 + 1].Y) - (int)(ctBarMove.n現在の値 / 100f * (this.ptモード選択バー座標[this.stModeBar[i].n現在存在している行].Y - this.ptモード選択バー座標[this.n現在の選択行モード選択 + 1].Y)) : 0;
									}
								}
								else
								{
									BarMoveX = ctBarMove.n現在の値 <= 100 ? (int)(this.ptモード選択バー座標[this.stModeBar[i].n現在存在している行 - 1].X - this.ptモード選択バー座標[this.n現在の選択行モード選択].X) - (int)(ctBarMove.n現在の値 / 100f * (this.ptモード選択バー座標[this.stModeBar[i].n現在存在している行 - 1].X - this.ptモード選択バー座標[this.n現在の選択行モード選択].X)) : 0;
									BarMoveY = ctBarMove.n現在の値 <= 100 ? (int)(this.ptモード選択バー座標[this.stModeBar[i].n現在存在している行 - 1].Y - this.ptモード選択バー座標[this.n現在の選択行モード選択].Y) - (int)(ctBarMove.n現在の値 / 100f * (this.ptモード選択バー座標[this.stModeBar[i].n現在存在している行 - 1].Y - this.ptモード選択バー座標[this.n現在の選択行モード選択].Y)) : 0;
								}

								this.stModeBar[i].BarTexture.vc拡大縮小倍率.Y = 1.0f;
								TJAPlayer3.Tx.ModeSelect_Bar[this.nbModes].vc拡大縮小倍率.Y = 1.0f;
								this.stModeBar[i].BarTexture.t2D描画(TJAPlayer3.app.Device, this.ptモード選択バー座標[stModeBar[i].n現在存在している行].X + BarAnimeX - BarMoveX, this.ptモード選択バー座標[stModeBar[i].n現在存在している行].Y + BarAnimeY - BarMoveY);
								TJAPlayer3.Tx.ModeSelect_Bar[this.nbModes].t2D描画(TJAPlayer3.app.Device, this.ptモード選択バー座標[stModeBar[i].n現在存在している行].X + BarAnimeX - BarMoveX, this.ptモード選択バー座標[stModeBar[i].n現在存在している行].Y + BarAnimeY - BarMoveY);
								TJAPlayer3.Tx.ModeSelect_Bar_Text[i]?.t2D描画(TJAPlayer3.app.Device, this.ptモード選択バー座標[stModeBar[i].n現在存在している行].X + BarAnimeX - BarMoveX, this.ptモード選択バー座標[stModeBar[i].n現在存在している行].Y + BarAnimeY - BarMoveY - 13, new Rectangle(0, 0, 642, 122));
							}
                        }
					}

					TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.SongSelect_NamePlate_X[0], TJAPlayer3.Skin.SongSelect_NamePlate_Y[0], 0, false, 255);
				}

                #endregion

                #region[ バージョン表示 ]

#if DEBUG

                //string strVersion = "KTT:J:A:I:2017072200";
                string strCreator = "https://github.com/AioiLight/TJAPlayer3";
                AssemblyName asmApp = Assembly.GetExecutingAssembly().GetName();
                TJAPlayer3.act文字コンソール.tPrint(4, 44, C文字コンソール.Eフォント種別.白, "DEBUG BUILD");
				TJAPlayer3.act文字コンソール.tPrint(4, 4, C文字コンソール.Eフォント種別.白, asmApp.Name + " Ver." + TJAPlayer3.VERSION + " (" + strCreator + ")" );
                TJAPlayer3.act文字コンソール.tPrint(4, 24, C文字コンソール.Eフォント種別.白, "Skin:" + TJAPlayer3.Skin.Skin_Name + " Ver." + TJAPlayer3.Skin.Skin_Version + " (" + TJAPlayer3.Skin.Skin_Creator + ")");
                //CDTXMania.act文字コンソール.tPrint(4, 24, C文字コンソール.Eフォント種別.白, strSubTitle);
                TJAPlayer3.act文字コンソール.tPrint(4, (720 - 24), C文字コンソール.Eフォント種別.白, "TJAPlayer3 forked TJAPlayer2 forPC(kairera0467)");

#endif
                #endregion
				
				CStage.Eフェーズ eフェーズid = base.eフェーズID;
				switch( eフェーズid )
				{
					case CStage.Eフェーズ.共通_フェードイン:
						if( this.actFI.On進行描画() != 0 )
						{
							base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
						}
						break;

					case CStage.Eフェーズ.共通_フェードアウト:
						if( this.actFO.On進行描画() == 0 )
						{
							TJAPlayer3.Skin.bgmタイトル.t停止する();
							TJAPlayer3.Skin.bgmタイトルイン.t停止する();
							break;
						}
						base.eフェーズID = CStage.Eフェーズ.共通_終了状態;
						
						switch ( this.n現在の選択行モード選択)
						{
							case (int)E戻り値.GAMESTART - 1:
								return (int)E戻り値.GAMESTART;

							case (int)E戻り値.DANGAMESTART - 1:
								return (int)E戻り値.DANGAMESTART;

							case (int)E戻り値.TAIKOTOWERSSTART - 1:
								return (int)E戻り値.TAIKOTOWERSSTART;

							case (int)E戻り値.SHOPSTART - 1:
								return (int)E戻り値.SHOPSTART;

							case (int)E戻り値.BOUKENSTART - 1:
								return (int)E戻り値.BOUKENSTART;

							case (int) E戻り値.CONFIG - 1:
								return (int) E戻り値.CONFIG;

							case (int)E戻り値.EXIT - 1:
								return (int) E戻り値.EXIT;
						}
						break;

					case CStage.Eフェーズ.タイトル_起動画面からのフェードイン:
						if( this.actFIfromSetup.On進行描画() != 0 )
						{
							base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
						}
						break;
				}
			}
			return 0;
		}
		public enum E戻り値
		{
			継続 = 0,
			GAMESTART,
			DANGAMESTART,
			TAIKOTOWERSSTART,
			SHOPSTART,
			BOUKENSTART,
			CONFIG,
			EXIT
		}


		// その他

		#region [ private ]
		//-----------------
		private CCounter ctコインイン待機;

		private CCounter ctバナパス読み込み待機;

		private CCounter ctバナパス読み込み成功;
		private CCounter ctバナパス読み込み失敗;

		private CCounter ctエントリーバー点滅;
		private CCounter ctエントリーバー決定点滅;

		private CCounter ctどんちゃんエントリーループ;
		private CCounter ctどんちゃんイン;
		private CCounter ctどんちゃんループ;

		private CCounter ctBarAnimeIn;
		private CCounter ctBarMove;

		

		private bool bバナパス読み込み;
		private bool bバナパス読み込み失敗;
		private bool bプレイヤーエントリー;
		private bool bプレイヤーエントリー決定;
		private bool bモード選択;
		private bool bどんちゃんカウンター初期化;

		private int n現在の選択行プレイヤーエントリー;
		private int n現在の選択行モード選択;

		private Point[] ptプレイヤーエントリーバー座標 =
			{ new Point(337, 488), new Point( 529, 487), new Point(743, 486) };

		//private Point[] ptモード選択バー座標 =
		//	{ new Point(290, 107), new Point(319, 306), new Point(356, 513), new Point(385, 712), new Point(414, 911), new Point(443, 1110), new Point(472, 1309) };

		
		private Point[] ptモード選択バー座標 =
			{ new Point(290, 107), new Point(319, 306), new Point(356, 513), new Point(385, 712), new Point(385, 712), new Point(385, 712), new Point(385, 712) };

		private int nbModes = 6;
		private STModeBar[] stModeBar = new STModeBar[6];

		private struct STModeBar
		{
			public int n現在存在している行;
			public CTexture BarTexture;
		}

		private bool b音声再生;
		private CActFIFOBlack actFI;
		private CActFIFOBlack actFIfromSetup;
		private CActFIFOBlack actFO;
		//-----------------
		#endregion
	}
}
