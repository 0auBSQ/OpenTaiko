using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace FDK
{
	public static class CpuCores
	{
		[DllImport("kernel32")]
		public static extern void GetSystemInfo(ref SYSTEM_INFO ptmpsi); 
		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_INFO {
			public uint dwOemId;
			public uint dwPageSize;
			public uint lpMinimumApplicationAddress;
			public uint lpMaximumApplicationAddress;
			public uint dwActiveProcessorMask;
			public uint dwNumberOfProcessors;
			public uint dwProcessorType;
			public uint dwAllocationGranularity;
			public uint dwProcessorLevel;
			public uint dwProcessorRevision;
		}
	}
}
