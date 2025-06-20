namespace OpenTaiko {
	internal class CLocalTriggers {
		private Dictionary<string, bool> _localTriggers;
		private int _player;

		public CLocalTriggers() {
			this._localTriggers = new Dictionary<string, bool>();
			this._player = -1;
		}

		public CLocalTriggers(int player) {
			this._localTriggers = new Dictionary<string, bool>();
			this._player = player;
		}

		public void Store(string triggerName, string expr) {
			this._localTriggers[triggerName] = CTExpression.Evaluate(expr, _player) != 0;
		}

		public void Elevate(string triggerName) {
			if (_player < 0) return;
			if (OpenTaiko.ConfigIni.bAutoPlay[_player] || (OpenTaiko.ConfigIni.bAIBattleMode && _player == 1)) return;

			bool _val = this.Get(triggerName);
			SaveFile _sf = OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(_player)];
			_sf.tSetGlobalTrigger(triggerName, _val);
		}

		public bool Get(string triggerName) {
			this._localTriggers.TryGetValue(triggerName, out bool triggerValue);
			return triggerValue;
		}
	}
}
