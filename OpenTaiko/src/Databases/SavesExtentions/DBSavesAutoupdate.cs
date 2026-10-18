using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

			// 0.6.1.2 - Nameplate ids as text; the equipped nameplate is looked up by its id only
			if (VersionComparer.CompareVersions(version, "v0.6.1.2") < 0) {
				BackupSavesDB(connection, "v0.6.1.2");
				NameplateIdsAsText(connection);
			}

			// 0.6.1.3 - Global strings: a save's named text values (a module's data as JSON, for example)
			if (VersionComparer.CompareVersions(version, "v0.6.1.3") < 0) {
				AddGlobalStringsTable(connection);
			}
		}

		#region [Helpers]

		// a copy of the save database before the step for that version runs: Saves.db3.before-<version>.bak
		private static void BackupSavesDB(SqliteConnection connection, string version) {
			string path = connection.DataSource;
			if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
			try {
				File.Copy(path, $"{path}.before-{version}.bak", true);
			} catch (Exception ex) {
				Trace.TraceWarning($"Saves.db3 could not be backed up before {version}: {ex.Message}");
			}
		}

		// the declared type of a column ("INTEGER", "TEXT", …), or null when the table has no such column
		private static string? ColumnType(SqliteConnection connection, SqliteTransaction transaction, string table, string column) {
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = "SELECT type FROM pragma_table_info($table) WHERE name = $column;";
			command.Parameters.AddWithValue("$table", table);
			command.Parameters.AddWithValue("$column", column);
			return command.ExecuteScalar() as string;
		}

		#endregion

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

		#region [0.6.1.2 - Nameplate ids as text]

		// Runs in one transaction with the version update, so an interrupted start runs it again from the start.
		// Each change checks the columns first.
		private static void NameplateIdsAsText(SqliteConnection connection) {
			using var transaction = connection.BeginTransaction();
			void Run(string sql) {
				using var command = connection.CreateCommand();
				command.Transaction = transaction;
				command.CommandText = sql;
				command.ExecuteNonQuery();
			}
			bool IsText(string table, string column) =>
				string.Equals(ColumnType(connection, transaction, table, column), "TEXT", StringComparison.OrdinalIgnoreCase);

			// unlocks: NameplateId INTEGER -> TEXT, one row per save and nameplate
			if (!IsText("nameplate_titles", "NameplateId")) {
				Run("ALTER TABLE nameplate_titles ADD COLUMN NameplateKey TEXT NOT NULL DEFAULT '';");
				Run("UPDATE nameplate_titles SET NameplateKey = CAST(NameplateId AS TEXT);");
				Run("ALTER TABLE nameplate_titles DROP COLUMN NameplateId;");
				Run("ALTER TABLE nameplate_titles RENAME COLUMN NameplateKey TO NameplateId;");
			}
			Run("DELETE FROM nameplate_titles WHERE rowid NOT IN (SELECT MIN(rowid) FROM nameplate_titles GROUP BY SaveId, NameplateId);");
			Run("CREATE UNIQUE INDEX IF NOT EXISTS idx_unique_nameplate ON nameplate_titles (SaveId, NameplateId);");

			// equipped nameplate: PlayerNameplateId INTEGER -> TEXT, '' = none, '0' = the free "Beginner" nameplate
			if (!IsText("saves", "PlayerNameplateId")) {
				string beginner = ColumnType(connection, transaction, "saves", "PlayerNameplateTitle") != null
					? "WHEN PlayerNameplateTitle IN ('Beginner', '初心者') THEN '0'"
					: "";
				Run("ALTER TABLE saves ADD COLUMN NameplateKey TEXT NOT NULL DEFAULT '0';");
				Run($"""
				UPDATE saves SET NameplateKey = CASE
					WHEN PlayerNameplateId >= 0 THEN CAST(PlayerNameplateId AS TEXT)
					{beginner}
					ELSE '' END;
				""");
				Run("ALTER TABLE saves DROP COLUMN PlayerNameplateId;");
				Run("ALTER TABLE saves RENAME COLUMN NameplateKey TO PlayerNameplateId;");
			}

			// the copies of the equipped nameplate's title, type and rarity
			foreach (string column in new[] { "PlayerNameplateTitle", "PlayerNameplateType", "PlayerNameplateRarityInt" })
				if (ColumnType(connection, transaction, "saves", column) != null)
					Run($"ALTER TABLE saves DROP COLUMN {column};");

			Run("UPDATE opentaiko_version SET SupportedVersion = 'v0.6.1.2';");
			transaction.Commit();
		}

		#endregion

		#region [0.6.1.3 - Global strings]

		// one value per save and name; runs in one transaction with the version update
		private static void AddGlobalStringsTable(SqliteConnection connection) {
			using var transaction = connection.BeginTransaction();
			using (var command = connection.CreateCommand()) {
				command.Transaction = transaction;
				command.CommandText = """
				CREATE TABLE IF NOT EXISTS "global_strings" (
					"EntryId"		INTEGER NOT NULL UNIQUE,
					"StringName"	TEXT NOT NULL,
					"StringValue"	TEXT NOT NULL DEFAULT '',
					"SaveId"		INTEGER,
					FOREIGN KEY("SaveId") REFERENCES "saves"("SaveId"),
					PRIMARY KEY("EntryId" AUTOINCREMENT),
					UNIQUE("SaveId", "StringName")
				);
				UPDATE opentaiko_version SET SupportedVersion = 'v0.6.1.3';
				""";
				command.ExecuteNonQuery();
			}
			transaction.Commit();
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
