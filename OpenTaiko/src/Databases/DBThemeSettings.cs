using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace OpenTaiko;

/// <summary>
/// Manages SkinSettings_[skin].db3 — a per-skin SQLite database that stores the runtime
/// values of settings declared in ThemeSettings.json.
///
/// Global settings are keyed by SettingId only.
/// Save-scoped settings are keyed by (SettingId, SaveId) using the INT64 SaveId
/// from the player's save file — not a controller slot index.
///
/// Call <see cref="Load"/> once when the skin loads (passing the skin folder path).
/// After that, Lua and the config UI can use <see cref="GetSetting"/> /
/// <see cref="GetSettingForSave"/> and the corresponding Set variants.
/// </summary>
internal class DBThemeSettings : IDisposable {
	// ── Public state ─────────────────────────────────────────────────────

	/// <summary>Ordered list of setting definitions loaded from ThemeSettings.json.</summary>
	public IReadOnlyList<CThemeSettingDef> Definitions => _defs;

	/// <summary>The file's "sections" map: section id → localized header (empty for the plain array form).</summary>
	public IReadOnlyDictionary<string, CLocalizationData> Sections => _sections;

	/// <summary>The header text a definition is listed under: its section resolved through the map, the text
	/// itself when it is not a declared id, "" when it has none.</summary>
	public string SectionLabel(CThemeSettingDef def) {
		if (string.IsNullOrWhiteSpace(def.Section)) return "";
		return _sections.TryGetValue(def.Section, out var loc) ? loc.GetString(def.Section) : def.Section;
	}

	// ── Private state ─────────────────────────────────────────────────────

	private List<CThemeSettingDef> _defs = [];
	private Dictionary<string, CLocalizationData> _sections = new();
	private SqliteConnection? _conn;
	private bool _disposed;
	// the values of session settings, for this run only (keyed by id, or "id|saveId" when save-scoped)
	private readonly Dictionary<string, string> _session = new();

	// ── Lifecycle ─────────────────────────────────────────────────────────

	/// <summary>
	/// Loads ThemeSettings.json from <paramref name="skinFolderPath"/> and opens/creates the skin's settings
	/// database, Global/ApplicationData/SkinSettings_[skin folder name].db3 (outside the skin folder, which a skin
	/// update deletes). Initialises any missing global rows with defaults.
	/// Safe to call if the JSON file does not exist — definitions will be empty.
	/// </summary>
	public void Load(string skinFolderPath) {
		(_defs, _sections) = LoadDefinitions(skinFolderPath);

		string dbPath = OpenTaiko.ResolveWritePath(System.IO.Path.GetFullPath(
			DataPath.GetAbsoluteDataPath(SettingsFileName(skinFolderPath))));
		System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
		MoveFromSkinFolder(skinFolderPath, dbPath);
		_conn = new SqliteConnection($"Data Source={dbPath}");
		_conn.Open();

		EnsureSchema();
		SeedGlobalDefaults();
	}

	public void Dispose() {
		if (_disposed) return;
		_disposed = true;
		_conn?.Close();
		_conn?.Dispose();
		_conn = null;
	}

	// ── Public API ────────────────────────────────────────────────────────

	/// <summary>Returns the stored value for a global setting, or the default if not found.</summary>
	public string GetSetting(string settingId) {
		var def = FindDef(settingId);
		if (def != null && def.Session) return _session.TryGetValue(settingId, out var sv) ? sv : def.Default;
		if (def == null || _conn == null) return def?.Default ?? "";

		var cmd = _conn.CreateCommand();
		cmd.CommandText = "SELECT Value FROM global_settings WHERE SettingId = $id;";
		cmd.Parameters.AddWithValue("$id", settingId);
		var result = cmd.ExecuteScalar();
		return result is string s ? s : def.Default;
	}

