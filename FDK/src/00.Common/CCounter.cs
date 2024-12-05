namespace FDK;

/// <summary>
/// 一定間隔で単純増加する整数（カウント値）を扱う。
/// </summary>
/// <remarks>
/// ○使い方
/// 1.CCounterの変数をつくる。
/// 2.CCounterを生成
///   ctCounter = new CCounter( 0, 3, 10, CDTXMania.Timer );
/// 3.進行メソッドを使用する。
/// 4.ウマー。
///
/// double値を使う場合、t進行db、t進行LoopDbを使うこと。
/// また、double版では間隔の値はミリ秒単位ではなく、通常の秒単位になります。
/// Note: For the double version, the given interval is in second only for new CCounter(), not for Start().
/// </remarks>
public class CCounter {
	public bool IsStarted {
		get;
		set;
	}
	// 値プロパティ
	public double BeginValue {
		get;
		private set;
	}
	public double EndValue {
		get;
		set;
	}
	public int CurrentValue {
		get;
		set;
	}

	public double _msInterval {
		get {
			return this.msInterval;
		}
		set {
			this.msInterval = Math.Max(1e-6, Math.Abs(value));
		}
	}

	public double msNowTime {
		get;
		set;
	}
	// 状態プロパティ

	public bool IsTicked {
		get { return (this.msNowTime != -1); }
	}
	public bool IsStoped {
		get { return !this.IsTicked; }
	}
	public bool IsEnded {
		get { return (this.CurrentValue >= this.EndValue); }
	}
	public bool IsUnEnded {
		get { return !this.IsEnded; }
	}

	// Constructor

	public CCounter() {
		this.NormalTimer = null;
		this.BeginValue = 0;
		this.EndValue = 0;
		this.CurrentValue = 0;
		this.CurrentValue = 0;
		this.msNowTime = CSoundTimer.UnusedNum;
	}

	/// <summary>生成と同時に開始する。</summary>
	public CCounter(double begin, double end, double msInterval, CTimer timer)
		: this() {
		this.Start(begin, end, msInterval, timer);
	}

	/// <summary>生成と同時に開始する。(double版)</summary>
	public CCounter(double begin, double end, double secInterval, CSoundTimer timer)
		: this() {
		this.Start(begin, end, secInterval * 1000.0f, timer);
	}


	// 状態操作メソッド

	/// <summary>
	/// カウントを開始する。
	/// </summary>
	/// <param name="begin">最初のカウント値。</param>
	/// <param name="end">最後のカウント値。</param>
	/// <param name="msInterval">カウント値を１増加させるのにかける時間（ミリ秒単位）。</param>
	/// <param name="timer">カウントに使用するタイマ。</param>
	public void Start(double begin, double end, double msInterval, CTimer timer) {
		this.BeginValue = begin;
		this.EndValue = end;
		this._msInterval = msInterval;
		this.NormalTimer = timer;
		this.msNowTime = this.NormalTimer.NowTimeMs;
		this.CurrentValue = (int)begin;
		this.IsStarted = true;
	}

	/// <summary>
	/// カウントを開始する。(double版)
	/// </summary>
	/// <param name="begin">最初のカウント値。</param>
	/// <param name="end">最後のカウント値。</param>
	/// <param name="msInterval">カウント値を１増加させるのにかける時間（ミリ秒単位）。</param>
	/// <param name="timer">カウントに使用するタイマ。</param>
	public void Start(double begin, double end, double msInterval, CSoundTimer timer) {
		this.BeginValue = begin;
		this.EndValue = end;
		this._msInterval = msInterval;
		this.TimerDB = timer;
		this.msNowTime = this.TimerDB.SystemTimeMs_Double;
		this.CurrentValue = (int)begin;
		this.IsStarted = true;
	}

	/// <summary>
	/// 前回の t進行() の呼び出しからの経過時間をもとに、必要なだけカウント値を増加させる。
	/// カウント値が終了値に達している場合は、それ以上増加しない（終了値を維持する）。
	/// </summary>
	public void Tick() {
		if ((this.NormalTimer != null) && (this.msNowTime != CTimer.UnusedNum)) {
			long msNow = this.NormalTimer.NowTimeMs;
			if (msNow < this.msNowTime)
				this.msNowTime = msNow;

			for (int i = 0; i < 8; ++i) {
				if ((msNow - this.msNowTime) < this.msInterval)
					return;
				if (++this.CurrentValue > this.EndValue)
					this.CurrentValue = (int)this.EndValue;

				this.msNowTime += this.msInterval;
			}

			this.TickJump(msNow);
		}
	}

	/// <summary>
	/// 前回の t進行() の呼び出しからの経過時間をもとに、必要なだけカウント値を増加させる。
	/// カウント値が終了値に達している場合は、それ以上増加しない（終了値を維持する）。
	/// </summary>
	public void TickDB() {
		if ((this.TimerDB != null) && (this.msNowTime != CSoundTimer.UnusedNum)) {
			double msNow = this.TimerDB.NowTimeMs;
			if (msNow < this.msNowTime)
				this.msNowTime = msNow;

			for (int i = 0; i < 8; ++i) {
				if ((msNow - this.msNowTime) < this.msInterval)
					return;
				if (++this.CurrentValue > this.EndValue)
					this.CurrentValue = (int)this.EndValue;

				this.msNowTime += this.msInterval;
			}

			this.TickJump(msNow);
		}
	}

