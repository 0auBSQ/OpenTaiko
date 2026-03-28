using Newtonsoft.Json;

namespace OpenTaiko;

class DBPuchichara {
	public class PuchicharaEffect {
		public PuchicharaEffect() {
			AllPurple = false;
			Autoroll = 0;
			ShowAdlib = false;
			SplitLane = false;
		}

		public float GetCoinMultiplier(float multRarity, bool allPurpleEnabled = true, bool autorollEnabled = true, bool showAdlibEnabled = true, bool splitLaneEnabled = true) {
			if (autorollEnabled && Autoroll > 0) multRarity *= 0f;
			if (showAdlibEnabled && ShowAdlib == true) multRarity *= 0.9f;
			return multRarity;
		}

		[JsonProperty("allpurple")]
		public bool AllPurple;

		[JsonProperty("AutoRoll")]
		public int Autoroll;

		[JsonProperty("showadlib")]
		public bool ShowAdlib;

		[JsonProperty("splitlane")]
		public bool SplitLane;
	}

	public class PuchicharaData {
		public PuchicharaData() {
			Name = "(None)";
			Rarity = "Common";
			Author = "(None)";
		}

		public PuchicharaData(string pcn, string pcr, string pca) {
			Name = pcn;
			Rarity = pcr;
			Author = pca;
		}

		public string tGetName() {
			if (Name is string) return (string)Name;
			else if (Name is CLocalizationData) return ((CLocalizationData)Name).GetString("");
			return "";
		}

		public string tGetAuthor() {
			if (Author is string) return (string)Author;
			else if (Author is CLocalizationData) return ((CLocalizationData)Author).GetString("");
			return "";
		}

		public string tGetDescription() {
			if (Description is string) return (string)Description;
			else if (Description is CLocalizationData) return ((CLocalizationData)Description).GetString("");
			return "";
		}

		// String or CLocalizationData
		[JsonProperty("name")]
		[JsonConverter(typeof(LocalizedStringConverter<CLocalizationData>))]
		public object Name;

		[JsonProperty("rarity")]
		public string Rarity;

		// String or CLocalizationData
		[JsonProperty("author")]
		[JsonConverter(typeof(LocalizedStringConverter<CLocalizationData>))]
		public object Author;

		// String or CLocalizationData
		[JsonProperty("description")]
		[JsonConverter(typeof(LocalizedStringConverter<CLocalizationData>))]
		public object Description;
	}

}
