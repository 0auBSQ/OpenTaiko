namespace OpenTaiko {
	/// <summary>A loaded, poseable 3D model. Implemented per file-format (glTF/GLB, OBJ, …); a new
	/// parser just implements this and gets wired into <see cref="LuaModelFunc"/> by file extension,
	/// so callers (and Lua) never care which format a model came from.</summary>
	public interface IModel {
		/// <summary>Register the model's materials/textures into a scene (call once per scene).</summary>
		void Register(Lua3DScene scene);
		/// <summary>Pose the model at animation <paramref name="anim"/>/<paramref name="time"/> (static
		/// models ignore these) and write its triangles into scene object <paramref name="objId"/> at the
		/// given world transform (translation, Y-rotation in degrees, uniform scale).</summary>
		void Pose(Lua3DScene scene, int objId, int anim, double time, double x, double y, double z, double yawDeg, double scale);
		/// <summary>Number of animations (0 for a static model).</summary>
		int AnimCount();
		/// <summary>Name of animation <paramref name="i"/> (empty if none).</summary>
		string AnimName(int i);
		/// <summary>Duration in seconds of animation <paramref name="i"/> (0 if none).</summary>
		double Duration(int i);
	}
}
