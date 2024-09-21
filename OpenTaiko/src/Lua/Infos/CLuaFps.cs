namespace OpenTaiko {
	internal class CLuaFps {
		public double deltaTime => OpenTaiko.FPS.DeltaTime;
		public int fps => OpenTaiko.FPS.NowFPS;
	}
}