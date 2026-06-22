using System;

namespace OpenTaiko {
	// ── General-purpose colliders (src/Lua/3D/Colliders) ────────────────────────────────────────────────
	// A lightweight, reusable collision-query layer, distinct from the physics-sim shapes in 3D/Physics
	// (those collide a dynamic body vs a static triangle soup). These answer two questions any stage asks:
	//   • collider.Raycast(ox,oy,oz, dx,dy,dz, maxDist) -> hit distance (or -1 on a miss)
	//   • collider.CollideWith(other)                   -> true if it overlaps another Collider, OR if a Ray hits it
	// Each shape is a class (SphereCollider / AABBCollider / CapsuleCollider) sharing the Collider base. Built
	// from the COLLIDERS factory and used straight from Lua, e.g. doom's per-fighter body+head hit boxes.
	//
	// Lua usage:
	//   local box  = COLLIDERS:Box(cx,cy,cz, hx,hy,hz)      -- AABB half-extents
	//   box:SetCenter(x,y,z)                                 -- move it each frame
	//   local d = box:Raycast(ox,oy,oz, dx,dy,dz, maxDist)   -- d>=0 = hit distance, <0 = miss
	//   if a:CollideWith(b) then ... end                     -- a vs collider b (overlap)
	//   if box:CollideWith(COLLIDERS:Ray(ox,oy,oz,dx,dy,dz,len)) then ... end

	// A ray (origin + normalized direction + length). Pass to CollideWith, or build one to reuse across tests.
	public sealed class Ray {
		public double Ox, Oy, Oz, Dx, Dy, Dz, Len;
		public Ray(double ox, double oy, double oz, double dx, double dy, double dz, double len) {
			Set(ox, oy, oz, dx, dy, dz, len);
		}
		public void Set(double ox, double oy, double oz, double dx, double dy, double dz, double len) {
			Ox = ox; Oy = oy; Oz = oz; Len = len;
			double l = Math.Sqrt(dx * dx + dy * dy + dz * dz); if (l < 1e-9) l = 1;
			Dx = dx / l; Dy = dy / l; Dz = dz / l;
		}
	}

	public abstract class Collider {
		public double Cx, Cy, Cz;     // centre / reference point (SetCenter moves the shape rigidly)
		public object Tag;            // optional payload so a hit can be mapped back (e.g. the Lua fighter table)

		public void SetCenter(double x, double y, double z) { Cx = x; Cy = y; Cz = z; }
		public void SetTag(object tag) { Tag = tag; }
		public object GetTag() => Tag;

		// rough bounding-sphere radius (broad-phase reject)
		public abstract double BoundRadius();
		// ray hit distance for a UNIT direction within maxDist, or -1. Implemented per shape.
		public abstract double RaycastUnit(double ox, double oy, double oz, double dx, double dy, double dz, double maxDist);

		// public ray query (normalizes the direction first) — returns the hit distance along (dx,dy,dz) or -1
		public double Raycast(double ox, double oy, double oz, double dx, double dy, double dz, double maxDist) {
			double l = Math.Sqrt(dx * dx + dy * dy + dz * dz); if (l < 1e-9) return -1;
			return RaycastUnit(ox, oy, oz, dx / l, dy / l, dz / l, maxDist);
		}
		public bool HitsRay(Ray r) => RaycastUnit(r.Ox, r.Oy, r.Oz, r.Dx, r.Dy, r.Dz, r.Len) >= 0;

		// CollideWith accepts another Collider (overlap test) OR a Ray (does it hit me?).
		public bool CollideWith(object other) {
			if (other is Ray r) return HitsRay(r);
			if (other is Collider c) return Overlap(this, c);
			return false;
		}

		// ── overlap dispatch (symmetric) ──────────────────────────────────────────────────────────────
		public static bool Overlap(Collider a, Collider b) {
			// broad-phase: bounding spheres must touch
			double dx = a.Cx - b.Cx, dy = a.Cy - b.Cy, dz = a.Cz - b.Cz;
			double br = a.BoundRadius() + b.BoundRadius();
			if (dx * dx + dy * dy + dz * dz > br * br) return false;
			switch (a) {
				case SphereCollider sa:
					switch (b) {
						case SphereCollider sb: return SphereSphere(sa, sb);
						case AABBCollider bb: return SphereBox(sa, bb);
						case CapsuleCollider cb: return SphereCapsule(sa, cb);
					}
					break;
				case AABBCollider ba:
					switch (b) {
						case SphereCollider sb: return SphereBox(sb, ba);
						case AABBCollider bb: return BoxBox(ba, bb);
						case CapsuleCollider cb: return CapsuleBox(cb, ba);
					}
					break;
				case CapsuleCollider ca:
					switch (b) {
						case SphereCollider sb: return SphereCapsule(sb, ca);
						case AABBCollider bb: return CapsuleBox(ca, bb);
						case CapsuleCollider cb: return CapsuleCapsule(ca, cb);
					}
					break;
			}
			return false;
		}

