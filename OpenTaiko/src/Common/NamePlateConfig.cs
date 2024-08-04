using Newtonsoft.Json;

namespace OpenTaiko {
	class NamePlateConfig {
		public void tNamePlateConfig() {
			// Deprecated, only converts to new format
			/*
            if (!File.Exists("NamePlate.json"))
                tSaveFile();
            */

			tLoadFile();
		}

		#region [Medals]

		public void tEarnCoins(int[] amounts) {
			if (amounts.Length < 2)
				return;

			for (int i = 0; i < 5; i++) {
				int p = OpenTaiko.GetActualPlayer(i);

				data.Medals[p] += amounts[i];
			}
			tSaveFile();
		}

		// Return false if the current amount of coins is to low
		public bool tSpendCoins(int amount, int player) {
			if (player > 1 || player < 0)
				return false;

			if (data.Medals[player] < amount)
				return false;

			data.Medals[player] -= amount;

			tSaveFile();

			return true;
		}

		#endregion

		#region [Dan titles]

		public bool tUpdateDanTitle(string title, bool isGold, int clearStatus, int player) {
			bool changed = false;

			bool iG = isGold;
			int cs = clearStatus;

			if (OpenTaiko.NamePlateConfig.data.DanTitles[player] == null)
				OpenTaiko.NamePlateConfig.data.DanTitles[player] = new Dictionary<string, SaveFile.CDanTitle>();

			if (OpenTaiko.NamePlateConfig.data.DanTitles[player].ContainsKey(title)) {
				if (OpenTaiko.NamePlateConfig.data.DanTitles[player][title].clearStatus > cs)
					cs = OpenTaiko.NamePlateConfig.data.DanTitles[player][title].clearStatus;
				if (OpenTaiko.NamePlateConfig.data.DanTitles[player][title].isGold)
					iG = true;
			}

			// Automatically set the dan to nameplate if new
			// Add a function within the NamePlate.cs file to update the title texture 

			if (!OpenTaiko.NamePlateConfig.data.DanTitles[player].ContainsKey(title) || cs != clearStatus || iG != isGold) {
				changed = true;
				/*
                TJAPlayer3.NamePlateConfig.data.Dan[player] = title;
                TJAPlayer3.NamePlateConfig.data.DanGold[player] = iG;
                TJAPlayer3.NamePlateConfig.data.DanType[player] = cs;
                */
			}


			SaveFile.CDanTitle danTitle = new SaveFile.CDanTitle(iG, cs);

			OpenTaiko.NamePlateConfig.data.DanTitles[player][title] = danTitle;

			tSaveFile();

			return changed;
		}

		#endregion

		#region [Auxilliary classes]

		public class CDanTitle {
			public CDanTitle(bool iG, int cs) {
				isGold = iG;
				clearStatus = cs;
			}

			[JsonProperty("isGold")]
			public bool isGold;

			[JsonProperty("clearStatus")]
			public int clearStatus;
		}

		public class CNamePlateTitle {
			public CNamePlateTitle(int type) {
				iType = type;
			}

			[JsonProperty("iType")]
			public int iType;
		}

		#endregion

		#region [Heya]

		public void tReindexCharacter(int p, string[] characterNamesList) {
			string character = this.data.CharacterName[p];

			if (characterNamesList.Contains(character))
				this.data.Character[p] = characterNamesList.ToList().IndexOf(character);

		}

		public void tUpdateCharacterName(int p, string newChara) {
			this.data.CharacterName[p] = newChara;
		}

		public void tApplyHeyaChanges() {
			this.tSaveFile();
		}

		#endregion

		public class Data {
			[JsonProperty("name")]
			public string[] Name = { "プレイヤー1", "プレイヤー2", "プレイヤー3", "プレイヤー4", "プレイヤー5" };

			[JsonProperty("title")]
			public string[] Title = { "初心者", "初心者", "初心者", "初心者", "初心者" };

			[JsonProperty("dan")]
			public string[] Dan = { "新人", "新人", "新人", "新人", "新人" };

			[JsonProperty("danGold")]
			public bool[] DanGold = { false, false, false, false, false };

			[JsonProperty("danType")]
			public int[] DanType = { 0, 0, 0, 0, 0 };

			[JsonProperty("titleType")]
			public int[] TitleType = { 0, 0, 0, 0, 0 };

			[JsonProperty("puchiChara")]
			public string[] PuchiChara = { "0", "0", "0", "0", "0" };

			[JsonProperty("medals")]
			public int[] Medals = { 0, 0, 0, 0, 0 };

			[JsonProperty("character")]
			public int[] Character = { 0, 0, 0, 0, 0 };

			[JsonProperty("characterName")]
			public string[] CharacterName = { "0", "0", "0", "0", "0" };

			[JsonProperty("danTitles")]
			public Dictionary<string, SaveFile.CDanTitle>[] DanTitles = new Dictionary<string, SaveFile.CDanTitle>[5];

			[JsonProperty("namePlateTitles")]
			public Dictionary<string, SaveFile.CNamePlateTitle>[] NamePlateTitles = new Dictionary<string, SaveFile.CNamePlateTitle>[5];

			[JsonProperty("unlockedPuchicharas")]
			public List<string>[] UnlockedPuchicharas = new List<string>[5]
			{
				new List<string>(),
				new List<string>(),
				new List<string>(),
				new List<string>(),
				new List<string>()
			};

		}

		public Data data = new Data();

		#region [private]

		private void tSaveFile() {
			ConfigManager.SaveConfig(data, "NamePlate.json");
		}

		private void tLoadFile() {
			//data = ConfigManager.GetConfig<Data>(@"NamePlate.json");

			if (!File.Exists("NamePlate.json"))
				return;

			var _data = ConfigManager.GetConfig<Data>(@"NamePlate.json");

			for (int i = 0; i < _data.Name.Length; i++) {
				var _sf = new SaveFile();
				_sf.tSaveFile((i + 1) + "P");
				_sf.data.Name = _data.Name[i];
				_sf.data.Title = _data.Title[i];
				_sf.data.Dan = _data.Dan[i];
				_sf.data.DanGold = _data.DanGold[i];
				_sf.data.DanType = _data.DanType[i];
				_sf.data.TitleType = _data.TitleType[i];
				_sf.data.PuchiChara = _data.PuchiChara[i];
				_sf.data.Medals = _data.Medals[i];
				_sf.data.Character = _data.Character[i];
				_sf.data.CharacterName = _data.CharacterName[i];
				_sf.data.DanTitles = _data.DanTitles[i];
				_sf.data.NamePlateTitles = _data.NamePlateTitles[i];
				_sf.data.UnlockedPuchicharas = _data.UnlockedPuchicharas[i];
				_sf.tApplyHeyaChanges();
			}

			System.IO.File.Move(@"NamePlate.json", @"NamePlate_old.json");
		}

		#endregion
	}
}
