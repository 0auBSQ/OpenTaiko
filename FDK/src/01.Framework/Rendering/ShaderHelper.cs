using Silk.NET.OpenGLES;

namespace FDK;

public static class ShaderHelper {
	public static uint CreateShader(string code, ShaderType shaderType) {
		uint vertexShader = Game.Gl.CreateShader(shaderType);

		Game.Gl.ShaderSource(vertexShader, code);

		Game.Gl.CompileShader(vertexShader);

		Game.Gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int status);

		if (status != (int)GLEnum.True) {
			string log = Game.Gl.GetShaderInfoLog(vertexShader);
			Game.Gl.DeleteShader(vertexShader);
			throw new Exception($"{shaderType} failed to compile:{log}");
		}

		return vertexShader;
	}

	public static uint CreateShaderProgram(uint vertexShader, uint fragmentShader) {
		uint program = Game.Gl.CreateProgram();

		Game.Gl.AttachShader(program, vertexShader);
		Game.Gl.AttachShader(program, fragmentShader);

		Game.Gl.LinkProgram(program);

		Game.Gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkStatus);

		if (linkStatus != (int)GLEnum.True) {
			string log = Game.Gl.GetProgramInfoLog(program);
			Game.Gl.DeleteProgram(program);
			throw new Exception($"Program failed to link:{log}");
		}

		Game.Gl.DetachShader(program, vertexShader);
		Game.Gl.DetachShader(program, fragmentShader);

		return program;
	}

	public static uint CreateShaderProgramFromSource(string vertexCode, string fragmentCode) {
		uint vertexShader = CreateShader(vertexCode, ShaderType.VertexShader);
		uint fragmentShader = 0;

		// the shaders are freed whether the program builds or not
		try {
			fragmentShader = CreateShader(fragmentCode, ShaderType.FragmentShader);
			return CreateShaderProgram(vertexShader, fragmentShader);
		} finally {
			Game.Gl.DeleteShader(vertexShader);
			if (fragmentShader != 0) Game.Gl.DeleteShader(fragmentShader);
		}
	}

	/// <summary>Compile + link a GLES 3.1 compute shader into a standalone program (the GPU
	/// raytracer's path-tracing kernel). Requires a 3.1+ context (see Game.ComputeShadersAvailable).</summary>
	public static uint CreateComputeProgramFromSource(string computeCode) {
		uint cs = CreateShader(computeCode, ShaderType.ComputeShader);

		uint program = Game.Gl.CreateProgram();
		Game.Gl.AttachShader(program, cs);
		Game.Gl.LinkProgram(program);
		Game.Gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkStatus);
		if (linkStatus != (int)GLEnum.True) {
			string log = Game.Gl.GetProgramInfoLog(program);
			Game.Gl.DeleteProgram(program);
			Game.Gl.DeleteShader(cs);
			throw new Exception($"Compute program failed to link:{log}");
		}

		Game.Gl.DetachShader(program, cs);
		Game.Gl.DeleteShader(cs);
		return program;
	}
}
