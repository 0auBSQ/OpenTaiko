using System;

namespace OpenTaiko;

/// <summary>
/// One settings row handed to the Lua <c>config_ui</c> ROActivity. NLua marshals this object so the Lua side
/// reads its public properties (to lay out the row) and calls its public methods (to edit the value). Each
/// mutator runs the <see cref="_apply"/> closure, which writes the matching <c>OpenTaiko.ConfigIni</c> field
/// (or theme DB setting) AND applies any live side effect. Authority stays in C#; saving happens on stage exit.
///
/// <para>NLua notes (kept deliberately simple): <c>Kind</c> is a STRING (not an enum); values come back from Lua
/// as numbers, so mutators take ints and the Lua side floors before calling.</para>
/// </summary>
public sealed class CLuaConfigOption {
	public string Category { get; init; } = "System";   // "System" | "Game" | "Theme"
	public string Section { get; init; } = "";           // sub-section header (free text)
	public string Kind { get; init; } = "Toggle";        // "Toggle" | "Int" | "Choice" | "Action" | "KeyConfig" | "Key"
	public string Name { get; init; } = "";              // localized label
	public string Desc { get; init; } = "";              // localized description / help text

	// --- value state (read by Lua; mutated only through the methods below) ---
	public bool On { get; private set; }                 // Toggle
	public int Value { get; private set; }               // Int (range); double/song-speed use a scaled int + Display()
	public int Min { get; init; }
	public int Max { get; init; }
	public int Step { get; init; } = 1;
	public string[] Choices { get; init; } = Array.Empty<string>();  // Choice
	public int Index { get; private set; }               // Choice selected index
	public int ChoiceCount => Choices.Length;            // convenience for the Lua side (avoids #array)

	/// <summary>Optional per-choice preview image paths (parallel to <see cref="Choices"/>). The skin chooser fills
	/// this so the Lua preview pane can show the selected skin's thumbnail; empty for every other option.</summary>
	public string[] Thumbnails { get; init; } = Array.Empty<string>();

	// For KeyConfig rows: which key part/group the "Configure ›" button opens.
	public string KeyPart { get; init; } = "";           // "Taiko" | "System"
	public string KeyGroup { get; init; } = "";          // optional sub-group filter

	/// <summary>Int rows only: render a number-only TEXT INPUT box (type the value) instead of a slider — for
	/// values like a port where dragging a 0..65535 slider is useless. Still uses Value/Min/Max/SetValue.</summary>
	public bool TextInput { get; init; }

	/// <summary>Key rows: the bound keyboard key as a SlimDXKeys.Key name ("F", "Space", "LeftControl"); "" = unbound.
	/// The Lua side captures a key through the key-config service and calls <see cref="SetKey"/>.</summary>
	public string KeyName { get; private set; } = "";

	/// <summary>Human-readable current value ("ON"/"OFF" handled Lua-side; here: choice label, int, scaled double…).</summary>
	public string Display() => _display(this);

	// --- mutators (called from Lua) ---
	public void Toggle() { On = !On; _apply(); }
	public void SetOn(bool v) { if (On == v) return; On = v; _apply(); }
	public void Add(int delta) { SetValue(Value + delta * Step); }
	public void SetValue(int v) {
		int nv = Math.Clamp(v, Min, Max);
		if (nv == Value) return;
		Value = nv; _apply();
	}
	public void SetIndex(int i) {
		int ni = Choices.Length == 0 ? 0 : Math.Clamp(i, 0, Choices.Length - 1);
		if (ni == Index) return;
		Index = ni; _apply();
	}
	public void Activate() { _action?.Invoke(_actionDone); } // Action / KeyConfig rows
	public void SetKey(string keyName) {
		keyName ??= "";
		if (keyName == KeyName) return;
		KeyName = keyName; _apply();
	}
	/// <summary>Drops the binding without running the apply hook (another Key row took this key).</summary>
	internal void ClearKeySilently() { KeyName = ""; }

