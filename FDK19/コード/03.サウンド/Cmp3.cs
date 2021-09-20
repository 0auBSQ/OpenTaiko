using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Threading;


namespace FDK
{
	public unsafe class Cmp3 : SoundDecoder
	{
//		static byte[] FOURCC = Encoding.ASCII.GetBytes( "SggO" );	// OggS の little endian


		#region [ SoundDecoder.dll インポート（mpr 関連）]
		//-----------------
		[DllImport( "SoundDecoder.dll" )]
		private static extern void mp3Close( int nHandle );
		[DllImport( "SoundDecoder.dll" )]
		private static extern int mp3Decode( int nHandle, IntPtr pDest, uint szDestSize, int bLoop );
		[DllImport( "SoundDecoder.dll" )]
		private static extern int mp3GetFormat( int nHandle, ref CWin32.WAVEFORMATEX wfx );
		[DllImport( "SoundDecoder.dll" )]
		private static extern uint mp3GetTotalPCMSize( int nHandle );
		[DllImport( "SoundDecoder.dll" )]
		private static extern int mp3Open( string fileName );
		[DllImport( "SoundDecoder.dll" )]
		private static extern int mp3Seek( int nHandle, uint dwPosition );
		//-----------------
		#endregion


		public override int Open( string filename )
		{
			return mp3Open( filename );
		}
		public override int GetFormat( int nHandle, ref CWin32.WAVEFORMATEX wfx )
		{
			return mp3GetFormat( nHandle, ref wfx );
		}
		public override uint GetTotalPCMSize( int nHandle )
		{
			return mp3GetTotalPCMSize( nHandle );
		}
		public override int Seek( int nHandle, uint dwPosition )
		{
			return mp3Seek( nHandle, dwPosition );
		}
		public override int Decode( int nHandle, IntPtr pDest, uint szDestSize, int bLoop )
		{
			return mp3Decode( nHandle, pDest, szDestSize, bLoop );
		}

		public override void Close( int nHandle )
		{
			mp3Close( nHandle );
		}

	}
}
