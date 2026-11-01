using NLua;

namespace OpenTaiko {
	/// <summary>
	/// Like <see cref="CLuaActivityScript"/> but loads the read-only <c>CONFIG</c> and <c>GetSaveFile</c> globals.
	/// The loading happens in the base constructor so that all called Lua functions see the RO versions.
	/// </summary>
	public class CLuaROActivityScript : CLuaActivityScriptBase {
		public CLuaROActivityScript(string dir, string name) : base(dir, name, writable: false) { }
	}
}
