using FDK;

namespace OpenTaiko.Tests;

// SoundManager.DeviceTryOrder: the sound devices tried in turn, always ending at BASS then none
public class SoundDeviceFallbackTests {
	private static ESoundDeviceType[] Order(ESoundDeviceType requested, bool isWindows)
		=> SoundManager.DeviceTryOrder(requested, isWindows).ToArray();

	[Fact]
	public void WindowsKeepsTheRequestedDeviceFirstAndEndsAtBass() {
		Assert.Equal(new[] { ESoundDeviceType.Bass, ESoundDeviceType.ExclusiveWASAPI, ESoundDeviceType.SharedWASAPI, ESoundDeviceType.ASIO, ESoundDeviceType.Unknown },
			Order(ESoundDeviceType.Bass, true));
		Assert.Equal(new[] { ESoundDeviceType.SharedWASAPI, ESoundDeviceType.ASIO, ESoundDeviceType.Bass, ESoundDeviceType.Unknown },
			Order(ESoundDeviceType.SharedWASAPI, true));
		Assert.Equal(new[] { ESoundDeviceType.ASIO, ESoundDeviceType.Bass, ESoundDeviceType.Unknown },
			Order(ESoundDeviceType.ASIO, true));
	}

	[Theory]
	[InlineData(ESoundDeviceType.ExclusiveWASAPI)]
	[InlineData(ESoundDeviceType.SharedWASAPI)]
	[InlineData(ESoundDeviceType.ASIO)]
	[InlineData(ESoundDeviceType.Bass)]
	public void OtherSystemsUseBassForWindowsOnlyDevices(ESoundDeviceType requested) {
		var order = Order(requested, false);
		Assert.Equal(ESoundDeviceType.Bass, order[0]);
		Assert.Equal(ESoundDeviceType.Unknown, order[^1]);
		Assert.DoesNotContain(ESoundDeviceType.ASIO, order[..1]);
	}

	[Fact]
	public void AnUnknownSettingStillTriesBass() {
		Assert.Equal(new[] { ESoundDeviceType.Bass, ESoundDeviceType.Unknown }, Order(ESoundDeviceType.Unknown, true));
		Assert.Equal(new[] { ESoundDeviceType.Bass, ESoundDeviceType.Unknown }, Order((ESoundDeviceType)42, false));
	}
}
