using System;

namespace OpenTaiko {
	/// <summary>
	/// Arbitrary-length vector for Lua stages. Components are 1-indexed (Lua convention):
	/// v:Get(1) is the first component. Operations between vectors require equal sizes
	/// (mismatches return a zero-length vector rather than throwing).
	/// </summary>
	public class LuaVector {
		public double[] V;

		public LuaVector(int n) { V = new double[Math.Max(0, n)]; }
		private LuaVector(double[] v) { V = v; }

		public int Size() => V.Length;
		public double Get(int i) => (i >= 1 && i <= V.Length) ? V[i - 1] : 0.0;
		public void Set(int i, double value) { if (i >= 1 && i <= V.Length) V[i - 1] = value; }

		private bool SameSize(LuaVector o) => o != null && o.V.Length == V.Length;

		public LuaVector Add(LuaVector o) {
			if (!SameSize(o)) return new LuaVector(0);
			var r = new double[V.Length];
			for (int i = 0; i < V.Length; i++) r[i] = V[i] + o.V[i];
			return new LuaVector(r);
		}
		public LuaVector Sub(LuaVector o) {
			if (!SameSize(o)) return new LuaVector(0);
			var r = new double[V.Length];
			for (int i = 0; i < V.Length; i++) r[i] = V[i] - o.V[i];
			return new LuaVector(r);
		}
		public LuaVector Mul(LuaVector o) {
			if (!SameSize(o)) return new LuaVector(0);
			var r = new double[V.Length];
			for (int i = 0; i < V.Length; i++) r[i] = V[i] * o.V[i];
			return new LuaVector(r);
		}
		public LuaVector Scale(double s) {
			var r = new double[V.Length];
			for (int i = 0; i < V.Length; i++) r[i] = V[i] * s;
			return new LuaVector(r);
		}
		public double Dot(LuaVector o) {
			if (!SameSize(o)) return 0.0;
			double sum = 0;
			for (int i = 0; i < V.Length; i++) sum += V[i] * o.V[i];
			return sum;
		}
		public double Length() {
			double sum = 0;
			for (int i = 0; i < V.Length; i++) sum += V[i] * V[i];
			return Math.Sqrt(sum);
		}
		public double LengthSq() {
			double sum = 0;
			for (int i = 0; i < V.Length; i++) sum += V[i] * V[i];
			return sum;
		}
		public double Distance(LuaVector o) {
			if (!SameSize(o)) return 0.0;
			double sum = 0;
			for (int i = 0; i < V.Length; i++) { double d = V[i] - o.V[i]; sum += d * d; }
			return Math.Sqrt(sum);
		}
		public LuaVector Normalized() {
			double l = Length();
			if (l < 1e-12) return new LuaVector(V.Length);
			var r = new double[V.Length];
			for (int i = 0; i < V.Length; i++) r[i] = V[i] / l;
			return new LuaVector(r);
		}
		public LuaVector Lerp(LuaVector o, double t) {
			if (!SameSize(o)) return new LuaVector(0);
			var r = new double[V.Length];
			for (int i = 0; i < V.Length; i++) r[i] = V[i] + (o.V[i] - V[i]) * t;
			return new LuaVector(r);
		}
		public LuaVector Clone() => new((double[])V.Clone());
	}

	public class LuaVectorFunc {
		/// <summary>Create a zero-filled vector of length n.</summary>
		public LuaVector CreateVector(int n) => new(n);
	}
}
