namespace OpenTaiko {
	internal class HEasingMethods {
		public enum EEaseType {
			IN = 0,
			OUT,
			INOUT,
			OUTIN
		}

		public enum EEaseFunction {
			LINEAR = 0,
			SINE,
			QUAD,
			CUBIC,
			QUART,
			QUINT,
			EXPO,
			CIRC,
			ELASTIC,
			BACK,
			BOUNCE
		}

		private static readonly Dictionary<EEaseFunction, Func<double, double>> _easeMethods = new Dictionary<EEaseFunction, Func<double, double>>() {
			[EEaseFunction.LINEAR] = _easeLinear,
			[EEaseFunction.SINE] = _easeSine,
			[EEaseFunction.QUAD] = _easeQuad,
			[EEaseFunction.CUBIC] = _easeCubic,
			[EEaseFunction.QUART] = _easeQuart,
			[EEaseFunction.QUINT] = _easeQuint,
			[EEaseFunction.EXPO] = _easeExpo,
			[EEaseFunction.CIRC] = _easeCirc,
			[EEaseFunction.ELASTIC] = _easeElastic,
			[EEaseFunction.BACK] = _easeBack,
			[EEaseFunction.BOUNCE] = _easeBounce,
		};

		private static double _easeOut(Func<double, double> f, double x) {
			return 1.0 - f(1 - x);
		}

		private static double _easeInOut(Func<double, double> f, double x) {
			return (x < 0.5) ? 0.5 * f(x * 2) : 0.5 * (1 - f((1 - x) * 2)) + 0.5;
		}

		private static double _easeOutIn(Func<double, double> f, double x) {
			return (x < 0.5) ? 0.5 * (1 - f((1 - x) * 2)) : 0.5 * (1 - (1 - f(1 - (1 - x) * 2))) + 0.5;
		}

		private static double _easeLinear(double x) {
			return x;
		}

		private static double _easeQuad(double x) {
			return x * x;
		}

		private static double _easeCubic(double x) {
			return x * x * x;
		}

		private static double _easeQuart(double x) {
			return x * x * x * x;
		}

		private static double _easeQuint(double x) {
			return x * x * x * x * x;
		}

		private static double _easeExpo(double x) {
			return x == 0 ? 0 : Math.Pow(2, 10 * (x - 1));
		}

		private static double _easeSine(double x) {
			return 1.0 - Math.Cos((x * Math.PI) / 2.0);
		}

		private static double _easeCirc(double x) {
			return 1.0 - Math.Sqrt(1 - Math.Pow(x, 2));
		}

		private static double _easeBack(double x) {
			return x * x * (2.7 * x - 1.7);
		}

		private static double _easeElastic(double x) {
			if (x == 0 || x == 1) return x;

			const double c4 = (2 * Math.PI) / 3;

			return -Math.Pow(2, 10 * x - 10) * Math.Sin((x * 10 - 10.75) * c4);
		}

		private static double _easeBounce(double x) {
			const double c1 = 2.75;
			const double c2 = 7.5625;
			double c3 = x * x;

			if (x < 1.0 / c1)
				return c2 * c3;
			else if (x < 2.0 / c1)
				return c2 * c3 + 0.75;
			else if (x < 2.5 / c1)
				return c2 * c3 + 0.9375;
			return c2 * c3 + 0.984375;
		}

		#region [Legacy]

