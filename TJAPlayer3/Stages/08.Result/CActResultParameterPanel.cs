using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.IO;
using FDK;

namespace TJAPlayer3
{
	internal class CActResultParameterPanel : CActivity
	{
		// コンストラクタ

		public CActResultParameterPanel()
		{
			ST文字位置[] st文字位置Array = new ST文字位置[11];
			ST文字位置 st文字位置 = new ST文字位置();
			st文字位置.ch = '0';
			st文字位置.pt = new Point(0, 0);
			st文字位置Array[0] = st文字位置;
			ST文字位置 st文字位置2 = new ST文字位置();
			st文字位置2.ch = '1';
			st文字位置2.pt = new Point(32, 0);
			st文字位置Array[1] = st文字位置2;
			ST文字位置 st文字位置3 = new ST文字位置();
			st文字位置3.ch = '2';
			st文字位置3.pt = new Point(64, 0);
			st文字位置Array[2] = st文字位置3;
			ST文字位置 st文字位置4 = new ST文字位置();
			st文字位置4.ch = '3';
			st文字位置4.pt = new Point(96, 0);
			st文字位置Array[3] = st文字位置4;
			ST文字位置 st文字位置5 = new ST文字位置();
			st文字位置5.ch = '4';
			st文字位置5.pt = new Point(128, 0);
			st文字位置Array[4] = st文字位置5;
			ST文字位置 st文字位置6 = new ST文字位置();
			st文字位置6.ch = '5';
			st文字位置6.pt = new Point(160, 0);
			st文字位置Array[5] = st文字位置6;
			ST文字位置 st文字位置7 = new ST文字位置();
			st文字位置7.ch = '6';
			st文字位置7.pt = new Point(192, 0);
			st文字位置Array[6] = st文字位置7;
			ST文字位置 st文字位置8 = new ST文字位置();
			st文字位置8.ch = '7';
			st文字位置8.pt = new Point(224, 0);
			st文字位置Array[7] = st文字位置8;
			ST文字位置 st文字位置9 = new ST文字位置();
			st文字位置9.ch = '8';
			st文字位置9.pt = new Point(256, 0);
			st文字位置Array[8] = st文字位置9;
			ST文字位置 st文字位置10 = new ST文字位置();
			st文字位置10.ch = '9';
			st文字位置10.pt = new Point(288, 0);
			st文字位置Array[9] = st文字位置10;
			ST文字位置 st文字位置11 = new ST文字位置();
			st文字位置11.ch = ' ';
			st文字位置11.pt = new Point(0, 0);
			st文字位置Array[10] = st文字位置11;
			this.st小文字位置 = st文字位置Array;

			ST文字位置[] st文字位置Array2 = new ST文字位置[11];
			ST文字位置 st文字位置12 = new ST文字位置();
			st文字位置12.ch = '0';
			st文字位置12.pt = new Point(0, 0);
			st文字位置Array2[0] = st文字位置12;
			ST文字位置 st文字位置13 = new ST文字位置();
			st文字位置13.ch = '1';
			st文字位置13.pt = new Point(32, 0);
			st文字位置Array2[1] = st文字位置13;
			ST文字位置 st文字位置14 = new ST文字位置();
			st文字位置14.ch = '2';
			st文字位置14.pt = new Point(64, 0);
			st文字位置Array2[2] = st文字位置14;
			ST文字位置 st文字位置15 = new ST文字位置();
			st文字位置15.ch = '3';
			st文字位置15.pt = new Point(96, 0);
			st文字位置Array2[3] = st文字位置15;
			ST文字位置 st文字位置16 = new ST文字位置();
			st文字位置16.ch = '4';
			st文字位置16.pt = new Point(128, 0);
			st文字位置Array2[4] = st文字位置16;
			ST文字位置 st文字位置17 = new ST文字位置();
			st文字位置17.ch = '5';
			st文字位置17.pt = new Point(160, 0);
			st文字位置Array2[5] = st文字位置17;
			ST文字位置 st文字位置18 = new ST文字位置();
			st文字位置18.ch = '6';
			st文字位置18.pt = new Point(192, 0);
			st文字位置Array2[6] = st文字位置18;
			ST文字位置 st文字位置19 = new ST文字位置();
			st文字位置19.ch = '7';
			st文字位置19.pt = new Point(224, 0);
			st文字位置Array2[7] = st文字位置19;
			ST文字位置 st文字位置20 = new ST文字位置();
			st文字位置20.ch = '8';
			st文字位置20.pt = new Point(256, 0);
			st文字位置Array2[8] = st文字位置20;
			ST文字位置 st文字位置21 = new ST文字位置();
			st文字位置21.ch = '9';
			st文字位置21.pt = new Point(288, 0);
			st文字位置Array2[9] = st文字位置21;
			ST文字位置 st文字位置22 = new ST文字位置();
			st文字位置22.ch = '%';
			st文字位置22.pt = new Point(0x37, 0);
			st文字位置Array2[10] = st文字位置22;
			this.st大文字位置 = st文字位置Array2;

			ST文字位置[] stScore文字位置Array = new ST文字位置[10];
			ST文字位置 stScore文字位置 = new ST文字位置();
			stScore文字位置.ch = '0';
			stScore文字位置.pt = new Point(0, 0);
			stScore文字位置Array[0] = stScore文字位置;
			ST文字位置 stScore文字位置2 = new ST文字位置();
			stScore文字位置2.ch = '1';
			stScore文字位置2.pt = new Point(51, 0);
			stScore文字位置Array[1] = stScore文字位置2;
			ST文字位置 stScore文字位置3 = new ST文字位置();
			stScore文字位置3.ch = '2';
			stScore文字位置3.pt = new Point(102, 0);
			stScore文字位置Array[2] = stScore文字位置3;
			ST文字位置 stScore文字位置4 = new ST文字位置();
			stScore文字位置4.ch = '3';
			stScore文字位置4.pt = new Point(153, 0);
			stScore文字位置Array[3] = stScore文字位置4;
			ST文字位置 stScore文字位置5 = new ST文字位置();
			stScore文字位置5.ch = '4';
			stScore文字位置5.pt = new Point(204, 0);
			stScore文字位置Array[4] = stScore文字位置5;
			ST文字位置 stScore文字位置6 = new ST文字位置();
			stScore文字位置6.ch = '5';
			stScore文字位置6.pt = new Point(255, 0);
			stScore文字位置Array[5] = stScore文字位置6;
			ST文字位置 stScore文字位置7 = new ST文字位置();
			stScore文字位置7.ch = '6';
			stScore文字位置7.pt = new Point(306, 0);
			stScore文字位置Array[6] = stScore文字位置7;
			ST文字位置 stScore文字位置8 = new ST文字位置();
			stScore文字位置8.ch = '7';
			stScore文字位置8.pt = new Point(357, 0);
			stScore文字位置Array[7] = stScore文字位置8;
			ST文字位置 stScore文字位置9 = new ST文字位置();
			stScore文字位置9.ch = '8';
			stScore文字位置9.pt = new Point(408, 0);
			stScore文字位置Array[8] = stScore文字位置9;
			ST文字位置 stScore文字位置10 = new ST文字位置();
			stScore文字位置10.ch = '9';
			stScore文字位置10.pt = new Point(459, 0);
			stScore文字位置Array[9] = stScore文字位置10;
			this.stScoreFont = stScore文字位置Array;

			base.list子Activities.Add(this.PuchiChara = new PuchiChara());

			this.ptFullCombo位置 = new Point[] { new Point(0x80, 0xed), new Point(0xdf, 0xed), new Point(0x141, 0xed) };
			base.b活性化してない = true;
		}


		// メソッド

		public void tアニメを完了させる()
		{
			this.ct表示用.n現在の値 = (int)this.ct表示用.n終了値;
		}


		public void tSkipResultAnimations()
        {
			ct全体進行.n現在の値 = (int)MountainAppearValue;
			
			for (int i = 0; i < 9; i++)
            {
				b音声再生[i] = true;
			}

			for (int i = 0; i < 5; i++)
            {
				if (!ctゲージアニメ[i].b進行中)
					ctゲージアニメ[i].t開始(0, gaugeValues[i] / 2, 59, TJAPlayer3.Timer);
				ctゲージアニメ[i].n現在の値 = (int)ctゲージアニメ[i].n終了値;
			}
			
			TJAPlayer3.Skin.soundGauge.t停止する();
		}

