using FDK;

namespace OpenTaiko;

/// <summary>
/// KeyboardSoundGroupLevelControlHandler is called by the song selection
/// and song play stages when handling keyboard input. By delegating to
/// this class they are able to support a centrally-managed and consistent
/// set of keyboard shortcuts for dynamically adjusting four sound group
/// levels (the volume keys are [ and ] by default):
/// - sound effect level, via Ctrl and either of the Minus or Equals keys
/// - voice level, via Shift and either of the Minus or Equals keys
/// - song playback level, via Alt and the volume keys
/// - master level, via the volume keys alone
///
/// When the sound group levels are adjusted in this manner, the
/// SoundGroupLevelController (and handlers bound to its events) ensure
/// that both the sound object group levels are updated and the application
/// configuration is updated. See ConfigIniToSoundGroupLevelControllerBinder
/// for more details on the latter.
/// </summary>
internal static class KeyboardSoundGroupLevelControlHandler {
	internal static void Handle(
		IInputDevice keyboard,
		SoundGroupLevelController soundGroupLevelController,
		CSkin skin,
		bool isSongPreview) {
		bool isAdjustmentPositive = OpenTaiko.ConfigIni.KeyAssign.System.SongVolIncrease.IsPressed();
		bool isAdjustmentNegative = OpenTaiko.ConfigIni.KeyAssign.System.SongVolDecrease.IsPressed();

		if (!(isAdjustmentPositive || isAdjustmentNegative)) return;

		ESoundGroup soundGroup;
		CSkin.CSystemSound? SystemSound = null;

		if (keyboard.KeyPressing((int)SlimDXKeys.Key.LeftControl) ||
			keyboard.KeyPressing((int)SlimDXKeys.Key.RightControl)) {
			soundGroup = ESoundGroup.SoundEffect;
			SystemSound = skin.soundDecideSFX;
		} else if (keyboard.KeyPressing((int)SlimDXKeys.Key.LeftShift) ||
				   keyboard.KeyPressing((int)SlimDXKeys.Key.RightShift)) {
			soundGroup = ESoundGroup.Voice;
			SystemSound = skin.soundGameStartSound;
		} else if (keyboard.KeyPressing((int)SlimDXKeys.Key.LeftAlt) ||
				   keyboard.KeyPressing((int)SlimDXKeys.Key.RightAlt)) {
			soundGroup = ESoundGroup.SongPlayback;
		} else {
			soundGroup = ESoundGroup.Master;
		}

		soundGroupLevelController.AdjustLevel(soundGroup, isAdjustmentPositive);
		SystemSound?.tPlay();
	}
}
