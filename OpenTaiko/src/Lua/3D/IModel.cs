using System;

namespace OpenTaiko {
	/// <summary>A loaded, poseable 3D model. Implemented per file-format (glTF/GLB, OBJ, …); a new
	/// parser just implements this and gets wired into <see cref="LuaModelFunc"/> by file extension,
	/// so callers (and Lua) never care which format a model came from.</summary>
	public interface IModel {
		/// <summary>Register the model's materials/textures into a scene (call once per scene).</summary>
		void Register(Lua3DScene scene);
		/// <summary>Pose the model at animation <paramref name="anim"/>/<paramref name="time"/> (static
		/// models ignore these): its triangles go into scene object <paramref name="objId"/> in model space,
		/// rebuilt only when the pose changed since that object's last Pose, and the object's transform places
		/// them (translation, Y-rotation in degrees, uniform scale).</summary>
		void Pose(Lua3DScene scene, int objId, int anim, double time, double x, double y, double z, double yawDeg, double scale);
		/// <summary>Number of animations (0 for a static model).</summary>
		int AnimCount();
		/// <summary>Name of animation <paramref name="i"/> (empty if none).</summary>
		string AnimName(int i);
		/// <summary>Duration in seconds of animation <paramref name="i"/> (0 if none).</summary>
		double Duration(int i);
	}

	/// <summary>The placement every model's Pose shares. The object's transform is T(x,y,z)·Ry(yaw)·S(|scale|),
	/// with Ry turning +X toward −Z (x' = c·x + s·z, z' = −s·x + c·z); a negative scale mirrors the posed
	/// triangles themselves, so the transform never mirrors. Each triangle carries a flat shade lit from a
	/// fixed world direction.</summary>
	internal static class ModelPlacement {
		/// <summary>The row-major object transform for a model placed at (x, y, z), turned by
		/// <paramref name="yawDeg"/> and scaled by |<paramref name="scale"/>|.</summary>
		public static void Matrix(double x, double y, double z, double yawDeg, double scale, Span<double> m) {
			double yr = yawDeg * Math.PI / 180.0, c = Math.Cos(yr), s = Math.Sin(yr), a = Math.Abs(scale);
			m[0] = c * a;  m[1] = 0; m[2] = s * a;  m[3] = x;
			m[4] = 0;      m[5] = a; m[6] = 0;      m[7] = y;
			m[8] = -s * a; m[9] = 0; m[10] = c * a; m[11] = z;
			m[12] = 0; m[13] = 0; m[14] = 0; m[15] = 1;
		}

		/// <summary>The shade light, fixed in the world at (0.35, 0.82, 0.45), seen from the frame of a model
		/// turned by <paramref name="yawDeg"/>.</summary>
		public static void Light(double yawDeg, out double lx, out double ly, out double lz) {
			double yr = yawDeg * Math.PI / 180.0, c = Math.Cos(yr), s = Math.Sin(yr);
			lx = c * 0.35 - s * 0.45; ly = 0.82; lz = s * 0.35 + c * 0.45;
		}

		/// <summary>Flat shade of one triangle: 0.5 + 0.5·|n·l| for its unit face normal n.</summary>
		public static double Shade(double x0, double y0, double z0, double x1, double y1, double z1,
			double x2, double y2, double z2, double lx, double ly, double lz) {
			double ex1 = x1 - x0, ey1 = y1 - y0, ez1 = z1 - z0, ex2 = x2 - x0, ey2 = y2 - y0, ez2 = z2 - z0;
			double nx = ey1 * ez2 - ez1 * ey2, ny = ez1 * ex2 - ex1 * ez2, nz = ex1 * ey2 - ey1 * ex2;
			double nl = Math.Sqrt(nx * nx + ny * ny + nz * nz); if (nl > 1e-9) { nx /= nl; ny /= nl; nz /= nl; }
			double nd = nx * lx + ny * ly + nz * lz; if (nd < 0) nd = -nd;
			return 0.5 + 0.5 * nd;
		}
	}
}
