using System;

namespace FDK
{
    /// <summary>
    /// The LoudnessMetadata structure is used to carry, and assist with
    /// calculations related to, integrated loudness and true peak
    /// loudness.
    /// </summary>
    [Serializable]
    public struct LoudnessMetadata
    {
        public readonly Lufs Integrated;
        public readonly Lufs? TruePeak;

        public LoudnessMetadata(Lufs integrated, Lufs? truePeak)
        {
            Integrated = integrated;
            TruePeak = truePeak;
        }
    }
}