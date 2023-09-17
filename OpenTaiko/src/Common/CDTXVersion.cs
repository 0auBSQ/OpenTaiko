using System;
using System.Collections.Generic;
using System.Text;

namespace TJAPlayer3
{
	/// <summary>
	/// <para>DTXMania のバージョン。</para>
	/// <para>例1："078b" → 整数部=078, 小数部=2000000 ('英字'+'yymmdd') </para>
	/// <para>例2："078a(100124)" → 整数部=078, 小数部=1100124 ('英字'+'yymmdd')</para>
	/// </summary>
    public class CDTXVersion
	{
		// プロパティ

		/// <summary>
		/// <para>バージョンが未知のときに true になる。</para>
		/// </summary>
		public bool Unknown
		{
			get;
			private set;
		}

		/// <summary>
		/// <para>DTXMania のバージョンの整数部を表す。</para>
		/// <para>例1："078b" → 整数部=078</para>
		/// <para>例2："078a(100124)" → 整数部=078</para>
		/// </summary>
		public int n整数部;

		/// <summary>
		/// <para>DTXMania のバージョンの小数部を表す。</para>
		/// <para>小数部は、'英字(0～26) * 1000000 + 日付(yymmdd)' の式で表される整数。</para>
		/// <para>例1："078b" → 小数部=2000000 </para>
		/// <para>例2："078a(100124)" → 小数部=1100124</para>
		/// </summary>
		public int n小数部;


		// コンストラクタ

		public CDTXVersion()
		{
			this.n整数部 = 0;
			this.n小数部 = 0;
			this.Unknown = true;
		}
		public CDTXVersion( int n整数部 )
		{
			this.n整数部 = n整数部;
			this.n小数部 = 0;
			this.Unknown = false;
		}
		public CDTXVersion( string Version )
		{
			this.n整数部 = 0;
			this.n小数部 = 0;
			this.Unknown = true;
			
			if( Version.ToLower().Equals( "unknown" ) )
			{
				this.Unknown = true;
			}
			else
			{
				int num = 0;
				int length = Version.Length;
				if( ( num < length ) && char.IsDigit( Version[ num ] ) )
				{
					// 整数部　取得
					while( ( num < length ) && char.IsDigit( Version[ num ] ) )
					{
						this.n整数部 = ( this.n整数部 * 10 ) + CDTXVersion.DIG10.IndexOf( Version[ num++ ] );
					}

					// 小数部(1)英字部分　取得
					while( ( num < length ) && ( ( Version[ num ] == ' ' ) || ( Version[ num ] == '(' ) ) )
					{
						num++;
					}
					if( ( num < length ) && ( CDTXVersion.DIG36.IndexOf( Version[ num ] ) >= 10 ) )
					{
						this.n小数部 = CDTXVersion.DIG36.IndexOf( Version[ num++ ] ) - 10;
						if( this.n小数部 >= 0x1a )
						{
							this.n小数部 -= 0x1a;
						}
						this.n小数部++;
					}

					// 小数部(2)日付部分(yymmdd)　取得
					while( ( num < length ) && ( ( Version[ num ] == ' ' ) || ( Version[ num ] == '(' ) ) )
					{
						num++;
					}
					for( int i = 0; i < 6; i++ )
					{
						this.n小数部 *= 10;
						if( ( num < length ) && char.IsDigit( Version[ num ] ) )
						{
							this.n小数部 += CDTXVersion.DIG10.IndexOf( Version[ num ] );
						}
						num++;
					}
					this.Unknown = false;
				}
				else
				{
					this.Unknown = true;
				}
			}
		}
		public CDTXVersion( int n整数部, int n小数部 )
		{
			this.n整数部 = n整数部;
			this.n小数部 = n小数部;
			this.Unknown = false;
		}

	
		// メソッド
		
		public string toString()
		{
			var result = new StringBuilder( 32 );

			// 整数部
			result.Append( this.n整数部.ToString( "000" ) );

			// 英字部分（あれば）
			if( this.n小数部 >= 1000000 )
			{
				int n英字 = Math.Min( this.n小数部 / 1000000, 26 );	// 1～26
				result.Append( CDTXVersion.DIG36[ 10 + ( n英字 - 1 ) ] );
			}

			// 日付部分（あれば）
			int n日付 = this.n小数部 % 1000000;
			if( n日付 > 0 )
			{
				result.Append( '(' );
				result.Append( n日付.ToString( "000000" ) );
				result.Append( ')' );
			}

			return result.ToString();
		}

		public static bool operator ==( CDTXVersion x, CDTXVersion y )
		{
			return ( ( ( x.n整数部 == y.n整数部 ) && ( x.n小数部 == y.n小数部 ) ) && ( x.Unknown == y.Unknown ) );
		}
		public static bool operator >( CDTXVersion x, CDTXVersion y )
		{
			return ( ( x.n整数部 > y.n整数部 ) || ( ( x.n整数部 == y.n整数部 ) && ( x.n小数部 > y.n小数部 ) ) );
		}
		public static bool operator >=( CDTXVersion x, CDTXVersion y )
		{
			return ( ( x.n整数部 > y.n整数部 ) || ( ( x.n整数部 == y.n整数部 ) && ( x.n小数部 >= y.n小数部 ) ) );
		}
		public static bool operator !=( CDTXVersion x, CDTXVersion y )
		{
			if( ( x.n整数部 == y.n整数部 ) && ( x.n小数部 == y.n小数部 ) )
			{
				return ( x.Unknown != y.Unknown );
			}
			return true;
		}
		public static bool operator <( CDTXVersion x, CDTXVersion y )
		{
			return ( ( x.n整数部 < y.n整数部 ) || ( ( x.n整数部 == y.n整数部 ) && ( x.n小数部 < y.n小数部 ) ) );
		}
		public static bool operator <=( CDTXVersion x, CDTXVersion y )
		{
			return ( ( x.n整数部 < y.n整数部 ) || ( ( x.n整数部 == y.n整数部 ) && ( x.n小数部 <= y.n小数部 ) ) );
		}
		public override bool Equals(object obj)			// 2011.1.3 yyagi: warningを無くすために追加
		{
			if (obj == null)
			{
				return false;
			}
			if (this.GetType() != obj.GetType())
			{
				return false;
			}
			CDTXVersion objCDTXVersion = (CDTXVersion)obj;
			if (!int.Equals(this.n整数部, objCDTXVersion.n整数部) || !int.Equals(this.n小数部, objCDTXVersion.n小数部))
			{
				return false;
			}
			return true;
		}
		public override int GetHashCode()				// 2011.1.3 yyagi: warningを無くすために追加
		{
			string v = this.toString();
			return v.GetHashCode();
		}

		// その他

		#region [ private ]
		//-----------------
		private const string DIG36 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
		private const string DIG10 = "0123456789";
		//-----------------
		#endregion
	}
}
