using System;

namespace OpenTaiko {
	/// <summary>
	/// Arbitrary-size (rows×cols) matrix for Lua stages, row-major and 1-indexed
	/// (Get(r,c) / Set(r,c,v)). Size-incompatible operations return an empty (0×0)
	/// result rather than throwing.
	/// </summary>
	public class LuaMatrix {
		public double[] M;
		public int Rows;
		public int Cols;

		public LuaMatrix(int rows, int cols) {
			Rows = Math.Max(0, rows);
			Cols = Math.Max(0, cols);
			M = new double[Rows * Cols];
		}
		private LuaMatrix(int rows, int cols, double[] m) { Rows = rows; Cols = cols; M = m; }

		public int RowCount() => Rows;
		public int ColCount() => Cols;
		public double Get(int r, int c) => (r >= 1 && r <= Rows && c >= 1 && c <= Cols) ? M[(r - 1) * Cols + (c - 1)] : 0.0;
		public void Set(int r, int c, double v) { if (r >= 1 && r <= Rows && c >= 1 && c <= Cols) M[(r - 1) * Cols + (c - 1)] = v; }

		public LuaMatrix Add(LuaMatrix o) {
			if (o == null || o.Rows != Rows || o.Cols != Cols) return new LuaMatrix(0, 0);
			var r = new double[M.Length];
			for (int i = 0; i < M.Length; i++) r[i] = M[i] + o.M[i];
			return new LuaMatrix(Rows, Cols, r);
		}
		public LuaMatrix Sub(LuaMatrix o) {
			if (o == null || o.Rows != Rows || o.Cols != Cols) return new LuaMatrix(0, 0);
			var r = new double[M.Length];
			for (int i = 0; i < M.Length; i++) r[i] = M[i] - o.M[i];
			return new LuaMatrix(Rows, Cols, r);
		}
		public LuaMatrix Scale(double s) {
			var r = new double[M.Length];
			for (int i = 0; i < M.Length; i++) r[i] = M[i] * s;
			return new LuaMatrix(Rows, Cols, r);
		}
		public LuaMatrix Mul(LuaMatrix o) {
			if (o == null || Cols != o.Rows) return new LuaMatrix(0, 0);
			var r = new double[Rows * o.Cols];
			for (int i = 0; i < Rows; i++)
				for (int j = 0; j < o.Cols; j++) {
					double s = 0;
					for (int k = 0; k < Cols; k++) s += M[i * Cols + k] * o.M[k * o.Cols + j];
					r[i * o.Cols + j] = s;
				}
			return new LuaMatrix(Rows, o.Cols, r);
		}
		public LuaVector MulVec(LuaVector v) {
			if (v == null || v.Size() != Cols) return new LuaVector(0);
			var res = new LuaVector(Rows);
			for (int i = 0; i < Rows; i++) {
				double s = 0;
				for (int k = 0; k < Cols; k++) s += M[i * Cols + k] * v.V[k];
				res.V[i] = s;
			}
			return res;
		}
		public LuaMatrix Transpose() {
			var r = new double[M.Length];
			for (int i = 0; i < Rows; i++)
				for (int j = 0; j < Cols; j++) r[j * Rows + i] = M[i * Cols + j];
			return new LuaMatrix(Cols, Rows, r);
		}
		public LuaMatrix Clone() => new(Rows, Cols, (double[])M.Clone());
	}

	public class LuaMatrixFunc {
		/// <summary>Create a zero-filled rows×cols matrix.</summary>
		public LuaMatrix CreateMatrix(int rows, int cols) => new(rows, cols);
		/// <summary>Create an n×n identity matrix.</summary>
		public LuaMatrix Identity(int n) {
			var m = new LuaMatrix(n, n);
			for (int i = 1; i <= n; i++) m.Set(i, i, 1);
			return m;
		}
	}
}
