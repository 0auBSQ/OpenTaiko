using FDK;

namespace OpenTaiko;

internal abstract class CActFIFOBase : CActivity {
	// メソッド

	public virtual void tフェードアウト開始(int? start = null, int? end = null, int? interval = null) {
		this.mode = EFIFOMode.FadeOut;
		this.counter = new CCounter(start ?? 0, end ?? 0, interval ?? 0, OpenTaiko.Timer);
	}

	public virtual void tフェードイン開始(int? start = null, int? end = null, int? interval = null) {
		this.mode = EFIFOMode.FadeIn;
		this.counter = new CCounter(start ?? 0, end ?? 0, interval ?? 0, OpenTaiko.Timer);
	}

	public virtual void tフェードイン完了()     // #25406 2011.6.9 yyagi
	{
		this.counter.CurrentValue = (int)this.counter.EndValue;
	}


	// CActivity 実装

	public override int Draw() {
		if (base.IsDeActivated || (this.counter == null)) {
			return 0;
		}
		this.counter.Tick();

		var subRes = this.DrawSub();
		if (subRes != 0) {
			this.tフェードイン完了();
			return subRes;
		}

		if (this.counter.CurrentValue != this.counter.EndValue) {
			return 0;
		}
		return 1;
	}

	public abstract int DrawSub();

	// その他

	#region [ private ]
	//-----------------
	protected CCounter counter;
	protected EFIFOMode mode;
	//-----------------
	#endregion
}
