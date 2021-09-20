using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace FDK
{
	// referenced from http://dalmore.blog7.fc2.com/blog-entry-34.html

	public static class CTaskBar
	{
		public static void ShowTaskBar( bool bShowTaskBar )
		{
			Int32 hWnd1 = FindWindow( "Shell_TrayWnd", null );
			if( hWnd1 != 0 )
			{             //タスクバーの表示
				if ( bShowTaskBar )
				{
					ShowWindow( hWnd1, TASKBAR_SHOW );                             //// タスクバーを常に表示
				}
				else
				{
					ShowWindow( hWnd1, TASKBAR_HIDE );
				}
				APPBARDATA pData = new APPBARDATA();
				pData.cbSize = Marshal.SizeOf( pData );
				pData.hWnd = (IntPtr)hWnd1;
				pData.lParam = (int)ABMsg.ABM_NEW;	//REMOVEにするとオートハイドになる
				//タスクバーにメッセージ送信
				SHAppBarMessage( ABMsg.ABM_SETSTATE, ref pData );
			}

			Int32 hWnd2 = FindWindow( "Button", "スタート" );
			if ( hWnd2 != 0 )
			{             //タスクバーの表示
				if ( bShowTaskBar )
				{
					ShowWindow( hWnd2, TASKBAR_SHOW );                             //// タスクバーを常に表示
				}
				else
				{
					ShowWindow( hWnd2, TASKBAR_HIDE );
				}
				APPBARDATA pData = new APPBARDATA();
				pData.cbSize = Marshal.SizeOf( pData );
				pData.hWnd = (IntPtr) hWnd2;
				pData.lParam = (int) ABMsg.ABM_NEW;	//REMOVEにするとオートハイドになる
				//タスクバーにメッセージ送信
				SHAppBarMessage( ABMsg.ABM_SETSTATE, ref pData );
			}
		}

		/// <summary>
		/// ABMsg 送るAppBarメッセージの識別子（以下のいずれか1つ）
		/// _ABM_ACTIVATE---AppBarがアクティブになった事をシステムに通知
		/// _ABM_GETAUTOHIDEBAR---スクリーンの特定の端に関連付けられているオートハイドAppBarのハンドルを返す
		/// _ABM_GETSTATE---タスクバーがオートハイドか常に最前面のどちらの常態にあるかを返す
		/// _ABM_GETTASKBARPOS---タスクバーの使用領域を返す
		/// _ABM_NEW---新しいAppBarを登録し、システムが通知に使用するメッセージIDを指定する
		/// _ABM_QUERYPOS---AppBarのためのサイズとスクリーン位置を要求する
		/// _ABM_REMOVE---AppBarの登録を削除する
		/// _ABM_SETAUTOHIDEBAR---スクリーンの端にオートハイドAppBarを登録または削除する
		/// _ABM_SETPOS---AppBarのサイズとスクリーン座標を設定する
		/// _ABM_WINDOWPOSCHANGED---AppBarの位置が変更されたことをシステムに通知する
		/// pData： TAppBarData構造体（各フィールドはdwMessageに依存する）
		/// </summary>
		private enum ABMsg : int
		{
			ABM_NEW = 0,
			ABM_REMOVE = 1,
			ABM_QUERYPOS = 2,
			ABM_SETPOS = 3,
			ABM_GETSTATE = 4,
			ABM_GETTASKBARPOS = 5,
			ABM_ACTIVATE = 6,
			ABM_GETAUTOHIDEBAR = 7,
			ABM_SETAUTOHIDEBAR = 8,
			ABM_WINDOWPOSCHANGED = 9,
			ABM_SETSTATE = 10
		}

		/// <summary>
		/// APPBARDATA SHAppBarMessage関数にて使用されるAppBarに関する構造体。
		/// cbSize.....SizeOf(TAppBarData)
		/// hWnd.....AppBarのハンドル
		/// uCallbackMessage.....任意のメッセージID（hWndのAppBarにメッセージを通知する際（ABM_NEWメッセージを送る際）に使用）
		/// uEdge.....スクリーンの端を指定するフラグ（ABM_GETAUTOHIDEBAR、ABM_QUERYPOS、ABM_SETAUTOHIDEBAR、ABM_SETPOSメッセージを送る際に使用し、以下のいずれか1つ）
		/// _ABE_BOTTOM---下サイド
		/// _ABE_LEFT--- 左サイド
		/// _ABE_RIGHT---右サイド
		/// _ABE_TOP---上サイド
		/// rc.....AppBarやタスクバーのスクリーン座標での表示領域（ABM_GETTASKBARPOS、ABM_QUERYPOS、ABM_SETPOSメッセージを送る際に使用する）
		/// lParam.....メッセージ依存のパラメータ（ABM_SETAUTOHIDEBARメッセージと共に使用される）
		/// </summary>
		[StructLayout( LayoutKind.Sequential )]
		private struct APPBARDATA
		{
			public int cbSize;
			public IntPtr hWnd;
			public uint uCallbackMessage;
			public ABEdge uEdge;
			public RECT rc;
			public int lParam;
		}
		/// <summary>
		/// ABEdge
		/// </summary>
		private enum ABEdge : int
		{
			ABE_LEFT = 0,
			ABE_TOP = 1,
			ABE_RIGHT = 2,
			ABE_BOTTOM = 3
		}
		/// <summary>
		/// RECT
		/// </summary>
		[StructLayout( LayoutKind.Sequential )]
		private struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}
		/// <summary>
		/// SHAppBarMessage
		/// </summary>
		/// <param name="dwMessage"></param>
		/// <param name="pData"></param>
		/// <returns></returns>
		[DllImport( "shell32.dll", CallingConvention = CallingConvention.StdCall )]
		private static extern int SHAppBarMessage( ABMsg dwMessage, ref APPBARDATA pData );

		[DllImport("user32.dll", EntryPoint = "ShowWindow")]
		private static extern int ShowWindow(Int32 hWnd, int nCmdShow);
		private const int TASKBAR_HIDE = 0;
		private const int TASKBAR_SHOW = 5;

		[DllImport( "user32.dll", EntryPoint = "FindWindow" )]
		private static extern Int32 FindWindow( String lpClassName, String lpWindowName );
	}
}
