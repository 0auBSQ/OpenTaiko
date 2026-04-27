using NLua;

namespace OpenTaiko;

internal class KusudamaScript : ScriptBG {
	private LuaFunction LuaKusuIn;
	private LuaFunction LuaKusuBroke;
	private LuaFunction LuaKusuMiss;

	public KusudamaScript(string filePath) : base(filePath) {
		if (LuaScript != null) {
			LuaKusuIn = LuaScript.GetFunction("kusuIn");
			LuaKusuBroke = LuaScript.GetFunction("kusuBroke");
			LuaKusuMiss = LuaScript.GetFunction("kusuMiss");
		}
	}

	public new void Dispose() {
		base.Dispose();
		LuaKusuIn?.Dispose();
		LuaKusuBroke?.Dispose();
		LuaKusuMiss?.Dispose();
	}

	public void KusuIn() => RunLuaCode(LuaKusuIn);
	public void KusuBroke() => RunLuaCode(LuaKusuBroke);
	public void KusuMiss() => RunLuaCode(LuaKusuMiss);
}
