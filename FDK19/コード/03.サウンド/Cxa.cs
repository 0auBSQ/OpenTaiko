using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Threading;


namespace FDK
{
	public unsafe class Cxa : SoundDecoder	//, IDisposable
	{
		static byte[] FOURCC = Encoding.ASCII.GetBytes( "1DWK" );	// KWD1 の little endian

		#region [ XA用構造体の宣言 ]
		[StructLayout(LayoutKind.Sequential)]
		public struct XASTREAMHEADER {
			public byte* pSrc;
			public uint nSrcLen;
			public uint nSrcUsed;
			public byte* pDst;
			public uint nDstLen;
			public uint nDstUsed;
		}

		[StructLayout( LayoutKind.Sequential )]
		public struct XAHEADER
		{
			public uint id;
			public uint nDataLen;
			public uint nSamples;
			public ushort nSamplesPerSec;
			public byte nBits;
			public byte nChannels;
			public uint nLoopPtr;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public short[] befL;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public short[] befR;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public byte[] pad;
		}
		#endregion

		#region [ xadec.dllとのリンク ]
		[DllImport( "xadec.dll", EntryPoint = "xaDecodeOpen", CallingConvention = CallingConvention.Cdecl )]
		public extern static IntPtr xaDecodeOpen( ref XAHEADER pxah, out FDK.CWin32.WAVEFORMATEX pwfx );
		[DllImport( "xadec.dll", EntryPoint = "xaDecodeClose", CallingConvention = CallingConvention.Cdecl )]
		public extern static bool xaDecodeClose( IntPtr hxas );
		[DllImport( "xadec.dll", EntryPoint = "xaDecodeSize", CallingConvention = CallingConvention.Cdecl )]
		public extern static bool xaDecodeSize( IntPtr hxas, uint slen, out uint pdlen );
		[DllImport( "xadec.dll", EntryPoint = "xaDecodeConvert", CallingConvention = CallingConvention.Cdecl )]
		public extern static bool xaDecodeConvert( IntPtr hxas, ref XASTREAMHEADER psh );
		#endregion

		public XAHEADER xaheader;
		public XASTREAMHEADER xastreamheader;
		public CWin32.WAVEFORMATEX waveformatex;

		private string filename;
		private byte[] srcBuf = null;
		private int nHandle = -1;

