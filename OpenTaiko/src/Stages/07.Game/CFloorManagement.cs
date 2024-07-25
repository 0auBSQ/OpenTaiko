using FDK;

namespace TJAPlayer3 {
	// Small static class which refers to the Tower mode important informations
	static internal class CFloorManagement {
		public static void reinitialize(int life) {
			CFloorManagement.LastRegisteredFloor = 1;
			CFloorManagement.MaxNumberOfLives = life;
			CFloorManagement.CurrentNumberOfLives = life;
			CFloorManagement.InvincibilityFrames = null;
		}

		public static void reload() {
			CFloorManagement.LastRegisteredFloor = 1;
			CFloorManagement.CurrentNumberOfLives = CFloorManagement.MaxNumberOfLives;
			CFloorManagement.InvincibilityFrames = null;
		}

		public static void damage() {
			if (CFloorManagement.InvincibilityFrames != null && CFloorManagement.InvincibilityFrames.CurrentValue < CFloorManagement.InvincibilityDurationSpeedDependent)
				return;

			if (CFloorManagement.CurrentNumberOfLives > 0) {
				CFloorManagement.InvincibilityFrames = new CCounter(0, CFloorManagement.InvincibilityDurationSpeedDependent + 1000, 1, TJAPlayer3.Timer);
				CFloorManagement.CurrentNumberOfLives--;
				//TJAPlayer3.Skin.soundTowerMiss.t再生する();
				TJAPlayer3.Skin.voiceTowerMiss[TJAPlayer3.SaveFile]?.tPlay();
			}
		}

		public static bool isBlinking() {
			if (CFloorManagement.InvincibilityFrames == null || CFloorManagement.InvincibilityFrames.CurrentValue >= CFloorManagement.InvincibilityDurationSpeedDependent)
				return false;

			if (CFloorManagement.InvincibilityFrames.CurrentValue % 200 > 100)
				return false;

			return true;
		}

		public static void loopFrames() {
			if (CFloorManagement.InvincibilityFrames != null)
				CFloorManagement.InvincibilityFrames.Tick();
		}

		public static int LastRegisteredFloor = 1;
		public static int MaxNumberOfLives = 5;
		public static int CurrentNumberOfLives = 5;

		public static double InvincibilityDurationSpeedDependent {
			get => ((double)InvincibilityDuration) / TJAPlayer3.ConfigIni.SongPlaybackSpeed;
		}

		// ms
		public static readonly int InvincibilityDuration = 2000;
		public static CCounter InvincibilityFrames = null;
	}
}
