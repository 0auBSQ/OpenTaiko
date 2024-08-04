using NLua;

namespace OpenTaiko {
	internal class CLuaModalScript : CLuaScript {
		private LuaFunction lfRegisterModal;
		private LuaFunction lfAnimationFinished;
		private LuaFunction lfUpdate;
		private LuaFunction lfDraw;

		public CLuaModalScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets) {

			lfRegisterModal = (LuaFunction)LuaScript["registerNewModal"];
			lfAnimationFinished = (LuaFunction)LuaScript["isAnimationFinished"];
			lfUpdate = (LuaFunction)LuaScript["update"];
			lfDraw = (LuaFunction)LuaScript["draw"];
		}

		// Function to retrieve if the currently playing modal animation (etc) finished playing, allowing to send the next modal
		public bool AnimationFinished() {
			if (!Available) return false;
			bool result = (bool)RunLuaCode(lfAnimationFinished)[0];
			return result;
		}

		// Informations of the newly added modal are initialized here
		public void RegisterNewModal(int player, int rarity, Modal.EModalType modalType, params object?[] args) {
			if (!Available) return;

			object[] newParams = new object[] { player, rarity, (int)modalType };
			if (args != null) newParams = newParams.Concat((object[])args).ToArray();
			RunLuaCode(lfRegisterModal, newParams);
		}

		// Handle inputs here (if necessary, like to add a shortcut to accelerate the animation etc
		public void Update(params object[] args) {
			if (!Available) return;

			RunLuaCode(lfUpdate, args);
		}

		public void Draw(params object[] args) {
			if (!Available) return;

			RunLuaCode(lfDraw, args);
		}
	}
}
