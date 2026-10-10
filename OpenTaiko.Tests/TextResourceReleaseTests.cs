using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using FDK;
using OpenTaiko;
using SkiaSharp;
using Xunit;

namespace OpenTaikoTests {
	// Per-play text resources are freed: a title texture keyed on a per-play font leaves the global title cache when
	// released, and a cached font renderer disposed the way the game disposes it (through IDisposable, or a base-typed
	// reference) frees the bitmaps it cached.
	public class TextResourceReleaseTests {
		// the GL delete inside CTexture.Dispose needs a context, which this process has none of; everything before it
		// (dropping the cache entries) has run by then
		private static void ReleaseWithoutGl(TitleTextureKey key) {
			try { TitleTextureKey.Release(key); } catch (NullReferenceException) { }
		}

		[Fact]
		public void Release_RemovesTheEntryAndDisposesTheTexture() {
			var font = new CCachedFontRenderer("", 20);   // no such font: the embedded fallback
			var key = new TitleTextureKey("Placeholder genre", font, Color.White, Color.Black, 1000);
			var texture = new CTexture();
			texture.CacheKeys[TitleTextureKey._titledictionary] = key;
			TitleTextureKey._titledictionary.Add(key, texture);

			ReleaseWithoutGl(key);
			Assert.False(TitleTextureKey._titledictionary.ContainsKey(key));
			Assert.Empty(texture.CacheKeys);   // only CTexture.Dispose clears it

			TitleTextureKey.Release(key);    // already gone: nothing to do
			TitleTextureKey.Release(null);
			var unused = new TitleTextureKey("Never drawn", font, Color.White, Color.Black, 1000);
			TitleTextureKey.Release(unused);  // never creates a texture
			Assert.False(TitleTextureKey._titledictionary.ContainsKey(unused));
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void DisposedFontRenderer_FreesItsCache(bool throughInterface) {
			var font = new CCachedFontRenderer("", 20);
			font.DrawText("Placeholder", Color.White, Color.Black, null, 30).Dispose();   // the renderer keeps its own copy

			var cacheField = typeof(CCachedFontRenderer).GetField("listFontCache", BindingFlags.NonPublic | BindingFlags.Instance)!;
			var cache = (IList)cacheField.GetValue(font)!;
			Assert.Single(cache);
			object entry = cache[0]!;
			var cached = (SKBitmap)entry.GetType().GetField("bmp")!.GetValue(entry)!;
			Assert.NotEqual(IntPtr.Zero, cached.Handle);

			if (throughInterface)
				((IDisposable)font).Dispose();   // tDisposeSafely and `using`
			else
				((CFontRenderer)font).Dispose();

			Assert.Null(cacheField.GetValue(font));
			Assert.Equal(IntPtr.Zero, cached.Handle);   // the cached bitmap itself was freed
		}
	}
}