		private static bool SphereSphere(SphereCollider a, SphereCollider b) {
			double dx = a.Cx - b.Cx, dy = a.Cy - b.Cy, dz = a.Cz - b.Cz, rr = a.R + b.R;
			return dx * dx + dy * dy + dz * dz <= rr * rr;
		}
		private static bool SphereBox(SphereCollider s, AABBCollider b) {
			double qx = Math.Max(b.Cx - b.Hx, Math.Min(s.Cx, b.Cx + b.Hx));
			double qy = Math.Max(b.Cy - b.Hy, Math.Min(s.Cy, b.Cy + b.Hy));
			double qz = Math.Max(b.Cz - b.Hz, Math.Min(s.Cz, b.Cz + b.Hz));
			double dx = s.Cx - qx, dy = s.Cy - qy, dz = s.Cz - qz;
			return dx * dx + dy * dy + dz * dz <= s.R * s.R;
		}
		private static bool BoxBox(AABBCollider a, AABBCollider b) {
			return Math.Abs(a.Cx - b.Cx) <= a.Hx + b.Hx
				&& Math.Abs(a.Cy - b.Cy) <= a.Hy + b.Hy
				&& Math.Abs(a.Cz - b.Cz) <= a.Hz + b.Hz;
		}
		private static bool SphereCapsule(SphereCollider s, CapsuleCollider c) {
			ClosestOnSeg(s.Cx, s.Cy, s.Cz, c.Cx - c.Hsx, c.Cy - c.Hsy, c.Cz - c.Hsz, c.Cx + c.Hsx, c.Cy + c.Hsy, c.Cz + c.Hsz, out double px, out double py, out double pz);
			double dx = s.Cx - px, dy = s.Cy - py, dz = s.Cz - pz, rr = s.R + c.R;
			return dx * dx + dy * dy + dz * dz <= rr * rr;
		}
		private static bool CapsuleBox(CapsuleCollider c, AABBCollider b) {
			// approximate: closest point on the capsule segment to the box centre, then sphere(R) vs box
			ClosestOnSeg(b.Cx, b.Cy, b.Cz, c.Cx - c.Hsx, c.Cy - c.Hsy, c.Cz - c.Hsz, c.Cx + c.Hsx, c.Cy + c.Hsy, c.Cz + c.Hsz, out double px, out double py, out double pz);
			double qx = Math.Max(b.Cx - b.Hx, Math.Min(px, b.Cx + b.Hx));
			double qy = Math.Max(b.Cy - b.Hy, Math.Min(py, b.Cy + b.Hy));
			double qz = Math.Max(b.Cz - b.Hz, Math.Min(pz, b.Cz + b.Hz));
			double dx = px - qx, dy = py - qy, dz = pz - qz;
			return dx * dx + dy * dy + dz * dz <= c.R * c.R;
		}
		private static bool CapsuleCapsule(CapsuleCollider a, CapsuleCollider b) {
			double d2 = SegSegDist2(a.Cx - a.Hsx, a.Cy - a.Hsy, a.Cz - a.Hsz, a.Cx + a.Hsx, a.Cy + a.Hsy, a.Cz + a.Hsz,
				b.Cx - b.Hsx, b.Cy - b.Hsy, b.Cz - b.Hsz, b.Cx + b.Hsx, b.Cy + b.Hsy, b.Cz + b.Hsz);
			double rr = a.R + b.R; return d2 <= rr * rr;
		}

