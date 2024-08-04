using NLua;

namespace OpenTaiko {
	internal class AnimeBG : ScriptBG {
		private LuaFunction LuaPlayAnimation;
		public AnimeBG(string filePath) : base(filePath) {
			if (LuaScript != null) {
				LuaPlayAnimation = LuaScript.GetFunction("playAnime");
			}
		}
		public new void Dispose() {
			base.Dispose();
			LuaPlayAnimation?.Dispose();
		}

		public void PlayAnimation() {
			if (LuaScript == null) return;
			try {
				LuaPlayAnimation.Call();
			} catch (Exception ex) {
			}
		}
	}
}
