using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SharpDX.Direct3D9;
using FDK;

namespace TJAPlayer3
{
	/// <summary>
	/// 描画フレーム毎にGPUをフラッシュして、描画遅延を防ぐ。
	/// DirectX9の、Occlusion Queryを用いる。(Flush属性付きでGetDataする)
	/// Device Lost対策のため、QueueをCActivitiyのManagedリソースとして扱う。
	/// On進行描画()を呼び出すことで、GPUをフラッシュする。
	/// </summary>
	internal class CActFlushGPU : CActivity
	{
		// CActivity 実装

		public override void OnManagedリソースの作成()
		{
			if ( !base.b活性化してない )
			{
				try			// #xxxxx 2012.12.31 yyagi: to prepare flush, first of all, I create q queue to the GPU.
				{
					IDirect3DQuery9 = new Query(TJAPlayer3.app.Device, QueryType.Occlusion);
				}
				catch ( Exception e )
				{
					Trace.TraceError( e.ToString() );
					Trace.TraceError( "例外が発生しましたが処理を継続します。 (e5c7cd0b-f7bb-4bf1-9ad9-db27b43ff63d)" );
				}
				base.OnManagedリソースの作成();
			}
		}
		public override void  OnManagedリソースの解放()
		{
			IDirect3DQuery9.Dispose();
			IDirect3DQuery9 = null;
			base.OnManagedリソースの解放();
		}
		public override int On進行描画()
		{
			if ( !base.b活性化してない )
			{
				IDirect3DQuery9.Issue( Issue.End );
				DWM.Flush();
				IDirect3DQuery9.GetData<int>( out int _, true );	// flush GPU queue
			}
			return 0;
		}

		// その他

		#region [ private ]
		//-----------------
		private Query IDirect3DQuery9;
		//-----------------
		#endregion
	}
}
