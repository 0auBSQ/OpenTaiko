﻿using FDK;
using Silk.NET.Maths;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko {
	class PuchiChara : CActivity {
		public PuchiChara() {
			base.IsDeActivated = true;
		}

		public override void Activate() {
			Counter = new CCounter(0, OpenTaiko.Skin.Game_PuchiChara[2] - 1, OpenTaiko.Skin.Game_PuchiChara_Timer * 0.5f, OpenTaiko.Timer);
			SineCounter = new CCounter(0, 360, OpenTaiko.Skin.Game_PuchiChara_SineTimer, SoundManager.PlayTimer);
			SineCounterIdle = new CCounter(1, 360, (float)OpenTaiko.Skin.Game_PuchiChara_SineTimer * 2f, OpenTaiko.Timer);
			this.inGame = false;
			base.Activate();
		}
		public override void DeActivate() {
			Counter = null;
			SineCounter = null;
			SineCounterIdle = null;
			base.DeActivate();
		}

		public static int tGetPuchiCharaIndexByName(int p) {
			var _pc = OpenTaiko.SaveFileInstances[p].data.PuchiChara;
			var _pcs = OpenTaiko.Skin.Puchicharas_Name;
			int puriChar = 0;
			if (_pcs.Contains(_pc))
				puriChar = _pcs.ToList().IndexOf(_pc);

			return puriChar;
		}

		public void ChangeBPM(double bpm) {
			Counter = new CCounter(0, OpenTaiko.Skin.Game_PuchiChara[2] - 1, (int)(OpenTaiko.Skin.Game_PuchiChara_Timer * bpm / OpenTaiko.Skin.Game_PuchiChara[2]), OpenTaiko.Timer);
			SineCounter = new CCounter(1, 360, OpenTaiko.Skin.Game_PuchiChara_SineTimer * bpm / 180, SoundManager.PlayTimer);
			this.inGame = true;
		}

		public void IdleAnimation() {
			this.inGame = false;
		}

		/// <summary>
		/// Draws Puchi Chara (small character) on the screen. Note: this is not an override.
		/// </summary>
		/// <param name="x">X coordinate (center)</param>
		/// <param name="y">Y coordinate (center)</param>
		/// <param name="alpha">Opacity (0-255)</param>
		/// <returns></returns>
		public int On進行描画(int x, int y, bool isGrowing, int alpha = 255, bool isBalloon = false, int player = 0, float scale = 1.0f) {
			if (!OpenTaiko.ConfigIni.ShowPuchiChara) return base.Draw();
			if (Counter == null || SineCounter == null || OpenTaiko.Tx.Puchichara == null) return base.Draw();
			Counter.TickLoop();
			SineCounter.TickLoopDB();
			SineCounterIdle.TickLoop();

			int p = OpenTaiko.GetActualPlayer(player);

			if (inGame)
				sineY = (double)SineCounter.CurrentValue;
			else
				sineY = (double)SineCounterIdle.CurrentValue;

			sineY = Math.Sin(sineY * (Math.PI / 180)) * (OpenTaiko.Skin.Game_PuchiChara_Sine * (isBalloon ? OpenTaiko.Skin.Game_PuchiChara_Scale[1] : OpenTaiko.Skin.Game_PuchiChara_Scale[0]));

			int puriChar = PuchiChara.tGetPuchiCharaIndexByName(p);
			var chara = OpenTaiko.Tx.Puchichara[puriChar].tx;

			if (chara != null) {
				float puchiScale = OpenTaiko.Skin.Resolution[1] / 720.0f;

				chara.vcScaleRatio = new Vector3D<float>((isBalloon ? OpenTaiko.Skin.Game_PuchiChara_Scale[1] * puchiScale : OpenTaiko.Skin.Game_PuchiChara_Scale[0] * puchiScale));
				chara.vcScaleRatio.X *= scale;
				chara.vcScaleRatio.Y *= scale;
				chara.Opacity = alpha;

				/* Todo :
                **
                ** - Yellow light color filter when isGrowing is true
                */

				int adjustedX = x - 32;
				int adjustedY = y - 32;

				chara.t2D拡大率考慮中央基準描画(adjustedX, adjustedY + (int)sineY, new Rectangle((Counter.CurrentValue + 2) * OpenTaiko.Skin.Game_PuchiChara[0], 0, OpenTaiko.Skin.Game_PuchiChara[0], OpenTaiko.Skin.Game_PuchiChara[1]));
			}

			return base.Draw();
		}

		public double sineY;

		public CCounter Counter;
		private CCounter SineCounter;
		private CCounter SineCounterIdle;
		private bool inGame;
	}
}
