using System;

namespace OpenTaiko {
	// Body roles for the 3D physics world (split by responsibility so stages stop hand-rolling collision):
	//   • Character — kinematic; YOU set its velocity each frame, the world MOVES it with collide-and-slide
	//                 against the static soup (slides along walls / rests on the ground, never dead-stops).
	//                 Karts / players use this.
	//   • Rigid     — gravity-driven and trades momentum with other dynamic bodies on contact.
	// (A future VehicleBody — a Rigid body with raycast "wheels" + suspension that floats the chassis above
	//  the ground and applies engine/steer forces — would live here too; for now karts are Character spheres
	//  driven by an arcade layer.)
	public enum PhysBodyKind { Character, Rigid }

	// collider shapes. Sphere = a ball; Capsule = a horizontal pill (a segment of HalfLen along the body's
	// forward axis, swept by Radius) — a far more car-shaped collider than a ball, so karts bump along their
	// length and don't roll oddly. The capsule's orientation is set each frame from the kart's heading.
	public enum PhysShape { Sphere, Capsule, Box }

	public sealed class PhysicsBody {
		internal PhysicsWorld World;
		public double X, Y, Z;                     // body CENTRE
		public double Vx, Vy, Vz;
		public double Radius = 0.5;
		public double Mass = 1.0;
		public PhysShape Shape = PhysShape.Sphere;
		public double HalfLen = 0.0;               // capsule half-length (0 = sphere)
		public double Hx = 0.5, Hy = 0.5, Hz = 0.5; // BOX half-extents (right, up, forward) in body space
		public double FwdX = 0, FwdZ = 1;          // capsule/box forward axis (unit, horizontal)
		public bool UseGravity = false;
		public bool Enabled = true;
		public bool OnFloor;
		public bool WallHit;                       // touched a near-vertical surface this step (a wall/barrier)
		public double FloorNx, FloorNy, FloorNz;   // last floor contact normal
		public double ImpX, ImpZ;                  // net dynamic-collision impulse this step (the bump shove)
		internal PhysBodyKind Kind;
		// ── slope handling (opt-in; karts set these so they hug descents + climb steeper roads without the
		//    shared character collision changing for doom/iso players) ──
		public bool Snap;                          // glue a grounded body down onto descending ground (no air-hops)
		internal bool WasGrounded;                 // OnFloor at the end of the previous sub-step (for Snap)
		public double FloorThresholdNy = -1;       // floor-vs-wall normal.y cutoff for THIS body (-1 = world default)
		public void SetSnap(bool s) { Snap = s; }

		// ── collision layer/mask (dynamic-vs-dynamic pass). Defaults (layer 0, mask all) keep every
		//    existing stage byte-identical; OWM3d characters set Mask=0 so NPCs never block the player. ──
		public int Layer = 0;                      // this body's layer index (0..30)
		public int Mask = ~0;                      // bit i set = this body collides with Layer-i bodies
		public void SetCollisionLayer(int l) { Layer = l < 0 ? 0 : (l > 30 ? 30 : l); }
		public void SetCollisionMask(int m) { Mask = m; }

		// ── SmoothContacts (opt-in, character controllers): gathered deepest-contact resolution with an
		//    internal-edge weld on floor seams, passive-touch wall filtering and a floor-gated step-down
		//    snap. Karts/doom bodies never set it, so their solver path is byte-identical. ──
		public bool SmoothContacts;
		public double WallNx, WallNz;              // last wall contact normal (horizontal, unit) when WallHit
		public void SetSmoothContacts(bool on) { SmoothContacts = on; }
		public double GetWallNx() => WallNx;
		public double GetWallNz() => WallNz;

		public void SetPos(double x, double y, double z) { X = x; Y = y; Z = z; }
		public void SetVelocity(double x, double y, double z) { Vx = x; Vy = y; Vz = z; }
		public void AddVelocity(double x, double y, double z) { Vx += x; Vy += y; Vz += z; }
		public void SetRadius(double r) { Radius = r > 0.01 ? r : 0.01; }
		public void SetCapsule(double halfLen) { Shape = PhysShape.Capsule; HalfLen = halfLen > 0 ? halfLen : 0; }
		public void SetBox(double hx, double hy, double hz) { Shape = PhysShape.Box; Hx = hx; Hy = hy; Hz = hz; }
		public void SetForward(double fx, double fz) { double l = Math.Sqrt(fx * fx + fz * fz); if (l > 1e-6) { FwdX = fx / l; FwdZ = fz / l; } }
		public void SetGravityEnabled(bool g) { UseGravity = g; }
		public void SetEnabled(bool e) { Enabled = e; }
		public void GetPos(out double x, out double y, out double z) { x = X; y = Y; z = Z; }
		public void GetVelocity(out double x, out double y, out double z) { x = Vx; y = Vy; z = Vz; }
		public double GetX() => X;
		public double GetY() => Y;
		public double GetZ() => Z;
		public double GetVx() => Vx;
		public double GetVy() => Vy;
		public double GetVz() => Vz;
		public double Speed() => Math.Sqrt(Vx * Vx + Vy * Vy + Vz * Vz);
		public bool IsOnFloor() => OnFloor;
		public bool HitWall() => WallHit;
		public double GetImpulseX() => ImpX;     // the dynamic-collision shove dealt to this body this step
		public double GetImpulseZ() => ImpZ;
	}
}
