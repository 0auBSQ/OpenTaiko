using ImGuiNET;

namespace LWGFW.Graphics;

public class ImGUIManager : IDisposable {
	private nint Context;

	public ImGUIManager() {
		Context = ImGui.CreateContext();
		ImGui.SetCurrentContext(Context);

		ImGuiIOPtr imguiIO = ImGui.GetIO();

		imguiIO.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
		imguiIO.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;

		ImGui.StyleColorsDark();
	}

	public void Dispose() {
		ImGui.DestroyContext(Context);
	}
}
