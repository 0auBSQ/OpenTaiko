using NLua;

namespace OpenTaiko {
	public class LuaCounter {
		public double Begin;
		public double End;
		public double Interval;
		private LuaFunction? lfEnded;
		private List<Action<double>> lfListeners = [];

		public double Value;

		private bool Ticking;

		public LuaCounter(double begin, double end, double interval, LuaFunction? ended = null) {
			Begin = begin;
			End = end;
			Interval = interval;

			lfEnded = ended;
		}

		public void Start() {
			Ticking = true;
		}

		public void Stop() {
			Ticking = false;
		}

		public void Reset() {
			Value = 0;
		}

		public void Listen(Action<double> listener) {
			lfListeners.Add(listener);
		}

		public void Tick() {
			if (!Ticking || Value == End) return;



			if (Interval == 0) {
				Value = End;
				Ended();

				return;
			}

			double nextValue = Value;
			double add = 1.0f / Interval * OpenTaiko.FPS.DeltaTime;
			if (End > Begin) {
				nextValue += add;
			} else {
				nextValue -= add;
			}

			if (nextValue >= End) {
				Value = End;
				Ended();

				return;
			}

			Value = nextValue;

			lfListeners.ForEach((listener) => listener(Value));
		}

		private void Ended() {
			lfListeners.ForEach((listener) => listener(Value));
			lfEnded?.Call();
		}
	}

	public class LuaCounterFunc {
		public LuaCounterFunc() { }

		public LuaCounter CreateCounter(double begin, double end, double interval, LuaFunction? ended = null) {
			return new LuaCounter(begin, end, interval, ended);
		}
	}
}
