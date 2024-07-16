using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using FDK;
using Silk.NET.SDL;

namespace TJAPlayer3
{
	internal class CTextConsole : CActivity
	{
		// 定数

		public enum EFontType
		{
			White,
			Cyan,
			Gray,
			WhiteSlim,
			CyanSlim,
			GraySlim
		}

		// メソッド

		public void tPrint( int x, int y, EFontType font, string strAlphanumericString )
		{
			if( !base.IsDeActivated && !string.IsNullOrEmpty( strAlphanumericString ) )
			{
                lcConsoleFont.tDrawText(x, y, font, strAlphanumericString);
            }
		}


		// CActivity 実装

		public override void Activate()
		{
			base.Activate();
		}
		public override void DeActivate()
		{
			base.DeActivate();
		}
		public override void CreateManagedResource()
		{
			if( !base.IsDeActivated )
			{
				lcConsoleFont = new CLuaConsoleFontScript(CSkin.Path("Modules/ConsoleFont"));

                base.CreateManagedResource();
			}
		}
		public override void ReleaseManagedResource()
		{
			if( !base.IsDeActivated )
			{
				lcConsoleFont?.Dispose();

                base.ReleaseManagedResource();
			}
		}


        // その他

        #region [ private ]
        //-----------------
        //private Rectangle[,] rc文字の矩形領域;
        //private const string str表記可能文字 = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~ ";
        //public int nFontWidth = 8, nFontHeight = 16;
        //private CTexture[] txフォント8x16 = new CTexture[ 2 ];
        private CLuaConsoleFontScript lcConsoleFont;
        //-----------------
        #endregion
    }
}
