using ManagedBass;

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
	// a stream fed from code through a stream procedure (a video's audio track)
	CSound tCreateUserSound(int frequency, int channels, double durationSeconds, StreamProcedure proc, ESoundGroup soundGroup);
}
