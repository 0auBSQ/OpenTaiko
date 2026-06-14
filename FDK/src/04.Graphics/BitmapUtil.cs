using System.Runtime.InteropServices;
using SkiaSharp;

namespace FDK;

public static class BitmapUtil {
	// 定数

	public const uint DIB_PAL_COLORS = 1;
	public const uint DIB_RGB_COLORS = 0;


	// 構造体

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct BITMAPFILEHEADER {
		public ushort bfType;
		public uint bfSize;
		public ushort bfReserved1;
		public ushort bfReserved2;
		public uint bfOffBits;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct BITMAPINFOHEADER {
		public const int BI_RGB = 0;
		public uint biSizeStructSize;
		public int biWidthBitmapWidthdot;
		public int biHeightBitmapHeightdot;
		public ushort biPlanesPlaneCount;
		public ushort biBitCount;
		public uint biCompressionCompressFormat;
		public uint biSizeImage;
		public int biXPelsPerMeteHorizontalDirectionResolution;
		public int biYPelsPerMeterVerticalDirectionResolution;
		public uint biClrUsedColorTableIndexCount;
		public uint biClrImportant;
	}


	// メソッド

	public static unsafe SKBitmap ToBitmap(IntPtr pBITMAPINFOHEADER) {
		BITMAPFILEHEADER bitmapfileheader;
		BITMAPINFOHEADER* bitmapinfoheaderPtr = (BITMAPINFOHEADER*)pBITMAPINFOHEADER;
		bitmapfileheader.bfType = 0x4d42;
		bitmapfileheader.bfOffBits = (uint)(sizeof(BITMAPFILEHEADER) + sizeof(BITMAPINFOHEADER));
		bitmapfileheader.bfSize = bitmapfileheader.bfOffBits + bitmapinfoheaderPtr->biSizeImage;
		MemoryStream output = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(output);
		byte[] destination = new byte[sizeof(BITMAPFILEHEADER)];
		Marshal.Copy((IntPtr)(&bitmapfileheader), destination, 0, destination.Length);
		writer.Write(destination);
		destination = new byte[sizeof(BITMAPINFOHEADER)];
		Marshal.Copy(pBITMAPINFOHEADER, destination, 0, destination.Length);
		writer.Write(destination);
		destination = new byte[bitmapinfoheaderPtr->biSizeImage];
		bitmapinfoheaderPtr++;
		Marshal.Copy((IntPtr)bitmapinfoheaderPtr, destination, 0, destination.Length);
		writer.Write(destination);
		writer.Flush();
		writer.BaseStream.Position = 0L;
		return null;
		//return new SKBitmap( writer.BaseStream );
	}
}
