using FDK;

namespace OpenTaiko;

static class Easing {
	public static float EaseIn(CCounter counter, float startPoint, float endPoint, CalcType type) {
		double CounterValue = counter.CurrentValue / (double)(int)counter.EndValue;
		return EaseIn(CounterValue, startPoint, endPoint, type);
	}

	public static float EaseIn(double CounterValue, float startPoint, float endPoint, CalcType type) {
		float Sa = endPoint - startPoint;
		double Value = 0;

		switch (type) {
			case CalcType.Quadratic: //Quadratic
				Value = Sa * CounterValue * CounterValue + startPoint;
				break;
			case CalcType.Cubic: //Cubic
				Value = Sa * CounterValue * CounterValue * CounterValue + startPoint;
				break;
			case CalcType.Quartic: //Quartic
				Value = Sa * CounterValue * CounterValue * CounterValue * CounterValue + startPoint;
				break;
			case CalcType.Quintic: //Quintic
				Value = Sa * CounterValue * CounterValue * CounterValue * CounterValue * CounterValue + startPoint;
				break;
			case CalcType.Sinusoidal: //Sinusoidal
				Value = -Sa * Math.Cos(CounterValue * (Math.PI / 2)) + Sa + startPoint;
				break;
			case CalcType.Exponential: //Exponential
				Value = Sa * Math.Pow(2, 10 * (CounterValue - 1)) + startPoint;
				break;
			case CalcType.Circular: //Circular
				Value = -Sa * (Math.Sqrt(1 - CounterValue * CounterValue) - 1) + startPoint;
				break;
			case CalcType.Linear: //Linear
				Value = Sa * (CounterValue) + startPoint;
				break;
			case CalcType.Back: // Back
				Value = Sa * ((Coef1_Back + 1) * CounterValue * CounterValue * CounterValue - Coef1_Back * CounterValue * CounterValue) + startPoint;
				break;
		}

		return (float)Value;
	}

	public static float EaseOut(CCounter counter, float startPoint, float endPoint, CalcType type) {
		double CounterValue = counter.CurrentValue / (double)(int)counter.EndValue;
		return EaseOut(CounterValue, startPoint, endPoint, type);
	}

	public static float EaseOut(double CounterValue, float startPoint, float endPoint, CalcType type) {
		float Sa = endPoint - startPoint;
		double Value = 0;

		switch (type) {
			case CalcType.Quadratic: //Quadratic
				Value = -Sa * CounterValue * (CounterValue - 2) + startPoint;
				break;
			case CalcType.Cubic: //Cubic
				CounterValue--;
				Value = Sa * (CounterValue * CounterValue * CounterValue + 1) + startPoint;
				break;
			case CalcType.Quartic: //Quartic
				CounterValue--;
				Value = -Sa * (CounterValue * CounterValue * CounterValue * CounterValue - 1) + startPoint;
				break;
			case CalcType.Quintic: //Quintic
				CounterValue--;
				Value = Sa * (CounterValue * CounterValue * CounterValue * CounterValue * CounterValue + 1) + startPoint;
				break;
			case CalcType.Sinusoidal: //Sinusoidal
				Value = Sa * Math.Sin(CounterValue * (Math.PI / 2)) + startPoint;
				break;
			case CalcType.Exponential: //Exponential
				Value = Sa * (-Math.Pow(2, -10 * CounterValue) + 1) + startPoint;
				break;
			case CalcType.Circular: //Circular
				CounterValue--;
				Value = Sa * Math.Sqrt(1 - CounterValue * CounterValue) + startPoint;
				break;
			case CalcType.Linear: //Linear
				Value = Sa * CounterValue + startPoint;
				break;
			case CalcType.Back: // Back
				CounterValue--;
				Value = Sa * (1 + (Coef1_Back + 1) * CounterValue * CounterValue * CounterValue + Coef1_Back * CounterValue * CounterValue) + startPoint;
				break;
		}

		return (float)Value;
	}

	public static float EaseInOut(CCounter counter, float startPoint, float endPoint, CalcType type) {
		double CounterValue = counter.CurrentValue / (double)counter.EndValue;
		return EaseInOut(CounterValue, startPoint, endPoint, type);
	}

