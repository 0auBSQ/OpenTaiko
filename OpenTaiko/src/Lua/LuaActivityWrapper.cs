namespace OpenTaiko {
	public class LuaActivityWrapper {
		// Used to search activities in the global activities dictionary from lua stages
		public static Dictionary<string, LuaActivityWrapper> _allLuaActivities = new Dictionary<string, LuaActivityWrapper>();

		#region [Setters]

		public static void ResetLuaActivityDictionary() {
			foreach (KeyValuePair<string, LuaActivityWrapper> _act in _allLuaActivities) {
				_act.Value.DisposeActivity();
			}
			_allLuaActivities.Clear();
		}

		#endregion

		#region [Getters]

		public static LuaActivityWrapper? GetLuaActivity(string name) {
			if (_allLuaActivities.TryGetValue(name, out var _act)) {
				return _act;
			}
			return null;
		}

		#endregion

		#region [Executers]

		public static void PropagateAfterSongEnumEvent() {
			foreach (KeyValuePair<string, LuaActivityWrapper> _act in _allLuaActivities) {
				_act.Value.AfterSongsEnum();
			}
		}

		public static void PropagateOnStart() {
			foreach (KeyValuePair<string, LuaActivityWrapper> _act in _allLuaActivities) {
				_act.Value.OnStart();
			}
		}

		public static void PropagateOnDestroy() {
			foreach (KeyValuePair<string, LuaActivityWrapper> _act in _allLuaActivities) {
				_act.Value.OnDestroy();
			}
		}

		#endregion

		private CLuaActivityScript lcActScript;

		public void DisposeActivity() {
			lcActScript?.Dispose();
		}

		public LuaActivityWrapper(string name, bool isGlobal = false) {
			if (isGlobal == false) lcActScript = new CLuaActivityScript(CSkin.Path($"Modules/Activities/{name}"), name);
			else lcActScript = new CLuaActivityScript(CSkin.Path($"Global/Activities/{name}"), $"[GLOBAL]{name}");

			_allLuaActivities.Add(name, this);

		}

		#region [Events]

		public object[]? Activate(object[] args) {
			return lcActScript?.Activate(args);
		}

		public object[]? Deactivate(object[] args) {
			return lcActScript?.Deactivate(args);
		}

		public object[]? Draw(object[] args) {
			return lcActScript?.Draw(args);
		}

		public object[]? Update(object[] args) {
			return lcActScript?.Update(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond, args);
		}

		#endregion

		#region [Events not present on CStage/CActivity]

		// Executes **Just after** loading the skin, once the readme notice appears, also executes everytime the skin is reloaded
		private void OnStart() {
			lcActScript?.OnStart();
		}


		// Executes everytime songs enum is done, including soft/hard reload and at start
		private void AfterSongsEnum() {
			lcActScript?.AfterSongsEnum();
		}

		// Executes before skin change, in order to deallocate any ressources carried by the skin's Lua modules
		private void OnDestroy() {
			lcActScript?.OnDestroy();
		}


		#endregion
	}

	public class LuaActivityFunc {
		public LuaActivityFunc() { }

		public LuaActivityWrapper? GetActivity(string name) {
			return LuaActivityWrapper.GetLuaActivity(name);
		}

	}
}
