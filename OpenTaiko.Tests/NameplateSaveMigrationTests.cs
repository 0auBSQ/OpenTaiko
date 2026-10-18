using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// v0.6.1.2: nameplate ids become text, the equipped nameplate's copied title/type/rarity columns are dropped
	public class NameplateSaveMigrationTests : IDisposable {
		private readonly string dir = Path.Combine(Path.GetTempPath(), "np_migration_" + Guid.NewGuid().ToString("N"));
		private readonly string db;

		public NameplateSaveMigrationTests() {
			Directory.CreateDirectory(dir);
			db = Path.Combine(dir, "Saves.db3");
			string template = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "OpenTaiko", ".init", "Saves.db3");
			File.Copy(template, db);
		}

		public void Dispose() {
			try { Directory.Delete(dir, true); } catch (IOException) { }
		}

		private SqliteConnection Open() {
			var connection = new SqliteConnection($"Data Source={db};Pooling=False");
			connection.Open();
			return connection;
		}

		private static void Run(SqliteConnection connection, string sql) {
			using var command = connection.CreateCommand();
			command.CommandText = sql;
			command.ExecuteNonQuery();
		}

		private static List<(long save, string id, string type)> Rows(SqliteConnection connection, string sql) {
			var rows = new List<(long, string, string)>();
			using var command = connection.CreateCommand();
			command.CommandText = sql;
			using var reader = command.ExecuteReader();
			while (reader.Read()) rows.Add((reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
			return rows;
		}

		private static bool HasColumn(SqliteConnection connection, string table, string column) {
			using var command = connection.CreateCommand();
			command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = '{column}';";
			return (long)command.ExecuteScalar()! > 0;
		}

		private static void AssertMigrated(SqliteConnection connection) {
			Assert.Equal(
				new List<(long, string, string)> {
					(0, "66", "text"), (1, "0", "text"), (2, "", "text"), (3, "0", "text"), (4, "292", "text"), (5, "0", "text"), (6, "0", "text"),
				},
				Rows(connection, "SELECT SaveId, PlayerNameplateId, typeof(PlayerNameplateId) FROM saves ORDER BY SaveId;"));
			Assert.Equal(
				new List<(long, string, string)> { (0, "12", "text"), (0, "66", "text"), (4, "292", "text") },
				Rows(connection, "SELECT SaveId, NameplateId, typeof(NameplateId) FROM nameplate_titles ORDER BY SaveId, NameplateId;"));

			Assert.False(HasColumn(connection, "saves", "PlayerNameplateTitle"));
			Assert.False(HasColumn(connection, "saves", "PlayerNameplateType"));
			Assert.False(HasColumn(connection, "saves", "PlayerNameplateRarityInt"));
			Assert.False(HasColumn(connection, "saves", "NameplateKey"));
			Assert.False(HasColumn(connection, "nameplate_titles", "NameplateKey"));

			using var version = connection.CreateCommand();
			version.CommandText = "SELECT SupportedVersion FROM opentaiko_version;";
			Assert.Equal("v0.6.1.3", version.ExecuteScalar());

			// the unique index keeps one row per save and nameplate
			using var duplicate = connection.CreateCommand();
			duplicate.CommandText = "INSERT OR IGNORE INTO nameplate_titles (NameplateId, SaveId) VALUES ('66', 0);";
			Assert.Equal(0, duplicate.ExecuteNonQuery());
		}

		[Fact]
		public void MigratesIdsToText_DropsCopies_AndRunsAgainSafely() {
			using (var connection = Open()) {
				// an old save: numeric ids, a duplicate unlock row, the default title as text, a cleared title
				Run(connection, """
					UPDATE saves SET PlayerNameplateId = 66, PlayerNameplateTitle = 'Forged in Silicon', PlayerNameplateType = 19, PlayerNameplateRarityInt = 5 WHERE SaveId = 0;
					UPDATE saves SET PlayerNameplateId = -1, PlayerNameplateTitle = '' WHERE SaveId = 2;
					UPDATE saves SET PlayerNameplateId = -1, PlayerNameplateTitle = '初心者' WHERE SaveId = 3;
					UPDATE saves SET PlayerNameplateId = 292, PlayerNameplateTitle = 'Masterpiece Charter' WHERE SaveId = 4;
					INSERT INTO nameplate_titles (NameplateId, SaveId) VALUES (66, 0), (66, 0), (12, 0), (292, 4);
					""");

				DBSavesAutoupdate.HandleSavesDBAutoupdates(connection);
				AssertMigrated(connection);
				Assert.True(File.Exists(db + ".before-v0.6.1.2.bak"));

				// a second start, and the step forced to run again, change nothing
				DBSavesAutoupdate.HandleSavesDBAutoupdates(connection);
				AssertMigrated(connection);
				Run(connection, "UPDATE opentaiko_version SET SupportedVersion = 'v0.6.1.1';");
				DBSavesAutoupdate.HandleSavesDBAutoupdates(connection);
				AssertMigrated(connection);
			}
		}
	}
}
