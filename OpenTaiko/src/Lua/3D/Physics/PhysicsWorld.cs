using System;
using System.Collections.Generic;

namespace OpenTaiko {
	// ── A small 3D physics/collision module (src/Lua/3D/Physics) ─────────────────────────────────────
	// Body roles (see PhysicsBody.cs): Character (kinematic, collide-and-slide) and Rigid (gravity + momentum
	// trades). Static collision geometry is a TRIANGLE SOUP. Collision = sphere-vs-triangle (closest-point,
	// Ericson "Real-Time Collision Detection") + an iterative push-out / velocity-projection slide (the MDN
	// "3D collision detection" + realtimerendering intersection references). Dynamic-vs-dynamic is
	// sphere-vs-sphere with a momentum-trading impulse, resolved EACH sub-step so fast bodies neither tunnel
	// thin walls nor force through each other.
	//
	// Lua:  local w = PHYSICS:NewWorld(); w:SetGravity(0,-32,0)
	//       w:BeginStatic(); w:AddTri(...)/w:AddQuad(...) ×N; w:EndStatic()
	//       local b = w:NewCharacter(0.6)  -- or w:NewRigid(0.6, mass)
	//       b:SetPos(x,y,z); b:SetVelocity(vx,vy,vz)
	//       w:Step(dt)
	//       local x,y,z = b:GetX(),b:GetY(),b:GetZ(); local onGround = b:IsOnFloor()

	internal struct PhysTri {
		public double ax, ay, az, bx, by, bz, cx, cy, cz;
		public double nx, ny, nz;        // unit face normal
	}

	public sealed class PhysicsWorld {
		private readonly List<PhysTri> _tris = new();
		private readonly List<PhysicsBody> _bodies = new();
		public double Gx = 0, Gy = -32, Gz = 0;
		public double FloorMaxY = 0.5;          // contact normal.y above this counts as "floor"
		public int SlideIters = 4;

		// uniform-grid broadphase over the static tris (XZ; tracks are roughly planar)
		private double _cell = 8.0;
		private readonly Dictionary<long, List<int>> _grid = new();
		private static long Key(int cx, int cz) => ((long)cx << 32) ^ (uint)cz;

		public void SetGravity(double x, double y, double z) { Gx = x; Gy = y; Gz = z; }
		public void SetFloorMaxAngleY(double y) { FloorMaxY = y; }

		public void BeginStatic() { _tris.Clear(); _grid.Clear(); }
		public void AddTri(double ax, double ay, double az, double bx, double by, double bz, double cx, double cy, double cz) {
			double ux = bx - ax, uy = by - ay, uz = bz - az;
			double vx = cx - ax, vy = cy - ay, vz = cz - az;
			double nx = uy * vz - uz * vy, ny = uz * vx - ux * vz, nz = ux * vy - uy * vx;
			double l = Math.Sqrt(nx * nx + ny * ny + nz * nz); if (l < 1e-12) return;   // degenerate
			nx /= l; ny /= l; nz /= l;
			_tris.Add(new PhysTri { ax = ax, ay = ay, az = az, bx = bx, by = by, bz = bz, cx = cx, cy = cy, cz = cz, nx = nx, ny = ny, nz = nz });
		}
		// also accept a quad (two tris) for convenience
		public void AddQuad(double ax, double ay, double az, double bx, double by, double bz,
			double cx, double cy, double cz, double dx, double dy, double dz) {
			AddTri(ax, ay, az, bx, by, bz, cx, cy, cz);
			AddTri(ax, ay, az, cx, cy, cz, dx, dy, dz);
		}
		public void EndStatic() {
			_grid.Clear();
			for (int i = 0; i < _tris.Count; i++) {
				var t = _tris[i];
				int x0 = (int)Math.Floor(Math.Min(t.ax, Math.Min(t.bx, t.cx)) / _cell);
				int x1 = (int)Math.Floor(Math.Max(t.ax, Math.Max(t.bx, t.cx)) / _cell);
				int z0 = (int)Math.Floor(Math.Min(t.az, Math.Min(t.bz, t.cz)) / _cell);
				int z1 = (int)Math.Floor(Math.Max(t.az, Math.Max(t.bz, t.cz)) / _cell);
				for (int cz = z0; cz <= z1; cz++)
					for (int cx = x0; cx <= x1; cx++) {
						long k = Key(cx, cz);
						if (!_grid.TryGetValue(k, out var l)) { l = new List<int>(); _grid[k] = l; }
						l.Add(i);
					}
			}
		}

