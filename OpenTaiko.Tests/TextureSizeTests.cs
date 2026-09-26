using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using FDK;
using SkiaSharp;
using Xunit;

namespace OpenTaikoTests {
	// An async (queued) texture must report its real size before its pixels are uploaded, and a load phase must not
	// count as complete while a queued texture is still waiting for its upload. No GL here: uploads never run.
	[Collection("texture streaming")]
	public class TextureSizeTests : IDisposable {
		private readonly string _dir = Path.Combine(Path.GetTempPath(), "ot_texsize_" + Guid.NewGuid().ToString("N"));

		public TextureSizeTests() { Directory.CreateDirectory(_dir); }

		public void Dispose() {
			CTexture.AsyncLoad = false;
			try { Directory.Delete(_dir, true); } catch { }
		}

		private string Png(string name, int w, int h) {
			string path = Path.Combine(_dir, name);
			using var bmp = new SKBitmap(w, h);
			bmp.Erase(SKColors.Red);
			using var data = bmp.Encode(SKEncodedImageFormat.Png, 100);
			File.WriteAllBytes(path, data.ToArray());
			return path;
		}

		private static CTexture Queue(string path, int maxDimension = 0) {
			var tex = new CTexture();
			CTexture.AsyncLoad = true;
			try { tex.MakeTexture(path, false, maxDimension); }
			finally { CTexture.AsyncLoad = false; }
			Assert.Equal(0u, tex.Pointer);   // queued, not uploaded
			return tex;
		}

		[Fact]
		public void QueuedTextureReportsItsSizeBeforeUpload() {
			var tex = Queue(Png("a.png", 321, 123));
			Assert.Equal(321, tex.szImageSize.Width);
			Assert.Equal(123, tex.szImageSize.Height);
			Assert.Equal(321, tex.szTextureSize.Width);
			Assert.Equal(123, tex.szTextureSize.Height);
		}

		[Fact]
		public void QueuedTextureSizeFollowsMaxDimension() {
			// the same clamp the upload applies: long side 400 -> 200
			var tex = Queue(Png("b.png", 400, 101), maxDimension: 200);
			Assert.Equal(200, tex.szImageSize.Width);
			Assert.Equal((int)Math.Round(101 * 0.5), tex.szImageSize.Height);
		}

		[Fact]
		public void UnreadableFileGetsThePlaceholderSize() {
			string path = Path.Combine(_dir, "broken.png");
			File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
			var tex = Queue(path);
			Assert.Equal(10, tex.szImageSize.Width);
			Assert.Equal(10, tex.szImageSize.Height);
		}

		[Fact]
		public void LongPathStillGivesTheRealSize() {
			// over 260 characters: a path-string open fails there, the stream open used for the header does not
			string dir = _dir;
			while (dir.Length < 300) dir = Path.Combine(dir, "a_rather_long_folder_name_for_the_test");
			Directory.CreateDirectory(dir);
			string path = Path.Combine(dir, "long.png");
			using (var bmp = new SKBitmap(77, 33)) {
				bmp.Erase(SKColors.Blue);
				using var data = bmp.Encode(SKEncodedImageFormat.Png, 100);
				File.WriteAllBytes(path, data.ToArray());
			}
			var tex = Queue(path);
			Assert.Equal(77, tex.szImageSize.Width);
			Assert.Equal(33, tex.szImageSize.Height);
		}

		[Fact]
		public void PixelsAreReadableBeforeTheUpload() {
			var tex = Queue(Png("red.png", 40, 20), maxDimension: 20);
			byte[] px = tex.ReadImageRGBA(out int w, out int h);
			Assert.NotNull(px);
			Assert.Equal(20, w);
			Assert.Equal(10, h);
			Assert.Equal(w * h * 4, px.Length);
			Assert.Equal(new byte[] { 255, 0, 0, 255 }, px[..4]);   // RGBA, the PNG is plain red
		}

		[Fact]
		public void PendingUploadIsReportedAndWhenUploadedWaits() {
			var tex = Queue(Png("d.png", 8, 8));
			Assert.True(tex.UploadPending);
			bool ran = false;
			tex.WhenUploaded(() => ran = true);
			Assert.False(ran);   // it runs from the upload, which needs GL (never here)

			var done = new CTexture();
			bool now = false;
			done.WhenUploaded(() => now = true);
			Assert.True(now);    // nothing pending: at once
		}

		[Fact]
		public void LoadPhaseWaitsForTheUploadNotTheDecode() {
			string path = Png("c.png", 64, 64);
			int before = Game.AsyncActions.Count;
			CTexture.BeginStreaming();
			try {
				Queue(path);
				// wait until the decode workers have taken and decoded everything (the upload queue stops growing):
				// the old rule already called the phase complete here
				var sw = Stopwatch.StartNew();
				int last = -1, still = 0;
				while (still < 4 && sw.ElapsedMilliseconds < 5000) {
					Thread.Sleep(50);
					int now = Game.AsyncActions.Count;
					still = (now == last && now > before) ? still + 1 : 0;
					last = now;
				}
				Assert.True(Game.AsyncActions.Count > before, "the decode worker never queued the upload");
				Assert.False(CTexture.StreamComplete);   // decoded but not uploaded: the phase is not done
			} finally {
				CTexture.EndStreaming();
			}
		}
	}

	[CollectionDefinition("texture streaming", DisableParallelization = true)]
	public class TextureStreamingCollection { }
}
