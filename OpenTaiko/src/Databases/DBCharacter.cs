using Newtonsoft.Json;

namespace OpenTaiko;

class DBCharacter {
	public class CharacterEffect {
		public CharacterEffect() {
			Gauge = "Normal";
			BombFactor = 20;
			FuseRollFactor = 0;
		}

		public float GetCoinMultiplier(float multRarity, bool gaugeEnabled = true, bool bombFactorEnabled = true, bool fuseRollFactorEnabled = true) {
			if (gaugeEnabled)
				multRarity *= HGaugeMethods.GetCoinMultiplier(Gauge);

			return multRarity;
		}

		public string tGetGaugeType() {
			return HGaugeMethods.IsForceNormalGauge() ? "Normal" : Gauge;
		}

		[JsonProperty("gauge")]
		public string Gauge;

		[JsonProperty("bombFactor")]
		public int BombFactor;

		[JsonProperty("fuseRollFactor")]
		public int FuseRollFactor;
	}

	public class CharacterData {
		public CharacterData() {
			Name = "(None)";
			Rarity = "Common";
			Author = "(None)";
			SpeechText = new CLocalizationData[6] { new CLocalizationData(), new CLocalizationData(), new CLocalizationData(), new CLocalizationData(), new CLocalizationData(), new CLocalizationData() };
		}

		public CharacterData(string pcn, string pcr, string pca, CLocalizationData[] pcst) {
			Name = pcn;
			Rarity = pcr;
			Author = pca;
			SpeechText = pcst;
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

		[JsonProperty("speechtext")]
		public CLocalizationData[] SpeechText;
	}

}
