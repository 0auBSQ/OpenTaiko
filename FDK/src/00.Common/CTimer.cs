using System.Diagnostics;

namespace FDK;

public class CTimer : CTimerBase {
	public enum TimerType {
		Unknown = -1,
		GameTimeReal = 0, // always accurate, mainly for event-based input timing
		GameTimeAtDraw = 1, // same accuracy at integer frame but not fraction frame, mainly for drawing
		SystemTimeCoarse = 2, // 10~16ms low precision (unused)
	}
	public TimerType CurrentTimerType {
		get;
		protected set;
	}


	public override long SystemTimeMs {
		get => this.CurrentTimerType switch {
			TimerType.GameTimeReal => Game.TimeMsReal,
			TimerType.GameTimeAtDraw => Game.TimeMs,
			TimerType.SystemTimeCoarse => Environment.TickCount64,
			_ => 0,
		};
	}

	public override double SystemTimeMs_Double {
		get => this.CurrentTimerType switch {
			TimerType.GameTimeReal => Game.dbTimeMsReal,
			TimerType.GameTimeAtDraw => Game.dbTimeMs,
			TimerType.SystemTimeCoarse => Environment.TickCount64,
			_ => 0,
		};
	}

	public CTimer(TimerType timerType)
		: base() {
		this.CurrentTimerType = timerType;

		if (ReferenceCount[(int)this.CurrentTimerType] == 0) {
			switch (this.CurrentTimerType) {
				case TimerType.GameTimeReal:
				case TimerType.GameTimeAtDraw:
				case TimerType.SystemTimeCoarse:
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
		this.CurrentTimerType = TimerType.Unknown;
	}

	#region [ protected ]
	//-----------------
	protected static int[] ReferenceCount = new int[3];
	//-----------------
	#endregion
}