	/// Jump over precalculated steps. Might be slower due to float division.
	private void TickJump(double msNow) {
		if ((msNow - this.msNowTime) < this.msInterval)
			return;
		int nStepsMax = (int)this.EndValue + 1 - (int)this.BeginValue;
		long nSteps = (long)((msNow - this.msNowTime) / this.msInterval);
		if (nSteps >= nStepsMax // attempt to prevent overflow
			|| (this.CurrentValue += (int)nSteps) > this.EndValue
			) {
			this.CurrentValue = (int)this.EndValue;
		}
		this.msNowTime += nSteps * this.msInterval;
	}

	/// <summary>
	/// 前回の t進行Loop() の呼び出しからの経過時間をもとに、必要なだけカウント値を増加させる。
	/// カウント値が終了値に達している場合は、次の増加タイミングで開始値に戻る（値がループする）。
	/// </summary>
	public void TickLoop() {
		if ((this.NormalTimer != null) && (this.msNowTime != CTimer.UnusedNum)) {
			long msNow = this.NormalTimer.NowTimeMs;
			if (msNow < this.msNowTime)
				this.msNowTime = msNow;

			for (int i = 0; i < 8; ++i) {
				if ((msNow - this.msNowTime) < this.msInterval)
					return;
				if (++this.CurrentValue > this.EndValue)
					this.CurrentValue = (int)this.BeginValue;

				this.msNowTime += this.msInterval;
			}

			this.TickLoopJump(msNow);
		}
	}

	/// <summary>
	/// 前回の t進行Loop() の呼び出しからの経過時間をもとに、必要なだけカウント値を増加させる。
	/// カウント値が終了値に達している場合は、次の増加タイミングで開始値に戻る（値がループする）。
	/// </summary>
	public void TickLoopDB() {
		if ((this.TimerDB != null) && (this.msNowTime != CSoundTimer.UnusedNum)) {
			double msNow = this.TimerDB.NowTimeMs;
			if (msNow < this.msNowTime)
				this.msNowTime = msNow;

			for (int i = 0; i < 8; ++i) {
				if ((msNow - this.msNowTime) < this.msInterval)
					return;
				if (++this.CurrentValue > this.EndValue)
					this.CurrentValue = (int)this.BeginValue;

				this.msNowTime += this.msInterval;
			}

			this.TickLoopJump(msNow);
		}
	}

	/// Jump over precalculated steps. Might be slower due to float division.
	private void TickLoopJump(double msNow) {
		if ((msNow - this.msNowTime) < this.msInterval)
			return;
		int nStepsMax = (int)this.EndValue + 1 - (int)this.BeginValue;
		long nSteps = (long)((msNow - this.msNowTime) / this.msInterval);
		int dVal = (int)(nSteps % nStepsMax); // attempt to prevent overflow
		if ((this.CurrentValue += dVal) > this.EndValue) {
			this.CurrentValue = (int)this.BeginValue;
		}
		this.msNowTime += nSteps * this.msInterval;
	}

	/// <summary>
	/// カウントを停止する。
	/// これ以降に t進行() や t進行Loop() を呼び出しても何も処理されない。
	/// </summary>
	public void Stop() {
		this.msNowTime = CTimer.UnusedNum;
	}

	public void ChangeInterval(double Value) {
		this._msInterval = Value;
	}

	// その他

	#region [ 応用：キーの反復入力をエミュレーションする ]
	//-----------------

	/// <summary>
	/// <para>「bキー押下」引数が true の間中、「tキー処理」デリゲート引数を呼び出す。</para>
	/// <para>ただし、2回目の呼び出しは1回目から 200ms の間を開けてから行い、3回目以降の呼び出しはそれぞれ 30ms の間隔で呼び出す。</para>
	/// <para>「bキー押下」が false の場合は何もせず、呼び出し回数を 0 にリセットする。</para>
	/// </summary>
	/// <param name="pressFlag">キーが押下されている場合は true。</param>
	/// <param name="keyProcess">キーが押下されている場合に実行する処理。</param>
	public void KeyIntervalFunc(bool pressFlag, KeyProcess keyProcess) {
		const int first = 0;
		const int second = 1;
		const int later = 2;

		if (pressFlag) {
			switch (this.CurrentValue) {
				case first:

					keyProcess();
					this.CurrentValue = second;
					this.msNowTime = this.NormalTimer.NowTimeMs;
					return;

				case second:

					if ((this.NormalTimer.NowTimeMs - this.msNowTime) > 200) {
						keyProcess();
						this.msNowTime = this.NormalTimer.NowTimeMs;
						this.CurrentValue = later;
					}
					return;

				case later:

					if ((this.NormalTimer.NowTimeMs - this.msNowTime) > 30) {
						keyProcess();
						this.msNowTime = this.NormalTimer.NowTimeMs;
					}
					return;
			}
		} else {
			this.CurrentValue = first;
		}
	}
	public delegate void KeyProcess();

	//-----------------
	#endregion

	#region [ private ]
	//-----------------
	private CTimer NormalTimer;
	private CSoundTimer TimerDB;
	private double msInterval;
	//-----------------
	#endregion
}
