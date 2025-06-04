using Microsoft.Data.Sqlite;

namespace OpenTaiko {
	internal class DBSavesAutoupdate {

		public static void HandleSavesDBAutoupdates(SqliteConnection connection) {
			// Pre 0.6.0
			FixSaveDB_IdxUniquePlay(connection);

			string version = GetDBVersion(connection);

			// 0.6.0.70 - Global Counters
			if (VersionComparer.CompareVersions(version, "v0.6.0.70") < 0) {
				AddActiveCountersTable(connection);
				SetDBVersion(connection, "v0.6.0.70");
			}
		}

		#region [DB Version]

		private static string GetDBVersion(SqliteConnection connection) {
			string _ver = "v0.6.0.0";

			var command = connection.CreateCommand();
			command.CommandText =
				@$"
                    SELECT *
                    FROM opentaiko_version;
                ";
			SqliteDataReader reader = command.ExecuteReader();
			while (reader.Read()) {
				_ver = (string)reader["SupportedVersion"];
			}
			reader.Close();

			return _ver;
		}

		private static void SetDBVersion(SqliteConnection connection, string new_version) {
			var command = connection.CreateCommand();
			command.CommandText = $"""
			UPDATE opentaiko_version
			SET SupportedVersion = '{new_version}';
			""";
			command.ExecuteNonQuery();
		}

		#endregion

		#region [0.6.0.70 - Active Counters]

		private static void AddActiveCountersTable(SqliteConnection connection) {
			var command = connection.CreateCommand();
			command.CommandText = $"""
			CREATE TABLE "global_counters" (
				"EntryId"		INTEGER NOT NULL UNIQUE,
				"CounterName"	TEXT NOT NULL DEFAULT 0,
				"CounterValue"	REAL NOT NULL DEFAULT 0,
				"SaveId"		INTEGER,
				FOREIGN KEY("SaveId") REFERENCES "saves"("SaveId"),
				PRIMARY KEY("EntryId" AUTOINCREMENT)
			);
			""";
			command.ExecuteNonQuery();
		}

		#endregion

		#region [Pre 0.6.0]

		private static void FixSaveDB_IdxUniquePlay(SqliteConnection connection) {
			var command = connection.CreateCommand();
			command.CommandText = $"""
			DROP INDEX IF EXISTS idx_unique_play;
			CREATE UNIQUE INDEX idx_unique_play ON best_plays (
				"ChartUniqueId",
				"ChartDifficulty",
				"PlayMods",
				"SaveId"
			);
			""";
			command.ExecuteNonQuery();
		}

		#endregion
	}
}
