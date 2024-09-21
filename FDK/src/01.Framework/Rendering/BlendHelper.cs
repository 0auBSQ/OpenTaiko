using Silk.NET.OpenGLES;

namespace SampleFramework;

public enum BlendType {
	Normal,
	Add,
	Screen,
	Multi,
	Sub
}

public static class BlendHelper {
	public static void SetBlend(BlendType blendType) {
		switch (blendType) {
			case BlendType.Normal:
				Game.Gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
				Game.Gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha);
				break;
			case BlendType.Add:
				Game.Gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
				Game.Gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
				break;
			case BlendType.Screen:
				Game.Gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
				Game.Gl.BlendFunc(GLEnum.One, GLEnum.OneMinusSrcColor);
				break;
			case BlendType.Multi:
				Game.Gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
				Game.Gl.BlendFunc(GLEnum.DstColor, GLEnum.OneMinusSrcAlpha);
				break;
			case BlendType.Sub:
				Game.Gl.BlendEquation(BlendEquationModeEXT.FuncReverseSubtract);
				Game.Gl.BlendFunc(GLEnum.SrcAlpha, GLEnum.One);
				break;
		}
	}
}
