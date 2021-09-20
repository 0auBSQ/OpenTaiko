namespace TJAPlayer3
{
	/// <summary>
	/// <para>プラグインホスト情報提供インターフェース。</para>
	/// <para>プラグインでは、On初期化() で渡されるこのインターフェースオブジェクトを通じて、
	/// DTXMania の持つ様々なリソースにアクセスできます。</para>
	/// </summary>
	public interface IPluginHost
	{
		/// <summary>
		/// DTXMania のバージョンを表します。
		/// </summary>
		global::TJAPlayer3.CDTXVersion DTXManiaVersion { get; }

		/// <summary>
		/// <para>Direct3D9 デバイスオブジェクト。</para>
		/// <para>ロストしたりリセットしたりすることがあるので、常に同じ値であるとは保証されません。</para>
		/// </summary>
		SlimDX.Direct3D9.Device D3D9Device { get; }

		/// <summary>
		/// <para>DirectSound の管理クラス。</para>
		/// <para>WAV, XA, OGG, MP3 のサウンドファイルから CSound オブジェクトを生成できます。</para>
		/// </summary>
		FDK.CSound管理 Sound管理 { get; }

		/// <summary>
		/// 描画エリアのサイズを返します（ピクセル単位）。
		/// </summary>
		System.Drawing.Size ClientSize { get; }

		/// <summary>
		/// 現在のステージのIDを表します。
		/// </summary>
		global::TJAPlayer3.CStage.Eステージ e現在のステージ { get; }

		/// <summary>
		/// 現在のステージにおけるフェーズのIDを表します。
		/// </summary>
		global::TJAPlayer3.CStage.Eフェーズ e現在のフェーズ { get; }

		/// <summary>
		/// <para>自分以外は入力データを扱ってはならないことを宣言します。</para>
		/// <para>DTXMania 本体は入力データのポーリングのみを行い、他のプラグインに対しては、On進行描画() の2つの入力に null を渡します。</para>
		/// </summary>
		/// <param name="act">宣言するプラグイン（すなわち this を指定する）</param>
		/// <returns>占有に成功すれば true を返し、既に誰かが占有中である場合には false を返します。</returns>
		bool t入力を占有する( IPluginActivity act );

		/// <summary>
		/// <para>自分以外が入力データを扱って良いことを宣言します。</para>
		/// <para>DTXMania 本体はポーリング以外の入力処理を開始し、他のプラグインに対しては、On進行描画() の2つの引数に有効な値を渡します。</para>
		/// </summary>
		/// <param name="act">宣言するプラグイン（すなわち this を指定する）</param>
		/// <returns>占有解除に成功すれば true、失敗すれば flase を返します。</returns>
		bool t入力の占有を解除する( IPluginActivity act );

		/// <summary>
		/// 指定されたシステムサウンド／BGMを再生します。
		/// </summary>
		/// <param name="sound">再生するシステムサウンドの識別子。</param>
		void tシステムサウンドを再生する( Eシステムサウンド sound );
	}
}
