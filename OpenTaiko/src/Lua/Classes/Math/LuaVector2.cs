using System;

namespace OpenTaiko {
	/// <summary>2D vector for Lua stages. Methods return new vectors (immutable style).</summary>
	public class LuaVector2 {
		public double X;
		public double Y;

		public LuaVector2() { X = 0; Y = 0; }
		public LuaVector2(double x, double y) { X = x; Y = y; }

		public LuaVector2 Add(LuaVector2 o) => new(X + o.X, Y + o.Y);
		public LuaVector2 Sub(LuaVector2 o) => new(X - o.X, Y - o.Y);
		public LuaVector2 Mul(LuaVector2 o) => new(X * o.X, Y * o.Y);   // component-wise
		public LuaVector2 Scale(double s) => new(X * s, Y * s);
		public LuaVector2 Negate() => new(-X, -Y);

		public double Dot(LuaVector2 o) => X * o.X + Y * o.Y;
		public double Cross(LuaVector2 o) => X * o.Y - Y * o.X;          // scalar (z component)
		public double Length() => Math.Sqrt(X * X + Y * Y);
		public double LengthSq() => X * X + Y * Y;
		public double Distance(LuaVector2 o) { double dx = X - o.X, dy = Y - o.Y; return Math.Sqrt(dx * dx + dy * dy); }

		public LuaVector2 Normalized() {
			double l = Math.Sqrt(X * X + Y * Y);
			return l < 1e-12 ? new(0, 0) : new(X / l, Y / l);
		}
		public LuaVector2 Lerp(LuaVector2 o, double t) => new(X + (o.X - X) * t, Y + (o.Y - Y) * t);
		public LuaVector2 Rotate(double radians) {
			double c = Math.Cos(radians), s = Math.Sin(radians);
			return new(X * c - Y * s, X * s + Y * c);
		}

		public LuaVector2 Clone() => new(X, Y);
		public void Set(double x, double y) { X = x; Y = y; }
		public double Unpack(out double y) { y = Y; return X; }          // local x, y = v:Unpack()
		public override string ToString() => $"({X}, {Y})";
	}

	public class LuaVector2Func {
		public LuaVector2 CreateVector2(double x, double y) => new(x, y);
		public LuaVector2 Zero() => new(0, 0);
		public LuaVector2 One() => new(1, 1);
	}
}