	/// <summary>
	/// Returns the stored value for a save-scoped setting, identified by the player's
	/// <paramref name="saveId"/> (from <c>SaveFile.data.SaveId</c>).
	/// Returns the setting's default value if no row exists yet.
	/// </summary>
	public string GetSettingForSave(string settingId, long saveId) {
		var def = FindDef(settingId);
		if (def != null && def.Session) return _session.TryGetValue($"{settingId}|{saveId}", out var sv) ? sv : def.Default;
		if (def == null || _conn == null) return def?.Default ?? "";

		var cmd = _conn.CreateCommand();
		cmd.CommandText = "SELECT Value FROM save_settings WHERE SettingId = $id AND SaveId = $sid;";
		cmd.Parameters.AddWithValue("$id", settingId);
		cmd.Parameters.AddWithValue("$sid", saveId);
		var result = cmd.ExecuteScalar();
		return result is string s ? s : def.Default;
	}

	/// <summary>Persists a value for a global setting (a session setting is only remembered for this run).</summary>
	public void SetSetting(string settingId, string value) {
		if (FindDef(settingId) is { Session: true }) { _session[settingId] = value; return; }
		if (_conn == null) return;
		var cmd = _conn.CreateCommand();
		cmd.CommandText = @"INSERT INTO global_settings(SettingId, Value) VALUES($id, $val)
			ON CONFLICT(SettingId) DO UPDATE SET Value = EXCLUDED.Value;";
		cmd.Parameters.AddWithValue("$id", settingId);
		cmd.Parameters.AddWithValue("$val", value);
		cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Persists a value for a save-scoped setting identified by <paramref name="saveId"/>.
	/// </summary>
	public void SetSettingForSave(string settingId, long saveId, string value) {
		if (FindDef(settingId) is { Session: true }) { _session[$"{settingId}|{saveId}"] = value; return; }
		if (_conn == null) return;
		var cmd = _conn.CreateCommand();
		cmd.CommandText = @"INSERT INTO save_settings(SettingId, SaveId, Value) VALUES($id, $sid, $val)
			ON CONFLICT(SettingId, SaveId) DO UPDATE SET Value = EXCLUDED.Value;";
		cmd.Parameters.AddWithValue("$id", settingId);
		cmd.Parameters.AddWithValue("$sid", saveId);
		cmd.Parameters.AddWithValue("$val", value);
		cmd.ExecuteNonQuery();
	}

	/// <summary>A stored global value that no definition declares (the engine's own theme settings), or null.</summary>
	public string? GetRaw(string settingId) {
		if (_conn == null) return null;
		var cmd = _conn.CreateCommand();
		cmd.CommandText = "SELECT Value FROM global_settings WHERE SettingId = $id;";
		cmd.Parameters.AddWithValue("$id", settingId);
		return cmd.ExecuteScalar() as string;
	}

	public void SetRaw(string settingId, string value) {
		if (_conn == null) return;
		var cmd = _conn.CreateCommand();
		cmd.CommandText = @"INSERT INTO global_settings(SettingId, Value) VALUES($id, $val)
			ON CONFLICT(SettingId) DO UPDATE SET Value = EXCLUDED.Value;";
		cmd.Parameters.AddWithValue("$id", settingId);
		cmd.Parameters.AddWithValue("$val", value);
		cmd.ExecuteNonQuery();
	}

	// ── Private helpers ───────────────────────────────────────────────────

	private const int MaxSkinNameLength = 64;
	private const string ForbiddenNameChars = "<>:\"/\\|?*%";

	/// <summary>SkinSettings_[skin folder name].db3: characters a file name can't hold become %XX (UTF-8), and a name
	/// over MaxSkinNameLength is cut and ends with 8 hex digits of a hash of the full name.</summary>
	internal static string SettingsFileName(string skinFolderPath) {
		string name = System.IO.Path.GetFileName(skinFolderPath.TrimEnd('/', '\\'));
		var sb = new StringBuilder();
		for (int i = 0; i < name.Length; i++) {
			char c = name[i];
			// Windows drops a trailing dot or space
			bool trailing = i == name.Length - 1 && (c == '.' || c == ' ');
			if (c < 0x20 || c == 0x7F || ForbiddenNameChars.IndexOf(c) >= 0 || trailing) {
				foreach (byte b in Encoding.UTF8.GetBytes(c.ToString())) sb.Append('%').Append(b.ToString("X2"));
			} else {
				sb.Append(c);
			}
		}
		string escaped = sb.ToString();
		if (escaped.Length > MaxSkinNameLength) {
			string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(name)))[..8];
			int cut = MaxSkinNameLength - hash.Length - 1;
			if (char.IsHighSurrogate(escaped[cut - 1])) cut--;
			escaped = escaped[..cut] + "_" + hash;
		}
		return $"SkinSettings_{escaped}.db3";
	}

	// moves the skin folder's old ThemeSettings.db3 (or iOS's writable copy of it) once
	private static void MoveFromSkinFolder(string skinFolderPath, string dbPath) {
		if (File.Exists(dbPath)) return;
		string old = System.IO.Path.Combine(skinFolderPath, "ThemeSettings.db3");
		string writableOld = OpenTaiko.ResolveWritePath(old);
		if (File.Exists(writableOld)) old = writableOld;
		if (!File.Exists(old)) return;
		try {
			File.Move(old, dbPath);
		} catch (Exception) {
			// read-only skin folder (iOS bundle) or a locked file: the values are copied
			try {
				File.Copy(old, dbPath);
			} catch (Exception ex) {
				Trace.TraceWarning($"[DBThemeSettings] Could not move {old} to {dbPath}: {ex.Message}");
			}
		}
	}

	// the file is either a plain array of definitions, or { "sections": { id: localized header }, "settings": [...] }
	private sealed class FileForm {
		[JsonProperty("sections")] public Dictionary<string, CLocalizationData>? Sections { get; set; }
		[JsonProperty("settings")] public List<CThemeSettingDef>? Settings { get; set; }
	}

	private static (List<CThemeSettingDef>, Dictionary<string, CLocalizationData>) LoadDefinitions(string skinFolderPath) {
		string jsonPath = System.IO.Path.Combine(skinFolderPath, "ThemeSettings.json");
		if (!File.Exists(jsonPath)) return ([], new());
		try {
			string json = File.ReadAllText(jsonPath);
			if (json.TrimStart('\uFEFF', ' ', '\t', '\r', '\n').StartsWith('['))
				return (JsonConvert.DeserializeObject<List<CThemeSettingDef>>(json) ?? [], new());
			var form = JsonConvert.DeserializeObject<FileForm>(json);
			return (form?.Settings ?? [], form?.Sections ?? new());
		} catch (Exception ex) {
			Trace.TraceError($"[DBThemeSettings] Failed to parse ThemeSettings.json: {ex}");
			return ([], new());
		}
	}

	private void EnsureSchema() {
		var cmd = _conn!.CreateCommand();
		cmd.CommandText = @"
			CREATE TABLE IF NOT EXISTS global_settings (
				SettingId TEXT PRIMARY KEY,
				Value     TEXT NOT NULL
			);
			CREATE TABLE IF NOT EXISTS save_settings (
				SettingId TEXT    NOT NULL,
				SaveId    INTEGER NOT NULL,
				Value     TEXT    NOT NULL,
				PRIMARY KEY (SettingId, SaveId)
			);";
		cmd.ExecuteNonQuery();
	}

	/// <summary>
	/// Seeds default rows for global settings only.
	/// Save-scoped rows are created on first write (SetSettingForSave) or returned as default
	/// on read (GetSettingForSave) — we don't know which SaveIds exist at load time.
	/// </summary>
	private void SeedGlobalDefaults() {
		foreach (var def in _defs) {
			if (def.IsSaveScoped) continue;
			var cmd = _conn!.CreateCommand();
			cmd.CommandText = @"INSERT OR IGNORE INTO global_settings(SettingId, Value)
				VALUES($id, $val);";
			cmd.Parameters.AddWithValue("$id", def.Id);
			cmd.Parameters.AddWithValue("$val", def.Default);
			cmd.ExecuteNonQuery();
		}
	}

	private CThemeSettingDef? FindDef(string id) =>
		_defs.Find(d => d.Id == id);
}
