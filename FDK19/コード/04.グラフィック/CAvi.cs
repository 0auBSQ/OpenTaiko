using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using SlimDX;

namespace FDK
{
	public class CAvi : IDisposable
	{
		// プロパティ

		public uint dwスケール 
		{
			get;
			set; 
		}
		public uint dwレート
		{
			get;
			set;
		}
		public uint nフレーム高さ
		{ 
			get;
			set;
		}
		public uint nフレーム幅
		{
			get;
			set; 
		}


		// コンストラクタ

		public CAvi( string filename )
		{
			if ( AVIFileOpen( out this.aviFile, filename, OpenFileFlags.OF_READ, IntPtr.Zero ) != 0 )
			{
				this.Release();
				throw new Exception( "AVIFileOpen failed." );
			}
			if ( AVIFileGetStream( this.aviFile, out this.aviStream, streamtypeVIDEO, 0 ) != 0 )
			{
				this.Release();
				throw new Exception( "AVIFileGetStream failed." );
			}
			var info = new AVISTREAMINFO();
			AVIStreamInfo( this.aviStream, ref info, Marshal.SizeOf( info ) );
			this.dwレート = info.dwRate;
			this.dwスケール = info.dwScale;
			this.nフレーム幅 = info.rcFrame.right - info.rcFrame.left;
			this.nフレーム高さ = info.rcFrame.bottom - info.rcFrame.top;
			try
			{
				this.frame = AVIStreamGetFrameOpen( this.aviStream, 0 );
			}
			catch
			{
				this.Release();
				throw new Exception( "AVIStreamGetFrameOpen failed." );
			}
		}


		// メソッド

		public static void t初期化()
		{
			AVIFileInit();
		}
		public static void t終了()
		{
			AVIFileExit();
		}
		
		public Bitmap GetFrame( int no )
		{
			if( this.aviStream == IntPtr.Zero )
				throw new InvalidOperationException();

			return BitmapUtil.ToBitmap( AVIStreamGetFrame( this.frame, no ) );
		}
		public int GetFrameNoFromTime( int time )
		{
			return (int) ( time * ( ( (double) this.dwレート ) / ( 1000.0 * this.dwスケール ) ) );
		}
		public IntPtr GetFramePtr( int no )
		{
			if( this.aviStream == IntPtr.Zero )
				throw new InvalidOperationException();

			return AVIStreamGetFrame( this.frame, no );
		}
		public int GetMaxFrameCount()
		{
			if( this.aviStream == IntPtr.Zero )
				throw new InvalidOperationException();

			return AVIStreamLength( this.aviStream );
		}
		
		public unsafe void tBitmap24ToGraphicsStreamR5G6B5( BitmapUtil.BITMAPINFOHEADER* pBITMAPINFOHEADER, DataStream gs, int nWidth, int nHeight )
		{
			int nBmpWidth = pBITMAPINFOHEADER->biWidthビットマップの幅dot;
			int nBmpHeight = pBITMAPINFOHEADER->biHeightビットマップの高さdot;
			int nBmpLineByte = ( nBmpWidth * 3 ) + ( ( 4 - ( ( nBmpWidth * 3 ) % 4 ) ) % 4 );

			ushort* pTexture = (ushort*) gs.DataPointer.ToPointer();
			byte* pBitmap = (byte*) ( pBITMAPINFOHEADER + 1 );
			
			for( int i = 0; i < nBmpHeight; i++ )
			{
				if( i >= nHeight )
					break;

				for( int j = 0; j < nBmpWidth; j++ )
				{
					if( j >= nWidth )
						break;

					ushort B = (ushort) ( ( *( ( pBitmap + ( ( ( nBmpHeight - i ) - 1 ) * nBmpLineByte ) ) + ( j * 3 ) + 0 ) >> 3 ) & 0x1f );
					ushort G = (ushort) ( ( *( ( pBitmap + ( ( ( nBmpHeight - i ) - 1 ) * nBmpLineByte ) ) + ( j * 3 ) + 1 ) >> 2 ) & 0x3f );
					ushort R = (ushort) ( ( *( ( pBitmap + ( ( ( nBmpHeight - i ) - 1 ) * nBmpLineByte ) ) + ( j * 3 ) + 2 ) >> 3 ) & 0x1f );
					*( pTexture + ( i * nWidth ) + j ) = (ushort) ( ( R << 11 ) | ( G << 5 ) | B );
				}
			}
		}
		public unsafe void tBitmap24ToGraphicsStreamX8R8G8B8( BitmapUtil.BITMAPINFOHEADER* pBITMAPINFOHEADER, DataStream ds, int nWidth, int nHeight )
		{
			int nBmpWidth = pBITMAPINFOHEADER->biWidthビットマップの幅dot;
			int nBmpHeight = pBITMAPINFOHEADER->biHeightビットマップの高さdot;
			int nBmpLineByte = ( nBmpWidth * 3 ) + ( ( 4 - ( ( nBmpWidth * 3 ) % 4 ) ) % 4 );
			
			uint* pTexture = (uint*) ds.DataPointer.ToPointer();
			byte* pBitmap = (byte*) ( pBITMAPINFOHEADER + 1 );
			
			for( int i = 0; i < nBmpHeight; i++ )
			{
				if( i >= nHeight )
					break;

				for( int j = 0; j < nBmpWidth; j++ )
				{
					if( j >= nWidth )
						break;

					uint B = *( ( pBitmap + ( ( ( nBmpHeight - i ) - 1 ) * nBmpLineByte ) ) + ( j * 3 ) + 0 );
					uint G = *( ( pBitmap + ( ( ( nBmpHeight - i ) - 1 ) * nBmpLineByte ) ) + ( j * 3 ) + 1 );
					uint R = *( ( pBitmap + ( ( ( nBmpHeight - i ) - 1 ) * nBmpLineByte ) ) + ( j * 3 ) + 2 );
					*( pTexture + ( i * nWidth ) + j ) = ( R << 16 ) | ( G << 8 ) | B;
				}
			}
		}

