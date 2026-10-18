using System;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// v0.6.1.3: global strings, one text value per save and name; the counters and triggers take their names as parameters
	public class GlobalStringsTests : IDisposable {
		private readonly string dir = Path.Combine(Path.GetTempPath(), "global_strings_" + Guid.NewGuid().ToString("N"));
		private readonly string db;

		public GlobalStringsTests() {
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
			DBSavesAutoupdate.HandleSavesDBAutoupdates(connection);
			return connection;
		}

		[Fact]
		public void Migration_AddsTheTable_AndRunsAgainSafely() {
			using var connection = Open();
			using (var version = connection.CreateCommand()) {
				version.CommandText = "SELECT SupportedVersion FROM opentaiko_version;";
				Assert.Equal("v0.6.1.3", version.ExecuteScalar());
			}
			DBSaves.DBSetGlobalString(connection, 1, "my_room", "{}");
			using (var reset = connection.CreateCommand()) {
				reset.CommandText = "UPDATE opentaiko_version SET SupportedVersion = 'v0.6.1.2';";
				reset.ExecuteNonQuery();
			}
			DBSavesAutoupdate.HandleSavesDBAutoupdates(connection);
			Assert.Equal("{}", DBSaves.GetGlobalStringsDict(connection, 1)["my_room"]);
		}

		[Fact]
		public void Strings_RoundTrip_PerSave_AndReplaceTheirValue() {
			using var connection = Open();
			const string json = "{\"name\":\"O'Brien's \\\"room\\\"\",\"note\":\"桜 – ok\",\"sql\":\"'); DROP TABLE saves; --\"}";
			DBSaves.DBSetGlobalString(connection, 1, "my_room", json);
			DBSaves.DBSetGlobalString(connection, 2, "my_room", "{\"v\":1}");
			Assert.Equal(json, DBSaves.GetGlobalStringsDict(connection, 1)["my_room"]);
			Assert.Equal("{\"v\":1}", DBSaves.GetGlobalStringsDict(connection, 2)["my_room"]);

			DBSaves.DBSetGlobalString(connection, 1, "my_room", "{\"v\":2}");
			var strings = DBSaves.GetGlobalStringsDict(connection, 1);
			Assert.Single(strings);
			Assert.Equal("{\"v\":2}", strings["my_room"]);
			Assert.Empty(DBSaves.GetGlobalStringsDict(connection, 3));
		}

		[Fact]
		public void CountersAndTriggers_TakeAnyName_AndAnyCulture() {
			using var connection = Open();
			var culture = CultureInfo.CurrentCulture;
			try {
				CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
				DBSaves.DBSetGlobalCounter(connection, 1, "it's a counter", 1.5);
				DBSaves.DBSetGlobalCounter(connection, 1, "it's a counter", 2.25);
			} finally {
				CultureInfo.CurrentCulture = culture;
			}
			var counters = DBSaves.GetGlobalCountersDict(connection, 1);
			Assert.Equal(2.25, counters["it's a counter"]);
			Assert.Single(counters);

			DBSaves.DBToggleGlobalTrigger(connection, 1, "it's a trigger", true);
			DBSaves.DBToggleGlobalTrigger(connection, 1, "it's a trigger", true);
			Assert.Equal(new[] { "it's a trigger" }, DBSaves.GetActiveTriggersSet(connection, 1));
			DBSaves.DBToggleGlobalTrigger(connection, 1, "it's a trigger", false);
			Assert.Empty(DBSaves.GetActiveTriggersSet(connection, 1));
		}
	}
}
