using NLua;

namespace OpenTaiko {
	class EndAnimeScript : ScriptBG {
		private LuaFunction LuaPlayEndAnime;

		public EndAnimeScript(string filePath) : base(filePath) {
			if (LuaScript != null) {
				LuaPlayEndAnime = LuaScript.GetFunction("playEndAnime");
			}
		}

		public new void Dispose() {
			base.Dispose();
			LuaPlayEndAnime?.Dispose();
		}

		public void PlayEndAnime(int player) {
			if (LuaScript == null) return;
			try {
				LuaPlayEndAnime.Call(player);
			} catch (Exception ex) {
			}
		}

		public new void Update(int player) {
			if (LuaScript == null) return;
			try {
				float currentFloorPositionMax140 = 0;

				if (OpenTaiko.stageSongSelect.rChoosenSong.arスコア[5] != null) {
					int maxFloor = OpenTaiko.stageSongSelect.rChoosenSong.arスコア[5].譜面情報.nTotalFloor;
					int nightTime = Math.Max(140, maxFloor / 2);

					currentFloorPositionMax140 = Math.Min(OpenTaiko.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] / (float)nightTime, 1f);
				}

				LuaUpdateValues.Call(OpenTaiko.FPS.DeltaTime, OpenTaiko.FPS.NowFPS, OpenTaiko.stage演奏ドラム画面.bIsAlreadyCleared, (double)currentFloorPositionMax140);
				/*LuaScript.SetObjectToPath("fps", TJAPlayer3.FPS.n現在のFPS);
                LuaScript.SetObjectToPath("deltaTime", TJAPlayer3.FPS.DeltaTime);
                LuaScript.SetObjectToPath("isClear", TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared);
                LuaScript.SetObjectToPath("towerNightOpacity", (double)(255 * currentFloorPositionMax140));*/
				if (!OpenTaiko.stage演奏ドラム画面.bPAUSE) LuaUpdate.Call(player);
			} catch (Exception ex) {
				LuaScript.Dispose();
				LuaScript = null;
			}
		}
		public new void Draw(int player) {
			if (LuaScript == null) return;
			try {
				LuaDraw.Call(player);
			} catch (Exception ex) {
				LuaScript.Dispose();
				LuaScript = null;
			}
		}
	}
}
