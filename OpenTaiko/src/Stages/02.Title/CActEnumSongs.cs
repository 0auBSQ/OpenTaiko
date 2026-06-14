using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using FDK;
using Silk.NET.Maths;
using SkiaSharp;

namespace OpenTaiko;

internal class CActEnumSongs : CActivity {
	public bool bCommandSongDataGet;


	/// <summary>
	/// Constructor
	/// </summary>
	public CActEnumSongs() {
		Init(false);
	}

	public CActEnumSongs(bool _bCommandSongDataGet) {
		Init(_bCommandSongDataGet);
	}
	private void Init(bool _bCommandSongDataGet) {
		base.IsDeActivated = true;
		bCommandSongDataGet = _bCommandSongDataGet;
	}

	// CActivity 実装

	public override void Activate() {
		if (this.IsActivated)
			return;
		base.Activate();

		try {
			this.ctNowEnumeratingSongs = new CCounter();    // 0, 1000, 17, CDTXMania.Timer );
			this.ctNowEnumeratingSongs.Start(0, 100, 17, OpenTaiko.Timer);
		} finally {
		}
	}
	public override void DeActivate() {
		if (this.IsDeActivated)
			return;

		base.DeActivate();
		this.ctNowEnumeratingSongs = null;
	}
	public override void CreateManagedResource() {
		//string pathNowEnumeratingSongs = CSkin.Path( @"Graphics\ScreenTitle NowEnumeratingSongs.png" );
		//if ( File.Exists( pathNowEnumeratingSongs ) )
		//{
		//	this.txNowEnumeratingSongs = CDTXMania.tテクスチャの生成( pathNowEnumeratingSongs, false );
		//}
		//else
		//{
		//	this.txNowEnumeratingSongs = null;
		//}
		//string pathDialogNowEnumeratingSongs = CSkin.Path( @"Graphics\ScreenConfig NowEnumeratingSongs.png" );
		//if ( File.Exists( pathDialogNowEnumeratingSongs ) )
		//{
		//	this.txDialogNowEnumeratingSongs = CDTXMania.tテクスチャの生成( pathDialogNowEnumeratingSongs, false );
		//}
		//else
		//{
		//	this.txDialogNowEnumeratingSongs = null;
		//}

		try {
			CCachedFontRenderer ftMessage = new CCachedFontRenderer(CLangManager.LangInstance.FontName, 40, CCachedFontRenderer.FontStyle.Bold);
			SKBitmap image = ftMessage.DrawText(CLangManager.LangInstance.GetString("SETTINGS_SYSTEM_RELOADSONG_STATUS"), Color.White);
			this.txMessage = new CTexture(image);
			this.txMessage.vcScaleRatio = new Vector3D<float>(0.5f, 0.5f, 1f);
			image.Dispose();
			OpenTaiko.tDisposeSafely(ref ftMessage);
		} catch (CTextureCreateFailedException e) {
			Trace.TraceError("テクスチャの生成に失敗しました。(txMessage)");
			Trace.TraceError(e.ToString());
			Trace.TraceError("例外が発生しましたが処理を継続します。 (761b726d-d27c-470d-be0b-a702971601b5)");
			this.txMessage = null;
		}

		base.CreateManagedResource();
	}
	public override void ReleaseManagedResource() {
		//CDTXMania.t安全にDisposeする( ref this.txDialogNowEnumeratingSongs );
		//CDTXMania.t安全にDisposeする( ref this.txNowEnumeratingSongs );
		OpenTaiko.tDisposeSafely(ref this.txMessage);
		base.ReleaseManagedResource();
	}

	public override int Draw() {
		if (this.IsDeActivated) {
			return 0;
		}
		this.ctNowEnumeratingSongs.TickLoop();
		if (OpenTaiko.Tx.Enum_Song != null) {
			OpenTaiko.Tx.Enum_Song.Opacity = (int)(176.0 + 80.0 * Math.Sin((double)(2 * Math.PI * this.ctNowEnumeratingSongs.CurrentValue * 2 / 100.0)));
			OpenTaiko.Tx.Enum_Song.t2DDraw(18, 7);
		}
		if (bCommandSongDataGet && OpenTaiko.Tx.Config_Enum_Song != null) {
			OpenTaiko.Tx.Config_Enum_Song.t2DDraw(180, 177);
			this.txMessage.t2DDraw(190, 197);
		}

		return 0;
	}

	public void RefreshSkin(bool isEnumerating) {
		this.DeActivate();
		this.ReleaseManagedResource();
		this.ReleaseUnmanagedResource();
		if (isEnumerating)
			this.Activate();
		this.CreateManagedResource();
		this.CreateUnmanagedResource();
	}

	private CCounter ctNowEnumeratingSongs;
	//private CTexture txNowEnumeratingSongs = null;
	//private CTexture txDialogNowEnumeratingSongs = null;
	private CTexture txMessage;
}
