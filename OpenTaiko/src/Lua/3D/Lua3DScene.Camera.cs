using System;

namespace OpenTaiko {
	// The scene camera: eye, yaw/pitch basis, lens, projection and the render distance.
	public partial class Lua3DScene {
		// Camera, owned by the scene: position + orientation (yaw/pitch → basis) +
		// lens (fov → scale, near plane). Lua feeds these and reads the basis / Project back.
		internal double _camX, _camY, _camZ;
		internal double _Rx = 1, _Ry, _Rz, _Ux, _Uy = 1, _Uz, _Fx, _Fy, _Fz = 1;

		internal double _yaw, _pitch;
		internal double _fov = 70.0;
		internal double _near = 0.06;
		internal double _scale = 1.0;
		internal double _renderDist = 0.0;   // 0 = unlimited; else objects past this are culled

		// ── Scene-owned camera ────────────────────────────────────────────────────────
		// The scene holds the camera state and derives the basis + projection scale itself,
		// so Lua only feeds eye position, yaw/pitch and the lens, then reads the basis back
		// for movement / picking.

		/// <summary>Eye position in world space.</summary>
		public void SetCameraPosition(double x, double y, double z) {
			if (x != _camX || y != _camY || z != _camZ) _rtRevision++;
			_camX = x; _camY = y; _camZ = z;
		}

		/// <summary>Look direction in degrees. Yaw turns about +Y; +pitch looks up (clamped ±89.9).
		/// Rebuilds the right/up/forward basis used by the rasterizer and Project.</summary>
		public void SetCameraAngles(double yawDeg, double pitchDeg) {
			if (pitchDeg > 89.9) pitchDeg = 89.9; else if (pitchDeg < -89.9) pitchDeg = -89.9;
			_yaw = yawDeg; _pitch = pitchDeg;
			double y = yawDeg * Math.PI / 180.0, p = pitchDeg * Math.PI / 180.0;
			double cy = Math.Cos(y), sy = Math.Sin(y), cp = Math.Cos(p), sp = Math.Sin(p);
			double fx = sy * cp, fz = cy * cp, ux = -sy * sp, uz = -cy * sp;
			// the basis is always rebuilt (RenderView may have flipped it); the tracers restart only when it moves
			if (fx != _Fx || sp != _Fy || fz != _Fz || cy != _Rx || _Ry != 0 || -sy != _Rz || ux != _Ux || cp != _Uy || uz != _Uz) _rtRevision++;
			_Fx = fx;  _Fy = sp;  _Fz = fz;   // forward
			_Rx = cy;  _Ry = 0;   _Rz = -sy;  // right
			_Ux = ux;  _Uy = cp;  _Uz = uz;   // up
		}

		/// <summary>Vertical field of view in degrees; updates the projection scale.</summary>
		public void SetCameraFov(double fovDeg) {
			_fov = fovDeg < 1 ? 1 : (fovDeg > 179 ? 179 : fovDeg);
			double scale = (_h * 0.5) / Math.Tan(_fov * 0.5 * Math.PI / 180.0);
			if (scale != _scale) _rtRevision++;
			_scale = scale;
		}

		/// <summary>Near clip plane (camera-space z, at least 0.01: a nearer plane leaves the 24-bit depth buffer
		/// almost no precision a few units away).</summary>
		public void SetCameraNear(double near) { _near = near < 0.01 ? 0.01 : near; }

		// The camera and pixel size a reflection pass changes (RenderView, RenderViewReflect), saved before the pass
		// and put back after it.
		private struct CameraState {
			public int W, H;
			public double X, Y, Z, Yaw, Pitch, Fov, Scale;
			public double Rx, Ry, Rz, Ux, Uy, Uz, Fx, Fy, Fz;
		}
		private CameraState SaveCamera() => new() {
			W = _w, H = _h, X = _camX, Y = _camY, Z = _camZ, Yaw = _yaw, Pitch = _pitch, Fov = _fov, Scale = _scale,
			Rx = _Rx, Ry = _Ry, Rz = _Rz, Ux = _Ux, Uy = _Uy, Uz = _Uz, Fx = _Fx, Fy = _Fy, Fz = _Fz,
		};
		private void RestoreCamera(in CameraState c) {
			_w = c.W; _h = c.H; _camX = c.X; _camY = c.Y; _camZ = c.Z; _yaw = c.Yaw; _pitch = c.Pitch; _fov = c.Fov; _scale = c.Scale;
			_Rx = c.Rx; _Ry = c.Ry; _Rz = c.Rz; _Ux = c.Ux; _Uy = c.Uy; _Uz = c.Uz; _Fx = c.Fx; _Fy = c.Fy; _Fz = c.Fz;
		}

		/// <summary>Project a world point to scene-buffer pixel coords. Returns (x, y, depth); depth is
		/// camera-space z — &lt;= 0 means behind the camera / off-screen (ignore x,y then). Lets Lua place
		/// 2D overlays (HP bars, markers, a sun sprite) over the 3D view.</summary>
		public (double, double, double) WorldToScreen(double wx, double wy, double wz) {
			double rx = wx - _camX, ry = wy - _camY, rz = wz - _camZ;
			double cx = rx * _Rx + ry * _Ry + rz * _Rz;
			double cy = rx * _Ux + ry * _Uy + rz * _Uz;
			double cz = rx * _Fx + ry * _Fy + rz * _Fz;
			if (cz <= _near) return (0, 0, -1);
			double iz = 1.0 / cz;
			double inv = (_w != 0) ? (double)_logicalW / _w : 1.0;   // pixel → logical screen coords (render-scale)
			return ((_w * 0.5 + cx * iz * _scale) * inv, (_h * 0.5 - cy * iz * _scale) * inv, cz);
		}

		/// <summary>Cull objects whose bounds lie entirely beyond this distance from the camera
		/// (0 = unlimited). Lowers the drawn geometry for big worlds.</summary>
		public void SetRenderDistance(double d) { _renderDist = d < 0 ? 0 : d; }

		public double GetCameraFov() => _fov;
		public double GetCameraNear() => _near;
		public double GetCameraYaw() => _yaw;
		public double GetCameraPitch() => _pitch;
		public (double, double, double) GetCameraPosition() => (_camX, _camY, _camZ);
		public (double, double, double) GetCameraForward() => (_Fx, _Fy, _Fz);
		public (double, double, double) GetCameraRight() => (_Rx, _Ry, _Rz);
		public (double, double, double) GetCameraUp() => (_Ux, _Uy, _Uz);

		/// <summary>Project a world point to screen pixels using the scene camera:
		/// returns (sx, sy, inFront). inFront is false when the point is behind the near plane.</summary>
		public (double, double, bool) Project(double wx, double wy, double wz) {
			double rx = wx - _camX, ry = wy - _camY, rz = wz - _camZ;
			double cz = rx * _Fx + ry * _Fy + rz * _Fz;
			if (cz < _near) return (0, 0, false);
			double cx = rx * _Rx + ry * _Ry + rz * _Rz;
			double cyv = rx * _Ux + ry * _Uy + rz * _Uz;
			double inv = (_w != 0) ? (double)_logicalW / _w : 1.0;   // pixel → logical screen coords (render-scale)
			return ((_w * 0.5 + cx / cz * _scale) * inv, (_h * 0.5 - cyv / cz * _scale) * inv, true);
		}
	}
}
