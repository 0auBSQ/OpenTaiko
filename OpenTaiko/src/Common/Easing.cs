using FDK;

namespace OpenTaiko;

static class Easing {
	public static int EaseIn(CCounter counter, float startPoint, float endPoint, CalcType type) {
		double CounterValue = counter.CurrentValue / (double)(int)counter.EndValue;
		return EaseIn(CounterValue, startPoint, endPoint, type);
	}

	public static int EaseIn(double CounterValue, float startPoint, float endPoint, CalcType type) {
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
				break;
		}

		return (int)Value;
	}

	public static int EaseOut(CCounter counter, float startPoint, float endPoint, CalcType type) {
		double CounterValue = counter.CurrentValue / (double)(int)counter.EndValue;
		return EaseOut(CounterValue, startPoint, endPoint, type);
	}

	public static int EaseOut(double CounterValue, float startPoint, float endPoint, CalcType type) {
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
				break;
		}

		return (int)Value;
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
				break;
		}

		return (float)Value;
	}

	public enum CalcType {
		Quadratic,
		Cubic,
		Quartic,
		Quintic,
		Sinusoidal,
		Exponential,
		Circular,
		Linear
	}
}