		// ── raycast vs the static soup (Möller–Trumbore), used by VehicleBody wheel suspension ──────────
		private static bool RayTri(double ox, double oy, double oz, double dx, double dy, double dz, in PhysTri t, out double dist) {
			dist = 0;
			double e1x = t.bx - t.ax, e1y = t.by - t.ay, e1z = t.bz - t.az;
			double e2x = t.cx - t.ax, e2y = t.cy - t.ay, e2z = t.cz - t.az;
			double px = dy * e2z - dz * e2y, py = dz * e2x - dx * e2z, pz = dx * e2y - dy * e2x;
			double det = e1x * px + e1y * py + e1z * pz;
			if (det > -1e-9 && det < 1e-9) return false;        // ray parallel to the triangle
			double inv = 1.0 / det;
			double tx = ox - t.ax, ty = oy - t.ay, tz = oz - t.az;
			double u = (tx * px + ty * py + tz * pz) * inv; if (u < 0 || u > 1) return false;
			double qx = ty * e1z - tz * e1y, qy = tz * e1x - tx * e1z, qz = tx * e1y - ty * e1x;
			double v = (dx * qx + dy * qy + dz * qz) * inv; if (v < 0 || u + v > 1) return false;
			dist = (e2x * qx + e2y * qy + e2z * qz) * inv;
			return dist > 1e-6;
		}
		// Cast a ray (origin + unit-ish dir × maxDist). Returns true on the NEAREST hit within maxDist, with the
		// hit distance and the surface normal. Iterates the grid cells the ray's XZ span touches.
		public bool Raycast(double ox, double oy, double oz, double dx, double dy, double dz, double maxDist,
			out double hitDist, out double hnx, out double hny, out double hnz) {
			hitDist = maxDist; hnx = 0; hny = 1; hnz = 0; bool hit = false;
			double ex = ox + dx * maxDist, ez = oz + dz * maxDist;
			int x0 = (int)Math.Floor(Math.Min(ox, ex) / _cell), x1 = (int)Math.Floor(Math.Max(ox, ex) / _cell);
			int z0 = (int)Math.Floor(Math.Min(oz, ez) / _cell), z1 = (int)Math.Floor(Math.Max(oz, ez) / _cell);
			for (int cz = z0; cz <= z1; cz++) for (int cx = x0; cx <= x1; cx++) {
				if (!_grid.TryGetValue(Key(cx, cz), out var list)) continue;
				for (int li = 0; li < list.Count; li++) {
					if (RayTri(ox, oy, oz, dx, dy, dz, _tris[list[li]], out double tt) && tt < hitDist) {
						hitDist = tt; var t = _tris[list[li]]; hnx = t.nx; hny = t.ny; hnz = t.nz; hit = true;
					}
				}
			}
			return hit;
		}
		// returns the ground height (Y) straight below/above (x, z) near startY, or NaN if none within reach
		public double GroundYAt(double x, double startY, double z, double reach) {
			if (Raycast(x, startY, z, 0, -1, 0, reach, out double d, out _, out _, out _)) return startY - d;
			return double.NaN;
		}

