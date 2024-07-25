namespace TJAPlayer3.Animations {
	/// <summary>
	/// フェードアウトを行うクラス。
	/// </summary>
	internal class FadeOut : Animator {
		/// <summary>
		/// フェードアウトを初期化します。
		/// </summary>
		/// <param name="timems">フェードアウトに掛ける秒数(ミリ秒)</param>
		public FadeOut(int timems) : base(0, timems - 1, 1, false) {
			TimeMs = timems;
		}

		/// <summary>
		/// フェードアウトの不透明度を255段階で返します。
		/// </summary>
		/// <returns>不透明度。</returns>
		public override object GetAnimation() {
			var opacity = (TimeMs - base.Counter.CurrentValue) * 255 / TimeMs;
			return opacity;
		}

		private readonly int TimeMs;
	}
}
