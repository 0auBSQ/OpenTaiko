using System;
using System.Text;
using FDK;
using ImGuiNET;

namespace OpenTaiko {
	/// <summary>
	/// ImGui-backed single-line text field (ImGui handles OS/IME composition + the candidate window). It is driven
	/// once per frame from <see cref="Update"/>, which returns <c>true</c> when the user presses Enter.
	/// <para>The ImGui window is only visible in Debug builds; in Release the host draws <see cref="DisplayText"/>.</para>
	/// </summary>
	public unsafe class CTextInput {
		/// <summary>
		/// Set by the iOS project to provide native text input via UIAlertController.
		/// Parameters: (currentText, maxLength, callback with result string or null if cancelled).
		/// </summary>
		public static Action<string, uint, Action<string?>>? iOSTextInputHandler;

		public CTextInput() { _cb = InputCallback; }
		/// <param name="text">Text to start with. Changes as user inputs text.</param>
		/// <param name="max_length">Max length in bytes that the string can be.</param>
		public CTextInput(string text, uint max_length) {
			Text = text;
			MaxLength = max_length;
			_cb = InputCallback;
		}

		// Kept alive in a field so the native side never calls into a collected delegate.
		private readonly ImGuiInputTextCallback _cb;
		private int _cursorBytePos;       // ImGui's caret, in UTF-8 BYTES (read every frame via the callback)
		private bool _needFocus = true;   // (re)acquire keyboard focus; re-armed whenever focus is lost so it can't soft-lock

		private int InputCallback(ImGuiInputTextCallbackData* data) {
			_cursorBytePos = data->CursorPos;
			return 0;
		}

		/// <summary>
		/// Returns <c>true</c> if the user presses Enter to confirm their text.
		/// </summary>
		public bool Update() {
			if (OperatingSystem.IsIOS()) {
				return UpdateiOS();
			}

			ImGui.SetNextWindowSize(new(300,150));

			ImGui.Begin("Text Input", ImGuiWindowFlags.NoResize);
			// Focus on entry, then re-grab ONLY when the field isn't active (i.e. focus was lost) — NEVER every
			// frame while editing, since re-focusing an active field snaps ImGui's caret back to the end. Because we
			// keep retrying whenever it's inactive, focus can't be permanently lost → no soft-lock.
			if (_needFocus) { ImGui.SetKeyboardFocusHere(); _needFocus = false; }
			bool entered = ImGui.InputText("text :)", ref Text, MaxLength,
				ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.CallbackAlways, _cb);
			// Re-arm focus whenever the field is NOT active — i.e. on focus loss AND after Enter/Escape deactivate it.
			// (Must NOT gate on `!entered`: EnterReturnsTrue deactivates on the same frame entered==true, and this
			// CTextInput instance is reused, so gating on !entered would leave focus un-armed → soft-lock on re-entry.)
			// While actively editing, IsItemActive() is true so this stays false → the caret is never snapped to end.
			if (!ImGui.IsItemActive()) _needFocus = true;
			ImGui.Text("This window is only visible in Debug mode.\nThis will never show on a Release build.\nDon't run multiple CInputText at once!");

			ImGui.End();

			return entered;
		}

		private bool _iOSAlertShown;
		private bool _iOSCompleted;
		private string? _iOSResult;

		/// <summary>
		/// True when the iOS alert was dismissed via Cancel.
		/// Checked by callers to back out of text input state.
		/// Resets on next Update() call.
		/// </summary>
		public bool iOSCancelled { get; private set; }

		private bool UpdateiOS() {
			iOSCancelled = false;

			if (_iOSCompleted) {
				bool confirmed = _iOSResult != null;
				if (confirmed) {
					Text = _iOSResult!;
				} else {
					iOSCancelled = true;
				}
				// Reset for potential reuse
				_iOSAlertShown = false;
				_iOSCompleted = false;
				_iOSResult = null;
				return confirmed;
			}

			if (!_iOSAlertShown && iOSTextInputHandler != null) {
				_iOSAlertShown = true;
				iOSTextInputHandler(Text, MaxLength, result => {
					_iOSResult = result;
					_iOSCompleted = true;
				});
			}

			return false;
		}

		public string Text = "";
		/// <summary>
		/// The current text with a blinking caret inserted at ImGui's real caret position (so Left/Right/Home/End are
		/// visible), to imitate an input text box. For the actual text, use <seealso cref="Text"/>.
		/// </summary>
		public string DisplayText {
			get
			{
				if (OperatingSystem.IsIOS() && _iOSAlertShown && !_iOSCompleted)
					return Text; // No blinking cursor while alert is shown
				if (OpenTaiko.Timer.SystemTimeMs % 1000 < 300) return Text;   // blink off
				int c = ByteToCharIndex(Text, _cursorBytePos);
				return Text.Substring(0, c) + "|" + Text.Substring(c);
			}
		}

		// ImGui reports the caret as a UTF-8 byte offset; convert to a C# (UTF-16) char index so Substring lands
		// correctly even with multibyte characters (CJK room names etc.).
		private static int ByteToCharIndex(string s, int bytePos) {
			if (bytePos <= 0 || s.Length == 0) return 0;
			byte[] b = Encoding.UTF8.GetBytes(s);
			if (bytePos >= b.Length) return s.Length;
			return Encoding.UTF8.GetString(b, 0, bytePos).Length;
		}

		/// <summary>
		/// Length in bytes, not char count.
		/// </summary>
		public uint MaxLength = 64;
	}
}
