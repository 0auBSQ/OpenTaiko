using System;
using System.Collections.Generic;
using System.Text;

namespace FDK
{
	public class CActivity
	{
		// プロパティ

		public bool IsActivated { get; private set; }
		public bool IsDeActivated
		{
			get
			{
				return !this.IsActivated;
			}
			set
			{
				this.IsActivated = !value;
			}
		}
		public List<CActivity> ChildActivities;

		/// <summary>
		/// <para>初めて On進行描画() を呼び出す場合に true を示す。（On活性化() 内で true にセットされる。）</para>
		/// <para>このフラグは、On活性化() では行えないタイミングのシビアな初期化を On進行描画() で行うために準備されている。利用は必須ではない。</para>
		/// <para>On進行描画() 側では、必要な初期化を追えたら false をセットすること。</para>
		/// </summary>
		protected bool IsFirstDraw = true;

	
		// コンストラクタ

		public CActivity()
		{
			this.IsDeActivated = true;
			this.ChildActivities = new List<CActivity>();
		}


		// ライフサイクルメソッド

		#region [ 子クラスで必要なもののみ override すること。]
		//-----------------

		public virtual void Activate()
		{
			// すでに活性化してるなら何もしない。
			if( this.IsActivated )
				return;

			this.IsActivated = true;		// このフラグは、以下の処理をする前にセットする。

			// 自身のリソースを作成する。
			//this.CreateManagedResource();
			//this.CreateUnmanagedResource();

			// すべての子 Activity を活性化する。
			foreach( CActivity activity in this.ChildActivities )
				activity.Activate();

			// その他の初期化
			this.IsFirstDraw = true;
		}
		public virtual void DeActivate()
		{
			// 活性化してないなら何もしない。
			if( this.IsDeActivated )
				return;

			// 自身のリソースを解放する。
			//this.ReleaseUnmanagedResource();
			//this.ReleaseManagedResource();

			// すべての 子Activity を非活性化する。
			foreach( CActivity activity in this.ChildActivities )
				activity.DeActivate();

			this.IsDeActivated = true;	// このフラグは、以上のメソッドを呼び出した後にセットする。
		}

		/// <summary>
		/// <para>Managed リソースの作成を行う。</para>
		/// <para>Direct3D デバイスが作成された直後に呼び出されるので、自分が活性化している時に限り、
		/// Managed リソースを作成（または再構築）すること。</para>
		/// <para>いつどのタイミングで呼び出されるか（いつDirect3Dが再作成されるか）分からないので、
		/// いつ何時呼び出されても問題無いようにコーディングしておくこと。</para>
		/// </summary>
		public virtual void CreateManagedResource()
		{
			// すべての 子Activity の Managed リソースを作成する。
			foreach( CActivity activity in this.ChildActivities )
				activity.CreateManagedResource();
		}

		/// <summary>
		/// <para>Unmanaged リソースの作成を行う。</para>
		/// <para>Direct3D デバイスが作成またはリセットされた直後に呼び出されるので、自分が活性化している時に限り、
		/// Unmanaged リソースを作成（または再構築）すること。</para>
		/// <para>いつどのタイミングで呼び出されるか（いつDirect3Dが再作成またはリセットされるか）分からないので、
		/// いつ何時呼び出されても問題無いようにコーディングしておくこと。</para>
		/// </summary>
		public virtual void CreateUnmanagedResource()
		{
			// すべての 子Activity の Unmanaged リソースを作成する。
			foreach( CActivity activity in this.ChildActivities )
				activity.CreateUnmanagedResource();
		}
		
		/// <summary>
		/// <para>Unmanaged リソースの解放を行う。</para>
		/// <para>Direct3D デバイスの解放直前またはリセット直前に呼び出される。</para>
		/// <para>いつどのタイミングで呼び出されるか（いつDirect3Dが解放またはリセットされるか）分からないので、
		/// いつ何時呼び出されても問題無いようにコーディングしておくこと。</para>
		/// </summary>
		public virtual void ReleaseUnmanagedResource()
		{
			// 活性化してないなら何もしない。
			if( this.IsDeActivated )
				return;

			// すべての 子Activity の Unmanaged リソースを解放する。
			foreach( CActivity activity in this.ChildActivities )
				activity.ReleaseUnmanagedResource();
		}

		/// <summary>
		/// <para>Managed リソースの解放を行う。</para>
		/// <para>Direct3D デバイスの解放直前に呼び出される。
		/// （Unmanaged リソースとは異なり、Direct3D デバイスのリセット時には呼び出されない。）</para>
		/// <para>いつどのタイミングで呼び出されるか（いつDirect3Dが解放されるか）分からないので、
		/// いつ何時呼び出されても問題無いようにコーディングしておくこと。</para>
		/// </summary>
		public virtual void ReleaseManagedResource()
		{
			// 活性化してないなら何もしない。
			if( this.IsDeActivated )
				return;

			// すべての 子Activity の Managed リソースを解放する。
			foreach( CActivity activity in this.ChildActivities )
				activity.ReleaseManagedResource();
		}

		/// <summary>
		/// <para>進行と描画を行う。（これらは分離されず、この１つのメソッドだけで実装する。）</para>
		/// <para>このメソッドは BeginScene() の後に呼び出されるので、メソッド内でいきなり描画を行ってかまわない。</para>
		/// </summary>
		/// <returns>任意の整数。呼び出し元との整合性を合わせておくこと。</returns>
		public virtual int Draw()
		{
			// 活性化してないなら何もしない。
			if( this.IsDeActivated )
				return 0;


			/* ここで進行と描画を行う。*/


			// 戻り値とその意味は子クラスで自由に決めていい。
			return 0;
		}
		
		//-----------------
		#endregion
	}
}