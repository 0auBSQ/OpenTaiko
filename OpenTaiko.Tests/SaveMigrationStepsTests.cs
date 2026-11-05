using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// v0.6.1.0 and v0.6.1.1: the hitsound and save uid columns, each added in one transaction with its version update
	public class SaveMigrationStepsTests : IDisposable {
		private readonly TempDir dir = new TempDir("save_migration_");
		private readonly string db;

		public SaveMigrationStepsTests() {
			db = Path.Combine(dir, "Saves.db3");
			string template = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "OpenTaiko", ".init", "Saves.db3");
			File.Copy(template, db);
		}

		public void Dispose() => dir.Dispose();

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

		private static Dictionary<long, (string uid, string hitsounds)> Saves(SqliteConnection connection) {
			var saves = new Dictionary<long, (string, string)>();
			using var command = connection.CreateCommand();
			command.CommandText = "SELECT SaveId, SaveUID, SelectedHitsounds FROM saves;";
			using var reader = command.ExecuteReader();
			while (reader.Read()) saves[reader.GetInt64(0)] = (reader.GetString(1), reader.GetString(2));
			return saves;
		}

		private static string Version(SqliteConnection connection) {
			using var command = connection.CreateCommand();
			command.CommandText = "SELECT SupportedVersion FROM opentaiko_version;";
			return (string)command.ExecuteScalar()!;
		}

		private static void AssertEverySaveHasItsOwnUid(Dictionary<long, (string uid, string hitsounds)> saves) {
			var uids = new HashSet<string>();
			foreach (var (uid, hitsounds) in saves.Values) {
				Assert.True(Guid.TryParse(uid, out _));
				Assert.True(uids.Add(uid));
				Assert.Equal("Taiko", hitsounds);
			}
		}

		[Fact]
		public void Migration_RunsTwice() {
			using var connection = Open();
			DBSavesAutoupdate.HandleSavesDBAutoupdates(connection);
			var first = Saves(connection);
			Assert.Equal("v0.6.1.3", Version(connection));
			Assert.Equal(7, first.Count);
			AssertEverySaveHasItsOwnUid(first);

			DBSavesAutoupdate.HandleSavesDBAutoupdates(connection);
			Assert.Equal("v0.6.1.3", Version(connection));
			Assert.Equal(first, Saves(connection));
		}

		[Fact]
		public void Steps_RunAgain_WhenTheirColumnsAlreadyExist() {
			using var connection = Open();
			DBSavesAutoupdate.HandleSavesDBAutoupdates(connection);
			string kept = Saves(connection)[0].uid;

			// an earlier start that stopped after adding both columns, before the version update and some uids
			Run(connection, "UPDATE saves SET SaveUID = '' WHERE SaveId <> 0;");
			Run(connection, "UPDATE opentaiko_version SET SupportedVersion = 'v0.6.0.70';");

			DBSavesAutoupdate.HandleSavesDBAutoupdates(connection);
			var saves = Saves(connection);
			Assert.Equal("v0.6.1.3", Version(connection));
			Assert.Equal(kept, saves[0].uid);
			AssertEverySaveHasItsOwnUid(saves);
		}
	}
}
