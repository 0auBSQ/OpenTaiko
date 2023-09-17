using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace TJAPlayer3
{
	/// <summary>
	/// すべてのアイテムの基本クラス。
	/// </summary>
	internal class CItemBase
	{
		// プロパティ

		public Eパネル種別 eパネル種別;
		public enum Eパネル種別
		{
			通常,
			その他
		}

		public E種別 e種別;
		public enum E種別
		{
			基本形,
			ONorOFFトグル,
			ONorOFFor不定スリーステート,
			整数,
			リスト,
			切替リスト
		}

		public string str項目名;
		public string str説明文;


		// コンストラクタ

		public CItemBase()
		{
			this.str項目名 = "";
			this.str説明文 = "";
		}
		public CItemBase( string str項目名 )
			: this()
		{
			this.t初期化( str項目名 );
		}
		public CItemBase(string str項目名, string str説明文jp)
			: this() {
			this.t初期化(str項目名, str説明文jp);
		}
		public CItemBase(string str項目名,  string str説明文jp, string str説明文en)
			: this() {
			this.t初期化(str項目名, str説明文jp, str説明文en);
		}

		public CItemBase(string str項目名, Eパネル種別 eパネル種別)
			: this()
		{
			this.t初期化( str項目名, eパネル種別 );
		}
		public CItemBase(string str項目名, Eパネル種別 eパネル種別, string str説明文jp)
			: this() {
			this.t初期化(str項目名, eパネル種別, str説明文jp);
		}
		public CItemBase(string str項目名, Eパネル種別 eパネル種別, string str説明文jp, string str説明文en)
			: this() {
			this.t初期化(str項目名, eパネル種別, str説明文jp, str説明文en);
		}

		
		// メソッド；子クラスで実装する

		public virtual void tEnter押下()
		{
		}
		public virtual void t項目値を次へ移動()
		{
		}
		public virtual void t項目値を前へ移動()
		{
		}
		public virtual void t初期化( string str項目名 )
		{
			this.t初期化( str項目名, Eパネル種別.通常 );
		}
		public virtual void t初期化(string str項目名, string str説明文jp) {
			this.t初期化(str項目名, Eパネル種別.通常, str説明文jp, str説明文jp);
		}
		public virtual void t初期化(string str項目名, string str説明文jp, string str説明文en) {
			this.t初期化(str項目名, Eパネル種別.通常, str説明文jp, str説明文en);
		}

		public virtual void t初期化( string str項目名, Eパネル種別 eパネル種別 )
		{
			this.t初期化(str項目名, eパネル種別, "", "");
		}
		public virtual void t初期化(string str項目名, Eパネル種別 eパネル種別, string str説明文jp) {
			this.t初期化(str項目名, eパネル種別, str説明文jp, str説明文jp);
		}
		public virtual void t初期化(string str項目名, Eパネル種別 eパネル種別, string str説明文jp, string str説明文en) {
			this.str項目名 = str項目名;
			this.eパネル種別 = eパネル種別;
			this.str説明文 = (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ja") ? str説明文jp : str説明文en;
		}
		public virtual object obj現在値()
		{
			return null;
		}
		public virtual int GetIndex()
		{
			return 0;
		}
		public virtual void SetIndex( int index )
		{
		}
	}
}
