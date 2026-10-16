using System;
using System.Collections.Generic;
using System.IO;

namespace OpenTaiko {
	/// <summary>
	/// The GLSL sources of the 3D renderers: the files of src/Lua/3D/Shaders, embedded in the assembly as
	/// "OpenTaiko.Shaders." followed by the file name. They hold pure ASCII: ANGLE rejects any other byte, comments
	/// included. Each source is read once and kept.
	/// </summary>
	internal static class ShaderSources {
		/// <summary>The resource name of a shader file is this prefix followed by the file name.</summary>
		public const string Prefix = "OpenTaiko.Shaders.";
		private static readonly Dictionary<string, string> s_loaded = new();

		/// <summary>The source of shader file <paramref name="name"/>, e.g. "world.vert".</summary>
		public static string Get(string name) {
			lock (s_loaded) {
				if (s_loaded.TryGetValue(name, out var src)) return src;
				using var stream = typeof(ShaderSources).Assembly.GetManifestResourceStream(Prefix + name)
					?? throw new InvalidOperationException($"The shader source {name} is not embedded in the assembly.");
				using var reader = new StreamReader(stream);
				src = reader.ReadToEnd();
				s_loaded[name] = src;
				return src;
			}
		}
	}
}
