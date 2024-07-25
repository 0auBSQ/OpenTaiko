namespace FDK {
	public interface IInputDevice : IDisposable {
		// プロパティ

		InputDeviceType CurrentType {
			get;
		}
		string GUID {
			get;
		}
		int ID {
			get;
		}
		List<STInputEvent> InputEvents {
			get;
		}


		// メソッドインターフェース

		void Polling(bool bバッファ入力を使用する);
		bool KeyPressed(int nKey);
		bool KeyPressed(List<int> nKey) { return nKey.Any(key => KeyPressed(key)); }
		bool KeyPressing(int nKey);
		bool KeyPressing(List<int> nKey) { return nKey.Any(key => KeyPressing(key)); }
		bool KeyReleased(int nKey);
		bool KeyReleased(List<int> nKey) { return nKey.Any(key => KeyReleased(key)); }
		bool KeyReleasing(int nKey);
		bool KeyReleasing(List<int> nKey) { return nKey.Any(key => KeyReleasing(key)); }
	}
}
