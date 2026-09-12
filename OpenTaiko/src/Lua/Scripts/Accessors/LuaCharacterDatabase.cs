using System.Linq;

namespace OpenTaiko {
	/// <summary>
	/// Lua-accessible database of all loaded characters.
	/// Exposed as the <c>CHARACTERLIST</c> global in every Lua script.
	/// Lifetime is managed by <see cref="TextureLoader"/> — created after characters are loaded
	/// and disposed on skin reload via <see cref="TextureLoader.DisposeTexture"/>.
	/// </summary>
	public class LuaCharacterDatabase : IDisposable {
		private readonly LuaCharacterEntry[] _entries;

		// ────────────────────────────────────────────────────────────────────
		// Construction
		// ────────────────────────────────────────────────────────────────────

		internal LuaCharacterDatabase(TextureLoader.CCharacterLuaSet[] characters) {
			_entries = characters
				.Where(s => s != null)
				.Select(s => new LuaCharacterEntry(s))
				.ToArray();
		}

		// ────────────────────────────────────────────────────────────────────
		// Queries
		// ────────────────────────────────────────────────────────────────────

		/// <summary>Total number of characters.</summary>
		public int Count => _entries.Length;

		/// <summary>Returns all characters as a list.</summary>
		public List<LuaCharacterEntry> GetAll() => [.. _entries];

		/// <summary>Returns the character entry at the given 0-based index, or nil if out of range.</summary>
		public LuaCharacterEntry? GetByIndex(int index) =>
			(index >= 0 && index < _entries.Length) ? _entries[index] : null;

		/// <summary>Returns the character entry whose folder name matches, or nil if not found.</summary>
		public LuaCharacterEntry? GetByName(string name) =>
			_entries.FirstOrDefault(e => e.FolderName == name);

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

	/// <summary>
	/// A single entry in <see cref="LuaCharacterDatabase"/>.
	/// Holds a <see cref="LuaCharacter"/> over the character's preview instance, which (like its textures)
	/// is only created when first used, and exposes character metadata and unlock helpers to Lua scripts.
	/// </summary>
	public class LuaCharacterEntry : IDisposable {
		/// <summary>Folder-name key used in save files.</summary>
		public string FolderName { get; }

		/// <summary>Localised display name.</summary>
		public string DisplayName { get; }

		/// <summary>Rarity string (e.g. "Common", "Rare").</summary>
		public string Rarity { get; }

		/// <summary>
		/// Name-bound <see cref="LuaCharacter"/> instance.
		/// No animations are loaded until <see cref="LuaCharacter.LoadAnimation"/> is called.
		/// </summary>
		public LuaCharacter Character { get; }

		/// <summary>Exposes the unlock condition to Lua.</summary>
		public LuaUnlockCondition UnlockCondition { get; }

		internal LuaCharacterEntry(TextureLoader.CCharacterLuaSet set) {
			FolderName = set.dirName;
			DisplayName = set.metadata.tGetName();
			Rarity = set.metadata.Rarity;
			// Non-owning: wraps the set's preview instance, created by the set on first access.
			Character = new LuaCharacter(() => set.Preview);
			UnlockCondition = new LuaUnlockCondition(set.unlock);
		}

		private bool _disposed;
		public void Dispose() {
			if (_disposed) return;
			_disposed = true;
			Character.Dispose();
		}
	}
}
