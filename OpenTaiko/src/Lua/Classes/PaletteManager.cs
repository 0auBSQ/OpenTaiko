using FDK;
using Newtonsoft.Json.Linq;
using NLua;

namespace OpenTaiko;

/// <summary>A cached gradient map paired with its blend strength.</summary>
public sealed class PaletteGradientEntry {
	public CGradientMap  Map    { get; }
	public float         Blend  { get; }
	public LuaGradientMap LuaMap { get; }
	internal PaletteGradientEntry(CGradientMap map, float blend) {
		Map    = map;
		Blend  = blend;
		LuaMap = new LuaGradientMap(map, null) { BlendStrength = blend };
	}
}

/// <summary>
/// Global gradient cache (lives for the whole program) and per-player-slot active palette.
/// All gradients — whether created via <c>GRADIENT:Create</c> or <c>character:SetPaletteGradient</c>
/// — go through this cache so the same stops+blend always reuses the same GPU texture.
/// Key format: <c>blend:2dp|pos%:2dp;r;g;b[;a]|...</c>
/// </summary>
public static class PaletteManager {
	private static readonly Dictionary<string, PaletteGradientEntry> _cache = new();
	private static readonly PaletteGradientEntry?[] _slots = new PaletteGradientEntry?[5];

	/// <summary>
	/// Parses a Lua stops table into the internal stop list format.
	/// Each entry in the table must be a sub-table <c>{ position, r, g, b [, a] }</c>.
	/// </summary>
	public static List<(float Pos, float R, float G, float B, float A)> ParseLuaStops(LuaTable stops) {
		var list = new List<(float Pos, float R, float G, float B, float A)>();
		foreach (var key in stops.Keys) {
			if (stops[key] is not LuaTable inner) continue;
			list.Add((
				Convert.ToSingle(inner[1]),
				Convert.ToSingle(inner[2]) / 255f,
				Convert.ToSingle(inner[3]) / 255f,
				Convert.ToSingle(inner[4]) / 255f,
				inner[5] != null ? Convert.ToSingle(inner[5]) / 255f : 1f));
		}
		return list;
	}

	/// <summary>
	/// Builds the cache key string for the given stops and blend.
	/// Format: <c>blend:2dp|pos%:2dp;r;g;b[;a]|...</c>
	/// </summary>
	public static string BuildCacheKey(
			List<(float Pos, float R, float G, float B, float A)> stops, float blend) {
		var sb = new System.Text.StringBuilder();
		sb.Append(blend.ToString("F2"));
		foreach (var (pos, r, g, b, a) in stops) {
			sb.Append('|');
			sb.Append((pos * 100f).ToString("F2"));
			sb.Append(';'); sb.Append((int)Math.Round(r * 255));
			sb.Append(';'); sb.Append((int)Math.Round(g * 255));
			sb.Append(';'); sb.Append((int)Math.Round(b * 255));
			if (a < 0.999f) { sb.Append(';'); sb.Append((int)Math.Round(a * 255)); }
		}
		return sb.ToString();
	}

	/// <summary>
	/// Returns a cached entry for <paramref name="key"/>, creating and caching it first if absent.
	/// Returns null when fewer than 2 stops are provided.
	/// </summary>
	public static PaletteGradientEntry? GetOrCreate(
			string key,
			List<(float Pos, float R, float G, float B, float A)> stops,
			float blend) {
		if (_cache.TryGetValue(key, out var existing)) return existing;
		if (stops.Count < 2) return null;
		var entry = new PaletteGradientEntry(new CGradientMap(stops), Math.Clamp(blend, 0f, 1f));
		_cache[key] = entry;
		return entry;
	}

	public static void   SetSlot(int slot, PaletteGradientEntry? entry) { if (slot is >= 0 and < 5) _slots[slot] = entry; }
	public static PaletteGradientEntry? GetSlot(int slot)               => slot is >= 0 and < 5 ? _slots[slot] : null;

	/// <summary>
	/// Reads the saved palette index from the player's save file, parses the character's
	/// Palettes.json, and sets the palette slot. Call this before constructing
	/// <see cref="TextureLoader.PlayerCharacters"/>[player] so the constructor picks it up.
	/// </summary>
	public static void RestoreFromSave(int player) {
		if (player is < 0 or >= 5) return;
		var sf = OpenTaiko.SaveFileInstances?[player];
		if (sf == null) return;

		var chars = OpenTaiko.Tx?.Characters;
		if (chars == null || chars.Length == 0) return;
		int charaIdx = Math.Clamp(sf.data.Character, 0, chars.Length - 1);
		var chara = chars[charaIdx];
		if (chara == null || string.IsNullOrEmpty(chara.dirName)) return;

		long savedIdx = (long)sf.tGetGlobalCounter(".character_palette_" + chara.dirName);
		if (savedIdx <= 0) return;

		string palettesPath = Path.Combine(chara._path, "Palettes.json");
		if (!File.Exists(palettesPath)) return;

		try {
			if (JToken.Parse(File.ReadAllText(palettesPath)) is not JArray arr) return;
			if (savedIdx >= arr.Count) return;

			var entryToken = arr[(int)savedIdx];
			float blend    = entryToken["blend"]?.ToObject<float>() ?? 0f;
			if (entryToken["stops"] is not JArray stopsArr || stopsArr.Count < 2) return;

			var list = new List<(float Pos, float R, float G, float B, float A)>();
			foreach (JToken s in stopsArr) {
				if (s is not JArray sa || sa.Count < 4) continue;
				list.Add((
					sa[0].ToObject<float>(),
					sa[1].ToObject<float>() / 255f,
					sa[2].ToObject<float>() / 255f,
					sa[3].ToObject<float>() / 255f,
					sa.Count >= 5 ? sa[4].ToObject<float>() / 255f : 1f));
			}
			if (list.Count < 2) return;

			var entry = GetOrCreate(BuildCacheKey(list, blend), list, blend);
			if (entry != null) SetSlot(player, entry);
		} catch { }
	}
}
