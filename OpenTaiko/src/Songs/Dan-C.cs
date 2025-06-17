namespace OpenTaiko;

/// <summary>
/// 段位認定を管理するクラス。
/// </summary>
[Serializable]
public class Dan_C {
	public Dan_C() {

	}

	public Dan_C(Dan_C dan_C) : this(dan_C.ExamType, dan_C.GetValue(), dan_C.ExamRange) {

	}

	/// <summary>
	/// 段位認定の条件を初期化し、生成します。
	/// </summary>
	/// <param name="examType">条件の種別。</param>
	/// <param name="value">条件の合格量。</param>
	/// <param name="examRange">条件の合格の範囲。</param>
	public Dan_C(Exam.Type examType, ReadOnlySpan<int> value, Exam.Range examRange) {
		IsEnable = true;
		ReachStatus = Exam.ReachStatus.Unknown;
		ExamType = examType;
		SetValue(value[0], value[1]);
		ExamRange = examRange;
	}

	/// <summary>
	/// 条件と現在の値を評価して、クリアしたかどうかを判断します。
	/// </summary>
	/// <param name="nowValue">その条件の現在の値。</param>
	public bool Update(int nowValue) {
		if (!ExamIsEnable || nowValue < 0)
			return false;
		bool isChangedAmount = (ExamRange == Exam.Range.Less && nowValue > Amount && Amount > GetValue()[0]) ? false // n未満でその数を超えたらfalseを返す。
			: ((int)nowValue != (int)Amount);
		Amount = nowValue;
		UpdateCleared();
		return isChangedAmount;
	}

	/// <summary>
	/// 段位認定の条件が有効であるかどうかを返します。
	/// </summary>
	/// <returns>段位認定の条件が有効であるかどうか。</returns>
	public bool ExamIsEnable => this.IsEnable;

	/// <summary>
	/// 各合格条件のボーダーを設定します。
	/// </summary>
	/// <param name="redValue">赤合格条件</param>
	/// <param name="goldValue">金合格条件</param>
	public void SetValue(int redValue, int goldValue) {
		if (redValue != -1) this.Value[0] = redValue;
		if (goldValue != -1) this.Value[1] = goldValue;
	}

	/// <summary>
	/// 各合格条件のボーダーを返します。
	/// </summary>
	/// <param name="isGoldValue">trueを指定すると、金合格条件を返します。</param>
	/// <returns>合格条件の値。</returns>
	public ReadOnlySpan<int> GetValue() => this.Value;

	/// <summary>
	/// 条件の種別を返します。
	/// </summary>
	/// <returns>条件の種別</returns>
	/// <summary>
	/// 条件の種別を設定します。
	/// </summary>
	/// <param name="type">条件の種別。</param>
	public Exam.Type ExamType { get => this.Type; private set => this.Type = value; }

	/// <summary>
	/// 条件の範囲を返します。
	/// </summary>
	/// <returns>条件の範囲</returns>
	/// <summary>
	/// 条件の範囲を設定します。
	/// </summary>
	/// <param name="range"></param>
	public Exam.Range ExamRange { get => this.Range; private set => this.Range = value; }

	/// <summary>
	/// 条件にクリアしているかどうか返します。
	/// </summary>
	/// <returns>条件にクリアしているかどうか。</returns>
	public Exam.Status GetExamStatus()
		=> this.IsCleared[1] ? Exam.Status.Better_Success
			: this.IsCleared[0] ? Exam.Status.Success
			: Exam.Status.Failure;

	/// <summary>
	/// 条件と現在の値をチェックして、合格もしくは金合格をしてるか否かを更新する。
	/// </summary>
	private void UpdateCleared() {
		if (ExamRange != Exam.Range.Less) {
			IsCleared[0] = (Amount >= GetValue()[0]);
			IsCleared[1] = (IsCleared[0] && Amount >= GetValue()[1]);
		} else {
			IsCleared[0] = (Amount < GetValue()[0]);
			IsCleared[1] = (IsCleared[0] && Amount < GetValue()[1]);
		}
	}

	/// <summary>
	/// ゲージの描画のための百分率を返す。
	/// </summary>
	/// <returns>Amountの百分率。</returns>
	public int GetAmountToPercent() {
		if (GetValue()[0] == 0 || this.GetDisplayedAmount() == 0) {
			return 0;
		}
		double ratio = (double)this.GetDisplayedAmount() / GetValue()[0];
		return (int)double.Clamp(ratio * 100.0, 1, 100.0);
	}

