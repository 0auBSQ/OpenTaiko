using System;
using System.Collections.Generic;
using System.Text;

namespace FDK
{
	public static class COS
	{
		/// <summary>
		/// OSがXP以前ならfalse, Vista以降ならtrueを返す
		/// </summary>
		/// <returns></returns>
		public static bool bIsVistaOrLater
		{
			get
			{
				//プラットフォームの取得
				System.OperatingSystem os = System.Environment.OSVersion;
				if ( os.Platform != PlatformID.Win32NT )		// NT系でなければ、XP以前か、PC Windows系以外のOSのため、Vista以降ではない。よってfalseを返す。
				{
					return false;
				}

				if ( os.Version.Major >= 6 )
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}
	}
}