		public override int Open( string filename )
		{
			this.filename = filename;

			#region [ XAヘッダと、XAデータの読み出し  ]
			xaheader = new XAHEADER();
			using ( FileStream fs = new FileStream( filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )	// FileShare を付けとかないと、Close() 後もロックがかかる??
			{
				using ( BinaryReader br = new BinaryReader( fs ) )
				{
					xaheader.id = br.ReadUInt32();
					xaheader.nDataLen = br.ReadUInt32();
					xaheader.nSamples = br.ReadUInt32();
					xaheader.nSamplesPerSec = br.ReadUInt16();
					xaheader.nBits = br.ReadByte();
					xaheader.nChannels = br.ReadByte();
					xaheader.nLoopPtr = br.ReadUInt32();

					xaheader.befL = new short[ 2 ];
					xaheader.befR = new short[ 2 ];
					xaheader.pad = new byte[ 4 ];

					xaheader.befL[ 0 ] = br.ReadInt16();
					xaheader.befL[ 1 ] = br.ReadInt16();
					xaheader.befR[ 0 ] = br.ReadInt16();
					xaheader.befR[ 1 ] = br.ReadInt16();
					xaheader.pad = br.ReadBytes( 4 );

					srcBuf = new byte[ xaheader.nDataLen ];
					srcBuf = br.ReadBytes( (int) xaheader.nDataLen );
				}
			}
			//string xaid = Encoding.ASCII.GetString( xah.id );
			#region [ デバッグ表示 ]
			//Debug.WriteLine( "**XAHEADER**" );
			//Debug.WriteLine( "id=             " + xaheader.id.ToString( "X8" ) );
			//Debug.WriteLine( "nDataLen=       " + xaheader.nDataLen.ToString( "X8" ) );
			//Debug.WriteLine( "nSamples=       " + xaheader.nSamples.ToString( "X8" ) );
			//Debug.WriteLine( "nSamplesPerSec= " + xaheader.nSamplesPerSec.ToString( "X4" ) );
			//Debug.WriteLine( "nBits=          " + xaheader.nBits.ToString( "X2" ) );
			//Debug.WriteLine( "nChannels=      " + xaheader.nChannels.ToString( "X2" ) );
			//Debug.WriteLine( "nLoopPtr=       " + xaheader.nLoopPtr.ToString( "X8" ) );
			//Debug.WriteLine( "befL[0]=        " + xaheader.befL[ 0 ].ToString( "X4" ) );
			//Debug.WriteLine( "befL[1]=        " + xaheader.befL[ 1 ].ToString( "X4" ) );
			//Debug.WriteLine( "befR[0]=        " + xaheader.befR[ 0 ].ToString( "X4" ) );
			//Debug.WriteLine( "befR[1]=        " + xaheader.befR[ 1 ].ToString( "X4" ) );
			#endregion
			#endregion

			IntPtr hxas;

			#region [ WAVEFORMEX情報の取得  ]
			waveformatex = new CWin32.WAVEFORMATEX();
			hxas = xaDecodeOpen( ref xaheader, out waveformatex );
			if ( hxas == null )
			{
				Trace.TraceError( "Error: xa: Open(): xaDecodeOpen(): " + Path.GetFileName( filename ) );
				return -1;
			}

			#region [ デバッグ表示 ]
			//Debug.WriteLine( "**WAVEFORMATEX**" );
			//Debug.WriteLine( "wFormatTag=      " + waveformatex.wFormatTag.ToString( "X4" ) );
			//Debug.WriteLine( "nChannels =      " + waveformatex.nChannels.ToString( "X4" ) );
			//Debug.WriteLine( "nSamplesPerSec=  " + waveformatex.nSamplesPerSec.ToString( "X8" ) );
			//Debug.WriteLine( "nAvgBytesPerSec= " + waveformatex.nAvgBytesPerSec.ToString( "X8" ) );
			//Debug.WriteLine( "nBlockAlign=     " + waveformatex.nBlockAlign.ToString( "X4" ) );
			//Debug.WriteLine( "wBitsPerSample=  " + waveformatex.wBitsPerSample.ToString( "X4" ) );
			//Debug.WriteLine( "cbSize=          " + waveformatex.cbSize.ToString( "X4" ) );
			#endregion
			#endregion

			this.nHandle = (int) hxas;
			return (int) hxas;
		}
		public override int GetFormat( int nHandle, ref CWin32.WAVEFORMATEX wfx )
		{
			#region [ WAVEFORMATEX構造体の手動コピー ]
			wfx.nAvgBytesPerSec = waveformatex.nAvgBytesPerSec;
			wfx.wBitsPerSample  =  waveformatex.wBitsPerSample;
			wfx.nBlockAlign     =  waveformatex.nBlockAlign;
			wfx.nChannels       =  waveformatex.nChannels;
			wfx.wFormatTag      = waveformatex.wFormatTag;
			wfx.nSamplesPerSec  = waveformatex.nSamplesPerSec;

			return 0;
			#endregion
		}
		public override uint GetTotalPCMSize( int nHandle )
		{
			#region [ データ長の取得 ]
			uint dlen;
			xaDecodeSize( (IntPtr) nHandle, xaheader.nDataLen, out dlen );
			#region [ デバッグ表示 ]
			//Debug.WriteLine( "**INTERNAL VALUE**" );
			//Debug.WriteLine( "dlen=          " + dlen );
			#endregion
			#endregion

			return dlen;
		}
		public override int Seek( int nHandle, uint dwPosition )
		{
			return 0;
		}
		public override int Decode( int nHandle, IntPtr pDest, uint szDestSize, int bLoop )
		{
			#region [ xaデータのデコード ]
			xastreamheader = new XASTREAMHEADER();
			unsafe
			{
				fixed ( byte* pXaBuf = srcBuf )
				{
					byte* pWavBuf = (byte*) pDest;

					xastreamheader.pSrc = pXaBuf;
					xastreamheader.nSrcLen = xaheader.nDataLen;
					xastreamheader.nSrcUsed = 0;
					xastreamheader.pDst = pWavBuf;
					xastreamheader.nDstLen = szDestSize;
					xastreamheader.nDstUsed = 0;
					if ( !xaDecodeConvert( (IntPtr) nHandle, ref xastreamheader ) )
					{
						Trace.TraceError( "Error: xaDecodeConvert(): " + Path.GetFileName( filename ) );
						return -1;
					}
				}
			}
			#region [ デバッグ表示 ]
			//Debug.WriteLine( "**XASTREAMHEADER**" );
			//Debug.WriteLine( "nSrcLen=  " + xastreamheader.nSrcLen );
			//Debug.WriteLine( "nSrcUsed= " + xastreamheader.nSrcUsed );
			//Debug.WriteLine( "nDstLen=  " + xastreamheader.nDstLen );
			//Debug.WriteLine( "nDstUsed= " + xastreamheader.nDstUsed );
			#endregion
			#endregion

			return 0;
		}
		public override void Close( int nHandle )
		{
			#region [ xaファイルのクローズ ]
			if ( !xaDecodeClose( (IntPtr) nHandle ) )
			{
				Trace.TraceError( "Error: xaDecodeClose(): " + Path.GetFileName( filename ) );
			}
			srcBuf = null;
			#endregion
		}



		//#region [ IDisposable 実装 ]
		////-----------------
		//private bool bDispose完了済み = false;
		//public void Dispose()
		//{
		//    if ( !this.bDispose完了済み )
		//    {
		//        if ( srcBuf != null )
		//        {
		//            srcBuf = null;
		//        }
		//        if ( dstBuf != null )
		//        {
		//            dstBuf = null;
		//        }

		//        if ( this.nHandle >= 0 )
		//        {
		//            this.Close( this.nHandle );
		//            this.nHandle = -1;
		//        }
		//        this.bDispose完了済み = true;
		//    }
		//}
		////-----------------
		//#endregion

#if false
		/// <summary>
		/// xaファイルを読み込んで、wavにdecodeする
		/// </summary>
		/// <param name="filename">xaファイル名</param>
		/// <param name="wavBuf">wavファイルが格納されるバッファ</param>
		/// <returns></returns>
		public bool Decode( string filename, out byte[] wavBuf )
		{
			// Debug.WriteLine( "xa: Decode: " + Path.GetFileName( filename ) );

			#region [ XAヘッダと、XAデータの読み出し  ]
			xaheader = new XAHEADER();
			byte[] xaBuf;
			using ( FileStream fs = new FileStream( filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) )	// FileShare を付けとかないと、Close() 後もロックがかかる??
			{
				using ( BinaryReader br = new BinaryReader( fs ) )
				{
					xaheader.id = br.ReadUInt32();
					xaheader.nDataLen = br.ReadUInt32();
					xaheader.nSamples = br.ReadUInt32();
					xaheader.nSamplesPerSec = br.ReadUInt16();
					xaheader.nBits = br.ReadByte();
					xaheader.nChannels = br.ReadByte();
					xaheader.nLoopPtr = br.ReadUInt32();

					xaheader.befL = new short[ 2 ];
					xaheader.befR = new short[ 2 ];
					xaheader.pad = new byte[ 4 ];

					xaheader.befL[ 0 ] = br.ReadInt16();
					xaheader.befL[ 1 ] = br.ReadInt16();
					xaheader.befR[ 0 ] = br.ReadInt16();
					xaheader.befR[ 1 ] = br.ReadInt16();
					xaheader.pad = br.ReadBytes( 4 );

					xaBuf = new byte[ xaheader.nDataLen ];
					xaBuf = br.ReadBytes( (int) xaheader.nDataLen );
				}
			}
			//string xaid = Encoding.ASCII.GetString( xah.id );
			#region [ デバッグ表示 ]
			//Debug.WriteLine( "**XAHEADER**" );
			//Debug.WriteLine( "id=             " + xaheader.id.ToString( "X8" ) );
			//Debug.WriteLine( "nDataLen=       " + xaheader.nDataLen.ToString( "X8" ) );
			//Debug.WriteLine( "nSamples=       " + xaheader.nSamples.ToString( "X8" ) );
			//Debug.WriteLine( "nSamplesPerSec= " + xaheader.nSamplesPerSec.ToString( "X4" ) );
			//Debug.WriteLine( "nBits=          " + xaheader.nBits.ToString( "X2" ) );
			//Debug.WriteLine( "nChannels=      " + xaheader.nChannels.ToString( "X2" ) );
			//Debug.WriteLine( "nLoopPtr=       " + xaheader.nLoopPtr.ToString( "X8" ) );
			//Debug.WriteLine( "befL[0]=        " + xaheader.befL[ 0 ].ToString( "X4" ) );
			//Debug.WriteLine( "befL[1]=        " + xaheader.befL[ 1 ].ToString( "X4" ) );
			//Debug.WriteLine( "befR[0]=        " + xaheader.befR[ 0 ].ToString( "X4" ) );
			//Debug.WriteLine( "befR[1]=        " + xaheader.befR[ 1 ].ToString( "X4" ) );
			#endregion
			#endregion

			object lockobj = new object();
			lock ( lockobj )	// スレッドセーフじゃないかも知れないので、念のため
			{
				#region [ WAVEFORMEX情報の取得  ]
				waveformatex = new CWin32.WAVEFORMATEX();
				IntPtr hxas = xaDecodeOpen( ref xaheader, out waveformatex );
				if ( hxas == null )
				{
					Trace.TraceError( "Error: xaDecodeOpen(): " + Path.GetFileName( filename ) );
					wavBuf = null;
					return false;
				}

				#region [ デバッグ表示 ]
				//Debug.WriteLine( "**WAVEFORMATEX**" );
				//Debug.WriteLine( "wFormatTag=      " + waveformatex.wFormatTag.ToString( "X4" ) );
				//Debug.WriteLine( "nChannels =      " + waveformatex.nChannels.ToString( "X4" ) );
				//Debug.WriteLine( "nSamplesPerSec=  " + waveformatex.nSamplesPerSec.ToString( "X8" ) );
				//Debug.WriteLine( "nAvgBytesPerSec= " + waveformatex.nAvgBytesPerSec.ToString( "X8" ) );
				//Debug.WriteLine( "nBlockAlign=     " + waveformatex.nBlockAlign.ToString( "X4" ) );
				//Debug.WriteLine( "wBitsPerSample=  " + waveformatex.wBitsPerSample.ToString( "X4" ) );
				//Debug.WriteLine( "cbSize=          " + waveformatex.cbSize.ToString( "X4" ) );
				#endregion
				#endregion

				#region [ データ長の取得 ]
				uint dlen;
				xaDecodeSize( hxas, xaheader.nDataLen, out dlen );
				#region [ デバッグ表示 ]
				//Debug.WriteLine( "**INTERNAL VALUE**" );
				//Debug.WriteLine( "dlen=          " + dlen );
				#endregion
				#endregion

				#region [ xaデータのデコード ]
				wavBuf = new byte[ dlen ];
				xastreamheader = new XASTREAMHEADER();

				unsafe
				{
					fixed ( byte* pXaBuf = xaBuf, pWavBuf = wavBuf )
					{
						xastreamheader.pSrc = pXaBuf;
						xastreamheader.nSrcLen = xaheader.nDataLen;
						xastreamheader.nSrcUsed = 0;
						xastreamheader.pDst = pWavBuf;
						xastreamheader.nDstLen = dlen;
						xastreamheader.nDstUsed = 0;
						bool b = xaDecodeConvert( hxas, ref xastreamheader );
						if ( !b )
						{
						    Trace.TraceError( "Error: xaDecodeConvert(): " + Path.GetFileName( filename ) );
							wavBuf = null;
							return false;
						}
					}
				}
				#region [ デバッグ表示 ]
				//Debug.WriteLine( "**XASTREAMHEADER**" );
				//Debug.WriteLine( "nSrcLen=  " + xastreamheader.nSrcLen );
				//Debug.WriteLine( "nSrcUsed= " + xastreamheader.nSrcUsed );
				//Debug.WriteLine( "nDstLen=  " + xastreamheader.nDstLen );
				//Debug.WriteLine( "nDstUsed= " + xastreamheader.nDstUsed );
				#endregion
				#endregion

				#region [ xaファイルのクローズ ]
				bool bb = xaDecodeClose( hxas );
				if ( !bb )
				{
					Trace.TraceError( "Error: xaDecodeClose(): " + Path.GetFileName( filename ) );
				}
				#endregion
			}

			return true;
		}
#endif
	}
}
