using FDK;
using static OpenTaiko.CActSelect曲リスト;
using Color = System.Drawing.Color;

namespace OpenTaiko {
	class CHeyaDisplayAssetInformations {
		private static TitleTextureKey? ttkDescription = null;

		private static String ToHex(System.Drawing.Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

		private static int XOrigin {
			get {
				return OpenTaiko.Skin.Heya_DescriptionTextOrigin[0];
			}
		}

		private static int YOrigin {
			get {
				return OpenTaiko.Skin.Heya_DescriptionTextOrigin[1];
			}
		}

		public static void DisplayCharacterInfo(CCachedFontRenderer pf, CCharacter character) {
			string description = "";
			description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_NAME").SafeFormat(character.metadata.tGetName())}\n";
			description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_RARITY").SafeFormat(
				"<c." + ToHex(HRarity.tRarityToColor(character.metadata.Rarity)) + ">"
				  + CLangManager.LangInstance.GetString($"HEYA_DESCRIPTION_RARITY{HRarity.tRarityToLangInt(character.metadata.Rarity)}")
				  + "</c>"
				)}\n";
			if (character.metadata.tGetDescription() != "") description += character.metadata.tGetDescription() + "\n";
			description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_AUTHOR").SafeFormat(character.metadata.tGetAuthor())}\n\n";

			var gaugeType = character.effect.Gauge;
			if (gaugeType == "Normal") {
				description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_GAUGETYPE").SafeFormat(CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_GAUGETYPE_NORMAL"))}\n";
				description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_GAUGETYPE_NORMAL_DESC")}\n";
			} else if (gaugeType == "Hard") {
				description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_GAUGETYPE").SafeFormat("<c.#ff4444>" + CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_GAUGETYPE_HARD") + "</c>")}\n";
				description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_GAUGETYPE_HARD_DESC")}\n";
			} else if (gaugeType == "Extreme") {
				description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_GAUGETYPE").SafeFormat("<c.#8c0000>" + CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_GAUGETYPE_EXTREME") + "</c>")}\n";
				description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_GAUGETYPE_EXTREME_DESC")}\n";
			}

			var bombFactor = character.effect.BombFactor;


			if (bombFactor < 10) description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_BOMBFACTOR").SafeFormat($"{bombFactor}")}\n";
			else if (bombFactor < 25) description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_BOMBFACTOR").SafeFormat($"<c.#b0b0b0>{bombFactor}</c>")}\n";
			else description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_BOMBFACTOR").SafeFormat($"<c.#6b6b6b>{bombFactor}</c>")}\n";

			var fuseFactor = character.effect.FuseRollFactor;
			if (fuseFactor < 10) description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_FUSEFACTOR").SafeFormat($"{fuseFactor}")}\n";
			else if (fuseFactor < 25) description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_FUSEFACTOR").SafeFormat($"<c.#b474c4>{fuseFactor}</c>")}\n";
			else description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_EFFECTS_FUSEFACTOR").SafeFormat($"<c.#7c009c>{fuseFactor}</c>")}\n";
			description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_COIN_MULTIPLIER").SafeFormat(character.effect.GetCoinMultiplier())}\n";


			if (ttkDescription is null || ttkDescription.str != description) {
				ttkDescription = new TitleTextureKey(description, pf, Color.White, Color.Black, 1000);
			}

			OpenTaiko.Tx.Heya_Description_Panel?.t2D描画(0, 0);
			TitleTextureKey.ResolveTitleTexture(ttkDescription).t2D描画(XOrigin, YOrigin);
		}

		public static void DisplayPuchicharaInfo(CCachedFontRenderer pf, CPuchichara puchi) {
			string description = "";
			description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_NAME").SafeFormat(puchi.metadata.tGetName())}\n";
			description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_RARITY").SafeFormat(
				"<c." + ToHex(HRarity.tRarityToColor(puchi.metadata.Rarity)) + ">"
				  + CLangManager.LangInstance.GetString($"HEYA_DESCRIPTION_RARITY{HRarity.tRarityToLangInt(puchi.metadata.Rarity)}")
				  + "</c>"
				)}\n";
			if (puchi.metadata.tGetDescription() != "") description += puchi.metadata.tGetDescription() + "\n";
			description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_AUTHOR").SafeFormat(puchi.metadata.tGetAuthor())}\n\n";

			if (puchi.effect.AllPurple) description += "All big notes become <c.#c800ff>Swap</c> notes\n";
			if (puchi.effect.ShowAdlib) description += "<c.#c4ffe2>ADLib</c> notes become visible\n";
			if (puchi.effect.Autoroll > 0) description += $"Automatic <c.#ffff00>Rolls</c> at {puchi.effect.Autoroll} hits/s\n";
			if (puchi.effect.SplitLane) description += "<c.#ff4040>Split</c> <c.#4053ff>Lanes</c>\n";
			description += $"{CLangManager.LangInstance.GetString("HEYA_DESCRIPTION_COIN_MULTIPLIER").SafeFormat(puchi.effect.GetCoinMultiplier())}\n";

			if (ttkDescription is null || ttkDescription.str != description) {
				ttkDescription = new TitleTextureKey(description, pf, Color.White, Color.Black, 1000);
			}

			OpenTaiko.Tx.Heya_Description_Panel?.t2D描画(0, 0);
			TitleTextureKey.ResolveTitleTexture(ttkDescription).t2D描画(XOrigin, YOrigin);
		}

		public static void DisplayNameplateTitleInfo(CCachedFontRenderer pf) {

		}

		public static void DisplayDanplateInfo(CCachedFontRenderer pf) {

		}
	}
}
