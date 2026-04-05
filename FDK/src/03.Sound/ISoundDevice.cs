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

	CSound tCreateSound(string strファイル名, ESoundGroup soundGroup);
	void tCreateSound(string strファイル名, CSound sound);
}