		internal IReadOnlyList<PhysTri> Tris => _tris;
		public PhysicsBody NewCharacter(double radius) { var b = new PhysicsBody { Radius = radius, Kind = PhysBodyKind.Character, World = this }; _bodies.Add(b); return b; }
		public VehicleBody NewVehicle(double radius, double mass) { var v = new VehicleBody(this, radius, mass); return v; }
		public PhysicsBody NewRigid(double radius, double mass) { var b = new PhysicsBody { Radius = radius, Mass = mass, Kind = PhysBodyKind.Rigid, UseGravity = true, World = this }; _bodies.Add(b); return b; }
		// a gravity-driven body that collides as an oriented BOX vs the static soup (uses SetForward to orient)
		public PhysicsBody NewBox(double hx, double hy, double hz, double mass) {
			var b = new PhysicsBody { Mass = mass > 0 ? mass : 1, Kind = PhysBodyKind.Rigid, UseGravity = true, World = this };
			b.SetBox(hx, hy, hz);
			b.Radius = Math.Max(hx, Math.Max(hy, hz));   // bounding radius for the broadphase / dynamic pass
			_bodies.Add(b); return b;
		}
		public MeshCollider NewMeshCollider() => new MeshCollider();      // build it, then call w:AddMesh(mc) inside BeginStatic/EndStatic
		public void AddMesh(MeshCollider mc) { mc?.AddToWorld(this); }
		public void RemoveBody(PhysicsBody b) { _bodies.Remove(b); }
		public int StaticTriCount() => _tris.Count;

		// closest point on triangle i to point p (Ericson, Real-Time Collision Detection §5.1.5)
		private void ClosestOnTri(in PhysTri t, double px, double py, double pz, out double ox, out double oy, out double oz) {
			double abx = t.bx - t.ax, aby = t.by - t.ay, abz = t.bz - t.az;
			double acx = t.cx - t.ax, acy = t.cy - t.ay, acz = t.cz - t.az;
			double apx = px - t.ax, apy = py - t.ay, apz = pz - t.az;
			double d1 = abx * apx + aby * apy + abz * apz;
			double d2 = acx * apx + acy * apy + acz * apz;
			if (d1 <= 0 && d2 <= 0) { ox = t.ax; oy = t.ay; oz = t.az; return; }
			double bpx = px - t.bx, bpy = py - t.by, bpz = pz - t.bz;
			double d3 = abx * bpx + aby * bpy + abz * bpz;
			double d4 = acx * bpx + acy * bpy + acz * bpz;
			if (d3 >= 0 && d4 <= d3) { ox = t.bx; oy = t.by; oz = t.bz; return; }
			double vc = d1 * d4 - d3 * d2;
			if (vc <= 0 && d1 >= 0 && d3 <= 0) { double v = d1 / (d1 - d3); ox = t.ax + abx * v; oy = t.ay + aby * v; oz = t.az + abz * v; return; }
			double cpx = px - t.cx, cpy = py - t.cy, cpz = pz - t.cz;
			double d5 = abx * cpx + aby * cpy + abz * cpz;
			double d6 = acx * cpx + acy * cpy + acz * cpz;
			if (d6 >= 0 && d5 <= d6) { ox = t.cx; oy = t.cy; oz = t.cz; return; }
			double vb = d5 * d2 - d1 * d6;
			if (vb <= 0 && d2 >= 0 && d6 <= 0) { double w = d2 / (d2 - d6); ox = t.ax + acx * w; oy = t.ay + acy * w; oz = t.az + acz * w; return; }
			double va = d3 * d6 - d5 * d4;
			if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0) { double w = (d4 - d3) / ((d4 - d3) + (d5 - d6)); ox = t.bx + (t.cx - t.bx) * w; oy = t.by + (t.cy - t.by) * w; oz = t.bz + (t.cz - t.bz) * w; return; }
			double denom = 1.0 / (va + vb + vc);
			double vv = vb * denom, ww = vc * denom;
			ox = t.ax + abx * vv + acx * ww; oy = t.ay + aby * vv + acy * ww; oz = t.az + abz * vv + acz * ww;
		}

