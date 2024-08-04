using System.Drawing;
using FDK;

namespace OpenTaiko {
	internal class CActImplJudgeText : CActivity {
		// コンストラクタ

		public CActImplJudgeText() {
			base.IsDeActivated = true;
		}

		public override void Activate() {
			JudgeAnimes = new List<JudgeAnime>[5];
			for (int i = 0; i < 5; i++) {
				JudgeAnimes[i] = new List<JudgeAnime>();
			}
			base.Activate();
		}

		public override void DeActivate() {
			for (int i = 0; i < 5; i++) {
				for (int j = 0; j < JudgeAnimes[i].Count; j++) {
					JudgeAnimes[i][j] = null;
				}
			}
			base.DeActivate();
		}

		// CActivity 実装（共通クラスからの差分のみ）
		public override int Draw() {
			if (!base.IsDeActivated) {
				for (int j = 0; j < 5; j++) {
					for (int i = 0; i < JudgeAnimes[j].Count; i++) {
						var judgeC = JudgeAnimes[j][i];
						if (judgeC.counter.CurrentValue == judgeC.counter.EndValue) {
							JudgeAnimes[j].Remove(judgeC);
							continue;
						}
						judgeC.counter.Tick();

						if (OpenTaiko.Tx.Judge != null) {
							float moveValue = CubicEaseOut(judgeC.counter.CurrentValue / 410.0f) - 1.0f;

							float x = 0;
							float y = 0;

							if (OpenTaiko.ConfigIni.nPlayerCount == 5) {
								x = OpenTaiko.Skin.Game_Judge_5P[0] + (OpenTaiko.Skin.Game_UIMove_5P[0] * j);
								y = OpenTaiko.Skin.Game_Judge_5P[1] + (OpenTaiko.Skin.Game_UIMove_5P[1] * j);
							} else if (OpenTaiko.ConfigIni.nPlayerCount == 4 || OpenTaiko.ConfigIni.nPlayerCount == 3) {
								x = OpenTaiko.Skin.Game_Judge_4P[0] + (OpenTaiko.Skin.Game_UIMove_4P[0] * j);
								y = OpenTaiko.Skin.Game_Judge_4P[1] + (OpenTaiko.Skin.Game_UIMove_4P[1] * j);
							} else {
								x = OpenTaiko.Skin.Game_Judge_X[j];
								y = OpenTaiko.Skin.Game_Judge_Y[j];
							}
							x += (moveValue * OpenTaiko.Skin.Game_Judge_Move[0]) + OpenTaiko.stage演奏ドラム画面.GetJPOSCROLLX(j);
							y += (moveValue * OpenTaiko.Skin.Game_Judge_Move[1]) + OpenTaiko.stage演奏ドラム画面.GetJPOSCROLLY(j);

							OpenTaiko.Tx.Judge.Opacity = (int)(255f - (judgeC.counter.CurrentValue >= 360 ? ((judgeC.counter.CurrentValue - 360) / 50.0f) * 255f : 0f));
							OpenTaiko.Tx.Judge.t2D描画(x, y, judgeC.rc);
						}
					}
				}
			}
			return 0;
		}

		public void Start(int player, ENoteJudge judge) {
			JudgeAnime judgeAnime = new();
			judgeAnime.counter.Start(0, 410, 1, OpenTaiko.Timer);
			judgeAnime.Judge = judge;

			//int njudge = judge == E判定.Perfect ? 0 : judge == E判定.Good ? 1 : judge == E判定.ADLIB ? 3 : judge == E判定.Auto ? 0 : 2;

			int njudge = 2;
			if (JudgesDict.ContainsKey(judge)) {
				njudge = JudgesDict[judge];
			}

			if (njudge == 0 && OpenTaiko.ConfigIni.SimpleMode) {
				return;
			}

			int height = OpenTaiko.Tx.Judge.szTextureSize.Height / 5;
			judgeAnime.rc = new Rectangle(0, (int)njudge * height, OpenTaiko.Tx.Judge.szTextureSize.Width, height);

			JudgeAnimes[player].Add(judgeAnime);
		}

		// その他

		#region [ private ]
		//-----------------

		private static Dictionary<ENoteJudge, int> JudgesDict = new Dictionary<ENoteJudge, int> {
			[ENoteJudge.Perfect] = 0,
			[ENoteJudge.Auto] = 0,
			[ENoteJudge.Good] = 1,
			[ENoteJudge.Bad] = 2,
			[ENoteJudge.Miss] = 2,
			[ENoteJudge.ADLIB] = 3,
			[ENoteJudge.Mine] = 4,
		};

		private List<JudgeAnime>[] JudgeAnimes = new List<JudgeAnime>[5];
		private class JudgeAnime {
			public ENoteJudge Judge;
			public Rectangle rc;
			public CCounter counter = new CCounter();
		}

		private float CubicEaseOut(float p) {
			float f = (p - 1);
			return f * f * f + 1;
		}
		//-----------------
		#endregion
	}
}
