namespace OpenTaiko {
	/// <summary>A single unlocked dan-title entry, exposed to Lua via <see cref="LuaSaveFile"/>.</summary>
	public class LuaDanTitleEntry {
		/// <summary>The title text (e.g. "十段").</summary>
		public string Title { get; }
		/// <summary>Whether this title was earned with a Gold clear.</summary>
		public bool IsGold { get; }
		/// <summary>Best clear status.</summary>
		public int ClearStatus { get; }

		internal LuaDanTitleEntry(string title, bool isGold, int clearStatus) {
			Title = title;
			IsGold = isGold;
			ClearStatus = clearStatus;
		}
	}
}
