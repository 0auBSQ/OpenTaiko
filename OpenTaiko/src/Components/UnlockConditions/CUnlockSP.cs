namespace OpenTaiko {
	internal class CUnlockSP : CUnlockCondition {


		public CUnlockSP(CUnlockConditionFactory.UnlockConditionJsonRaw rawJson) : base(rawJson) {
			this.RequiredArgCount = 2;
			this.ConditionId = "sp";
		}

		public override (bool, string?) tConditionMet(int player, EScreen screen = EScreen.MyRoom) {
			if (this.Values.Length % this.RequiredArgCount == 0
						&& this.Reference.Length == this.Values.Length / this.RequiredArgCount) {
				int _satisfactoryPlays = this.tGetCountChartsPassingCondition(player);

				bool fulfiled = this.tValueRequirementMet(_satisfactoryPlays, this.Reference.Length);

				if (screen == EScreen.Internal) {
					return (fulfiled, "");
				} else {
					return (fulfiled, null);
				}
			} else
				return (false, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR2", this.ConditionId, this.RequiredArgCount.ToString()));
		}

		public override string tConditionMessage(EScreen screen = EScreen.MyRoom) {
			if (!(this.Values.Length % this.RequiredArgCount == 0
						&& this.Reference.Length == this.Values.Length / this.RequiredArgCount))
				return CLangManager.LangInstance.GetString("UNLOCK_CONDITION_ERROR2", this.ConditionId, this.RequiredArgCount.ToString());

			// Only the player loaded as 1P can check unlockables in real time
			var SaveData = OpenTaiko.SaveFileInstances[OpenTaiko.SaveFile].data;
			var ChartStats = SaveData.bestPlaysStats;

			// Check distinct plays
			List<string> _rows = new List<string>();
			var _challengeCount = this.Values.Length / this.RequiredArgCount;

			var _count = 0;
			for (int i = 0; i < _challengeCount; i++) {
				int _base = i * this.RequiredArgCount;
				string _songId = this.Reference[i];
				var _aimedDifficulty = this.Values[_base];
				var _aimedStatus = this.Values[_base + 1];

				var diffString = CLangManager.LangInstance.GetDifficulty(_aimedDifficulty);
				var statusString = GetRequiredClearStatus(_aimedStatus);
				var _songName = CSongDict.tGetNodeFromID(_songId)?.ldTitle.GetString("") ?? "[Not found]";

				_rows.Add(CLangManager.LangInstance.GetString("UNLOCK_CONDITION_CHALLENGE_PLAYDIFF", statusString, _songName, diffString));


				// Safisfied count
				if (_aimedDifficulty >= (int)Difficulty.Easy && _aimedDifficulty <= (int)Difficulty.Edit) {
					string key = _songId + _aimedDifficulty.ToString();
					var _cht = SaveData.bestPlaysDistinctCharts.TryGetValue(key, out var value) ? value : null;
					if (_cht != null && _cht.ClearStatus + 1 >= _aimedStatus) _count++;

				} else if (_aimedDifficulty < (int)Difficulty.Easy) {
					for (int diff = (int)Difficulty.Easy; diff <= (int)Difficulty.Edit; diff++) {
						string key = _songId + diff.ToString();
						var _cht = SaveData.bestPlaysDistinctCharts.TryGetValue(key, out var value) ? value : null;
						if (_cht != null && _cht.ClearStatus + 1 >= _aimedStatus) {
							_count++;
							break;
						}
					}
				}
			}

			// Push front
			_rows.Insert(0, CLangManager.LangInstance.GetString("UNLOCK_CONDITION_CHALLENGE", _count, _challengeCount));
			return String.Join("\n", _rows);
		}

		protected override int tGetCountChartsPassingCondition(int player) {
			var bpDistinctCharts = OpenTaiko.SaveFileInstances[player].data.bestPlaysDistinctCharts;
			var chartStats = OpenTaiko.SaveFileInstances[player].data.bestPlaysStats;

			var _count = 0;
			for (int i = 0; i < this.Values.Length / this.RequiredArgCount; i++) {
				int _base = i * this.RequiredArgCount;
				string _songId = this.Reference[i];
				var _aimedDifficulty = this.Values[_base];
				var _aimedStatus = this.Values[_base + 1];

				if (_aimedDifficulty >= (int)Difficulty.Easy && _aimedDifficulty <= (int)Difficulty.Edit) {
					string key = _songId + _aimedDifficulty.ToString();
					var _cht = bpDistinctCharts.TryGetValue(key, out var value) ? value : null;
					if (_cht != null && _cht.ClearStatus + 1 >= _aimedStatus) _count++;

				} else if (_aimedDifficulty < (int)Difficulty.Easy) {
					for (int diff = (int)Difficulty.Easy; diff <= (int)Difficulty.Edit; diff++) {
						string key = _songId + diff.ToString();
						var _cht = bpDistinctCharts.TryGetValue(key, out var value) ? value : null;
						if (_cht != null && _cht.ClearStatus + 1 >= _aimedStatus) {
							_count++;
							break;
						}
					}
				}
			}
			return _count;
		}
	}
}
