using System;
using System.Collections.Generic;
using System.Text;

namespace FDK
{
	public class CConversion
	{
		// プロパティ

		public static readonly string str16進数文字 = "0123456789ABCDEFabcdef";
		public static readonly string str36進数文字 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
		

		// メソッド

		public static bool bONorOFF( char c )
		{
			return ( c != '0' );
		}

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

		public static int n値を範囲内に丸めて返す( int value, int min, int max )
		{
			if( value < min )
				return min;

			if( value > max )
				return max;

			return value;
		}
		public static int n値を文字列から取得して範囲内に丸めて返す( string text, int min, int max, int defaultValue )
		{
			int num;
			if( ( int.TryParse( text, out num ) && ( num >= min ) ) && ( num <= max ) )
				return num;

			return defaultValue;
        }

	    public static double db値を文字列から取得して範囲内に丸めて返す( string text, double min, double max, double defaultValue )
	    {
	        double num;
	        if( ( double.TryParse( text, out num ) && ( num >= min ) ) && ( num <= max ) )
	            return num;

	        return defaultValue;
	    }

        // #23568 2010.11.04 ikanick add
        public static int n値を文字列から取得して範囲内にちゃんと丸めて返す(string text, int min, int max, int defaultValue)
        {
            // 1 と違って範囲外の場合ちゃんと丸めて返します。
            int num;
            if (int.TryParse(text, out num)) {
                if ((num >= min) && (num <= max))
                    return num;
			    if ( num < min )
				    return min;
			    if ( num > max )
				    return max;
            }

            return defaultValue;
        }
        // --------------------ここまで-------------------------/
		public static int StringToInt( string text, int defaultValue )
		{
			int num;
			if( !int.TryParse( text, out num ) )
				num = defaultValue;

			return num;
		}
		
		public static int n16進数2桁の文字列を数値に変換して返す( string strNum )
		{
			if( strNum.Length < 2 )
				return -1;

			int digit2 = str16進数文字.IndexOf( strNum[ 0 ] );
			if( digit2 < 0 )
				return -1;

			if( digit2 >= 16 )
				digit2 -= (16 - 10);		// A,B,C... -> 1,2,3...

			int digit1 = str16進数文字.IndexOf( strNum[ 1 ] );
			if( digit1 < 0 )
				return -1;

			if( digit1 >= 16 )
				digit1 -= (16 - 10);

			return digit2 * 16 + digit1;
		}
		public static int n36進数2桁の文字列を数値に変換して返す( string strNum )
		{
			if( strNum.Length < 2 )
				return -1;

			int digit2 = str36進数文字.IndexOf( strNum[ 0 ] );
			if( digit2 < 0 )
				return -1;

			if( digit2 >= 36 )
				digit2 -= (36 - 10);		// A,B,C... -> 1,2,3...

			int digit1 = str36進数文字.IndexOf( strNum[ 1 ] );
			if( digit1 < 0 )
				return -1;

			if( digit1 >= 36 )
				digit1 -= (36 - 10);

			return digit2 * 36 + digit1;
		}
		public static int n小節番号の文字列3桁を数値に変換して返す( string strNum )
		{
			if( strNum.Length >= 3 )
			{
				int digit3 = str36進数文字.IndexOf( strNum[ 0 ] );
				if( digit3 < 0 )
					return -1;

				if( digit3 >= 36 )									// 3桁目は36進数
					digit3 -= (36 - 10);

				int digit2 = str16進数文字.IndexOf( strNum[ 1 ] );	// 2桁目は10進数
				if( ( digit2 < 0 ) || ( digit2 > 9 ) )
					return -1;

				int digit1 = str16進数文字.IndexOf( strNum[ 2 ] );	// 1桁目も10進数
				if( ( digit1 >= 0 ) && ( digit1 <= 9 ) )
					return digit3 * 100 + digit2 * 10 + digit1;
			}
			return -1;
		}
		
		public static string str小節番号を文字列3桁に変換して返す( int num )
		{
			if( ( num < 0 ) || ( num >= 3600 ) )	// 3600 == Z99 + 1
				return "000";

			int digit4 = num / 100;
			int digit2 = ( num % 100 ) / 10;
			int digit1 = ( num % 100 ) % 10;
			char ch3 = str36進数文字[ digit4 ];
			char ch2 = str16進数文字[ digit2 ];
			char ch1 = str16進数文字[ digit1 ];
			return ( ch3.ToString() + ch2.ToString() + ch1.ToString() );
		}
		public static string str数値を16進数2桁に変換して返す( int num )
		{
			if( ( num < 0 ) || ( num >= 0x100 ) )
				return "00";

			char ch2 = str16進数文字[ num / 0x10 ];
			char ch1 = str16進数文字[ num % 0x10 ];
			return ( ch2.ToString() + ch1.ToString() );
		}
		public static string str数値を36進数2桁に変換して返す( int num )
		{
			if( ( num < 0 ) || ( num >= 36 * 36 ) )
				return "00";

			char ch2 = str36進数文字[ num / 36 ];
			char ch1 = str36進数文字[ num % 36 ];
			return ( ch2.ToString() + ch1.ToString() );
		}

        public static int[] StringToIntArray( string str )
        {
            //0,1,2 ...の形式で書かれたstringをint配列に変換する。
            //一応実装はしたものの、例外処理などはまだ完成していない。
            //str = "0,1,2";
            if( String.IsNullOrEmpty( str ) )
                return null;

            string[] strArray = str.Split( ',' );
            List<int> listIntArray;
            listIntArray = new List<int>();

            for( int n = 0; n < strArray.Length; n++ )
            {
                int n追加する数値 = Convert.ToInt32( strArray[ n ] );
                listIntArray.Add( n追加する数値 );
            }
            int[] nArray = new int[] { 1 };
            nArray = listIntArray.ToArray();

            return nArray;
        }


        /// <summary>
        /// 百分率数値を255段階数値に変換するメソッド。透明度用。
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static int nParsentTo255( double num )
        {
            return (int)(255.0 * num);
        }

        /// <summary>
        /// 255段階数値を百分率に変換するメソッド。
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public static int n255ToParsent( int num )
        {
            return (int)(100.0 / num);
        }

        public static Color4 n255ToColor4( int nR, int nG, int nB )
        {
            float fR = n255ToParsent( nR );
            float fG = n255ToParsent( nG );
            float fB = n255ToParsent( nB );

            return new Color4( fR, fG, fB, 1f );
        }

		public static Color4 ColorToColor4(System.Drawing.Color col)
        {
			return new Color4(col.R / 255f, col.G / 255f, col.B / 255f, col.A / 255f);
        }

		public static int[] SeparateDigits(int num)
		{
			int[] digits = new int[num.ToString().Length];
			for (int i = 0; i < digits.Length; i++)
			{
				digits[i] = num % 10;
				num /= 10;
			}
			return digits;
		}

		#region [ private ]
		//-----------------

		// private コンストラクタでインスタンス生成を禁止する。
		private CConversion()
		{
		}
		//-----------------
		#endregion
	} 
}
