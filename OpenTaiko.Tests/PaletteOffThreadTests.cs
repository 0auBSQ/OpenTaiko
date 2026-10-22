using System.Collections.Generic;
using System.Threading.Tasks;
using FDK;
using OpenTaiko;
using Xunit;

namespace OpenTaikoTests {
	// At boot each save's character palette is restored inside the texture loader, which runs on a worker thread
	// (async texture load). A gradient made there must not call GL: its texture is uploaded on the render thread,
	// like a queued CTexture. No GL here: the queued upload never runs.
	[Collection("texture streaming")]
	public class PaletteOffThreadTests {
		[Fact]
		public void PaletteMadeOnALoaderThreadQueuesItsUpload() {
			var stops = new List<(float Pos, float R, float G, float B, float A)> {
				(0f, 0.1f, 0.2f, 0.3f, 1f),
				(1f, 0.9f, 0.8f, 0.7f, 1f),
			};
			string key = PaletteManager.BuildCacheKey(stops, 0.5f) + "|" + System.Guid.NewGuid().ToString("N");
			int before = Game.AsyncActions.Count;

			var entry = Task.Run(() => PaletteManager.GetOrCreate(key, stops, 0.5f)).GetAwaiter().GetResult();

			Assert.NotNull(entry);
			Assert.Equal(0.5f, entry.Blend);
			Assert.Equal(256 * 4, entry.Map.Lut.Length);
			Assert.Equal(0u, entry.Map.TextureId);   // uploaded later, on the render thread
			Assert.True(Game.AsyncActions.Count > before, "the gradient never queued its upload");
			Assert.Same(entry, PaletteManager.GetOrCreate(key, stops, 0.5f));
		}
	}
}
