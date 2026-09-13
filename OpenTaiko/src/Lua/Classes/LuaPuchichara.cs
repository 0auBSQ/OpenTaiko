namespace OpenTaiko {
	/// <summary>
	/// Lua-side wrapper around a <see cref="CPuchichara"/> instance.
	/// Owns its own <see cref="LuaTexture"/> objects (loaded from the puchichara folder)
	/// and exposes metadata and unlock-condition helpers to Lua scripts.
	/// Lifetime is managed by <see cref="LuaPuchicharaDatabase"/>.
	/// </summary>
	public class LuaPuchichara : IDisposable {
		/// <summary>The sprite-sheet texture (Chara.png).</summary>
		public LuaTexture tx { get; }

		/// <summary>The full render texture (Render.png).</summary>
		public LuaTexture render { get; }

		/// <summary>Localised display name.</summary>
		public string Name => _metadata.tGetName();

		/// <summary>Localised author name.</summary>
		public string Author => _metadata.tGetAuthor();

		/// <summary>Rarity string (e.g. "Common", "Rare").</summary>
		public string Rarity => _metadata.Rarity;

		/// <summary>Folder-name key used in save files.</summary>
		public string FolderName { get; }

		/// <summary>Exposes the unlock condition to Lua.</summary>
		public LuaUnlockCondition UnlockCondition { get; }

		/// <summary>Localised description; empty when the puchichara has none.</summary>
		public string Description => _metadata.tGetDescription();

		/// <summary>Effects.json values keyed by name (AllPurple, Autoroll, ShowAdlib, SplitLane, and any added later).</summary>
		public Dictionary<string, object> Effects => _effects ??= LuaEffectDict.From(_puchi.effect);
		public float CoinMultiplier => _puchi.GetEffectCoinMultiplier();

		private readonly DBPuchichara.PuchicharaData _metadata;
		private readonly CPuchichara _puchi;
		private Dictionary<string, object>? _effects;

		// ────────────────────────────────────────────────────────────────────
		// Construction (internal — created by LuaPuchicharaDatabase only)
		// ────────────────────────────────────────────────────────────────────

		internal LuaPuchichara(CPuchichara puchi) {
			_puchi = puchi;
			_metadata = puchi.metadata;
			FolderName = Path.GetFileName(puchi._path);
			UnlockCondition = new LuaUnlockCondition(puchi.unlock);

			tx = LoadTexture(Path.Combine(puchi._path, "Chara.png"));
			render = LoadTexture(Path.Combine(puchi._path, "Render.png"));
		}

		private static LuaTexture LoadTexture(string path) {
			if (!File.Exists(path)) return new LuaTexture();
			try {
				return new LuaTexture(OpenTaiko.tTextureCreate(path));
			} catch {
				return new LuaTexture();
			}
		}

		/// <summary>Returns the human-readable unlock condition message.</summary>
		public string GetUnlockMessage() =>
			UnlockCondition.GetConditionMessage();

		// ────────────────────────────────────────────────────────────────────
		// IDisposable
		// ────────────────────────────────────────────────────────────────────

		private bool _disposed;
		public void Dispose() {
			if (_disposed) return;
			_disposed = true;
			tx.Dispose();
			render.Dispose();
		}
	}
}
