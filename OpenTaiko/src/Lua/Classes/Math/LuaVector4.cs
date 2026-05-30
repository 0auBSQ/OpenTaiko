using System;

namespace OpenTaiko {
	/// <summary>4D vector for Lua stages. Methods return new vectors (immutable style).</summary>
	public class LuaVector4 {
		public double X;
		public double Y;
		public double Z;
		public double W;

		public LuaVector4() { X = 0; Y = 0; Z = 0; W = 0; }
		public LuaVector4(double x, double y, double z, double w) { X = x; Y = y; Z = z; W = w; }

		public LuaVector4 Add(LuaVector4 o) => new(X + o.X, Y + o.Y, Z + o.Z, W + o.W);
		public LuaVector4 Sub(LuaVector4 o) => new(X - o.X, Y - o.Y, Z - o.Z, W - o.W);
		public LuaVector4 Mul(LuaVector4 o) => new(X * o.X, Y * o.Y, Z * o.Z, W * o.W);
		public LuaVector4 Scale(double s) => new(X * s, Y * s, Z * s, W * s);
		public LuaVector4 Negate() => new(-X, -Y, -Z, -W);

		public double Dot(LuaVector4 o) => X * o.X + Y * o.Y + Z * o.Z + W * o.W;
		public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
		public double LengthSq() => X * X + Y * Y + Z * Z + W * W;
		public double Distance(LuaVector4 o) {
			double dx = X - o.X, dy = Y - o.Y, dz = Z - o.Z, dw = W - o.W;
			return Math.Sqrt(dx * dx + dy * dy + dz * dz + dw * dw);
		}

		public LuaVector4 Normalized() {
			double l = Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
			return l < 1e-12 ? new(0, 0, 0, 0) : new(X / l, Y / l, Z / l, W / l);
		}
		public LuaVector4 Lerp(LuaVector4 o, double t)
			=> new(X + (o.X - X) * t, Y + (o.Y - Y) * t, Z + (o.Z - Z) * t, W + (o.W - W) * t);

		public LuaVector4 Clone() => new(X, Y, Z, W);
		public void Set(double x, double y, double z, double w) { X = x; Y = y; Z = z; W = w; }
		public double Unpack(out double y, out double z, out double w) { y = Y; z = Z; w = W; return X; }
		public override string ToString() => $"({X}, {Y}, {Z}, {W})";
	}

	public class LuaVector4Func {
		public LuaVector4 CreateVector4(double x, double y, double z, double w) => new(x, y, z, w);
		public LuaVector4 Zero() => new(0, 0, 0, 0);
		public LuaVector4 One() => new(1, 1, 1, 1);
	}
}
