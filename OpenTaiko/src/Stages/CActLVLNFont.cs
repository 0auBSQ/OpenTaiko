using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace OpenTaiko;

public class CActLVLNFont : CActivity {
	// コンストラクタ

	const int numWidth = 15;
	const int numHeight = 19;

	public CActLVLNFont() {
		string numChars = "0123456789?-";
		stNumber = new STNumber[12, 4];

		for (int j = 0; j < 4; j++) {
			for (int i = 0; i < 12; i++) {
				this.stNumber[i, j].ch = numChars[i];
				this.stNumber[i, j].rc = new Rectangle(
					(i % 4) * numWidth + (j % 2) * 64,
					(i / 4) * numHeight + (j / 2) * 64,
					numWidth,
					numHeight
				);
			}
		}
	}


	// メソッド
	public void tStringDraw(int x, int y, string str) {
		this.tStringDraw(x, y, str, EFontColor.White, EFontAlign.Right);
	}
	public void tStringDraw(int x, int y, string str, EFontColor efc, EFontAlign efa) {
		if (!base.IsDeActivated && !string.IsNullOrEmpty(str)) {
			if (this.txValue != null) {
				bool bRightAlign = (efa == EFontAlign.Right);

				if (bRightAlign)                            // 右詰なら文字列反転して右から描画
				{
					char[] chars = str.ToCharArray();
					Array.Reverse(chars);
					str = new string(chars);
				}

				foreach (char ch in str) {
					int p = (ch == '-' ? 11 : ch - '0');
					STNumber s = stNumber[p, (int)efc];
					int sw = s.rc.Width;
					int delta = bRightAlign ? 0 : -sw;
					this.txValue.t2DDraw(x + delta, y, s.rc);
					x += bRightAlign ? -sw : sw;
				}
			}
		}
	}


	// CActivity 実装

	public override void CreateManagedResource() {
		this.txValue = OpenTaiko.tTextureCreate(CSkin.Path(@"Graphics\ScreenSelect level numbers.png"));
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		if (this.txValue != null) {
			this.txValue.Dispose();
			this.txValue = null;
		}
		base.ReleaseManagedResource();
	}


	// その他

	#region [ private ]
	//-----------------
	[StructLayout(LayoutKind.Sequential)]
	private struct STNumber {
		public char ch;
		public Rectangle rc;
	}

	public enum EFontColor {
		Red = 0,
		Yellow = 1,
		Orange = 2,
		White = 3
	}
	public enum EFontAlign {
		Left,
		Right
	}
	private STNumber[,] stNumber;
	private CTexture txValue;
	//-----------------
	#endregion
}
