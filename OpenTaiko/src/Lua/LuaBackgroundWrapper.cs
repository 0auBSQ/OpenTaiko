using FDK;

namespace OpenTaiko;

/// <summary>
/// Per-frame game state handed to a background module's <c>activate</c>/<c>update</c>/<c>draw</c> functions.
/// Replaces the old <c>BGScriptAPI.lua</c> global prelude + <c>setConstValues</c>/<c>updateValues</c> push.
///
/// The array fields (<see cref="isClear"/>/<see cref="gauge"/>/<see cref="bpm"/>/<see cref="gogo"/>) deliberately
/// SHARE the live game arrays, so Lua indexes them 0-based exactly as the old scripts did (e.g. <c>state.isClear[player]</c>
/// with a 0-based <c>player</c> — NLua exposes a CLR array with 0-based indexing, unlike a Lua table). A single instance
/// is reused per host and mutated each frame (no per-frame table marshalling). <c>deltaTime</c>/<c>fps</c> are NOT here —
/// scripts read the existing <c>fps</c> global (<c>fps.deltaTime</c>/<c>fps.NowFPS</c>), as the other ROActivities do.
/// </summary>
public sealed class LuaBackgroundState {
	// Const-ish — filled once on entry by RefreshConst().
	public int playerCount;
	public bool p1IsBlue;
	public string lang = "";
	public bool simplemode;
	public string[] puchicharaRarities = { "Common", "Common", "Common", "Common", "Common" };
	public string[] characterRarities = { "Common", "Common", "Common", "Common", "Common" };

	// Per-frame gameplay — filled by RefreshGameplay() (gameplay hosts only; stage backgrounds leave defaults).
	public bool[] isClear = new bool[5];
	public double towerNightNum;
	public int battleState;
	public bool battleWin;
	public double[] gauge = new double[5];
	public double[] bpm = new double[5];
	public bool[] gogo = new bool[5];
	public double timeStamp = -1.0;
	public bool paused;
	public int player;   // set by per-player hosts (clear animations) before Update/Draw; ignored by whole-screen bgs

	/// <summary>Fill the const-ish values from config + the current character/puchichara rarities (mirrors the old
	/// <c>setConstValues</c> push in <c>ScriptBG.Init</c>). Call once when the host activates.</summary>
	public void RefreshConst() {
		playerCount = OpenTaiko.ConfigIni.nPlayerCount;
		p1IsBlue = OpenTaiko.P1IsBlue();
		lang = OpenTaiko.ConfigIni.sLang;
		simplemode = OpenTaiko.ConfigIni.SimpleMode;

		if (OpenTaiko.Tx.Puchichara != null && OpenTaiko.Tx.Characters != null) {
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount && i < 5; i++) {
				puchicharaRarities[i] = OpenTaiko.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(i)].metadata.Rarity;
				characterRarities[i] = OpenTaiko.Tx.Characters[OpenTaiko.SaveFileInstances[i].data.Character].metadata.Rarity;
			}
		}
	}

	/// <summary>RefreshConst() then return this, for a one-liner at the host's activate call site.</summary>
	public LuaBackgroundState Refreshed() { RefreshConst(); return this; }

	/// <summary>Fill the per-frame gameplay values from the live play screen (mirrors the old <c>updateValues</c> push
	/// in <c>ScriptBG.Update</c>, including the Tower night factor + the TJA-time-synced timestamp). Safe to call only
	/// during gameplay; no-ops when there is no play screen.</summary>
	public void RefreshGameplay() {
		var gs = OpenTaiko.stageGameScreen;
		if (gs == null) return;

		isClear = gs.bIsAlreadyCleared;
		battleState = gs.AIBattleState;
		battleWin = gs.bIsAIBattleWin;
		gauge = gs.actGauge.dbCurrentGaugeValue;
		bpm = gs.actPlayInfo.dbBPM;
		gogo = gs.bIsGOGOTIME;
		paused = gs.bPAUSE;

		// Tower day→night factor (0..1 over the first half of the climb), as computed in ScriptBG.Update.
		float towerNight = 0;
		if (OpenTaiko.SongMount.rChoosenSong != null && OpenTaiko.SongMount.rChoosenSong.score[5] != null) {
			int maxFloor = OpenTaiko.SongMount.rChoosenSong.score[5].ChartInfo.nTotalFloor;
			int nightTime = Math.Max(140, maxFloor / 2);
			towerNight = Math.Min(gs.actPlayInfo.NowMeasure[0] / (float)nightTime, 1f);
		}
		towerNightNum = towerNight;

		// Chart-synced timestamp in seconds (Dan uses DELAY rather than OFFSET, so its sync can't be perfect).
		double ts = -1.0;
		if (OpenTaiko.TJA != null) {
			double msTimeOffset = OpenTaiko.SongMount.nChoosenSongDifficulty[0] != (int)Difficulty.Dan ? 0 : -CTja.msDanNextSongDelay;
			ts = (OpenTaiko.TJA.RawTjaTimeToDefTime(
					OpenTaiko.TJA.TjaTimeToRawTjaTimeNote(
						OpenTaiko.TJA.GameTimeToTjaTime(SoundManager.PlayTimer.NowTimeMs))
				) + msTimeOffset) / 1000.0;
		}
		timeStamp = ts;
	}
}

