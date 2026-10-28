namespace OpenTaiko;

class Databases {
	public void tDatabases() {
		DBCDN = new DBCDN();
		DBNameplateUnlockables = new DBNameplateUnlockables();
		DBSongUnlockables = new DBSongUnlockables();
	}

	/// <summary>
	/// (Re)loads ThemeSettings.json, ThemeSettings.db3, and the skin locale manager for the current skin folder.
	/// Called from TextureLoader after the skin path is resolved.
	/// </summary>
	public void LoadThemeSettings() {
		DBThemeSettings?.Dispose();
		DBThemeSettings = new DBThemeSettings();
		// CSkin.Path("") gives the resolved skin folder path (with trailing separator).
		string skinFolder = CSkin.Path("");
		DBThemeSettings.Load(skinFolder);
		SkinLocaleManager = new CSkinLocaleManager(skinFolder, OpenTaiko.Skin.Skin_DefaultLocale);
	}

	public DBCDN DBCDN = null!;
	public DBNameplateUnlockables DBNameplateUnlockables = null!;
	public DBSongUnlockables DBSongUnlockables = null!;
	public DBThemeSettings? DBThemeSettings;
	public CSkinLocaleManager? SkinLocaleManager;
}
