using FDK;

namespace OpenTaiko;

// Tracks Tower mode state for a single gameplay session.
// Owned by CStage演奏画面共通 and initialized in Activate().
internal class CFloorManagement {
	public CFloorManagement(int life) {
		reinitialize(life);
	}

	public void reinitialize(int life) {
		LastRegisteredFloor = 1;
		MaxNumberOfLives = life;
		CurrentNumberOfLives = life;
		InvincibilityFrames = null;

		if (MaxNumberOfLives <= 0) {
			MaxNumberOfLives = 5;
			CurrentNumberOfLives = 5;
		}
	}

	public void reload() {
		LastRegisteredFloor = 1;
		CurrentNumberOfLives = MaxNumberOfLives;
		InvincibilityFrames = null;
	}

	public void damage() {
		if (InvincibilityFrames != null && InvincibilityFrames.CurrentValue < InvincibilityDurationSpeedDependent)
			return;

		if (CurrentNumberOfLives > 0) {
			InvincibilityFrames = new CCounter(0, InvincibilityDurationSpeedDependent + 1000, 1, OpenTaiko.Timer);
			CurrentNumberOfLives--;
			//TJAPlayer3.Skin.soundTowerMiss.t再生する();
			CCharacter.GetCharacter(0).PlayVoice(0, CCharacter.VOICE_TOWER_MISS);
		}
	}

	public bool isBlinking() {
		if (InvincibilityFrames == null || InvincibilityFrames.CurrentValue >= InvincibilityDurationSpeedDependent)
			return false;

		if (InvincibilityFrames.CurrentValue % 200 > 100)
			return false;

		return true;
	}

	public void loopFrames() {
		if (InvincibilityFrames != null)
			InvincibilityFrames.Tick();
	}

	public int LastRegisteredFloor = 1;
	public int MaxNumberOfLives = 5;
	public int CurrentNumberOfLives = 5;

	// Divides by song speed so the wall-clock window shrinks at high speeds,
	// preventing cheese by skipping fewer notes when using speed mods.
	public double InvincibilityDurationSpeedDependent {
		get => CTja.TjaDurationToGameDuration(InvincibilityDuration);
	}

	// ms
	public readonly int InvincibilityDuration = 2000;
	public CCounter? InvincibilityFrames = null;
}