		// one static contact: push the body out + slide its velocity. Floor contacts (normal mostly up) push
		// the full normal and slide the full velocity (rest on ground / ramp launch); wall/edge contacts push +
		// slide HORIZONTALLY only so a grounded body is shoved sideways, never popped into the air.
		private void Contact(PhysicsBody b, double nx, double ny, double nz, double push, double oy) {
			double fty = b.FloorThresholdNy >= 0 ? b.FloorThresholdNy : FloorMaxY;   // karts allow steeper "floor"
			if (ny > fty && oy < b.Y) {   // floor only if the contact is BELOW the centre (not a wall top)
				b.X += nx * push; b.Y += ny * push; b.Z += nz * push;
				double vn = b.Vx * nx + b.Vy * ny + b.Vz * nz;
				if (vn < 0) { b.Vx -= vn * nx; b.Vy -= vn * ny; b.Vz -= vn * nz; }
				b.OnFloor = true; b.FloorNx = nx; b.FloorNy = ny; b.FloorNz = nz;
			} else {
				double hl = Math.Sqrt(nx * nx + nz * nz);
				if (hl > 1e-6) {
					double hnx = nx / hl, hnz = nz / hl;
					b.X += hnx * push; b.Z += hnz * push;
					double vh = b.Vx * hnx + b.Vz * hnz;
					if (vh < 0) { b.Vx -= vh * hnx; b.Vz -= vh * hnz; }
					b.WallHit = true;
				}
			}
		}

