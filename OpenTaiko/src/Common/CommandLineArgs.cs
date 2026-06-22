using System;
using System.Linq;

namespace OpenTaiko;

/// <summary>Which mode the process runs in, selected by <c>--mode=…</c>.</summary>
internal enum AppMode {
	Normal,    // play the game (default)
	Record,    // --mode=record : offline gameplay-video export (see VideoExporter)
	CheckGl,   // --mode=checkgl : boot the window + GL context, report, and quit (Linux/Wayland test)
}

/// <summary>
/// Parses OpenTaiko's OWN command-line options — the app mode plus the recorder's settings — into one
/// typed object. FDK-level windowing/platform flags (<c>-w</c>/<c>-p</c>/<c>-f</c>) are parsed separately
/// inside <see cref="FDK.Game"/>; this only owns the app-level switches.
///
/// Forms accepted for every option: <c>--name value</c> and <c>--name=value</c> (case-insensitive).
///   --mode=record  --uid &lt;id&gt;  [--difficulties 3,4] [--fps 60] [--size 1920x1080] [--out file.mp4]
///   --mode=checkgl
/// </summary>
internal sealed class CommandLineArgs {
	public AppMode Mode = AppMode.Normal;

	// --mode=record options (unused in other modes)
	public string Uid = "";
	public int[] Difficulties = Array.Empty<int>();
	public int Fps = 60;
	public int Width = 1920, Height = 1080;
	public string OutPath = "";
	public string? DifficultiesError;   // set when --difficulties was malformed (surfaced by the consumer)

	public static CommandLineArgs Parse(string[] args) {
		var cli = new CommandLineArgs();
		cli.Mode = (Get(args, "--mode") ?? "").ToLowerInvariant() switch {
			"record" => AppMode.Record,
			"checkgl" => AppMode.CheckGl,
			_ => AppMode.Normal,
		};
		if (cli.Mode != AppMode.Record) return cli;

		cli.Uid = (Get(args, "--uid") ?? "").Trim();

		string diffs = Get(args, "--difficulties") ?? Get(args, "--diff") ?? "3";
		try {
			cli.Difficulties = diffs.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(int.Parse).ToArray();
		} catch { cli.Difficulties = Array.Empty<int>(); }
		if (cli.Difficulties.Length < 1 || cli.Difficulties.Length > 5 || cli.Difficulties.Any(d => d < 0 || d > 4))
			cli.DifficultiesError = "--difficulties must be 1 to 5 comma-separated values between 0 and 4.";

		if (int.TryParse(Get(args, "--fps"), out int fps) && fps >= 10 && fps <= 240) cli.Fps = fps;

		string? size = Get(args, "--size");
		if (size != null) {
			var p = size.ToLowerInvariant().Split('x');
			if (p.Length == 2 && int.TryParse(p[0], out int w) && int.TryParse(p[1], out int h) && w > 0 && h > 0) {
				cli.Width = w; cli.Height = h;
			}
		}

		cli.OutPath = Get(args, "--out") ?? $"export_{cli.Uid}_{string.Join("-", cli.Difficulties)}.mp4";
		return cli;
	}

	/// <summary>Value for <c>--name value</c> or <c>--name=value</c> (case-insensitive); null if absent.</summary>
	private static string? Get(string[] args, string name) {
		for (int i = 0; i < args.Length; i++) {
			if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
				return i + 1 < args.Length ? args[i + 1] : "";
			if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
				return args[i].Substring(name.Length + 1);
		}
		return null;
	}
}
