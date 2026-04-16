namespace OpenTaiko;

/// <summary>
/// Data for a single virtual character slot (AI or V1-V5).
/// None of this is persisted to disk — it is all runtime state.
/// </summary>
public class CVirtualSlotData {
	public string CharacterFolderName  = "None";
	public string PuchicharaFolderName = "None";
	/// <summary>Player name shown on the nameplate.</summary>
	public string NameplatePlayerName  = "VSlot";
	/// <summary>Title text shown on the nameplate.</summary>
	public string NameplateTitle       = "";
	/// <summary>Dan text shown on the nameplate.</summary>
	public string NameplateDan         = "";

	/// <summary>
	/// Permanent save-data-like object for this virtual slot, passed to the nameplate
	/// Lua script's Activate call.  Because Lua stores the reference, the data must
	/// remain alive and be updated in-place (never replaced).
	/// </summary>
	internal readonly SaveFile.Data VirtualData = new SaveFile.Data();
}

/// <summary>
/// Manages the six virtual character slots and per-player-spot mount overrides.
///
/// Slots:
///   AI   – the AI battle opponent, character driven by the skin's "aislot_chara" setting.
///   V[0] – V[4]  (V1-V5) – five generic slots for skinners to use (e.g. story mode).
///
/// MountSlot(playerSpot, slotInfo) redirects what character/puchi/nameplate data is
/// shown for a given in-game player spot during gameplay and the results screen.
/// No save file is written; everything reverts when MountSlot is called again.
/// </summary>
public static class CVirtualSlotManager {
	// ── The six virtual slots ─────────────────────────────────────────────────

	public static CVirtualSlotData AI = new();
	/// <summary>V1-V5, index 0-4.</summary>
	public static CVirtualSlotData[] V = new CVirtualSlotData[5];

	// ── Per-player-spot mount override ────────────────────────────────────────
	// _mounts[0..4] corresponds to player spots 1-5.
	// null   = use that player's own save file (default).
	// "NP"   = use save file N (1-based, e.g. "1P", "3P").
	// "AI"   = use the AI virtual slot.
	// "V1"-"V5" = use virtual slot V[0..4].
	private static readonly string?[] _mounts = new string?[5];

	// ── Initialisation ────────────────────────────────────────────────────────

	public static void Initialize() {
		for (int i = 0; i < 5; i++)
			V[i] = new CVirtualSlotData();

		AI = new CVirtualSlotData();
		RefreshAICharacter();
		RefreshAINameplate();

		// Clear all mounts
		for (int i = 0; i < 5; i++)
			_mounts[i] = null;
	}

	/// <summary>
	/// Re-reads the "aislot_chara" theme setting and updates the AI slot's character.
	/// Call after theme settings are (re)loaded.
	/// </summary>
	public static void RefreshAICharacter() {
		string charaName = OpenTaiko.Databases?.DBThemeSettings?.GetSetting("aislot_chara") ?? "None";
		AI.CharacterFolderName = charaName;
	}

	/// <summary>
	/// Populates the AI slot's <see cref="CVirtualSlotData.VirtualData"/> with nameplate #66
	/// visuals (title text, type, rarity).  Call after the nameplate database is loaded.
	/// </summary>
	public static void RefreshAINameplate() {
		var npDb = OpenTaiko.Databases?.DBNameplateUnlockables?.data;
		if (npDb != null && npDb.TryGetValue(66L, out var np66)) {
			AI.VirtualData.Title         = np66.nameplateInfo.cld.GetString("");
			AI.VirtualData.TitleId       = 66;
			AI.VirtualData.TitleType     = np66.nameplateInfo.iType;
			AI.VirtualData.TitleRarityInt = HRarity.tRarityToLangInt(np66.rarity);
		}
	}

	// ── Mount / unmount ───────────────────────────────────────────────────────

