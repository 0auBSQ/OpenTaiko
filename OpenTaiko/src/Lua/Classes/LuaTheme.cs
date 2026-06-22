namespace OpenTaiko {
	public class LuaThemeFunc {
		public LuaVector2 GetResolution() {
			return new LuaVector2(OpenTaiko.Skin.Resolution[0], OpenTaiko.Skin.Resolution[1]);
		}

		/// <summary>
		/// Returns the current value of a global theme setting as a string.
		/// Returns an empty string if the setting or DB is not available.
		/// </summary>
		public string GetThemeSetting(string settingId) {
			return OpenTaiko.Databases?.DBThemeSettings?.GetSetting(settingId) ?? "";
		}

		/// <summary>
		/// Returns the current value of a save-scoped theme setting for the given player.
		/// <paramref name="player"/> is 1-based (matching the Lua convention used elsewhere).
		/// Looks up the SaveId from the player's loaded save file.
		/// Returns the setting's default value if no row exists for that SaveId.
		/// </summary>
		public string GetThemeSettingForPlayer(string settingId, int player) {
			int slot = Math.Max(0, Math.Min(player - 1, OpenTaiko.SaveFileInstances.Length - 1));
			long saveId = OpenTaiko.SaveFileInstances[slot]?.data?.SaveId ?? 0L;
			return OpenTaiko.Databases?.DBThemeSettings?.GetSettingForSave(settingId, saveId) ?? "";
		}

		// ── Skin-scoped localization ──────────────────────────────────────────────

		/// <summary>
		/// Returns the localized string for <paramref name="key"/> from the skin's Locales folder.
		/// Falls back to the skin's DefaultLocale (SkinConfig.ini), then to "[LOCALE NOT FOUND: key]".
		/// </summary>
		public string GetSkinString(string key) =>
			OpenTaiko.Databases?.SkinLocaleManager?.GetString(key)
				?? $"[LOCALE NOT FOUND: {key}]";

		// ── Definition enumeration (for Lua stages that want to list all settings) ──

		/// <summary>Number of setting definitions loaded from ThemeSettings.json.</summary>
		public int GetDefinitionCount() =>
			OpenTaiko.Databases?.DBThemeSettings?.Definitions.Count ?? 0;

		/// <summary>
		/// Returns the Id of the definition at the given 0-based index.
		/// Returns an empty string for out-of-range indices.
		/// </summary>
		public string GetDefinitionId(int index) {
			var defs = OpenTaiko.Databases?.DBThemeSettings?.Definitions;
			if (defs == null || index < 0 || index >= defs.Count) return "";
			return defs[index].Id;
		}

		/// <summary>Returns the scope ("global" or "save") of the definition at the given 0-based index.</summary>
		public string GetDefinitionScope(int index) {
			var defs = OpenTaiko.Databases?.DBThemeSettings?.Definitions;
			if (defs == null || index < 0 || index >= defs.Count) return "";
			return defs[index].Scope;
		}

		/// <summary>Returns the type ("bool", "int", "double", "string", "enum") of the definition at the given 0-based index.</summary>
		public string GetDefinitionType(int index) {
			var defs = OpenTaiko.Databases?.DBThemeSettings?.Definitions;
			if (defs == null || index < 0 || index >= defs.Count) return "";
			return defs[index].Type;
		}
	}
}
