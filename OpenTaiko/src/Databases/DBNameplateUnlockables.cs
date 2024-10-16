﻿using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using static OpenTaiko.DBNameplateUnlockables;

namespace OpenTaiko {
	internal class DBNameplateUnlockables : CSavableT<Dictionary<Int64, NameplateUnlockable>> {
		public DBNameplateUnlockables() {
			_fn = @$"{OpenTaiko.strEXEのあるフォルダ}Databases{Path.DirectorySeparatorChar}NameplateUnlockables.db3";

			using (var connection = new SqliteConnection(@$"Data Source={_fn}")) {
				connection.Open();

				// Get existing languages
				List<string> _translations = HDatabaseHelpers.GetAvailableLanguage(connection, "translation");

				// Get nameplates
				var command = connection.CreateCommand();
				command.CommandText =
				@$"
                    SELECT np.*, {String.Join(", ", _translations.Select((code, _) => $@"{code}.String AS {code}_String"))}
                    FROM nameplates np
                    {String.Join(Environment.NewLine, _translations.Select((code, _) => $@"LEFT JOIN translation_{code} {code} ON np.NameplateId = {code}.NameplateId"))}
                ";

				var reader = command.ExecuteReader();
				while (reader.Read()) {
					NameplateUnlockable nu = new NameplateUnlockable();
					nu.rarity = (string)reader["Rarity"];
					nu.unlockConditions = new DBUnlockables.CUnlockConditions();
					nu.unlockConditions.Condition = (string)reader["UnlockCondition"];
					nu.unlockConditions.Values = JsonConvert.DeserializeObject<int[]>((string)reader["UnlockValues"]) ?? new int[] { 0 };
					nu.unlockConditions.Type = (string)reader["UnlockType"];
					nu.unlockConditions.Reference = JsonConvert.DeserializeObject<string[]>((string)reader["UnlockReferences"]) ?? new string[] { "" };
					nu.nameplateInfo = new SaveFile.CNamePlateTitle((int)((Int64)reader["NameplateType"]));

					nu.nameplateInfo.cld.SetString("default", (string)reader["DefaultString"]);
					foreach (string tr in _translations) {
						if (reader[@$"{tr}_String"] != DBNull.Value)
							nu.nameplateInfo.cld.SetString(tr, (string)reader[@$"{tr}_String"]);
					}

					data[((Int64)reader["NameplateId"])] = nu;
				}
				reader.Close();
			}
		}
		public class NameplateUnlockable {
			[JsonProperty("NameplateInfo")]
			public SaveFile.CNamePlateTitle nameplateInfo;

			[JsonProperty("Rarity")]
			public string rarity;

			[JsonProperty("UnlockCondition")]
			public DBUnlockables.CUnlockConditions unlockConditions;
		}

		public void tGetUnlockedItems(int _player, ModalQueue mq) {
			int player = OpenTaiko.GetActualPlayer(_player);
			var _sf = OpenTaiko.SaveFileInstances[player].data.UnlockedNameplateIds;
			bool _edited = false;

			foreach (KeyValuePair<Int64, NameplateUnlockable> item in data) {
				var _npvKey = (int)item.Key;
				if (!_sf.Contains(_npvKey))// !_sf.ContainsKey(_npvKey))
				{
					var _fulfilled = item.Value.unlockConditions.tConditionMetWrapper(player, DBUnlockables.CUnlockConditions.EScreen.Internal).Item1;

					if (_fulfilled) {
						_sf.Add(_npvKey);
						_edited = true;
						mq.tAddModal(
							new Modal(
								Modal.EModalType.Title,
								HRarity.tRarityToModalInt(item.Value.rarity),
								item,
								OpenTaiko.NamePlate.lcNamePlate
								),
							_player);

						DBSaves.RegisterUnlockedNameplate(OpenTaiko.SaveFileInstances[player].data.SaveId, _npvKey);
					}
				}
			}

			if (_edited)
				OpenTaiko.SaveFileInstances[player].tApplyHeyaChanges();
		}
	}
}
