using FDK;

namespace TJAPlayer3 {
	class CSongObject {
		public CSongObject(string name, float x, float y, string path) {
			this.name = path;
			this.isVisible = false;

			this.x = x;
			this.y = y;
			this.rotation = 0f;
			this.opacity = 255;
			this.xScale = 1.0f;
			this.yScale = 1.0f;
			this.color = new Color4(1f, 1f, 1f, 1f);
			this.frame = 0;

			FileAttributes attr = File.GetAttributes(path);

			if ((attr & FileAttributes.Directory) == FileAttributes.Directory) {
				textures = TJAPlayer3.Tx.TxCSongFolder(path);
			} else {
				textures = new CTexture[1];
				textures[0] = TJAPlayer3.Tx.TxCSong(path);
			}
		}

		public void tStartAnimation(double animInterval, bool loop) {
			counter.Start(0, textures.Length - 1, animInterval, TJAPlayer3.Timer);
			counter.CurrentValue = this.frame;

			this.isLooping = loop;
			this.isAnimating = true;
		}

		public void tStopAnimation() {
			counter.Stop();
			this.isAnimating = false;
		}

		public void tDraw() {
			if (isAnimating) {
				if (isLooping) counter.TickLoop();
				else {
					counter.Tick();
					if (counter.IsEnded) this.tStopAnimation();
				}

				frame = counter.CurrentValue;
			}

			CTexture tx = this.textures[frame];
			if (frame + 1 > textures.Length) return;
			if (tx == null) return;

			tx.fZ軸中心回転 = CConversion.DegreeToRadian(this.rotation);
			tx.color4 = this.color;
			tx.Opacity = this.opacity;

			float screen_ratiox = TJAPlayer3.Skin.Resolution[0] / 1280.0f;
			float screen_ratioy = TJAPlayer3.Skin.Resolution[1] / 720.0f;
			if (isVisible) tx.t2D描画SongObj((int)(this.x * screen_ratiox), (int)(this.y * screen_ratioy), this.xScale * screen_ratiox, this.yScale * screen_ratioy);
		}

		public void tDispose() {
			this.isVisible = false;
			foreach (CTexture tx in textures) {
				if (tx != null) tx.Dispose();
			}
		}

		private CCounter counter = new CCounter();
		private bool isAnimating;
		private bool isLooping;

		public CTexture[] textures;

		public string name;
		public bool isVisible;

		public float x;
		public float y;
		public float rotation;
		public int opacity;
		public float xScale;
		public float yScale;
		public Color4 color;

		public int frame;
	}
}
