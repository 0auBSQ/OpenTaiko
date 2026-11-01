using Silk.NET.OpenGLES;

namespace FDK;

public enum BlendType {
	Normal,
	Add,
	Screen,
	Multi,
	Sub
}

/// <summary>A blend state: the equation, then the source and destination factors for the colour and for the alpha.</summary>
public readonly record struct BlendFactors(BlendEquationModeEXT Equation, GLEnum SrcRgb, GLEnum DstRgb, GLEnum SrcAlpha, GLEnum DstAlpha);

public static class BlendHelper {
	/// <summary>The blend for a source whose colour is not multiplied by its alpha (the other shaders).</summary>
	public static BlendFactors Straight(BlendType blendType) => blendType switch {
		BlendType.Add => Same(BlendEquationModeEXT.FuncAdd, GLEnum.SrcAlpha, GLEnum.One),
		BlendType.Screen => Same(BlendEquationModeEXT.FuncAdd, GLEnum.One, GLEnum.OneMinusSrcColor),
		BlendType.Multi => Same(BlendEquationModeEXT.FuncAdd, GLEnum.DstColor, GLEnum.OneMinusSrcAlpha),
		BlendType.Sub => Same(BlendEquationModeEXT.FuncReverseSubtract, GLEnum.SrcAlpha, GLEnum.One),
		_ => Same(BlendEquationModeEXT.FuncAdd, GLEnum.SrcAlpha, GLEnum.OneMinusSrcAlpha),
	};

	/// <summary>The blend for a source whose colour is multiplied by its alpha (the 2D texture shader). Over an opaque
	/// destination Normal, Add and Sub give the colours <see cref="Straight"/> gives, and Screen and Multi fade with
	/// the source's alpha. Normal and Screen add the source's coverage to the destination alpha, the others keep it.</summary>
	public static BlendFactors Premultiplied(BlendType blendType) => blendType switch {
		BlendType.Add => new(BlendEquationModeEXT.FuncAdd, GLEnum.One, GLEnum.One, GLEnum.Zero, GLEnum.One),
		BlendType.Screen => new(BlendEquationModeEXT.FuncAdd, GLEnum.One, GLEnum.OneMinusSrcColor, GLEnum.One, GLEnum.OneMinusSrcAlpha),
		BlendType.Multi => new(BlendEquationModeEXT.FuncAdd, GLEnum.DstColor, GLEnum.OneMinusSrcAlpha, GLEnum.Zero, GLEnum.One),
		BlendType.Sub => new(BlendEquationModeEXT.FuncReverseSubtract, GLEnum.One, GLEnum.One, GLEnum.Zero, GLEnum.One),
		_ => new(BlendEquationModeEXT.FuncAdd, GLEnum.One, GLEnum.OneMinusSrcAlpha, GLEnum.One, GLEnum.OneMinusSrcAlpha),
	};

	private static BlendFactors Same(BlendEquationModeEXT equation, GLEnum src, GLEnum dst) => new(equation, src, dst, src, dst);

	public static void SetBlend(BlendType blendType) {
		var b = Straight(blendType);
		Game.Gl.BlendEquation(b.Equation);
		Game.Gl.BlendFunc(b.SrcRgb, b.DstRgb);
	}

	public static void SetPremultipliedBlend(BlendType blendType) {
		var b = Premultiplied(blendType);
		Game.Gl.BlendEquation(b.Equation);
		Game.Gl.BlendFuncSeparate(b.SrcRgb, b.DstRgb, b.SrcAlpha, b.DstAlpha);
	}
}
