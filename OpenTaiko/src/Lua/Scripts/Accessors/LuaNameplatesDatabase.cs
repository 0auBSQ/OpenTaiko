using NLua;

namespace OpenTaiko {
	/// <summary>
	/// Lua-accessible database of all nameplate unlockables.
	/// Exposed as the <c>NAMEPLATESLIST</c> global in every Lua script.
	/// </summary>
	public class LuaNameplatesDatabase {
		private static DBNameplateUnlockables Db => OpenTaiko.Databases.DBNameplateUnlockables;

		/// <summary>Total number of nameplates in the database.</summary>
		public int Count => Db.KeysInOrder.Count;

		/// <summary>Returns the nameplate at the given 0-based position in the database, or nil if out of range.</summary>
		public LuaNameplateInfo? GetByIndex(int index) {
			if (index < 0 || index >= Db.KeysInOrder.Count) return null;
			string id = Db.KeysInOrder[index];
			return new LuaNameplateInfo(Db.data[id], id);
		}

		/// <summary>Returns the nameplate with the given id (a string, or a whole number for the shipped ones), or nil if not found.</summary>
		public LuaNameplateInfo? GetById(object id) {
			string key = DBNameplateUnlockables.tKey(id);
			if (Db.data.TryGetValue(key, out var npu))
				return new LuaNameplateInfo(npu, key);
			return null;
		}

		/// <summary>Returns all nameplates in the database as a list.</summary>
		public List<LuaNameplateInfo> GetAll() {
			return Db.KeysInOrder
				.Select(id => new LuaNameplateInfo(Db.data[id], id))
				.ToList();
		}

		/// <summary>
		/// Returns all nameplates for which <paramref name="predicate"/> returns true.
		/// The predicate receives a <see cref="LuaNameplateInfo"/> and must return a boolean.
		/// </summary>
		public List<LuaNameplateInfo> FindWhere(LuaFunction predicate) {
			var result = new List<LuaNameplateInfo>();
			foreach (string id in Db.KeysInOrder) {
				var info = new LuaNameplateInfo(Db.data[id], id);
				var ret = predicate.Call(info);
				if (ret != null && ret.Length > 0 && ret[0] is bool b && b)
					result.Add(info);
			}
			return result;
		}
	}
}
