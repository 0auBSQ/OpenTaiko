using System;

namespace OpenTaiko {
	/// <summary>One live particle: a camera-facing billboard that moves, fades and resizes over its
	/// life. Owned by a <see cref="ParticleSystem"/>; billboarded by the rasterizer's transparent
	/// pass.</summary>
	internal struct Particle {
		public double X, Y, Z, VX, VY, VZ;
		public double Life, MaxLife;       // seconds remaining / total
		public double Size0, Size1;        // billboard size at birth → death (world units)
		public double R, G, B, A0;         // colour 0-255 + start opacity 0-1
		public double Gravity, Drag;       // m/s² downward, and velocity damping per second
		public bool Additive;              // true = add to the framebuffer (glow), false = alpha blend
		public int Sprite;                 // sprite id to billboard (-1 = flat square; soft circle etc.)
		public double Rot, RotVel;         // billboard rotation (radians) + spin rate (rad/s) — leaves/petals tumble
		public byte SizeCurve;             // 0 linear Size0→Size1, 1 ease-out, 2 pop (sin-arch: grow then die)
		public byte FadeCurve;             // 0 linear, 1 smooth in/out, 2 flash (bright pop then long tail)
	}

	/// <summary>A pool of particles. Lua emits bursts into it, calls Update(dt) each frame, and the
	/// rasterizer billboards every live particle in its transparent pass. Reusable across stages
	/// (muzzle flashes, impacts, smoke, beams, magic, …). Dead particles are swap-removed so the
	/// live set stays packed for cheap iteration.</summary>
	internal sealed class ParticleSystem {
		public Particle[] P = new Particle[256];
		public int Count;
		public int Cap = 4000;   // hard cap so a stray burst can't tank the framerate (raise via PsSetCap)
		public int CurSprite = -1;   // sprite stamped onto particles emitted next (set via PsSetSprite)
		// stamped onto the NEXT emits like CurSprite (PsSetNextRotation / PsSetNextCurves)
		public double CurRot, CurRotVel;
		public byte CurSizeCurve, CurFadeCurve;

		public void Add(in Particle p) {
			if (Count >= Cap) return;
			if (Count == P.Length) Array.Resize(ref P, Math.Min(P.Length * 2, Cap < 256 ? 256 : Cap));
			P[Count++] = p;
		}

		public void Clear() => Count = 0;

		/// <summary>Advance the simulation: age, gravity, drag, then integrate position.</summary>
		public void Update(double dt) {
			for (int i = Count - 1; i >= 0; i--) {
				ref Particle p = ref P[i];
				p.Life -= dt;
				if (p.Life <= 0) { P[i] = P[--Count]; continue; }   // swap-remove dead
				p.VY -= p.Gravity * dt;
				double df = 1.0 - p.Drag * dt; if (df < 0) df = 0;
				p.VX *= df; p.VY *= df; p.VZ *= df;
				p.X += p.VX * dt; p.Y += p.VY * dt; p.Z += p.VZ * dt;
			}
		}
	}
}
