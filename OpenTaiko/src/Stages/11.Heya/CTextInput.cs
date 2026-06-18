using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using ImGuiNET;

namespace OpenTaiko {
	public class CTextInput {
		/// <summary>
		/// Set by the iOS project to provide native text input via UIAlertController.
		/// Parameters: (currentText, maxLength, callback with result string or null if cancelled).
		/// </summary>
		public static Action<string, uint, Action<string?>>? iOSTextInputHandler;

		public CTextInput() { }
		/// <summary>
		/// 
		/// </summary>
		/// <param name="text">Text to start with. Changes as user inputs text.</param>
		/// <param name="max_length">Max length in bytes that the string can be.</param>
		public CTextInput(string text, uint max_length) : base() {
			Text = text;
			MaxLength = max_length;
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
			ImGui.SetKeyboardFocusHere();
			if (ImGui.InputText("text :)", ref Text, MaxLength, ImGuiInputTextFlags.EnterReturnsTrue)) return true;
			ImGui.Text("This window is only visible in Debug mode.\nThis will never show on a Release build.\nDon't run multiple CInputText at once!");

			ImGui.End();

			return false;
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
		/// Used to display the current text with a blinking cursor, to imitate an input text box. For actual text, use <seealso cref="Text"/>.
		/// </summary>
		public string DisplayText {
			get
			{
				if (OperatingSystem.IsIOS() && _iOSAlertShown && !_iOSCompleted)
					return Text; // No blinking cursor while alert is shown
				return Text + (OpenTaiko.Timer.SystemTimeMs % 1000 >= 300 ? "|" : "");
			}
		}
		/// <summary>
		/// Length in bytes, not char count.
		/// </summary>
		public uint MaxLength = 64;
	}
}
