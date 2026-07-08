using System;
using System.Collections.Generic;
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

			// 0.6.1.0 - Per-save hitsound selection
			if (VersionComparer.CompareVersions(version, "v0.6.1.0") < 0) {
				AddSelectedHitsoundsColumn(connection);
				SetDBVersion(connection, "v0.6.1.0");
			}

			// 0.6.1.1 - Per-save unique id (uuid4), used to key per-save data such as the My Room save
			if (VersionComparer.CompareVersions(version, "v0.6.1.1") < 0) {
				AddSaveUidColumn(connection);
				SetDBVersion(connection, "v0.6.1.1");
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

		#region [0.6.1.0 - Hitsounds per save]

		private static void AddSelectedHitsoundsColumn(SqliteConnection connection) {
			var command = connection.CreateCommand();
			command.CommandText = """
			ALTER TABLE saves ADD COLUMN SelectedHitsounds TEXT NOT NULL DEFAULT 'Taiko';
			""";
			command.ExecuteNonQuery();
		}

		#endregion

		#region [0.6.1.1 - Per-save UID]

		// Adds a SaveUID column and gives every EXISTING row a fresh uuid4. Guid.NewGuid() is a
		// version-4 UUID drawn from the OS cryptographic RNG (already properly seeded, collision-safe —
		// no manual seeding needed), so two saves can never share a UID.
		private static void AddSaveUidColumn(SqliteConnection connection) {
			var add = connection.CreateCommand();
			add.CommandText = "ALTER TABLE saves ADD COLUMN SaveUID TEXT NOT NULL DEFAULT '';";
			add.ExecuteNonQuery();

			var ids = new List<long>();
			var sel = connection.CreateCommand();
			sel.CommandText = "SELECT SaveId FROM saves;";
			using (var reader = sel.ExecuteReader()) {
				while (reader.Read()) ids.Add(Convert.ToInt64(reader["SaveId"]));
			}
			foreach (var id in ids) {
				var up = connection.CreateCommand();
				up.CommandText = "UPDATE saves SET SaveUID = $uid WHERE SaveId = $id;";
				up.Parameters.AddWithValue("$uid", Guid.NewGuid().ToString());
				up.Parameters.AddWithValue("$id", id);
				up.ExecuteNonQuery();
			}
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
