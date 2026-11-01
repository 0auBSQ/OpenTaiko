using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// #HISPEED: the HBScroll visual beat advances at hispeed × BPM, hispeed real or complex. TjaCases/hispeed.tja
	// follows the Desmos example (complex #HISPEED, lx6skvqfjm); hispeed.json carries the example's hispeed
	// segments and note beats, from which the reference beat function is integrated here.
	[Collection("tja")]
	public class HiSpeedTests : IClassFixture<TjaFixture> {
		public HiSpeedTests(TjaFixture _) { }

		private static string CasesDir => Path.Combine(AppContext.BaseDirectory, "TjaCases");

		private sealed record Segment(double FromBeat, double Re, double Im);

		private sealed class Reference {
			public double Bpm;
			public List<Segment> Segments = new();
			public List<double> NoteBeats = new();
			public List<Complex> NoteScrolls = new();
			public double MsPerBeat => 60000.0 / this.Bpm;

			// the visual beat (in 16ths) at a quarter-note beat: the hispeed integrated from beat 0
			public Complex Beat16(double beat) {
				double re = 0, im = 0;
				for (int i = 0; i < this.Segments.Count; i++) {
					double from = this.Segments[i].FromBeat;
					double to = (i + 1 < this.Segments.Count) ? this.Segments[i + 1].FromBeat : double.PositiveInfinity;
					double len = Math.Min(beat, to) - from;
					if (len <= 0) continue;
					re += 4 * len * this.Segments[i].Re;
					im += 4 * len * this.Segments[i].Im;
				}
				return new(re, im);
			}
		}

		private static Reference LoadReference() {
			using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(CasesDir, "hispeed.json")));
			var h = doc.RootElement.GetProperty("hispeed");
			var r = new Reference { Bpm = h.GetProperty("bpm").GetDouble() };
			foreach (var s in h.GetProperty("segments").EnumerateArray())
				r.Segments.Add(new Segment(s[0].GetDouble(), s[1].GetDouble(), s[2].GetDouble()));
			foreach (var b in h.GetProperty("noteBeats").EnumerateArray())
				r.NoteBeats.Add(b.GetDouble());
			foreach (var s in h.GetProperty("noteScrolls").EnumerateArray())
				r.NoteScrolls.Add(new(s[0].GetDouble(), s[1].GetDouble()));
			return r;
		}

		private static CTja Parse(string tjaText) {
			string dir = Path.Combine(Path.GetTempPath(), "ot_tja_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			try {
				string p = Path.Combine(dir, "chart.tja");
				File.WriteAllText(p, tjaText);
				OpenTaiko.OpenTaiko.ConfigIni.nPlayerCount = 1;
				var tja = new CTja();
				tja.Activate();
				tja.tInput(p, Difficulty.Oni, 0, true, 0);
				return tja;
			} finally { try { Directory.Delete(dir, true); } catch { } }
		}

		private static List<CChip> Dons(CTja tja)
			=> tja.listChip.Where(c => c.nChannelNo == 0x11).OrderBy(c => c.idxDefine).ToList();

		private static void AssertClose(double expected, double actual, string what, double tol = 1e-6)
			=> Assert.True(Math.Abs(expected - actual) <= tol, $"{what}: expected {expected}, got {actual}");

		// the chart is TMG (imaginary axis up, as the Desmos draws it); the default compat must agree on every beat
		[Theory]
		[InlineData("COMPAT:TMG")]
		[InlineData("COMPAT:OOS")]
		public void DesmosPath_NoteBeatsFollowTheHispeedIntegral(string compatHeader) {
			var r = LoadReference();
			string source = File.ReadAllText(Path.Combine(CasesDir, "hispeed.tja"));
			Assert.Contains("COMPAT:TMG", source);
			var tja = Parse(source.Replace("COMPAT:TMG", compatHeader));
			Assert.Equal(compatHeader == "COMPAT:TMG" ? CTja.ETjaCompat.TMG : CTja.ETjaCompat.OOS, tja.COMPAT);
			var dons = Dons(tja);
			Assert.Equal(r.NoteBeats.Count, dons.Count);

			for (int i = 0; i < dons.Count; i++) {
				double beat = r.NoteBeats[i];
				var vb = r.Beat16(beat);
				AssertClose(beat * r.MsPerBeat, dons[i].dbSoundTimems, $"note {i} time", 0.01);
				AssertClose(vb.Real, dons[i].fBMSCROLLTime.Real, $"note {i} beat Re");
				AssertClose(vb.Imaginary, dons[i].fBMSCROLLTime.Imaginary, $"note {i} beat Im");
			}

			// every note keeps its own scroll next to the hispeed (the burst notes are half a millisecond apart, so
			// the TaikoJiro-like compats, which take a note's scroll from the timing point at its time, keep them too)
			for (int i = 0; i < dons.Count; i++) {
				AssertClose(r.NoteScrolls[i].Real, dons[i].dbSCROLL.Real, $"note {i} scroll Re");
				AssertClose(r.NoteScrolls[i].Imaginary, dons[i].dbSCROLL.Imaginary, $"note {i} scroll Im");
			}

			// at mid-circle the notes form the Desmos rosette: each note's own scroll times its beat difference
			{
				double t0 = 12 * r.MsPerBeat;
				var point = CStagePlayScreenCommon.GetNowPBPMPoint(tja, t0, CTja.ECourse.eNormal);
				var b = CStagePlayScreenCommon.GetNowPBMTime(point, t0, tja.COMPAT);
				for (int i = 26; i < dons.Count; i++) {
					var d = dons[i].fBMSCROLLTime - b;
					var s = r.NoteScrolls[i];
					var p = getN4Beats_BeatBasedScroll(d, dons[i].dbSCROLL);
					var sd = s * d;
					AssertClose(sd.Real / 16, p.Real, $"note {i} rosette Re");
					AssertClose(sd.Imaginary / 16, p.Imaginary, $"note {i} rosette Im");
				}
				// the note at beat 12 sits on the judge mark, its neighbours do not
				AssertClose(0, dons[42].fBMSCROLLTime.Real - b.Real, "beat-12 note Re");
				AssertClose(0, dons[42].fBMSCROLLTime.Imaginary - b.Imaginary, "beat-12 note Im");
				Assert.True(Math.Abs(dons[41].fBMSCROLLTime.Real - b.Real) + Math.Abs(dons[41].fBMSCROLLTime.Imaginary - b.Imaginary) > 0.1, "the note before beat 12 is off the judge mark");
			}

			// the played beat at any time follows the same integral, on the real timing points
			foreach (double beat in new[] { 0.5, 1.5, 2.5, 4, 5.5, 7.5, 9, 10.3, 12.1, 13.9, 15 }) {
				double ms = beat * r.MsPerBeat;
				var point = CStagePlayScreenCommon.GetNowPBPMPoint(tja, ms, CTja.ECourse.eNormal);
				var p = CStagePlayScreenCommon.GetNowPBMTime(point, ms, tja.COMPAT);
				var vb = r.Beat16(beat);
				AssertClose(vb.Real, p.Real, $"played beat Re at {beat}");
				AssertClose(vb.Imaginary, p.Imaginary, $"played beat Im at {beat}");
			}

			// the hispeed in effect at a note is the segment's
			foreach (var (i, beat) in r.NoteBeats.Select((b, i) => (i, b)).Where(t => t.b > 0)) {
				var seg = r.Segments.Last(s => s.FromBeat <= beat + 1e-9);
				AssertClose(seg.Re, dons[i].dbHISPEED.Real, $"note {i} hispeed Re");
				AssertClose(seg.Im, dons[i].dbHISPEED.Imaginary, $"note {i} hispeed Im");
			}
		}

		[Fact]
		public void HispeedAndScrollActTogether() {
			var tja = Parse("TITLE:both\nBPM:120\nCOURSE:Oni\n#HBSCROLL\n#START\n#HISPEED 2\n1,\n#SCROLL 2\n1,\n#HISPEED 1.5\n1,\n#SCROLL 1+1i\n1,\n#HISPEED -i\n1,\n#END\n");
			var dons = Dons(tja);
			Assert.Equal(5, dons.Count);
			// (hispeed, scroll) per note: neither command touches the other
			var want = new (double hsRe, double hsIm, double scRe, double scIm)[] { (2, 0, 1, 0), (2, 0, 2, 0), (1.5, 0, 2, 0), (1.5, 0, 1, 1), (0, -1, 1, 1) };
			for (int i = 0; i < want.Length; i++) {
				AssertClose(want[i].hsRe, dons[i].dbHISPEED.Real, $"note {i} hispeed Re");
				AssertClose(want[i].hsIm, dons[i].dbHISPEED.Imaginary, $"note {i} hispeed Im");
				AssertClose(want[i].scRe, dons[i].dbSCROLL.Real, $"note {i} scroll Re");
				AssertClose(want[i].scIm, dons[i].dbSCROLL.Imaginary, $"note {i} scroll Im");
			}
			// the beats: a 4/4 measure is 16 sixteenths at hispeed 1, whatever the scroll
			double[] wantRe = { 0, 32, 64, 88, 112 };
			double[] wantIm = { 0, 0, 0, 0, 0 };
			for (int i = 0; i < 5; i++) {
				AssertClose(wantRe[i], dons[i].fBMSCROLLTime.Real, $"note {i} beat Re");
				AssertClose(wantIm[i], dons[i].fBMSCROLLTime.Imaginary, $"note {i} beat Im");
			}
			// under #HISPEED -i the beat runs down the imaginary axis: 16 sixteenths later it is at -16i
			var point = CStagePlayScreenCommon.GetNowPBPMPoint(tja, dons[4].dbSoundTimems + 2000, CTja.ECourse.eNormal);
			var p = CStagePlayScreenCommon.GetNowPBMTime(point, dons[4].dbSoundTimems + 2000, tja.COMPAT);
			AssertClose(112, p.Real, "played beat Re under -i");
			AssertClose(-16, p.Imaginary, "played beat Im under -i");
		}

		[Fact]
		public void PositionIsTheComplexProductOfScrollAndBeat() {
			// scroll × Δbeat with Δbeat = 16 sixteenths (one measure): i turns a real beat up the imaginary axis
			var b = getN4Beats_BeatBasedScroll(new(16, 0), new(0, 1));
			AssertClose(0, b.Real, "i × 16"); AssertClose(1, b.Imaginary, "i × 16");
			// and an imaginary beat (complex #HISPEED) back onto the real axis, the other way
			b = getN4Beats_BeatBasedScroll(new(0, 16), new(0, 1));
			AssertClose(-1, b.Real, "i × 16i"); AssertClose(0, b.Imaginary, "i × 16i");
			// a general product
			b = getN4Beats_BeatBasedScroll(new(16, 16), new(2, 1));
			AssertClose((2 * 16 - 1 * 16) / 16.0, b.Real, "(2+i)(16+16i) re"); AssertClose((2 * 16 + 1 * 16) / 16.0, b.Imaginary, "(2+i)(16+16i) im");
			// BMScroll ignores the scroll but keeps the beat's imaginary part
			b = getN4Beats_BeatBasedScroll(new(16, 8), new(3, 3), EScrollMode.BMScroll);
			AssertClose(1, b.Real, "bmscroll re"); AssertClose(0.5, b.Imaginary, "bmscroll im");
		}

		private static Complex getN4Beats_BeatBasedScroll(Complex th16DBeat, Complex scroll, EScrollMode eScrollMode = EScrollMode.HBScroll)
			=> NotesManager.getN4Beats(new(double.NaN, double.NaN), th16DBeat, double.NaN, scroll, eScrollMode);
	}
}
