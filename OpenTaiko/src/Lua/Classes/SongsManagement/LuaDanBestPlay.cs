namespace OpenTaiko {
	/// <summary>Wraps the DanExam1–7 best-play arrays for a single Dan record.</summary>
	internal class LuaDanBestPlay {
		private BestPlayRecords.CBestPlayRecord? _record;

		public LuaDanBestPlay() { }
		public LuaDanBestPlay(BestPlayRecords.CBestPlayRecord record) { _record = record; }

		public bool HasRecord => _record != null;

		/// <summary>Returns the best exam scores for the given slot (1-based: 1=EXAM1 … 7=EXAM7).
		/// Global exams return a length-1 array; per-song exams return length = song count.
		/// Returns an empty array when no record exists or the slot is out of range.</summary>
		public int[] GetExam(int examSlot) {
			if (_record == null) return Array.Empty<int>();
			return examSlot switch {
				1 => _record.DanExam1.ToArray(),
				2 => _record.DanExam2.ToArray(),
				3 => _record.DanExam3.ToArray(),
				4 => _record.DanExam4.ToArray(),
				5 => _record.DanExam5.ToArray(),
				6 => _record.DanExam6.ToArray(),
				7 => _record.DanExam7.ToArray(),
				_ => Array.Empty<int>()
			};
		}
	}
}
