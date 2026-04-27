using NLua;

namespace OpenTaiko;

internal class AnimeBG : ScriptBG {
	private NamedLuaFunction LuaPlayAnimation = new("playAnime");
	public AnimeBG(string filePath) : base(filePath) {
		LuaPlayAnimation.Load(LuaScript);
	}
	public new void Dispose() {
		base.Dispose();
		LuaPlayAnimation.Dispose();
	}

	public void PlayAnimation() => RunLuaCode(LuaPlayAnimation);
}
