using System;
using System.IO;
using Xunit;

namespace OpenTaiko.Tests;

// CSkin.ReadSkinVersion: the SkinConfig.ini version that decides whether a skin is offered and loaded
public class SkinVersionTests : IDisposable {
	private readonly string _dir = Path.Combine(Path.GetTempPath(), "otk_skinver_" + Guid.NewGuid().ToString("N"));

	public SkinVersionTests() => Directory.CreateDirectory(_dir);
	public void Dispose() => Directory.Delete(_dir, true);

	private Version? Read(string config) {
		File.WriteAllText(Path.Combine(_dir, "SkinConfig.ini"), config);
		return CSkin.ReadSkinVersion(_dir);
	}

	[Fact]
	public void ReadsTheFullVersion() {
		Assert.Equal(new Version(0, 7, 0, 0), Read(";Skin information\nName=OWM\nVersion=0.7.0.0\n"));
	}

	[Fact]
	public void OldVersionsAreBelowTheMinimum() {
		Assert.True(Read("Version=0.6.0.24\n") < CSkin.MinimumSkinVersion);
		Assert.True(Read("Version=0.6.1.0\n") < CSkin.MinimumSkinVersion);
	}

	[Fact]
	public void MissingPartsReadAsZero() {
		Assert.Equal(new Version(0, 7, 0, 0), Read("Version=0.7\n"));
		Assert.False(Read("Version=0.7\n") < CSkin.MinimumSkinVersion);
		Assert.True(Read("Version=0.8.1\n") > CSkin.MinimumSkinVersion);
	}

	[Fact]
	public void SpacesCrlfAndSuffixesAreAccepted() {
		Assert.Equal(new Version(0, 7, 1, 0), Read("Name=A\r\n Version = 0.7.1.0 \r\n"));
		Assert.Equal(new Version(0, 7, 0, 2), Read("Version=0.7.0.2-beta\n"));
	}

	[Fact]
	public void TheLastVersionLineWins() {
		Assert.Equal(new Version(0, 7, 0, 0), Read("Version=0.6.0.0\nVersion=0.7.0.0\n"));
	}

	[Fact]
	public void NoOrUnreadableVersionIsNull() {
		Assert.Null(Read("Name=A\nCreator=B\n"));
		Assert.Null(Read("Version=Unknown\n"));
		Assert.Null(CSkin.ReadSkinVersion(Path.Combine(_dir, "missing")));
	}
}
