using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using FDK.ExtensionMethods;

namespace TJAPlayer3
{
    internal static class CLagLogger
    {
        private const int MaximumLag = 200;
        private const int MinimumLag = 0 - MaximumLag;
        private const int Offset = 1 - MinimumLag;

        private static readonly List<int> LagValues = new List<int>(2000);

        public static void Add(int nPlayer, CDTX.CChip pChip)
        {
            if (nPlayer != 0)
            {
                return;
            }

            switch (pChip.nチャンネル番号)
            {
                case 0x15:
                case 0x16:
                case 0x17:
                case 0x18:
                case 0x1F:
                    return;
            }

            var pChipNLag = pChip.nLag;

            LagValues.Add(pChipNLag);
        }

        public static double? LogAndReturnMeanLag()
        {
            if (LagValues.Count < 30)
            {
                return null;
            }

            var orderedLagValues = LagValues.OrderBy(x => x).ToList();

            var mean = orderedLagValues.Average();
            var mid = (orderedLagValues.Count - 1) / 2.0;
            var median = (orderedLagValues[(int)(mid)] + orderedLagValues[(int)(mid + 0.5)]) / 2.0;
            var groups = orderedLagValues.GroupBy(v => v).ToList();
            var maxCount = groups.Max(g => g.Count());
            var modes = string.Join(",", groups.Where(g => g.Count() == maxCount).Select(o => o.Key.ToString()).ToArray());
            var stdev = Math.Sqrt(orderedLagValues.Select(o => Math.Pow(o - mean, 2)).Average());

            Trace.TraceInformation(
                $"{nameof(CLagLogger)}.{nameof(LogAndReturnMeanLag)}: Mean lag: {mean}. Median lag: {median}. Mode(s) of lag: {modes}. Standard deviation of lag: {stdev}.");

            var hitChipCountsIndexedByOffsetLag = new int[1 + MaximumLag + 1 + MaximumLag + 1];
            foreach (var pChipNLag in LagValues)
            {
                hitChipCountsIndexedByOffsetLag[pChipNLag.Clamp(MinimumLag - 1, MaximumLag + 1) + Offset]++;
            }

            var sbHeader = new StringBuilder();
            var sbData = new StringBuilder();

            var doneOne = false;
            for (var i = 0; i < hitChipCountsIndexedByOffsetLag.Length; i++)
            {
                var count = hitChipCountsIndexedByOffsetLag[i];

                if (count != 0)
                {
                    if (doneOne)
                    {
                        sbHeader.Append(",");
                        sbData.Append(",");
                    }
                    else
                    {
                        doneOne = true;
                    }

                    var lag = i - Offset;
                    if (lag < MinimumLag)
                    {
                        sbHeader.Append($"< {MinimumLag}");
                    }
                    else if (lag > MaximumLag)
                    {
                        sbHeader.Append($"> {MaximumLag}");
                    }
                    else
                    {
                        sbHeader.Append(lag);
                    }

                    sbData.Append(count);
                }
            }

            Trace.TraceInformation(
                $"{nameof(CLagLogger)}.{nameof(LogAndReturnMeanLag)}: Hit chip counts, indexed by lag in milliseconds:{Environment.NewLine}{sbHeader}{Environment.NewLine}{sbData}");

            LagValues.Clear();

            return mean;
        }
    }
}
