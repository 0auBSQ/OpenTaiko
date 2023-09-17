using System;
using System.Collections.Generic;
using System.Text;

namespace TJAPlayer3
{
	/// <summary>
	/// 「スリーステート」（ON, OFF, 不定 の3状態）を表すアイテム。
	/// </summary>
	internal class CItemThreeState : CItemBase
	{
		// プロパティ

		public E状態 e現在の状態;
		public enum E状態
		{
			ON,
			OFF,
			不定
		}


		// コンストラクタ

		public CItemThreeState()
		{
			base.e種別 = CItemBase.E種別.ONorOFFor不定スリーステート;
			this.e現在の状態 = E状態.不定;
		}
		public CItemThreeState( string str項目名, E状態 e初期状態 )
			: this()
		{
			this.t初期化( str項目名, e初期状態 );
		}
		public CItemThreeState(string str項目名, E状態 e初期状態, string str説明文jp)
			: this() {
			this.t初期化(str項目名, e初期状態, str説明文jp, str説明文jp);
		}
		public CItemThreeState(string str項目名, E状態 e初期状態, string str説明文jp, string str説明文en)
			: this() {
			this.t初期化(str項目名, e初期状態, str説明文jp, str説明文en);
		}

		public CItemThreeState( string str項目名, E状態 e初期状態, CItemBase.Eパネル種別 eパネル種別 )
			: this()
		{
			this.t初期化( str項目名, e初期状態, eパネル種別 );
		}
		public CItemThreeState(string str項目名, E状態 e初期状態, CItemBase.Eパネル種別 eパネル種別, string str説明文jp)
			: this() {
			this.t初期化(str項目名, e初期状態, eパネル種別, str説明文jp, str説明文jp);
		}
		public CItemThreeState(string str項目名, E状態 e初期状態, CItemBase.Eパネル種別 eパネル種別, string str説明文jp, string str説明文en)
			: this() {
			this.t初期化(str項目名, e初期状態, eパネル種別, str説明文jp, str説明文en);
		}


		// CItemBase 実装

		public override void tEnter押下()
		{
			this.t項目値を次へ移動();
		}
		public override void t項目値を次へ移動()
		{
			switch( this.e現在の状態 )
			{
				case E状態.ON:
					this.e現在の状態 = E状態.OFF;
					return;

				case E状態.OFF:
					this.e現在の状態 = E状態.ON;
					return;

				case E状態.不定:
					this.e現在の状態 = E状態.ON;
					return;
			}
		}
		public override void t項目値を前へ移動()
		{
			switch( this.e現在の状態 )
			{
				case E状態.ON:
					this.e現在の状態 = E状態.OFF;
					return;

				case E状態.OFF:
					this.e現在の状態 = E状態.ON;
					return;

				case E状態.不定:
					this.e現在の状態 = E状態.OFF;
					return;
			}
		}
		public void t初期化( string str項目名, E状態 e初期状態 )
		{
			this.t初期化( str項目名, e初期状態, CItemBase.Eパネル種別.通常 );
		}
		public void t初期化(string str項目名, E状態 e初期状態, string str説明文jp) {
			this.t初期化(str項目名, e初期状態, CItemBase.Eパネル種別.通常, str説明文jp, str説明文jp);
		}
		public void t初期化(string str項目名, E状態 e初期状態, string str説明文jp, string str説明文en) {
			this.t初期化(str項目名, e初期状態, CItemBase.Eパネル種別.通常, str説明文jp, str説明文en);
		}

		public void t初期化( string str項目名, E状態 e初期状態, CItemBase.Eパネル種別 eパネル種別 )
		{
			this.t初期化(str項目名, e初期状態, CItemBase.Eパネル種別.通常, "", "");
		}
		public void t初期化(string str項目名, E状態 e初期状態, CItemBase.Eパネル種別 eパネル種別, string str説明文jp) {
			this.t初期化(str項目名, e初期状態, CItemBase.Eパネル種別.通常, str説明文jp, str説明文jp);
		}
		public void t初期化(string str項目名, E状態 e初期状態, CItemBase.Eパネル種別 eパネル種別, string str説明文jp, string str説明文en) {
			base.t初期化(str項目名, eパネル種別, str説明文jp, str説明文en);
			this.e現在の状態 = e初期状態;
		}
		public override object obj現在値()
		{
			if ( this.e現在の状態 == E状態.不定 )
			{
				return "- -";
			}
			else
			{
				return this.e現在の状態.ToString();
			}
		}
		public override int GetIndex()
		{
			return (int)this.e現在の状態;
		}
		public override void SetIndex( int index )
		{
		    switch (index )
		    {
		        case 0:
		            this.e現在の状態 = E状態.ON;
		            break;
		        case 1:
		            this.e現在の状態 = E状態.OFF;
		            break;
		        case 2:
		            this.e現在の状態 = E状態.不定;
		            break;
		        default:
		            throw new ArgumentOutOfRangeException();
		    }
		}
	}
}