/// <summary>
/// Hosts ONE background module (a <c>Script.lua</c> rewritten to the ROActivity LuaTexture API) as a per-instance
/// Lua VM, the replacement for the old <c>ScriptBG</c>. Unlike <see cref="LuaROActivityWrapper"/> (a name-keyed
/// singleton in a static registry), a background is created per host on stage entry and disposed on exit, so Up+Down
/// (+ Mob, clear-anims, kusudama…) can be live simultaneously and a random/preset variant is chosen per play.
///
/// It wraps a <see cref="CLuaROActivityScript"/> (read-only CONFIG/SaveFile + the full modern API + the onStart
/// coroutine). <see cref="Activate"/> drives <c>onStart</c> to completion synchronously (textures load behind the
/// loading bar via <c>TEXTURE:CreateTextureSync</c>, exactly as <c>ScriptBG.Init</c> loaded them), then calls the Lua
/// <c>activate(state)</c>. Per-frame <c>Update</c>/<c>Draw</c> reuse the script's pre-loaded function handles (no
/// per-call allocation); the rare event hooks (clearIn/playEndAnime/kusu*/skipAnime) go through <see cref="Call"/>.
/// </summary>
public sealed class LuaBackgroundWrapper : IDisposable {
	private CLuaBackgroundScript? _script;
	private bool _started;   // onStart is driven once; re-Activate (e.g. per-song clear-anims) only re-runs activate(state)

	/// <summary>True when a Script.lua was found + loaded (false ⇒ every call no-ops, like a missing ScriptBG).</summary>
	public bool Exists => _script != null;

	public LuaBackgroundWrapper(string dir) => Load(dir);

	/// <summary>Fallback list: load the first directory that has a Script.lua (mirrors ScriptBG's params ctor — e.g.
	/// Dan_Gold → Dan_Red → generic clear animation).</summary>
	public LuaBackgroundWrapper(params string[] dirs) {
		foreach (var dir in dirs) {
			Load(dir);
			if (Exists) return;
		}
	}

	private void Load(string dir) {
		if (string.IsNullOrEmpty(dir) || !File.Exists(Path.Combine(dir, "Script.lua"))) return;
		string name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		_script = new CLuaBackgroundScript(dir, string.IsNullOrEmpty(name) ? "background" : name);
	}

	/// <summary>Drive onStart to completion ONCE (synchronous load, behind the loading bar), then call
	/// <c>activate(state)</c>. Re-calling only re-runs activate (so a persistent per-song host like the clear
	/// animations re-inits without reloading/duplicating its textures).</summary>
	public void Activate(LuaBackgroundState state) {
		if (_script == null) return;
		if (!_started) {
			_started = true;
			_script.BeginOnStart();
			while (_script.StepOnStart(out _)) { }   // run the load coroutine straight through — same as ScriptBG's sync Init
		}
		_script.Activate(state);
	}

	/// <summary>Lua <c>update(timestamp, state)</c> — timestamp is the chart-synced time in ms (also in state.timeStamp).</summary>
	public void Update(LuaBackgroundState state) => _script?.UpdateState((long)(state.timeStamp * 1000.0), state);

	/// <summary>Lua <c>draw(state)</c>.</summary>
	public void Draw(LuaBackgroundState state) => _script?.Draw(state);

	/// <summary>Call an optional event hook (clearIn/clearOut/playEndAnime/kusuIn/kusuBroke/kusuMiss/skipAnime). No-ops
	/// silently when the script does not define it. Event-rate only — do not call per frame.</summary>
	public object[]? Call(string functionName, params object[] args) => _script?.CallFunction(functionName, args);

	public void Dispose() {
		_script?.Dispose();   // CLuaScript.Dispose frees all textures/sounds AND removes itself from listScripts
		_script = null;
	}
}
