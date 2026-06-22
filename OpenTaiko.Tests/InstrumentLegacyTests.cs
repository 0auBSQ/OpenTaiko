using System;
using System.IO;
using FDK;
using OpenTaiko;
using Xunit;

using STKEYASSIGN = OpenTaiko.CConfigIni.CKeyAssign.STKEYASSIGN;

namespace OpenTaikoTests {
	// ── Regression guards for the DTXMania instrument cleanup ─────────────────────────────────────────
	// Background: OpenTaiko inherited DTXMania's Drums/Guitar/Bass instrument model. Guitar/Bass were
	// removed, STDGBVALUE<T> was deleted (its single live slot folded into plain Taiko-only fields), and
	// the Drums/Taiko parts were merged into one Taiko part (EInstrumentPad deprecated; EKeyConfigPart kept).
	//
	// These tests pin the BEHAVIOR that has to survive all of that: key bindings still persist and load
	// per part, the part→padset→pad routing CPad relies on still resolves, the score structs still
	// validate, and TJA LEVEL still parses. They assert observable behavior (codes, devices, sections,
	// validity), not the now-removed types — so re-pointing the few renamed identifiers and staying green
	// proves the refactor was behavior-preserving.
	[Collection("tja")]   // sequential: these touch the OpenTaiko statics / temp Config.ini files
	public class InstrumentLegacyTests : IClassFixture<TjaFixture> {
		static InstrumentLegacyTests() {
			// CConfigIni reads/writes Shift_JIS; Program.Main registers this at startup, the test host doesn't.
			System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
		}
		public InstrumentLegacyTests(TjaFixture _) { }   // bootstraps OpenTaiko statics for the TJA parse test

		// ── helpers ──────────────────────────────────────────────────────────────────────────────────
		static string TempPath() =>
			Path.Combine(Path.GetTempPath(), "ot_cfg_" + Guid.NewGuid().ToString("N") + ".ini");

		// Replace every binding on a pad slot with a single known one (so the round-trip is deterministic).
		static void OnlyKey(STKEYASSIGN[] slot, InputDeviceType dev, int id, int code) {
			for (int i = 0; i < slot.Length; i++)
				slot[i] = new STKEYASSIGN(InputDeviceType.Unknown, 0, 0);
			slot[0] = new STKEYASSIGN(dev, id, code);
		}

		static void AssertKey(STKEYASSIGN[] slot, InputDeviceType dev, int id, int code) {
			Assert.Equal(dev, slot[0].InputDevice);
			Assert.Equal(id, slot[0].ID);
			Assert.Equal(code, slot[0].Code);
		}

		// CConfigIni defaults pin what the old `for (i<3) { … }` per-instrument init loop used to set —
		// guards the loop→direct-field rewrite from silently changing a startup default.
		[Fact]
		public void CConfigIni_Defaults_AreTaikoOnly() {
			var cfg = new CConfigIni();                          // ctor runs the default initialization
			Assert.False(cfg.bReverse);                          // Reverse off by default
			Assert.Equal(EJudgeTextDisplayPosition.AboveLane, cfg.JudgeTextDisplayPosition);
			Assert.Equal(10, cfg.nMinDisplayedCombo);            // default minimum displayed combo
		}

		// The core persistence path the instrument concept drove: gameplay + system + training key
		// bindings must survive a write→read round-trip with their device/id/code intact, land in the
		// right [..KeyAssign] sections, and never produce Guitar/Bass sections.
		[Fact]
		public void KeyAssign_RoundTrip_PreservesAllParts_NoGuitarBass() {
			var cfg = new CConfigIni();
			// gameplay part — distinct codes, mix of input devices, plus a 2P binding
			OnlyKey(cfg.KeyAssign.Taiko.LeftRed,   InputDeviceType.Keyboard, 0, 111);
			OnlyKey(cfg.KeyAssign.Taiko.RightRed,  InputDeviceType.Keyboard, 0, 112);
			OnlyKey(cfg.KeyAssign.Taiko.LeftBlue,  InputDeviceType.MidiIn,   1, 42);
			OnlyKey(cfg.KeyAssign.Taiko.RightBlue, InputDeviceType.Joystick, 3, 7);
			OnlyKey(cfg.KeyAssign.Taiko.LeftRed2P, InputDeviceType.Keyboard, 0, 200);
			OnlyKey(cfg.KeyAssign.Taiko.Decide,    InputDeviceType.Keyboard, 0, 28);
			// system part (stored separately, written to [SystemKeyAssign])
			OnlyKey(cfg.KeyAssign.System.Capture,      InputDeviceType.Keyboard, 0, 88);
			OnlyKey(cfg.KeyAssign.System.ToggleAutoP1, InputDeviceType.Keyboard, 0, 65);
			// training part (shares the gameplay padset, written to [TrainingKeyAssign])
			OnlyKey(cfg.KeyAssign.Taiko.TrainingToggleAuto, InputDeviceType.Keyboard, 0, 77);

			string path = TempPath();
			try {
				cfg.tExport(path);
				string text = File.ReadAllText(path);
				Assert.Contains("[DrumsKeyAssign]", text);
				Assert.Contains("[SystemKeyAssign]", text);
				Assert.Contains("[TrainingKeyAssign]", text);
				Assert.DoesNotContain("[GuitarKeyAssign]", text);
				Assert.DoesNotContain("[BassKeyAssign]", text);

				var rt = new CConfigIni();
				rt.LoadFromFile(path);
				AssertKey(rt.KeyAssign.Taiko.LeftRed,   InputDeviceType.Keyboard, 0, 111);
				AssertKey(rt.KeyAssign.Taiko.RightRed,  InputDeviceType.Keyboard, 0, 112);
				AssertKey(rt.KeyAssign.Taiko.LeftBlue,  InputDeviceType.MidiIn,   1, 42);
				AssertKey(rt.KeyAssign.Taiko.RightBlue, InputDeviceType.Joystick, 3, 7);
				AssertKey(rt.KeyAssign.Taiko.LeftRed2P, InputDeviceType.Keyboard, 0, 200);
				AssertKey(rt.KeyAssign.Taiko.Decide,    InputDeviceType.Keyboard, 0, 28);
				AssertKey(rt.KeyAssign.System.Capture,      InputDeviceType.Keyboard, 0, 88);
				AssertKey(rt.KeyAssign.System.ToggleAutoP1, InputDeviceType.Keyboard, 0, 65);
				AssertKey(rt.KeyAssign.Taiko.TrainingToggleAuto, InputDeviceType.Keyboard, 0, 77);
			} finally { try { File.Delete(path); } catch { } }
		}

