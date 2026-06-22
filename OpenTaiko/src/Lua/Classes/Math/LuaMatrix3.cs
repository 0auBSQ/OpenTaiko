using System;

namespace OpenTaiko {
	/// <summary>3×3 matrix (row-major, 1-indexed Get/Set) for Lua stages.</summary>
	public class LuaMatrix3 {
		public double[] M = new double[9];   // [r*3 + c], row-major

		public LuaMatrix3() { }
		public LuaMatrix3(double[] m) { for (int i = 0; i < 9 && i < m.Length; i++) M[i] = m[i]; }

		public double Get(int r, int c) => M[(r - 1) * 3 + (c - 1)];
		public void Set(int r, int c, double v) { M[(r - 1) * 3 + (c - 1)] = v; }

		public LuaMatrix3 Mul(LuaMatrix3 o) {
			var r = new double[9];
			for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++) {
					double s = 0;
					for (int k = 0; k < 3; k++) s += M[i * 3 + k] * o.M[k * 3 + j];
					r[i * 3 + j] = s;
				}
			return new LuaMatrix3(r);
		}

		public LuaVector3 MulVec(LuaVector3 v) => new(
			M[0] * v.X + M[1] * v.Y + M[2] * v.Z,
			M[3] * v.X + M[4] * v.Y + M[5] * v.Z,
			M[6] * v.X + M[7] * v.Y + M[8] * v.Z);

		public LuaMatrix3 Scale(double s) {
			var r = new double[9];
			for (int i = 0; i < 9; i++) r[i] = M[i] * s;
			return new LuaMatrix3(r);
		}
		public LuaMatrix3 Transpose() {
			var r = new double[9];
			for (int i = 0; i < 3; i++)
				for (int j = 0; j < 3; j++) r[j * 3 + i] = M[i * 3 + j];
			return new LuaMatrix3(r);
		}
		public double Determinant()
			=> M[0] * (M[4] * M[8] - M[5] * M[7])
			 - M[1] * (M[3] * M[8] - M[5] * M[6])
			 + M[2] * (M[3] * M[7] - M[4] * M[6]);
		public LuaMatrix3 Clone() => new((double[])M.Clone());
	}

	public class LuaMatrix3Func {
		public LuaMatrix3 Identity() => new(new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 });
		public LuaMatrix3 Create(double m11, double m12, double m13,
								 double m21, double m22, double m23,
								 double m31, double m32, double m33)
			=> new(new double[] { m11, m12, m13, m21, m22, m23, m31, m32, m33 });
		/// <summary>Yaw/pitch/roll rotation matrix (radians), matching the engine convention.</summary>
		public LuaMatrix3 YawPitchRoll(double yaw, double pitch, double roll) {
			double cy = Math.Cos(yaw), sy = Math.Sin(yaw);
			double cp = Math.Cos(pitch), sp = Math.Sin(pitch);
			double cr = Math.Cos(roll), sr = Math.Sin(roll);
			return new(new double[] {
				cy * cp,                 sy * cp,                 -sp,
				cy * sp * sr - sy * cr,  sy * sp * sr + cy * cr,  cp * sr,
				cy * sp * cr + sy * sr,  sy * sp * cr - cy * sr,  cp * cr
			});
		}
	}
}
