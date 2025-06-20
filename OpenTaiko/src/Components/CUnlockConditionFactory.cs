using Newtonsoft.Json;

namespace OpenTaiko {
	internal class CUnlockConditionFactory {
		public class UnlockConditionJsonRaw {
			public UnlockConditionJsonRaw() {
				Condition = "";
				Values = new int[] { 0 };
				Type = "me";
				Reference = new string[] { "" };
			}
			public UnlockConditionJsonRaw(string cd, int[] vl, string tp, string[] rf) {
				Condition = cd;
				Values = vl;
				Type = tp;
				Reference = rf;
			}

			// Condition type
			[JsonProperty("condition")]
			public string Condition;

			// Condition values
			[JsonProperty("values")]
			public int[] Values;

			// Condition type
			[JsonProperty("type")]
			public string Type;

			// Referenced charts
			[JsonProperty("references")]
			public string[] Reference;
		}

		/*
		 * == Conditions avaliable ==
		 * ch : "Coins here", coin requirement, payable within the heya menu, 1 value : [Coin price]
		 * cs : "Coins shop", coin requirement, payable only within the Medal shop selection screen
		 * cm : "Coins menu", coin requirement, payable only within the song select screen (used only for songs)
		 * ce : "Coins earned", coins earned since the creation of the save file, 1 value : [Total earned coins]
		 * sd : "Song distinct (performance)", count of the number of distinct chart where a given status was got, condition 2 values : [Song count, Clear status (0~4)], input 1 value [Count of fullfiled songs]
		 * dp : "Difficulty pass", count of difficulties pass, unlock check during the results screen, condition 3 values : [Difficulty int (0~4), Clear status (0~4), Number of performances], input 1 value [Plays fitting the condition]
		 * lp : "Level pass", count of level pass, unlock check during the results screen, condition 3 values : [Star rating, Clear status (0~4), Number of performances], input 1 value [Plays fitting the condition]
		 * sp : "Song performance", count of a specific song pass, unlock check during the results screen, condition 2 x n values for n songs  : [Difficulty int (0~4, if -1 : Any), Clear status (0~4), ...], input 1 value [Count of fullfiled songs], n references for n songs (Song ids)
		 * sg : "Song genre (performance)", count of any unique song pass within a specific genre folder, unlock check during the results screen, condition 2 x n values for n songs : [Song count, Clear status (0~4), ...], input 1 value [Count of fullfiled genres], n references for n genres (Genre names)
		 * sc : "Song charter (performance)", count of any chart pass by a specific charter, unlock check during the results screen, condition 2 x n values for n songs : [Song count, Clear status (0~4), ...], input 1 value [Count of fullfiled charters], n references for n charters (Charter names)
		 * tp : "Total plays", 1 value : [Total playcount]
		 * ap : "AI battle plays", 1 value : [AI battle playcount]
		 * aw : "AI battle wins", 1 value : [AI battle wins count]
		 * ig : "Impossible to Get", (not recommanded) used to be able to have content in database that is impossible to unlock, no values
		 * gt : "Global trigger", 1 value : [0: OFF, 1: ON], 1 reference : [Global trigger name]
		 * gc : "Global counter", 1 value : [Value to compare], 1 reference : [Global counter name]
		 *
		 *
		 * == Combined conditions (coming soon)
		 * andcomb : "AND combination", fullfil all the included conditions to unlock the asset, n references for each condition in the reference array as a stringified JSON
		 * orcomb : "OR combination", fullfil at least one of the included conditions to unlock the asset, n references for each condition in the reference array as a stringified JSON
		 */

		public CUnlockCondition GenerateUnlockObjectFromJsonRaw(UnlockConditionJsonRaw? rawJson) {
			if (rawJson == null) {
				return new CUnlockError(null);
			}

			switch (rawJson.Condition) {
				case "ch": {
						return new CUnlockCH(rawJson);
					}
				case "cs": {
						return new CUnlockCS(rawJson);
					}
				case "cm": {
						return new CUnlockCM(rawJson);
					}
				case "ce": {
						return new CUnlockCE(rawJson);
					}
				case "sd": {
						return new CUnlockSD(rawJson);
					}
				case "dp": {
						return new CUnlockDP(rawJson);
					}
				case "lp": {
						return new CUnlockLP(rawJson);
					}
				case "sp": {
						return new CUnlockSP(rawJson);
					}
				case "sg": {
						return new CUnlockSG(rawJson);
					}
				case "sc": {
						return new CUnlockSC(rawJson);
					}
				case "tp": {
						return new CUnlockTP(rawJson);
					}
				case "ap": {
						return new CUnlockAP(rawJson);
					}
				case "aw": {
						return new CUnlockAW(rawJson);
					}
				case "ig": {
						return new CUnlockIG(rawJson);
					}
				case "gt": {
						return new CUnlockGT(rawJson);
					}
				case "gc": {
						return new CUnlockGC(rawJson);
					}
				case "andcomb": {
						return new CUnlockAndComb(rawJson);
					}
				case "orcomb": {
						return new CUnlockOrComb(rawJson);
					}
			}

			return new CUnlockError(null);
		}

		public CUnlockCondition GenerateUnlockObjectFromJsonPath(string jsonPath) {
			UnlockConditionJsonRaw? rawJson = ConfigManager.GetConfig<UnlockConditionJsonRaw>(jsonPath);

			return this.GenerateUnlockObjectFromJsonRaw(rawJson);
		}
	}
}
