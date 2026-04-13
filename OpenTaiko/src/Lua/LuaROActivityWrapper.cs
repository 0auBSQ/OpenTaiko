namespace OpenTaiko {
	/// <summary>
	/// Lua-facing accessor for looking up ROActivities by name.
	/// Exposed as the <c>ROACTIVITY</c> global in all Lua scripts.
	/// </summary>
	public class LuaROActivityFunc {
		public LuaROActivityWrapper? GetROActivity(string name) =>
			LuaROActivityWrapper.GetROActivity(name);
	}

	/// <summary>
	/// Wraps a <see cref="CLuaROActivityScript"/> loaded from <c>Modules/ROActivities/{name}/Script.lua</c>.
	/// Scripts in ROActivities receive read-only views of CONFIG and GetSaveFile — any attempt
	/// to write through those globals produces an error rather than modifying game state.
	/// </summary>
	public class LuaROActivityWrapper {
		public static Dictionary<string, LuaROActivityWrapper> _allROActivities = new Dictionary<string, LuaROActivityWrapper>();

		#region [Static management]

		public static void ResetROActivityDictionary() {
			foreach (var pair in _allROActivities)
				pair.Value.DisposeActivity();
			_allROActivities.Clear();
		}

		public static LuaROActivityWrapper? GetROActivity(string name) {
			_allROActivities.TryGetValue(name, out var act);
			return act;
		}

		public static void PropagateAfterSongEnumEvent() {
			foreach (var pair in _allROActivities)
				pair.Value.AfterSongsEnum();
		}

		public static void PropagateOnStart() {
			foreach (var pair in _allROActivities)
				pair.Value.OnStart();
		}

		public static void PropagateOnDestroy() {
			foreach (var pair in _allROActivities)
				pair.Value.OnDestroy();
		}

		#endregion

		private CLuaROActivityScript lcActScript;

		public void DisposeActivity() {
			lcActScript?.Dispose();
		}

		public LuaROActivityWrapper(string name) {
			lcActScript = new CLuaROActivityScript(CSkin.Path($"Modules/ROActivities/{name}"), name);
			_allROActivities[name] = this;
		}

		#region [Standard lifecycle events]

		public bool IsActive => lcActScript?.IsActive() ?? false;

		public object[]? Activate(params object[] args) => lcActScript?.Activate(args);
		public object[]? Deactivate(params object[] args) => lcActScript?.Deactivate(args);
		public object[]? Draw(params object[] args) => lcActScript?.Draw(args);
		public object[]? Update(params object[] args) => lcActScript?.Update(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond, args);

		#endregion

		#region [Generic Lua function call]

		/// <summary>
		/// Calls a named Lua function defined in this ROActivity's script.
		/// </summary>
		public object[]? Call(string functionName, params object[] args) => lcActScript?.CallFunction(functionName, args);

		#endregion

		#region [Extra skin events]

		private void OnStart() => lcActScript?.OnStart();
		private void AfterSongsEnum() => lcActScript?.AfterSongsEnum();
		private void OnDestroy() => lcActScript?.OnDestroy();

		#endregion
	}
}
