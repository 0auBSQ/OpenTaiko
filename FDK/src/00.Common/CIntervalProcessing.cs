namespace FDK {
	/// <summary>
	/// <para>一定の間隔で処理を行うテンプレートパターンの定義。</para>
	/// <para>たとえば、t進行() で 5ms ごとに行う処理を前回のt進行()の呼び出しから 15ms 後に呼び出した場合は、処理が 3回 実行される。</para>
	/// </summary>
	public class CIntervalProcessing : IDisposable {
		public delegate void dgProc();
		public void Tick(long interval, dgProc proc) {
			// タイマ更新

			if (this.timer == null)
				return;
			this.timer.Update();


			// 初めての進行処理

			if (this.PrevTime == CTimer.UnusedNum)
				this.PrevTime = this.timer.NowTimeMs;


			// タイマが一回りしてしまった時のため……

			if (this.timer.NowTimeMs < this.PrevTime)
				this.PrevTime = this.timer.NowTimeMs;


			// 時間内の処理を実行。

			while ((this.timer.NowTimeMs - this.PrevTime) >= interval) {
				proc();

				this.PrevTime += interval;
			}
		}

		#region [ IDisposable 実装 ]
		//-----------------
		public void Dispose() {
			timer.Dispose();
		}
		//-----------------
		#endregion

		#region [ protected ]
		//-----------------
		protected CTimer timer = new CTimer(CTimer.TimerType.MultiMedia);
		protected long PrevTime = CTimer.UnusedNum;
		//-----------------
		#endregion
	}
}