		// ── shared geometry helpers ─────────────────────────────────────────────────────────────────────
		protected static void ClosestOnSeg(double px, double py, double pz, double ax, double ay, double az, double bx, double by, double bz, out double cx, out double cy, out double cz) {
			double ex = bx - ax, ey = by - ay, ez = bz - az;
			double len2 = ex * ex + ey * ey + ez * ez;
			double t = len2 > 1e-12 ? ((px - ax) * ex + (py - ay) * ey + (pz - az) * ez) / len2 : 0;
			if (t < 0) t = 0; else if (t > 1) t = 1;
			cx = ax + ex * t; cy = ay + ey * t; cz = az + ez * t;
		}
		// squared distance between two segments
		protected static double SegSegDist2(double p1x, double p1y, double p1z, double q1x, double q1y, double q1z,
			double p2x, double p2y, double p2z, double q2x, double q2y, double q2z) {
			double d1x = q1x - p1x, d1y = q1y - p1y, d1z = q1z - p1z;
			double d2x = q2x - p2x, d2y = q2y - p2y, d2z = q2z - p2z;
			double rx = p1x - p2x, ry = p1y - p2y, rz = p1z - p2z;
			double a = d1x * d1x + d1y * d1y + d1z * d1z;
			double e = d2x * d2x + d2y * d2y + d2z * d2z;
			double f = d2x * rx + d2y * ry + d2z * rz;
			double s, t;
			if (a <= 1e-12 && e <= 1e-12) { s = t = 0; }
			else if (a <= 1e-12) { s = 0; t = Clamp01(f / e); }
			else {
				double c = d1x * rx + d1y * ry + d1z * rz;
				if (e <= 1e-12) { t = 0; s = Clamp01(-c / a); }
				else {
					double b = d1x * d2x + d1y * d2y + d1z * d2z;
					double denom = a * e - b * b;
					s = denom > 1e-12 ? Clamp01((b * f - c * e) / denom) : 0;
					t = (b * s + f) / e;
					if (t < 0) { t = 0; s = Clamp01(-c / a); }
					else if (t > 1) { t = 1; s = Clamp01((b - c) / a); }
				}
			}
			double cx1 = p1x + d1x * s, cy1 = p1y + d1y * s, cz1 = p1z + d1z * s;
			double cx2 = p2x + d2x * t, cy2 = p2y + d2y * t, cz2 = p2z + d2z * t;
			double dx = cx1 - cx2, dy = cy1 - cy2, dz = cz1 - cz2;
			return dx * dx + dy * dy + dz * dz;
		}
		private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
	}

	// sphere about (Cx,Cy,Cz) radius R
	public sealed class SphereCollider : Collider {
		public double R;
		public SphereCollider(double r) { R = r; }
		public void SetRadius(double r) { R = r; }
		public override double BoundRadius() => R;
		public override double RaycastUnit(double ox, double oy, double oz, double dx, double dy, double dz, double maxDist) {
			double mx = Cx - ox, my = Cy - oy, mz = Cz - oz;
			double tca = mx * dx + my * dy + mz * dz;       // projection of centre onto the ray
			double d2 = mx * mx + my * my + mz * mz - tca * tca;
			if (d2 > R * R) return -1;
			double thc = Math.Sqrt(R * R - d2);
			double t = tca - thc; if (t < 0) t = tca + thc;  // first intersection ahead of the origin
			if (t < 0 || t > maxDist) return -1;
			return t;
		}
	}

	// axis-aligned box about (Cx,Cy,Cz) with half-extents (Hx,Hy,Hz)
	public sealed class AABBCollider : Collider {
		public double Hx, Hy, Hz;
		public AABBCollider(double hx, double hy, double hz) { Hx = hx; Hy = hy; Hz = hz; }
		public void SetHalfExtents(double hx, double hy, double hz) { Hx = hx; Hy = hy; Hz = hz; }
		public override double BoundRadius() => Math.Sqrt(Hx * Hx + Hy * Hy + Hz * Hz);
		public override double RaycastUnit(double ox, double oy, double oz, double dx, double dy, double dz, double maxDist) {
			// slab method
			double tmin = 0, tmax = maxDist;
			if (!Slab(ox, dx, Cx - Hx, Cx + Hx, ref tmin, ref tmax)) return -1;
			if (!Slab(oy, dy, Cy - Hy, Cy + Hy, ref tmin, ref tmax)) return -1;
			if (!Slab(oz, dz, Cz - Hz, Cz + Hz, ref tmin, ref tmax)) return -1;
			return tmin >= 0 ? tmin : (tmax <= maxDist ? tmax : -1);
		}
		private static bool Slab(double o, double d, double lo, double hi, ref double tmin, ref double tmax) {
			if (Math.Abs(d) < 1e-9) return o >= lo && o <= hi;     // parallel: inside the slab or miss
			double inv = 1.0 / d;
			double t1 = (lo - o) * inv, t2 = (hi - o) * inv;
			if (t1 > t2) { double tmp = t1; t1 = t2; t2 = tmp; }
			if (t1 > tmin) tmin = t1;
			if (t2 < tmax) tmax = t2;
			return tmin <= tmax;
		}
	}

