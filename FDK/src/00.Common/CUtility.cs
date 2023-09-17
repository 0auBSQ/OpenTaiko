using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace FDK
{
	public class CUtility
	{

		public static void RunCompleteGC()
		{
			GC.Collect();					// アクセス不可能なオブジェクトを除去し、ファイナライぜーション実施。
			GC.WaitForPendingFinalizers();	// ファイナライゼーションが終わるまでスレッドを待機。
			GC.Collect();					// ファイナライズされたばかりのオブジェクトに関連するメモリを開放。

			// 出展: http://msdn.microsoft.com/ja-jp/library/ms998547.aspx#scalenetchapt05_topic10
		}


		// ログ

		public static void LogBlock( string name, Action method )
		{
			Trace.TraceInformation( "--------------------" );
			Trace.TraceInformation( "開始 - " + name );
			Trace.Indent();
			try
			{
				method();
			}
			finally
			{
				Trace.Unindent();
				Trace.TraceInformation( "終了 - " + name );
				Trace.TraceInformation( "--------------------" );
			}
		}
		public static void LogException( Exception e )
		{
			Trace.WriteLine( "---例外ここから----" );
			Trace.WriteLine( e.ToString() );
			Trace.WriteLine( "---例外ここまで----" );
		}


		// IO

		public static string t指定した拡張子を持つファイルを検索し最初に見つけたファイルの絶対パスを返す( string strフォルダパス, List<string> extensions )
		{
			string[] files = Directory.GetFiles( strフォルダパス );		// GetFiles() は完全パスを返す。


			// ファイル順より拡張子順を優先して検索する。→ 拡張子リストの前方の拡張子ほど先に発見されるようにするため。

			foreach( string ext in extensions )
			{
				foreach( string file in files )
				{
					string fileExt = Path.GetExtension( file );

					if( fileExt.Equals( ext, StringComparison.OrdinalIgnoreCase ) )
						return file;		// あった
				}
			}

			return null;	// なかった
		}

		public static void ReadXML<T>( string fileName, out T xmlObject )
		{
			xmlObject = default( T );

			FileStream fs = null;
			StreamReader sr = null;
			try
			{
				fs = new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );	// FileShare を付けとかないと、Close() 後もロックがかかる。
				sr = new StreamReader( fs, Encoding.UTF8 );
				var xmlsl = new System.Xml.Serialization.XmlSerializer( typeof( T ) );
				xmlObject = (T) xmlsl.Deserialize( sr );
			}
			finally
			{
				if( sr != null )
					sr.Close();		// fr も一緒にClose()される
			}
		}
		public static void WriteXML<T>( string fileName, T xmlObject )
		{
			FileStream fs = null;
			StreamWriter sw = null;
			try
			{
				fs = new FileStream( fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite );	// FileShare を付けとかないと、Close() 後もロックがかかる。
				sw = new StreamWriter( fs, Encoding.UTF8 );
				var xmlsl = new System.Xml.Serialization.XmlSerializer( typeof( T ) );
				xmlsl.Serialize( sw, xmlObject );
			}
			finally
			{
				if( sw != null )
					sw.Close();		// fs も一緒にClose()される
			}
		}


		// 数学

		public static double DegreeToRadian( double angle )
		{
			return ( ( Math.PI * angle ) / 180.0 );
		}
		public static double RadianToDegree( double angle )
		{
			return ( angle * 180.0 / Math.PI );
		}
		public static float DegreeToRadian( float angle )
		{
			return (float) DegreeToRadian( (double) angle );
		}
		public static float RadianToDegree( float angle )
		{
			return (float) RadianToDegree( (double) angle );
		}

        public static bool ToggleBoolian( ref bool bFlag )
        {
            if( bFlag == true ) bFlag = false;
            else if( bFlag == false ) bFlag = true;

            return true;
        }
	}	
}
