using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Input;

namespace FDK;

// imgui copy and paste through the window's clipboard (silk's controller leaves them unset)
// imgui calls these on the game thread; the windowing backend allows its clipboard on the window thread only
internal static class ImGuiClipboard {
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate nint GetTextFn(nint userData);
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void SetTextFn(nint userData, nint text);

	// referenced for as long as ImGui may call them
	private static GetTextFn? _get;
	private static SetTextFn? _set;
	private static nint _text;   // utf-8 text handed to ImGui, valid until its next read

	private const int ReadTimeoutMs = 250;   // the window thread answers within a pass; longer only while it shuts down

	public static void Attach(ImGuiIOPtr io, IKeyboard keyboard) {
		_get = _ => {
			string text = Read(keyboard) ?? "";
			if (_text != 0) Marshal.FreeCoTaskMem(_text);
			_text = Marshal.StringToCoTaskMemUTF8(text);
			return _text;
		};
		_set = (_, text) => {
			string value = Marshal.PtrToStringUTF8(text) ?? "";
			Game.RunOnWindowThread(() => keyboard.ClipboardText = value);
		};
		io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_get);
		io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_set);
	}

	// paste waits for the window thread's answer
	private static string? Read(IKeyboard keyboard) {
		string? text = null;
		var done = new ManualResetEventSlim();   // not disposed: a late window thread may still set it
		Game.RunOnWindowThread(() => {
			try { text = keyboard.ClipboardText; }
			finally { done.Set(); }
		});
		return done.Wait(ReadTimeoutMs) ? text : null;
	}
}