		// The input routing CPad.HasInput performs is `KeyAssign[(int)part][(int)pad]`. This guards that
		// the part-indexer and the named gameplay accessor resolve to the SAME padset (so input keeps
		// reaching the gameplay bindings after the Drums→Taiko merge), and that System is a distinct part.
		[Fact]
		public void KeyAssign_PartIndexer_RoutesToGameplayPadset() {
			var cfg = new CConfigIni();
			Assert.Same(cfg.KeyAssign.Taiko, cfg.KeyAssign[(int)EKeyConfigPart.Taiko]);

			OnlyKey(cfg.KeyAssign.Taiko.LeftRed, InputDeviceType.Keyboard, 0, 123);
			var viaIndexer = cfg.KeyAssign[(int)EKeyConfigPart.Taiko][(int)EKeyConfigPad.LRed];
			Assert.Equal(123, viaIndexer[0].Code);
			Assert.Equal(InputDeviceType.Keyboard, viaIndexer[0].InputDevice);

			Assert.NotSame(cfg.KeyAssign.Taiko, cfg.KeyAssign[(int)EKeyConfigPart.System]);
		}

		// STSKILL kept only the Taiko (index 0) slot after Guitar/Bass were stripped; the [0,100] range
		// check on the setter must remain, and any other index must be rejected.
		[Fact]
		public void STSKILL_Indexer_Index0Only_WithRangeCheck() {
			var s = new CScore.STChartInfo.STSKILL();
			s[0] = 87.5;
			Assert.Equal(87.5, s[0]);
			Assert.Equal(87.5, s.Drums);
			Assert.Throws<IndexOutOfRangeException>(() => s[1]);
			Assert.Throws<ArgumentOutOfRangeException>(() => s[0] = 100.1);
			Assert.Throws<ArgumentOutOfRangeException>(() => s[0] = -0.1);
		}

		// `bスコアが有効である` used to sum レベル[Drums]+レベル[Guitar]+レベル[Bass]; after the
		// STDGBVALUE removal it reads the single Taiko level. Validity must still gate on a non-zero level.
		[Fact]
		public void CScore_ScoreValidity_GatedByTaikoLevel() {
			var sc = new CScore();
			Assert.False(sc.bScoreEnabled);   // freshly constructed → level 0 → invalid
			sc.ChartInfo.Level = 8;
			Assert.True(sc.bScoreEnabled);
			sc.ChartInfo.Level = 0;
			Assert.False(sc.bScoreEnabled);
		}

		// CTja.LEVEL went from STDGBVALUE<int> to a plain int; the LEVEL command parse must still land
		// the chart level on it (this exercises the real t入力 parse path).
		[Fact]
		public void CTja_ParsesLevel_IntoPlainField() {
			string dir = Path.Combine(Path.GetTempPath(), "ot_tja_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			string p = Path.Combine(dir, "level.tja");
			try {
				File.WriteAllText(p,
					"TITLE:LevelParseTest\nBPM:120\nWAVE:none.ogg\nCOURSE:Oni\nLEVEL:7\n#START\n1010,\n#END\n");
				var tja = new CTja();
				tja.Activate();
				tja.tInput(p, 3 /* Oni */, 0, false, 0);
				Assert.Equal(7, tja.LEVEL);
			} finally { try { Directory.Delete(dir, true); } catch { } }
		}
	}
}