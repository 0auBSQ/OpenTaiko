using System;
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
			base.b活性化してない = true;
		}

        public override void On活性化()
        {
			JudgeAnimes = new JudgeAnime[2, 512];
			for (int i = 0; i < 512; i++)
			{
				JudgeAnimes[0, i] = new JudgeAnime();
				JudgeAnimes[1, i] = new JudgeAnime();
			}
            base.On活性化();
        }

        public override void On非活性化()
        {
			for (int i = 0; i < 512; i++)
            {
				JudgeAnimes[0, i] = null;
				JudgeAnimes[1, i] = null;
            }
            base.On非活性化();
        }

        // CActivity 実装（共通クラスからの差分のみ）
        public override int On進行描画()
		{
			if (!base.b活性化してない)
			{
				for (int i = 0; i < 512; i++)
				{
					for(int j = 0; j < 2; j++)
					{
						if (JudgeAnimes[j, i].counter.b停止中) continue;
						JudgeAnimes[j, i].counter.t進行();

						if (TJAPlayer3.Tx.Judge != null)
                        {
							float x = TJAPlayer3.Skin.nScrollFieldX[0] - TJAPlayer3.Tx.Judge.szテクスチャサイズ.Width / 2;
							float y = (TJAPlayer3.Skin.nScrollFieldY[j] - 53 + CubicEaseOut((float)(JudgeAnimes[j, i].counter.n現在の値 >= 180 ? 180 : JudgeAnimes[j, i].counter.n現在の値) / 180f) * 20f) - 10;
							TJAPlayer3.Tx.Judge.Opacity = (int)(255f - (JudgeAnimes[j, i].counter.n現在の値 >= 360 ? ((JudgeAnimes[j, i].counter.n現在の値 - 360) / 50.0f) * 255f : 0f));
							TJAPlayer3.Tx.Judge.t2D描画(TJAPlayer3.app.Device, x, y, JudgeAnimes[j, i].rc);
                        }
						
					}
				}
			}
            return 0;
		}

		public void Start(int player, E判定 judge)
		{
			JudgeAnimes[player, JudgeAnime.Index].counter.t開始(0, 410, 1, TJAPlayer3.Timer);
			JudgeAnimes[player, JudgeAnime.Index].Judge = judge;
			int njudge = judge == E判定.Perfect ? 0 : judge == E判定.Good ? 1 : judge == E判定.Auto ? 0 : 2;
			JudgeAnimes[player, JudgeAnime.Index].rc = new Rectangle(0, (int)njudge * 60, 90, 60);
			JudgeAnime.Index++;
			if (JudgeAnime.Index >= 511) JudgeAnime.Index = 0;
		}

		// その他

		#region [ private ]
		//-----------------

		private JudgeAnime[,] JudgeAnimes;
		private class JudgeAnime
        {
			public static int Index;
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
