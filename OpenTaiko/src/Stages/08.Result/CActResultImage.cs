﻿using System.Diagnostics;
using FDK;

namespace OpenTaiko;

internal class CActResultImage : CActivity {
	// コンストラクタ
	/// <summary>
	/// リザルト画像を表示させるクラス。XG化するにあたって動画は廃止。
	/// また、中央の画像も表示する。(STAGE表示、STANDARD_CLASSICなど)
	/// </summary>
	public CActResultImage() {
		base.IsDeActivated = true;
	}


	// メソッド

	public void tアニメを完了させる() {
		this.ct登場用.CurrentValue = (int)this.ct登場用.EndValue;
	}


	// CActivity 実装

	public override void Activate() {

		base.Activate();
	}
	public override void DeActivate() {
		if (this.ct登場用 != null) {
			this.ct登場用 = null;
		}
		base.DeActivate();
	}
	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}
	public override unsafe int Draw() {
		if (base.IsDeActivated) {
			return 0;
		}
		if (base.IsFirstDraw) {
			this.ct登場用 = new CCounter(0, 100, 5, OpenTaiko.Timer);
			base.IsFirstDraw = false;
		}
		this.ct登場用.Tick();

		if (!this.ct登場用.IsEnded) {
			return 0;
		}
		return 1;
	}


	// その他

	#region [ private ]
	//-----------------
	private CCounter ct登場用;
	private CTexture r表示するリザルト画像;
	private CTexture txリザルト画像;

	private bool tプレビュー画像の指定があれば構築する() {
		if (string.IsNullOrEmpty(OpenTaiko.TJA.PREIMAGE)) {
			return false;
		}
		OpenTaiko.tDisposeSafely(ref this.txリザルト画像);
		this.r表示するリザルト画像 = null;
		string path = OpenTaiko.TJA.strフォルダ名 + OpenTaiko.TJA.PREIMAGE;
		if (!File.Exists(path)) {
			Trace.TraceWarning("ファイルが存在しません。({0})", new object[] { path });
			return false;
		}
		this.txリザルト画像 = OpenTaiko.tテクスチャの生成(path);
		this.r表示するリザルト画像 = this.txリザルト画像;
		return (this.r表示するリザルト画像 != null);
	}
	//-----------------
	#endregion
}
