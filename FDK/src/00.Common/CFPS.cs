namespace FDK;

public class CFPS {
	// Properties

	public int NowFPS {
		get;
		private set;
	}
	public double DeltaTime {
		get;
		private set;
	}
	public bool ChangedFPS {
		get;
		private set;
	}


	// Constructor

	public CFPS() {
		this.NowFPS = 0;
		this.DeltaTime = 0;
		this.FPSTimer = new CTimer(CTimer.TimerType.MultiMedia);
		this.BeginTime = this.FPSTimer.NowTimeMs;
		this.CoreFPS = 0;
		this.ChangedFPS = false;
	}


	// メソッド

	public void Update() {
		this.FPSTimer.Update();
		this.ChangedFPS = false;

		const long INTERVAL = 1000;
		this.DeltaTime = (this.FPSTimer.NowTimeMs - this.PrevFrameTime) / 1000.0;
		PrevFrameTime = this.FPSTimer.NowTimeMs;
		while ((this.FPSTimer.NowTimeMs - this.BeginTime) >= INTERVAL) {
			this.NowFPS = this.CoreFPS;
			this.CoreFPS = 0;
			this.ChangedFPS = true;
			this.BeginTime += INTERVAL;
		}
		this.CoreFPS++;
	}


	// その他

	#region [ private ]
	//-----------------
	private CTimer FPSTimer;
	private long BeginTime;
	private long PrevFrameTime;
	private int CoreFPS;
	//-----------------
	#endregion
}
