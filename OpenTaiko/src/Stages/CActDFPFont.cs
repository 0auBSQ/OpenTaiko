using System.Runtime.InteropServices;
using FDK;
using Silk.NET.Maths;
using Rectangle = System.Drawing.Rectangle;

namespace OpenTaiko;

internal class CActDFPFont : CActivity {
	// コンストラクタ

	public CActDFPFont() {
	}


	// メソッド

	public int nStringLengthdot(string str) {
		return this.nStringLengthdot(str, 1f);
	}
	public int nStringLengthdot(string str, float fScale) {
		if (string.IsNullOrEmpty(str)) {
			return 0;
		}
		int num = 0;
		foreach (char ch in str) {
			foreach (STTextRegion stTextRegion in this.stTextRegion) {
				if (stTextRegion.ch == ch) {
					num += (int)((stTextRegion.rc.Width - 5) * fScale);
					break;
				}
			}
		}
		return num;
	}
	public void tStringDraw(int x, int y, string str) {
		this.tStringDraw(x, y, str, false, 1f);
	}
	public void tStringDraw(int x, int y, string str, bool bEmphasis) {
		this.tStringDraw(x, y, str, bEmphasis, 1f);
	}
	public void tStringDraw(int x, int y, string str, bool bEmphasis, float fScale) {
		if (!base.IsDeActivated && !string.IsNullOrEmpty(str)) {
			CTexture texture = bEmphasis ? OpenTaiko.Tx.Config_Font_Bold : OpenTaiko.Tx.Config_Font;
			if (texture != null) {
				texture.vcScaleRatio = new Vector3D<float>(fScale, fScale, 1f);
				foreach (char ch in str) {
					foreach (STTextRegion stTextRegion in this.stTextRegion) {
						if (stTextRegion.ch == ch) {
							texture.t2DDraw(x, y, stTextRegion.rc);
							x += (int)((stTextRegion.rc.Width - 5) * fScale);
							break;
						}
					}
				}
			}
		}
	}


	// CActivity 実装

