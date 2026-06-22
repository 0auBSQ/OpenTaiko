using Silk.NET.OpenGLES;

namespace FDK;

public static class ShaderHelper {
	public static uint CreateShader(string code, ShaderType shaderType) {
		uint vertexShader = Game.Gl.CreateShader(shaderType);

		Game.Gl.ShaderSource(vertexShader, code);

		Game.Gl.CompileShader(vertexShader);

		Game.Gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int status);

		if (status != (int)GLEnum.True)
			throw new Exception($"{shaderType} failed to compile:{Game.Gl.GetShaderInfoLog(vertexShader)}");

		return vertexShader;
	}

	public static uint CreateShaderProgram(uint vertexShader, uint fragmentShader) {
		uint program = Game.Gl.CreateProgram();

		Game.Gl.AttachShader(program, vertexShader);
		Game.Gl.AttachShader(program, fragmentShader);

		Game.Gl.LinkProgram(program);

		Game.Gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkStatus);

		if (linkStatus != (int)GLEnum.True)
			throw new Exception($"Program failed to link:{Game.Gl.GetProgramInfoLog(program)}");

		Game.Gl.DetachShader(program, vertexShader);
		Game.Gl.DetachShader(program, fragmentShader);

		return program;
	}

	public static uint CreateShaderProgramFromSource(string vertexCode, string fragmentCode) {
		uint vertexShader = CreateShader(vertexCode, ShaderType.VertexShader);
		uint fragmentShader = CreateShader(fragmentCode, ShaderType.FragmentShader);

		uint program = CreateShaderProgram(vertexShader, fragmentShader);

		Game.Gl.DeleteShader(vertexShader);
		Game.Gl.DeleteShader(fragmentShader);

		return program;
	}

	/// <summary>Compile + link a GLES 3.1 compute shader into a standalone program (the GPU
	/// raytracer's path-tracing kernel). Requires a 3.1+ context (see Game.ComputeShadersAvailable).</summary>
	public static uint CreateComputeProgramFromSource(string computeCode) {
		uint cs = CreateShader(computeCode, ShaderType.ComputeShader);

		uint program = Game.Gl.CreateProgram();
		Game.Gl.AttachShader(program, cs);
		Game.Gl.LinkProgram(program);
		Game.Gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkStatus);
		if (linkStatus != (int)GLEnum.True)
			throw new Exception($"Compute program failed to link:{Game.Gl.GetProgramInfoLog(program)}");

		Game.Gl.DetachShader(program, cs);
		Game.Gl.DeleteShader(cs);
		return program;
	}
}
