using Newtonsoft.Json;

namespace OpenTaiko {
	internal class DBSkinPreset {
		public class SkinScene {
			public SkinScene() {
				UpperBackground = null;
				LowerBackground = null;
				DancerSet = null;
				FooterSet = null;
				MobSet = null;
				RunnerSet = null;
			}

			[JsonProperty("UP")]
			public string[] UpperBackground;

			[JsonProperty("DOWN")]
			public string[] LowerBackground;

			[JsonProperty("DANCER")]
			public string[] DancerSet;

			[JsonProperty("FOOTER")]
			public string[] FooterSet;

			[JsonProperty("MOB")]
			public string[] MobSet;

			[JsonProperty("RUNNER")]
			public string[] RunnerSet;
		}
		public class SkinPreset {
			public SkinPreset() {
				Regular = new Dictionary<string, SkinScene>();
				Dan = new Dictionary<string, SkinScene>();
				Tower = new Dictionary<string, SkinScene>();
				AI = new Dictionary<string, SkinScene>();
			}


			[JsonProperty("Regular")]
			public Dictionary<string, SkinScene> Regular;

			[JsonProperty("Dan")]
			public Dictionary<string, SkinScene> Dan;

			[JsonProperty("Tower")]
			public Dictionary<string, SkinScene> Tower;

			[JsonProperty("AI")]
			public Dictionary<string, SkinScene> AI;

		}
	}
}
