using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using OpenTaiko.Animations;

namespace OpenTaiko {
	internal class CCharacterController {
		public string strLoopAnimation { get; set; } = CCharacter.ANIM_GAME_NORMAL;
		public string? strActionAnimation { get; set; } = null;
		public double dbDuration { get; set; } = CCharacter.DEFAULT_DURATION;
		public bool bLooping { get; set; } = true;

		private string strCurrentAnimation => strActionAnimation ?? strLoopAnimation;
		private bool[] bPlayingAction = new bool[5];
		private int nBasePlayerIndex;

		public CCharacterController(int basePlayerIndex) {
			this.nBasePlayerIndex = basePlayerIndex;
		}

		public void PlayAction(int player, string animationType) {
			CCharacter character = CCharacter.GetCharacter(nBasePlayerIndex);
			if (!character.AvaiableAnimation(player, animationType)) return;
			strActionAnimation = animationType;
			character.ResetAnimationCounter(player, animationType);

			bPlayingAction[player] = true;
		}

		public void ResetCounter(int player) {
			CCharacter character = CCharacter.GetCharacter(nBasePlayerIndex);
			character.ResetAnimationCounter(player, strCurrentAnimation);
		}

		public void Update(int player) {
			CCharacter character = CCharacter.GetCharacter(nBasePlayerIndex);
			character.SetAnimationDuration(player, strCurrentAnimation, dbDuration);

			bool looping = !bPlayingAction[player] && bLooping;
			bool animationEnded = character.Update(player, strCurrentAnimation, looping);
			if (bPlayingAction[player] && animationEnded) {
				bPlayingAction[player] = false;
				strActionAnimation = null;

				ResetCounter(player);
				Update(player);
			}
		}

		public void Draw(int player, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {
			CCharacter character = CCharacter.GetCharacter(nBasePlayerIndex);
			character.Draw(player, strCurrentAnimation, x, y, scaleX, scaleY, opacity, color, flipX);
		}
	}
}
