using System.Diagnostics;

namespace FDK;

public class CTimer : CTimerBase {
	public enum TimerType {
		Unknown = -1,
		PerformanceCounter = 0, // always accurate, mainly for event-based input timing
		MultiMedia = 1, // same accuracy at integer frame but not fraction frame, mainly for drawing
		GetTickCount = 2, // 10~16ms low precision (unused)
	}
	public TimerType CurrentTimerType {
		get;
		protected set;
	}


	public override long SystemTimeMs {
		get {
			switch (this.CurrentTimerType) {
				case TimerType.PerformanceCounter:
					return performanceTimer?.ElapsedMilliseconds ?? 0;

				case TimerType.MultiMedia:
					return Game.TimeMs;

				case TimerType.GetTickCount:
					return (long)Environment.TickCount;
			}
			return 0;
		}
	}

	public CTimer(TimerType timerType)
		: base() {
		this.CurrentTimerType = timerType;

		if (ReferenceCount[(int)this.CurrentTimerType] == 0) {
			switch (this.CurrentTimerType) {
				case TimerType.PerformanceCounter:
					if (!this.GetSetPerformanceCounter())
						this.GetSetTickCount();
					break;

				case TimerType.MultiMedia:
					break;

				case TimerType.GetTickCount:
					this.GetSetTickCount();
					break;

				default:
					throw new ArgumentException(string.Format("Unknown timer type. [{0}]", this.CurrentTimerType));
			}
		}

		base.Reset();

		ReferenceCount[(int)this.CurrentTimerType]++;
	}

	public override void Dispose() {
		if (this.CurrentTimerType == TimerType.Unknown)
			return;

		int type = (int)this.CurrentTimerType;

		ReferenceCount[type] = Math.Max(ReferenceCount[type] - 1, 0);

		if (ReferenceCount[type] == 0) {
			if (this.CurrentTimerType == TimerType.PerformanceCounter) {
				performanceTimer?.Stop();
				performanceTimer = null;
			}
		}

		this.CurrentTimerType = TimerType.Unknown;
	}

	#region [ protected ]
	//-----------------
	protected static Stopwatch? performanceTimer = null;
	protected static int[] ReferenceCount = new int[3];

	protected bool GetSetTickCount() {
		this.CurrentTimerType = TimerType.GetTickCount;
		return true;
	}
	protected bool GetSetPerformanceCounter() {
		performanceTimer = Stopwatch.StartNew();
		if (Stopwatch.Frequency != 0) {
			this.CurrentTimerType = TimerType.PerformanceCounter;
			return true;
		}
		return false;
	}
	//-----------------
	#endregion
}
