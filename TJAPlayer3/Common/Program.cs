using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using FDK;
using System.Reflection;

namespace TJAPlayer3
{
	internal class Program
	{
		#region [ 二重起動チェック、DLL存在チェック ]
		//-----------------------------
		private static Mutex mutex二重起動防止用;

		private static bool tDLLの存在チェック( string strDll名, string str存在しないときに表示するエラー文字列jp, string str存在しないときに表示するエラー文字列en, bool bLoadDllCheck )
		{
			string str存在しないときに表示するエラー文字列 = ( CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja" ) ?
				str存在しないときに表示するエラー文字列jp : str存在しないときに表示するエラー文字列en;
			if ( bLoadDllCheck )
			{
				IntPtr hModule = LoadLibrary( strDll名 );		// 実際にLoadDll()してチェックする
				if ( hModule == IntPtr.Zero )
				{
					MessageBox.Show( str存在しないときに表示するエラー文字列, "DTXMania runtime error", MessageBoxButtons.OK, MessageBoxIcon.Hand );
					return false;
				}
				FreeLibrary( hModule );
			}
			else
			{													// 単純にファイルの存在有無をチェックするだけ (プロジェクトで「参照」していたり、アンマネージドなDLLが暗黙リンクされるものはこちら)
				string path = Path.Combine( System.IO.Directory.GetCurrentDirectory(), strDll名 );
				if ( !File.Exists( path ) )
				{
					MessageBox.Show( str存在しないときに表示するエラー文字列, "DTXMania runtime error", MessageBoxButtons.OK, MessageBoxIcon.Hand );
					return false;
				}
			}
			return true;
		}
		private static bool tDLLの存在チェック( string strDll名, string str存在しないときに表示するエラー文字列jp, string str存在しないときに表示するエラー文字列en )
		{
			return true;
			//return tDLLの存在チェック( strDll名, str存在しないときに表示するエラー文字列jp, str存在しないときに表示するエラー文字列en, false );
		}

		#region [DllImport]
		[DllImport( "kernel32", CharSet = CharSet.Unicode, SetLastError = true )]
		internal static extern void FreeLibrary( IntPtr hModule );

		[DllImport( "kernel32", CharSet = CharSet.Unicode, SetLastError = true )]
		internal static extern IntPtr LoadLibrary( string lpFileName );
		#endregion
		//-----------------------------
		#endregion

		[STAThread] 
		static void Main()
		{
			mutex二重起動防止用 = new Mutex( false, "DTXManiaMutex" );

			if ( mutex二重起動防止用.WaitOne( 0, false ) )
			{
				string newLine = Environment.NewLine;
				bool bDLLnotfound = false;

				Trace.WriteLine( "Current Directory: " + Environment.CurrentDirectory );
				Trace.WriteLine( "EXEのあるフォルダ: " + Path.GetDirectoryName( Application.ExecutablePath ) );

                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;


                #region [DLLの存在チェック]
                if ( !tDLLの存在チェック( "dll\\SlimDX" + TJAPlayer3.SLIMDXDLL + ".dll",
					"SlimDX" + TJAPlayer3.SLIMDXDLL + ".dll またはその依存するdllが存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
					"SlimDX" + TJAPlayer3.SLIMDXDLL + ".dll, or its depended DLL, is not found." + newLine + "Please download DTXMania again."
					) ) bDLLnotfound = true;
				if ( !tDLLの存在チェック( "dll\\FDK.dll",
					"FDK.dll またはその依存するdllが存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
					"FDK.dll, or its depended DLL, is not found." + newLine + "Please download DTXMania again."
					) ) bDLLnotfound = true;
				if ( !tDLLの存在チェック( "dll\\xadec.dll",
					"xadec.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
					"xadec.dll is not found." + newLine + "Please download DTXMania again."
					) ) bDLLnotfound = true;
				if ( !tDLLの存在チェック( "dll\\SoundDecoder.dll",
					"SoundDecoder.dll またはその依存するdllが存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
					"SoundDecoder.dll, or its depended DLL, is not found." + newLine + "Please download DTXMania again."
					) ) bDLLnotfound = true;
				if ( !tDLLの存在チェック( TJAPlayer3.D3DXDLL,
					TJAPlayer3.D3DXDLL + " が存在しません。" + newLine + "DirectX Redist フォルダの DXSETUP.exe を実行し、" + newLine + "必要な DirectX ランタイムをインストールしてください。",
					TJAPlayer3.D3DXDLL + " is not found." + newLine + "Please execute DXSETUP.exe in \"DirectX Redist\" folder, to install DirectX runtimes required for DTXMania.",
					true
					) ) bDLLnotfound = true;
				if ( !tDLLの存在チェック( "dll\\bass.dll",
					"bass.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
					"baas.dll is not found." + newLine + "Please download DTXMania again."
					) ) bDLLnotfound = true;
				if ( !tDLLの存在チェック( "dll\\Bass.Net.dll",
					"Bass.Net.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
					"Bass.Net.dll is not found." + newLine + "Please download DTXMania again."
					) ) bDLLnotfound = true;
				if ( !tDLLの存在チェック( "dll\\bassmix.dll",
					"bassmix.dll を読み込めません。bassmix.dll か bass.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
					"bassmix.dll is not loaded. bassmix.dll or bass.dll must not exist." + newLine + "Please download DTXMania again."
					) ) bDLLnotfound = true;
				if ( !tDLLの存在チェック( "dll\\bassasio.dll",
					"bassasio.dll を読み込めません。bassasio.dll か bass.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
					"bassasio.dll is not loaded. bassasio.dll or bass.dll must not exist." + newLine + "Please download DTXMania again."
					) ) bDLLnotfound = true;
				if ( !tDLLの存在チェック( "dll\\basswasapi.dll",
					"basswasapi.dll を読み込めません。basswasapi.dll か bass.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
					"basswasapi.dll is not loaded. basswasapi.dll or bass.dll must not exist." + newLine + "Please download DTXMania again."
					) ) bDLLnotfound = true;
				if ( !tDLLの存在チェック( "dll\\bass_fx.dll",
					"bass_fx.dll を読み込めません。bass_fx.dll か bass.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
					"bass_fx.dll is not loaded. bass_fx.dll or bass.dll must not exist." + newLine + "Please download DTXMania again."
					) ) bDLLnotfound = true;
				if ( !tDLLの存在チェック( "dll\\DirectShowLib-2005.dll",
					"DirectShowLib-2005.dll が存在しません。" + newLine + "DTXManiaをダウンロードしなおしてください。",
					"DirectShowLib-2005.dll is not found." + newLine + "Please download DTXMania again."
					) ) bDLLnotfound = true;
				#endregion
				if ( !bDLLnotfound )
				{
#if DEBUG && TEST_ENGLISH
					Thread.CurrentThread.CurrentCulture = new CultureInfo( "en-US" );
#endif

					DWM.EnableComposition( false );	// Disable AeroGrass temporally

					// BEGIN #23670 2010.11.13 from: キャッチされない例外は放出せずに、ログに詳細を出力する。
					// BEGIM #24606 2011.03.08 from: DEBUG 時は例外発生箇所を直接デバッグできるようにするため、例外をキャッチしないようにする。
#if !DEBUG
					try
#endif
					{
						using ( var mania = new TJAPlayer3() )
							mania.Run();

						Trace.WriteLine( "" );
						Trace.WriteLine( "遊んでくれてありがとう！" );
					}
#if !DEBUG
					catch( Exception e )
					{
						Trace.WriteLine( "" );
						Trace.Write( e.ToString() );
						Trace.WriteLine( "" );
						Trace.WriteLine( "エラーだゴメン！（涙" );
                        AssemblyName asmApp = Assembly.GetExecutingAssembly().GetName();
                        MessageBox.Show( "エラーが発生しました。\n" +
                            e.ToString(), asmApp.Name + " Ver." + asmApp.Version.ToString().Substring(0, asmApp.Version.ToString().Length - 2) + " Error", MessageBoxButtons.OK, MessageBoxIcon.Error );	// #23670 2011.2.28 yyagi to show error dialog
					}
#endif
					// END #24606 2011.03.08 from
					// END #23670 2010.11.13 from

					if ( Trace.Listeners.Count > 1 )
						Trace.Listeners.RemoveAt( 1 );
				}

				// BEGIN #24615 2011.03.09 from: Mutex.WaitOne() が true を返した場合は、Mutex のリリースが必要である。

				mutex二重起動防止用.ReleaseMutex();
				mutex二重起動防止用 = null;

				// END #24615 2011.03.09 from
			}
			else		// DTXManiaが既に起動中
			{
				
				// → 引数が0個の時はそのまま終了
				// 1個( コンパクトモード or DTXV -S) か2個 (DTXV -Nxxx ファイル名)のときは、そのプロセスにコマンドラインを丸々投げて終了する

				for ( int i = 0; i < 5; i++ )		// 検索結果のハンドルがZeroになることがあるので、200ms間隔で5回リトライする
				{
					#region [ 既に起動中のDTXManiaプロセスを検索する。]
					// このやり方だと、ShowInTaskbar=falseでタスクバーに表示されないパターンの時に検索に失敗するようだが
					// DTXManiaでそのパターンはない？のでこのままいく。
					// FindWindowを使えばこのパターンにも対応できるが、C#でビルドするアプリはウインドウクラス名を自前指定できないので、これは使わない。

					Process current = Process.GetCurrentProcess();
					Process[] running = Process.GetProcessesByName( current.ProcessName );
					Process target = null;
					//IntPtr hWnd = FindWindow( null, "DTXMania .NET style release " + CDTXMania.VERSION );

					foreach ( Process p in running )
					{
						if ( p.Id != current.Id )	// プロセス名は同じでかつ、プロセスIDが自分自身とは異なるものを探す
						{
							if ( p.MainModule.FileName == current.MainModule.FileName && p.MainWindowHandle != IntPtr.Zero )
							{
								target = p;
								break;
							}
						}
					}
					#endregion

					#region [ 起動中のDTXManiaがいれば、そのプロセスにコマンドラインを投げる ]
					if ( target != null )
					{
						string[] commandLineArgs = Environment.GetCommandLineArgs();
						if ( commandLineArgs != null && commandLineArgs.Length > 1 )
						{
							string arg = null;
							for ( int j = 1; j < commandLineArgs.Length; j++ )
							{
								if ( j == 1 )
								{
									arg += commandLineArgs[ j ];
								}
								else
								{
									arg += " " + "\"" + commandLineArgs[ j ] + "\"";
								}
							}

//Trace.TraceInformation( "Message=" + arg + ", len(w/o null)=" + arg.Length );

							if ( arg != null )
							{
								FDK.CSendMessage.sendmessage( target.MainWindowHandle, current.MainWindowHandle, arg );
							}
						}
						break;
					}
					#endregion
					else
					{
						Trace.TraceInformation( "メッセージ送信先のプロセスが見つからず。5回リトライします。" );
						Thread.Sleep( 200 );
					}
				}
			}
		}
	}
}
