using FDK;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// Dynamic Beat retunes the music while it plays: setting a timer's time moves both of its clocks, and a speed change
	// rebases each sound's start time so (time since the start) x speed still lands where the sound is.
	public class DynamicBeatSyncTests {
		private sealed class ManualTimer : CTimerBase {
			public long Now;
			public double NowDouble;
			public override long SystemTimeMs => Now;
			public override double SystemTimeMs_Double => NowDouble;
			public override void Dispose() { }
		}

		private static ManualTimer Started(long at) {
			var timer = new ManualTimer { Now = at, NowDouble = at + 0.25 };
			timer.Reset();
			return timer;
		}

		[Fact]
		public void SettingTheTime_MovesBothClocks_WhileRunning() {
			var timer = Started(1000);
			timer.Now = 1500; timer.NowDouble = 1500.25;
			timer.Update();
			timer.NowTimeMs = 200;
			Assert.Equal(200, timer.NowTimeMs);
			Assert.Equal(200.0, timer.NowTimeMs_Double, 6);
			timer.Now = 1600; timer.NowDouble = 1600.25;
			timer.Update();
			Assert.Equal(300, timer.NowTimeMs);
			Assert.Equal(300.0, timer.NowTimeMs_Double, 6);
		}

		[Fact]
		public void SettingTheTime_MovesBothClocks_WhilePaused() {
			var timer = Started(1000);
			timer.Now = 1500; timer.NowDouble = 1500.25;
			timer.Update();
			timer.Pause();
			timer.NowTimeMs = 300;
			Assert.Equal(300, timer.NowTimeMs);
			Assert.Equal(300.0, timer.NowTimeMs_Double, 6);
			timer.Now = 2000; timer.NowDouble = 2000.25;
			timer.Resume();
			Assert.Equal(300, timer.NowTimeMs);
			Assert.Equal(300.0, timer.NowTimeMs_Double, 6);
		}

		[Fact]
		public void SettingTheDoubleTime_MovesTheWholeMillisecondClockToo() {
			var timer = Started(1000);
			timer.NowTimeMs_Double = 400.6;
			Assert.Equal(400.6, timer.NowTimeMs_Double, 6);
			Assert.Equal(401, timer.NowTimeMs);
		}

		[Theory]
		[InlineData(10000, 40000, 1.0, 1.05)]
		[InlineData(10000, 40000, 1.05, 1.0)]
		[InlineData(0, 123457, 1.5, 1.65)]
		[InlineData(5000, 5001, 1.2, 1.25)]
		public void Rebase_KeepsThePosition(long start, long now, double oldSpeed, double newSpeed) {
			long rebased = CTja.tRebasedPlaybackStartTime(start, now, oldSpeed, newSpeed);
			double before = (now - start) * oldSpeed, after = (now - rebased) * newSpeed;
			Assert.InRange(after, before - newSpeed, before + newSpeed);   // within the whole-millisecond rounding
		}

		[Fact]
		public void Rebase_LeavesAStartNotReachedYet() {
			Assert.Equal(5000, CTja.tRebasedPlaybackStartTime(5000, 4000, 1.0, 1.2));
			Assert.Equal(5000, CTja.tRebasedPlaybackStartTime(5000, 5000, 1.0, 1.2));
		}

		[Fact]
		public void ChainedChanges_AddUpToThePlayedPosition() {
			// 30 s at 1.0, then 30 s at 1.05, then 60 s at 1.2: the sound played 30 + 31.5 + 72 = 133.5 s of audio
			long start = 0;
			start = CTja.tRebasedPlaybackStartTime(start, 30000, 1.0, 1.05);
			start = CTja.tRebasedPlaybackStartTime(start, 60000, 1.05, 1.2);
			Assert.InRange((120000 - start) * 1.2, 133500 - 3, 133500 + 3);
		}
	}
}
