using System;
using System.Globalization;
using System.Linq;
using System.Text;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// The GLSL sources embedded from src/Lua/3D/Shaders: they load, hold only ASCII (ANGLE rejects any other byte,
	// comments included), and the path tracer kernel's constants follow the layout the C# side writes.
	public class ShaderSourcesTests {
		private static readonly string[] Files = {
			"world.vert", "world.frag", "depth.vert", "depth.frag", "depth_sprite.vert", "depth_sprite.frag",
			"particle.vert", "particle.frag", "fullscreen.vert", "sky.vert", "post.frag", "outline.frag", "diorama.frag",
			"grass.vert", "grass.frag", "overlay2d.vert", "overlay2d.frag", "rt_present.vert", "rt_present.frag", "raytrace.comp",
		};

		[Fact]
		public void EveryShaderIsEmbeddedAndPureAscii() {
			var embedded = typeof(ShaderSources).Assembly.GetManifestResourceNames()
				.Where(n => n.StartsWith(ShaderSources.Prefix, StringComparison.Ordinal)).OrderBy(n => n, StringComparer.Ordinal);
			Assert.Equal(Files.Select(f => ShaderSources.Prefix + f).OrderBy(n => n, StringComparer.Ordinal), embedded);
			foreach (var f in Files) {
				string src = ShaderSources.Get(f);
				Assert.Contains("void main", src);
				Assert.False(src.Any(c => c > 127), f + " holds a non-ASCII character");
				Assert.Same(src, ShaderSources.Get(f));   // read once
			}
		}

		[Fact]
		public void KernelConstantsFollowTheTracerLayout() {
			string k = ShaderSources.Get("raytrace.comp");
			Assert.StartsWith("#version 310 es\n", k);
			Assert.Contains($"const int TRI={RtScene.TRI}, NODE={RtScene.NODE}, INST={RtScene.INST}, PRM={RtScene.PRM}, MTL={RtScene.MTL}, "
				+ $"LGT={RtScene.LGT}, RECT={GpuRaytracer.RECT}, MAXD={Raytracer.MaxDepth}, STK={RtScene.Stack};\n", k);
			Assert.Contains($"const float BOX_SLACK={Num(RtScene.BoxSlack)}, DET_EPS={Num(RtScene.DetEps)}, NOHIT=1e30;\n", k);
			Assert.Equal(1e30, RtScene.NoHit);
			// the gem's facets: the CPU tracer's table
			var f = Raytracer.GemFacets;
			Assert.Equal(33 * 4, f.Length);
			var gem = new StringBuilder("const vec4 GEM[33]=vec4[33](");
			for (int i = 0; i < f.Length; i += 4) gem.Append(i == 0 ? "" : ",").Append($"vec4({G(f[i])},{G(f[i + 1])},{G(f[i + 2])},{G(f[i + 3])})");
			gem.Append(");");
			Assert.Contains(gem.ToString(), k);
			// the variants put their #defines right after the #version line
			Assert.Equal(k, GpuRaytracer.BuildComputeSourceLean());
			Assert.Equal("#version 310 es\n#define HAS_SDF\n#define HAS_NMAP\n#define HAS_CUT\n" + k.Substring("#version 310 es\n".Length), GpuRaytracer.BuildComputeSource());
		}

		private static string Num(double v) => v.ToString("0.0##########", CultureInfo.InvariantCulture);
		// a GLSL float literal with a decimal point and no exponent, tiny magnitudes as 0.0
		private static string G(double v) => Math.Abs(v) < 1e-7 ? "0.0" : v.ToString("0.0########", CultureInfo.InvariantCulture);
	}
}