		// BOX-vs-soup: sample the 8 oriented corners (each a tiny sphere) and push the body out of any tri they
		// penetrate. Approximate (corner-based) but enough for an arcade box resting/sliding on the track.
		private void CollideBoxStatic(PhysicsBody b) {
			double rx = b.FwdZ, rz = -b.FwdX;                 // right axis (forward = FwdX,FwdZ; up = +Y)
			const double cr = 0.12;                            // corner contact radius
			for (int iter = 0; iter < SlideIters; iter++) {
				bool any = false;
				for (int sx = -1; sx <= 1; sx += 2) for (int sy = -1; sy <= 1; sy += 2) for (int sz = -1; sz <= 1; sz += 2) {
					double pxp = b.X + rx * (sx * b.Hx) + b.FwdX * (sz * b.Hz);
					double pyp = b.Y + sy * b.Hy;
					double pzp = b.Z + rz * (sx * b.Hx) + b.FwdZ * (sz * b.Hz);
					int gcx = (int)Math.Floor(pxp / _cell), gcz = (int)Math.Floor(pzp / _cell);
					for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++) {
						if (!_grid.TryGetValue(Key(gcx + dx, gcz + dz), out var list)) continue;
						for (int li = 0; li < list.Count; li++) {
							ClosestOnTri(_tris[list[li]], pxp, pyp, pzp, out double ox, out double oy, out double oz);
							double ex = pxp - ox, ey = pyp - oy, ez = pzp - oz;
							double d2 = ex * ex + ey * ey + ez * ez;
							if (d2 >= cr * cr || d2 < 1e-12) continue;
							double d = Math.Sqrt(d2);
							Contact(b, ex / d, ey / d, ez / d, cr - d, oy);
							any = true;
						}
					}
				}
				if (!any) break;
			}
		}

		// resolve one body against the static soup: push out of every overlapping tri + slide its velocity
		private void CollideStatic(PhysicsBody b) {
			b.OnFloor = false; b.WallHit = false;
			if (b.Shape == PhysShape.Box) { CollideBoxStatic(b); return; }
			double r = b.Radius;
			for (int iter = 0; iter < SlideIters; iter++) {
				int gcx = (int)Math.Floor(b.X / _cell), gcz = (int)Math.Floor(b.Z / _cell);
				bool any = false;
				for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++) {
					if (!_grid.TryGetValue(Key(gcx + dx, gcz + dz), out var list)) continue;
					for (int li = 0; li < list.Count; li++) {
						var t = _tris[list[li]];
						ClosestOnTri(t, b.X, b.Y, b.Z, out double ox, out double oy, out double oz);
						double ex = b.X - ox, ey = b.Y - oy, ez = b.Z - oz;
						double d2 = ex * ex + ey * ey + ez * ez;
						if (d2 >= r * r || d2 < 1e-12) continue;
						double d = Math.Sqrt(d2);
						double nx = ex / d, ny = ey / d, nz = ez / d;     // contact normal (out of the surface)
						double push = r - d;
						double fty = b.FloorThresholdNy >= 0 ? b.FloorThresholdNy : FloorMaxY;   // karts: steeper roads still count as floor
						// FLOOR only if the normal points up AND the contact is BELOW the body centre (genuine
						// ground/ramp the body sits ON). A contact at/above the centre is a wall face or a wall's
						// TOP EDGE — treat it as a wall (horizontal push only) so the kart can't ride up and hop
						// over a barrier (no height-axis recoil from any side/edge collision).
						if (ny > fty && oy < b.Y) {
							// floor / ramp: push along the FULL normal + slide the full velocity, so the body
							// rests on the ground and a ramp's slope redirects motion upward (the jump launch)
							b.X += nx * push; b.Y += ny * push; b.Z += nz * push;
							double vn = b.Vx * nx + b.Vy * ny + b.Vz * nz;
							if (vn < 0) { b.Vx -= vn * nx; b.Vy -= vn * ny; b.Vz -= vn * nz; }
							b.OnFloor = true; b.FloorNx = nx; b.FloorNy = ny; b.FloorNz = nz;
						} else {
							// wall / edge: push + slide HORIZONTALLY only, so a grounded vehicle that scrapes a
							// barrier (or its top/bottom edge) is shoved sideways, never popped up into the air
							double hl = Math.Sqrt(nx * nx + nz * nz);
							if (hl > 1e-6) {
								double hnx = nx / hl, hnz = nz / hl;
								b.X += hnx * push; b.Z += hnz * push;
								double vh = b.Vx * hnx + b.Vz * hnz;
								if (vh < 0) { b.Vx -= vh * hnx; b.Vz -= vh * hnz; }
								b.WallHit = true;
							}
						}
						any = true;
					}
				}
				if (!any) break;
			}
		}

		public void Step(double dt) {
			// SUB-STEP so a fast body can't tunnel through a thin wall in one frame: cap the per-sub move to
			// well under the smallest body radius.
			double maxv = 0;
			foreach (var b in _bodies) { if (!b.Enabled) continue; double v = Math.Abs(b.Vx) + Math.Abs(b.Vy) + Math.Abs(b.Vz); if (v > maxv) maxv = v; }
			int nsub = (int)Math.Ceiling(maxv * dt / 0.35); if (nsub < 1) nsub = 1; if (nsub > 8) nsub = 8;
			double h = dt / nsub;
			foreach (var b in _bodies) { b.ImpX = 0; b.ImpZ = 0; }   // accumulate this step's bump shove

			for (int sub = 0; sub < nsub; sub++) {
				foreach (var b in _bodies) {
					if (!b.Enabled) continue;
					if (b.UseGravity && !b.OnFloor) { b.Vx += Gx * h; b.Vy += Gy * h; b.Vz += Gz * h; }
					b.X += b.Vx * h; b.Y += b.Vy * h; b.Z += b.Vz * h;
					CollideStatic(b);
					if (b.OnFloor && b.Vy < 0) b.Vy = 0;   // rest on the ground (no gravity build-up while supported)
					// GROUND-SNAP (karts): if we were grounded and didn't intentionally launch (Vy not strongly up),
					// glue down to ground just below us so a descending road doesn't make the kart hop/airborne. A
					// real jump (ramp gives big +Vy) or a gap (no ground within reach) is left alone → still flies.
					if (b.Snap && b.WasGrounded && !b.OnFloor && b.Vy <= 1.0) {
						double gy = GroundYAt(b.X, b.Y + b.Radius, b.Z, b.Radius + 0.7);
						if (!double.IsNaN(gy)) {
							double targetY = gy + b.Radius;
							if (b.Y > targetY && b.Y - targetY <= 0.7) {   // a small gap below → snap onto it
								b.Y = targetY; if (b.Vy < 0) b.Vy = 0;
								b.OnFloor = true; b.FloorNx = 0; b.FloorNy = 1; b.FloorNz = 0;
							}
						}
					}
					b.WasGrounded = b.OnFloor;
				}
				ResolveDynamic();   // EVERY sub-step → a body can't be driven deep into another then snap out (jitter)
			}
		}

		// dynamic-vs-dynamic: sphere-sphere separation + momentum-trading impulse, resolved in the HORIZONTAL
		// plane only — these are grounded vehicles, so a bump must shove them apart sideways, never launch one
		// up into the air (the contact normal's vertical part is dropped). Mass weights the response.
		public double Restitution = 0.1;
		public double MaxImpulseDV = 7.0;          // cap the per-body velocity change from one bump (no rocketing)
		public double LevelGap = 2.0;              // bodies more than this far apart in Y are on different levels
		// closest point on a body's collider (capsule segment, or just the centre for a sphere) to (px,pz), XZ
		private static void ClosestXZ(PhysicsBody b, double px, double pz, out double ox, out double oz) {
			if (b.Shape != PhysShape.Capsule || b.HalfLen <= 0) { ox = b.X; oz = b.Z; return; }
			double ax = b.X - b.FwdX * b.HalfLen, az = b.Z - b.FwdZ * b.HalfLen;   // segment ends
			double bx = b.X + b.FwdX * b.HalfLen, bz = b.Z + b.FwdZ * b.HalfLen;
			double ex = bx - ax, ez = bz - az; double el = ex * ex + ez * ez;
			double t = el > 1e-9 ? ((px - ax) * ex + (pz - az) * ez) / el : 0; if (t < 0) t = 0; else if (t > 1) t = 1;
			ox = ax + ex * t; oz = az + ez * t;
		}
		private void ResolveDynamic() {
			for (int i = 0; i < _bodies.Count; i++) {
				var a = _bodies[i]; if (!a.Enabled) continue;
				for (int j = i + 1; j < _bodies.Count; j++) {
					var c = _bodies[j]; if (!c.Enabled) continue;
					if (Math.Abs(a.Y - c.Y) > LevelGap) continue;   // different levels (e.g. figure-8 strands) → no collision
					// contact points (capsule-aware): closest point on each collider to the other's centre
					ClosestXZ(a, c.X, c.Z, out double pax, out double paz);
					ClosestXZ(c, a.X, a.Z, out double pcx, out double pcz);
					double dx = pcx - pax, dz = pcz - paz;
					double rr = a.Radius + c.Radius;
					double hd2 = dx * dx + dz * dz;
					if (hd2 >= rr * rr) continue;
					double hd, nx, nz;
					if (hd2 < 1e-6) { nx = 1; nz = 0; hd = 0; }     // stacked → shove apart so they never sit overlapping
					else { hd = Math.Sqrt(hd2); nx = dx / hd; nz = dz / hd; }
					double pen = (rr - hd);
					double ma = a.Mass, mc = c.Mass, msum = ma + mc;
					a.X -= nx * pen * (mc / msum); a.Z -= nz * pen * (mc / msum);
					c.X += nx * pen * (ma / msum); c.Z += nz * pen * (ma / msum);
					double rvn = (c.Vx - a.Vx) * nx + (c.Vz - a.Vz) * nz;   // closing speed (horizontal)
					if (rvn < 0) {
						double jimp = -(1 + Restitution) * rvn / (1 / ma + 1 / mc);
						double cap = MaxImpulseDV / Math.Max(1.0 / ma, 1.0 / mc);   // so each body's Δv ≤ MaxImpulseDV
						if (jimp > cap) jimp = cap;
						double dax = -(jimp / ma) * nx, daz = -(jimp / ma) * nz;
						double dcx = (jimp / mc) * nx, dcz = (jimp / mc) * nz;
						a.Vx += dax; a.Vz += daz;   // horizontal only → no vertical pop
						c.Vx += dcx; c.Vz += dcz;
						a.ImpX += dax; a.ImpZ += daz;   // record the shove so BOTH bodies get a lasting knock
						c.ImpX += dcx; c.ImpZ += dcz;
					}
				}
			}
		}
	}

	public sealed class LuaPhysicsFunc {
		public PhysicsWorld NewWorld() => new PhysicsWorld();
	}
}
