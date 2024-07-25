using Color = System.Drawing.Color;

namespace TJAPlayer3 {
	internal class HRarity {
		private static Dictionary<string, Color> RarityToColor = new Dictionary<string, Color> {
			["Poor"] = Color.Gray,
			["Common"] = Color.White,
			["Uncommon"] = Color.Lime,
			["Rare"] = Color.Blue,
			["Epic"] = Color.Purple,
			["Legendary"] = Color.Orange,
			["Mythical"] = Color.Pink,
		};

		private static Dictionary<string, int> RarityToModalInt = new Dictionary<string, int> {
			["Poor"] = 0,
			["Common"] = 0,
			["Uncommon"] = 1,
			["Rare"] = 2,
			["Epic"] = 3,
			["Legendary"] = 4,
			["Mythical"] = 4,
		};

		private static Dictionary<string, int> RarityToLangInt = new Dictionary<string, int> {
			["Poor"] = 0,
			["Common"] = 1,
			["Uncommon"] = 2,
			["Rare"] = 3,
			["Epic"] = 4,
			["Legendary"] = 5,
			["Mythical"] = 6,
		};

		private static Dictionary<string, float> RarityToCoinMultiplier = new Dictionary<string, float> {
			["Poor"] = 1f,
			["Common"] = 1f,
			["Uncommon"] = 1f,
			["Rare"] = 1f,
			["Epic"] = 1f,
			["Legendary"] = 1f,
			["Mythical"] = 1f,
		};

		public static Color tRarityToColor(string rarity) {

			Color textColor = Color.White;

			if (RarityToColor.ContainsKey(rarity))
				textColor = RarityToColor[rarity];

			return textColor;

		}

		public static int tRarityToModalInt(string rarity) {
			int modalInt = 0;

			if (RarityToModalInt.ContainsKey(rarity))
				modalInt = RarityToModalInt[rarity];

			return modalInt;
		}

		public static int tRarityToLangInt(string rarity) {
			int modalInt = 1;

			if (RarityToLangInt.ContainsKey(rarity))
				modalInt = RarityToLangInt[rarity];

			return modalInt;
		}

		public static float tRarityToRarityToCoinMultiplier(string rarity) {
			float coinMult = 1f;

			if (RarityToCoinMultiplier.ContainsKey(rarity))
				coinMult = RarityToCoinMultiplier[rarity];

			return coinMult;
		}


	}
}