	/// <summary>
	/// Redirects player spot <paramref name="playerSpot"/> (1-5) to use the visual data
	/// from <paramref name="slotInfo"/>:
	/// <list type="bullet">
	///   <item><c>"1P"–"5P"</c> – use that player's save file.</item>
	///   <item><c>"AI"</c>      – use the AI virtual slot.</item>
	///   <item><c>"V1"–"V5"</c> – use virtual slot V[0]–V[4].</item>
	/// </list>
	/// Passing the player's own save file code (e.g. <c>"2P"</c> for spot 2) is
	/// equivalent to clearing the override.
	/// </summary>
	public static void MountSlot(int playerSpot, string slotInfo) {
		if (playerSpot < 1 || playerSpot > 5) return;
		int player = playerSpot - 1;

		// Record old character index before applying the new mount.
		int oldCharIdx = GetCharacterIndex(player);

		_mounts[player] = slotInfo;

		// If the effective character changed, swap the loaded animations for this player
		// slot so the new character is actually rendered.  We do NOT touch any save-file
		// data (mountedCharacter, data.Character, etc.) — virtual slots are self-contained.
		int newCharIdx = GetCharacterIndex(player);
		if (oldCharIdx != newCharIdx) {
			OpenTaiko.Tx?.SwapCharacterAnimations(oldCharIdx, newCharIdx, player);
		}

		// Refresh the nameplate for this spot so it picks up the new data.
		OpenTaiko.NamePlate?.tNamePlateRefreshTitles(player);
	}

	/// <summary>Returns the raw mount string for <paramref name="player"/> (0-based), or <c>null</c> if unmounted.</summary>
	public static string? GetMount(int player) =>
		(player >= 0 && player < 5) ? _mounts[player] : null;

	// ── Accessors used by the game engine ─────────────────────────────────────

	/// <summary>
	/// Returns the <c>Tx.Characters</c> index to use for <paramref name="player"/> (0-based).
	/// </summary>
	public static int GetCharacterIndex(int player) {
		string? mount = _mounts[player];

		if (mount == null)
			return OpenTaiko.SaveFileInstances[player].data.Character;

		if (TryParseSaveSlot(mount, out int srcPlayer))
			return OpenTaiko.SaveFileInstances[srcPlayer].data.Character;

		CVirtualSlotData? vdata = GetVirtualData(mount);
		return vdata != null
			? FindCharacterIndexByName(vdata.CharacterFolderName)
			: OpenTaiko.SaveFileInstances[player].data.Character;
	}

	/// <summary>
	/// Returns the puchichara folder name to use for <paramref name="player"/> (0-based).
	/// </summary>
	public static string GetPuchicharaName(int player) {
		string? mount = _mounts[player];

		if (mount == null)
			return OpenTaiko.SaveFileInstances[player].data.PuchiChara;

		if (TryParseSaveSlot(mount, out int srcPlayer))
			return OpenTaiko.SaveFileInstances[srcPlayer].data.PuchiChara;

		CVirtualSlotData? vdata = GetVirtualData(mount);
		return vdata?.PuchicharaFolderName ?? OpenTaiko.SaveFileInstances[player].data.PuchiChara;
	}

	/// <summary>
	/// Returns nameplate (name, title, dan) override for <paramref name="player"/> (0-based),
	/// or <c>null</c> to fall back to the save file.
	/// </summary>
	public static (string name, string title, string dan)? GetNameplateOverride(int player) {
		string? mount = _mounts[player];
		if (mount == null) return null;

		if (TryParseSaveSlot(mount, out int srcPlayer)) {
			// Only create an override when redirecting to a *different* player's save file.
			if (srcPlayer == player) return null;
			var d = OpenTaiko.SaveFileInstances[srcPlayer].data;
			return (d.Name, d.Title, d.Dan);
		}

		CVirtualSlotData? vdata = GetVirtualData(mount);
		if (vdata == null) return null;
		return (vdata.NameplatePlayerName, vdata.NameplateTitle, vdata.NameplateDan);
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	/// <summary>
	/// Returns the <c>Tx.Characters</c> index for a character identified by folder name.
	/// Returns 0 if not found.
	/// </summary>
	public static int FindCharacterIndexByName(string name) {
		var chars = OpenTaiko.Tx?.Characters;
		if (chars == null) return 0;
		for (int i = 0; i < chars.Length; i++) {
			if (string.Equals(chars[i].dirName, name, StringComparison.OrdinalIgnoreCase))
				return i;
		}
		return 0;
	}

	public static CVirtualSlotData? GetVirtualData(string slotInfo) => slotInfo switch {
		"AI" => AI,
		"V1" => V[0],
		"V2" => V[1],
		"V3" => V[2],
		"V4" => V[3],
		"V5" => V[4],
		_ => null
	};

	/// <summary>Parses "1P"–"5P" into a 0-based player index.</summary>
	private static bool TryParseSaveSlot(string slotInfo, out int playerIndex) {
		playerIndex = 0;
		if (slotInfo.Length == 2 && slotInfo[1] == 'P' && char.IsDigit(slotInfo[0])) {
			int n = slotInfo[0] - '0';
			if (n >= 1 && n <= 5) {
				playerIndex = n - 1;
				return true;
			}
		}
		return false;
	}
}
