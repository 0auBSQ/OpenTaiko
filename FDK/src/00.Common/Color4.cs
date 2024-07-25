namespace FDK {
	public class Color4 {
		public float Red;
		public float Green;
		public float Blue;
		public float Alpha;

		public Color4(float r, float g, float b, float a) {
			Red = r;
			Green = g;
			Blue = b;
			Alpha = a;
		}

		public Color4(int rgba) {
			Alpha = ((rgba >> 24) & 255) / 255.0f;
			Blue = ((rgba >> 16) & 255) / 255.0f;
			Green = ((rgba >> 8) & 255) / 255.0f;
			Red = (rgba & 255) / 255.0f;
		}
	}
}
