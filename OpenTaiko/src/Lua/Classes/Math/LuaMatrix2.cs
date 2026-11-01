using System;

namespace OpenTaiko {
	/// <summary>2×2 matrix (row-major, 1-indexed Get/Set) for Lua stages.</summary>
	public class LuaMatrix2 {
		public double[] M = new double[4];   // [r*2 + c], row-major

		public LuaMatrix2() { }
		public LuaMatrix2(double m11, double m12, double m21, double m22) {
			M[0] = m11; M[1] = m12; M[2] = m21; M[3] = m22;
		}

		public double Get(int r, int c) => M[(r - 1) * 2 + (c - 1)];
		public void Set(int r, int c, double v) { M[(r - 1) * 2 + (c - 1)] = v; }

		public LuaMatrix2 Mul(LuaMatrix2 o) => new(
			M[0] * o.M[0] + M[1] * o.M[2], M[0] * o.M[1] + M[1] * o.M[3],
			M[2] * o.M[0] + M[3] * o.M[2], M[2] * o.M[1] + M[3] * o.M[3]);

		public LuaVector2 MulVec(LuaVector2 v) => new(
			M[0] * v.X + M[1] * v.Y,
			M[2] * v.X + M[3] * v.Y);

		public LuaMatrix2 Scale(double s) => new(M[0] * s, M[1] * s, M[2] * s, M[3] * s);
		public LuaMatrix2 Transpose() => new(M[0], M[2], M[1], M[3]);
		public double Determinant() => M[0] * M[3] - M[1] * M[2];
		public LuaMatrix2 Clone() => new(M[0], M[1], M[2], M[3]);
	}

	public class LuaMatrix2Func {
		public LuaMatrix2 Identity() => new(1, 0, 0, 1);
		public LuaMatrix2 Create(double m11, double m12, double m21, double m22) => new(m11, m12, m21, m22);
		public LuaMatrix2 Rotation(double radians) {
			double c = Math.Cos(radians), s = Math.Sin(radians);
			return new(c, -s, s, c);
		}
		public LuaMatrix2 Scaling(double sx, double sy) => new(sx, 0, 0, sy);
	}
}
