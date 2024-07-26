namespace FDK {
	/// <summary>
	/// <para>タイマの抽象クラス。</para>
	/// <para>このクラスを継承し、override したクラスを作成することで、任意のクロックを持つタイマを作成できる。</para>
	/// </summary>
	public abstract class CTimerBase : IDisposable {
		public const long UnusedNum = -1;

		// この２つを override する。
		public abstract long SystemTimeMs {
			get;
		}
		public double SystemTimeMs_Double {
			get;
			set;
		}
		public abstract void Dispose();

		#region [ DTXMania用に、語尾にmsのつかない宣言を追加 ]
		public long SystemTime {
			get { return SystemTimeMs; }
		}
		public long NowTime {
			get { return NowTimeMs; }
			set { NowTimeMs = value; }
		}
		public long PrevResetTime {
			get { return PrevResetTimeMs; }
		}

		//double
		public double SystemTime_Double {
			get { return SystemTimeMs_Double; }
		}
		public double NowTime_Double {
			get { return NowTimeMs_Double; }
			set { NowTimeMs_Double = value; }
		}
		public double PrevResetTime_Double {
			get { return PrevResetTimeMs_Double; }
		}
		#endregion

		public long NowTimeMs {
			get {
				if (this.StopCount > 0)
					return (this.PauseSystemTimeMs - this.PrevResetTimeMs);

				return (this.UpdateSystemTime - this.PrevResetTimeMs);
			}
			set {
				if (this.StopCount > 0)
					this.PrevResetTimeMs = this.PauseSystemTimeMs - value;
				else
					this.PrevResetTimeMs = this.UpdateSystemTime - value;
			}
		}
		public long RealNowTimeMs {
			get {
				if (this.StopCount > 0)
					return (this.PauseSystemTimeMs - this.PrevResetTimeMs);

				return (this.SystemTimeMs - this.PrevResetTimeMs);
			}
		}
		public long PrevResetTimeMs {
			get;
			protected set;
		}


		public double NowTimeMs_Double {
			get {
				if (this.StopCount > 0)
					return (this.PauseSystemTimeMs_Double - this.PrevResetTimeMs_Double);

				return (this.UpdateSystemTime_Double - this.PrevResetTimeMs_Double);
			}
			set {
				if (this.StopCount > 0)
					this.PrevResetTimeMs_Double = this.PauseSystemTimeMs_Double - value;
				else
					this.PrevResetTimeMs_Double = this.UpdateSystemTime_Double - value;
			}
		}
		public double RealNowTimeMs_Double {
			get {
				if (this.StopCount > 0)
					return (this.PauseSystemTimeMs_Double - this.PrevResetTimeMs_Double);

				return (this.SystemTimeMs_Double - this.PrevResetTimeMs_Double);
			}
		}
		public double PrevResetTimeMs_Double {
			get;
			protected set;
		}

		public bool IsUnStoped {
			get {
				return (this.StopCount == 0);
			}
		}

		public void Reset() {
			this.Update();
			this.PrevResetTimeMs = this.UpdateSystemTime;
			this.PauseSystemTimeMs = this.UpdateSystemTime;
			this.StopCount = 0;
		}
		public void Pause() {
			if (this.StopCount == 0) {
				this.PauseSystemTimeMs = this.UpdateSystemTime;
				this.PauseSystemTimeMs_Double = this.UpdateSystemTime_Double;
			}

			this.StopCount++;
		}
		public void Update() {
			this.UpdateSystemTime = this.SystemTimeMs;
			this.UpdateSystemTime_Double = this.SystemTimeMs_Double;
		}
		public void Resume() {
			if (this.StopCount > 0) {
				this.StopCount--;
				if (this.StopCount == 0) {
					this.Update();
					this.PrevResetTimeMs += this.UpdateSystemTime - this.PauseSystemTimeMs;
					this.PrevResetTimeMs_Double += this.UpdateSystemTime_Double - this.PauseSystemTimeMs_Double;
				}
			}
		}

		#region [ protected ]
		//-----------------
		protected long PauseSystemTimeMs = 0;
		protected long UpdateSystemTime = 0;
		protected double PauseSystemTimeMs_Double = 0;
		protected double UpdateSystemTime_Double = 0;
		protected int StopCount = 0;
		//-----------------
		#endregion
	}
}
