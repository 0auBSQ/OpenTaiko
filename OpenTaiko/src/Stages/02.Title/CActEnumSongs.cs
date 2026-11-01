using FDK;

namespace OpenTaiko;

// Song-enumeration status overlay. The visuals (icon + progress bar + "loaded / total" text) now live in
// the Lua "song_enum" ROActivity; this class only forwards the enumeration progress to it each frame.
internal class CActEnumSongs : CActivity {
	public bool bCommandSongDataGet;

	/// <summary>
	/// Constructor
	/// </summary>
	public CActEnumSongs() {
		Init(false);
	}

	public CActEnumSongs(bool _bCommandSongDataGet) {
		Init(_bCommandSongDataGet);
	}
	private void Init(bool _bCommandSongDataGet) {
		base.IsDeActivated = true;
		bCommandSongDataGet = _bCommandSongDataGet;
	}

	private static LuaROActivityWrapper? UI => LuaROActivityWrapper.GetROActivity("song_enum");

	// CActivity 実装

	public override void Activate() {
		if (this.IsActivated)
			return;
		base.Activate();
		UI?.Activate();
	}
	public override void DeActivate() {
		if (this.IsDeActivated)
			return;
		UI?.Deactivate();
		base.DeActivate();
	}

	public override int Draw() {
		if (this.IsDeActivated) {
			return 0;
		}
		var songManager = OpenTaiko.EnumSongs?.SongManager;
		int done = songManager?.nSearchFileCount ?? 0;
		int total = songManager?.nTotalSongFilesToSearch ?? 0;
		UI?.Draw(bCommandSongDataGet, done, total);
		return 0;
	}

	public void RefreshSkin(bool isEnumerating) {
		this.DeActivate();
		if (isEnumerating)
			this.Activate();
	}
}
