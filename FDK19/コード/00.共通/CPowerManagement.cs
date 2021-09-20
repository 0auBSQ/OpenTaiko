using System;
using System.Collections.Generic;
using System.Text;

namespace FDK
{
	/// <summary>
	/// システムとモニタの省電力制御を行う
	/// </summary>
	public static class CPowerManagement
	{
		/// <summary>
		/// 本体/モニタの省電力モード移行を抑止する
		/// </summary>
		public static void tDisableMonitorSuspend()
		{
			CWin32.SetThreadExecutionState( CWin32.ExecutionState.SystemRequired | CWin32.ExecutionState.DisplayRequired );
		}

		/// <summary>
		/// 本体/モニタの省電力モード移行抑制を解除する
		/// </summary>
		public static void tEnableMonitorSuspend()
		{
			CWin32.SetThreadExecutionState( CWin32.ExecutionState.Continuous );		// スリープ抑止状態を解除
		}
	}
}
