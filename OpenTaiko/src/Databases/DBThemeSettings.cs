using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace OpenTaiko;

/// <summary>
/// Manages ThemeSettings.db3 — a per-skin SQLite database that stores the runtime
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

	// ── Private state ─────────────────────────────────────────────────────

	private List<CThemeSettingDef> _defs = [];
	private SqliteConnection? _conn;
	private bool _disposed;

	// ── Lifecycle ─────────────────────────────────────────────────────────

	/// <summary>
	/// Loads ThemeSettings.json from <paramref name="skinFolderPath"/> and opens/creates
	/// ThemeSettings.db3 in the same folder.  Initialises any missing global rows with defaults.
	/// Safe to call if the JSON file does not exist — definitions will be empty.
	/// </summary>
	public void Load(string skinFolderPath) {
		_defs = LoadDefinitions(skinFolderPath);

		string dbPath = System.IO.Path.Combine(skinFolderPath, "ThemeSettings.db3");
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
		if (def == null || _conn == null) return def?.Default ?? "";

		var cmd = _conn.CreateCommand();
		cmd.CommandText = "SELECT Value FROM save_settings WHERE SettingId = $id AND SaveId = $sid;";
		cmd.Parameters.AddWithValue("$id", settingId);
		cmd.Parameters.AddWithValue("$sid", saveId);
		var result = cmd.ExecuteScalar();
		return result is string s ? s : def.Default;
	}

	/// <summary>Persists a value for a global setting.</summary>
	public void SetSetting(string settingId, string value) {
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
		if (_conn == null) return;
		var cmd = _conn.CreateCommand();
		cmd.CommandText = @"INSERT INTO save_settings(SettingId, SaveId, Value) VALUES($id, $sid, $val)
			ON CONFLICT(SettingId, SaveId) DO UPDATE SET Value = EXCLUDED.Value;";
		cmd.Parameters.AddWithValue("$id", settingId);
		cmd.Parameters.AddWithValue("$sid", saveId);
		cmd.Parameters.AddWithValue("$val", value);
		cmd.ExecuteNonQuery();
	}

	// ── Private helpers ───────────────────────────────────────────────────

	private static List<CThemeSettingDef> LoadDefinitions(string skinFolderPath) {
		string jsonPath = System.IO.Path.Combine(skinFolderPath, "ThemeSettings.json");
		if (!File.Exists(jsonPath)) return [];
		try {
			string json = File.ReadAllText(jsonPath);
			return JsonConvert.DeserializeObject<List<CThemeSettingDef>>(json) ?? [];
		} catch (Exception ex) {
			Trace.TraceError($"[DBThemeSettings] Failed to parse ThemeSettings.json: {ex}");
			return [];
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