	public double GetBetterAmountToPercent() {
		if (this.Type is Exam.Type.Accuracy)
			return 0; // uses its own rules
		if (GetValue()[1] - GetValue()[0] <= 0)
			return (this.GetDisplayedAmount() >= GetValue()[0]) ? 100 : 0;
		double ratio = (double)(this.GetDisplayedAmount() - GetValue()[0]) / (GetValue()[1] - GetValue()[0]);
		return double.Clamp(ratio * 100.0, 0.0, 100.0);
	}

	public int GetDisplayedAmount() {
		int amount = (this.ExamRange != Exam.Range.Less) ? this.Amount : this.GetValue()[0] - this.Amount;
		return Math.Max(0, amount);
	}

	// オーバーライドメソッド
	/// <summary>
	/// ToString()のオーバーライドメソッド。段位認定モードの各条件の現在状況をString型で返します。
	/// </summary>
	/// <returns>段位認定モードの各条件の現在状況。</returns>
	public override string ToString() {
		return String.Format("Type: {0} / Value: {1}/{2} / Range: {3} / Amount: {4} / Clear: {5} / Percent: {6} / ReachStatus: {7}",
			ExamType, GetValue()[0], GetValue()[1], ExamRange,
			Amount, GetExamStatus(), GetAmountToPercent(), ReachStatus);
	}


	#region [ Serialized fields, keep their name untouched ]
	/// <summary>
	/// その条件が有効であるかどうか。
	/// </summary>
	private bool IsEnable;
	/// <summary>
	/// 条件の種別。
	/// </summary>
	private Exam.Type Type;
	/// <summary>
	/// 条件の範囲。
	/// </summary>
	private Exam.Range Range;
	/// <summary>
	/// 条件の値。
	/// </summary>
	private int[] Value = new int[] { 0, 0 };
	/// <summary>
	/// 量。
	/// </summary>
	public int Amount;
	/// <summary>
	/// 条件をクリアしているか否か。
	/// </summary>
	private readonly bool[] IsCleared = new[] { false, false };

	/// <summary>
	/// 曲ごとの条件を格納する
	/// </summary>
	// public Dan_C[] SongExam = new Dan_C[3];

	/// <summary>
	/// 条件の達成見込みがなくなったら、真になる。
	/// この変数が一度trueになれば、基本的にfalseに戻ることはない。
	/// (スコア加算については、この限りではない。)
	/// </summary>
	[Obsolete("use `ReachStatus == Exam.ReachStatus.Failure`")] private bool NotReached = false;
	#endregion

	[NonSerialized] public Exam.ReachStatus ReachStatus = Exam.ReachStatus.Low;
}

public static class Exam {
	/// <summary>
	/// 段位認定の条件の種別。
	/// </summary>
	public enum Type {
		Gauge,
		JudgePerfect,
		JudgeGood,
		JudgeBad,
		Score,
		Roll,
		Hit,
		Combo,
		Accuracy,
		JudgeADLIB,
		JudgeMine,
		Total,
	}

	/// <summary>
	/// 段位認定の合格範囲。
	/// </summary>
	public enum Range {
		/// <summary>
		/// 以上
		/// </summary>
		More,
		/// <summary>
		/// 未満
		/// </summary>
		Less
	}

	/// <summary>
	/// ステータス。
	/// </summary>
	public enum Status {
		/// <summary>
		/// 不合格
		/// </summary>
		Failure,
		/// <summary>
		/// 合格
		/// </summary>
		Success,
		/// <summary>
		/// より良い合格
		/// </summary>
		Better_Success
	}

	public enum ReachStatus {
		Unknown, // no transition
		Failure, // grey (transparent)
		Danger, // blinking red
		Low, // more: yellow / less: red
		High, // light yellow
		Near_Success, // blinking light yellow
		Nearer_Success, // rapidly blinking light yellow
		Success_Or_Better, // pink
		Near_Better_Success, // blinking pink
		Nearer_Better_Success, // rapidly blinking pink
		Better_Success, // rainbow
	}

	public static ReachStatus ToReachStatus(Status status) => status switch {
		Status.Success => ReachStatus.Success_Or_Better,
		Status.Better_Success => ReachStatus.Better_Success,
		_ => ReachStatus.Failure,
	};
}
