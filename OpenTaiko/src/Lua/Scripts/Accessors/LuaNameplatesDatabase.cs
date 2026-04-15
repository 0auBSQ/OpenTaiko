using NLua;

namespace OpenTaiko {
	/// <summary>
	/// Lua-accessible database of all nameplate unlockables.
	/// Exposed as the <c>NAMEPLATESLIST</c> global in every Lua script.
	/// </summary>
	public class LuaNameplatesDatabase {
		/// <summary>Total number of nameplates in the database.</summary>
		public int Count => OpenTaiko.Databases.DBNameplateUnlockables.data.Count;

		/// <summary>Returns the nameplate at the given 0-based position in the database, or nil if out of range.</summary>
		public LuaNameplateInfo? GetByIndex(int index) {
			var keys = OpenTaiko.Databases.DBNameplateUnlockables.data.Keys;
			if (index < 0 || index >= keys.Count) return null;
			int id = (int)keys.ElementAt(index);
			return new LuaNameplateInfo(OpenTaiko.Databases.DBNameplateUnlockables.data[(Int64)id], id);
		}

		/// <summary>Returns the nameplate with the given ID, or nil if not found.</summary>
		public LuaNameplateInfo? GetById(int id) {
			if (OpenTaiko.Databases.DBNameplateUnlockables.data.TryGetValue(id, out var npu))
				return new LuaNameplateInfo(npu, id);
			return null;
		}

		/// <summary>Returns all nameplates in the database as a list.</summary>
		public List<LuaNameplateInfo> GetAll() {
			return OpenTaiko.Databases.DBNameplateUnlockables.data
				.Select(kv => new LuaNameplateInfo(kv.Value, (int)kv.Key))
				.ToList();
		}

		/// <summary>
		/// Returns all nameplates for which <paramref name="predicate"/> returns true.
		/// The predicate receives a <see cref="LuaNameplateInfo"/> and must return a boolean.
		/// </summary>
		public List<LuaNameplateInfo> FindWhere(LuaFunction predicate) {
			var result = new List<LuaNameplateInfo>();
			foreach (var kv in OpenTaiko.Databases.DBNameplateUnlockables.data) {
				var info = new LuaNameplateInfo(kv.Value, (int)kv.Key);
				var ret = predicate.Call(info);
				if (ret != null && ret.Length > 0 && ret[0] is bool b && b)
					result.Add(info);
			}
			return result;
		}
	}
}
