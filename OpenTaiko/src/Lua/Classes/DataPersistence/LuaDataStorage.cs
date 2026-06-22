using System.Text;
#if !BUILDING_AS_LIBRARY
using LightningDB;
#endif

namespace OpenTaiko {
	/// <summary>
	/// Lua-facing key/value store backed by LMDB. Each operation opens its OWN short-lived
	/// <see cref="LightningEnvironment"/> and disposes it immediately (via <c>using</c>). This is
	/// deliberate: LightningDB's <c>LightningEnvironment</c> finalizer THROWS
	/// ("The LightningEnvironment was not disposed and cannot be reliably dealt with from the finalizer")
	/// when an environment is garbage-collected without being disposed, which crashes the whole process.
	/// Lua scripts obtain these handles via <c>DATABASE:OpenLocalDatabase(...)</c> and have no reliable
	/// way to dispose them (NLua just drops the reference, and the GC then finalizes the env), so keeping
	/// a long-lived environment was a latent process-killer. Opening per operation keeps every environment
	/// deterministically disposed and also avoids LMDB's "same env opened twice in one process" hazard.
	/// These stores see only occasional reads/writes (save-on-change, mission/track load), so the
	/// per-operation open cost is negligible.
	/// </summary>
	public class LuaDataStorage : IDisposable {
		private readonly string Path;

		public LuaDataStorage(string path) {
			Path = path;
#if !BUILDING_AS_LIBRARY
			// LMDB needs the environment directory to exist before Open(); create it once up front so the
			// first read of a brand-new store doesn't error. iOS has no LMDB (Read/Write are no-ops), so
			// skip the directory creation — it would otherwise hit the read-only app bundle on device.
			try { System.IO.Directory.CreateDirectory(path); }
			catch (Exception ex) { LogNotification.PopError($"Failed to init the database: {ex.Message}"); }
#endif
		}

		public void Write(string key, string value) {
#if !BUILDING_AS_LIBRARY
			try {
				using var env = new LightningEnvironment(Path);
				env.Open();
				using var tx = env.BeginTransaction();
				using var db = tx.OpenDatabase(configuration: new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create });
				tx.Put(db, Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value));
				tx.Commit();
			} catch (Exception ex) {
				LogNotification.PopError($"Failed to write the value '{value}' to the entry '{key}': {ex.Message}");
			}
#else
			// iOS: LMDB has no iOS-native binary; persist as a JSON key/value file under the writable
			// Documents area (the app bundle is read-only on device). Used by e.g. the boot stage's
			// "new_user" flag, so this MUST persist — a no-op would trap boot in first-time setup forever.
			try {
				var all = tLoadAll();
				all[key] = value;
				string dir = OpenTaiko.ResolveWritePath(Path);
				System.IO.Directory.CreateDirectory(dir);
				System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "store.json"), Newtonsoft.Json.JsonConvert.SerializeObject(all));
			} catch (Exception ex) {
				LogNotification.PopError($"Failed to write the value '{value}' to the entry '{key}': {ex.Message}");
			}
#endif
		}

		public string? Read(string key) {
#if !BUILDING_AS_LIBRARY
			try {
				using var env = new LightningEnvironment(Path);
				env.Open();
				using var tx = env.BeginTransaction(TransactionBeginFlags.ReadOnly);
				using var db = tx.OpenDatabase();
				var (resultCode, _key, value) = tx.Get(db, Encoding.UTF8.GetBytes(key));
				return resultCode == MDBResultCode.Success ? Encoding.UTF8.GetString(value.AsSpan()) : null;
			} catch (Exception ex) {
				LogNotification.PopError($"Failed to read the entry '{key}': {ex.Message}");
				return null;
			}
#else
			try {
				return tLoadAll().TryGetValue(key, out var v) ? v : null;
			} catch { return null; }
#endif
		}

#if BUILDING_AS_LIBRARY
		// iOS JSON backing store: one store.json per database dir, under the writable Documents area.
		private System.Collections.Generic.Dictionary<string, string> tLoadAll() {
			try {
				string file = System.IO.Path.Combine(OpenTaiko.ResolveWritePath(Path), "store.json");
				if (System.IO.File.Exists(file))
					return Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(System.IO.File.ReadAllText(file)) ?? new System.Collections.Generic.Dictionary<string, string>();
			} catch { }
			return new System.Collections.Generic.Dictionary<string, string>();
		}
#endif

		public void Dispose() { }   // nothing persistent is held; each operation disposes its own environment
	}

	public class LuaDataStorageFunc {
		private string DirPath;

		public LuaDataStorageFunc(string dirPath) {
			DirPath = dirPath;
		}

		public LuaDataStorage OpenLocalDatabase(string path) {
			string full_path = $@"{DirPath}{Path.DirectorySeparatorChar}{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}";
			return new LuaDataStorage(full_path);
		}

		public LuaDataStorage OpenGlobalDatabase(string path) {
			string full_path = DataPath.GetAbsoluteDataPath($@"LMDB/{path}");
			return new LuaDataStorage(full_path);
		}
	}

	/// <summary>
	/// Read-only view of <see cref="LuaDataStorage"/>. <see cref="Write"/> is a no-op that logs an error.
	/// Used inside ROActivity scripts to prevent persistent writes.
	/// </summary>
	public class LuaRODataStorage : LuaDataStorage {
		public LuaRODataStorage(string path) : base(path) { }

		private static void BlockWrite(string method) =>
			LogNotification.PopError($"[ROActivity] '{method}' is a write operation and is not allowed in a read-only module.");

		public new void Write(string key, string value) => BlockWrite("DATABASE.Write");
	}

	/// <summary>
	/// Variant of <see cref="LuaDataStorageFunc"/> that returns <see cref="LuaRODataStorage"/> instances.
	/// Registered as the <c>DATABASE</c> global inside ROActivity scripts.
	/// </summary>
	public class LuaRODataStorageFunc : LuaDataStorageFunc {
		private string DirPath;

		public LuaRODataStorageFunc(string dirPath) : base(dirPath) {
			DirPath = dirPath;
		}

		public new LuaRODataStorage OpenLocalDatabase(string path) {
			string full_path = $@"{DirPath}{Path.DirectorySeparatorChar}{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}";
			return new LuaRODataStorage(full_path);
		}

		public new LuaRODataStorage OpenGlobalDatabase(string path) {
			string full_path = DataPath.GetAbsoluteDataPath($@"LMDB/{path}");
			return new LuaRODataStorage(full_path);
		}
	}
}