		#region [ Dispose-Finalize パターン実装 ]
		//-----------------
		public void Dispose()
		{
			this.Dispose( true );
			GC.SuppressFinalize( this );		// 2011.8.19 from: 忘れてた。
		}
		protected void Dispose( bool disposeManagedObjects )
		{
			if( this.bDispose完了済み )
				return;

			if( disposeManagedObjects )
			{
				// (A) Managed リソースの解放
			}

			// (B) Unamanaged リソースの解放

			if( this.frame != IntPtr.Zero )
				AVIStreamGetFrameClose( this.frame );

			this.Release();
			this.bDispose完了済み = true;
		}
		~CAvi()
		{
			this.Dispose( false );
		}
		//-----------------
		#endregion


		// その他

		#region [ Win32 AVI関連関数インポート ]
		//-----------------
		internal enum OpenFileFlags : uint
		{
			OF_CANCEL = 0x800,
			OF_CREATE = 0x1000,
			OF_DELETE = 0x200,
			OF_EXIST = 0x4000,
			OF_PARSE = 0x100,
			OF_PROMPT = 0x2000,
			OF_READ = 0,
			OF_READWRITE = 2,
			OF_REOPEN = 0x8000,
			OF_SHARE_COMPAT = 0,
			OF_SHARE_DENY_NONE = 0x40,
			OF_SHARE_DENY_READ = 0x30,
			OF_SHARE_DENY_WRITE = 0x20,
			OF_SHARE_EXCLUSIVE = 0x10,
			OF_VERIFY = 0x400,
			OF_WRITE = 1
		}

		[StructLayout( LayoutKind.Sequential, Pack = 1 )]
		internal struct AVISTREAMINFO
		{
			public uint fccType;
			public uint fccHandler;
			public uint dwFlags;
			public uint dwCaps;
			public ushort wPriority;
			public ushort wLanguage;
			public uint dwScale;
			public uint dwRate;
			public uint dwStart;
			public uint dwLength;
			public uint dwInitialFrames;
			public uint dwSuggestedBufferSize;
			public uint dwQuality;
			public uint dwSampleSize;
			public CAvi.RECT rcFrame;
			public uint dwEditCount;
			public uint dwFormatChangeCount;
			[MarshalAs( UnmanagedType.ByValArray, SizeConst = 0x40 )]
			public ushort[] szName;
		}

		[StructLayout( LayoutKind.Sequential, Pack = 1 )]
		internal struct RECT
		{
			public uint left;
			public uint top;
			public uint right;
			public uint bottom;
		}

		[DllImport( "AVIFIL32" )]
		private static extern void AVIFileExit();
		[DllImport( "AVIFIL32" )]
		private static extern uint AVIFileGetStream( IntPtr pfile, out IntPtr ppavi, uint fccType, int lParam );
		[DllImport( "AVIFIL32" )]
		private static extern void AVIFileInit();
		[DllImport( "AVIFIL32" )]
		private static extern uint AVIFileOpen( out IntPtr ppfile, string szFile, OpenFileFlags mode, IntPtr pclsidHandler );
		[DllImport( "AVIFIL32" )]
		private static extern int AVIFileRelease( IntPtr pfile );
		[DllImport( "AVIFIL32" )]
		private static extern IntPtr AVIStreamGetFrame( IntPtr pgf, int lPos );
		[DllImport( "AVIFIL32" )]
		private static extern uint AVIStreamGetFrameClose( IntPtr pget );
		[DllImport( "AVIFIL32" )]
		private static extern IntPtr AVIStreamGetFrameOpen( IntPtr pavi, int lpbiWanted );
		[DllImport( "AVIFIL32" )]
		private static extern int AVIStreamInfo( IntPtr pavi, ref AVISTREAMINFO psi, int lSize );
		[DllImport( "AVIFIL32" )]
		private static extern int AVIStreamLength( IntPtr pavi );
		[DllImport( "AVIFIL32" )]
		private static extern int AVIStreamRelease( IntPtr pavi );
		//-----------------
		#endregion

		#region [ private ]
		//-----------------
		private IntPtr aviFile = IntPtr.Zero;
		private const string AVIFILE32 = "AVIFIL32";
		private const int AVIGETFRAMEF_BESTDISPLAYFMT = 1;
		private IntPtr aviStream = IntPtr.Zero;
		private bool bDispose完了済み;
		private IntPtr frame = IntPtr.Zero;
		private static readonly uint streamtypeAUDIO = mmioFOURCC( 'a', 'u', 'd', 's' );
		private static readonly uint streamtypeMIDI = mmioFOURCC( 'm', 'i', 'd', 's' );
		private static readonly uint streamtypeTEXT = mmioFOURCC( 't', 'x', 't', 's' );
		private static readonly uint streamtypeVIDEO = mmioFOURCC( 'v', 'i', 'd', 's' );

		private static uint mmioFOURCC( char c0, char c1, char c2, char c3 )
		{
			return ( (uint) c3 << 0x18 ) | ( (uint) c2 << 0x10 ) | ( (uint) c1 << 0x08 ) | (uint) c0;
		}
		private void Release()
		{
			if( this.aviStream != IntPtr.Zero )
			{
				AVIStreamRelease( this.aviStream );
				this.aviStream = IntPtr.Zero;
			}
			if( this.aviFile != IntPtr.Zero )
			{
				AVIFileRelease( this.aviFile );
				this.aviFile = IntPtr.Zero;
			}
		}
		//-----------------
		#endregion
	}
}
