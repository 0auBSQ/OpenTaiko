using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Globalization;
using NLua;

namespace OpenTaiko {
	public class LuaCounter {
		public float Begin;
		public float End;
		public float Interval;
		private LuaFunction? lfEnded;

		public float Value;

		private bool Ticking;

		public LuaCounter(float begin, float end, float interval, LuaFunction? ended = null) {
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

		public void Tick() {
			if (!Ticking || Value == End) return;

			if (Interval == 0) {
				Value = End;
				Ended();

				return;
			}

			float nextValue = Value;
			float add = 1.0f / Interval * (float)OpenTaiko.FPS.DeltaTime;
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
		}

		private void Ended() {
			lfEnded?.Call();
		}
	}

	public class LuaCounterFunc {
		public LuaCounterFunc() { }

		public LuaCounter CreateCounter(float begin, float end, float interval, LuaFunction? ended = null) {
			return new LuaCounter(begin, end, interval, ended);
		}
	}
}
