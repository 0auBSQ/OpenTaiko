using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Threading;


namespace FDK
{
	public unsafe class Cogg : SoundDecoder
	{
		static byte[] FOURCC = Encoding.ASCII.GetBytes( "SggO" );	// OggS の little endian


		#region [ SoundDecoder.dll インポート（ogg 関連）]
		//-----------------
		[DllImport( "SoundDecoder.dll" )]
		private static extern void oggClose( int nHandle );
		[DllImport( "SoundDecoder.dll" )]
		private static extern int oggDecode( int nHandle, IntPtr pDest, uint szDestSize, int bLoop );
		[DllImport( "SoundDecoder.dll" )]
		private static extern int oggGetFormat( int nHandle, ref CWin32.WAVEFORMATEX wfx );
		[DllImport( "SoundDecoder.dll" )]
		private static extern uint oggGetTotalPCMSize( int nHandle );
		[DllImport( "SoundDecoder.dll" )]
		private static extern int oggOpen( string fileName );
		[DllImport( "SoundDecoder.dll" )]
		private static extern int oggSeek( int nHandle, uint dwPosition );
		//-----------------
		#endregion


		public override int Open( string filename )
		{
			return oggOpen( filename );
		}
		public override int GetFormat( int nHandle, ref CWin32.WAVEFORMATEX wfx )
		{
			return oggGetFormat( nHandle, ref wfx );
		}
		public override uint GetTotalPCMSize( int nHandle )
		{
			return oggGetTotalPCMSize( nHandle );
		}
		public override int Seek( int nHandle, uint dwPosition )
		{
			return oggSeek( nHandle, dwPosition );
		}
		public override int Decode( int nHandle, IntPtr pDest, uint szDestSize, int bLoop )
		{
			return oggDecode( nHandle, pDest, szDestSize, bLoop );
		}

		public override void Close( int nHandle )
		{
			oggClose( nHandle );
		}

	}
}
