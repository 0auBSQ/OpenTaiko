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
				.Select(s => s.Preview)
				.Where(c => c != null)
				.Select(c => new LuaCharacterEntry(c))
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
	/// Holds a name-bound <see cref="LuaCharacter"/> (no textures pre-loaded)
	/// and exposes character metadata and unlock helpers to Lua scripts.
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

		internal LuaCharacterEntry(CCharacterLua character) {
			FolderName = character.info.dirName;
			DisplayName = character.info.metadata.tGetName();
			Rarity = character.info.metadata.Rarity;
			// Non-owning: wraps the already-loaded CCharacterLua — no extra Lua scripts created.
			Character = new LuaCharacter(character);
			UnlockCondition = new LuaUnlockCondition(character.info.unlock);
		}

		private bool _disposed;
		public void Dispose() {
			if (_disposed) return;
			_disposed = true;
			Character.Dispose();
		}
	}
}
