using NLua;

namespace OpenTaiko;

/// <summary>
/// A <see cref="CLuaROActivityScript"/> specialised for backgrounds. Backgrounds push a per-frame <c>state</c> table
/// into Lua <c>update(timestamp, state)</c>; the inherited <see cref="CLuaActivityScript.Update"/> can't be used for
/// that because it takes <c>(long timestamp, params object[] args)</c> and forwards <c>args</c> (already an array) to
/// RunLuaCode's own <c>params</c>, which nests it as a single element — so Lua would receive <c>state</c> wrapped in a
/// one-element array instead of the object itself. <see cref="UpdateState"/> instead passes the timestamp and the
/// state as two BARE objects, which RunLuaCode collects into one flat array, so Lua gets <c>update(timestamp, state)</c>
/// with the state passed directly. (activate/draw already pass their single argument flat via the base class.)
/// </summary>
internal class CLuaBackgroundScript : CLuaROActivityScript {
	private NamedLuaFunction lfUpdateState = new("update");

	public CLuaBackgroundScript(string dir, string name) : base(dir, name) {
		lfUpdateState.Load(LuaScript);
	}

	/// <summary>Call Lua <c>update(timestamp, state)</c> with <paramref name="state"/> passed directly (not nested).</summary>
	public void UpdateState(long timestamp, object state) => RunLuaCode(lfUpdateState, timestamp, state);
}
