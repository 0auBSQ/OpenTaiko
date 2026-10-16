using System;
using System.Collections.Generic;

namespace OpenTaiko {
	// Particle systems.
	public partial class Lua3DScene {
		// ── Particle systems ──────────────────────────────────────────────────────────────
		// A reusable billboard particle pool. Lua creates a system, emits bursts, and calls
		// PsUpdate(dt) each frame; live particles render automatically in the transparent pass.
		internal readonly List<ParticleSystem> _particleSystems = new();
		private int _psIdBase;   // id of _particleSystems[0]: ClearObjects moves it past every id handed out
		private readonly Random _prng = new();
		private ParticleSystem? Ps(int id) {
			int i = id - _psIdBase;
			return !_disposedValue && i >= 0 && i < _particleSystems.Count ? _particleSystems[i] : null;
		}
		private static double Clamp255(double v) => v > 0 ? (v < 255 ? v : 255) : 0;

		/// <summary>Create a particle system and return its handle (-1 after Dispose).</summary>
		public int NewParticleSystem() {
			if (_disposedValue) return -1;
			_particleSystems.Add(new ParticleSystem()); return _psIdBase + _particleSystems.Count - 1;
		}
		/// <summary>Remove all live particles from a system.</summary>
		public void PsClear(int id) { Ps(id)?.Clear(); }
		/// <summary>Advance a system's simulation by dt seconds (gravity, drag, motion, ageing).</summary>
		public void PsUpdate(int id, double dt) { Ps(id)?.Update(dt); }
		/// <summary>Live particle count (e.g. to know when an effect has finished).</summary>
		public int PsCount(int id) => Ps(id)?.Count ?? 0;
		/// <summary>Max live particles a system will hold (default 4000). Raise it for heavy showcases;
		/// extra emits beyond the cap are dropped.</summary>
		public void PsSetCap(int id, int cap) { var ps = Ps(id); if (ps != null) ps.Cap = cap < 0 ? 0 : cap; }

		/// <summary>Emit one particle. Colour r,g,b are 0-255; a0 is start opacity 0-1; size0→size1
		/// is the billboard size (world units) over its life; additive!=0 glows (else alpha-blends).</summary>
		public void PsEmit(int id, double x, double y, double z, double vx, double vy, double vz,
			double r, double g, double b, double a0, double size0, double size1, double life,
			double gravity, double drag, int additive) {
			var psy = Ps(id); if (psy == null) return;
			psy.Add(new Particle {
				X = x, Y = y, Z = z, VX = vx, VY = vy, VZ = vz,
				R = Clamp255(r), G = Clamp255(g), B = Clamp255(b), A0 = a0, Size0 = size0, Size1 = size1,
				Life = life, MaxLife = life, Gravity = gravity, Drag = drag, Additive = additive != 0,
				Sprite = psy.CurSprite,
				Rot = psy.CurRot < 0 ? _prng.NextDouble() * Math.PI * 2 : psy.CurRot,
				RotVel = psy.CurRotVel == 0 ? 0 : psy.CurRotVel * (0.5 + _prng.NextDouble()) * (_prng.Next(2) == 0 ? -1 : 1),
				SizeCurve = psy.CurSizeCurve, FadeCurve = psy.CurFadeCurve,
			});
		}

