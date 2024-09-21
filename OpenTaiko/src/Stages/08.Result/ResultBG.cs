using NLua;

namespace OpenTaiko {
	class ResultBG : ScriptBG {
		private LuaFunction LuaSkipAnimation;

		public ResultBG(string filePath) : base(filePath) {
			if (LuaScript != null) {
				LuaSkipAnimation = LuaScript.GetFunction("skipAnime");
			}
		}

		public new void Dispose() {
			base.Dispose();
			LuaSkipAnimation?.Dispose();
		}

		public void SkipAnimation() {
			if (LuaScript == null) return;
			try {
				LuaSkipAnimation.Call();
			} catch (Exception ex) {
			}
		}
	}
}
