using System.Collections.Generic;

namespace TJAPlayer3.C曲リストノードComparers
{
    internal sealed class ComparerChain<T> : IComparer<T> where T : class
    {
        private readonly IComparer<T>[] _comparers;

        public ComparerChain(params IComparer<T>[] comparers)
        {
            _comparers = comparers;
        }

        public int Compare(T x, T y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            for (int i = 0; i < _comparers.Length; i++)
            {
                var result = _comparers[i].Compare(x, y);

                if (result != 0)
                {
                    return result;
                }
            }

            return 0;
        }
    }
}