using System.Drawing;
using FDK;

namespace OpenTaiko;

internal class CTextConsole : CActivity {
	public enum EFontType {
		White,
		Cyan,
		Gray,
		WhiteSlim,
		CyanSlim,
		GraySlim
	}

	public (int x, int y) Print(int x, int y, EFontType font, string alphanumericString)
		=> this.Print(x, y, x, font, alphanumericString);

	/// Print text with initial cursor position at (x, y) and set cursor to xLineBegin on line wraps
	/// Returns (x, y) of final cursor position
	public (int x, int y) Print(int x, int y, int xLineBegin, EFontType font, string alphanumericString) {
		if (base.IsDeActivated || string.IsNullOrEmpty(alphanumericString)) {
			return (x, y);
		}

		foreach (var ch in alphanumericString) {
			if (ch == '\n') {
				x = xLineBegin;
				y += this.fontHeight;
			} else {
				int index = printableCharacters.IndexOf(ch);
				if (index >= 0) {
					if (this.fontTextures[(int)((int)font / (int)EFontType.WhiteSlim)] != null) {
						this.fontTextures[(int)((int)font / (int)EFontType.WhiteSlim)].t2D描画(x, y, this.characterRectangles[(int)((int)font % (int)EFontType.WhiteSlim), index]);
					}
				}

				x += this.fontWidth;
			}
		}
		return (x, y);
	}

	public override void DeActivate() {
		if (this.characterRectangles != null)
			this.characterRectangles = null;

		base.DeActivate();
	}

	public override void CreateManagedResource() {
		if (base.IsDeActivated) {
			return;
		}

		this.fontTextures[0] = OpenTaiko.Tx.TxC(@"Console_Font.png");
		this.fontTextures[1] = OpenTaiko.Tx.TxC(@"Console_Font_Small.png");

		this.fontWidth = this.fontTextures[0].szTextureSize.Width / 32;
		this.fontHeight = this.fontTextures[0].szTextureSize.Height / 16;

		this.characterRectangles = new Rectangle[3, printableCharacters.Length];
		for (int i = 0; i < 3; i++) {
			for (int j = 0; j < printableCharacters.Length; j++) {
				int regionX = this.fontWidth * 16, regionY = this.fontHeight * 8;
				this.characterRectangles[i, j].X = ((i / 2) * regionX) + ((j % 16) * this.fontWidth);
				this.characterRectangles[i, j].Y = ((i % 2) * regionY) + ((j / 16) * this.fontHeight);
				this.characterRectangles[i, j].Width = this.fontWidth;
				this.characterRectangles[i, j].Height = this.fontHeight;
			}
		}

		base.CreateManagedResource();
	}

	public override void ReleaseManagedResource() {
		if (base.IsDeActivated) {
			return;
		}

		for (int i = 0; i < 2; i++) {
			if (this.fontTextures[i] == null) {
				continue;
			}

			this.fontTextures[i].Dispose();
			this.fontTextures[i] = null;
		}
		base.ReleaseManagedResource();
	}

	#region [ private ]
	//-----------------
	private Rectangle[,] characterRectangles;
	private const string printableCharacters = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~ ";
	public int fontWidth = 8, fontHeight = 16;
	private CTexture[] fontTextures = new CTexture[2];
	//-----------------
	#endregion
}
