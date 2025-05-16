using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;
using ImGuiNET;

namespace OpenTaiko {
	public class CTextInput {
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
			ImGui.SetNextWindowSize(new(300,150));

			ImGui.Begin("Text Input", ImGuiWindowFlags.NoResize);
			ImGui.SetKeyboardFocusHere();
			if (ImGui.InputText("text :)", ref Text, MaxLength, ImGuiInputTextFlags.EnterReturnsTrue)) return true;
			ImGui.Text("This window is only visible in Debug mode.\nThis will never show on a Release build.\nDon't run multiple CInputText at once!");

			ImGui.End();

			return false;
		}

		public string Text = "";
		/// <summary>
		/// Used to display the current text with a blinking cursor, to imitate an input text box. For actual text, use <seealso cref="Text"/>.
		/// </summary>
		public string DisplayText {
			get
			{
				return Text + (OpenTaiko.Timer.SystemTimeMs % 1000 >= 300 ? "|" : "");
			}
		}
		/// <summary>
		/// Length in bytes, not char count.
		/// </summary>
		public uint MaxLength = 64;
	}
}
