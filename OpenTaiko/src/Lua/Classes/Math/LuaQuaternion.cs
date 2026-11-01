using System;

namespace OpenTaiko {
	/// <summary>
	/// Unit quaternion for Lua stages — rotations without gimbal lock. Methods return new
	/// quaternions/values (immutable style). Multi-component results come back as multiple
	/// Lua values, e.g.  local x, y, z = q:RotateVec(0, 0, 1)
	/// </summary>
	public class LuaQuaternion {
		public double X, Y, Z, W;

		public LuaQuaternion() { X = 0; Y = 0; Z = 0; W = 1; }
		public LuaQuaternion(double x, double y, double z, double w) { X = x; Y = y; Z = z; W = w; }

		public LuaQuaternion Mul(LuaQuaternion o) => new(
			W * o.X + X * o.W + Y * o.Z - Z * o.Y,
			W * o.Y - X * o.Z + Y * o.W + Z * o.X,
			W * o.Z + X * o.Y - Y * o.X + Z * o.W,
			W * o.W - X * o.X - Y * o.Y - Z * o.Z);

		public double Dot(LuaQuaternion o) => X * o.X + Y * o.Y + Z * o.Z + W * o.W;
		public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z + W * W);

		public LuaQuaternion Normalized() {
			double l = Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
			if (l < 1e-12) return new(0, 0, 0, 1);
			double inv = 1.0 / l;
			return new(X * inv, Y * inv, Z * inv, W * inv);
		}

		public LuaQuaternion Conjugate() => new(-X, -Y, -Z, W);

		/// <summary>Rotate a vector by this quaternion. Returns x, y, z.</summary>
		public (double, double, double) RotateVec(double vx, double vy, double vz) {
			// v' = v + 2*cross(q.xyz, cross(q.xyz, v) + q.w*v)
			double tx = 2 * (Y * vz - Z * vy);
			double ty = 2 * (Z * vx - X * vz);
			double tz = 2 * (X * vy - Y * vx);
			double rx = vx + W * tx + (Y * tz - Z * ty);
			double ry = vy + W * ty + (Z * tx - X * tz);
			double rz = vz + W * tz + (X * ty - Y * tx);
			return (rx, ry, rz);
		}

		public LuaQuaternion Slerp(LuaQuaternion o, double t) {
			double d = Dot(o);
			LuaQuaternion b = o;
			if (d < 0) { b = new(-o.X, -o.Y, -o.Z, -o.W); d = -d; }
			if (d > 0.9995) { // nearly parallel → lerp
				return new LuaQuaternion(X + (b.X - X) * t, Y + (b.Y - Y) * t, Z + (b.Z - Z) * t, W + (b.W - W) * t).Normalized();
			}
			double theta0 = Math.Acos(d);
			double theta = theta0 * t;
			double s0 = Math.Sin(theta0 - theta) / Math.Sin(theta0);
			double s1 = Math.Sin(theta) / Math.Sin(theta0);
			return new(X * s0 + b.X * s1, Y * s0 + b.Y * s1, Z * s0 + b.Z * s1, W * s0 + b.W * s1);
		}

		public LuaQuaternion Clone() => new(X, Y, Z, W);
		public (double, double, double, double) Unpack() => (X, Y, Z, W);
	}

	public class LuaQuaternionFunc {
		public LuaQuaternion Identity() => new(0, 0, 0, 1);
		public LuaQuaternion Create(double x, double y, double z, double w) => new(x, y, z, w);

		/// <summary>Rotation of <paramref name="angle"/> radians about the (ax,ay,az) axis.</summary>
		public LuaQuaternion FromAxisAngle(double ax, double ay, double az, double angle) {
			double l = Math.Sqrt(ax * ax + ay * ay + az * az);
			if (l < 1e-12) return new(0, 0, 0, 1);
			double inv = 1.0 / l;
			double h = angle * 0.5;
			double s = Math.Sin(h);
			return new(ax * inv * s, ay * inv * s, az * inv * s, Math.Cos(h));
		}
	}
}
