﻿namespace OpenTaiko {
	class CSavableT<T> where T : new() {
		public virtual string _fn {
			get;
			protected set;
		}
		public void tDBInitSavable() {
			if (!File.Exists(_fn))
				tSaveFile();

			tLoadFile();
		}

		public T data = new T();

		#region [private]

		private void tSaveFile() {
			ConfigManager.SaveConfig(data, _fn);
		}

		private void tLoadFile() {
			data = ConfigManager.GetConfig<T>(_fn);
		}

		#endregion

	}
}
