using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace OpenTaikoTests {
	public class ReplayDeterminismTests {
		const long GREAT = 25, GOOD = 75, BAD = 108;
		readonly record struct Note(long T, bool Don);
		readonly record struct In(long T, bool Don);
		readonly record struct Roll(long Start, long End);
		enum Mode { Precise, OldOrder, Fix }

		static int JudgeBad(List<Note> notes, List<In> inputsIn, List<Roll> rolls, Mode mode, double frameStep, double phase) {
			var hit = new bool[notes.Count]; var missed = new bool[notes.Count];
			int late = 0, cursor = 0;
			var inputs = inputsIn.OrderBy(i => i.T).ToList();
			bool InRoll(long t) { foreach (var r in rolls) if (t >= r.Start - 17 && t < r.End) return true; return false; }
			void AutoMiss(double clock) { for (int i = 0; i < notes.Count; i++) if (!hit[i] && !missed[i] && clock - notes[i].T > BAD) missed[i] = true; }
			void Judge(In inp) {
				if (InRoll(inp.T)) return;                         // roll tap: never hits a note
				int bi = -1;
				for (int i = 0; i < notes.Count; i++) {
					if (hit[i] || missed[i] || notes[i].Don != inp.Don) continue;   // colour must match
					if (notes[i].T > inp.T + BAD) break;
					if (Math.Abs(inp.T - notes[i].T) <= BAD && (bi < 0 || notes[i].T < notes[bi].T)) bi = i;
				}
				if (bi >= 0) { hit[bi] = true; if (Math.Abs(inp.T - notes[bi].T) > GOOD) late++; }
			}
			void Pump(double clock, bool interleave) {
				while (cursor < inputs.Count && inputs[cursor].T <= clock) {
					var inp = inputs[cursor++];
					if (interleave) AutoMiss(inp.T);   // resolve misses up to this hit's exact time first
					Judge(inp);
				}
			}
			if (mode == Mode.Precise) {
				foreach (var inp in inputs) { AutoMiss(inp.T); Judge(inp); }   // frame-independent reference
			} else {
				double end = notes[^1].T + 1000;
				for (double clock = phase; clock <= end; clock += Math.Max(1, frameStep)) {
					if (clock < 0) continue;
					if (mode == Mode.Fix) { Pump(clock, true); AutoMiss(clock); }   // interleaved pump BEFORE auto-miss
					else { AutoMiss(clock); Pump(clock, false); }                    // OLD: auto-miss THEN batch pump
				}
				Pump(double.MaxValue, mode == Mode.Fix); AutoMiss(double.MaxValue);
			}
			return late + (notes.Count - hit.Count(h => h));
		}

		static readonly double[] Cadences = { 4, 8, 12, 16.7, 20, 25, 33.3, 50, 66, 80, 100 };

		// a rich, drift-prone slice: close notes (within a bad window of neighbours), both colours, a late hit, a
		// never-hit note, and a roll whose taps must not clear notes.
		static readonly List<Note> RICH = new() {
			new(1000,true), new(1080,false), new(1160,true), new(1240,false), new(1320,true), new(1400,false),
			new(1480,true), new(1560,false), new(2000,true), new(2080,false), new(2160,true), new(2240,false),
		};
		static readonly List<Roll> RICH_ROLLS = new() { new(1650, 1950) };
		static readonly List<In> RICH_IN = new() {
			new(1000,true), new(1082,false), new(1160,true), new(1240,false), new(1330,false), new(1400,false),
			new(1478,true), new(1700,true), new(1780,false), new(1860,true),
			new(2000,true), new(2085,false), new(2160,true), new(2242,false),
		};

		// minimal guaranteed-drift case: two same-colour notes; input 1050 is in-window for BOTH. Precise clears
		// both (1000 Good, 1100 Great -> Bad 0). If a frame overshoots 1000's bad boundary (1108) before the inputs
		// are pumped, the old order auto-misses 1000, the 1050 input re-targets 1100, and 1000 -> Bad (drift).
		static readonly List<Note> DRIFT = new() { new(1000,true), new(1100,true) };
		static readonly List<In> DRIFT_IN = new() { new(1050,true), new(1100,true) };
		static readonly List<Roll> NONE = new();

		[Fact]
		public void Fix_ReproducesPreciseResult_AtEveryCadenceAndPhase() {
			int precise = JudgeBad(RICH, RICH_IN, RICH_ROLLS, Mode.Precise, 0, 0);
			foreach (var f in Cadences)
				for (double phase = 0; phase < f; phase += 3) {
					int got = JudgeBad(RICH, RICH_IN, RICH_ROLLS, Mode.Fix, f, phase);
					Assert.True(got == precise, $"fix drifted at {f}ms/frame phase {phase}: precise={precise}, fix={got}");
				}
		}

		[Fact]
		public void OldOrder_DriftsFromPrecise_AtSomeCadence() {
			int precise = JudgeBad(DRIFT, DRIFT_IN, NONE, Mode.Precise, 0, 0);
			bool drifts = false;
			foreach (var f in Cadences)
				for (double phase = 0; phase < f && !drifts; phase += 1)
					if (JudgeBad(DRIFT, DRIFT_IN, NONE, Mode.OldOrder, f, phase) != precise) drifts = true;
			Assert.True(drifts, "expected the old order (auto-miss then batch pump) to drift from precise for some cadence/phase");
		}

		// and the fix stays correct on that same drift-prone case, at every cadence/phase
		[Fact]
		public void Fix_HoldsOnTheDriftCase() {
			int precise = JudgeBad(DRIFT, DRIFT_IN, NONE, Mode.Precise, 0, 0);
			foreach (var f in Cadences)
				for (double phase = 0; phase < f; phase += 1)
					Assert.Equal(precise, JudgeBad(DRIFT, DRIFT_IN, NONE, Mode.Fix, f, phase));
		}
	}
}