		/// <summary>Emit <paramref name="count"/> particles from (x,y,z) in random directions (biased
		/// toward dir if its length &gt; 0) at up to <paramref name="speed"/>, with colour jitter and
		/// per-particle life variation. A one-call helper for impacts / puffs / muzzle flashes.</summary>
		public void PsBurst(int id, double x, double y, double z, int count,
			double dirX, double dirY, double dirZ, double spread, double speed,
			double r, double g, double b, double colorJitter, double a0, double size0, double size1,
			double life, double lifeVar, double gravity, double drag, int additive) {
			var ps = Ps(id); if (ps == null) return;
			if (count > ps.Cap - ps.Count) count = ps.Cap - ps.Count;   // the rest would be dropped by Add
			if (count <= 0) return;
			double dl = Math.Sqrt(dirX * dirX + dirY * dirY + dirZ * dirZ);
			bool hasDir = dl > 1e-6;
			if (hasDir) { dirX /= dl; dirY /= dl; dirZ /= dl; }
			for (int i = 0; i < count; i++) {
				// random unit direction, then blend toward dir by (1-spread)
				double ux, uy, uz;
				do { ux = _prng.NextDouble() * 2 - 1; uy = _prng.NextDouble() * 2 - 1; uz = _prng.NextDouble() * 2 - 1; }
				while (ux * ux + uy * uy + uz * uz > 1.0 || ux * ux + uy * uy + uz * uz < 1e-6);
				double ul = Math.Sqrt(ux * ux + uy * uy + uz * uz); ux /= ul; uy /= ul; uz /= ul;
				if (hasDir) {
					ux = dirX + (ux - dirX) * spread; uy = dirY + (uy - dirY) * spread; uz = dirZ + (uz - dirZ) * spread;
					double bl = Math.Sqrt(ux * ux + uy * uy + uz * uz); if (bl > 1e-6) { ux /= bl; uy /= bl; uz /= bl; }
				}
				double sp = speed * (0.4 + 0.6 * _prng.NextDouble());
				double jit = (_prng.NextDouble() * 2 - 1) * colorJitter;
				double lifeF = life * (1.0 - lifeVar + 2 * lifeVar * _prng.NextDouble());
				ps.Add(new Particle {
					X = x, Y = y, Z = z, VX = ux * sp, VY = uy * sp, VZ = uz * sp,
					R = Clamp255(r + jit), G = Clamp255(g + jit), B = Clamp255(b + jit), A0 = a0,
					Size0 = size0, Size1 = size1, Life = lifeF, MaxLife = lifeF,
					Gravity = gravity, Drag = drag, Additive = additive != 0,
					Sprite = ps.CurSprite,
					Rot = ps.CurRot < 0 ? _prng.NextDouble() * Math.PI * 2 : ps.CurRot,
					RotVel = ps.CurRotVel == 0 ? 0 : ps.CurRotVel * (0.5 + _prng.NextDouble()) * (_prng.Next(2) == 0 ? -1 : 1),
					SizeCurve = ps.CurSizeCurve, FadeCurve = ps.CurFadeCurve,
				});
			}
		}

		/// <summary>Stamp this sprite onto particles emitted next into the system (-1 = flat square).</summary>
		public void PsSetSprite(int id, int spriteId) { var ps = Ps(id); if (ps != null) ps.CurSprite = spriteId; }

		/// <summary>Billboard SPIN for particles emitted next: start angle (deg; &lt; 0 = random per
		/// particle) + spin rate (deg/s; bursts randomize magnitude ±50% and sign per particle).
		/// Tumbling leaves / sakura petals / debris. 0,0 = off (default).</summary>
		public void PsSetNextRotation(int id, double rotDeg, double rotVelDegPerSec) {
			var ps = Ps(id); if (ps == null) return;
			ps.CurRot = rotDeg * Math.PI / 180.0;
			ps.CurRotVel = rotVelDegPerSec * Math.PI / 180.0;
		}

		/// <summary>Size/fade CURVES for particles emitted next. sizeCurve: 0 linear, 1 ease-out,
		/// 2 pop (grow then shrink). fadeCurve: 0 linear, 1 smooth in/out, 2 flash (bright pop, long
		/// tail). The flat linear defaults match the old behaviour.</summary>
		public void PsSetNextCurves(int id, int sizeCurve, int fadeCurve) {
			var ps = Ps(id); if (ps == null) return;
			ps.CurSizeCurve = (byte)(sizeCurve < 0 ? 0 : sizeCurve > 2 ? 2 : sizeCurve);
			ps.CurFadeCurve = (byte)(fadeCurve < 0 ? 0 : fadeCurve > 2 ? 2 : fadeCurve);
		}
	}
}
