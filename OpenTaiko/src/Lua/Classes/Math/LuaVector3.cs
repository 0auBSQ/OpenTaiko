using System;

namespace OpenTaiko {
	/// <summary>3D vector for Lua stages. Methods return new vectors (immutable style).</summary>
	public class LuaVector3 {
		public double X;
		public double Y;
		public double Z;

		public LuaVector3() { X = 0; Y = 0; Z = 0; }
		public LuaVector3(double x, double y, double z) { X = x; Y = y; Z = z; }

		public LuaVector3 Add(LuaVector3 o) => new(X + o.X, Y + o.Y, Z + o.Z);
		public LuaVector3 Sub(LuaVector3 o) => new(X - o.X, Y - o.Y, Z - o.Z);
		public LuaVector3 Mul(LuaVector3 o) => new(X * o.X, Y * o.Y, Z * o.Z);
		public LuaVector3 Scale(double s) => new(X * s, Y * s, Z * s);
		public LuaVector3 Negate() => new(-X, -Y, -Z);

		public double Dot(LuaVector3 o) => X * o.X + Y * o.Y + Z * o.Z;
		public LuaVector3 Cross(LuaVector3 o) => new(
			Y * o.Z - Z * o.Y,
			Z * o.X - X * o.Z,
			X * o.Y - Y * o.X);
		public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);
		public double LengthSq() => X * X + Y * Y + Z * Z;
		public double Distance(LuaVector3 o) {
			double dx = X - o.X, dy = Y - o.Y, dz = Z - o.Z;
			return Math.Sqrt(dx * dx + dy * dy + dz * dz);
		}

		public LuaVector3 Normalized() {
			double l = Math.Sqrt(X * X + Y * Y + Z * Z);
			return l < 1e-12 ? new(0, 0, 0) : new(X / l, Y / l, Z / l);
		}
		public LuaVector3 Lerp(LuaVector3 o, double t) => new(X + (o.X - X) * t, Y + (o.Y - Y) * t, Z + (o.Z - Z) * t);
		public LuaVector3 Reflect(LuaVector3 n) {
			double d = 2.0 * (X * n.X + Y * n.Y + Z * n.Z);
			return new(X - d * n.X, Y - d * n.Y, Z - d * n.Z);
		}

		public LuaVector3 Clone() => new(X, Y, Z);
		public void Set(double x, double y, double z) { X = x; Y = y; Z = z; }
		public (double, double, double) Unpack() => (X, Y, Z);           // local x,y,z = v:Unpack()
		public override string ToString() => $"({X}, {Y}, {Z})";
	}

	public class LuaVector3Func {
		public LuaVector3 CreateVector3(double x, double y, double z) => new(x, y, z);
		public LuaVector3 Zero() => new(0, 0, 0);
		public LuaVector3 One() => new(1, 1, 1);
	}
}
