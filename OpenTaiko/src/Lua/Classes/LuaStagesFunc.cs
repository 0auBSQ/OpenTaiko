using NLua;

namespace OpenTaiko {
	// The STAGES global: a stage declares the namespace other stages list it under, with an info table of
	// its own choosing (a title, a description...) that comes back with each listed stage
	public class LuaStagesFunc {
		private const int MaxInfoDepth = 8;

		private readonly Lua _lua;
		private readonly bool _fromGlobal;
		private readonly CLuaStageScript? _stage;

		internal LuaStagesFunc(Lua lua, bool fromGlobal, CLuaStageScript? stage) {
			_lua = lua;
			_fromGlobal = fromGlobal;
			_stage = stage;
		}

		// Sets the namespace of the calling stage and its info (plain data: strings, numbers, booleans and tables
		// of them); nil or "" clears it. Works from the top of Script.lua and from any callback. Only a stage has
		// a namespace: in another module it does nothing and returns false.
		public bool SetNamespace(string? ns, LuaTable? info = null) {
			if (_stage == null) return false;
			_stage.StageNamespace = ns ?? "";
			_stage.StageNamespaceInfo = (ns ?? "") == "" ? null : CopyTable(info, 0);
			return true;
		}

		// The stages of a namespace, one per name: the stage Exit("stage", name) from this module would open,
		// listed when that stage declared the namespace. An array of { name, isGlobal, info }, ordered by name.
		public LuaTable ListNamespace(string ns) {
			LuaTable list = NewTable();
			long i = 1;
			foreach (var stage in LuaStageWrapper.GetStagesInNamespace(ns ?? "", _fromGlobal)) {
				LuaTable entry = NewTable();
				entry["name"] = stage.customStageName;
				entry["isGlobal"] = stage.IsGlobal;
				entry["info"] = stage.NamespaceInfo != null ? ToTable(stage.NamespaceInfo, 0) : NewTable();
				list[i++] = entry;
			}
			return list;
		}

		private LuaTable NewTable() => (LuaTable)_lua.DoString("return {}")[0];

		// a Lua table copied as plain data, so another stage's Lua state can read it
		internal static Dictionary<object, object>? CopyTable(LuaTable? table, int depth) {
			if (table == null || depth >= MaxInfoDepth) return null;
			var copy = new Dictionary<object, object>();
			foreach (object key in table.Keys) {
				object? k = PlainKey(key);
				if (k == null) continue;
				object? v = table[key] switch {
					string s => s,
					bool b => b,
					long l => l,
					double d => d,
					LuaTable t => CopyTable(t, depth + 1),
					_ => null
				};
				if (v != null) copy[k] = v;
			}
			return copy;
		}

		private static object? PlainKey(object key) => key switch {
			string s => s,
			long l => l,
			double d when d == Math.Floor(d) && Math.Abs(d) < 1e15 => (long)d,
			_ => null
		};

		private LuaTable ToTable(Dictionary<object, object> data, int depth) {
			LuaTable t = NewTable();
			foreach (var (k, v) in data) {
				if (v is Dictionary<object, object> sub) {
					if (depth + 1 < MaxInfoDepth) t[k] = ToTable(sub, depth + 1);
				} else {
					t[k] = v;
				}
			}
			return t;
		}
	}
}
