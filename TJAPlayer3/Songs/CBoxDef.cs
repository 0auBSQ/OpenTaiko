﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Drawing;

namespace TJAPlayer3
{
	internal class CBoxDef
	{
		// プロパティ

		public Color Color;
		public string Genre;
		public string Title;
		public string[] strBoxText = new string[3];
		public Color ForeColor;
        public Color BackColor;
        public bool IsChangedForeColor;
        public bool IsChangedBackColor;


		// コンストラクタ

		public CBoxDef()
		{
			for (int i = 0; i < 3; i++)
				this.strBoxText[i] = "";
			this.Title = "";
			this.Genre = "";
            ForeColor = Color.White;
            BackColor = Color.Black;

		}
		public CBoxDef( string boxdefファイル名 )
			: this()
		{
			this.t読み込み( boxdefファイル名 );
		}


		// メソッド

		public void t読み込み( string boxdefファイル名 )
		{
			StreamReader reader = new StreamReader( boxdefファイル名, Encoding.GetEncoding( "Shift_JIS" ) );
			string str = null;
			while( ( str = reader.ReadLine() ) != null )
			{
				if( str.Length != 0 )
				{
					try
					{
						char[] ignoreCharsWoColon = new char[] { ' ', '\t' };

						str = str.TrimStart( ignoreCharsWoColon );
						if( ( str[ 0 ] == '#' ) && ( str[ 0 ] != ';' ) )
						{
							if( str.IndexOf( ';' ) != -1 )
							{
								str = str.Substring( 0, str.IndexOf( ';' ) );
							}
                        
							char[] ignoreChars = new char[] { ':', ' ', '\t' };
		
							if ( str.StartsWith( "#TITLE", StringComparison.OrdinalIgnoreCase ) )
							{
								this.Title = str.Substring( 6 ).Trim( ignoreChars );
							}
							else if( str.StartsWith( "#GENRE", StringComparison.OrdinalIgnoreCase ) )
							{
								this.Genre = str.Substring( 6 ).Trim( ignoreChars );
							}
							else if ( str.StartsWith( "#FONTCOLOR", StringComparison.OrdinalIgnoreCase ) )
							{
								this.Color = ColorTranslator.FromHtml( str.Substring( 10 ).Trim( ignoreChars ) );
							}
                            else if (str.StartsWith("#FORECOLOR", StringComparison.OrdinalIgnoreCase))
                            {
                                this.ForeColor = ColorTranslator.FromHtml(str.Substring(10).Trim(ignoreChars));
                                IsChangedForeColor = true;
                            }
                            else if (str.StartsWith("#BACKCOLOR", StringComparison.OrdinalIgnoreCase))
                            {
                                this.BackColor = ColorTranslator.FromHtml(str.Substring(10).Trim(ignoreChars));
                                IsChangedBackColor = true;
							}
							else
							{
								for(int i = 0; i < 3; i++)
                                {
									if (str.StartsWith("#BOXEXPLANATION" + (i + 1).ToString(), StringComparison.OrdinalIgnoreCase))
                                    {
										this.strBoxText[i] = str.Substring(16).Trim(ignoreChars);
                                    }
								}
							}
						}
						continue;
					}
					catch (Exception e)
					{
					    Trace.TraceError( e.ToString() );
					    Trace.TraceError( "例外が発生しましたが処理を継続します。 (178a9a36-a59e-4264-8e4c-b3c3459db43c)" );
						continue;
					}
				}
			}
			reader.Close();
		}
	}
}
