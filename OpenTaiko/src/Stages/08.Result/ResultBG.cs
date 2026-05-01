using NLua;

namespace OpenTaiko;

class ResultBG : ScriptBG {
	private NamedLuaFunction LuaSkipAnimation = new("skipAnime");

	public ResultBG(string filePath) : base(filePath) {
		LuaSkipAnimation.Load(LuaScript);
	}

	public new void Dispose() {
		base.Dispose();
		LuaSkipAnimation.Dispose();
	}

	public void SkipAnimation() => RunLuaCode(LuaSkipAnimation);
}
