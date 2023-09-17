﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;
using FDK;
using System.Drawing;

namespace TJAPlayer3
{
	internal class CAct演奏Drums判定文字列 : CActivity
	{
		// コンストラクタ

		public CAct演奏Drums判定文字列()
		{
			base.IsDeActivated = true;
		}

        public override void Activate()
        {
			JudgeAnimes = new List<JudgeAnime>[5];
			for (int i = 0; i < 5; i++)
			{
				JudgeAnimes[i] = new List<JudgeAnime>();
			}
            base.Activate();
        }

        public override void DeActivate()
        {
			for (int i = 0; i < 5; i++)
            {
				for (int j = 0; j < JudgeAnimes[i].Count; j++)
				{
					JudgeAnimes[i][j] = null;
				}
			}
            base.DeActivate();
        }

        // CActivity 実装（共通クラスからの差分のみ）
        public override int Draw()
		{
			if (!base.IsDeActivated)
			{
				for(int j = 0; j < 5; j++)
				{
					for (int i = 0; i < JudgeAnimes[j].Count; i++)
					{
						var judgeC = JudgeAnimes[j][i];
						if (judgeC.counter.CurrentValue == judgeC.counter.EndValue)
						{
							JudgeAnimes[j].Remove(judgeC);
							continue;
						}
						judgeC.counter.Tick();

						if (TJAPlayer3.Tx.Judge != null)
						{
							float moveValue = CubicEaseOut(judgeC.counter.CurrentValue / 410.0f) - 1.0f;

							float x = 0;
							float y = 0;

							if (TJAPlayer3.ConfigIni.nPlayerCount == 5)
							{
								x = TJAPlayer3.Skin.Game_Judge_5P[0] + (TJAPlayer3.Skin.Game_UIMove_5P[0] * j);
								y = TJAPlayer3.Skin.Game_Judge_5P[1] + (TJAPlayer3.Skin.Game_UIMove_5P[1] * j);
							}
							else if (TJAPlayer3.ConfigIni.nPlayerCount == 4 || TJAPlayer3.ConfigIni.nPlayerCount == 3)
							{
								x = TJAPlayer3.Skin.Game_Judge_4P[0] + (TJAPlayer3.Skin.Game_UIMove_4P[0] * j);
								y = TJAPlayer3.Skin.Game_Judge_4P[1] + (TJAPlayer3.Skin.Game_UIMove_4P[1] * j);
							}
							else
							{
								x = TJAPlayer3.Skin.Game_Judge_X[j];
								y = TJAPlayer3.Skin.Game_Judge_Y[j];
							}
							x += (moveValue * TJAPlayer3.Skin.Game_Judge_Move[0]) + TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLX(j);
							y += (moveValue * TJAPlayer3.Skin.Game_Judge_Move[1]) + TJAPlayer3.stage演奏ドラム画面.GetJPOSCROLLY(j);

							TJAPlayer3.Tx.Judge.Opacity = (int)(255f - (judgeC.counter.CurrentValue >= 360 ? ((judgeC.counter.CurrentValue - 360) / 50.0f) * 255f : 0f));
							TJAPlayer3.Tx.Judge.t2D描画(x, y, judgeC.rc);
                        }
					}
				}
			}
            return 0;
		}

		public void Start(int player, E判定 judge)
		{
			JudgeAnime judgeAnime = new();
			judgeAnime.counter.Start(0, 410, 1, TJAPlayer3.Timer);
			judgeAnime.Judge = judge;

			//int njudge = judge == E判定.Perfect ? 0 : judge == E判定.Good ? 1 : judge == E判定.ADLIB ? 3 : judge == E判定.Auto ? 0 : 2;

			int njudge = 2;
			if (JudgesDict.ContainsKey(judge))
            {
				njudge = JudgesDict[judge];
            }

			int height = TJAPlayer3.Tx.Judge.szテクスチャサイズ.Height / 5;
			judgeAnime.rc = new Rectangle(0, (int)njudge * height, TJAPlayer3.Tx.Judge.szテクスチャサイズ.Width, height);

			JudgeAnimes[player].Add(judgeAnime);
		}

		// その他

		#region [ private ]
		//-----------------

		private static Dictionary<E判定, int> JudgesDict = new Dictionary<E判定, int>
		{
			[E判定.Perfect] = 0,
			[E判定.Auto] = 0,
			[E判定.Good] = 1,
			[E判定.Bad] = 2,
			[E判定.Miss] = 2,
			[E判定.ADLIB] = 3,
			[E判定.Mine] = 4,
		};

		private List<JudgeAnime>[] JudgeAnimes = new List<JudgeAnime>[5];
		private class JudgeAnime
        {
			public E判定 Judge;
			public Rectangle rc;
			public CCounter counter = new CCounter();
		}

		private float CubicEaseOut(float p)
		{
			float f = (p - 1);
			return f * f * f + 1;
		}
		//-----------------
		#endregion
	}
}
