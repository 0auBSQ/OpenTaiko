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

		private bool Loop;
		private bool Bounce;

		// For null-safety
		public LuaCounter() {
			Begin = 0;
			End = 0;
			Interval = 0;
			Value = 0;

			lfEnded = null;
			Loop = false;
			Bounce = false;

			Reset();
		}

		public LuaCounter(double begin, double end, double interval, LuaFunction? ended = null) {
			Begin = begin;
			End = end;
			Interval = interval;
			Value = Begin;

			lfEnded = ended;
			Loop = false;
			Bounce = false;

			Reset();
		}

		public void SetLoop(bool loop) {
			Loop = loop;
			Bounce = false;
		}

		public void SetBounce(bool bounce) {
			Bounce = bounce;
			Loop = false;
		}

		public void Start() {
			Reset();
			Ticking = true;
		}

		public void Resume() {
			Ticking = true;
		}

		public void Stop() {
			Ticking = false;
		}

		public void Reset() {
			Value = Begin;
		}

		public void Listen(Action<double> listener) {
			lfListeners.Add(listener);
		}

		public void ClearListeners() {
			lfListeners.Clear();
		}

		public void Tick() {
			if (!Ticking) return;


			// Specific when the counter is invalid (interval 0 or begin and end being the same)
			// End directly without taking into account the Looping/Bouncing state
			if (Interval == 0 || Begin == End) {
				DoEnd();
				return;
			}

			FixIntegrity();

			double nextValue = Value;
			double add = 1.0 / Interval * OpenTaiko.FPS.DeltaTime;

			nextValue += add;

			if (ValueReachedEnd(nextValue)) {
				if (Loop == true) DoLoop(nextValue);
				else if (Bounce == true) DoBounce(nextValue);
				else {
					DoEnd();
					return;
				}

			} else {
				Value = nextValue;
			}

			lfListeners.ForEach((listener) => listener(Value));
		}

		private void FixIntegrity() {
			if (Interval == 0) return;
			if (Interval > 0 && End >= Begin) return;
			if (Interval < 0 && Begin >= End) return;
			(Begin, End) = (End, Begin);
		}


		private bool ValueReachedEnd(double val) {
			if (End == Begin) return true;
			if (End > Begin) {
				return val >= End;
			}
			return val <= End;
		}

		private void DoLoop(double value) {
			double min = Math.Min(Begin, End);
			double max = Math.Max(Begin, End);
			double range = max - min;

			// If inside the interval already, keep it as-is (prevents mangling when Begin > End)
			if (value >= min && value <= max) {
				Value = value;
				return;
			}

			// Wrap into [min, max) robustly for arbitrary overshoot (positive or negative)
			double relative = value - min;
			double mod = ((relative % range) + range) % range; // [0, range)

			// Calculate how many complete loops occurred
			int loops = (int)Math.Abs(relative / range);

			Value = min + mod;

			// Call ended callback for each complete loop
			for (int i = 0; i < loops; i++) {
				lfEnded?.Call();
			}
		}

		private void DoBounce(double value) {
			double min = Math.Min(Begin, End);
			double max = Math.Max(Begin, End);
			double length = max - min;

			double shifted = value - min;
			double period = 2 * length;

			// Normalize into [0, 2*length)
			double mod = shifted % period;
			if (mod < 0) mod += period;

			// Calculate how many complete bounces occurred
			int bounces = (int)Math.Abs(shifted / length);

			// Bounce fold
			double folded;
			bool bounced;
			if (mod <= length) {
				folded = mod;
				bounced = false;
			} else {
				folded = period - mod;
				bounced = true;
			}

			Value = min + folded;

			// Flip sign if we landed in a reflected segment
			if (bounced) {
				Interval = -Interval;
			}

			// Call ended callback for each complete bounce
			for (int i = 0; i < bounces; i++) {
				lfEnded?.Call();
			}
		}


		private void DoEnd() {
			Value = End;
			lfListeners.ForEach((listener) => listener(Value));
			lfEnded?.Call();
			Stop();
		}
	}

	public class LuaCounterFunc {
		public LuaCounterFunc() { }

		public LuaCounter CreateCounter(double begin, double end, double interval, LuaFunction? ended = null) {
			return new LuaCounter(begin, end, interval, ended);
		}

		public LuaCounter CreateCounterDuration(double begin, double end, double seconds, LuaFunction? ended = null) {
			if (seconds <= 0) return EmptyCounter();
			if (begin == end) return EmptyCounter();

			double interval = seconds / Math.Abs(end - begin);
			if (end < begin) interval = -interval;
			return new LuaCounter(begin, end, interval, ended);
		}


		public LuaCounter EmptyCounter() {
			return new LuaCounter();
		}
	}
}
