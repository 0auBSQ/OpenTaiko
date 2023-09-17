using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TJAPlayer3
{
    public class CCourse
    {
        //2016.11.07 kairera0467
        //とりあえずメモ代わりに
        //
        //
        //○コースデータ（.tjc）
        
        //複数の曲を共通のゲージで連続して演奏します。
        //通常ゲージは某段位認定っぽい増減の仕方をします（補正なし）。
        
        //また曲は自動的に再生されるため、ばいそく等のオプションの使用は出来ません。
        
        //.tjcのファイルは他の譜面（.tja等）と同様に扱われるので譜面データと同じフォルダに入れても構いません。
        //
        //    ●ヘッダ

        //        TITLE:	コースの名前。

        //        COURSE:	「Easy」「Normal」「Hard」「Oni」「Edit」もしくは0-4の値。
        //            譜面の難易度。（コースと呼ぶとややこしいですがご諒承ください）
        //            指定した譜面は全てここで指定する難易度の譜面が流れます。
        //            曲ごとの指定は（今のところ）できません。

        //        LIFE:	ライフ。省略可。	
        //            ここに値を入れると通常ゲージの代わりにライフ制になります。

        //        SONG:	曲データ。
        //            演奏する曲のファイル名（.tja/.tjf）をtaikojiro.exeのあるディレクトリからの相対パスで指定。
        //            指定されたデータは上にあるものから順に演奏されます。


        public void t入力( string strファイル名 )
        {
            StreamReader reader = new StreamReader( strファイル名 );
            string str = reader.ReadToEnd();
            reader.Close();
        }
    }
}
