using Newtonsoft.Json;

namespace OpenTaiko;

/// <summary>
/// Represents a single skin-specific setting definition loaded from ThemeSettings.json.
/// </summary>
internal class CThemeSettingDef {
	/// <summary>Unique identifier used as the key in the DB and in Lua.</summary>
	[JsonProperty("id")]
	public string Id { get; set; } = "";

	/// <summary>
	/// Type of setting value: "bool", "int", "double", "string", "enum", or "key" (a keyboard key name;
	/// the settings menu captures it, skins read it and match it with INPUT:KeyboardPressed).
	/// </summary>
	[JsonProperty("type")]
	public string Type { get; set; } = "string";

	/// <summary>
	/// Scope: "global" (one value for the whole game) or "save" (one value per SaveId).
	/// Defaults to "global".
	/// </summary>
	[JsonProperty("scope")]
	public string Scope { get; set; } = "global";

	/// <summary>Display label shown in the config menu (localized).</summary>
	[JsonProperty("label")]
	public CLocalizationData Label { get; set; } = new();

	/// <summary>Description shown in the config menu description panel (localized).</summary>
	[JsonProperty("description")]
	public CLocalizationData Description { get; set; } = new();

	/// <summary>
	/// Optional sub-section the setting is listed under on the Theme page: the id of an entry of the file's
	/// "sections" map (a localized header declared once), or a plain header text. Settings sharing a section
	/// are listed together, in the order of their first appearance. Empty = the page's default header.
	/// </summary>
	[JsonProperty("section")]
	public string Section { get; set; } = "";

	/// <summary>Default value serialized as a string (parsed according to Type).</summary>
	[JsonProperty("default")]
	public string Default { get; set; } = "";

	/// <summary>Minimum value for int/double types.</summary>
	[JsonProperty("min")]
	public double Min { get; set; } = 0;

	/// <summary>Maximum value for int/double types.</summary>
	[JsonProperty("max")]
	public double Max { get; set; } = 100;

	/// <summary>Ordered list of option strings for enum type.</summary>
	[JsonProperty("options")]
	public string[] Options { get; set; } = [];

	/// <summary>Step of the settings menu's left/right for int types (1 when absent).</summary>
	[JsonProperty("step")]
	public int Step { get; set; } = 1;

	/// <summary>
	/// A session setting lives in memory only: it starts at its default on every launch and is never
	/// written to ThemeSettings.db3 (an event mode switch, a counter for the session).
	/// </summary>
	[JsonProperty("session")]
	public bool Session { get; set; } = false;

	/// <summary>A hidden setting is not listed in the settings menu; skins read and write it themselves.</summary>
	[JsonProperty("hidden")]
	public bool Hidden { get; set; } = false;

	// ── Helpers ──────────────────────────────────────────────────────────

	public bool IsSaveScoped => string.Equals(Scope, "save", StringComparison.OrdinalIgnoreCase);

	public int DefaultInt => int.TryParse(Default, out int v) ? v : 0;
	public double DefaultDouble => double.TryParse(Default, System.Globalization.NumberStyles.Any,
		System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : 0.0;
	public bool DefaultBool => Default == "1" || string.Equals(Default, "true", StringComparison.OrdinalIgnoreCase);
	public int DefaultEnumIndex => Math.Max(0, Array.IndexOf(Options, Default));
}
