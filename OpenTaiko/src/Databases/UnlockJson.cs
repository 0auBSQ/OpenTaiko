using Newtonsoft.Json;

namespace OpenTaiko;

/// <summary>
/// Deserialised shape of an Unlock.json file.
/// Place an Unlock.json alongside any supported asset (charts, characters, …)
/// to define its unlock condition without touching the central databases.
/// </summary>
internal class UnlockJson {
	[JsonProperty("hidden_index")]
	public int HiddenIndex = 0;

	[JsonProperty("rarity")]
	public string Rarity = "Common";

	[JsonProperty("condition")]
	public string Condition = "ch";

	[JsonProperty("values")]
	public int[] Values = new int[] { 100 };

	[JsonProperty("type")]
	public string Type = "me";

	[JsonProperty("references")]
	public string[] References = new string[] { "" };

	[JsonProperty("custom_unlock_text")]
	public CLocalizationData? CustomUnlockText = null;
}