	public override void CreateManagedResource() {
		//this.tx通常文字 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\Screen font dfp.png" ), false );
		//this.tx強調文字 = CDTXMania.tテクスチャの生成( CSkin.Path( @"Graphics\Screen font dfp em.png" ), false );


		STTextRegion[] stTextRegionArray = new STTextRegion[0x5d + 2];
		STTextRegion stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion = stTextRegion94;
		stTextRegion.ch = ' ';
		stTextRegion.rc = new Rectangle(10, 3, 13, 0x1b);
		stTextRegionArray[0] = stTextRegion;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion2 = stTextRegion94;
		stTextRegion2.ch = '!';
		stTextRegion2.rc = new Rectangle(0x19, 3, 14, 0x1b);
		stTextRegionArray[1] = stTextRegion2;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion3 = stTextRegion94;
		stTextRegion3.ch = '"';
		stTextRegion3.rc = new Rectangle(0x2c, 3, 0x11, 0x1b);
		stTextRegionArray[2] = stTextRegion3;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion4 = stTextRegion94;
		stTextRegion4.ch = '#';
		stTextRegion4.rc = new Rectangle(0x40, 3, 0x18, 0x1b);
		stTextRegionArray[3] = stTextRegion4;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion5 = stTextRegion94;
		stTextRegion5.ch = '$';
		stTextRegion5.rc = new Rectangle(90, 3, 0x15, 0x1b);
		stTextRegionArray[4] = stTextRegion5;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion6 = stTextRegion94;
		stTextRegion6.ch = '%';
		stTextRegion6.rc = new Rectangle(0x71, 3, 0x1b, 0x1b);
		stTextRegionArray[5] = stTextRegion6;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion7 = stTextRegion94;
		stTextRegion7.ch = '&';
		stTextRegion7.rc = new Rectangle(0x8e, 3, 0x18, 0x1b);
		stTextRegionArray[6] = stTextRegion7;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion8 = stTextRegion94;
		stTextRegion8.ch = '\'';
		stTextRegion8.rc = new Rectangle(0xab, 3, 11, 0x1b);
		stTextRegionArray[7] = stTextRegion8;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion9 = stTextRegion94;
		stTextRegion9.ch = '(';
		stTextRegion9.rc = new Rectangle(0xc0, 3, 0x10, 0x1b);
		stTextRegionArray[8] = stTextRegion9;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion10 = stTextRegion94;
		stTextRegion10.ch = ')';
		stTextRegion10.rc = new Rectangle(0xd0, 3, 0x10, 0x1b);
		stTextRegionArray[9] = stTextRegion10;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion11 = stTextRegion94;
		stTextRegion11.ch = '*';
		stTextRegion11.rc = new Rectangle(0xe2, 3, 0x15, 0x1b);
		stTextRegionArray[10] = stTextRegion11;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion12 = stTextRegion94;
		stTextRegion12.ch = '+';
		stTextRegion12.rc = new Rectangle(2, 0x1f, 0x18, 0x1b);
		stTextRegionArray[11] = stTextRegion12;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion13 = stTextRegion94;
		stTextRegion13.ch = ',';
		stTextRegion13.rc = new Rectangle(0x1b, 0x1f, 11, 0x1b);
		stTextRegionArray[12] = stTextRegion13;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion14 = stTextRegion94;
		stTextRegion14.ch = '-';
		stTextRegion14.rc = new Rectangle(0x29, 0x1f, 13, 0x1b);
		stTextRegionArray[13] = stTextRegion14;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion15 = stTextRegion94;
		stTextRegion15.ch = '.';
		stTextRegion15.rc = new Rectangle(0x37, 0x1f, 11, 0x1b);
		stTextRegionArray[14] = stTextRegion15;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion16 = stTextRegion94;
		stTextRegion16.ch = '/';
		stTextRegion16.rc = new Rectangle(0x44, 0x1f, 0x15, 0x1b);
		stTextRegionArray[15] = stTextRegion16;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion17 = stTextRegion94;
		stTextRegion17.ch = '0';
		stTextRegion17.rc = new Rectangle(0x5b, 0x1f, 20, 0x1b);
		stTextRegionArray[0x10] = stTextRegion17;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion18 = stTextRegion94;
		stTextRegion18.ch = '1';
		stTextRegion18.rc = new Rectangle(0x75, 0x1f, 14, 0x1b);
		stTextRegionArray[0x11] = stTextRegion18;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion19 = stTextRegion94;
		stTextRegion19.ch = '2';
		stTextRegion19.rc = new Rectangle(0x86, 0x1f, 0x15, 0x1b);
		stTextRegionArray[0x12] = stTextRegion19;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion20 = stTextRegion94;
		stTextRegion20.ch = '3';
		stTextRegion20.rc = new Rectangle(0x9d, 0x1f, 20, 0x1b);
		stTextRegionArray[0x13] = stTextRegion20;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion21 = stTextRegion94;
		stTextRegion21.ch = '4';
		stTextRegion21.rc = new Rectangle(0xb3, 0x1f, 20, 0x1b);
		stTextRegionArray[20] = stTextRegion21;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion22 = stTextRegion94;
		stTextRegion22.ch = '5';
		stTextRegion22.rc = new Rectangle(0xca, 0x1f, 0x13, 0x1b);
		stTextRegionArray[0x15] = stTextRegion22;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion23 = stTextRegion94;
		stTextRegion23.ch = '6';
		stTextRegion23.rc = new Rectangle(0xe0, 0x1f, 20, 0x1b);
		stTextRegionArray[0x16] = stTextRegion23;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion24 = stTextRegion94;
		stTextRegion24.ch = '7';
		stTextRegion24.rc = new Rectangle(4, 0x3b, 0x13, 0x1b);
		stTextRegionArray[0x17] = stTextRegion24;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion25 = stTextRegion94;
		stTextRegion25.ch = '8';
		stTextRegion25.rc = new Rectangle(0x18, 0x3b, 20, 0x1b);
		stTextRegionArray[0x18] = stTextRegion25;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion26 = stTextRegion94;
		stTextRegion26.ch = '9';
		stTextRegion26.rc = new Rectangle(0x2f, 0x3b, 0x13, 0x1b);
		stTextRegionArray[0x19] = stTextRegion26;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion27 = stTextRegion94;
		stTextRegion27.ch = ':';
		stTextRegion27.rc = new Rectangle(0x44, 0x3b, 12, 0x1b);
		stTextRegionArray[0x1a] = stTextRegion27;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion28 = stTextRegion94;
		stTextRegion28.ch = ';';
		stTextRegion28.rc = new Rectangle(0x51, 0x3b, 13, 0x1b);
		stTextRegionArray[0x1b] = stTextRegion28;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion29 = stTextRegion94;
		stTextRegion29.ch = '<';
		stTextRegion29.rc = new Rectangle(0x60, 0x3b, 20, 0x1b);
		stTextRegionArray[0x1c] = stTextRegion29;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion30 = stTextRegion94;
		stTextRegion30.ch = '=';
		stTextRegion30.rc = new Rectangle(0x74, 0x3b, 0x11, 0x1b);
		stTextRegionArray[0x1d] = stTextRegion30;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion31 = stTextRegion94;
		stTextRegion31.ch = '>';
		stTextRegion31.rc = new Rectangle(0x85, 0x3b, 20, 0x1b);
		stTextRegionArray[30] = stTextRegion31;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion32 = stTextRegion94;
		stTextRegion32.ch = '?';
		stTextRegion32.rc = new Rectangle(0x9c, 0x3b, 20, 0x1b);
		stTextRegionArray[0x1f] = stTextRegion32;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion33 = stTextRegion94;
		stTextRegion33.ch = 'A';
		stTextRegion33.rc = new Rectangle(0xb1, 0x3b, 0x17, 0x1b);
		stTextRegionArray[0x20] = stTextRegion33;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion34 = stTextRegion94;
		stTextRegion34.ch = 'B';
		stTextRegion34.rc = new Rectangle(0xcb, 0x3b, 0x15, 0x1b);
		stTextRegionArray[0x21] = stTextRegion34;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion35 = stTextRegion94;
		stTextRegion35.ch = 'C';
		stTextRegion35.rc = new Rectangle(0xe3, 0x3b, 0x16, 0x1b);
		stTextRegionArray[0x22] = stTextRegion35;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion36 = stTextRegion94;
		stTextRegion36.ch = 'D';
		stTextRegion36.rc = new Rectangle(2, 0x57, 0x16, 0x1b);
		stTextRegionArray[0x23] = stTextRegion36;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion37 = stTextRegion94;
		stTextRegion37.ch = 'E';
		stTextRegion37.rc = new Rectangle(0x1a, 0x57, 0x16, 0x1b);
		stTextRegionArray[0x24] = stTextRegion37;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion38 = stTextRegion94;
		stTextRegion38.ch = 'F';
		stTextRegion38.rc = new Rectangle(0x30, 0x57, 0x16, 0x1b);
		stTextRegionArray[0x25] = stTextRegion38;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion39 = stTextRegion94;
		stTextRegion39.ch = 'G';
		stTextRegion39.rc = new Rectangle(0x48, 0x57, 0x16, 0x1b);
		stTextRegionArray[0x26] = stTextRegion39;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion40 = stTextRegion94;
		stTextRegion40.ch = 'H';
		stTextRegion40.rc = new Rectangle(0x61, 0x57, 0x18, 0x1b);
		stTextRegionArray[0x27] = stTextRegion40;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion41 = stTextRegion94;
		stTextRegion41.ch = 'I';
		stTextRegion41.rc = new Rectangle(0x7a, 0x57, 13, 0x1b);
		stTextRegionArray[40] = stTextRegion41;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion42 = stTextRegion94;
		stTextRegion42.ch = 'J';
		stTextRegion42.rc = new Rectangle(0x88, 0x57, 20, 0x1b);
		stTextRegionArray[0x29] = stTextRegion42;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion43 = stTextRegion94;
		stTextRegion43.ch = 'K';
		stTextRegion43.rc = new Rectangle(0x9d, 0x57, 0x18, 0x1b);
		stTextRegionArray[0x2a] = stTextRegion43;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion44 = stTextRegion94;
		stTextRegion44.ch = 'L';
		stTextRegion44.rc = new Rectangle(0xb7, 0x57, 20, 0x1b);
		stTextRegionArray[0x2b] = stTextRegion44;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion45 = stTextRegion94;
		stTextRegion45.ch = 'M';
		stTextRegion45.rc = new Rectangle(0xce, 0x57, 0x1a, 0x1b);
		stTextRegionArray[0x2c] = stTextRegion45;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion46 = stTextRegion94;
		stTextRegion46.ch = 'N';
		stTextRegion46.rc = new Rectangle(0xe9, 0x57, 0x17, 0x1b);
		stTextRegionArray[0x2d] = stTextRegion46;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion47 = stTextRegion94;
		stTextRegion47.ch = 'O';
		stTextRegion47.rc = new Rectangle(2, 0x73, 0x18, 0x1b);
		stTextRegionArray[0x2e] = stTextRegion47;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion48 = stTextRegion94;
		stTextRegion48.ch = 'P';
		stTextRegion48.rc = new Rectangle(0x1c, 0x73, 0x15, 0x1b);
		stTextRegionArray[0x2f] = stTextRegion48;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion49 = stTextRegion94;
		stTextRegion49.ch = 'Q';
		stTextRegion49.rc = new Rectangle(0x33, 0x73, 0x17, 0x1b);
		stTextRegionArray[0x30] = stTextRegion49;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion50 = stTextRegion94;
		stTextRegion50.ch = 'R';
		stTextRegion50.rc = new Rectangle(0x4c, 0x73, 0x16, 0x1b);
		stTextRegionArray[0x31] = stTextRegion50;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion51 = stTextRegion94;
		stTextRegion51.ch = 'S';
		stTextRegion51.rc = new Rectangle(100, 0x73, 0x15, 0x1b);
		stTextRegionArray[50] = stTextRegion51;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion52 = stTextRegion94;
		stTextRegion52.ch = 'T';
		stTextRegion52.rc = new Rectangle(0x7c, 0x73, 0x16, 0x1b);
		stTextRegionArray[0x33] = stTextRegion52;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion53 = stTextRegion94;
		stTextRegion53.ch = 'U';
		stTextRegion53.rc = new Rectangle(0x93, 0x73, 0x16, 0x1b);
		stTextRegionArray[0x34] = stTextRegion53;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion54 = stTextRegion94;
		stTextRegion54.ch = 'V';
		stTextRegion54.rc = new Rectangle(0xad, 0x73, 0x16, 0x1b);
		stTextRegionArray[0x35] = stTextRegion54;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion55 = stTextRegion94;
		stTextRegion55.ch = 'W';
		stTextRegion55.rc = new Rectangle(0xc5, 0x73, 0x1a, 0x1b);
		stTextRegionArray[0x36] = stTextRegion55;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion56 = stTextRegion94;
		stTextRegion56.ch = 'X';
		stTextRegion56.rc = new Rectangle(0xe0, 0x73, 0x1a, 0x1b);
		stTextRegionArray[0x37] = stTextRegion56;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion57 = stTextRegion94;
		stTextRegion57.ch = 'Y';
		stTextRegion57.rc = new Rectangle(4, 0x8f, 0x17, 0x1b);
		stTextRegionArray[0x38] = stTextRegion57;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion58 = stTextRegion94;
		stTextRegion58.ch = 'Z';
		stTextRegion58.rc = new Rectangle(0x1b, 0x8f, 0x16, 0x1b);
		stTextRegionArray[0x39] = stTextRegion58;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion59 = stTextRegion94;
		stTextRegion59.ch = '[';
		stTextRegion59.rc = new Rectangle(0x31, 0x8f, 0x11, 0x1b);
		stTextRegionArray[0x3a] = stTextRegion59;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion60 = stTextRegion94;
		stTextRegion60.ch = '\\';
		stTextRegion60.rc = new Rectangle(0x42, 0x8f, 0x19, 0x1b);
		stTextRegionArray[0x3b] = stTextRegion60;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion61 = stTextRegion94;
		stTextRegion61.ch = ']';
		stTextRegion61.rc = new Rectangle(0x5c, 0x8f, 0x11, 0x1b);
		stTextRegionArray[60] = stTextRegion61;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion62 = stTextRegion94;
		stTextRegion62.ch = '^';
		stTextRegion62.rc = new Rectangle(0x71, 0x8f, 0x10, 0x1b);
		stTextRegionArray[0x3d] = stTextRegion62;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion63 = stTextRegion94;
		stTextRegion63.ch = '_';
		stTextRegion63.rc = new Rectangle(0x81, 0x8f, 0x13, 0x1b);
		stTextRegionArray[0x3e] = stTextRegion63;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion64 = stTextRegion94;
		stTextRegion64.ch = 'a';
		stTextRegion64.rc = new Rectangle(150, 0x8f, 0x13, 0x1b);
		stTextRegionArray[0x3f] = stTextRegion64;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion65 = stTextRegion94;
		stTextRegion65.ch = 'b';
		stTextRegion65.rc = new Rectangle(0xac, 0x8f, 20, 0x1b);
		stTextRegionArray[0x40] = stTextRegion65;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion66 = stTextRegion94;
		stTextRegion66.ch = 'c';
		stTextRegion66.rc = new Rectangle(0xc3, 0x8f, 0x12, 0x1b);
		stTextRegionArray[0x41] = stTextRegion66;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion67 = stTextRegion94;
		stTextRegion67.ch = 'd';
		stTextRegion67.rc = new Rectangle(0xd8, 0x8f, 0x15, 0x1b);
		stTextRegionArray[0x42] = stTextRegion67;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion68 = stTextRegion94;
		stTextRegion68.ch = 'e';
		stTextRegion68.rc = new Rectangle(2, 0xab, 0x13, 0x1b);
		stTextRegionArray[0x43] = stTextRegion68;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion69 = stTextRegion94;
		stTextRegion69.ch = 'f';
		stTextRegion69.rc = new Rectangle(0x17, 0xab, 0x11, 0x1b);
		stTextRegionArray[0x44] = stTextRegion69;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion70 = stTextRegion94;
		stTextRegion70.ch = 'g';
		stTextRegion70.rc = new Rectangle(40, 0xab, 0x15, 0x1b);
		stTextRegionArray[0x45] = stTextRegion70;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion71 = stTextRegion94;
		stTextRegion71.ch = 'h';
		stTextRegion71.rc = new Rectangle(0x3f, 0xab, 20, 0x1b);
		stTextRegionArray[70] = stTextRegion71;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion72 = stTextRegion94;
		stTextRegion72.ch = 'i';
		stTextRegion72.rc = new Rectangle(0x55, 0xab, 13, 0x1b);
		stTextRegionArray[0x47] = stTextRegion72;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion73 = stTextRegion94;
		stTextRegion73.ch = 'j';
		stTextRegion73.rc = new Rectangle(0x62, 0xab, 0x10, 0x1b);
		stTextRegionArray[0x48] = stTextRegion73;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion74 = stTextRegion94;
		stTextRegion74.ch = 'k';
		stTextRegion74.rc = new Rectangle(0x74, 0xab, 20, 0x1b);
		stTextRegionArray[0x49] = stTextRegion74;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion75 = stTextRegion94;
		stTextRegion75.ch = 'l';
		stTextRegion75.rc = new Rectangle(0x8a, 0xab, 13, 0x1b);
		stTextRegionArray[0x4a] = stTextRegion75;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion76 = stTextRegion94;
		stTextRegion76.ch = 'm';
		stTextRegion76.rc = new Rectangle(0x98, 0xab, 0x1a, 0x1b);
		stTextRegionArray[0x4b] = stTextRegion76;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion77 = stTextRegion94;
		stTextRegion77.ch = 'n';
		stTextRegion77.rc = new Rectangle(0xb5, 0xab, 20, 0x1b);
		stTextRegionArray[0x4c] = stTextRegion77;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion78 = stTextRegion94;
		stTextRegion78.ch = 'o';
		stTextRegion78.rc = new Rectangle(0xcc, 0xab, 0x13, 0x1b);
		stTextRegionArray[0x4d] = stTextRegion78;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion79 = stTextRegion94;
		stTextRegion79.ch = 'p';
		stTextRegion79.rc = new Rectangle(0xe1, 0xab, 0x15, 0x1b);
		stTextRegionArray[0x4e] = stTextRegion79;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion80 = stTextRegion94;
		stTextRegion80.ch = 'q';
		stTextRegion80.rc = new Rectangle(2, 0xc7, 20, 0x1b);
		stTextRegionArray[0x4f] = stTextRegion80;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion81 = stTextRegion94;
		stTextRegion81.ch = 'r';
		stTextRegion81.rc = new Rectangle(0x18, 0xc7, 0x12, 0x1b);
		stTextRegionArray[80] = stTextRegion81;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion82 = stTextRegion94;
		stTextRegion82.ch = 's';
		stTextRegion82.rc = new Rectangle(0x2a, 0xc7, 0x13, 0x1b);
		stTextRegionArray[0x51] = stTextRegion82;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion83 = stTextRegion94;
		stTextRegion83.ch = 't';
		stTextRegion83.rc = new Rectangle(0x3f, 0xc7, 0x10, 0x1b);
		stTextRegionArray[0x52] = stTextRegion83;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion84 = stTextRegion94;
		stTextRegion84.ch = 'u';
		stTextRegion84.rc = new Rectangle(80, 0xc7, 20, 0x1b);
		stTextRegionArray[0x53] = stTextRegion84;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion85 = stTextRegion94;
		stTextRegion85.ch = 'v';
		stTextRegion85.rc = new Rectangle(0x68, 0xc7, 20, 0x1b);
		stTextRegionArray[0x54] = stTextRegion85;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion86 = stTextRegion94;
		stTextRegion86.ch = 'w';
		stTextRegion86.rc = new Rectangle(0x7f, 0xc7, 0x1a, 0x1b);
		stTextRegionArray[0x55] = stTextRegion86;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion87 = stTextRegion94;
		stTextRegion87.ch = 'x';
		stTextRegion87.rc = new Rectangle(0x9a, 0xc7, 0x16, 0x1b);
		stTextRegionArray[0x56] = stTextRegion87;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion88 = stTextRegion94;
		stTextRegion88.ch = 'y';
		stTextRegion88.rc = new Rectangle(0xb1, 0xc7, 0x16, 0x1b);
		stTextRegionArray[0x57] = stTextRegion88;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion89 = stTextRegion94;
		stTextRegion89.ch = 'z';
		stTextRegion89.rc = new Rectangle(200, 0xc7, 0x13, 0x1b);
		stTextRegionArray[0x58] = stTextRegion89;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion90 = stTextRegion94;
		stTextRegion90.ch = '{';
		stTextRegion90.rc = new Rectangle(220, 0xc7, 15, 0x1b);
		stTextRegionArray[0x59] = stTextRegion90;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion91 = stTextRegion94;
		stTextRegion91.ch = '|';
		stTextRegion91.rc = new Rectangle(0xeb, 0xc7, 13, 0x1b);
		stTextRegionArray[90] = stTextRegion91;
		stTextRegion94 = new STTextRegion();
		STTextRegion stTextRegion92 = stTextRegion94;
		stTextRegion92.ch = '}';
		stTextRegion92.rc = new Rectangle(1, 0xe3, 15, 0x1b);
		stTextRegionArray[0x5b] = stTextRegion92;
		STTextRegion stTextRegion93 = new STTextRegion();
		stTextRegion93.ch = '~';
		stTextRegion93.rc = new Rectangle(0x12, 0xe3, 0x12, 0x1b);
		stTextRegionArray[0x5c] = stTextRegion93;

		stTextRegionArray[0x5d] = new STTextRegion();                       // #24954 2011.4.23 yyagi
		stTextRegionArray[0x5d].ch = '@';
		stTextRegionArray[0x5d].rc = new Rectangle(38, 227, 28, 28);
		stTextRegionArray[0x5e] = new STTextRegion();
		stTextRegionArray[0x5e].ch = '`';
		stTextRegionArray[0x5e].rc = new Rectangle(69, 226, 14, 29);

		float scaleX = OpenTaiko.Tx.Config_Font.szTextureSize.Width / 256.0f;
		float scaleY = OpenTaiko.Tx.Config_Font.szTextureSize.Height / 256.0f;

		for (int i = 0; i < stTextRegionArray.Length; i++) {
			stTextRegionArray[i].rc = new Rectangle((int)(stTextRegionArray[i].rc.X * scaleX), (int)(stTextRegionArray[i].rc.Y * scaleY),
				(int)(stTextRegionArray[i].rc.Width * scaleX), (int)(stTextRegionArray[i].rc.Height * scaleY));
		}

		this.stTextRegion = stTextRegionArray;
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		//if( !base.b活性化してない )
		//{
		//	if( this.tx強調文字 != null )
		//	{
		//		this.tx強調文字.Dispose();
		//		this.tx強調文字 = null;
		//	}
		//	if( this.tx通常文字 != null )
		//	{
		//		this.tx通常文字.Dispose();
		//		this.tx通常文字 = null;
		//	}
		base.ReleaseManagedResource();
		//}
	}


	// その他

	#region [ private ]
	//-----------------
	[StructLayout(LayoutKind.Sequential)]
	private struct STTextRegion {
		public char ch;
		public Rectangle rc;
	}

	private STTextRegion[] stTextRegion;
	//private CTexture tx強調文字;
	//private CTexture tx通常文字;
	//-----------------
	#endregion
}
