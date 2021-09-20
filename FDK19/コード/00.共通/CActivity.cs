using System;
using System.Collections.Generic;
using System.Text;

namespace FDK
{
	public class CActivity
	{
		// プロパティ

		public bool b活性化してる { get; private set; }
		public bool b活性化してない
		{
			get
			{
				return !this.b活性化してる;
			}
			set
			{
				this.b活性化してる = !value;
			}
		}
		public List<CActivity> list子Activities;

		/// <summary>
		/// <para>初めて On進行描画() を呼び出す場合に true を示す。（On活性化() 内で true にセットされる。）</para>
		/// <para>このフラグは、On活性化() では行えないタイミングのシビアな初期化を On進行描画() で行うために準備されている。利用は必須ではない。</para>
		/// <para>On進行描画() 側では、必要な初期化を追えたら false をセットすること。</para>
		/// </summary>
		protected bool b初めての進行描画 = true;

	
		// コンストラクタ

		public CActivity()
		{
			this.b活性化してない = true;
			this.list子Activities = new List<CActivity>();
		}


		// ライフサイクルメソッド

		#region [ 子クラスで必要なもののみ override すること。]
		//-----------------

		public virtual void On活性化()
		{
			// すでに活性化してるなら何もしない。
			if( this.b活性化してる )
				return;

			this.b活性化してる = true;		// このフラグは、以下の処理をする前にセットする。

			// 自身のリソースを作成する。
			this.OnManagedリソースの作成();
			this.OnUnmanagedリソースの作成();

			// すべての子 Activity を活性化する。
			foreach( CActivity activity in this.list子Activities )
				activity.On活性化();

			// その他の初期化
			this.b初めての進行描画 = true;
		}
		public virtual void On非活性化()
		{
			// 活性化してないなら何もしない。
			if( this.b活性化してない )
				return;

			// 自身のリソースを解放する。
			this.OnUnmanagedリソースの解放();
			this.OnManagedリソースの解放();

			// すべての 子Activity を非活性化する。
			foreach( CActivity activity in this.list子Activities )
				activity.On非活性化();

			this.b活性化してない = true;	// このフラグは、以上のメソッドを呼び出した後にセットする。
		}

		/// <summary>
		/// <para>Managed リソースの作成を行う。</para>
		/// <para>Direct3D デバイスが作成された直後に呼び出されるので、自分が活性化している時に限り、
		/// Managed リソースを作成（または再構築）すること。</para>
		/// <para>いつどのタイミングで呼び出されるか（いつDirect3Dが再作成されるか）分からないので、
		/// いつ何時呼び出されても問題無いようにコーディングしておくこと。</para>
		/// </summary>
		public virtual void OnManagedリソースの作成()
		{
			// 活性化してないなら何もしない。
			if( this.b活性化してない )
				return;

			// すべての 子Activity の Managed リソースを作成する。
			foreach( CActivity activity in this.list子Activities )
				activity.OnManagedリソースの作成();
		}

		/// <summary>
		/// <para>Unmanaged リソースの作成を行う。</para>
		/// <para>Direct3D デバイスが作成またはリセットされた直後に呼び出されるので、自分が活性化している時に限り、
		/// Unmanaged リソースを作成（または再構築）すること。</para>
		/// <para>いつどのタイミングで呼び出されるか（いつDirect3Dが再作成またはリセットされるか）分からないので、
		/// いつ何時呼び出されても問題無いようにコーディングしておくこと。</para>
		/// </summary>
		public virtual void OnUnmanagedリソースの作成()
		{
			// 活性化してないなら何もしない。
			if( this.b活性化してない )
				return;

			// すべての 子Activity の Unmanaged リソースを作成する。
			foreach( CActivity activity in this.list子Activities )
				activity.OnUnmanagedリソースの作成();
		}
		
		/// <summary>
		/// <para>Unmanaged リソースの解放を行う。</para>
		/// <para>Direct3D デバイスの解放直前またはリセット直前に呼び出される。</para>
		/// <para>いつどのタイミングで呼び出されるか（いつDirect3Dが解放またはリセットされるか）分からないので、
		/// いつ何時呼び出されても問題無いようにコーディングしておくこと。</para>
		/// </summary>
		public virtual void OnUnmanagedリソースの解放()
		{
			// 活性化してないなら何もしない。
			if( this.b活性化してない )
				return;

			// すべての 子Activity の Unmanaged リソースを解放する。
			foreach( CActivity activity in this.list子Activities )
				activity.OnUnmanagedリソースの解放();
		}

		/// <summary>
		/// <para>Managed リソースの解放を行う。</para>
		/// <para>Direct3D デバイスの解放直前に呼び出される。
		/// （Unmanaged リソースとは異なり、Direct3D デバイスのリセット時には呼び出されない。）</para>
		/// <para>いつどのタイミングで呼び出されるか（いつDirect3Dが解放されるか）分からないので、
		/// いつ何時呼び出されても問題無いようにコーディングしておくこと。</para>
		/// </summary>
		public virtual void OnManagedリソースの解放()
		{
			// 活性化してないなら何もしない。
			if( this.b活性化してない )
				return;

			// すべての 子Activity の Managed リソースを解放する。
			foreach( CActivity activity in this.list子Activities )
				activity.OnManagedリソースの解放();
		}

		/// <summary>
		/// <para>進行と描画を行う。（これらは分離されず、この１つのメソッドだけで実装する。）</para>
		/// <para>このメソッドは BeginScene() の後に呼び出されるので、メソッド内でいきなり描画を行ってかまわない。</para>
		/// </summary>
		/// <returns>任意の整数。呼び出し元との整合性を合わせておくこと。</returns>
		public virtual int On進行描画()
		{
			// 活性化してないなら何もしない。
			if( this.b活性化してない )
				return 0;


			/* ここで進行と描画を行う。*/


			// 戻り値とその意味は子クラスで自由に決めていい。
			return 0;
		}
		
		//-----------------
		#endregion
	}
}