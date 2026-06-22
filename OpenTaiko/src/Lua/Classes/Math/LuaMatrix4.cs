using System;

namespace OpenTaiko {
	/// <summary>4×4 matrix (row-major, 1-indexed Get/Set) for Lua stages.</summary>
	public class LuaMatrix4 {
		public double[] M = new double[16];   // [r*4 + c], row-major

		public LuaMatrix4() { }
		public LuaMatrix4(double[] m) { for (int i = 0; i < 16 && i < m.Length; i++) M[i] = m[i]; }

		public double Get(int r, int c) => M[(r - 1) * 4 + (c - 1)];
		public void Set(int r, int c, double v) { M[(r - 1) * 4 + (c - 1)] = v; }

		public LuaMatrix4 Mul(LuaMatrix4 o) {
			var r = new double[16];
			for (int i = 0; i < 4; i++)
				for (int j = 0; j < 4; j++) {
					double s = 0;
					for (int k = 0; k < 4; k++) s += M[i * 4 + k] * o.M[k * 4 + j];
					r[i * 4 + j] = s;
				}
			return new LuaMatrix4(r);
		}

		public LuaVector4 MulVec(LuaVector4 v) => new(
			M[0] * v.X + M[1] * v.Y + M[2] * v.Z + M[3] * v.W,
			M[4] * v.X + M[5] * v.Y + M[6] * v.Z + M[7] * v.W,
			M[8] * v.X + M[9] * v.Y + M[10] * v.Z + M[11] * v.W,
			M[12] * v.X + M[13] * v.Y + M[14] * v.Z + M[15] * v.W);

		public LuaMatrix4 Scale(double s) {
			var r = new double[16];
			for (int i = 0; i < 16; i++) r[i] = M[i] * s;
			return new LuaMatrix4(r);
		}
		public LuaMatrix4 Transpose() {
			var r = new double[16];
			for (int i = 0; i < 4; i++)
				for (int j = 0; j < 4; j++) r[j * 4 + i] = M[i * 4 + j];
			return new LuaMatrix4(r);
		}
		public LuaMatrix4 Clone() => new((double[])M.Clone());
	}

	public class LuaMatrix4Func {
		public LuaMatrix4 Identity() => new(new double[] {
			1, 0, 0, 0,  0, 1, 0, 0,  0, 0, 1, 0,  0, 0, 0, 1 });
		public LuaMatrix4 Translation(double x, double y, double z) => new(new double[] {
			1, 0, 0, x,  0, 1, 0, y,  0, 0, 1, z,  0, 0, 0, 1 });
		public LuaMatrix4 Scaling(double x, double y, double z) => new(new double[] {
			x, 0, 0, 0,  0, y, 0, 0,  0, 0, z, 0,  0, 0, 0, 1 });
	}
}
