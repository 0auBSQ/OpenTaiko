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
		private bool bPlayingAction = false;
		private int iPlayer;

		public CCharacterController(int iPlayer) {
			this.iPlayer = iPlayer;
		}

		public void PlayAction(string animationType) {
			CCharacter character = CCharacter.GetCharacter(iPlayer);
			if (!character.AvailableAnimation(animationType)) return;
			strActionAnimation = animationType;
			character.ResetAnimationCounter(animationType);

			bPlayingAction = true;
		}

		public void StopAction() {
			strActionAnimation = null;
			bPlayingAction = false;
		}

		public void ResetCounter() {
			CCharacter character = CCharacter.GetCharacter(iPlayer);
			character.ResetAnimationCounter(strCurrentAnimation);
		}

		public void Update() {
			CCharacter character = CCharacter.GetCharacter(iPlayer);
			character.SetAnimationDuration(strCurrentAnimation, dbDuration);

			bool looping = !bPlayingAction && bLooping;
			bool animationEnded = character.Update(strCurrentAnimation, looping);
			if (bPlayingAction && animationEnded) {
				StopAction();
				ResetCounter();
				Update();
			}
		}

		public void Draw(float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null) {
			CCharacter character = CCharacter.GetCharacter(iPlayer);
			character.Draw(strCurrentAnimation, x, y, scaleX, scaleY, opacity, color, gradientMap: PaletteManager.GetEffectivePalette(iPlayer)?.LuaMap);
		}
	}
}
