using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.IO;

namespace FDK
{
	public static class BitmapUtil
	{
		// 定数

		public const uint DIB_PAL_COLORS = 1;
		public const uint DIB_RGB_COLORS = 0;


		// 構造体

		[StructLayout( LayoutKind.Sequential, Pack = 1 )]
		public struct BITMAPFILEHEADER
		{
			public ushort bfType;
			public uint bfSize;
			public ushort bfReserved1;
			public ushort bfReserved2;
			public uint bfOffBits;
		}

		[StructLayout( LayoutKind.Sequential, Pack = 1 )]
		public struct BITMAPINFOHEADER
		{
			public const int BI_RGB = 0;
			public uint biSize構造体のサイズ;
			public int biWidthビットマップの幅dot;
			public int biHeightビットマップの高さdot;
			public ushort biPlanes面の数;
			public ushort biBitCount;
			public uint biCompression圧縮形式;
			public uint biSizeImage画像イメージのサイズ;
			public int biXPelsPerMete水平方向の解像度;
			public int biYPelsPerMeter垂直方向の解像度;
			public uint biClrUsed色テーブルのインデックス数;
			public uint biClrImportant表示に必要な色インデックスの数;
		}


		// メソッド

		public static unsafe Bitmap ToBitmap( IntPtr pBITMAPINFOHEADER )
		{
			BITMAPFILEHEADER bitmapfileheader;
			BITMAPINFOHEADER* bitmapinfoheaderPtr = (BITMAPINFOHEADER*) pBITMAPINFOHEADER;
			bitmapfileheader.bfType = 0x4d42;
			bitmapfileheader.bfOffBits = (uint) ( sizeof( BITMAPFILEHEADER ) + sizeof( BITMAPINFOHEADER ) );
			bitmapfileheader.bfSize = bitmapfileheader.bfOffBits + bitmapinfoheaderPtr->biSizeImage画像イメージのサイズ;
			MemoryStream output = new MemoryStream();
			BinaryWriter writer = new BinaryWriter( output );
			byte[] destination = new byte[ sizeof( BITMAPFILEHEADER ) ];
			Marshal.Copy( (IntPtr) ( &bitmapfileheader ), destination, 0, destination.Length );
			writer.Write( destination );
			destination = new byte[ sizeof( BITMAPINFOHEADER ) ];
			Marshal.Copy( pBITMAPINFOHEADER, destination, 0, destination.Length );
			writer.Write( destination );
			destination = new byte[ bitmapinfoheaderPtr->biSizeImage画像イメージのサイズ ];
			bitmapinfoheaderPtr++;
			Marshal.Copy( (IntPtr) bitmapinfoheaderPtr, destination, 0, destination.Length );
			writer.Write( destination );
			writer.Flush();
			writer.BaseStream.Position = 0L;
			return new Bitmap( writer.BaseStream );
		}
	}
}
