using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenTaiko;

/// <summary>
/// Manages skin-scoped locale files loaded from {skinFolder}/Locales/{langId}.json.
///
/// Lookup priority:
///   1. File for the currently active game language.
///   2. File for the skin's configured default locale (DefaultLocale in SkinConfig.ini, defaults to "en").
///   3. A "[LOCALE NOT FOUND: {key}]" error string if neither file exists or contains the key.
/// </summary>
internal class CSkinLocaleManager {
	// ── Construction / loading ────────────────────────────────────────────────

	/// <param name="skinFolder">Absolute path to the skin folder (trailing separator optional).</param>
	/// <param name="defaultLocale">Locale id to fall back to (from SkinConfig.ini DefaultLocale).</param>
	public CSkinLocaleManager(string skinFolder, string defaultLocale) {
		_skinFolder = skinFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		_defaultLocale = string.IsNullOrWhiteSpace(defaultLocale) ? "en" : defaultLocale;
	}

	// ── Public API ───────────────────────────────────────────────────────────

	/// <summary>
	/// Returns the localized string for <paramref name="key"/>, following the fallback chain described above.
	/// </summary>
	public string GetString(string key) {
		string currentLang = CLangManager.fetchLang();

		// 1. Current language
		var entries = LoadOrGet(currentLang);
		if (entries != null && entries.TryGetValue(key, out var val))
			return val;

		// 2. Skin default locale (skip if same as current)
		if (!string.Equals(currentLang, _defaultLocale, StringComparison.OrdinalIgnoreCase)) {
			entries = LoadOrGet(_defaultLocale);
			if (entries != null && entries.TryGetValue(key, out val))
				return val;
		}

		// 3. Not found
		return $"[LOCALE NOT FOUND: {key}]";
	}

	// ── Private ───────────────────────────────────────────────────────────────

	private readonly string _skinFolder;
	private readonly string _defaultLocale;
	// null value = file does not exist (cached miss); non-null = loaded entries.
	private readonly Dictionary<string, Dictionary<string, string>?> _cache = new(StringComparer.OrdinalIgnoreCase);

	private Dictionary<string, string>? LoadOrGet(string langId) {
		if (_cache.TryGetValue(langId, out var cached))
			return cached;

		string path = System.IO.Path.Combine(_skinFolder, "Locales", $"{langId}.json");
		if (!File.Exists(path)) {
			_cache[langId] = null;
			return null;
		}

		try {
			JsonNodeOptions nodeOpts = new() { PropertyNameCaseInsensitive = false };
			JsonDocumentOptions docOpts = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
			JsonNode? node = JsonNode.Parse(File.ReadAllText(path), nodeOpts, docOpts);
			var entries = node?["Entries"]?.Deserialize<Dictionary<string, string>>() ?? new();
			_cache[langId] = entries;
			return entries;
		} catch (Exception ex) {
			Trace.TraceWarning($"CSkinLocaleManager: failed to load '{path}': {ex.Message}");
			_cache[langId] = null;
			return null;
		}
	}
}
