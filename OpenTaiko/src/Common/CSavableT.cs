namespace OpenTaiko;

class CSavableT<T> where T : new() {
	public virtual string _fn {
		get;
		protected set;
	}
	public void tDBInitSavable() {
		// iOS: the app bundle is read-only, so writes go to the Documents copy (ResolveWritePath). Seed
		// it once from any existing/bundled file (ResolveAssetPath) so a shipped default isn't lost.
		string writeFn = OpenTaiko.ResolveWritePath(_fn);
		if (!File.Exists(writeFn)) {
			string readFn = OpenTaiko.ResolveAssetPath(_fn);
			if (readFn != writeFn && File.Exists(readFn))
				data = ConfigManager.GetConfig<T>(readFn);
			tSaveFile();
		}

		tLoadFile();
	}

	public T data = new T();

	#region [private]

	private void tSaveFile() {
		ConfigManager.SaveConfig(data, OpenTaiko.ResolveWritePath(_fn));
	}

	private void tLoadFile() {
		data = ConfigManager.GetConfig<T>(OpenTaiko.ResolveWritePath(_fn));
	}

	#endregion

}
