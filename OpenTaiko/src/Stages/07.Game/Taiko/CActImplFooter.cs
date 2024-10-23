using FDK;

namespace OpenTaiko;

internal class CActImplFooter : CActivity {
	/// <summary>
	/// フッター
	/// </summary>
	public CActImplFooter() {
		base.IsDeActivated = true;
	}

	public override void Activate() {
		var footerDir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.FOOTER}");
		var preset = HScenePreset.GetBGPreset();

		if (preset == null) return;

		if (Directory.Exists(footerDir)) {
			Random random = new Random();

			var upDirs = Directory.GetFiles(footerDir);
			if (preset.FooterSet != null) {
				var _presetPath = (preset.FooterSet.Length > 0) ? $@"{footerDir}" + preset.FooterSet[random.Next(0, preset.FooterSet.Length)] + ".png" : "";
				var path = File.Exists(_presetPath)
					? _presetPath
					: (upDirs.Length > 0 ? upDirs[random.Next(0, upDirs.Length)] : "");

				Mob_Footer = OpenTaiko.tテクスチャの生成(path);
			}
		}

		base.Activate();
	}

	public override void DeActivate() {
		OpenTaiko.tDisposeSafely(ref Mob_Footer);

		base.DeActivate();
	}

	public override void CreateManagedResource() {
		base.CreateManagedResource();
	}

	public override void ReleaseManagedResource() {
		base.ReleaseManagedResource();
	}

	public override int Draw() {
		this.Mob_Footer?.t2D描画(0, OpenTaiko.Skin.Resolution[1] - this.Mob_Footer.szTextureSize.Height);
		return base.Draw();
	}

	#region[ private ]
	//-----------------
	public CTexture Mob_Footer;
	//-----------------
	#endregion
}
