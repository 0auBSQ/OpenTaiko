using System;

namespace FDK.ExtensionMethods
{
    public static class Int32Extensions
    {
        public static int Clamp(this int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
    }
}
