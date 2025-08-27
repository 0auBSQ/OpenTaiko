using Microsoft.Data.Sqlite;

namespace OpenTaiko {
	public class LuaSQL {
		private readonly string _connectionString;

		public LuaSQL(string path) {
			_connectionString = @$"Data Source={path}";
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

		public LuaSQLFunc(string dirPath) {
			DirPath = dirPath;
		}

		public LuaSQL OpenSQLDatabase(string path) {
			string full_path = $@"{DirPath}{Path.DirectorySeparatorChar}{path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)}";
			return new LuaSQL(full_path);
		}
	}
}
