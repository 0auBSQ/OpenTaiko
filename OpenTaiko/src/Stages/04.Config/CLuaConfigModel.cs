using System;
using System.Collections.Generic;

namespace OpenTaiko;

/// <summary>
/// The whole config schema handed to the Lua <c>config_ui</c> ROActivity on activate. Lua reads
/// <see cref="Options"/> (a <c>List</c> — indexed 0-based from Lua via <c>Options[i]</c> / <c>Options.Count</c>),
/// the category labels, and <see cref="Keys"/> for the key-config sub-view. <see cref="RequestExit"/> /
/// <see cref="PlaySfx"/> let Lua call back into the stage without new wrapper plumbing.
/// </summary>
public sealed class CLuaConfigModel {
	public List<CLuaConfigOption> Options { get; } = new();
	public CLuaKeyConfigService Keys { get; init; } = null!;

	// Lists (not arrays) so Lua reads them uniformly with Options: 0-based [i] + .Count
	public List<string> Categories { get; init; } = new();        // "System","Game","Theme"
	public List<string> CategoryLabels { get; init; } = new();    // localized
	public List<string> CategoryDescs { get; init; } = new();     // localized

	/// <summary>Lua calls this when the user backs out at the top level → stage starts its fade-out + save.</summary>
	public Action RequestExit { get; init; } = () => { };

	/// <summary>Lua-callable SFX hook ("decide"/"cancel"/"move") so the menu reuses the engine sounds.</summary>
	public Action<string> PlaySfx { get; init; } = _ => { };
}
