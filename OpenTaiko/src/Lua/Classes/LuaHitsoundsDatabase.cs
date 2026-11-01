namespace OpenTaiko {
	/// <summary>
	/// Lua-accessible list of all hitsound sets available in the skin.
	/// Exposed as the <c>HITSOUNDSLIST</c> global in every Lua script.
	/// Backed by <see cref="CHitSounds"/> loaded from <c>Global/HitSounds/</c>.
	/// </summary>
	public class LuaHitsoundsDatabase {
		public int Count => OpenTaiko.Skin.hsHitSoundsInformations?.Count ?? 0;

		/// <summary>Returns the hitsound entry at the given 0-based index, or <c>null</c> if out of range.</summary>
		public LuaHitsoundEntry? GetByIndex(int index) {
			var hs = OpenTaiko.Skin.hsHitSoundsInformations;
			if (hs == null || index < 0 || index >= hs.Count) return null;
			return new LuaHitsoundEntry(index, hs);
		}

		/// <summary>Returns the hitsound entry whose folder name matches (case-insensitive), or <c>null</c> if not found.</summary>
		public LuaHitsoundEntry? GetByName(string name) {
			var hs = OpenTaiko.Skin.hsHitSoundsInformations;
			if (hs == null) return null;
			int idx = hs.GetIndexByFolderName(name);
			// GetIndexByFolderName returns 0 as fallback; verify the name actually matched.
			if (idx == 0 && !hs.GetFolderName(0).Equals(name, StringComparison.OrdinalIgnoreCase))
				return null;
			return new LuaHitsoundEntry(idx, hs);
		}
	}

	/// <summary>A single hitsound-set entry exposed to Lua.</summary>
	public class LuaHitsoundEntry {
		private readonly int _index;
		private readonly CHitSounds _hs;

		internal LuaHitsoundEntry(int index, CHitSounds hs) {
			_index = index;
			_hs = hs;
		}

		/// <summary>The directory / folder name of this hitsound set (e.g. "Taiko").</summary>
		public string FolderName => _hs.GetFolderName(_index);

		/// <summary>The localised display name of this hitsound set.</summary>
		public string DisplayName => _hs.names[_index]?.GetString("???") ?? FolderName;

		/// <summary>Absolute path of the Don ("dong") sound file for this hitsound set.</summary>
		public string DonPath => _hs.GetDonPath(_index);

		/// <summary>Absolute path of the Ka sound file for this hitsound set.</summary>
		public string KaPath => _hs.GetKaPath(_index);
	}
}
