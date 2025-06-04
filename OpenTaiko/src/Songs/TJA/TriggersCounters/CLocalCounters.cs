namespace OpenTaiko {
	internal class CLocalCounters {
		private Dictionary<string, double> _localCounters;
		private int _player;

		public CLocalCounters() {
			this._localCounters = new Dictionary<string, double>();
			this._player = -1;
		}

		public CLocalCounters(int player) {
			this._localCounters = new Dictionary<string, double>();
			this._player = player;
		}

		public void Store(string counterName, string expr) {
			this._localCounters[counterName] = CTExpression.Evaluate(expr, _player);
		}

		public void Elevate(string counterName) {
			if (_player < 0) return;
			if (OpenTaiko.ConfigIni.bAutoPlay[_player] || (OpenTaiko.ConfigIni.bAIBattleMode && _player == 1)) return;

			double _val = this.Get(counterName);
			SaveFile _sf = OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(_player)];
			_sf.tSetGlobalCounter(counterName, _val);
		}

		public double Get(string counterName) {
			this._localCounters.TryGetValue(counterName, out double counterValue);
			return counterValue;
		}
	}
}
