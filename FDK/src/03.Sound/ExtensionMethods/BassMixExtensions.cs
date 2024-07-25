using ManagedBass;
using ManagedBass.Mix;

namespace FDK.BassMixExtension {
	public static class BassMixExtensions {
		public static bool ChannelPlay(int hHandle) {
			return BassMix.ChannelRemoveFlag(hHandle, BassFlags.MixerChanPause);
		}

		public static bool ChannelPause(int hHandle) {
			return BassMix.ChannelAddFlag(hHandle, BassFlags.MixerChanPause);
		}

		public static bool ChannelIsPlaying(int hHandle) {
			return !BassMix.ChannelHasFlag(hHandle, BassFlags.MixerChanPause);
		}
	}
}
