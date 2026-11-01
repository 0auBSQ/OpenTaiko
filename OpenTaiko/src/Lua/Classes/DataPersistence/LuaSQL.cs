using Microsoft.Data.Sqlite;

namespace OpenTaiko {
	public class LuaSQL {
		private readonly string _connectionString;

		// a read-only connection fails, with an error, on any statement that writes
		public LuaSQL(string path, bool readOnly = false) {
			_connectionString = readOnly ? @$"Data Source={path};Mode=ReadOnly" : @$"Data Source={path}";
		}

		public Dictionary<int, Dictionary<string, object?>> Query(string query) {
			try {
				using (var connection = new SqliteConnection(_connectionString)) {
					connection.Open();

					var result = new Dictionary<int, Dictionary<string, object?>>();
					var b = result.Values;

					var command = connection.CreateCommand();
					command.CommandText = query;

					var reader = command.ExecuteReader();
					int rowIndex = 1; // Lua tables are 1-indexed

					while (reader.Read()) {
						var row = new Dictionary<string, object?>();
						for (int i = 0; i < reader.FieldCount; i++) {
							string colName = reader.GetName(i);
							object? val = reader.IsDBNull(i) ? null : reader.GetValue(i);
							row[colName] = val;
						}
						result[rowIndex++] = row;
					}

					return result;
				}
			} catch (Exception ex) {
				LogNotification.PopError(ex.ToString());
				return new Dictionary<int, Dictionary<string, object?>>();
			}
		}
	}

	public class LuaSQLFunc {
		private string DirPath;
		// false in read-only modules, whose databases open read-only
		private readonly bool _writable;

		public LuaSQLFunc(string dirPath, bool writable = true) {
			DirPath = dirPath;
			_writable = writable;
		}

		public LuaSQL OpenSQLDatabase(string path) {
			string full_path = $@"{DirPath}{Path.DirectorySeparatorChar}{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}";
			return new LuaSQL(full_path, readOnly: !_writable);
		}
	}
}
