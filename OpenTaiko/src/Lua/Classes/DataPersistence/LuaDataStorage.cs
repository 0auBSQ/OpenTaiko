using System.Text;
using LightningDB;

namespace OpenTaiko {
	public class LuaDataStorage : IDisposable {
		private string Path;
		private LightningEnvironment? LEnv;

		private LightningEnvironment? InitEnvironment(string path) {
			LightningEnvironment? env = null;

			try {
				env = new LightningEnvironment(path);
				env.Open();

				using (var tx = env.BeginTransaction())
				using (tx.OpenDatabase(configuration: new DatabaseConfiguration {
					Flags = DatabaseOpenFlags.Create
				})) {
					tx.Commit();
				}
			} catch (Exception ex) {
				LogNotification.PopError($"Failed to init the database: {ex.Message}");
				env?.Dispose();
				env = null;
			}

			return env;
		}

		public LuaDataStorage(string path) {
			Path = path;
			LEnv = InitEnvironment(path);
		}

		public void Write(string key, string value) {
			if (LEnv == null) {
				LogNotification.PopError($"Failed to write the value '{value}' to the entry '{key}': The LightningEnvironment failed to setup");
				return;
			}
			using (var tx = LEnv.BeginTransaction())
			using (var db = tx.OpenDatabase(configuration: new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create })) {
				// Put a key-value pair into the database
				tx.Put(db, Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value));
				tx.Commit();
			}
		}

		public string? Read(string key) {
			if (LEnv == null) {
				LogNotification.PopError($"Failed to read the entry '{key}': The LightningEnvironment failed to setup");
				return null;
			}

			// Begin a read-only transaction to retrieve the value
			using (var tx = LEnv.BeginTransaction(TransactionBeginFlags.ReadOnly))
			using (var db = tx.OpenDatabase()) {
				var (resultCode, _key, value) = tx.Get(db, Encoding.UTF8.GetBytes(key));
				if (resultCode == MDBResultCode.Success) {
					return Encoding.UTF8.GetString(value.AsSpan());
				} else {
					return null;
				}
			}
		}

		public void Dispose() {
			LEnv?.Dispose();
		}
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
