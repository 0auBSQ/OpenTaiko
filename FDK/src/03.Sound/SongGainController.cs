namespace FDK
{
    /// <summary>
    /// SongGainController provides a central place through which song preview
    /// and song playback attempt to apply BS1770GAIN-based loudness metadata
    /// or .tja SONGVOL as the Gain of a song sound.
    ///
    /// By doing so through SongGainController instead of directly against the
    /// song (preview) CSound object, SongGainController can override the Gain
    /// value based on configuration or other information.
    /// </summary>
    public sealed class SongGainController
    {
        public bool ApplyLoudnessMetadata { private get; set; }
        public Lufs TargetLoudness { private get; set; }
        public bool ApplySongVol { private get; set; }

        public void Set(int songVol, LoudnessMetadata? songLoudnessMetadata, CSound sound)
        {
            if (ApplyLoudnessMetadata && songLoudnessMetadata.HasValue)
            {
                var gain = TargetLoudness - songLoudnessMetadata.Value.Integrated;

                sound.SetGain(gain, songLoudnessMetadata.Value.TruePeak);
            }
            else
            {
                sound.SetGain(ApplySongVol ? songVol : CSound.DefaultSongVol);
            }
        }
    }
}
