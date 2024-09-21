using System.Drawing;
using FDK;

namespace OpenTaiko {
	internal class CPluginHost : IPluginHost {
		// コンストラクタ

		public CPluginHost() {
			this._DTXManiaVersion = new CDTXVersion(OpenTaiko.VERSION);
		}


		// IPluginHost 実装

		public CDTXVersion DTXManiaVersion {
			get { return this._DTXManiaVersion; }
		}
		public CTimer Timer {
			get { return OpenTaiko.Timer; }
		}
		public SoundManager Sound管理 {
			get { return OpenTaiko.SoundManager; }
		}
		public Size ClientSize {
			get { return new Size(OpenTaiko.app.Window_.Size.X, OpenTaiko.app.Window_.Size.Y); }
		}
		public CStage.EStage e現在のステージ {
			get { return (OpenTaiko.r現在のステージ != null) ? OpenTaiko.r現在のステージ.eStageID : CStage.EStage.None; }
		}
		public CStage.EPhase e現在のフェーズ {
			get { return (OpenTaiko.r現在のステージ != null) ? OpenTaiko.r現在のステージ.ePhaseID : CStage.EPhase.Common_NORMAL; }
		}
		public bool t入力を占有する(IPluginActivity act) {
			if (OpenTaiko.act現在入力を占有中のプラグイン != null)
				return false;

			OpenTaiko.act現在入力を占有中のプラグイン = act;
			return true;
		}
		public bool t入力の占有を解除する(IPluginActivity act) {
			if (OpenTaiko.act現在入力を占有中のプラグイン == null || OpenTaiko.act現在入力を占有中のプラグイン != act)
				return false;

			OpenTaiko.act現在入力を占有中のプラグイン = null;
			return true;
		}
		public void tシステムサウンドを再生する(Eシステムサウンド sound) {
			if (OpenTaiko.Skin != null)
				OpenTaiko.Skin[sound].tPlay();
		}


		// その他

		#region [ private ]
		//-----------------
		private CDTXVersion _DTXManiaVersion;
		//-----------------
		#endregion
	}
}
