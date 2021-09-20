using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace FDK
{
	using HRESULT = Int32;
	using BOOL = Int32;

	[ComImport, Guid( "CE3CE3EE-5C4E-4BDC-A467-C068E1FC3DA5" )]
	public class MemoryRenderer		// 何も継承してはならない。
	{
		// 何も記述してはならない。
		// 代わりに、MemoryRenderer の生成後、キャストで↓のインターフェースを取得する。
	}

	[ComImport, Guid( "FFAA4A1A-D63D-4688-9C66-D18CA7B99488" ), InterfaceType( ComInterfaceType.InterfaceIsIUnknown )]
	public interface IMemoryRenderer
	{
		[PreserveSig]
		HRESULT GetWidth( out long nWidht );
		
		[PreserveSig]
		HRESULT GetHeight( out long nHeight );

		[PreserveSig]
		HRESULT GetBufferSize( out long nBufferSize );

		[PreserveSig]
		HRESULT GetCurrentBuffer( IntPtr pBuffer, long nBufferSize );

		[PreserveSig]
		HRESULT IsBottomUp( out BOOL bBottomUp );
	}
}
