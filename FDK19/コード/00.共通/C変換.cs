using System;
using System.Collections.Generic;
using System.Text;

namespace FDK
{
	public class C変換
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

		public static int n値を範囲内に丸めて返す( int n値, int n最小値, int n最大値 )
		{
			if( n値 < n最小値 )
				return n最小値;

			if( n値 > n最大値 )
				return n最大値;

			return n値;
		}
		public static int n値を文字列から取得して範囲内に丸めて返す( string str数値文字列, int n最小値, int n最大値, int n取得失敗時のデフォルト値 )
		{
			int num;
			if( ( int.TryParse( str数値文字列, out num ) && ( num >= n最小値 ) ) && ( num <= n最大値 ) )
				return num;

			return n取得失敗時のデフォルト値;
        }

	    public static double db値を文字列から取得して範囲内に丸めて返す( string str数値文字列, double db最小値, double db最大値, double db取得失敗時のデフォルト値 )
	    {
	        double num;
	        if( ( double.TryParse( str数値文字列, out num ) && ( num >= db最小値 ) ) && ( num <= db最大値 ) )
	            return num;

	        return db取得失敗時のデフォルト値;
	    }

        // #23568 2010.11.04 ikanick add
        public static int n値を文字列から取得して範囲内にちゃんと丸めて返す(string str数値文字列, int n最小値, int n最大値, int n取得失敗時のデフォルト値)
        {
            // 1 と違って範囲外の場合ちゃんと丸めて返します。
            int num;
            if (int.TryParse(str数値文字列, out num)) {
                if ((num >= n最小値) && (num <= n最大値))
                    return num;
			    if ( num < n最小値 )
				    return n最小値;
			    if ( num > n最大値 )
				    return n最大値;
            }

            return n取得失敗時のデフォルト値;
        }
        // --------------------ここまで-------------------------/
		public static int n値を文字列から取得して返す( string str数値文字列, int n取得失敗時のデフォルト値 )
		{
			int num;
			if( !int.TryParse( str数値文字列, out num ) )
				num = n取得失敗時のデフォルト値;

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

        public static int[] ar配列形式のstringをint配列に変換して返す( string str )
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

        public static SlimDX.Color4 n255ToColor4( int nR, int nG, int nB )
        {
            float fR = n255ToParsent( nR );
            float fG = n255ToParsent( nG );
            float fB = n255ToParsent( nB );

            return new SlimDX.Color4( fR, fG, fB );
        }

		#region [ private ]
		//-----------------

		// private コンストラクタでインスタンス生成を禁止する。
		private C変換()
		{
		}
		//-----------------
		#endregion
	} 
}
