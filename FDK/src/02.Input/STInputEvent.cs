using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FDK {
	// 構造体

	[StructLayout(LayoutKind.Sequential)]
	public struct STInputEvent {
		public int nKey { get; set; }
		public bool Pressed { get; set; }
		public bool Released { get; set; }
		public long nTimeStamp { get; set; }
		public int nVelocity { get; set; }
	}
}
