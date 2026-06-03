using System;
using System.Collections.Generic;

namespace OpenTaiko {
	// ── VehicleBody + WheelCollider (src/Lua/3D/Physics) ─────────────────────────────────────────────
	// A car-style body modelled on the common engine pattern (a chassis + raycast "wheels" with spring
	// suspension). To keep the proven horizontal collision (collide-and-slide vs the static soup, kart-vs-kart
	// impulse — see PhysicsWorld), the chassis IS a Character PhysicsBody that the world steps as usual; the
	// VehicleBody adds, ON TOP, four WheelColliders that RAYCAST the ground each frame to give:
	//   • a reliable multi-wheel "grounded" flag (steadier than a single sphere's floor test),
	//   • per-wheel suspension COMPRESSION (0 extended … 1 compressed) for wheel/visual bob, and
	//   • chassis PITCH/ROLL that follow the slope the wheels sit on (the suspension "lean" you see in a kart
	//     game over ramps and off-camber turns).
	// The arcade driving layer (engine/steer/grip) still sets the chassis velocity; the VehicleBody proxies the
	// body methods the driver uses, so it drops in where a Character body was.
	public sealed class WheelCollider {
		public double Lx, Lz;            // mount offset in chassis space: x = right, z = forward
		public double Radius = 0.32;
		public double Rest = 0.5;        // suspension rest length (chassis sits ~Rest above the wheel contact)
		public bool Steered, Powered;
		// runtime (read by the stage for visuals)
		public bool Grounded;
		public double Compression;       // 0 = fully extended (airborne), 1 = fully compressed
		public double ContactY;          // world ground Y under the wheel
		public double Spin;              // accumulated visual roll angle (rad)
		public double GetCompression() => Compression;
		public double GetSpin() => Spin;
		public bool IsGrounded() => Grounded;
	}

	public sealed class VehicleBody {
		internal PhysicsWorld World;
		public PhysicsBody Body;                 // the chassis collider (Character) — proven XZ/Y collision
		public readonly List<WheelCollider> Wheels = new();
		public double Yaw;                        // heading (rad)
		public double Pitch, Roll;                // chassis tilt eased from the wheel contacts (suspension look)
		public double TiltEase = 9.0;             // how fast pitch/roll chase their target
		public double MaxTilt = 0.5;              // clamp (rad) so a cliff edge can't flip the chassis
		public bool AllGrounded;                  // ≥3 wheels on the ground

		public VehicleBody(PhysicsWorld w, double radius, double mass) {
			World = w;
			Body = w.NewCharacter(radius);
			Body.Mass = mass > 0 ? mass : 1.0;
			Body.SetGravityEnabled(true);
			Body.Snap = true;                 // hug descending roads (no hops on slight down-slopes)
			Body.FloorThresholdNy = 0.35;     // treat steeper roads as drivable floor, not a wall (still rejects ~vertical borders)
		}

		public WheelCollider AddWheel(double lx, double lz, double radius, double rest, bool steered, bool powered) {
			var wh = new WheelCollider { Lx = lx, Lz = lz, Radius = radius > 0 ? radius : 0.32, Rest = rest > 0 ? rest : 0.5, Steered = steered, Powered = powered };
			Wheels.Add(wh);
			return wh;
		}
		public WheelCollider GetWheel(int i) => (i >= 0 && i < Wheels.Count) ? Wheels[i] : null;
		public int WheelCount() => Wheels.Count;

