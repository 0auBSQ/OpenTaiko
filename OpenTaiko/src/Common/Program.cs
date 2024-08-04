using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenTaiko {
	internal class Program {
		#region [ 二重起動チェック、DLL存在チェック ]
		//-----------------------------
		private static Mutex mutex二重起動防止用;

		private static bool tDLLの存在チェック(string strDll名, string str存在しないときに表示するエラー文字列jp, string str存在しないときに表示するエラー文字列en, bool bLoadDllCheck) {
			string str存在しないときに表示するエラー文字列 = (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja") ?
				str存在しないときに表示するエラー文字列jp : str存在しないときに表示するエラー文字列en;
			if (bLoadDllCheck) {
				IntPtr hModule = LoadLibrary(strDll名);      // 実際にLoadDll()してチェックする
				if (hModule == IntPtr.Zero) {
					//MessageBox.Show( str存在しないときに表示するエラー文字列, "DTXMania runtime error", MessageBoxButtons.OK, MessageBoxIcon.Hand );
					return false;
				}
				FreeLibrary(hModule);
			} else {                                                    // 単純にファイルの存在有無をチェックするだけ (プロジェクトで「参照」していたり、アンマネージドなDLLが暗黙リンクされるものはこちら)
				string path = Path.Combine(System.IO.Directory.GetCurrentDirectory(), strDll名);
				if (!File.Exists(path)) {
					//MessageBox.Show( str存在しないときに表示するエラー文字列, "DTXMania runtime error", MessageBoxButtons.OK, MessageBoxIcon.Hand );
					return false;
				}
			}
			return true;
		}
		private static bool tDLLの存在チェック(string strDll名, string str存在しないときに表示するエラー文字列jp, string str存在しないときに表示するエラー文字列en) {
			return true;
			//return tDLLの存在チェック( strDll名, str存在しないときに表示するエラー文字列jp, str存在しないときに表示するエラー文字列en, false );
		}

		#region [DllImport]
		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern void FreeLibrary(IntPtr hModule);

		[DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
		internal static extern IntPtr LoadLibrary(string lpFileName);
		#endregion
		//-----------------------------
		#endregion

		[STAThread]
		static void Main() {
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			mutex二重起動防止用 = new Mutex(false, "DTXManiaMutex");

			if (mutex二重起動防止用.WaitOne(0, false)) {
				string newLine = Environment.NewLine;
				bool bDLLnotfound = false;

				Trace.WriteLine("Current Directory: " + Environment.CurrentDirectory);
				//Trace.WriteLine( "EXEのあるフォルダ: " + Path.GetDirectoryName( Application.ExecutablePath ) );

				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;


				{
					// BEGIN #23670 2010.11.13 from: キャッチされない例外は放出せずに、ログに詳細を出力する。
					// BEGIM #24606 2011.03.08 from: DEBUG 時は例外発生箇所を直接デバッグできるようにするため、例外をキャッチしないようにする。
#if !DEBUG
					try
#endif
					{

						string osplatform = "";
						if (OperatingSystem.IsWindows())
							osplatform = "win";
						else if (OperatingSystem.IsMacOS())
							osplatform = "osx";
						else if (OperatingSystem.IsLinux())
							osplatform = "linux";
						else
							throw new PlatformNotSupportedException("TJAPlayer3-f does not support this OS.");

						string platform = "";

						switch (RuntimeInformation.ProcessArchitecture) {
							case Architecture.X64:
								platform = "x64";
								break;
							case Architecture.X86:
								platform = "x86";
								break;
							case Architecture.Arm:
								platform = "arm";
								break;
							case Architecture.Arm64:
								platform = "arm64";
								break;
							default:
								throw new PlatformNotSupportedException($"TJAPlayer3 does not support this Architecture. ({RuntimeInformation.ProcessArchitecture})");
						}

						FFmpeg.AutoGen.ffmpeg.RootPath = AppContext.BaseDirectory + @"FFmpeg/" + osplatform + "-" + platform + "/";
						DirectoryInfo info = new DirectoryInfo(AppContext.BaseDirectory + @"Libs/" + osplatform + "-" + platform + "/");

						//実行ファイルの階層にライブラリをコピー
						foreach (FileInfo fileinfo in info.GetFiles()) {
							fileinfo.CopyTo(AppContext.BaseDirectory + fileinfo.Name, true);
						}

						using (var mania = new OpenTaiko())
							mania.Run();

						Trace.WriteLine("");
						Trace.WriteLine("遊んでくれてありがとう！");
					}
#if !DEBUG
					catch( Exception e )
					{
						Trace.WriteLine( "" );
						Trace.WriteLine( "OpenTaiko ran into an unexpected error, and can't continue from here! Σ(っ °Д °;)っ" );
						Trace.WriteLine( "More details: " );
						Trace.Write( e.ToString() );
					}
#endif
					// END #24606 2011.03.08 from
					// END #23670 2010.11.13 from

					if (Trace.Listeners.Count > 1)
						Trace.Listeners.RemoveAt(1);
				}

				// BEGIN #24615 2011.03.09 from: Mutex.WaitOne() が true を返した場合は、Mutex のリリースが必要である。

				mutex二重起動防止用.ReleaseMutex();
				mutex二重起動防止用 = null;

				// END #24615 2011.03.09 from
			} else      // DTXManiaが既に起動中
			  {

				// → 引数が0個の時はそのまま終了
				// 1個( コンパクトモード or DTXV -S) か2個 (DTXV -Nxxx ファイル名)のときは、そのプロセスにコマンドラインを丸々投げて終了する

				for (int i = 0; i < 5; i++)     // 検索結果のハンドルがZeroになることがあるので、200ms間隔で5回リトライする
				{
					#region [ 既に起動中のDTXManiaプロセスを検索する。]
					// このやり方だと、ShowInTaskbar=falseでタスクバーに表示されないパターンの時に検索に失敗するようだが
					// DTXManiaでそのパターンはない？のでこのままいく。
					// FindWindowを使えばこのパターンにも対応できるが、C#でビルドするアプリはウインドウクラス名を自前指定できないので、これは使わない。

					Process current = Process.GetCurrentProcess();
					Process[] running = Process.GetProcessesByName(current.ProcessName);
					Process target = null;
					//IntPtr hWnd = FindWindow( null, "DTXMania .NET style release " + CDTXMania.VERSION );

					foreach (Process p in running) {
						if (p.Id != current.Id) // プロセス名は同じでかつ、プロセスIDが自分自身とは異なるものを探す
						{
							if (p.MainModule.FileName == current.MainModule.FileName && p.MainWindowHandle != IntPtr.Zero) {
								target = p;
								break;
							}
						}
					}
					#endregion

					#region [ 起動中のDTXManiaがいれば、そのプロセスにコマンドラインを投げる ]
					if (target != null) {
						string[] commandLineArgs = Environment.GetCommandLineArgs();
						if (commandLineArgs != null && commandLineArgs.Length > 1) {
							string arg = null;
							for (int j = 1; j < commandLineArgs.Length; j++) {
								if (j == 1) {
									arg += commandLineArgs[j];
								} else {
									arg += " " + "\"" + commandLineArgs[j] + "\"";
								}
							}
						}
						break;
					}
					#endregion
					else {
						Trace.TraceInformation("メッセージ送信先のプロセスが見つからず。5回リトライします。");
						Thread.Sleep(200);
					}
				}
			}
		}
	}
}
