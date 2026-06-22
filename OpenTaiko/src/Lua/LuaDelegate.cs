using NLua;

namespace OpenTaiko {
	// Converts Lua functions to .NET delegates WITHOUT NLua's Reflection.Emit delegate generation,
	// which is unavailable under iOS AOT (PlatformNotSupportedException from NLua.CodeGeneration). NLua
	// passes a LuaFunction parameter directly (no codegen), so the Lua-facing APIs take LuaFunction and
	// convert here via ordinary lambdas (compiled at build time, so AOT-safe).
	internal static class LuaDelegate {
		// Wrap a Lua function as an Action<T>. Null fn => null (for optional callbacks).
		public static Action<T>? AsAction<T>(LuaFunction? fn)
			=> fn == null ? null : (arg) => fn.Call(arg);

		// Wrap a Lua function as a bool predicate. Lua truthiness: a returned value that is neither
		// nil nor false counts as true.
		public static Func<T, bool>? AsPredicate<T>(LuaFunction? fn)
			=> fn == null ? null : (arg) => {
				var r = fn.Call(arg);
				return r is { Length: > 0 } && r[0] is not (null or false);
			};
	}
}