		/*

        private static double _easeSine(EEaseType type, double x)
        {
            switch (type) {
                case EEaseType.IN:
                default:
                    return 1 - Math.Cos((x * Math.PI) / 2.0);
                case EEaseType.OUT:
                    return Math.Sin((x * Math.PI) / 2.0);
                case EEaseType.INOUT:
                    return -(Math.Cos(x * Math.PI) - 1) / 2.0;
                case EEaseType.OUTIN:
                    return x;
            }
        }

        private static double _easeQuad(EEaseType type, double x)
        {
            switch (type)
            {
                case EEaseType.IN:
                default:
                    return x * x;
                case EEaseType.OUT:
                    return 1 - (1 - x) * (1 - x);
                case EEaseType.INOUT:
                    return x < 0.5 ? 2 * x * x : 1 - Math.Pow(-2 * x + 2, 2) / 2.0;
                case EEaseType.OUTIN:
                    return x;
            }
        }

        private static double _easeCubic(EEaseType type, double x)
        {
            switch (type)
            {
                case EEaseType.IN:
                default:
                    return x * x * x;
                case EEaseType.OUT:
                    return 1 - Math.Pow(1 - x, 3);
                case EEaseType.INOUT:
                    return x < 0.5 ? 4 * x * x * x : 1 - Math.Pow(-2 * x + 2, 3) / 2.0;
                case EEaseType.OUTIN:
                    return x;
            }
        }

        private static double _easeQuart(EEaseType type, double x)
        {
            switch (type)
            {
                case EEaseType.IN:
                default:
                    return x * x * x * x;
                case EEaseType.OUT:
                    return 1 - Math.Pow(1 - x, 4);
                case EEaseType.INOUT:
                    return x < 0.5 ? 8 * x * x * x * x : 1 - Math.Pow(-2 * x + 2, 4) / 2.0;
                case EEaseType.OUTIN:
                    return x;
            }
        }

        private static double _easeQuint(EEaseType type, double x)
        {
            switch (type)
            {
                case EEaseType.IN:
                default:
                    return x * x * x * x * x;
                case EEaseType.OUT:
                    return 1 - Math.Pow(1 - x, 5);
                case EEaseType.INOUT:
                    return x < 0.5 ? 16 * x * x * x * x * x : 1 - Math.Pow(-2 * x + 2, 5) / 2.0;
                case EEaseType.OUTIN:
                    return x;
            }
        }

        private static double _easeExpo(EEaseType type, double x)
        {
            switch (type)
            {
                case EEaseType.IN:
                default:
                    return x == 0 ? 0 : Math.Pow(2, 10 * x - 10);
                case EEaseType.OUT:
                    return x == 1 ? 1 : 1 - Math.Pow(2, -10 * x);
                case EEaseType.INOUT:
                    return x == 0
                      ? 0
                      : x == 1
                          ? 1
                          : x < 0.5 
                            ? Math.Pow(2, 20 * x - 10) / 2
                            : (2 - Math.Pow(2, -20 * x + 10)) / 2; ;
                case EEaseType.OUTIN:
                    return x;
            }
        }

        private static double _easeCirc(EEaseType type, double x)
        {
            switch (type)
            {
                case EEaseType.IN:
                default:
                    return 1 - Math.Sqrt(1 - Math.Pow(x, 2));
                case EEaseType.OUT:
                    return Math.Sqrt(1 - Math.Pow(x - 1, 2));
                case EEaseType.INOUT:
                    return x < 0.5
                      ? (1 - Math.Sqrt(1 - Math.Pow(2 * x, 2))) / 2
                      : (Math.Sqrt(1 - Math.Pow(-2 * x + 2, 2)) + 1) / 2;
                case EEaseType.OUTIN:
                    return x;
            }
        }

        private static double _easeBack(EEaseType type, double x)
        {
            const double c1 = 1.70158;
            const double c2 = c1 * 1.525;
            const double c3 = c1 + 1;
            switch (type)
            {
                case EEaseType.IN:
                default:
                    return c3 * x * x * x - c1 * x * x;
                case EEaseType.OUT:
                    return 1 + c3 * Math.Pow(x - 1, 3) + c1 * Math.Pow(x - 1, 2);
                case EEaseType.INOUT:
                    return x < 0.5
                      ? (Math.Pow(2 * x, 2) * ((c2 + 1) * 2 * x - c2)) / 2
                      : (Math.Pow(2 * x - 2, 2) * ((c2 + 1) * (x * 2 - 2) + c2) + 2) / 2; 
                case EEaseType.OUTIN:
                    return x;
            }
        }

        private static double _easeElastic(EEaseType type, double x)
        {
            const double c4 = (2 * Math.PI) / 3;
            const double c5 = (2 * Math.PI) / 4.5;
            switch (type)
            {
                case EEaseType.IN:
                default:
                    return x == 0
                      ? 0
                      : x == 1
                          ? 1
                          : -Math.Pow(2, 10 * x - 10) * Math.Sin((x * 10 - 10.75) * c4);
                case EEaseType.OUT:
                    return x == 0
                      ? 0
                      : x == 1
                          ? 1
                          : Math.Pow(2, -10 * x) * Math.Sin((x * 10 - 0.75) * c4) + 1;
                case EEaseType.INOUT:
                    return x == 0
                      ? 0
                      : x == 1
                          ? 1
                          : x < 0.5
                              ? -(Math.Pow(2, 20 * x - 10) * Math.Sin((20 * x - 11.125) * c5)) / 2
                              : (Math.Pow(2, -20 * x + 10) * Math.Sin((20 * x - 11.125) * c5)) / 2 + 1;
                case EEaseType.OUTIN:
                    return x;
            }
        }

        private static double _easeOutBounce(double x)
        {
            const double n1 = 7.5625;
            const double d1 = 2.75;

            if (x < 1 / d1)
                return n1 * x * x;
            else if (x < 2 / d1)
                return n1 * (x -= 1.5 / d1) * x + 0.75;
            else if (x < 2.5 / d1)
                return n1 * (x -= 2.25 / d1) * x + 0.9375;
            else
                return n1 * (x -= 2.625 / d1) * x + 0.984375;
        }

        private static double _easeBounce(EEaseType type, double x)
        {
            switch (type)
            {
                case EEaseType.IN:
                default:
                    return 1 - _easeOutBounce(1 - x);
                case EEaseType.OUT:
                    return _easeOutBounce(x);
                case EEaseType.INOUT:
                    return x < 0.5
                      ? (1 - _easeOutBounce(1 - 2 * x)) / 2
                      : (1 + _easeOutBounce(2 * x - 1)) / 2;
                case EEaseType.OUTIN:
                    return x;
            }
        }
        */

		#endregion

		public static double tCalculateEaseNorm(EEaseType type, EEaseFunction function, double ratio) {
			switch (type) {
				case EEaseType.IN:
				default:
					return _easeMethods[function](ratio);
				case EEaseType.OUT:
					return _easeOut(_easeMethods[function], ratio);
				case EEaseType.INOUT:
					return _easeInOut(_easeMethods[function], ratio);
				case EEaseType.OUTIN:
					return _easeOutIn(_easeMethods[function], ratio);
			}
		}

		public static double tCalculateEase(EEaseType type, EEaseFunction function, double timeStart, double timeEnd, double timeCurrent, double valueStart = 0, double valueEnd = 1) {
			if (timeStart == timeEnd) return valueEnd;
			double ratio = (timeCurrent - timeStart) / (timeEnd - timeStart);
			double ratio_eased = tCalculateEaseNorm(type, function, ratio);//_easeMethods[function](type, ratio);
			return valueStart + ratio_eased * (valueEnd - valueStart);
		}
	}
}