	// --- C#-only wiring (never touched from Lua) ---
	internal Action _apply = () => { };                  // write config + live side effect
	internal Action<Action?>? _action;                   // Kind == "Action" / "KeyConfig"
	internal Action? _actionDone;                        // Kind == "Action" / "KeyConfig"
	internal Func<CLuaConfigOption, string> _display = o => o.Value.ToString();

	// --- builders used by CConfigOptionBuilder ---
	internal static CLuaConfigOption Toggle_(string cat, string sec, string name, string desc, bool cur, Action<bool> apply) {
		var o = new CLuaConfigOption { Category = cat, Section = sec, Kind = "Toggle", Name = name, Desc = desc };
		o.On = cur;
		o._apply = () => apply(o.On);
		o._display = x => x.On ? "ON" : "OFF";
		return o;
	}
	internal static CLuaConfigOption Int_(string cat, string sec, string name, string desc, int cur, int min, int max, int step, Action<int> apply, Func<CLuaConfigOption, string>? display = null) {
		var o = new CLuaConfigOption { Category = cat, Section = sec, Kind = "Int", Name = name, Desc = desc, Min = min, Max = max, Step = step };
		o.Value = Math.Clamp(cur, min, max);
		o._apply = () => apply(o.Value);
		if (display != null) o._display = display;
		return o;
	}
	/// <summary>An Int that renders as a number-only text input box (type the value) instead of a slider.</summary>
	internal static CLuaConfigOption IntInput_(string cat, string sec, string name, string desc, int cur, int min, int max, Action<int> apply) {
		var o = new CLuaConfigOption { Category = cat, Section = sec, Kind = "Int", Name = name, Desc = desc, Min = min, Max = max, Step = 1, TextInput = true };
		o.Value = Math.Clamp(cur, min, max);
		o._apply = () => apply(o.Value);
		return o;
	}
	internal static CLuaConfigOption Choice_(string cat, string sec, string name, string desc, string[] choices, int cur, Action<int> apply, string[]? thumbnails = null) {
		var o = new CLuaConfigOption { Category = cat, Section = sec, Kind = "Choice", Name = name, Desc = desc, Choices = choices, Thumbnails = thumbnails ?? Array.Empty<string>() };
		o.Index = (choices.Length == 0) ? 0 : Math.Clamp(cur, 0, choices.Length - 1);
		o._apply = () => apply(o.Index);
		o._display = x => (x.Choices.Length > 0 && x.Index >= 0 && x.Index < x.Choices.Length) ? x.Choices[x.Index] : "";
		return o;
	}
	internal static CLuaConfigOption Action_(string cat, string sec, string name, string desc, Action<Action?> action, Action? actionDone = null) {
		var o = new CLuaConfigOption { Category = cat, Section = sec, Kind = "Action", Name = name, Desc = desc };
		o._action = action;
		o._actionDone = actionDone;
		o._display = _ => "";
		return o;
	}
	internal static CLuaConfigOption Action_(string cat, string sec, string name, string desc, Action action)
		=> Action_(cat, sec, name, desc, onDone => action());
	internal static CLuaConfigOption KeyConfig_(string cat, string sec, string name, string desc, string part, string group, Action<Action?> open, Action? close = null) {
		var o = new CLuaConfigOption { Category = cat, Section = sec, Kind = "KeyConfig", Name = name, Desc = desc, KeyPart = part, KeyGroup = group };
		o._action = open;
		o._actionDone = close;
		o._display = _ => "›";
		return o;
	}
	internal static CLuaConfigOption KeyConfig_(string cat, string sec, string name, string desc, string part, string group, Action? open = null)
		=> KeyConfig_(cat, sec, name, desc, part, group, (open == null) ? onClose => { } : onClose => open());
	/// <summary>A single keyboard key (a theme "key" setting): the row shows the key's label and captures a new one.</summary>
	internal static CLuaConfigOption Key_(string cat, string sec, string name, string desc, string cur, Action<string> apply) {
		var o = new CLuaConfigOption { Category = cat, Section = sec, Kind = "Key", Name = name, Desc = desc };
		o.KeyName = cur ?? "";
		o._apply = () => apply(o.KeyName);
		o._display = x => CLuaKeyConfigService.KeyboardLabelOf(x.KeyName);
		return o;
	}
}
