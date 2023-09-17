using System;

namespace FDK.ExtensionMethods
{
    public static class DoubleExtensions
    {
        public static double Clamp(this double value, double min, double max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}