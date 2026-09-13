using System.Collections.Generic;
using System.Reflection;

namespace OpenTaiko {
	/// <summary>
	/// Snapshot of an Effects.json object (a character or puchichara effect class) as a dictionary keyed by
	/// field name, so Lua reads effects by name and any effect added later shows up without a new binding.
	/// </summary>
	internal static class LuaEffectDict {
		public static Dictionary<string, object> From(object? effect) {
			var dict = new Dictionary<string, object>();
			if (effect == null) return dict;
			foreach (var f in effect.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)) {
				var v = f.GetValue(effect);
				if (v != null) dict[f.Name] = v;
			}
			return dict;
		}
	}
}