	// capsule = a segment (centre ± half-segment vector Hs) of radius R. CapsuleY builds a vertical one.
	public sealed class CapsuleCollider : Collider {
		public double Hsx, Hsy, Hsz, R;
		public CapsuleCollider(double hsx, double hsy, double hsz, double r) { Hsx = hsx; Hsy = hsy; Hsz = hsz; R = r; }
		public override double BoundRadius() => Math.Sqrt(Hsx * Hsx + Hsy * Hsy + Hsz * Hsz) + R;
		public override double RaycastUnit(double ox, double oy, double oz, double dx, double dy, double dz, double maxDist) {
			// march a few samples and test the distance to the segment vs R (robust + simple; fine for queries).
			double ax = Cx - Hsx, ay = Cy - Hsy, az = Cz - Hsz, bx = Cx + Hsx, by = Cy + Hsy, bz = Cz + Hsz;
			// quick reject: ray vs bounding sphere
			double br = BoundRadius();
			double mx = Cx - ox, my = Cy - oy, mz = Cz - oz; double tca = mx * dx + my * dy + mz * dz;
			double bd2 = mx * mx + my * my + mz * mz - tca * tca;
			if (bd2 > br * br) return -1;
			double start = Math.Max(0, tca - br), end = Math.Min(maxDist, tca + br);
			if (end < start) return -1;
			int steps = 24; double prev = -1;
			for (int i = 0; i <= steps; i++) {
				double t = start + (end - start) * i / steps;
				double sx = ox + dx * t, sy = oy + dy * t, sz = oz + dz * t;
				ClosestOnSeg(sx, sy, sz, ax, ay, az, bx, by, bz, out double px, out double py, out double pz);
				double ddx = sx - px, ddy = sy - py, ddz = sz - pz;
				double dist2 = ddx * ddx + ddy * ddy + ddz * ddz;
				if (dist2 <= R * R) {
					// refine back toward the entry with a few bisection steps
					if (prev < 0) return t;
					double lo = prev, hi = t;
					for (int k = 0; k < 8; k++) {
						double mt = (lo + hi) * 0.5;
						double mxx = ox + dx * mt, myy = oy + dy * mt, mzz = oz + dz * mt;
						ClosestOnSeg(mxx, myy, mzz, ax, ay, az, bx, by, bz, out double qx, out double qy, out double qz);
						double e2 = (mxx - qx) * (mxx - qx) + (myy - qy) * (myy - qy) + (mzz - qz) * (mzz - qz);
						if (e2 <= R * R) hi = mt; else lo = mt;
					}
					return hi;
				}
				prev = t;
			}
			return -1;
		}
	}

	// Lua factory: COLLIDERS:Box(...) / :Sphere(...) / :Capsule(...) / :CapsuleY(...) / :Ray(...)
	public sealed class LuaCollidersFunc {
		public SphereCollider Sphere(double cx, double cy, double cz, double r) { var c = new SphereCollider(r); c.SetCenter(cx, cy, cz); return c; }
		public AABBCollider Box(double cx, double cy, double cz, double hx, double hy, double hz) { var c = new AABBCollider(hx, hy, hz); c.SetCenter(cx, cy, cz); return c; }
		public CapsuleCollider Capsule(double cx, double cy, double cz, double hsx, double hsy, double hsz, double r) { var c = new CapsuleCollider(hsx, hsy, hsz, r); c.SetCenter(cx, cy, cz); return c; }
		public CapsuleCollider CapsuleY(double cx, double cy, double cz, double halfHeight, double r) { var c = new CapsuleCollider(0, halfHeight, 0, r); c.SetCenter(cx, cy, cz); return c; }
		public Ray Ray(double ox, double oy, double oz, double dx, double dy, double dz, double len) => new Ray(ox, oy, oz, dx, dy, dz, len);
	}
}