		// CActivity 実装

		public override void On活性化()
		{
			this.sdDTXで指定されたフルコンボ音 = null;

			base.On活性化();
		}
		public override void On非活性化()
		{
			if (this.ct表示用 != null)
			{
				this.ct表示用 = null;
			}

			for(int i = 0; i < this.b音声再生.Length; i++)
            {
				b音声再生[i] = false;
            }

			if (this.sdDTXで指定されたフルコンボ音 != null)
			{
				TJAPlayer3.Sound管理.tサウンドを破棄する(this.sdDTXで指定されたフルコンボ音);
				this.sdDTXで指定されたフルコンボ音 = null;
			}
			base.On非活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if (!base.b活性化してない)
			{
				ct全体進行 = new CCounter(0, 50000, 1, TJAPlayer3.Timer);

				ctゲージアニメ = new CCounter[5];
				for (int i = 0; i < 5; i++)
					ctゲージアニメ[i] = new CCounter();

				ct虹ゲージアニメ = new CCounter();

				ctSoul = new CCounter();

				ctEndAnime = new CCounter();
				ctBackgroundAnime = new CCounter(0, 1000, 1, TJAPlayer3.Timer);
				ctBackgroundAnime_Clear = new CCounter(0, 1000, 1, TJAPlayer3.Timer);
				ctMountain_ClearIn = new CCounter();

				RandomText = TJAPlayer3.Random.Next(3);

				ctFlash_Icon = new CCounter(0, 3000, 1, TJAPlayer3.Timer);
				ctRotate_Flowers = new CCounter(0, 1500, 1, TJAPlayer3.Timer);
				ctShine_Plate = new CCounter(0, 1000, 1, TJAPlayer3.Timer);

				ctAISectionChange = new CCounter(0, 2000, 1, TJAPlayer3.Timer);
				ctAISectionChange.n現在の値 = 255;

				pfAISectionText = new CPrivateFastFont(new FontFamily(TJAPlayer3.ConfigIni.FontName), TJAPlayer3.Skin.Result_AIBattle_SectionText_Scale);

				ttkAISection = new CActSelect曲リスト.TitleTextureKey[TJAPlayer3.stage演奏ドラム画面.AIBattleSections.Count];
                for (int i = 0; i < ttkAISection.Length; i++)
                {
					ttkAISection[i] = new CActSelect曲リスト.TitleTextureKey($"{i + 1}区", pfAISectionText, Color.White, Color.Black, 1280);

				}

				for (int i = 0; i < 5; i++)
                {
					CResultCharacter.tMenuResetTimer(CResultCharacter.ECharacterResult.NORMAL);
					CResultCharacter.tDisableCounter(CResultCharacter.ECharacterResult.CLEAR);
					CResultCharacter.tDisableCounter(CResultCharacter.ECharacterResult.FAILED);
					CResultCharacter.tDisableCounter(CResultCharacter.ECharacterResult.FAILED_IN);
				}
				

				gaugeValues = new int[5];
				for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
					gaugeValues[i] = (int)TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値[i];
				}

				// Replace by max between 2 gauges if 2p
				GaugeFactor = Math.Max(Math.Max(Math.Max(Math.Max(gaugeValues[0], gaugeValues[1]), gaugeValues[2]), gaugeValues[3]), gaugeValues[4]) / 2;
				
				MountainAppearValue = 10275 + (66 * GaugeFactor);

				this.PuchiChara.IdleAnimation();

				base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if (!base.b活性化してない)
			{
				TJAPlayer3.t安全にDisposeする(ref pfAISectionText);

				base.OnManagedリソースの解放();
			}
		}
		public override int On進行描画()
		{
			if (base.b活性化してない)
			{
				return 0;
			}
			if (base.b初めての進行描画)
			{
				this.ct表示用 = new CCounter(0, 0x3e7, 2, TJAPlayer3.Timer);
				base.b初めての進行描画 = false;
			}
			this.ct表示用.t進行();

			ct全体進行.t進行();

			for (int i = 0; i < 5; i++)
				ctゲージアニメ[i].t進行();

			ctEndAnime.t進行();
			ctBackgroundAnime.t進行Loop();
			ctMountain_ClearIn.t進行();

			ctFlash_Icon.t進行Loop();
			ctRotate_Flowers.t進行Loop();
			ctShine_Plate.t進行Loop();

			ctAISectionChange.t進行();

			// this.PuchiChara.IdleAnimation();

			if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower)
			{
				int[] namePlate_x = new int[5];
				int[] namePlate_y = new int[5];

				for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
				{
					if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
					{
						namePlate_x[i] = TJAPlayer3.Skin.Result_NamePlate_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * i);
						namePlate_y[i] = TJAPlayer3.Skin.Result_NamePlate_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * i);
					}
					else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
					{
						namePlate_x[i] = TJAPlayer3.Skin.Result_NamePlate_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * i);
						namePlate_y[i] = TJAPlayer3.Skin.Result_NamePlate_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * i);
					}
					else
					{
						namePlate_x[i] = TJAPlayer3.Skin.Result_NamePlate_X[i];
						namePlate_y[i] = TJAPlayer3.Skin.Result_NamePlate_Y[i];
					}
				}

				#region [ Ensou result contents ]

				int AnimeCount = 3000 + GaugeFactor * 59;
				int ScoreApparitionTimeStamp = AnimeCount + 420 * 4 + 840;

				bool is1P = (TJAPlayer3.ConfigIni.nPlayerCount == 1);
				bool is2PSide = TJAPlayer3.P1IsBlue();

				int shift = 635;

				for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
				{
					if (TJAPlayer3.ConfigIni.bAIBattleMode && i == 1) break;

					// 1 if right, 0 if left
					int shiftPos = (i == 1 || is2PSide) ? 1 : i;
					int pos = i;
					if (is2PSide)
						pos = 1;


					#region [General plate animations]

					if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
					{
						if (shiftPos == 0)
							TJAPlayer3.Tx.Result_Panel.t2D描画(TJAPlayer3.app.Device, 0, 0);
						else
							TJAPlayer3.Tx.Result_Panel_2P.t2D描画(TJAPlayer3.app.Device, 0, 0);
					}
                    else
					{
						if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
						{
							TJAPlayer3.Tx.Result_Panel_5P[i].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_UIMove_5P[0] * i, 0);
						}
						else
						{
							TJAPlayer3.Tx.Result_Panel_4P[i].t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_UIMove_4P[0] * i, 0);
						}
					}

					//if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
					{
						int bar_x;
						int bar_y;
						int gauge_base_x;
						int gauge_base_y;
						if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
						{
							TJAPlayer3.Tx.Result_Gauge_Base[shiftPos].vc拡大縮小倍率.X = 0.5f;
							bar_x = TJAPlayer3.Skin.Result_DifficultyBar_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
							bar_y = TJAPlayer3.Skin.Result_DifficultyBar_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);
							gauge_base_x = TJAPlayer3.Skin.Result_Gauge_Base_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
							gauge_base_y = TJAPlayer3.Skin.Result_Gauge_Base_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);
						}
						else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
						{
							TJAPlayer3.Tx.Result_Gauge_Base[shiftPos].vc拡大縮小倍率.X = 0.5f;
							bar_x = TJAPlayer3.Skin.Result_DifficultyBar_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
							bar_y = TJAPlayer3.Skin.Result_DifficultyBar_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);
							gauge_base_x = TJAPlayer3.Skin.Result_Gauge_Base_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
							gauge_base_y = TJAPlayer3.Skin.Result_Gauge_Base_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);
						}
						else
						{
							TJAPlayer3.Tx.Result_Gauge_Base[shiftPos].vc拡大縮小倍率.X = 1.0f;
							bar_x = TJAPlayer3.Skin.Result_DifficultyBar_X[pos];
							bar_y = TJAPlayer3.Skin.Result_DifficultyBar_Y[pos];
							gauge_base_x = TJAPlayer3.Skin.Result_Gauge_Base_X[pos];
							gauge_base_y = TJAPlayer3.Skin.Result_Gauge_Base_Y[pos];
						}

						TJAPlayer3.Tx.Result_Diff_Bar.t2D描画(TJAPlayer3.app.Device, bar_x, bar_y,
						new RectangleF(0, TJAPlayer3.stage選曲.n確定された曲の難易度[i] * TJAPlayer3.Skin.Result_DifficultyBar_Size[1], TJAPlayer3.Skin.Result_DifficultyBar_Size[0], TJAPlayer3.Skin.Result_DifficultyBar_Size[1]));

						TJAPlayer3.Tx.Result_Gauge_Base[shiftPos].t2D描画(TJAPlayer3.app.Device, gauge_base_x, gauge_base_y);
						TJAPlayer3.Tx.Result_Gauge_Base[shiftPos].vc拡大縮小倍率.X = 1.0f;
					}

					if (ct全体進行.n現在の値 >= 2000)
					{
						#region [ Gauge updates ]

						if (!b音声再生[0])
						{
							TJAPlayer3.Skin.soundGauge.t再生する();
							b音声再生[0] = true;
						}

						// Split gauge counter, one for each player in two
						if (!ctゲージアニメ[i].b進行中)
						{
							ctゲージアニメ[i].t開始(0, gaugeValues[i] / 2, 59, TJAPlayer3.Timer);
							if (ct全体進行.n現在の値 >= MountainAppearValue)
								ctゲージアニメ[i].n現在の値 = (int)ctゲージアニメ[i].n終了値;
						}

						{
							int gauge_x;
							int gauge_y;
							if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
							{
								TJAPlayer3.Tx.Result_Gauge[shiftPos].vc拡大縮小倍率.X = 0.5f;
								gauge_x = TJAPlayer3.Skin.Result_Gauge_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
								gauge_y = TJAPlayer3.Skin.Result_Gauge_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);
							}
							else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
							{
								TJAPlayer3.Tx.Result_Gauge[shiftPos].vc拡大縮小倍率.X = 0.5f;
								gauge_x = TJAPlayer3.Skin.Result_Gauge_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
								gauge_y = TJAPlayer3.Skin.Result_Gauge_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);
							}
							else
							{
								TJAPlayer3.Tx.Result_Gauge[shiftPos].vc拡大縮小倍率.X = 1.0f;
								gauge_x = TJAPlayer3.Skin.Result_Gauge_X[pos];
								gauge_y = TJAPlayer3.Skin.Result_Gauge_Y[pos];
							}

							TJAPlayer3.Tx.Result_Gauge[shiftPos].t2D描画(TJAPlayer3.app.Device, gauge_x, gauge_y,
							new RectangleF(TJAPlayer3.Skin.Result_Gauge_Rect[0], TJAPlayer3.Skin.Result_Gauge_Rect[1], (TJAPlayer3.Skin.Result_Gauge_Rect[2] / 50.0f) * ctゲージアニメ[i].n現在の値, TJAPlayer3.Skin.Result_Gauge_Rect[3]));
							TJAPlayer3.Tx.Result_Gauge[shiftPos].vc拡大縮小倍率.X = 1.0f;
						}
						// Modify to array for each players using i


						int soultext_width = TJAPlayer3.Tx.Result_Soul_Text.szテクスチャサイズ.Width / 3;
						int soultext_height = TJAPlayer3.Tx.Result_Soul_Text.szテクスチャサイズ.Height;

						int soulText_x;
						int soulText_y;
						int soulFire_x;
						int soulFire_y;
						if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
						{
							soulText_x = TJAPlayer3.Skin.Result_Soul_Text_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
							soulText_y = TJAPlayer3.Skin.Result_Soul_Text_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);
							soulFire_x = TJAPlayer3.Skin.Result_Soul_Fire_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
							soulFire_y = TJAPlayer3.Skin.Result_Soul_Fire_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);
							TJAPlayer3.Tx.Result_Rainbow[ct虹ゲージアニメ.n現在の値].vc拡大縮小倍率.X = 0.5f;
						}
						else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
						{
							soulText_x = TJAPlayer3.Skin.Result_Soul_Text_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
							soulText_y = TJAPlayer3.Skin.Result_Soul_Text_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);
							soulFire_x = TJAPlayer3.Skin.Result_Soul_Fire_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
							soulFire_y = TJAPlayer3.Skin.Result_Soul_Fire_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);
							TJAPlayer3.Tx.Result_Rainbow[ct虹ゲージアニメ.n現在の値].vc拡大縮小倍率.X = 0.5f;
						}
						else
						{
							soulText_x = TJAPlayer3.Skin.Result_Soul_Text_X[pos];
							soulText_y = TJAPlayer3.Skin.Result_Soul_Text_Y[pos];
							soulFire_x = TJAPlayer3.Skin.Result_Soul_Fire_X[pos];
							soulFire_y = TJAPlayer3.Skin.Result_Soul_Fire_Y[pos];
							TJAPlayer3.Tx.Result_Rainbow[ct虹ゲージアニメ.n現在の値].vc拡大縮小倍率.X = 1.0f;
						}

						if (ctゲージアニメ[i].b終了値に達した)
						{
							if (ctゲージアニメ[i].n現在の値 != 50)
                            {
								// Gauge didn't reach rainbow
								if (TJAPlayer3.ConfigIni.nPlayerCount < 2
									|| ctゲージアニメ[(i == 0) ? 1 : 0].b終了値に達した)
									TJAPlayer3.Skin.soundGauge.t停止する();
							}
							else
							{
								// Gauge reached rainbow
								if (!TJAPlayer3.Skin.soundGauge.b再生中)
                                {
									TJAPlayer3.Skin.soundGauge.t停止する();
								}
									
								if (!ct虹ゲージアニメ.b進行中)
									ct虹ゲージアニメ.t開始(0, 40, 1000 / 60, TJAPlayer3.Timer);

								if (!ctSoul.b進行中)
									ctSoul.t開始(0, 8, 33, TJAPlayer3.Timer);

								ct虹ゲージアニメ.t進行Loop();
								ctSoul.t進行Loop();

								int rainbow_x;
								int rainbow_y;
								if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
								{
									rainbow_x = TJAPlayer3.Skin.Result_Gauge_Rainbow_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
									rainbow_y = TJAPlayer3.Skin.Result_Gauge_Rainbow_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);
									TJAPlayer3.Tx.Result_Rainbow[ct虹ゲージアニメ.n現在の値].vc拡大縮小倍率.X = 0.5f;
								}
								else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
								{
									rainbow_x = TJAPlayer3.Skin.Result_Gauge_Rainbow_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
									rainbow_y = TJAPlayer3.Skin.Result_Gauge_Rainbow_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);
									TJAPlayer3.Tx.Result_Rainbow[ct虹ゲージアニメ.n現在の値].vc拡大縮小倍率.X = 0.5f;
								}
								else
								{
									rainbow_x = TJAPlayer3.Skin.Result_Gauge_Rainbow_X[pos];
									rainbow_y = TJAPlayer3.Skin.Result_Gauge_Rainbow_Y[pos];
									TJAPlayer3.Tx.Result_Rainbow[ct虹ゲージアニメ.n現在の値].vc拡大縮小倍率.X = 1.0f;
								}
								TJAPlayer3.Tx.Result_Rainbow[ct虹ゲージアニメ.n現在の値].t2D描画(TJAPlayer3.app.Device, rainbow_x, rainbow_y);

								{
									int soulfire_width = TJAPlayer3.Tx.Result_Soul_Fire.szテクスチャサイズ.Width / 8;
									int soulfire_height = TJAPlayer3.Tx.Result_Soul_Fire.szテクスチャサイズ.Height;

									TJAPlayer3.Tx.Result_Soul_Fire.t2D中心基準描画(TJAPlayer3.app.Device, soulFire_x, soulFire_y, new Rectangle(soulfire_width * ctSoul.n現在の値, 0, soulfire_width, soulfire_height));

									TJAPlayer3.Tx.Result_Soul_Text.t2D中心基準描画(TJAPlayer3.app.Device, soulText_x, soulText_y, new Rectangle(soultext_width * 1, 0, soultext_width, soultext_height));

									if (ctSoul.n現在の値 % 2 == 0)
										TJAPlayer3.Tx.Result_Soul_Text.t2D中心基準描画(TJAPlayer3.app.Device, soulText_x, soulText_y, new Rectangle(soultext_width * 2, 0, soultext_width, soultext_height));
								}
							}
						}
						if (ctゲージアニメ[i].n現在の値 != 50)
						{
							{
								int clearText_x;
								int clearText_y;
								if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
								{
									clearText_x = TJAPlayer3.Skin.Result_Gauge_ClearText_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
									clearText_y = TJAPlayer3.Skin.Result_Gauge_ClearText_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);
									TJAPlayer3.Tx.Result_Gauge[shiftPos].vc拡大縮小倍率.X = 0.5f;
								}
								else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
								{
									clearText_x = TJAPlayer3.Skin.Result_Gauge_ClearText_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
									clearText_y = TJAPlayer3.Skin.Result_Gauge_ClearText_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);
									TJAPlayer3.Tx.Result_Gauge[shiftPos].vc拡大縮小倍率.X = 0.5f;
								}
								else
								{
									clearText_x = TJAPlayer3.Skin.Result_Gauge_ClearText_X[pos];
									clearText_y = TJAPlayer3.Skin.Result_Gauge_ClearText_Y[pos];
									TJAPlayer3.Tx.Result_Gauge[shiftPos].vc拡大縮小倍率.X = 1.0f;
								}
								if (ctゲージアニメ[i].n現在の値 < 40)
								{
									TJAPlayer3.Tx.Result_Gauge[shiftPos].t2D描画(TJAPlayer3.app.Device, clearText_x, clearText_y,
										new RectangleF(TJAPlayer3.Skin.Result_Gauge_ClearText_Rect[0], TJAPlayer3.Skin.Result_Gauge_ClearText_Rect[1], TJAPlayer3.Skin.Result_Gauge_ClearText_Rect[2], TJAPlayer3.Skin.Result_Gauge_ClearText_Rect[3]));
								}
								else
								{
									TJAPlayer3.Tx.Result_Gauge[shiftPos].t2D描画(TJAPlayer3.app.Device, clearText_x, clearText_y,
										   new RectangleF(TJAPlayer3.Skin.Result_Gauge_ClearText_Clear_Rect[0], TJAPlayer3.Skin.Result_Gauge_ClearText_Clear_Rect[1], TJAPlayer3.Skin.Result_Gauge_ClearText_Clear_Rect[2], TJAPlayer3.Skin.Result_Gauge_ClearText_Clear_Rect[3]));
								}

								TJAPlayer3.Tx.Result_Soul_Text.t2D中心基準描画(TJAPlayer3.app.Device, soulText_x, soulText_y,
									new Rectangle(soultext_width * (ctゲージアニメ[i].n現在の値 <= 30 ? 0 : 1), 0, soultext_width, soultext_height));
							}
						}

						#endregion
					}

					if (ct全体進行.n現在の値 >= 2000)
					{
						// Change score kiroku to total scores to have the contents for both players, unbloat it
						{
							#region [ Separate results display (excluding score) ]

							int Interval = 420;

							float AddCount = 135;

							int[] scoresArr =
							{
							TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nGreat,
							TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nGood,
							TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nMiss,
							//TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nRoll.ToString(),
							TJAPlayer3.stage演奏ドラム画面.GetRoll(i),
							TJAPlayer3.stage演奏ドラム画面.actCombo.n現在のコンボ数.最高値[i]
						};

							int[][] num_x;

							int[][] num_y;
							if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
							{
								num_x = new int[][] { new int[5], new int[5], new int[5], new int[5], new int[5] };
								num_y = new int[][] { new int[5], new int[5], new int[5], new int[5], new int[5] };

								num_x[0][pos] = TJAPlayer3.Skin.Result_Perfect_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
								num_y[0][pos] = TJAPlayer3.Skin.Result_Perfect_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);

								num_x[1][pos] = TJAPlayer3.Skin.Result_Good_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
								num_y[1][pos] = TJAPlayer3.Skin.Result_Good_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);

								num_x[2][pos] = TJAPlayer3.Skin.Result_Miss_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
								num_y[2][pos] = TJAPlayer3.Skin.Result_Miss_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);

								num_x[3][pos] = TJAPlayer3.Skin.Result_Roll_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
								num_y[3][pos] = TJAPlayer3.Skin.Result_Roll_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);

								num_x[4][pos] = TJAPlayer3.Skin.Result_MaxCombo_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
								num_y[4][pos] = TJAPlayer3.Skin.Result_MaxCombo_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);
							}
							else if (TJAPlayer3.ConfigIni.nPlayerCount > 2)
							{
								num_x = new int[][] { new int[5], new int[5], new int[5], new int[5], new int[5] };
								num_y = new int[][] { new int[5], new int[5], new int[5], new int[5], new int[5] };

								num_x[0][pos] = TJAPlayer3.Skin.Result_Perfect_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
								num_y[0][pos] = TJAPlayer3.Skin.Result_Perfect_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);

								num_x[1][pos] = TJAPlayer3.Skin.Result_Good_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
								num_y[1][pos] = TJAPlayer3.Skin.Result_Good_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);

								num_x[2][pos] = TJAPlayer3.Skin.Result_Miss_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
								num_y[2][pos] = TJAPlayer3.Skin.Result_Miss_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);

								num_x[3][pos] = TJAPlayer3.Skin.Result_Roll_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
								num_y[3][pos] = TJAPlayer3.Skin.Result_Roll_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);

								num_x[4][pos] = TJAPlayer3.Skin.Result_MaxCombo_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
								num_y[4][pos] = TJAPlayer3.Skin.Result_MaxCombo_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);
							}
                            else
							{
								num_x = new int[][] {
									TJAPlayer3.Skin.Result_Perfect_X,
									TJAPlayer3.Skin.Result_Good_X,
									TJAPlayer3.Skin.Result_Miss_X,
									TJAPlayer3.Skin.Result_Roll_X,
									TJAPlayer3.Skin.Result_MaxCombo_X
								};

								num_y = new int[][] {
									TJAPlayer3.Skin.Result_Perfect_Y,
									TJAPlayer3.Skin.Result_Good_Y,
									TJAPlayer3.Skin.Result_Miss_Y,
									TJAPlayer3.Skin.Result_Roll_Y,
									TJAPlayer3.Skin.Result_MaxCombo_Y
								};
							}

							for (int k = 0; k < 5; k++)
							{
								if (ct全体進行.n現在の値 >= AnimeCount + (Interval * k))
								{
									TJAPlayer3.Tx.Result_Number.vc拡大縮小倍率.X = ct全体進行.n現在の値 <= AnimeCount + (Interval * k) + AddCount ? 1.3f - (float)Math.Sin((ct全体進行.n現在の値 - (AnimeCount + (Interval * k))) / (AddCount / 90) * (Math.PI / 180)) * 0.3f : 1.0f;
									TJAPlayer3.Tx.Result_Number.vc拡大縮小倍率.Y = ct全体進行.n現在の値 <= AnimeCount + (Interval * k) + AddCount ? 1.3f - (float)Math.Sin((ct全体進行.n現在の値 - (AnimeCount + (Interval * k))) / (AddCount / 90) * (Math.PI / 180)) * 0.3f : 1.0f;

									this.t小文字表示(num_x[k][pos], num_y[k][pos], scoresArr[k]);

									TJAPlayer3.Tx.Result_Number.vc拡大縮小倍率.X = 1f;
									TJAPlayer3.Tx.Result_Number.vc拡大縮小倍率.Y = 1f;

									if (!this.b音声再生[1 + k])
									{
										TJAPlayer3.Skin.soundPon.t再生する();
										this.b音声再生[1 + k] = true;
									}
								}
								else
									break;
							}

							#endregion

							#region [ Score display ]

							if (ct全体進行.n現在の値 >= AnimeCount + Interval * 4 + 840)
							{
								int score_x;
								int score_y;
								if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
								{
									score_x = TJAPlayer3.Skin.Result_Score_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
									score_y = TJAPlayer3.Skin.Result_Score_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);
								}
								else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
								{
									score_x = TJAPlayer3.Skin.Result_Score_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
									score_y = TJAPlayer3.Skin.Result_Score_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);
								}
								else
								{
									score_x = TJAPlayer3.Skin.Result_Score_X[pos];
									score_y = TJAPlayer3.Skin.Result_Score_Y[pos];
								}

								int AnimeCount1 = AnimeCount + Interval * 4 + 840;

								TJAPlayer3.Tx.Result_Score_Number.vc拡大縮小倍率.X = ct全体進行.n現在の値 <= AnimeCount1 + 270 ? 1.0f + (float)Math.Sin((ct全体進行.n現在の値 - AnimeCount1) / 1.5f * (Math.PI / 180)) * 0.65f :
																				  ct全体進行.n現在の値 <= AnimeCount1 + 360 ? 1.0f - (float)Math.Sin((ct全体進行.n現在の値 - AnimeCount1 - 270) * (Math.PI / 180)) * 0.1f : 1.0f;
								TJAPlayer3.Tx.Result_Score_Number.vc拡大縮小倍率.Y = ct全体進行.n現在の値 <= AnimeCount1 + 270 ? 1.0f + (float)Math.Sin((ct全体進行.n現在の値 - AnimeCount1) / 1.5f * (Math.PI / 180)) * 0.65f :
																				  ct全体進行.n現在の値 <= AnimeCount1 + 360 ? 1.0f - (float)Math.Sin((ct全体進行.n現在の値 - AnimeCount1 - 270) * (Math.PI / 180)) * 0.1f : 1.0f;

								this.tスコア文字表示(score_x, score_y, (int)TJAPlayer3.stage演奏ドラム画面.actScore.Get(E楽器パート.DRUMS, i));// TJAPlayer3.stage演奏ドラム画面.CChartScore[i].nScore.ToString()));

								if (!b音声再生[6])
								{
									TJAPlayer3.Skin.soundScoreDon.t再生する();
									b音声再生[6] = true;
								}
							}

							#endregion
						}
					}

					#endregion

				}

				if (ctAISectionChange.n現在の値 == ctAISectionChange.n終了値 && TJAPlayer3.stage演奏ドラム画面.AIBattleSections.Count > 5)
                {
					NextAISection();
                }
				else if (nNowAISection > 0 && TJAPlayer3.stage演奏ドラム画面.AIBattleSections.Count <= 5)
				{
					// Fix locked sections
					nNowAISection = 0;
                }

				if (TJAPlayer3.ConfigIni.bAIBattleMode)
                {
					TJAPlayer3.Tx.Result_AIBattle_Panel_AI.t2D描画(TJAPlayer3.app.Device, 0, 0);

					int batch_width = TJAPlayer3.Tx.Result_AIBattle_Batch.szテクスチャサイズ.Width / 3;
					int batch_height = TJAPlayer3.Tx.Result_AIBattle_Batch.szテクスチャサイズ.Height;


					for (int i = 0; i < TJAPlayer3.stage演奏ドラム画面.AIBattleSections.Count; i++)
					{
						int nowIndex = (i / 5);
						int drawCount = Math.Min(TJAPlayer3.stage演奏ドラム画面.AIBattleSections.Count - (nowIndex * 5), 5);

						int drawPos = i % 5;
						int batch_total_width = TJAPlayer3.Skin.Result_AIBattle_Batch_Move[0] * drawCount;

						var section = TJAPlayer3.stage演奏ドラム画面.AIBattleSections[i];
						int upDown = (drawPos % 2);

						int x = TJAPlayer3.Skin.Result_AIBattle_Batch[0] + (TJAPlayer3.Skin.Result_AIBattle_Batch_Move[0] * drawPos) - (batch_total_width / 2);
						int y = TJAPlayer3.Skin.Result_AIBattle_Batch[1] + (TJAPlayer3.Skin.Result_AIBattle_Batch_Move[1] * upDown);

						int opacityCounter = Math.Min(ctAISectionChange.n現在の値, 255);

						if (nowIndex == nNowAISection)
						{
							TJAPlayer3.Tx.Result_AIBattle_Batch.Opacity = opacityCounter;
							TJAPlayer3.Tx.Result_AIBattle_SectionPlate.Opacity = opacityCounter;
							if (TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkAISection[i]) != null)
								TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkAISection[i]).Opacity = opacityCounter;
						}
						else
						{
							TJAPlayer3.Tx.Result_AIBattle_Batch.Opacity = 255 - opacityCounter;
							TJAPlayer3.Tx.Result_AIBattle_SectionPlate.Opacity = 255 - opacityCounter;
							if (TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkAISection[i]) != null)
								TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkAISection[i]).Opacity = 255 - opacityCounter;
						}

						TJAPlayer3.Tx.Result_AIBattle_Batch.t2D描画(TJAPlayer3.app.Device, x, y, new RectangleF(batch_width * 0, 0, batch_width, batch_height));

						switch (section.End)
						{
							case CStage演奏画面共通.AIBattleSection.EndType.Clear:
								TJAPlayer3.Tx.Result_AIBattle_Batch.t2D描画(TJAPlayer3.app.Device, x, y, new Rectangle(batch_width * 1, 0, batch_width, batch_height));
								break;
							case CStage演奏画面共通.AIBattleSection.EndType.Lose:
								TJAPlayer3.Tx.Result_AIBattle_Batch.t2D描画(TJAPlayer3.app.Device, x, y, new Rectangle(batch_width * 2, 0, batch_width, batch_height));
								break;
						}

						TJAPlayer3.Tx.Result_AIBattle_Batch.Opacity = 255;

						TJAPlayer3.Tx.Result_AIBattle_SectionPlate.t2D描画(TJAPlayer3.app.Device, x + TJAPlayer3.Skin.Result_AIBattle_SectionPlate_Offset[0], y + TJAPlayer3.Skin.Result_AIBattle_SectionPlate_Offset[1]);

						TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(ttkAISection[i])?.t2D中心基準描画(TJAPlayer3.app.Device, x + TJAPlayer3.Skin.Result_AIBattle_SectionText_Offset[0], y + TJAPlayer3.Skin.Result_AIBattle_SectionText_Offset[1]);
					}

					if (ct全体進行.n現在の値 >= MountainAppearValue)
					{
						float flagScale = 2.0f - (Math.Min(Math.Max(ct全体進行.n現在の値 - MountainAppearValue, 0), 200) / 200.0f);

						CTexture tex = TJAPlayer3.stage結果.bClear[0] ? TJAPlayer3.Tx.Result_AIBattle_WinFlag_Clear : TJAPlayer3.Tx.Result_AIBattle_WinFlag_Lose;

						tex.vc拡大縮小倍率.X = flagScale;
						tex.vc拡大縮小倍率.Y = flagScale;

						tex.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_AIBattle_WinFlag[0], TJAPlayer3.Skin.Result_AIBattle_WinFlag[1]);
					}
				}


				// Should be Score + 4000, to synchronize with Stage Kekka

				// MountainAppearValue = 2000 + (ctゲージアニメ.n終了値 * 66) + 8360 - 85;



				#region [Character related animations]

				for (int p = 0; p < TJAPlayer3.ConfigIni.nPlayerCount; p++)
				{
					if (TJAPlayer3.ConfigIni.bAIBattleMode && p == 1) break;

					int pos = p;
					if (is2PSide)
						pos = 1;

					if (ct全体進行.n現在の値 >= MountainAppearValue)
					{
						#region [Mountain animation counter setup]

						if (!this.ctMountain_ClearIn.b進行中)
							this.ctMountain_ClearIn.t開始(0, 515, 3, TJAPlayer3.Timer);

						if (TJAPlayer3.stage結果.bClear[p])
						{
							if (!CResultCharacter.tIsCounterProcessing(p, CResultCharacter.ECharacterResult.CLEAR))
								CResultCharacter.tMenuResetTimer(p, CResultCharacter.ECharacterResult.CLEAR);
						}
						else
						{
							if (!CResultCharacter.tIsCounterProcessing(p, CResultCharacter.ECharacterResult.FAILED_IN))
								CResultCharacter.tMenuResetTimer(p, CResultCharacter.ECharacterResult.FAILED_IN);
							else if (CResultCharacter.tIsCounterEnded(p, CResultCharacter.ECharacterResult.FAILED_IN)
								&& !CResultCharacter.tIsCounterProcessing(p, CResultCharacter.ECharacterResult.FAILED))
								CResultCharacter.tMenuResetTimer(p, CResultCharacter.ECharacterResult.FAILED);
						}


						#endregion

						/* TO DO */

						// Alter Mountain appear value/Crown appear value if no Score Rank/no Crown
					}

					#region [Character Animations]

					int _charaId = TJAPlayer3.NamePlateConfig.data.Character[TJAPlayer3.GetActualPlayer(p)];

					//int chara_x = TJAPlayer3.Skin.Characters_Result_X[_charaId][pos];
					//int chara_y = TJAPlayer3.Skin.Characters_Result_Y[_charaId][pos];

					int chara_x = namePlate_x[pos] + TJAPlayer3.Tx.NamePlateBase.szテクスチャサイズ.Width / 2;
					int chara_y = namePlate_y[pos];


                    if (CResultCharacter.tIsCounterProcessing(p, CResultCharacter.ECharacterResult.CLEAR))
						CResultCharacter.tMenuDisplayCharacter(p, chara_x, chara_y, CResultCharacter.ECharacterResult.CLEAR, pos);
					else if (CResultCharacter.tIsCounterProcessing(p, CResultCharacter.ECharacterResult.FAILED))
						CResultCharacter.tMenuDisplayCharacter(p, chara_x, chara_y, CResultCharacter.ECharacterResult.FAILED, pos);
					else if (CResultCharacter.tIsCounterProcessing(p, CResultCharacter.ECharacterResult.FAILED_IN))
						CResultCharacter.tMenuDisplayCharacter(p, chara_x, chara_y, CResultCharacter.ECharacterResult.FAILED_IN, pos);
					else
						CResultCharacter.tMenuDisplayCharacter(p, chara_x, chara_y, CResultCharacter.ECharacterResult.NORMAL, pos);

					#endregion


					#region [PuchiChara]

					int puchi_x = chara_x + TJAPlayer3.Skin.Adjustments_MenuPuchichara_X[TJAPlayer3.ConfigIni.nPlayerCount <= 2 ? pos : 0];
                    int puchi_y = chara_y + TJAPlayer3.Skin.Adjustments_MenuPuchichara_Y[TJAPlayer3.ConfigIni.nPlayerCount <= 2 ? pos : 0];

                    //int ttdiff = 640 - 152;
					//int ttps = 640 + ((pos == 1) ? ttdiff + 60 : -ttdiff);

					//this.PuchiChara.On進行描画(ttps, 562, false, 255, false, p);

                    this.PuchiChara.On進行描画(puchi_x, puchi_y, false, 255, false, p);

                    #endregion

                    if (ct全体進行.n現在の値 >= MountainAppearValue)
					{
						float AddCount = 135;

						int baseX = (pos == 1) ? 1280 - 182 : 182;
						int baseY = 602;

						#region [Cherry blossom animation]

						if (gaugeValues[p] >= 80.0f && TJAPlayer3.ConfigIni.nPlayerCount <= 2)
						{
							TJAPlayer3.Tx.Result_Flower.vc拡大縮小倍率.X = 0.6f * (ct全体進行.n現在の値 <= MountainAppearValue + AddCount ? 1.3f - (float)Math.Sin((ct全体進行.n現在の値 - MountainAppearValue) / (AddCount / 90) * (Math.PI / 180)) * 0.3f : 1.0f);
							TJAPlayer3.Tx.Result_Flower.vc拡大縮小倍率.Y = 0.6f * (ct全体進行.n現在の値 <= MountainAppearValue + AddCount ? 1.3f - (float)Math.Sin((ct全体進行.n現在の値 - MountainAppearValue) / (AddCount / 90) * (Math.PI / 180)) * 0.3f : 1.0f);

							int flower_width = TJAPlayer3.Tx.Result_Flower.szテクスチャサイズ.Width;
							int flower_height = TJAPlayer3.Tx.Result_Flower.szテクスチャサイズ.Height / 2;

							TJAPlayer3.Tx.Result_Flower.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_Flower_X[pos], TJAPlayer3.Skin.Result_Flower_Y[pos], 
								new Rectangle(0, 0, flower_width, flower_height));
						}

						#endregion

						#region [Cherry blossom Rotating flowers]

						if (gaugeValues[p] >= 80.0f && TJAPlayer3.ConfigIni.nPlayerCount <= 2)
						{
							float FlowerTime = ctRotate_Flowers.n現在の値;

							for (int i = 0; i < 5; i++)
							{

								if ((int)FlowerTime < ApparitionTimeStamps[i] || (int)FlowerTime > ApparitionTimeStamps[i] + 2 * ApparitionFade + ApparitionDuration)
									TJAPlayer3.Tx.Result_Flower_Rotate[i].Opacity = 0;
								else if ((int)FlowerTime <= ApparitionTimeStamps[i] + ApparitionDuration + ApparitionFade && (int)FlowerTime >= ApparitionTimeStamps[i] + ApparitionFade)
									TJAPlayer3.Tx.Result_Flower_Rotate[i].Opacity = 255;
								else
								{
									int CurrentGradiant = 0;
									if ((int)FlowerTime >= ApparitionTimeStamps[i] + ApparitionFade + ApparitionDuration)
										CurrentGradiant = ApparitionFade - ((int)FlowerTime - ApparitionTimeStamps[i] - ApparitionDuration - ApparitionFade);
									else
										CurrentGradiant = (int)FlowerTime - ApparitionTimeStamps[i];


									TJAPlayer3.Tx.Result_Flower_Rotate[i].Opacity = (255 * CurrentGradiant) / ApparitionFade;
								}

								TJAPlayer3.Tx.Result_Flower_Rotate[i].vc拡大縮小倍率.X = 0.6f;
								TJAPlayer3.Tx.Result_Flower_Rotate[i].vc拡大縮小倍率.Y = 0.6f;
								TJAPlayer3.Tx.Result_Flower_Rotate[i].fZ軸中心回転 = (float)(FlowerTime - ApparitionTimeStamps[i]) / (FlowerRotationSpeeds[i] * 360f);

								TJAPlayer3.Tx.Result_Flower_Rotate[i].t2D中心基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_Flower_Rotate_X[pos][i], TJAPlayer3.Skin.Result_Flower_Rotate_Y[pos][i]);
							}

						}

						#endregion

						#region [Panel shines]

						if (gaugeValues[p] >= 80.0f && TJAPlayer3.ConfigIni.nPlayerCount <= 2)
						{
							int ShineTime = (int)ctShine_Plate.n現在の値;
							int Quadrant500 = ShineTime % 500;

							for (int i = 0; i < TJAPlayer3.Skin.Result_PlateShine_Count; i++)
							{
								if (i < 3 && ShineTime >= 500 || i >= 3 && ShineTime < 500)
									TJAPlayer3.Tx.Result_Shine.Opacity = 0;
								else if (Quadrant500 >= ShinePFade && Quadrant500 <= 500 - ShinePFade)
									TJAPlayer3.Tx.Result_Shine.Opacity = 255;
								else
									TJAPlayer3.Tx.Result_Shine.Opacity = (255 * Math.Min(Quadrant500, 500 - Quadrant500)) / ShinePFade;

								TJAPlayer3.Tx.Result_Shine.vc拡大縮小倍率.X = 0.15f;
								TJAPlayer3.Tx.Result_Shine.vc拡大縮小倍率.Y = 0.15f;

								TJAPlayer3.Tx.Result_Shine.t2D中心基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_PlateShine_X[pos][i], TJAPlayer3.Skin.Result_PlateShine_Y[pos][i]);
							}

						}


						#endregion

						#region [Speech bubble animation]
						// Speech Bubble

						int Mood = 0;

						if (gaugeValues[p] >= 100.0f)
							Mood = 3;
						else if (gaugeValues[p] >= 80.0f)
							Mood = 2;
						else if (gaugeValues[p] >= 40.0f)
							Mood = 1;

						if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
						{
							int speechBuddle_width = TJAPlayer3.Tx.Result_Speech_Bubble[pos].szテクスチャサイズ.Width / 4;
							int speechBuddle_height = TJAPlayer3.Tx.Result_Speech_Bubble[pos].szテクスチャサイズ.Height / 3;

							TJAPlayer3.Tx.Result_Speech_Bubble[pos].vc拡大縮小倍率.X = 0.9f * (ct全体進行.n現在の値 <= MountainAppearValue + AddCount ? 1.3f - (float)Math.Sin((ct全体進行.n現在の値 - MountainAppearValue) / (AddCount / 90) * (Math.PI / 180)) * 0.3f : 1.0f);
							TJAPlayer3.Tx.Result_Speech_Bubble[pos].vc拡大縮小倍率.Y = 0.9f * (ct全体進行.n現在の値 <= MountainAppearValue + AddCount ? 1.3f - (float)Math.Sin((ct全体進行.n現在の値 - MountainAppearValue) / (AddCount / 90) * (Math.PI / 180)) * 0.3f : 1.0f);
							TJAPlayer3.Tx.Result_Speech_Bubble[pos].t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Result_Speech_Bubble_X[pos], TJAPlayer3.Skin.Result_Speech_Bubble_Y[pos],
								new Rectangle(Mood * speechBuddle_width, RandomText * speechBuddle_height, speechBuddle_width, speechBuddle_height));
						}
						if (!b音声再生[9])
						{
							if (gaugeValues[p] >= 80.0f)
                            {
								//TJAPlayer3.Skin.soundDonClear.t再生する();
								TJAPlayer3.Skin.voiceResultClearSuccess[TJAPlayer3.GetActualPlayer(p)]?.t再生する();
							}
							else
                            {
								//TJAPlayer3.Skin.soundDonFailed.t再生する();
								TJAPlayer3.Skin.voiceResultClearFailed[TJAPlayer3.GetActualPlayer(p)]?.t再生する();
							}
								
							if (p == TJAPlayer3.ConfigIni.nPlayerCount - 1)
								b音声再生[9] = true;
						}

						#endregion
					}






					if (ct全体進行.n現在の値 >= ScoreApparitionTimeStamp + 1000)
					{
						//if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
						{
							#region [Score rank apparition]

							int scoreRank_width = TJAPlayer3.Tx.Result_ScoreRankEffect.szテクスチャサイズ.Width / 7;
							int scoreRank_height = TJAPlayer3.Tx.Result_ScoreRankEffect.szテクスチャサイズ.Height / 4;

							if (ct全体進行.n現在の値 <= ScoreApparitionTimeStamp + 1180)
							{
								TJAPlayer3.Tx.Result_ScoreRankEffect.Opacity = (int)((ct全体進行.n現在の値 - (ScoreApparitionTimeStamp + 1000)) / 180.0f * 255.0f);
								TJAPlayer3.Tx.Result_ScoreRankEffect.vc拡大縮小倍率.X = 1.0f + (float)Math.Sin((float)(ct全体進行.n現在の値 - (ScoreApparitionTimeStamp + 910)) / 1.5f * (Math.PI / 180)) * 1.4f;
								TJAPlayer3.Tx.Result_ScoreRankEffect.vc拡大縮小倍率.Y = 1.0f + (float)Math.Sin((float)(ct全体進行.n現在の値 - (ScoreApparitionTimeStamp + 910)) / 1.5f * (Math.PI / 180)) * 1.4f;
							}
							else if (ct全体進行.n現在の値 <= ScoreApparitionTimeStamp + 1270)
							{
								TJAPlayer3.Tx.Result_ScoreRankEffect.vc拡大縮小倍率.X = 0.5f + (float)Math.Sin((float)(ct全体進行.n現在の値 - (ScoreApparitionTimeStamp + 1180)) * (Math.PI / 180)) * 0.5f;
								TJAPlayer3.Tx.Result_ScoreRankEffect.vc拡大縮小倍率.Y = 0.5f + (float)Math.Sin((float)(ct全体進行.n現在の値 - (ScoreApparitionTimeStamp + 1180)) * (Math.PI / 180)) * 0.5f;
							}
							else
							{
								TJAPlayer3.Tx.Result_ScoreRankEffect.Opacity = 255;
								TJAPlayer3.Tx.Result_ScoreRankEffect.vc拡大縮小倍率.X = 1f;
								TJAPlayer3.Tx.Result_ScoreRankEffect.vc拡大縮小倍率.Y = 1f;
							}

							if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan && TJAPlayer3.stage結果.nスコアランク[p] > 0)
							{
								int CurrentFlash = 0;
								int[] FlashTimes = { 1500, 1540, 1580, 1620, 1660, 1700, 1740, 1780 };

								if (ctFlash_Icon.n現在の値 >= FlashTimes[0] && ctFlash_Icon.n現在の値 <= FlashTimes[1] || ctFlash_Icon.n現在の値 >= FlashTimes[4] && ctFlash_Icon.n現在の値 <= FlashTimes[5])
									CurrentFlash = 1;
								else if (ctFlash_Icon.n現在の値 >= FlashTimes[1] && ctFlash_Icon.n現在の値 <= FlashTimes[2] || ctFlash_Icon.n現在の値 >= FlashTimes[5] && ctFlash_Icon.n現在の値 <= FlashTimes[6])
									CurrentFlash = 2;
								else if (ctFlash_Icon.n現在の値 >= FlashTimes[2] && ctFlash_Icon.n現在の値 <= FlashTimes[3] || ctFlash_Icon.n現在の値 >= FlashTimes[6] && ctFlash_Icon.n現在の値 <= FlashTimes[7])
									CurrentFlash = 3;


								int scoreRankEffect_x;
								int scoreRankEffect_y;
								if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
								{
									scoreRankEffect_x = TJAPlayer3.Skin.Result_ScoreRankEffect_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
									scoreRankEffect_y = TJAPlayer3.Skin.Result_ScoreRankEffect_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);
								}
								else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
								{
									scoreRankEffect_x = TJAPlayer3.Skin.Result_ScoreRankEffect_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
									scoreRankEffect_y = TJAPlayer3.Skin.Result_ScoreRankEffect_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);
								}
								else
								{
									scoreRankEffect_x = TJAPlayer3.Skin.Result_ScoreRankEffect_X[pos];
									scoreRankEffect_y = TJAPlayer3.Skin.Result_ScoreRankEffect_Y[pos];
								}

								TJAPlayer3.Tx.Result_ScoreRankEffect.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, scoreRankEffect_x, scoreRankEffect_y,
									new Rectangle((TJAPlayer3.stage結果.nスコアランク[p] - 1) * scoreRank_width, CurrentFlash * scoreRank_height, scoreRank_width, scoreRank_height));

								if (!b音声再生[7] && ct全体進行.n現在の値 >= ScoreApparitionTimeStamp + 1180)
								{
									TJAPlayer3.Skin.soundRankIn.t再生する();
									b音声再生[7] = true;
								}
							}

							#endregion
						}
					}


					if (ct全体進行.n現在の値 >= ScoreApparitionTimeStamp + 2500)
					{
						//if (TJAPlayer3.ConfigIni.nPlayerCount <= 2)
						{
							#region [Crown apparition]

							int crownEffect_width = TJAPlayer3.Tx.Result_CrownEffect.szテクスチャサイズ.Width / 3;
							int crownEffect_height = TJAPlayer3.Tx.Result_CrownEffect.szテクスチャサイズ.Height / 4;

							if (ct全体進行.n現在の値 <= ScoreApparitionTimeStamp + 2680)
							{
								TJAPlayer3.Tx.Result_CrownEffect.Opacity = (int)((ct全体進行.n現在の値 - (ScoreApparitionTimeStamp + 2500)) / 180.0f * 255.0f);
								TJAPlayer3.Tx.Result_CrownEffect.vc拡大縮小倍率.X = 1.0f + (float)Math.Sin((float)(ct全体進行.n現在の値 - (ScoreApparitionTimeStamp + 2410)) / 1.5f * (Math.PI / 180)) * 1.4f;
								TJAPlayer3.Tx.Result_CrownEffect.vc拡大縮小倍率.Y = 1.0f + (float)Math.Sin((float)(ct全体進行.n現在の値 - (ScoreApparitionTimeStamp + 2410)) / 1.5f * (Math.PI / 180)) * 1.4f;
							}
							else if (ct全体進行.n現在の値 <= ScoreApparitionTimeStamp + 2770)
							{
								TJAPlayer3.Tx.Result_CrownEffect.vc拡大縮小倍率.X = 0.5f + (float)Math.Sin((float)(ct全体進行.n現在の値 - (ScoreApparitionTimeStamp + 2680)) * (Math.PI / 180)) * 0.5f;
								TJAPlayer3.Tx.Result_CrownEffect.vc拡大縮小倍率.Y = 0.5f + (float)Math.Sin((float)(ct全体進行.n現在の値 - (ScoreApparitionTimeStamp + 2680)) * (Math.PI / 180)) * 0.5f;
							}
							else
							{
								TJAPlayer3.Tx.Result_CrownEffect.Opacity = 255;
								TJAPlayer3.Tx.Result_CrownEffect.vc拡大縮小倍率.X = 1f;
								TJAPlayer3.Tx.Result_CrownEffect.vc拡大縮小倍率.Y = 1f;
							}

							int ClearType = TJAPlayer3.stage結果.nクリア[p] - 1;

							if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)(Difficulty.Dan) && ClearType >= 0)
							{
								int CurrentFlash = 0;
								int[] FlashTimes = { 2000, 2040, 2080, 2120, 2160, 2200, 2240, 2280 };

								if (ctFlash_Icon.n現在の値 >= FlashTimes[0] && ctFlash_Icon.n現在の値 <= FlashTimes[1] || ctFlash_Icon.n現在の値 >= FlashTimes[4] && ctFlash_Icon.n現在の値 <= FlashTimes[5])
									CurrentFlash = 1;
								else if (ctFlash_Icon.n現在の値 >= FlashTimes[1] && ctFlash_Icon.n現在の値 <= FlashTimes[2] || ctFlash_Icon.n現在の値 >= FlashTimes[5] && ctFlash_Icon.n現在の値 <= FlashTimes[6])
									CurrentFlash = 2;
								else if (ctFlash_Icon.n現在の値 >= FlashTimes[2] && ctFlash_Icon.n現在の値 <= FlashTimes[3] || ctFlash_Icon.n現在の値 >= FlashTimes[6] && ctFlash_Icon.n現在の値 <= FlashTimes[7])
									CurrentFlash = 3;


								int crownEffect_x;
								int crownEffect_y;
								if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
								{
									crownEffect_x = TJAPlayer3.Skin.Result_CrownEffect_5P[0] + (TJAPlayer3.Skin.Result_UIMove_5P[0] * pos);
									crownEffect_y = TJAPlayer3.Skin.Result_CrownEffect_5P[1] + (TJAPlayer3.Skin.Result_UIMove_5P[1] * pos);
								}
								else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
								{
									crownEffect_x = TJAPlayer3.Skin.Result_CrownEffect_4P[0] + (TJAPlayer3.Skin.Result_UIMove_4P[0] * pos);
									crownEffect_y = TJAPlayer3.Skin.Result_CrownEffect_4P[1] + (TJAPlayer3.Skin.Result_UIMove_4P[1] * pos);
								}
								else
								{
									crownEffect_x = TJAPlayer3.Skin.Result_CrownEffect_X[pos];
									crownEffect_y = TJAPlayer3.Skin.Result_CrownEffect_Y[pos];
								}

								TJAPlayer3.Tx.Result_CrownEffect.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, crownEffect_x, crownEffect_y,
									new Rectangle(ClearType * crownEffect_width, CurrentFlash * crownEffect_height, crownEffect_width, crownEffect_height));

								if (!b音声再生[8] && ct全体進行.n現在の値 >= ScoreApparitionTimeStamp + 2680)
								{
									TJAPlayer3.Skin.soundCrownIn.t再生する();
									b音声再生[8] = true;
								}
							}

							#endregion
						}
					}
				}

				#endregion

				

				#endregion
			}

			if (!this.ct表示用.b終了値に達した)
			{
				return 0;
			}
			return 1;
		}



		// その他

		#region [ private ]
		//-----------------
		[StructLayout(LayoutKind.Sequential)]
		private struct ST文字位置
		{
			public char ch;
			public Point pt;
		}

		public CCounter ct全体進行;
		public CCounter[] ctゲージアニメ;
		private CCounter ct虹ゲージアニメ;
		private CCounter ctSoul;

		public CCounter ctEndAnime;
		public CCounter ctMountain_ClearIn;
		public CCounter ctBackgroundAnime;
		public CCounter ctBackgroundAnime_Clear;

		private int RandomText;

		private CCounter ctFlash_Icon;
		private CCounter ctRotate_Flowers;
		private CCounter ctShine_Plate;

		public PuchiChara PuchiChara;

		public float MountainAppearValue;
		private int GaugeFactor;

		public bool[] b音声再生 = { false, false, false, false, false, false, false, false, false, false, false, false, false };

		// Cherry blossom flowers variables
		/*
		public int[] FlowerXPos = { -114, -37, 114, 78, -75 };
		public int[] FlowerYPos = { -33, 3, -36, -81, -73 };
		*/
		public float[] FlowerRotationSpeeds = { 5f, 3f, -6f, 4f, -2f };
		public int[] ApparitionTimeStamps = { 10, 30, 50, 100, 190 };
		public int ApparitionFade = 100;
		public int ApparitionDuration = 300;

		// Plate shine variables 
		public int[] ShinePXPos = { 114 - 25, 114 - 16, -37 - 23, -37 - 9, -75 + 20, 78 - 13};
		public int[] ShinePYPos = { -36 + 52, -36 + 2, 3 - 7, 3 + 30, -73 - 23, -81 - 31};
		public int ShinePFade = 100;

		public int[] gaugeValues;

		private CCounter ct表示用;
		private readonly Point[] ptFullCombo位置;
		private CSound sdDTXで指定されたフルコンボ音;
		private readonly ST文字位置[] st小文字位置;
		private readonly ST文字位置[] st大文字位置;
		private ST文字位置[] stScoreFont;

		private CActSelect曲リスト.TitleTextureKey[] ttkAISection;

		private CPrivateFastFont pfAISectionText;

		private CCounter ctAISectionChange;

		private int nNowAISection;

		private void NextAISection()
		{
			ctAISectionChange = new CCounter(0, 2000, 1, TJAPlayer3.Timer);
			ctAISectionChange.n現在の値 = 0;

			nNowAISection++;
			if (nNowAISection >= Math.Ceiling(TJAPlayer3.stage演奏ドラム画面.AIBattleSections.Count / 5.0))
            {
				nNowAISection = 0;

			}
		}

		public void t小文字表示(int x, int y, int num)
		{
			int[] nums = C変換.SeparateDigits(num);
			for (int j = 0; j < nums.Length; j++)
			{
				float offset = j;

				float width = TJAPlayer3.Tx.Result_Number.sz画像サイズ.Width / 11.0f;
				float height = TJAPlayer3.Tx.Result_Number.sz画像サイズ.Height / 2.0f;

				float _x = x - (TJAPlayer3.Skin.Result_Number_Interval[0] * offset) + (width * 2);
				float _y = y - (TJAPlayer3.Skin.Result_Number_Interval[1] * offset);

				TJAPlayer3.Tx.Result_Number.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, _x + (width / 2), _y + (height / 2), 
					new RectangleF(width * nums[j], 0, width, height));
			}
		}
		private void t大文字表示(int x, int y, string str)
		{
			this.t大文字表示(x, y, str, false);
		}
		private void t大文字表示(int x, int y, string str, bool b強調)
		{
			foreach (char ch in str)
			{
				for (int i = 0; i < this.st大文字位置.Length; i++)
				{
					if (this.st大文字位置[i].ch == ch)
					{
						Rectangle rectangle = new Rectangle(this.st大文字位置[i].pt.X, this.st大文字位置[i].pt.Y, 11, 0x10);
						if (ch == '.')
						{
							rectangle.Width -= 2;
							rectangle.Height -= 2;
						}
						if (TJAPlayer3.Tx.Result_Number != null)
						{
							TJAPlayer3.Tx.Result_Number.t2D描画(TJAPlayer3.app.Device, x, y, rectangle);
						}
						break;
					}
				}
				x += 8;
			}
		}

		public void tスコア文字表示(int x, int y, int num)
		{
			int[] nums = C変換.SeparateDigits(num);
			for (int j = 0; j < nums.Length; j++)
			{
				float offset = j;
				float _x = x - (TJAPlayer3.Skin.Result_Score_Number_Interval[0] * offset);
				float _y = y - (TJAPlayer3.Skin.Result_Score_Number_Interval[1] * offset);

				float width = TJAPlayer3.Tx.Result_Score_Number.sz画像サイズ.Width / 10.0f;
				float height = TJAPlayer3.Tx.Result_Score_Number.sz画像サイズ.Height;

				TJAPlayer3.Tx.Result_Score_Number.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, _x, _y + (height / 2), new RectangleF(width * nums[j], 0, width, height));
			}
		}
		//-----------------
		#endregion
	}
}
