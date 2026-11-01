namespace OpenTaiko;

// The random draws of the chart mods (the Random modes, Minesweeper, Avalanche): one Next(100) per element of a draw
// timeline, in its order, from a seed. A chart drawing over its own timeline gets them in order. A chart drawing over
// another chart's timeline, as the AI battle's AI over player 1's, gets for each chip the draw of its twin there, so two
// loads of one course make the same decisions even though only player 1's timeline holds the mixer events and the two
// can order chips of one time differently. The draws are taken before the mods change either chart, as a twin is the
// chip with the same definition index, times and channel (the n-th such chip for the n-th: TCI charts define none). A
// chip without a twin gets the next draw after the timeline's.
internal static class ChartModDraws {
	private readonly record struct TwinKey(int IdxDefine, int MsTimeInt, double MsTime, int Channel, int Occurrence);

	// the fun mods' seed, so their draws differ from the note shuffle's of the same seed; a negative seed stays unseeded
	internal static int FunModSeed(int seed) => (seed < 0) ? seed : seed ^ 0x2545F491;

	// one draw in 0..99 for each chip of the chart, by index
	internal static int[] Over(List<CChip> chart, List<CChip> drawTimeline, int seed) {
		var rng = (seed >= 0) ? new Random(seed) : new Random();
		var draws = new int[chart.Count];
		if (ReferenceEquals(chart, drawTimeline)) {
			for (int i = 0; i < draws.Length; i++)
				draws[i] = rng.Next(100);
			return draws;
		}

		var twinDraws = new Dictionary<TwinKey, int>(drawTimeline.Count);
		foreach (var key in TwinKeysOf(drawTimeline))
			twinDraws.Add(key, rng.Next(100));
		var keys = TwinKeysOf(chart);
		for (int i = 0; i < draws.Length; i++)
			draws[i] = twinDraws.TryGetValue(keys[i], out int n) ? n : rng.Next(100);
		return draws;
	}

	// each chip's twin key, in timeline order
	private static TwinKey[] TwinKeysOf(List<CChip> chips) {
		var occurrences = new Dictionary<(int, int, double, int), int>(chips.Count);
		var keys = new TwinKey[chips.Count];
		for (int i = 0; i < chips.Count; i++) {
			var c = chips[i];
			var key = (c.idxDefine, c.nSoundTimems, c.dbSoundTimems, c.nChannelNo);
			occurrences.TryGetValue(key, out int n);
			occurrences[key] = n + 1;
			keys[i] = new TwinKey(c.idxDefine, c.nSoundTimems, c.dbSoundTimems, c.nChannelNo, n);
		}
		return keys;
	}
}
