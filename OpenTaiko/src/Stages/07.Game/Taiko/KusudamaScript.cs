using NLua;

namespace OpenTaiko;

internal class KusudamaScript : ScriptBG {
	private NamedLuaFunction LuaKusuIn = new("kusuIn");
	private NamedLuaFunction LuaKusuBroke = new("kusuBroke");
	private NamedLuaFunction LuaKusuMiss = new("kusuMiss");

	public KusudamaScript(string filePath) : base(filePath) {
		LuaKusuIn.Load(LuaScript);
		LuaKusuBroke.Load(LuaScript);
		LuaKusuMiss.Load(LuaScript);
	}

	public new void Dispose() {
		base.Dispose();
		LuaKusuIn.Dispose();
		LuaKusuBroke.Dispose();
		LuaKusuMiss.Dispose();
	}

	public void KusuIn() => RunLuaCode(LuaKusuIn);
	public void KusuBroke() => RunLuaCode(LuaKusuBroke);
	public void KusuMiss() => RunLuaCode(LuaKusuMiss);
}
