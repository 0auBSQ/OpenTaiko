namespace FDK;

internal interface ISoundDevice : IDisposable {
	ESoundDeviceType SoundDeviceType { get; }
	int nMasterVolume { get; set; }
	long OutputDelay { get; }
	long BufferSize { get; }
	long ElapsedTimeMs { get; }
	double dbElapsedTimeMs { get; }
	long UpdateSystemTimeMs { get; }
	double dbUpdateSystemTimeMs { get; }
	CTimer SystemTimer { get; }
	long nBytesPerSec { get; }

	CSound tCreateSound(string strFileName, ESoundGroup soundGroup);
	void tCreateSound(string strFileName, CSound sound);
}
