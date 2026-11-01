using System;
using System.Collections.Generic;

namespace OpenTaiko {
	// ── Collider types (src/Lua/3D/Physics) ─────────────────────────────────────────────────────────
	// Shapes a physics body can use, kept in their own file as requested:
	//   • Sphere / Capsule  — on PhysicsBody (the default dynamic colliders).
	//   • BoxCollider       — an oriented box; apply to a body so it collides as a box vs the static soup.
	//   • MeshCollider      — collide against the WHOLE triangle geometry of an object/level (feed its tris
	//                         into a world's static soup). The "mesh collider on all the object's triangles".
	//   • WheelCollider     — a raycast suspension wheel (see PhysicsVehicle.cs / VehicleBody).

	// Collide against every triangle of an object/level mesh. Build it from tris (or quads, split into two),
	// then AddToWorld between PhysicsWorld.BeginStatic/EndStatic — the mesh becomes the static collision soup.
	public sealed class MeshCollider {
		private readonly List<double> _t = new();   // 9 doubles per tri (ax,ay,az, bx,by,bz, cx,cy,cz)

		public void AddTri(double ax, double ay, double az, double bx, double by, double bz, double cx, double cy, double cz) {
			_t.Add(ax); _t.Add(ay); _t.Add(az); _t.Add(bx); _t.Add(by); _t.Add(bz); _t.Add(cx); _t.Add(cy); _t.Add(cz);
		}
		public void AddQuad(double ax, double ay, double az, double bx, double by, double bz,
			double cx, double cy, double cz, double dx, double dy, double dz) {
			AddTri(ax, ay, az, bx, by, bz, cx, cy, cz);
			AddTri(ax, ay, az, cx, cy, cz, dx, dy, dz);
		}
		public int TriCount() => _t.Count / 9;
		public void Clear() => _t.Clear();
		// push this mesh's triangles into a world's static soup (call inside BeginStatic/EndStatic)
		public void AddToWorld(PhysicsWorld w) {
			for (int i = 0; i + 8 < _t.Count; i += 9)
				w.AddTri(_t[i], _t[i + 1], _t[i + 2], _t[i + 3], _t[i + 4], _t[i + 5], _t[i + 6], _t[i + 7], _t[i + 8]);
		}
	}

	// An oriented box collider (half-extents + a horizontal forward axis). Apply it to a body so the body
	// collides as a box vs the static soup (PhysicsWorld.CollideBoxStatic samples its 8 corners), and/or read
	// its world-space corners.
	public sealed class BoxCollider {
		public double Hx, Hy, Hz;            // half-extents: right, up, forward
		public double FwdX = 0, FwdZ = 1;    // unit forward (horizontal)

		public BoxCollider(double hx, double hy, double hz) { Hx = hx; Hy = hy; Hz = hz; }
		public void SetForward(double fx, double fz) { double l = Math.Sqrt(fx * fx + fz * fz); if (l > 1e-6) { FwdX = fx / l; FwdZ = fz / l; } }
		public void ApplyTo(PhysicsBody b) { b.SetBox(Hx, Hy, Hz); b.SetForward(FwdX, FwdZ); }
		// world corner for signs (sx,sy,sz) each ∈ {-1,+1}, about centre (cx,cy,cz)
		public void Corner(double cx, double cy, double cz, int sx, int sy, int sz, out double x, out double y, out double z) {
			double rx = FwdZ, rz = -FwdX;     // right axis
			x = cx + rx * (sx * Hx) + FwdX * (sz * Hz);
			y = cy + sy * Hy;
			z = cz + rz * (sx * Hx) + FwdZ * (sz * Hz);
		}
	}
}
