using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using FDK.ExtensionMethods;

namespace FDK
{
    /// <summary>
    /// SoundGroupLevelController holds the current sound level value for each
    /// of the unique sound groups, along with an increment by which they can
    /// easily be adjusted.
    ///
    /// Configuration changes to the sound group levels are provided to the
    /// controller via binding code which allows CConfigIni and
    /// SoundGroupLevelController to be unaware of one another.
    /// See ConfigIniToSoundGroupLevelControllerBinder for more details.
    ///
    /// Dynamic adjustment of sound group levels during song selection and song
    /// playback are managed via a small dependency taken by the respective
    /// stage classes. See KeyboardSoundGroupLevelControlHandler and its usages
    /// for more details.
    ///
    /// As new sound objects are created, including when reloading sounds due
    /// to a changer in audio output device, SoundGroupLevelController ensures
    /// that they are provided with the current level for their associated
    /// sound group by subscribing to notifications regarding changes to a
    /// collection of sound objects provided during construction. This
    /// observable collection comes from the sound manager, but without either
    /// it or this class being directly aware of one another.
    ///
    /// As sound group levels are changed, SoundGroupLevelController updates
    /// all existing sound objects group levels by iterating that same
    /// observable collection.
    /// </summary>
    public sealed class SoundGroupLevelController
    {
        private readonly Dictionary<ESoundGroup, int> _levelBySoundGroup = new Dictionary<ESoundGroup, int>
        {
            [ESoundGroup.SoundEffect] = CSound.MaximumGroupLevel,
            [ESoundGroup.Voice] = CSound.MaximumGroupLevel,
            [ESoundGroup.SongPreview] = CSound.MaximumGroupLevel,
            [ESoundGroup.SongPlayback] = CSound.MaximumGroupLevel,
            [ESoundGroup.Unknown] = CSound.MaximumGroupLevel
        };

        private readonly ObservableCollection<CSound> _sounds;

        private int _keyboardSoundLevelIncrement;

        public SoundGroupLevelController(ObservableCollection<CSound> sounds)
        {
            _sounds = sounds;

            _sounds.CollectionChanged += SoundsOnCollectionChanged;
        }

        public void SetLevel(ESoundGroup soundGroup, int level)
        {
            var clampedLevel = level.Clamp(CSound.MinimumGroupLevel, CSound.MaximumGroupLevel);

            if (_levelBySoundGroup[soundGroup] == clampedLevel)
            {
                return;
            }

            _levelBySoundGroup[soundGroup] = clampedLevel;

            foreach (var sound in _sounds)
            {
                if (sound.SoundGroup == soundGroup)
                {
                    SetLevel(sound);
                }
            }

            RaiseLevelChanged(soundGroup, clampedLevel);
        }

        public void SetKeyboardSoundLevelIncrement(int keyboardSoundLevelIncrement)
        {
            _keyboardSoundLevelIncrement = keyboardSoundLevelIncrement;
        }

        public void AdjustLevel(ESoundGroup soundGroup, bool isAdjustmentPositive)
        {
            var adjustmentIncrement = isAdjustmentPositive
                ? _keyboardSoundLevelIncrement
                : -_keyboardSoundLevelIncrement;

            SetLevel(soundGroup, _levelBySoundGroup[soundGroup] + adjustmentIncrement);
        }

        private void SetLevel(CSound sound)
        {
            sound.GroupLevel = _levelBySoundGroup[sound.SoundGroup];
        }

        private void SoundsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Replace:
                    foreach (CSound sound in e.NewItems)
                    {
                        SetLevel(sound);
                    }
                    break;
            }
        }

        private void RaiseLevelChanged(ESoundGroup soundGroup, int level)
        {
            LevelChanged?.Invoke(this, new LevelChangedEventArgs(soundGroup, level));
        }

        public class LevelChangedEventArgs : EventArgs
        {
            public LevelChangedEventArgs(ESoundGroup soundGroup, int level)
            {
                SoundGroup = soundGroup;
                Level = level;
            }

            public ESoundGroup SoundGroup { get; private set; }
            public int Level { get; private set; }
        }

        public event EventHandler<LevelChangedEventArgs> LevelChanged;
    }
}
