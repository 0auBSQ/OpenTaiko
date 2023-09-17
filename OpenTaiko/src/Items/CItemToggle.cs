using System;
using System.Collections.Generic;
using System.Text;

namespace TJAPlayer3
{
	/// <summary>
	/// 「トグル」（ON, OFF の2状態）を表すアイテム。
	/// </summary>
	internal class CItemToggle : CItemBase
	{
		// プロパティ

		public bool bON;

		
		// コンストラクタ

		public CItemToggle()
		{
			base.e種別 = CItemBase.E種別.ONorOFFトグル;
			this.bON = false;
		}
		public CItemToggle( string str項目名, bool b初期状態 )
			: this()
		{
			this.t初期化( str項目名, b初期状態 );
		}
		public CItemToggle(string str項目名, bool b初期状態, string str説明文jp)
			: this() {
			this.t初期化(str項目名, b初期状態, str説明文jp);
		}
		public CItemToggle(string str項目名, bool b初期状態, string str説明文jp, string str説明文en)
			: this() {
			this.t初期化(str項目名, b初期状態, str説明文jp, str説明文en);
		}
		public CItemToggle(string str項目名, bool b初期状態, CItemBase.Eパネル種別 eパネル種別)
			: this()
		{
			this.t初期化( str項目名, b初期状態, eパネル種別 );
		}
		public CItemToggle(string str項目名, bool b初期状態, CItemBase.Eパネル種別 eパネル種別, string str説明文jp)
			: this() {
			this.t初期化(str項目名, b初期状態, eパネル種別, str説明文jp);
		}
		public CItemToggle(string str項目名, bool b初期状態, CItemBase.Eパネル種別 eパネル種別, string str説明文jp, string str説明文en)
			: this() {
			this.t初期化(str項目名, b初期状態, eパネル種別, str説明文jp, str説明文en);
		}


		// CItemBase 実装

		public override void tEnter押下()
		{
			this.t項目値を次へ移動();
		}
		public override void t項目値を次へ移動()
		{
			this.bON = !this.bON;
		}
		public override void t項目値を前へ移動()
		{
			this.t項目値を次へ移動();
		}
		public void t初期化( string str項目名, bool b初期状態 )
		{
			this.t初期化( str項目名, b初期状態, CItemBase.Eパネル種別.通常 );
		}
		public void t初期化(string str項目名, bool b初期状態, string str説明文jp) {
			this.t初期化(str項目名, b初期状態, CItemBase.Eパネル種別.通常, str説明文jp, str説明文jp);
		}
		public void t初期化(string str項目名, bool b初期状態, string str説明文jp, string str説明文en) {
			this.t初期化(str項目名, b初期状態, CItemBase.Eパネル種別.通常, str説明文jp, str説明文en);
		}

		public void t初期化(string str項目名, bool b初期状態, CItemBase.Eパネル種別 eパネル種別)
		{
			this.t初期化(str項目名, b初期状態, eパネル種別, "", "");
		}
		public void t初期化(string str項目名, bool b初期状態, CItemBase.Eパネル種別 eパネル種別, string str説明文jp) {
			this.t初期化(str項目名, b初期状態, eパネル種別, str説明文jp, str説明文jp);
		}
		public void t初期化(string str項目名, bool b初期状態, CItemBase.Eパネル種別 eパネル種別, string str説明文jp, string str説明文en) {
			base.t初期化(str項目名, eパネル種別, str説明文jp, str説明文en);
			this.bON = b初期状態;
		}
		public override object obj現在値()
		{
			return ( this.bON ) ? "ON" : "OFF";
		}
		public override int GetIndex()
		{
			return ( this.bON ) ? 1 : 0;
		}
		public override void SetIndex( int index )
		{
			switch ( index )
			{
				case 0:
					this.bON = false;
					break;
				case 1:
					this.bON = true;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
