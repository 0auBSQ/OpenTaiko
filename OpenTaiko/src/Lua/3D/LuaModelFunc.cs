using System;
using System.IO;

namespace OpenTaiko {
	/// <summary>Lua-facing model loader: <c>MODEL:Load("models/foo.glb")</c> → <see cref="IModel"/>
	/// (or nil). The concrete parser is chosen by file extension, so new formats are added here without
	/// changing any caller. Supported: <c>.glb</c>/<c>.gltf</c> (skinned), <c>.obj</c> (static).</summary>
	public class LuaModelFunc {
		private readonly string _dir;
		public LuaModelFunc(string dir) { _dir = dir; }

		public IModel? Load(string relPath) {
			try {
				string full = Path.IsPathRooted(relPath) ? relPath : Path.Combine(_dir, relPath);
				string ext = Path.GetExtension(full).ToLowerInvariant();
				switch (ext) {
					case ".glb":
					case ".gltf":
						return GltfModel.Load(full);
					case ".obj":
						return ObjModel.Load(full);
					default:
						System.Diagnostics.Trace.TraceWarning("MODEL: unsupported model extension '" + ext + "'");
						return null;
				}
			} catch (Exception e) {
				System.Diagnostics.Trace.TraceWarning("MODEL load failed: " + e.Message);
				return null;
			}
		}
	}
}
