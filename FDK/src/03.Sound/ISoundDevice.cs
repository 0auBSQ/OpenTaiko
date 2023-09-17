using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace FDK
{
	internal interface ISoundDevice : IDisposable
	{
		ESoundDeviceType SoundDeviceType { get; }
		int nMasterVolume { get; set; }
		long OutputDelay { get; }
		long BufferSize { get; }
		long ElapsedTimeMs { get; }
		long UpdateSystemTimeMs { get; }
		CTimer SystemTimer { get; }

		CSound tCreateSound( string strファイル名, ESoundGroup soundGroup );
		void tCreateSound( string strファイル名, CSound sound );
	}
}
