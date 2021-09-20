using System;
using System.Collections.Generic;
using System.Text;

namespace FDK
{
	public enum ESoundDeviceType
	{
		ExclusiveWASAPI,
		SharedWASAPI,
		ASIO,
		DirectSound,
		Unknown,
	}
}
