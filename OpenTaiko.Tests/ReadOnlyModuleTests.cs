using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// read-only modules (ROActivities, backgrounds) cannot write STORAGE files, change SQL databases or mount songs
	public class ReadOnlyModuleTests : IDisposable {
		private readonly string dir = Path.Combine(Path.GetTempPath(), "ro_module_" + Guid.NewGuid().ToString("N"));

		public ReadOnlyModuleTests() => Directory.CreateDirectory(dir);

		public void Dispose() {
			try { Directory.Delete(dir, true); } catch (IOException) { }
		}

		[Fact]
		public void Storage_WritesOnlyWhenWritable() {
			Assert.False(new LuaStorageFunc(dir, writable: false).WriteText("ro.txt", "x"));
			Assert.False(File.Exists(Path.Combine(dir, "ro.txt")));
			Assert.False(new LuaStorageFunc(dir, writable: false).WriteLobbyCode("ro.txt", "x"));

			Assert.True(new LuaStorageFunc(dir).WriteText("rw.txt", "x"));
			Assert.Equal("x", File.ReadAllText(Path.Combine(dir, "rw.txt")));
		}

		[Fact]
		public void Sql_ReadsButDoesNotWrite_WhenReadOnly() {
			var writable = new LuaSQLFunc(dir).OpenSQLDatabase("test.db3");
			writable.Query("CREATE TABLE items (name TEXT); INSERT INTO items VALUES ('a');");

			var readOnly = new LuaSQLFunc(dir, writable: false).OpenSQLDatabase("test.db3");
			readOnly.Query("INSERT INTO items VALUES ('b');");
			var rows = readOnly.Query("SELECT name FROM items;");
			Assert.Single(rows);
			Assert.Equal("a", rows[1]["name"]);

			// a missing database is not created
			readOnly = new LuaSQLFunc(dir, writable: false).OpenSQLDatabase("missing.db3");
			Assert.Empty(readOnly.Query("SELECT 1;"));
			Assert.False(File.Exists(Path.Combine(dir, "missing.db3")));
		}

		[Fact]
		public void DanBuilder_DoesNotMount_WhenReadOnly() {
			var builder = new LuaDanBuildFunc(writable: false);
			builder.AddSong(new LuaSongNode(), 3);
			Assert.False(builder.Mount());
		}

		[Fact]
		public void SongNodes_DoNotMount_WhileAReadOnlyScriptRuns() {
			var running = typeof(CLuaScript).GetField("_running", BindingFlags.NonPublic | BindingFlags.Static)!;
			Assert.False(CLuaScript.BlockedInReadOnly("test"));

			running.SetValue(null, RuntimeHelpers.GetUninitializedObject(typeof(CLuaROActivityScript)));
			try {
				Assert.True(CLuaScript.BlockedInReadOnly("test"));
				Assert.False(new LuaSongNode().Mount());
				Assert.False(new LuaSongNode().MountIfNotLocked());
			} finally {
				running.SetValue(null, null);
			}
		}
	}
}
