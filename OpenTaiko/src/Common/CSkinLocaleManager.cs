using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenTaiko;

/// <summary>
/// Manages skin-scoped locale files: every {skinFolder}/Locales/{langId}/*.json (one file per screen
/// or feature), and {skinFolder}/Locales/{langId}.json when a skin keeps a single file.
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

		var files = new List<string>();
		string single = System.IO.Path.Combine(_skinFolder, "Locales", $"{langId}.json");
		if (File.Exists(single)) files.Add(single);
		string folder = System.IO.Path.Combine(_skinFolder, "Locales", langId);
		if (Directory.Exists(folder)) {
			var inFolder = Directory.GetFiles(folder, "*.json");
			Array.Sort(inFolder, StringComparer.OrdinalIgnoreCase);
			files.AddRange(inFolder);
		}
		if (files.Count == 0) {
			_cache[langId] = null;
			return null;
		}

		// every file adds its entries; a key defined twice keeps the last file's value (files in name order)
		var entries = new Dictionary<string, string>();
		foreach (string path in files) {
			try {
				JsonNodeOptions nodeOpts = new() { PropertyNameCaseInsensitive = false };
				JsonDocumentOptions docOpts = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
				JsonNode? node = JsonNode.Parse(File.ReadAllText(path), nodeOpts, docOpts);
				var part = node?["Entries"]?.Deserialize<Dictionary<string, string>>();
				if (part == null) continue;
				foreach (var kv in part) {
					if (entries.ContainsKey(kv.Key))
						Trace.TraceWarning($"CSkinLocaleManager: '{kv.Key}' is defined again in '{path}'");
					entries[kv.Key] = kv.Value;
				}
			} catch (Exception ex) {
				Trace.TraceWarning($"CSkinLocaleManager: failed to load '{path}': {ex.Message}");
			}
		}
		_cache[langId] = entries;
		return entries;
	}
}
