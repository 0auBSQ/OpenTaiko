using System;
using System.Collections.Generic;
using System.Text;

namespace FDK
{
	public interface IInputDevice : IDisposable
	{
		// プロパティ

		InputDeviceType CurrentType
		{
			get;
		}
		string GUID 
		{
			get; 
		}
		int ID 
		{
			get;
		}
		List<STInputEvent> InputEvents
		{
			get;
		}


		// メソッドインターフェース

		void Polling( bool bバッファ入力を使用する );
		bool KeyPressed( int nKey );
		bool KeyPressing( int nKey );
		bool KeyReleased( int nKey );
		bool KeyReleasing( int nKey );
	}
}
