using System.Linq;

namespace OpenTaiko {
	/// <summary>
	/// Lua-accessible database of all loaded puchichara.
	/// Exposed as the <c>PUCHICHARALIST</c> global in every Lua script.
	/// Lifetime is managed by <see cref="TextureLoader"/> — created after puchichara textures are loaded
	/// and disposed on skin reload via <see cref="TextureLoader.DisposeTexture"/>.
	/// </summary>
	public class LuaPuchicharaDatabase : IDisposable {
		private readonly LuaPuchichara[] _entries;

		// ────────────────────────────────────────────────────────────────────
		// Construction
		// ────────────────────────────────────────────────────────────────────

		internal LuaPuchicharaDatabase(CPuchichara[] puchicharas) {
			_entries = puchicharas.Select(p => new LuaPuchichara(p)).ToArray();
		}

		// ────────────────────────────────────────────────────────────────────
		// Queries
		// ────────────────────────────────────────────────────────────────────

		/// <summary>Total number of puchichara.</summary>
		public int Count => _entries.Length;

		/// <summary>Returns all puchichara as a list.</summary>
		public List<LuaPuchichara> GetAll() => [.. _entries];

		/// <summary>Returns the puchichara at the given 0-based index, or nil if out of range.</summary>
		public LuaPuchichara? GetByIndex(int index) =>
			(index >= 0 && index < _entries.Length) ? _entries[index] : null;

		/// <summary>Returns the puchichara whose folder name matches, or nil if not found.</summary>
		public LuaPuchichara? GetByName(string name) =>
			_entries.FirstOrDefault(p => p.FolderName == name);

		/// <summary>
		/// Returns the puchichara currently equipped by the given 0-based player,
		/// or nil if the player's selection cannot be resolved.
		/// </summary>
		public LuaPuchichara? GetPlayerPuchichara(int player) {
			int idx = PuchiChara.tGetPuchiCharaIndexByName(player);
			return GetByIndex(idx);
		}

		// ────────────────────────────────────────────────────────────────────
		// IDisposable
		// ────────────────────────────────────────────────────────────────────

		private bool _disposed;
		public void Dispose() {
			if (_disposed) return;
			_disposed = true;
			foreach (var entry in _entries)
				entry.Dispose();
		}
	}
}