	public static float EaseInOut(double CounterValue, float startPoint, float endPoint, CalcType type) {
		float Sa = endPoint - startPoint;
		double Value = 0;

		switch (type) {
			case CalcType.Quadratic: //Quadratic
				CounterValue *= 2;
				if (CounterValue < 1) {
					Value = Sa / 2 * CounterValue * CounterValue + startPoint;
					break;
				}
				CounterValue--;
				Value = -Sa / 2 * (CounterValue * (CounterValue - 2) - 1) + startPoint;
				break;
			case CalcType.Cubic: //Cubic
				CounterValue *= 2;
				if (CounterValue < 1) {
					Value = Sa / 2 * CounterValue * CounterValue * CounterValue + startPoint;
					break;
				}
				CounterValue -= 2;
				Value = Sa / 2 * (CounterValue * CounterValue * CounterValue + 2) + startPoint;
				break;
			case CalcType.Quartic: //Quartic
				CounterValue *= 2;
				if (CounterValue < 1) {
					Value = Sa / 2 * CounterValue * CounterValue * CounterValue * CounterValue + startPoint;
					break;
				}
				CounterValue -= 2;
				Value = -Sa / 2 * (CounterValue * CounterValue * CounterValue * CounterValue - 2) + startPoint;
				break;
			case CalcType.Quintic: //Quintic
				CounterValue *= 2;
				if (CounterValue < 1) {
					Value = Sa / 2 * CounterValue * CounterValue * CounterValue * CounterValue * CounterValue + startPoint;
					break;
				}
				CounterValue -= 2;
				Value = Sa / 2 * (CounterValue * CounterValue * CounterValue * CounterValue * CounterValue + 2) + startPoint;
				break;
			case CalcType.Sinusoidal: //Sinusoidal
				Value = -Sa / 2 * (Math.Cos(Math.PI * CounterValue) - 1) + startPoint;
				break;
			case CalcType.Exponential: //Exponential
				CounterValue *= 2;
				if (CounterValue < 1) {
					Value = Sa / 2 * Math.Pow(2, 10 * (CounterValue - 1)) + startPoint;
					break;
				}
				CounterValue--;
				Value = Sa / 2 * (-Math.Pow(2, -10 * CounterValue) + 2) + startPoint;
				break;
			case CalcType.Circular: //Circular
				CounterValue *= 2;
				if (CounterValue < 1) {
					Value = -Sa / 2 * (Math.Sqrt(1 - CounterValue * CounterValue) - 1) + startPoint;
					break;
				}
				CounterValue -= 2;
				Value = Sa / 2 * (Math.Sqrt(1 - CounterValue * CounterValue) + 1) + startPoint;
				break;
			case CalcType.Linear: //Linear
				Value = Sa * CounterValue + startPoint;
				break;
			case CalcType.Back: // Back
				CounterValue *= 2;
				if (CounterValue < 1) {
					Value = Sa / 2 * ((Coef2_Back + 1) * CounterValue * CounterValue * CounterValue - Coef2_Back * CounterValue * CounterValue) + startPoint;
					break;
				}
				CounterValue -= 2;
				Value = Sa / 2 * (2 + (Coef2_Back + 1) * CounterValue * CounterValue * CounterValue + Coef2_Back * CounterValue * CounterValue) + startPoint;
				break;
		}

		return (float)Value;
	}

	// Value of a chart tween msElapsed after its start. Returns true once the tween is over (the value is then the end
	// value). An ease type other than IN, OUT or IN_OUT goes from start to end linearly.
	public static bool EvaluateTween(double msElapsed, double msDuration, string? easeType, float startPoint, float endPoint, CalcType type, out float value) {
		if (!(msDuration > 0) || msElapsed >= msDuration) {
			value = endPoint;
			return true;
		}
		double progress = Math.Max(0, msElapsed / msDuration);
		value = easeType switch {
			"IN" => EaseIn(progress, startPoint, endPoint, type),
			"OUT" => EaseOut(progress, startPoint, endPoint, type),
			"IN_OUT" => EaseInOut(progress, startPoint, endPoint, type),
			_ => EaseIn(progress, startPoint, endPoint, CalcType.Linear),
		};
		if (float.IsNaN(value))
			value = startPoint;
		return false;
	}

	public enum CalcType {
		Quadratic,
		Cubic,
		Quartic,
		Quintic,
		Sinusoidal,
		Exponential,
		Circular,
		Linear,
		Back,
	}

	private const double Coef1_Back = 1.70158;
	private const double Coef2_Back = Coef1_Back * 1.525;
}
