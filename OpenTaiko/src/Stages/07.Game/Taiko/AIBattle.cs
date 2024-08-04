using System.Drawing;
using FDK;

// Minimalist menu class to use for custom menus
namespace OpenTaiko {
	class AIBattle : CStage {
		public AIBattle() {
			base.IsDeActivated = true;
		}

		public override void Activate() {
			// On activation

			BarFlashCounter = new CCounter(0, 1000.0, 0.00035, SoundManager.PlayTimer);

			BatchAnimeCounter = new CCounter(0, 10000.0, 1.0 / 1000.0, SoundManager.PlayTimer);

			base.Activate();
		}

		public override void DeActivate() {
			// On de-activation

			base.DeActivate();
		}

		public override void CreateManagedResource() {
			// Ressource allocation

			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {
			// Ressource freeing

			base.ReleaseManagedResource();
		}

		public override int Draw() {
			BarFlashCounter.TickLoopDB();
			BatchAnimeCounter.TickDB();

			OpenTaiko.Tx.AIBattle_SectionTime_Panel?.t2D描画(OpenTaiko.Skin.Game_AIBattle_SectionTime_Panel[0], OpenTaiko.Skin.Game_AIBattle_SectionTime_Panel[1]);

			OpenTaiko.Tx.AIBattle_SectionTime_Bar_Base?.t2D描画(OpenTaiko.Skin.Game_AIBattle_SectionTime_Bar[0], OpenTaiko.Skin.Game_AIBattle_SectionTime_Bar[1],
				new System.Drawing.RectangleF(0, 0, OpenTaiko.Tx.AIBattle_SectionTime_Bar_Base.szTextureSize.Width, OpenTaiko.Tx.AIBattle_SectionTime_Bar_Base.szTextureSize.Height));

			void drawBar(CTexture barTex, float length) {
				barTex?.t2D描画(OpenTaiko.Skin.Game_AIBattle_SectionTime_Bar[0], OpenTaiko.Skin.Game_AIBattle_SectionTime_Bar[1],
					new System.Drawing.RectangleF(0, 0, barTex.szTextureSize.Width * length, barTex.szTextureSize.Height));
			}

			var nowSection = OpenTaiko.stage演奏ドラム画面.NowAIBattleSection;

			float nowLength = OpenTaiko.stage演奏ドラム画面.NowAIBattleSectionTime / (float)nowSection.Length;
			nowLength = Math.Min(nowLength, 1.0f);

			if (nowLength < 0.75) {
				drawBar(OpenTaiko.Tx.AIBattle_SectionTime_Bar_Normal, nowLength);
			} else {
				OpenTaiko.Tx.AIBattle_SectionTime_Bar_Finish.Opacity = (int)(Math.Sin((BarFlashCounter.CurrentValue / 1000.0) * Math.PI) * 255);
				drawBar(OpenTaiko.Tx.AIBattle_SectionTime_Bar_Finish, nowLength);
			}

			for (int i = 0; i < OpenTaiko.stage演奏ドラム画面.AIBattleSections.Count; i++) {
				int upDown = (i % 2);

				int base_width = OpenTaiko.Tx.AIBattle_Batch_Base.szTextureSize.Width / 6;
				int base_height = OpenTaiko.Tx.AIBattle_Batch_Base.szTextureSize.Height;

				int base_x = OpenTaiko.Skin.Game_AIBattle_Batch_Base[0] + (OpenTaiko.Skin.Game_AIBattle_Batch_Move[0] * i);
				int base_y = OpenTaiko.Skin.Game_AIBattle_Batch_Base[1] + (OpenTaiko.Skin.Game_AIBattle_Batch_Move[1] * upDown);

				int nowBatchBaseRectX;

				if (i == 0) {
					nowBatchBaseRectX = 2 + (upDown == 0 ? 0 : 1);
				} else if (i == OpenTaiko.stage演奏ドラム画面.AIBattleSections.Count - 1) {
					nowBatchBaseRectX = 4 + (upDown == 0 ? 0 : 1);
				} else {
					nowBatchBaseRectX = (upDown == 0 ? 0 : 1);
				}

				OpenTaiko.Tx.AIBattle_Batch_Base?.t2D描画(base_x, base_y, new System.Drawing.RectangleF(base_width * nowBatchBaseRectX, 0, base_width, base_height));
			}

			for (int i = 0; i < OpenTaiko.stage演奏ドラム画面.NowAIBattleSectionCount; i++) {

				var section = OpenTaiko.stage演奏ドラム画面.AIBattleSections[i];

				int upDown = (i % 2);

				int width = OpenTaiko.Tx.AIBattle_Batch.szTextureSize.Width / 6;
				int height = OpenTaiko.Tx.AIBattle_Batch.szTextureSize.Height / 2;

				float value = 0.0f;

				float inScale = 0.0f;

				int drawFrame = 5;

				if (section.IsAnimated) {
					value = 1.0f;
				} else {
					if (BatchAnimeCounter.CurrentValue < 100) {
						inScale = 1.0f - (BatchAnimeCounter.CurrentValue / 100.0f);
					} else if (BatchAnimeCounter.CurrentValue >= 700 && BatchAnimeCounter.CurrentValue < 1000) {
						drawFrame = (int)(((BatchAnimeCounter.CurrentValue - 700) / 300.0) * 4.0);
					} else if (BatchAnimeCounter.CurrentValue >= 1400 && BatchAnimeCounter.CurrentValue <= 1500) {
						value = Math.Min((BatchAnimeCounter.CurrentValue - 1400) / 100.0f, 1.0f);
					} else if (BatchAnimeCounter.CurrentValue >= 1500) {
						value = 1.0f;
						section.IsAnimated = true;
					}
				}

				float _x = OpenTaiko.Skin.Game_AIBattle_Batch[0] + (OpenTaiko.Skin.Game_AIBattle_Batch_Move[0] * i);
				float _y = OpenTaiko.Skin.Game_AIBattle_Batch[1] + (OpenTaiko.Skin.Game_AIBattle_Batch_Move[1] * upDown);

				_x = OpenTaiko.Skin.Game_AIBattle_Batch_Anime[0] + ((_x - OpenTaiko.Skin.Game_AIBattle_Batch_Anime[0]) * value);
				_y = OpenTaiko.Skin.Game_AIBattle_Batch_Anime[1] + ((_y - OpenTaiko.Skin.Game_AIBattle_Batch_Anime[1]) * value);


				float size_x = OpenTaiko.Skin.Game_AIBattle_Batch_Anime_Size[0] +
					((OpenTaiko.Skin.Game_AIBattle_Batch_Size[0] - OpenTaiko.Skin.Game_AIBattle_Batch_Anime_Size[0]) * value);

				float size_y = OpenTaiko.Skin.Game_AIBattle_Batch_Anime_Size[1] +
					((OpenTaiko.Skin.Game_AIBattle_Batch_Size[1] - OpenTaiko.Skin.Game_AIBattle_Batch_Anime_Size[1]) * value);

				OpenTaiko.Tx.AIBattle_Batch.vcScaleRatio.X = (size_x / (float)width) + inScale;
				OpenTaiko.Tx.AIBattle_Batch.vcScaleRatio.Y = (size_y / (float)height) + inScale;

				switch (section.End) {
					case CStage演奏画面共通.AIBattleSection.EndType.Clear:
						OpenTaiko.Tx.AIBattle_Batch?.t2D拡大率考慮中央基準描画(_x, _y, new System.Drawing.RectangleF(width * drawFrame, 0, width, height));
						break;
					case CStage演奏画面共通.AIBattleSection.EndType.Lose:
						OpenTaiko.Tx.AIBattle_Batch?.t2D拡大率考慮中央基準描画(_x, _y, new System.Drawing.RectangleF(width * drawFrame, height, width, height));
						break;
				}
			}

			for (int player = 0; player < 2; player++) {
				OpenTaiko.Tx.AIBattle_Judge_Meter[player]?.t2D描画(OpenTaiko.Skin.Game_AIBattle_Judge_Meter_X[player], OpenTaiko.Skin.Game_AIBattle_Judge_Meter_Y[player]);


				int[] numArr = new int[4]
				{
					OpenTaiko.stage演奏ドラム画面.CSectionScore[player].nGreat,
					OpenTaiko.stage演奏ドラム画面.CSectionScore[player].nGood,
					OpenTaiko.stage演奏ドラム画面.CSectionScore[player].nMiss,
					OpenTaiko.stage演奏ドラム画面.CSectionScore[player].nRoll
				};

				int[] num_x = new int[4]
				{
					OpenTaiko.Skin.Game_AIBattle_Judge_Number_Perfect_X[player],
					OpenTaiko.Skin.Game_AIBattle_Judge_Number_Good_X[player],
					OpenTaiko.Skin.Game_AIBattle_Judge_Number_Miss_X[player],
					OpenTaiko.Skin.Game_AIBattle_Judge_Number_Roll_X[player]
				};

				int[] num_y = new int[4]
				{
					OpenTaiko.Skin.Game_AIBattle_Judge_Number_Perfect_Y[player],
					OpenTaiko.Skin.Game_AIBattle_Judge_Number_Good_Y[player],
					OpenTaiko.Skin.Game_AIBattle_Judge_Number_Miss_Y[player],
					OpenTaiko.Skin.Game_AIBattle_Judge_Number_Roll_Y[player]
				};

				for (int i = 0; i < 4; i++) {
					DrawJudgeNumber(num_x[i], num_y[i], numArr[i]);
				}
			}


			return 0;
		}

		#region [Private]

		private CCounter BarFlashCounter;
		public CCounter BatchAnimeCounter;

		private void DrawJudgeNumber(int x, int y, int num) {
			int[] nums = CConversion.SeparateDigits(num);
			for (int j = 0; j < nums.Length; j++) {
				float offset = j - (nums.Length / 2.0f);

				float width = OpenTaiko.Tx.AIBattle_Judge_Number.sz画像サイズ.Width / 10.0f;
				float height = OpenTaiko.Tx.AIBattle_Judge_Number.sz画像サイズ.Height;

				float _x = x - (OpenTaiko.Skin.Game_AIBattle_Judge_Number_Interval[0] * offset);
				float _y = y - (OpenTaiko.Skin.Game_AIBattle_Judge_Number_Interval[1] * offset);

				OpenTaiko.Tx.AIBattle_Judge_Number.t2D拡大率考慮中央基準描画(_x + (width / 2), _y + (height / 2),
					new RectangleF(width * nums[j], 0, width, height));
			}
		}

		#endregion
	}
}
