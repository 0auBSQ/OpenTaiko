using FDK;

namespace TJAPlayer3
{
    /// <summary>
    /// KeyboardSoundGroupLevelControlHandler is called by the song selection
    /// and song play stages when handling keyboard input. By delegating to
    /// this class they are able to support a centrally-managed and consistent
    /// set of keyboard shortcuts for dynamically adjusting four sound group
    /// levels:
    /// - sound effect level, via Ctrl and either of the Minus or Equals keys
    /// - voice level, via Shift and either of the Minus or Equals keys
    /// - song preview and song playback level, via the Minus or Equals key
    ///
    /// When the sound group levels are adjusted in this manner, the
    /// SoundGroupLevelController (and handlers bound to its events) ensure
    /// that both the sound object group levels are updated and the application
    /// configuration is updated. See ConfigIniToSoundGroupLevelControllerBinder
    /// for more details on the latter.
    /// </summary>
    internal static class KeyboardSoundGroupLevelControlHandler
    {
        internal static void Handle(
            IInputDevice keyboard,
            SoundGroupLevelController soundGroupLevelController,
            CSkin skin,
            bool isSongPreview)
        {
            var isAdjustmentPositive = keyboard.KeyPressed((int)SlimDXKeys.Key.RightBracket);
            if (!(isAdjustmentPositive || keyboard.KeyPressed((int)SlimDXKeys.Key.LeftBracket)))
            {
                return;
            }

            ESoundGroup soundGroup;
            CSkin.Cシステムサウンド システムサウンド = null;

            if (keyboard.KeyPressing((int)SlimDXKeys.Key.LeftControl) ||
                keyboard.KeyPressing((int)SlimDXKeys.Key.RightControl))
            {
                soundGroup = ESoundGroup.SoundEffect;
                システムサウンド = skin.sound決定音;
            }
            else if (keyboard.KeyPressing((int)SlimDXKeys.Key.LeftShift) ||
                     keyboard.KeyPressing((int)SlimDXKeys.Key.RightShift))
            {
                soundGroup = ESoundGroup.Voice;
                システムサウンド = skin.soundゲーム開始音;
            }
            else if (isSongPreview)
            {
                soundGroup = ESoundGroup.SongPlayback;
            }
            else
            {
                soundGroup = ESoundGroup.SongPlayback;
            }

            soundGroupLevelController.AdjustLevel(soundGroup, isAdjustmentPositive);
            システムサウンド?.t再生する();
        }
    }
}
