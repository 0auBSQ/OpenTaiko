namespace OpenTaiko {
	// What the last frame drew and uploaded, for a stage's performance readout.
	internal sealed partial class GpuRasterizer {
		// objects whose vertex buffer was rebuilt and uploaded since the last Render began, and their vertices
		private int _stUploads, _stUploadVerts;

		/// <summary>The last main pass: opaque objects drawn one by one, transparent objects, merged static buckets in view,
		/// particles, and the objects (and their vertices) re-uploaded this frame because their geometry changed.</summary>
		internal (int opaque, int transparent, int merged, int particles, int uploads, int uploadVerts) FrameStats
			=> (_opaque.Count, _trans.Count, _mergeVis.Count, _pN, _stUploads, _stUploadVerts);
	}
}
