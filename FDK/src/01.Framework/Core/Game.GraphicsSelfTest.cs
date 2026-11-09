using System;
using System.Diagnostics;
using Silk.NET.OpenGLES;

namespace FDK;

// Graphics self-test ("--mode=checkgl"): on the first rendered frame, verify the window + ANGLE/GL
// context actually came up — this is the exact path AngleContext exercises, including the Wayland
// wl_egl_window surface — report the driver strings, set the process exit code, and quit. No game
// assets are touched, so it's a clean, scriptable way to verify the Linux/Wayland rendering path.
// Hooked from Game.Window_Render; armed by Program when --mode=checkgl is passed.
public abstract partial class Game {
	/// <summary>Set by the entry point for --mode=checkgl; consumed on the first render frame.</summary>
	public static bool GraphicsSelfTest;
	private bool _selfTestDone;

	private void RunGraphicsSelfTest() {
		if (_selfTestDone) { try { Window_!.Close(); } catch { } return; }
		_selfTestDone = true;
		try {
			Gl.ClearColor(0.10f, 0.20f, 0.35f, 1f);
			Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			string ver = Gl.GetStringS(StringName.Version);
			string rend = Gl.GetStringS(StringName.Renderer);
			string vend = Gl.GetStringS(StringName.Vendor);
			// macOS only: the game-owned NSOpenGLContext can present too; only skip when macOS has no such
			// context (the hosted/iOS path never gets here).
			if (!OperatingSystem.IsMacOS() || _cocoaContext != null) Context.SwapBuffers();
			string line = $"[checkgl] OK  version='{ver}' renderer='{rend}' vendor='{vend}' compute={ComputeShadersAvailable}";
			Trace.TraceInformation(line);
			Console.WriteLine(line);
			Environment.ExitCode = 0;
		} catch (Exception ex) {
			Trace.TraceError("[checkgl] FAILED: " + ex);
			Console.Error.WriteLine("[checkgl] FAILED: " + ex.Message);
			Environment.ExitCode = 70;
		}
		this.Exit();
	}
}
