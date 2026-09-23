using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
			public List<(double Re, double Im)> NoteScrolls = new();
			public double MsPerBeat => 60000.0 / this.Bpm;

			// the visual beat (in 16ths) at a quarter-note beat: the hispeed integrated from beat 0
			public (double X, double Y) Beat16(double beat) {
				double x = 0, y = 0;
				for (int i = 0; i < this.Segments.Count; i++) {
					double from = this.Segments[i].FromBeat;
					double to = (i + 1 < this.Segments.Count) ? this.Segments[i + 1].FromBeat : double.PositiveInfinity;
					double len = Math.Min(beat, to) - from;
					if (len <= 0) continue;
					x += 4 * len * this.Segments[i].Re;
					y += 4 * len * this.Segments[i].Im;
				}
				return (x, y);
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
				r.NoteScrolls.Add((s[0].GetDouble(), s[1].GetDouble()));
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
				var (x, y) = r.Beat16(beat);
				AssertClose(beat * r.MsPerBeat, dons[i].dbSoundTimems, $"note {i} time", 0.01);
				AssertClose(x, dons[i].fBMSCROLLTime, $"note {i} beat X");
				AssertClose(y, dons[i].fBMSCROLLTimeY, $"note {i} beat Y");
			}

			// every note keeps its own scroll next to the hispeed (the burst notes are half a millisecond apart, so
			// the TaikoJiro-like compats, which take a note's scroll from the timing point at its time, keep them too)
			for (int i = 0; i < dons.Count; i++) {
				AssertClose(r.NoteScrolls[i].Re, dons[i].dbSCROLL, $"note {i} scroll");
				AssertClose(r.NoteScrolls[i].Im, dons[i].dbSCROLL_Y, $"note {i} scroll Y");
			}

			// at mid-circle the notes form the Desmos rosette: each note's own scroll times its beat difference
			{
				double t0 = 12 * r.MsPerBeat;
				var point = CStagePlayScreenCommon.GetNowPBPMPoint(tja, t0, CTja.ECourse.eNormal);
				var (bx, by) = CStagePlayScreenCommon.GetNowPBMTime(point, t0, tja.COMPAT);
				for (int i = 26; i < dons.Count; i++) {
					var (dx, dy) = (dons[i].fBMSCROLLTime - bx, dons[i].fBMSCROLLTimeY - by);
					var (sx, sy) = r.NoteScrolls[i];
					var (px, py) = NotesManager.ComplexN4Beats(dx, dy, dons[i].dbSCROLL, dons[i].dbSCROLL_Y, EScrollMode.HBScroll);
					AssertClose((sx * dx - sy * dy) / 16, px, $"note {i} rosette X");
					AssertClose((sx * dy + sy * dx) / 16, py, $"note {i} rosette Y");
				}
				// the note at beat 12 sits on the judge mark, its neighbours do not
				AssertClose(0, dons[42].fBMSCROLLTime - bx, "beat-12 note X");
				AssertClose(0, dons[42].fBMSCROLLTimeY - by, "beat-12 note Y");
				Assert.True(Math.Abs(dons[41].fBMSCROLLTime - bx) + Math.Abs(dons[41].fBMSCROLLTimeY - by) > 0.1, "the note before beat 12 is off the judge mark");
			}

			// the played beat at any time follows the same integral, on the real timing points
			foreach (double beat in new[] { 0.5, 1.5, 2.5, 4, 5.5, 7.5, 9, 10.3, 12.1, 13.9, 15 }) {
				double ms = beat * r.MsPerBeat;
				var point = CStagePlayScreenCommon.GetNowPBPMPoint(tja, ms, CTja.ECourse.eNormal);
				var (px, py) = CStagePlayScreenCommon.GetNowPBMTime(point, ms, tja.COMPAT);
				var (x, y) = r.Beat16(beat);
				AssertClose(x, px, $"played beat X at {beat}");
				AssertClose(y, py, $"played beat Y at {beat}");
			}

			// the hispeed in effect at a note is the segment's
			foreach (var (i, beat) in r.NoteBeats.Select((b, i) => (i, b)).Where(t => t.b > 0)) {
				var seg = r.Segments.Last(s => s.FromBeat <= beat + 1e-9);
				AssertClose(seg.Re, dons[i].dbHISPEED, $"note {i} hispeed");
				AssertClose(seg.Im, dons[i].dbHISPEED_Y, $"note {i} hispeed Y");
			}
		}

		[Fact]
		public void HispeedAndScrollActTogether() {
			var tja = Parse("TITLE:both\nBPM:120\nCOURSE:Oni\n#HBSCROLL\n#START\n#HISPEED 2\n1,\n#SCROLL 2\n1,\n#HISPEED(1.5)\n1,\n#SCROLL 1+1i\n1,\n#HISPEED -i\n1,\n#END\n");
			var dons = Dons(tja);
			Assert.Equal(5, dons.Count);
			// (hispeed, scroll) per note: neither command touches the other
			var want = new (double hs, double hsY, double sc, double scY)[] { (2, 0, 1, 0), (2, 0, 2, 0), (1.5, 0, 2, 0), (1.5, 0, 1, 1), (0, -1, 1, 1) };
			for (int i = 0; i < want.Length; i++) {
				AssertClose(want[i].hs, dons[i].dbHISPEED, $"note {i} hispeed");
				AssertClose(want[i].hsY, dons[i].dbHISPEED_Y, $"note {i} hispeed Y");
				AssertClose(want[i].sc, dons[i].dbSCROLL, $"note {i} scroll");
				AssertClose(want[i].scY, dons[i].dbSCROLL_Y, $"note {i} scroll Y");
			}
			// the beats: a 4/4 measure is 16 sixteenths at hispeed 1, whatever the scroll
			double[] wantX = { 0, 32, 64, 88, 112 };
			double[] wantY = { 0, 0, 0, 0, 0 };
			for (int i = 0; i < 5; i++) {
				AssertClose(wantX[i], dons[i].fBMSCROLLTime, $"note {i} beat X");
				AssertClose(wantY[i], dons[i].fBMSCROLLTimeY, $"note {i} beat Y");
			}
			// under #HISPEED -i the beat runs down the imaginary axis: 16 sixteenths later it is at -16i
			var point = CStagePlayScreenCommon.GetNowPBPMPoint(tja, dons[4].dbSoundTimems + 2000, CTja.ECourse.eNormal);
			var (px, py) = CStagePlayScreenCommon.GetNowPBMTime(point, dons[4].dbSoundTimems + 2000, tja.COMPAT);
			AssertClose(112, px, "played beat X under -i");
			AssertClose(-16, py, "played beat Y under -i");
		}

		[Fact]
		public void PositionIsTheComplexProductOfScrollAndBeat() {
			// scroll × Δbeat with Δbeat = 16 sixteenths (one measure): i turns a real beat up the imaginary axis
			var (x, y) = NotesManager.ComplexN4Beats(16, 0, 0, 1, EScrollMode.HBScroll);
			AssertClose(0, x, "i × 16"); AssertClose(1, y, "i × 16");
			// and an imaginary beat (complex #HISPEED) back onto the real axis, the other way
			(x, y) = NotesManager.ComplexN4Beats(0, 16, 0, 1, EScrollMode.HBScroll);
			AssertClose(-1, x, "i × 16i"); AssertClose(0, y, "i × 16i");
			// a general product
			(x, y) = NotesManager.ComplexN4Beats(16, 16, 2, 1, EScrollMode.HBScroll);
			AssertClose((2 * 16 - 1 * 16) / 16.0, x, "(2+i)(16+16i) re"); AssertClose((2 * 16 + 1 * 16) / 16.0, y, "(2+i)(16+16i) im");
			// BMScroll ignores the scroll but keeps the beat's imaginary part
			(x, y) = NotesManager.ComplexN4Beats(16, 8, 3, 3, EScrollMode.BMScroll);
			AssertClose(1, x, "bmscroll re"); AssertClose(0.5, y, "bmscroll im");
		}
	}
}
