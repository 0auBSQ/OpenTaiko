using System;
using System.Collections.Generic;
using System.Text;

namespace TJAPlayer3
{
	/// <summary>
	/// 「整数」を表すアイテム。
	/// </summary>
	internal class CItemInteger : CItemBase
	{
		// プロパティ

		public int n現在の値;
		public bool b値がフォーカスされている;


		// コンストラクタ

		public CItemInteger()
		{
			base.e種別 = CItemBase.E種別.整数;
			this.n最小値 = 0;
			this.n最大値 = 0;
			this.n現在の値 = 0;
			this.b値がフォーカスされている = false;
		}
		public CItemInteger( string str項目名, int n最小値, int n最大値, int n初期値 )
			: this()
		{
			this.t初期化( str項目名, n最小値, n最大値, n初期値 );
		}
		public CItemInteger(string str項目名, int n最小値, int n最大値, int n初期値, string str説明文jp)
			: this() {
			this.t初期化(str項目名, n最小値, n最大値, n初期値, str説明文jp);
		}
		public CItemInteger(string str項目名, int n最小値, int n最大値, int n初期値, string str説明文jp, string str説明文en)
			: this() {
			this.t初期化(str項目名, n最小値, n最大値, n初期値, str説明文jp, str説明文en);
		}

	
		public CItemInteger( string str項目名, int n最小値, int n最大値, int n初期値, CItemBase.Eパネル種別 eパネル種別 )
			: this()
		{
			this.t初期化( str項目名, n最小値, n最大値, n初期値, eパネル種別 );
		}
		public CItemInteger(string str項目名, int n最小値, int n最大値, int n初期値, CItemBase.Eパネル種別 eパネル種別, string str説明文jp)
			: this() {
			this.t初期化(str項目名, n最小値, n最大値, n初期値, eパネル種別, str説明文jp);
		}
		public CItemInteger(string str項目名, int n最小値, int n最大値, int n初期値, CItemBase.Eパネル種別 eパネル種別, string str説明文jp, string str説明文en)
			: this() {
			this.t初期化(str項目名, n最小値, n最大値, n初期値, eパネル種別, str説明文jp, str説明文en);
		}


		// CItemBase 実装

		public override void tEnter押下()
		{
			this.b値がフォーカスされている = !this.b値がフォーカスされている;
		}
		public override void t項目値を次へ移動()
		{
			if( ++this.n現在の値 > this.n最大値 )
			{
				this.n現在の値 = this.n最大値;
			}
		}
		public override void t項目値を前へ移動()
		{
			if( --this.n現在の値 < this.n最小値 )
			{
				this.n現在の値 = this.n最小値;
			}
		}
		public void t初期化( string str項目名, int n最小値, int n最大値, int n初期値 )
		{
			this.t初期化( str項目名, n最小値, n最大値, n初期値, CItemBase.Eパネル種別.通常, "", "" );
		}
		public void t初期化(string str項目名, int n最小値, int n最大値, int n初期値, string str説明文jp) {
			this.t初期化(str項目名, n最小値, n最大値, n初期値, CItemBase.Eパネル種別.通常, str説明文jp, str説明文jp);
		}
		public void t初期化(string str項目名, int n最小値, int n最大値, int n初期値, string str説明文jp, string str説明文en) {
			this.t初期化(str項目名, n最小値, n最大値, n初期値, CItemBase.Eパネル種別.通常, str説明文jp, str説明文en);
		}

	
		public void t初期化( string str項目名, int n最小値, int n最大値, int n初期値, CItemBase.Eパネル種別 eパネル種別 )
		{
			this.t初期化( str項目名, n最小値, n最大値, n初期値, eパネル種別, "", "" );
		}
		public void t初期化(string str項目名, int n最小値, int n最大値, int n初期値, CItemBase.Eパネル種別 eパネル種別, string str説明文jp) {
			this.t初期化(str項目名, n最小値, n最大値, n初期値, eパネル種別, str説明文jp, str説明文jp);
		}
		public void t初期化(string str項目名, int n最小値, int n最大値, int n初期値, CItemBase.Eパネル種別 eパネル種別, string str説明文jp, string str説明文en) {
			base.t初期化(str項目名, eパネル種別, str説明文jp, str説明文en);
			this.n最小値 = n最小値;
			this.n最大値 = n最大値;
			this.n現在の値 = n初期値;
			this.b値がフォーカスされている = false;
		}
		public override object obj現在値()
		{
			return this.n現在の値;
		}
		public override int GetIndex()
		{
			return this.n現在の値;
		}
		public override void SetIndex( int index )
		{
			this.n現在の値 = index;
		}
		// その他

		#region [ private ]
		//-----------------
		private int n最小値;
		private int n最大値;
		//-----------------
		#endregion
	}
}
