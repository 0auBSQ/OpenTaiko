namespace OpenTaiko {
	/// <summary>
	/// A renderer for a <see cref="Lua3DScene"/>. Implementations read the scene's shared data
	/// (camera, objects, lights, primitives, textures, buffers) and write the colour buffer.
	/// The scene swaps between implementations via <c>SetMode</c>.
	/// </summary>
	internal interface IRenderer {
		/// <summary>Render the scene into its colour buffer (does not Upload).</summary>
		void Render(Lua3DScene scene);

		/// <summary>Drop any cached / accumulated state (called on mode switch or resize).</summary>
		void Invalidate();
	}
}
