using System;

namespace OpenTaiko {
	/// <summary>A pass's camera for culling: the eye, the forward axis and the four side planes of the view the world
	/// vertex shader projects (camera basis, projection scale and target size), widened a little so a cull never
	/// reaches into the screen edge.</summary>
	internal struct ViewFrustum {
		private const double SIDE_PAD = 1.08;   // the side planes' slopes, widened
		private const double DIST_PAD = 0.5;    // world units every sphere and box is grown by
		public double Ex, Ey, Ez, Fx, Fy, Fz;
		// outward unit normals of the right, left, top and bottom planes, which all pass through the eye
		private double _rx, _ry, _rz, _lx, _ly, _lz, _tx, _ty, _tz, _bx, _by, _bz;

		public static ViewFrustum Of(Lua3DScene s) => Of(s._camX, s._camY, s._camZ, s._Rx, s._Ry, s._Rz, s._Ux, s._Uy, s._Uz, s._Fx, s._Fy, s._Fz, s._scale, s._w, s._h);

		/// <summary>The view of an eye at (ex, ey, ez) with right, up and forward axes r, u, f and a projection of
		/// <paramref name="scale"/> pixels per unit at depth 1 over a <paramref name="w"/> x <paramref name="h"/> target.</summary>
		public static ViewFrustum Of(double ex, double ey, double ez, double rx, double ry, double rz, double ux, double uy, double uz,
			double fx, double fy, double fz, double scale, int w, int h) {
			double kx = w * 0.5 / scale * SIDE_PAD, ky = h * 0.5 / scale * SIDE_PAD;   // the half-width and half-height at depth 1
			var v = new ViewFrustum { Ex = ex, Ey = ey, Ez = ez, Fx = fx, Fy = fy, Fz = fz };
			Plane(rx, ry, rz, fx, fy, fz, kx, out v._rx, out v._ry, out v._rz);
			Plane(-rx, -ry, -rz, fx, fy, fz, kx, out v._lx, out v._ly, out v._lz);
			Plane(ux, uy, uz, fx, fy, fz, ky, out v._tx, out v._ty, out v._tz);
			Plane(-ux, -uy, -uz, fx, fy, fz, ky, out v._bx, out v._by, out v._bz);
			return v;
		}
		// the side plane through the eye that contains the forward-going line at slope k along axis a
		private static void Plane(double ax, double ay, double az, double fx, double fy, double fz, double k, out double nx, out double ny, out double nz) {
			nx = ax - k * fx; ny = ay - k * fy; nz = az - k * fz;
			double l = Math.Sqrt(nx * nx + ny * ny + nz * nz);
			if (l > 1e-12) { nx /= l; ny /= l; nz /= l; }
		}

		/// <summary>Camera depth (along the forward axis) of a point.</summary>
		public readonly double Depth(double x, double y, double z) => (x - Ex) * Fx + (y - Ey) * Fy + (z - Ez) * Fz;
		/// <summary>The squared distance from the eye to a point.</summary>
		public readonly double DistSq(double x, double y, double z) => (x - Ex) * (x - Ex) + (y - Ey) * (y - Ey) + (z - Ez) * (z - Ez);
		/// <summary>The largest camera depth of a box given by its centre and half extents.</summary>
		public readonly double FarDepth(double cx, double cy, double cz, double hx, double hy, double hz)
			=> Depth(cx, cy, cz) + Math.Abs(Fx) * hx + Math.Abs(Fy) * hy + Math.Abs(Fz) * hz;
		/// <summary>The smallest camera depth of a box given by its centre and half extents.</summary>
		public readonly double NearDepth(double cx, double cy, double cz, double hx, double hy, double hz)
			=> Depth(cx, cy, cz) - (Math.Abs(Fx) * hx + Math.Abs(Fy) * hy + Math.Abs(Fz) * hz);
		/// <summary>The squared distance from the eye to the nearest point of a box.</summary>
		public readonly double BoxDistSq(double cx, double cy, double cz, double hx, double hy, double hz) {
			double dx = Math.Max(Math.Abs(cx - Ex) - hx, 0), dy = Math.Max(Math.Abs(cy - Ey) - hy, 0), dz = Math.Max(Math.Abs(cz - Ez) - hz, 0);
			return dx * dx + dy * dy + dz * dz;
		}

		/// <summary>False when the sphere lies wholly behind the eye or outside a side plane.</summary>
		public readonly bool SphereInView(double x, double y, double z, double r) {
			double vx = x - Ex, vy = y - Ey, vz = z - Ez, rr = r + DIST_PAD;
			if (vx * Fx + vy * Fy + vz * Fz < -rr) return false;
			if (vx * _rx + vy * _ry + vz * _rz > rr) return false;
			if (vx * _lx + vy * _ly + vz * _lz > rr) return false;
			if (vx * _tx + vy * _ty + vz * _tz > rr) return false;
			if (vx * _bx + vy * _by + vz * _bz > rr) return false;
			return true;
		}
		/// <summary>False when the box (centre, half extents) lies wholly behind the eye or outside a side plane.</summary>
		public readonly bool BoxInView(double cx, double cy, double cz, double hx, double hy, double hz) {
			double vx = cx - Ex, vy = cy - Ey, vz = cz - Ez;
			return !Outside(vx, vy, vz, -Fx, -Fy, -Fz, hx, hy, hz)
				&& !Outside(vx, vy, vz, _rx, _ry, _rz, hx, hy, hz) && !Outside(vx, vy, vz, _lx, _ly, _lz, hx, hy, hz)
				&& !Outside(vx, vy, vz, _tx, _ty, _tz, hx, hy, hz) && !Outside(vx, vy, vz, _bx, _by, _bz, hx, hy, hz);
		}
		// whether a box centred at v (from the eye) lies wholly on the outer side of the plane with unit normal n
		private static bool Outside(double vx, double vy, double vz, double nx, double ny, double nz, double hx, double hy, double hz)
			=> vx * nx + vy * ny + vz * nz - (Math.Abs(nx) * hx + Math.Abs(ny) * hy + Math.Abs(nz) * hz) > DIST_PAD;
	}
}