		// ── proxies so the VehicleBody drops in where a Character body was ──────────────────────────
		public void SetPos(double x, double y, double z) { Body.SetPos(x, y, z); }
		public void SetVelocity(double x, double y, double z) { Body.SetVelocity(x, y, z); }
		public void AddVelocity(double x, double y, double z) { Body.AddVelocity(x, y, z); }
		public void SetForward(double fx, double fz) { Body.SetForward(fx, fz); Yaw = Math.Atan2(fx, fz); }
		public void SetYaw(double y) { Yaw = y; Body.SetForward(Math.Sin(y), Math.Cos(y)); }
		public void SetGravityEnabled(bool g) { Body.SetGravityEnabled(g); }
		public void SetEnabled(bool e) { Body.SetEnabled(e); }
		public void SetCapsule(double halfLen) { Body.SetCapsule(halfLen); }
		public void SetRadius(double r) { Body.SetRadius(r); }
		public double GetX() => Body.X;
		public double GetY() => Body.Y;
		public double GetZ() => Body.Z;
		public double GetVx() => Body.Vx;
		public double GetVy() => Body.Vy;
		public double GetVz() => Body.Vz;
		public bool IsOnFloor() => AllGrounded || Body.OnFloor;
		public bool HitWall() => Body.HitWall();
		public double GetImpulseX() => Body.GetImpulseX();
		public double GetImpulseZ() => Body.GetImpulseZ();
		public double GetPitch() => Pitch;
		public double GetRoll() => Roll;

		// Raycast the wheels and derive grounded / compression / chassis tilt. Call once per frame AFTER the
		// world Step (the chassis body has its final pose). `speed` spins the wheels for the visual.
		public void UpdateWheels(double dt, double speed) {
			double fx = Math.Sin(Yaw), fz = Math.Cos(Yaw);     // forward
			double rx = Math.Cos(Yaw), rz = -Math.Sin(Yaw);    // right
			int grounded = 0;
			double frontY = 0, rearY = 0, leftY = 0, rightY = 0; int nF = 0, nR = 0, nL = 0, nRt = 0;
			double wheelbase = 1.0, track = 1.0;
			foreach (var wh in Wheels) {
				if (Math.Abs(wh.Lz) * 2 > wheelbase) wheelbase = Math.Abs(wh.Lz) * 2;
				if (Math.Abs(wh.Lx) * 2 > track) track = Math.Abs(wh.Lx) * 2;
				double wx = Body.X + rx * wh.Lx + fx * wh.Lz;
				double wz = Body.Z + rz * wh.Lx + fz * wh.Lz;
				double top = Body.Y + wh.Rest;                 // cast from above the wheel mount
				double maxD = wh.Rest * 2 + wh.Radius * 2 + 0.6;
				if (World.Raycast(wx, top, wz, 0, -1, 0, maxD, out double d, out _, out _, out _)) {
					wh.ContactY = top - d;
					double ride = Body.Y - wh.ContactY;        // how high the chassis sits over this wheel's ground
					double full = wh.Rest + wh.Radius;
					double c = 1.0 - ride / (full > 1e-3 ? full : 1.0);
					wh.Compression = c < 0 ? 0 : (c > 1 ? 1 : c);
					wh.Grounded = ride <= full + 0.25;
				} else {
					wh.Grounded = false; wh.Compression = 0; wh.ContactY = Body.Y - wh.Rest - wh.Radius;
				}
				if (wh.Grounded) {
					grounded++;
					if (wh.Lz >= 0) { frontY += wh.ContactY; nF++; } else { rearY += wh.ContactY; nR++; }
					if (wh.Lx < 0) { leftY += wh.ContactY; nL++; } else { rightY += wh.ContactY; nRt++; }
				}
				wh.Spin += speed * dt / (wh.Radius > 0.05 ? wh.Radius : 0.05);
			}
			// pitch from front-vs-rear contact, roll from left-vs-right; only when supported, else level out
			double tgtPitch = 0, tgtRoll = 0;
			if (grounded >= 3) {
				if (nF > 0 && nR > 0) tgtPitch = Math.Atan2(rearY / nR - frontY / nF, wheelbase);   // nose down on a climb's far side
				if (nL > 0 && nRt > 0) tgtRoll = Math.Atan2(leftY / nL - rightY / nRt, track);
			}
			if (tgtPitch > MaxTilt) tgtPitch = MaxTilt; else if (tgtPitch < -MaxTilt) tgtPitch = -MaxTilt;
			if (tgtRoll > MaxTilt) tgtRoll = MaxTilt; else if (tgtRoll < -MaxTilt) tgtRoll = -MaxTilt;
			double k = 1 - Math.Exp(-TiltEase * dt);
			Pitch += (tgtPitch - Pitch) * k;
			Roll += (tgtRoll - Roll) * k;
			AllGrounded = grounded >= 3;
		}
	}
}
