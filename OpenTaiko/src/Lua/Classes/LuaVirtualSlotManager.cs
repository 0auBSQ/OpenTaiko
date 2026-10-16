namespace OpenTaiko;

/// <summary>
/// Lua-facing API for the five generic virtual character slots (V1-V5).
/// The AI slot is managed internally by the game engine; it is not exposed here.
///
/// Exposed as the global <c>VIRTUALSLOTS</c> inside Lua stages.
/// </summary>
public class LuaVirtualSlotManager {
	// ── V-slot data getters/setters ────────────────────────────────────────────

	/// <summary>Gets the character folder name for slot V<paramref name="slot"/> (1-5).</summary>
	public string GetCharacter(int slot) {
		var v = GetV(slot);
		return v?.CharacterFolderName ?? "None";
	}

	/// <summary>Sets the character folder name for slot V<paramref name="slot"/> (1-5).</summary>
	public void SetCharacter(int slot, string folderName) {
		var v = GetV(slot);
		if (v != null) v.CharacterFolderName = folderName ?? "None";
	}

	/// <summary>Gets the puchichara folder name for slot V<paramref name="slot"/> (1-5).</summary>
	public string GetPuchichara(int slot) {
		var v = GetV(slot);
		return v?.PuchicharaFolderName ?? "None";
	}

	/// <summary>Sets the puchichara folder name for slot V<paramref name="slot"/> (1-5).</summary>
	public void SetPuchichara(int slot, string folderName) {
		var v = GetV(slot);
		if (v != null) v.PuchicharaFolderName = folderName ?? "None";
	}

	/// <summary>Gets the nameplate player name for slot V<paramref name="slot"/> (1-5).</summary>
	public string GetNameplateName(int slot) {
		var v = GetV(slot);
		return v?.NameplatePlayerName ?? "VSlot";
	}

	/// <summary>Sets the nameplate player name for slot V<paramref name="slot"/> (1-5).</summary>
	public void SetNameplateName(int slot, string name) {
		var v = GetV(slot);
		if (v != null) v.NameplatePlayerName = name ?? "VSlot";
	}

	/// <summary>Gets the nameplate title text for slot V<paramref name="slot"/> (1-5).</summary>
	public string GetNameplateTitle(int slot) {
		var v = GetV(slot);
		return v?.NameplateTitle ?? "";
	}

	/// <summary>Sets the nameplate title text for slot V<paramref name="slot"/> (1-5).</summary>
	public void SetNameplateTitle(int slot, string title) {
		var v = GetV(slot);
		if (v != null) v.NameplateTitle = title ?? "";
	}

	/// <summary>Gets the nameplate dan text for slot V<paramref name="slot"/> (1-5).</summary>
	public string GetNameplateDan(int slot) {
		var v = GetV(slot);
		return v?.NameplateDan ?? "";
	}

	/// <summary>Sets the nameplate dan text for slot V<paramref name="slot"/> (1-5).</summary>
	public void SetNameplateDan(int slot, string dan) {
		var v = GetV(slot);
		if (v != null) v.NameplateDan = dan ?? "";
	}

	// ── Nameplate styling (type / rarity / dan plate) ───────────────────────────

	/// <summary>
	/// Applies a catalogue nameplate to slot V<paramref name="slot"/> (1-5): sets the title text, type and
	/// rarity from the nameplate database. The id is a string, or a whole number for the shipped ones.
	/// Used to mirror a remote player's nameplate in online play, where only the id crosses the wire.
	/// An empty id, or one unknown on this machine, shows the plate without a title.
	/// </summary>
	public void SetNameplateById(int slot, object nameplateId) {
		var v = GetV(slot);
		if (v == null) return;
		v.VirtualData.TitleId = DBNameplateUnlockables.tKey(nameplateId);
		OpenTaiko.Databases?.DBNameplateUnlockables?.tResolveNameplate(v.VirtualData);
		v.NameplateTitle = v.VirtualData.Title;
	}

	/// <summary>Sets the nameplate title TYPE (style index) for slot V<paramref name="slot"/> (1-5) directly.</summary>
	public void SetNameplateType(int slot, int type) {
		var v = GetV(slot);
		if (v != null) v.VirtualData.TitleType = type;
	}

	/// <summary>Sets the nameplate title RARITY (lang int) for slot V<paramref name="slot"/> (1-5) directly.</summary>
	public void SetNameplateRarity(int slot, int rarity) {
		var v = GetV(slot);
		if (v != null) v.VirtualData.TitleRarityInt = rarity;
	}

	/// <summary>Sets the dan-plate TYPE for slot V<paramref name="slot"/> (1-5).</summary>
	public void SetNameplateDanType(int slot, int danType) {
		var v = GetV(slot);
		if (v != null) v.VirtualData.DanType = danType;
	}

	/// <summary>Sets whether the dan plate is rendered gold for slot V<paramref name="slot"/> (1-5).</summary>
	public void SetNameplateDanGold(int slot, bool gold) {
		var v = GetV(slot);
		if (v != null) v.VirtualData.DanGold = gold;
	}

	// ── Mount / unmount ───────────────────────────────────────────────────────

	/// <summary>
	/// Redirects player spot <paramref name="playerSpot"/> (1-5) to display the visual data
	/// from <paramref name="slotInfo"/>:
	/// <list type="bullet">
	///   <item><c>"1P"–"5P"</c> – use that player's save file.</item>
	///   <item><c>"AI"</c>      – use the AI virtual slot.</item>
	///   <item><c>"V1"–"V5"</c> – use virtual slot V[0]–V[4].</item>
	/// </list>
	/// Nothing is written to disk; the override lasts until MountSlot is called again.
	/// </summary>
	public void MountSlot(int playerSpot, string slotInfo) {
		CVirtualSlotManager.MountSlot(playerSpot, slotInfo);
	}

	// ── Helper ────────────────────────────────────────────────────────────────

	private static CVirtualSlotData? GetV(int slot) {
		if (slot < 1 || slot > 5) return null;
		return CVirtualSlotManager.V[slot - 1];
	}
}
