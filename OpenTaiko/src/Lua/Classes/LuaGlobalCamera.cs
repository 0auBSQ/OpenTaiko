using System;

namespace OpenTaiko {
	/// <summary>
	/// The whole-screen 2D camera — the same global presentation transform the TJA <c>#CAMERA</c>
	/// commands drive (<see cref="OpenTaiko.fCamXOffset"/>/<c>fCamYOffset</c>/<c>fCamRotation</c>/
	/// <c>fCamXScale</c>/<c>fCamYScale</c>). It pans/zooms/rotates the entire rendered frame (2D HUD
	/// AND any blitted 3D scene), and adds a decaying **screen shake** for dialogue/impacts.
	///
	/// Exposed to Lua as the <c>GLOBALCAMERA</c> singleton. A stage drives it each frame and clears it
	/// on the way out:
	/// <code>
	///   function update(ts) ... GLOBALCAMERA:Update(dt) end
	///   -- on an impact:      GLOBALCAMERA:Shake(18, 0.35)        -- 18px for 0.35s
	///   function deactivate() GLOBALCAMERA:Reset() end             -- don't leak into the next stage
	/// </code>
	/// Note: the 3D scene's own camera (iso framing) is set directly in Lua via
	/// <c>scene:SetCameraPosition/Angles/Fov</c>; this class is only the global screen camera.
	/// </summary>
	public class LuaGlobalCamera {
		private double _baseX, _baseY, _rot;          // base pan (px in 1280×720 space) + rotation (deg)
		private double _zx = 1, _zy = 1;              // zoom
		private double _shakeAmp, _shakeRot, _shakeT, _shakeDur;
		private readonly Random _rng = new();

		// ── base transform (persist until changed) ──────────────────────────────────────
		/// <summary>Pan the whole screen by (x,y) pixels (1280×720 reference space).</summary>
		public void SetOffset(double x, double y) { _baseX = x; _baseY = y; }
		/// <summary>Zoom the whole screen (1 = none). Separate X/Y allowed.</summary>
		public void SetZoom(double sx, double sy) { _zx = sx; _zy = sy; }
		public void SetUniformZoom(double s) { _zx = s; _zy = s; }
		/// <summary>Rotate the whole screen about its centre, in degrees.</summary>
		public void SetRotation(double deg) { _rot = deg; }
		public double GetOffsetX() => _baseX; public double GetOffsetY() => _baseY;
		public double GetZoomX() => _zx; public double GetZoomY() => _zy; public double GetRotation() => _rot;

		// ── shake ───────────────────────────────────────────────────────────────────────
		/// <summary>Start a decaying screen shake of `amplitudePx` for `seconds`.</summary>
		public void Shake(double amplitudePx, double seconds) => Shake(amplitudePx, seconds, 0);
		/// <summary>Screen shake with an optional rotational wobble (degrees).</summary>
		public void Shake(double amplitudePx, double seconds, double rotAmpDeg) {
			if (seconds <= 0) return;
			if (amplitudePx >= _shakeAmp || _shakeT <= 0) {   // a stronger hit overrides a fading one
				_shakeAmp = amplitudePx; _shakeRot = rotAmpDeg; _shakeDur = seconds; _shakeT = seconds;
			}
		}
		public bool IsShaking => _shakeT > 0;

		// ── per-frame ─────────────────────────────────────────────────────────────────
		/// <summary>Advance shake decay and push base+shake onto the global screen-camera fields.</summary>
		public void Update(double dt) {
			if (dt < 0) dt = 0; else if (dt > 0.25) dt = 0.25;
			double jx = 0, jy = 0, jr = 0;
			if (_shakeT > 0) {
				_shakeT -= dt;
				double k = _shakeT > 0 ? _shakeT / _shakeDur : 0;   // linear decay
				double cur = _shakeAmp * k;
				jx = (_rng.NextDouble() * 2 - 1) * cur;
				jy = (_rng.NextDouble() * 2 - 1) * cur;
				jr = (_rng.NextDouble() * 2 - 1) * _shakeRot * k;
			} else { _shakeAmp = 0; _shakeRot = 0; }
			OpenTaiko.fCamXOffset = (float)(_baseX + jx);
			OpenTaiko.fCamYOffset = (float)(_baseY + jy);
			OpenTaiko.fCamRotation = (float)(_rot + jr);
			OpenTaiko.fCamXScale = (float)_zx;
			OpenTaiko.fCamYScale = (float)_zy;
		}

		/// <summary>Recentre everything and stop any shake (call on stage exit).</summary>
		public void Reset() {
			_baseX = _baseY = _rot = 0; _zx = _zy = 1; _shakeT = 0; _shakeAmp = 0; _shakeRot = 0;
			OpenTaiko.fCamXOffset = 0; OpenTaiko.fCamYOffset = 0; OpenTaiko.fCamRotation = 0;
			OpenTaiko.fCamXScale = 1f; OpenTaiko.fCamYScale = 1f;
		}
	}
}
