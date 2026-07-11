namespace OpenTaiko {
	/// <summary>
	/// Lua-accessible wrapper around <see cref="CTextInput"/>.
	/// Call <see cref="Update"/> once per frame inside your <c>update()</c> function; it returns
	/// <c>true</c> when the user presses Enter to confirm.
	/// <para>
	/// In Debug builds an ImGui overlay shows the text field.
	/// In Release builds the overlay is hidden, so draw <see cref="DisplayText"/> in your own UI.
	/// </para>
	/// </summary>
	public class LuaTextInput : IDisposable {
		private readonly CTextInput _input;

		public LuaTextInput(string initialText, int maxLength) {
			_input = new CTextInput(initialText, (uint)Math.Max(1, maxLength));
		}

		/// <summary>Processes keyboard input. Returns <c>true</c> when the user presses Enter.</summary>
		public bool Update() => _input.Update();

		/// <summary>
		/// iOS only: true on the frame the native text alert was cancelled / confirmed. Check these to
		/// leave text-input state on iOS, where there is no Enter/Escape key to confirm or back out.
		/// </summary>
		[NLua.LuaHide]
		public bool iOSCancelled => _input.iOSCancelled;
		[NLua.LuaHide]
		public bool iOSConfirmed => _input.iOSConfirmed;

		/// <summary>The current text value. Can be read and set at any time.</summary>
		public string Text {
			get => _input.Text;
			set => _input.Text = value ?? "";
		}

		/// <summary>The current text with a blinking cursor appended, suitable for on-screen display.</summary>
		public string DisplayText => _input.DisplayText;

		public void Dispose() { }
	}
}
