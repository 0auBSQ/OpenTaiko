using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;
using FDK;

namespace TJAPlayer3
{
	internal class CPluginHost : IPluginHost
	{
		// コンストラクタ

		public CPluginHost()
		{
			this._DTXManiaVersion = new CDTXVersion( TJAPlayer3.VERSION );
		}


		// IPluginHost 実装

		public CDTXVersion DTXManiaVersion
		{
			get { return this._DTXManiaVersion; }
		}
		public Device D3D9Device
		{
			get { return (TJAPlayer3.app != null ) ? TJAPlayer3.app.Device.UnderlyingDevice : null; }
		}
		public Format TextureFormat
		{
			get { return TJAPlayer3.TextureFormat; }
		}
		public CTimer Timer
		{
			get { return TJAPlayer3.Timer; }
		}
		public CSound管理 Sound管理
		{
			get { return TJAPlayer3.Sound管理; }
		}
		public Size ClientSize
		{
			get { return TJAPlayer3.app.Window.ClientSize; }
		}
		public CStage.Eステージ e現在のステージ
		{
			get { return ( TJAPlayer3.r現在のステージ != null ) ? TJAPlayer3.r現在のステージ.eステージID : CStage.Eステージ.何もしない; }
		}
		public CStage.Eフェーズ e現在のフェーズ
		{
			get { return ( TJAPlayer3.r現在のステージ != null ) ? TJAPlayer3.r現在のステージ.eフェーズID : CStage.Eフェーズ.共通_通常状態; }
		}
		public bool t入力を占有する(IPluginActivity act)
		{
			if (TJAPlayer3.act現在入力を占有中のプラグイン != null)
				return false;

			TJAPlayer3.act現在入力を占有中のプラグイン = act;
			return true;
		}
		public bool t入力の占有を解除する(IPluginActivity act)
		{
			if (TJAPlayer3.act現在入力を占有中のプラグイン == null || TJAPlayer3.act現在入力を占有中のプラグイン != act)
				return false;

			TJAPlayer3.act現在入力を占有中のプラグイン = null;
			return true;
		}
		public void tシステムサウンドを再生する( Eシステムサウンド sound )
		{
			if( TJAPlayer3.Skin != null )
				TJAPlayer3.Skin[ sound ].t再生する();
		}
		
		
		// その他

		#region [ private ]
		//-----------------
		private CDTXVersion _DTXManiaVersion;
		//-----------------
		#endregion
	}
}
