using System;

namespace FDK
{
    /// <summary>
    /// The Lufs structure is used to carry, and assist with calculations related to,
    /// Loudness Units relative to Full Scale. LUFS are measured in absolute scale
    /// and whole values represent one decibel.
    /// </summary>
    [Serializable]
    public struct Lufs
    {
        private readonly double _value;

        public Lufs(double value)
        {
            _value = value;
        }

        public double ToDouble() => _value;

        public Lufs Min(Lufs lufs)
        {
            return new Lufs(Math.Min(_value, lufs._value));
        }

        public Lufs Negate()
        {
            return new Lufs(-_value);
        }

        public override string ToString()
        {
            return _value.ToString();
        }

        public static Lufs operator- (Lufs left, Lufs right)
        {
            return new Lufs(left._value - right._value);
        }

        public static Lufs operator+ (Lufs left, Lufs right)
        {
            return new Lufs(left._value + right._value);
        }
    }
}
