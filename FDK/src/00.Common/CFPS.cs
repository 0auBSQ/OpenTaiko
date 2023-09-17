using System;
using System.Collections.Generic;
using System.Text;

namespace FDK
{
	public class CFPS
	{
		// プロパティ

		public int NowFPS
		{
			get;
			private set;
		}
		public double DeltaTime
		{
			get;
			private set;
		}
		public bool ChangedFPS
		{
			get;
			private set;
		}


		// コンストラクタ

		public CFPS()
		{
			this.NowFPS = 0;
			this.DeltaTime = 0;
			this.FPSTimer = new CTimer( CTimer.TimerType.MultiMedia );
			this.BeginTime = this.FPSTimer.NowTime;
			this.CoreFPS = 0;
			this.ChangedFPS = false;
		}


		// メソッド

		public void Update()
		{
			this.FPSTimer.Update();
			this.ChangedFPS = false;

			const long INTERVAL = 1000;
			this.DeltaTime = (this.FPSTimer.NowTime - this.PrevFrameTime) / 1000.0;
			PrevFrameTime = this.FPSTimer.NowTime;
			while ( ( this.FPSTimer.NowTime - this.BeginTime ) >= INTERVAL )
			{
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
		private CTimer	FPSTimer;
		private long BeginTime;
		private long PrevFrameTime;
		private int		CoreFPS;
		//-----------------
		#endregion
	}
}
