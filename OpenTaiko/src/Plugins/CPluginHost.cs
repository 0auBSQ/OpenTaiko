using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
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
		public CTimer Timer
		{
			get { return TJAPlayer3.Timer; }
		}
		public SoundManager Sound管理
		{
			get { return TJAPlayer3.Sound管理; }
		}
		public Size ClientSize
		{
			get { return new Size(TJAPlayer3.app.Window_.Size.X, TJAPlayer3.app.Window_.Size.Y); }
		}
		public CStage.EStage e現在のステージ
		{
			get { return ( TJAPlayer3.r現在のステージ != null ) ? TJAPlayer3.r現在のステージ.eStageID : CStage.EStage.None; }
		}
		public CStage.EPhase e現在のフェーズ
		{
			get { return ( TJAPlayer3.r現在のステージ != null ) ? TJAPlayer3.r現在のステージ.ePhaseID : CStage.EPhase.Common_NORMAL; }
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
