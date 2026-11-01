using System.Text;
using LightningDB;

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
	/// <br/>
	/// Read-only base of <see cref="LuaDataStorage"/>. <see cref="Write"/> is a no-op that logs an error.
	/// Used inside ROActivity scripts to prevent persistent writes.
	/// </summary>
	public class LuaRODataStorage : IDisposable {
		protected readonly string Path;

		private static void BlockWrite(string method) =>
			LogNotification.PopError($"[ROActivity] 'DATABASE.{method}' is a write operation and is not allowed in a read-only module.");

		public LuaRODataStorage AsReadOnly() => new(this.Path);

		public LuaRODataStorage(string path) {
			// Remap to the writable location: a no-op on desktop, the writable Documents mirror on iOS (the
			// app bundle is read-only there). LMDB needs the environment directory to exist before Open().
			Path = OpenTaiko.ResolveWritePath(path);
			try { System.IO.Directory.CreateDirectory(Path); }
			catch (Exception ex) { LogNotification.PopError($"Failed to init the database: {ex.Message}"); }
		}

		public virtual void Write(string key, string value) => BlockWrite(nameof(Write));

		public string? Read(string key) {
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
		}


		public void Dispose() { }   // nothing persistent is held; each operation disposes its own environment
	}

	/// <summary>
	/// Variant of <see cref="LuaDataStorageFunc"/> that returns <see cref="LuaRODataStorage"/> instances.
	/// Registered as the <c>DATABASE</c> global inside ROActivity scripts.
	/// </summary>
	public class LuaRODataStorageFunc {
		protected string DirPath;

		public LuaRODataStorageFunc AsReadOnly() => new(this.DirPath);

		public LuaRODataStorageFunc(string dirPath) {
			DirPath = dirPath;
		}

		protected string GetLocalDatabasePath(string path)
			=> $@"{DirPath}{Path.DirectorySeparatorChar}{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}";
		protected string GetGlobalDatabasePath(string path)
			=> DataPath.GetAbsoluteDataPath($@"LMDB/{path}");

		public virtual LuaRODataStorage OpenLocalDatabase(string path) => new(GetLocalDatabasePath(path));
		public virtual LuaRODataStorage OpenGlobalDatabase(string path) => new (GetGlobalDatabasePath(path));
	}

	/// <summary>
	/// Writable derivation of <see cref="LuaRODataStorage"/>.
	/// </summary>
	public class LuaDataStorage : LuaRODataStorage {
		public LuaDataStorage(string path) : base(path) { }

		public override void Write(string key, string value) {
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
		}

	}

	/// <summary>
	/// Variant of <see cref="LuaRODataStorageFunc"/> that returns <see cref="LuaDataStorageFunc"/> instances.
	/// Registered as the <c>DATABASE</c> global inside Activity scripts.
	/// </summary>
	public class LuaDataStorageFunc : LuaRODataStorageFunc {
		public LuaDataStorageFunc(string dirPath) : base(dirPath) { }

		public override LuaDataStorage OpenLocalDatabase(string path) => new(GetLocalDatabasePath(path));
		public override LuaDataStorage OpenGlobalDatabase(string path) => new(GetGlobalDatabasePath(path));
	}
}
