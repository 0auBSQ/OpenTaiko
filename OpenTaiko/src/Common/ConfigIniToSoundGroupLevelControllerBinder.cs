using System;
using FDK;

namespace TJAPlayer3
{
    /// <summary>
    /// The ConfigIniToSoundGroupLevelControllerBinder allows for updated sound
    /// group level values, and keyboard sound level adjustment increment
    /// values, to flow between CConfigIni and the SoundGroupLevelController
    /// without either of those two classes being aware of one another.
    /// See those classes properties, methods, and events for more details. 
    /// </summary>
    internal static class ConfigIniToSoundGroupLevelControllerBinder
    {
        internal static void Bind(CConfigIni configIni, SoundGroupLevelController soundGroupLevelController)
        {
            soundGroupLevelController.SetLevel(ESoundGroup.SoundEffect, configIni.SoundEffectLevel);
            soundGroupLevelController.SetLevel(ESoundGroup.Voice, configIni.VoiceLevel);
            soundGroupLevelController.SetLevel(ESoundGroup.SongPreview, configIni.SongPreviewLevel);
            soundGroupLevelController.SetLevel(ESoundGroup.SongPlayback, configIni.SongPlaybackLevel);
            soundGroupLevelController.SetKeyboardSoundLevelIncrement(configIni.KeyboardSoundLevelIncrement);

            configIni.PropertyChanged += (sender, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(CConfigIni.SoundEffectLevel):
                        soundGroupLevelController.SetLevel(ESoundGroup.SoundEffect, configIni.SoundEffectLevel);
                        break;
                    case nameof(CConfigIni.VoiceLevel):
                        soundGroupLevelController.SetLevel(ESoundGroup.Voice, configIni.VoiceLevel);
                        break;
                    case nameof(CConfigIni.SongPreviewLevel):
                        soundGroupLevelController.SetLevel(ESoundGroup.SongPreview, configIni.SongPreviewLevel);
                        break;
                    case nameof(CConfigIni.SongPlaybackLevel):
                        soundGroupLevelController.SetLevel(ESoundGroup.SongPlayback, configIni.SongPlaybackLevel);
                        break;
                    case nameof(CConfigIni.KeyboardSoundLevelIncrement):
                        soundGroupLevelController.SetKeyboardSoundLevelIncrement(configIni.KeyboardSoundLevelIncrement);
                        break;
                }
            };

            soundGroupLevelController.LevelChanged += (sender, args) =>
            {
                switch (args.SoundGroup)
                {
                    case ESoundGroup.SoundEffect:
                        configIni.SoundEffectLevel = args.Level;
                        break;
                    case ESoundGroup.Voice:
                        configIni.VoiceLevel = args.Level;
                        break;
                    case ESoundGroup.SongPreview:
                        configIni.SongPreviewLevel = args.Level;
                        break;
                    case ESoundGroup.SongPlayback:
                        configIni.SongPlaybackLevel = args.Level;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
        }
    }
}
