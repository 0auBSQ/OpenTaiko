namespace OpenTaiko {
	public class LuaActivityWrapper : LuaROActivityWrapper {
		// Used to search activities in the global activities dictionary from lua stages
		// The skin's activities and the global ones (Global/Modules/Activities) are kept apart, as a name can be in both
		public static Dictionary<string, LuaActivityWrapper> _skinLuaActivities = new Dictionary<string, LuaActivityWrapper>();
		public static Dictionary<string, LuaActivityWrapper> _globalLuaActivities = new Dictionary<string, LuaActivityWrapper>();

		private static IEnumerable<LuaActivityWrapper> _allLuaActivities => _skinLuaActivities.Values.Concat(_globalLuaActivities.Values);

		// All global operations except querying is also performed on the global ROActivities dictionary
		#region [Setters]

		public static void ResetLuaActivityDictionary() {
			ResetROActivityDictionary();
			foreach (LuaActivityWrapper _act in _allLuaActivities) {
				_act.DisposeActivity();
			}
			_skinLuaActivities.Clear();
			_globalLuaActivities.Clear();
		}

		#endregion

		#region [Getters]

		// An activity by name: the skin's first, unless preferGlobal (the lookup comes from a global module)
		public static LuaActivityWrapper? GetLuaActivity(string name, bool preferGlobal = false) {
			var (first, second) = preferGlobal ? (_globalLuaActivities, _skinLuaActivities) : (_skinLuaActivities, _globalLuaActivities);
			if (first.TryGetValue(name, out var _act) || second.TryGetValue(name, out _act)) {
				return _act;
			}
			return null;
		}

		#endregion

		#region [Executers]

		public static new void PropagateAfterSongEnumEvent() {
			LuaROActivityWrapper.PropagateAfterSongEnumEvent();
			foreach (LuaActivityWrapper _act in _allLuaActivities) {
				_act.AfterSongsEnum();
			}
		}

		public static new void PropagateOnDestroy() {
			LuaROActivityWrapper.PropagateOnDestroy();
			foreach (LuaActivityWrapper _act in _allLuaActivities) {
				_act.OnDestroy();
			}
		}

		#endregion

		public LuaActivityWrapper(string name, bool isGlobal = false) {
			IsGlobal = isGlobal;
			if (isGlobal == false) lcActScript = new CLuaActivityScript(CSkin.Path($"Modules/Activities/{name}"), name);
			else lcActScript = new CLuaActivityScript(CSkin.GlobalModulePath("Activities", name), $"[GLOBAL]{name}");

			(isGlobal ? _globalLuaActivities : _skinLuaActivities).Add(name, this);

		}
		}

	public class LuaActivityFunc : LuaROActivityFunc {
		public LuaActivityFunc(bool fromGlobal = false) : base(fromGlobal) { }

		public LuaActivityWrapper? GetActivity(string name) {
			return LuaActivityWrapper.GetLuaActivity(name, _fromGlobal);
		}

	}
}
