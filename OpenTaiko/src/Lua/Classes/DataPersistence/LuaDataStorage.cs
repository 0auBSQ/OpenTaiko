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
}
