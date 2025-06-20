using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using static OpenTaiko.DBNameplateUnlockables;

namespace OpenTaiko;

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

				#region [ Build raw json ]

				CUnlockConditionFactory.UnlockConditionJsonRaw _raw = new CUnlockConditionFactory.UnlockConditionJsonRaw(
					(string)reader["UnlockCondition"],
					JsonConvert.DeserializeObject<int[]>((string)reader["UnlockValues"]) ?? new int[] { 0 },
					(string)reader["UnlockType"],
					JsonConvert.DeserializeObject<string[]>((string)reader["UnlockReferences"]) ?? new string[] { "" }
					);

				nu.unlockConditions = OpenTaiko.UnlockConditionFactory.GenerateUnlockObjectFromJsonRaw(_raw);

				#endregion

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
		public CUnlockCondition unlockConditions;
	}

	public void tGetUnlockedItems(int _player, ModalQueue mq) {
		int player = OpenTaiko.GetActualPlayer(_player);
		var _sf = OpenTaiko.SaveFileInstances[player].data.UnlockedNameplateIds;
		bool _edited = false;

		foreach (KeyValuePair<Int64, NameplateUnlockable> item in data) {
			var _npvKey = (int)item.Key;
			if (!_sf.Contains(_npvKey))// !_sf.ContainsKey(_npvKey))
			{
				var _fulfilled = item.Value.unlockConditions.tConditionMet(player, CUnlockCondition.EScreen.Internal).Item1;

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

	public bool AddToDatabase(string title, int type, string rarity, string unlock_condition, string unlock_type, string unlock_values, string unlock_references, Dictionary<string, string> translations) {
		Trace.TraceInformation("Requested a new entry into NameplateUnlockables.db3.");
		_fn = @$"{OpenTaiko.strEXEのあるフォルダ}Databases{Path.DirectorySeparatorChar}NameplateUnlockables.db3";

		using (var connection = new SqliteConnection($"Data Source={_fn}")) {
			connection.Open();

			// Fetch the highest unique ID, and add 1 onto it to use for our new nameplate
			Int64 id = 0;
			using (var id_command = connection.CreateCommand()) {
				Trace.TraceInformation("Fetching available nameplate ID.");

				id_command.CommandText = $"""
					SELECT * FROM nameplates ORDER BY NameplateId DESC LIMIT 1;
					""";

				using (var id_reader = id_command.ExecuteReader()) {
					while (id_reader.Read()) {
						id = (Int64)id_reader["NameplateId"] + 1;
					}
					id_reader.Close();

					Trace.TraceInformation($"'{id}' was determined to be an available nameplate ID.");
				}
			}

			var command = connection.CreateCommand();
			command.CommandText = $"""
				INSERT INTO nameplates (NameplateId, DefaultString, NameplateType, Rarity, UnlockCondition, UnlockType, UnlockValues, UnlockReferences)
				VALUES
				(
				{id},
				'{title.EscapeSingleQuotes()}',
				{type},
				'{rarity}',
				'{unlock_condition}',
				'{unlock_type}',
				'{unlock_values}',
				'{unlock_references.EscapeSingleQuotes()}');
				""";

			if (command.ExecuteNonQuery() < 1) {
				Trace.TraceInformation("INSERT was executed, but nothing was inserted. Terminating connection.");
				return false;
			}
			if (translations.Count == 0) {
				Trace.TraceInformation("Inserted a new nameplate into the database with the following details:\n" +
					$"ID: {id}\n" +
					$"Title: {title}\n" +
					$"Type: {type}\n" +
					$"Rarity: {rarity}\n" +
					$"Unlock Condition: {unlock_condition}\n" +
					$"Unlock Type: {unlock_type}\n" +
					$"Unlock Values: {unlock_values}\n" +
					$"Unlock References: {unlock_references}");
				Trace.TraceInformation("INSERT was executed, and was deemed successful. No translations were provided, so this step will be skipped. Terminating connection.");
				return true;
			}

			// Fetch table names so that we can get translation tables (future-proofing for new languages)
			List<string> table_names = [];
			using (var table_command = connection.CreateCommand()) {
				table_command.CommandText = $"""
					SELECT name FROM sqlite_schema WHERE type='table';
					""";

				using (var table_reader = table_command.ExecuteReader()) {
					while (table_reader.Read()) {
						table_names.Add((string)table_reader["name"]);
					}
					table_reader.Close();
				}
			}

			foreach (var entry in translations) {
				string table_name = $"translation_{entry.Key.ToLower()}";
				if (!table_names.Contains(table_name)) continue;

				// Fetch the highest unique ID, and add 1 onto it
				Int64 trans_id = 0;
				using (var transid_command = connection.CreateCommand()) {
					transid_command.CommandText = $"""
					SELECT * FROM {table_name} ORDER BY TranslationId DESC LIMIT 1;
					""";

					using (var transid_reader = transid_command.ExecuteReader()) {
						while (transid_reader.Read()) {
							trans_id = (Int64)transid_reader["TranslationId"] + 1;
						}
						transid_reader.Close();
					}
				}

				var trans_command = connection.CreateCommand();
				trans_command.CommandText = $"""
					INSERT INTO {table_name} (TranslationId, String, NameplateId)
					VALUES
					({trans_id}, '{entry.Value.EscapeSingleQuotes()}', {id});
					""";
				trans_command.ExecuteNonQuery();
				Trace.TraceInformation($"{table_name} received a new entry.");
			}

			Trace.TraceInformation("Inserted a new nameplate into the database with the following details:\n" +
				$"ID: {id}\n" +
				$"Title: {title}\n" +
				$"Type: {type}\n" +
				$"Rarity: {rarity}\n" +
				$"Unlock Condition: {unlock_condition}\n" +
				$"Unlock Type: {unlock_type}\n" +
				$"Unlock Values: {unlock_values}\n" +
				$"Unlock References: {unlock_references}\n" +
				$"Translations: {string.Join(" / ", translations.Select(item => item.Key + ", " + item.Value))}");
		}

		Trace.TraceInformation("INSERT was executed, and was deemed successful. Terminating connection.");
		return true;
	}
}
