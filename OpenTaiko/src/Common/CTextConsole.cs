using System.Drawing;
using FDK;

namespace OpenTaiko {
	internal class CTextConsole : CActivity {
		// 定数

		public enum EFontType {
			White,
			Cyan,
			Gray,
			WhiteSlim,
			CyanSlim,
			GraySlim
		}

		// メソッド

		public void tPrint(int x, int y, EFontType font, string strAlphanumericString) {
			if (base.IsDeActivated || string.IsNullOrEmpty(strAlphanumericString)) {
				return;
			}

			int BOL = x;
			foreach (var ch in strAlphanumericString) {
				if (ch == '\n') {
					x = BOL;
					y += this.nFontHeight;
				} else {
					int index = str表記可能文字.IndexOf(ch);
					if (index >= 0) {
						if (this.txフォント8x16[(int)((int)font / (int)EFontType.WhiteSlim)] != null) {
							this.txフォント8x16[(int)((int)font / (int)EFontType.WhiteSlim)].t2D描画(x, y, this.rc文字の矩形領域[(int)((int)font % (int)EFontType.WhiteSlim), index]);
						}
					}

					x += this.nFontWidth;
				}
			}
		}


		// CActivity 実装

		public override void DeActivate() {
			if (this.rc文字の矩形領域 != null)
				this.rc文字の矩形領域 = null;

			base.DeActivate();
		}
		public override void CreateManagedResource() {
			if (base.IsDeActivated) {
				return;
			}

			this.txフォント8x16[0] = OpenTaiko.Tx.TxC(@"Console_Font.png");
			this.txフォント8x16[1] = OpenTaiko.Tx.TxC(@"Console_Font_Small.png");

			this.nFontWidth = this.txフォント8x16[0].szTextureSize.Width / 32;
			this.nFontHeight = this.txフォント8x16[0].szTextureSize.Height / 16;

			this.rc文字の矩形領域 = new Rectangle[3, str表記可能文字.Length];
			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < str表記可能文字.Length; j++) {
					int regionX = this.nFontWidth * 16, regionY = this.nFontHeight * 8;
					this.rc文字の矩形領域[i, j].X = ((i / 2) * regionX) + ((j % 16) * this.nFontWidth);
					this.rc文字の矩形領域[i, j].Y = ((i % 2) * regionY) + ((j / 16) * this.nFontHeight);
					this.rc文字の矩形領域[i, j].Width = this.nFontWidth;
					this.rc文字の矩形領域[i, j].Height = this.nFontHeight;
				}
			}

			base.CreateManagedResource();
		}
		public override void ReleaseManagedResource() {
			if (base.IsDeActivated) {
				return;
			}

			for (int i = 0; i < 2; i++) {
				if (this.txフォント8x16[i] != null) {
					this.txフォント8x16[i].Dispose();
					this.txフォント8x16[i] = null;
				}
			}
			base.ReleaseManagedResource();
		}


		// その他

		#region [ private ]
		//-----------------
		private Rectangle[,] rc文字の矩形領域;
		private const string str表記可能文字 = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~ ";
		public int nFontWidth = 8, nFontHeight = 16;
		private CTexture[] txフォント8x16 = new CTexture[2];
		//-----------------
		#endregion
	}
}
