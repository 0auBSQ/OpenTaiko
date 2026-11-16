namespace OpenTaiko {
	// What the last frame drew and uploaded, for a stage's performance readout.
	internal sealed partial class GpuRasterizer {
		// objects whose vertex buffer was rebuilt and uploaded since the last Render began, and their vertices
		private int _stUploads, _stUploadVerts;

		/// <summary>The last main pass: opaque objects drawn one by one, transparent objects, merged static buckets in view,
		/// particles, and the objects (and their vertices) re-uploaded this frame because their geometry changed.</summary>
		internal (int opaque, int transparent, int merged, int particles, int uploads, int uploadVerts) FrameStats
			=> (_opaque.Count, _trans.Count, _mergeVis.Count, _pN, _stUploads, _stUploadVerts);

		/// <summary>GL memory as "key=megabytes" pairs; textures count level 0 (mips add a third).</summary>
		internal string MemoryReport() {
			long tex = 0, spr = 0, vbo = 0, mrg = 0;
			foreach (var e in _glTex.Values) tex += (long)e.W * e.H * 4;
			foreach (var e in _glSprite.Values) spr += (long)e.W * e.H * 4;
			foreach (var c in _cache.Values) vbo += (long)c.Bytes * (c.Ring == null ? 1 : RING);
			foreach (var m in _merge.Values) mrg += m.Bytes;
			var inv = System.Globalization.CultureInfo.InvariantCulture;
			return string.Format(inv, "glTex={0:F1} glTexN={1} glSprite={2:F1} glSpriteN={3} vbo={4:F1} vboN={5} merge={6:F1} mergeN={7}",
				tex / 1048576.0, _glTex.Count, spr / 1048576.0, _glSprite.Count, vbo / 1048576.0, _cache.Count, mrg / 1048576.0, _merge.Count);
		}
	}
}
