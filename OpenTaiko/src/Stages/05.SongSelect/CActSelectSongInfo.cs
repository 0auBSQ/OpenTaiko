using System.Drawing;
using FDK;

// Minimalist menu class to use for custom menus
namespace OpenTaiko {
	class CActSelectSongInfo : CStage {
		public CActSelectSongInfo() {
			base.IsDeActivated = true;
		}

		public override void Activate() {
			// On activation

			if (base.IsActivated)
				return;



			base.Activate();
		}

		public override void DeActivate() {
			// On de-activation

			base.DeActivate();
		}

		public override void CreateManagedResource() {
			// Ressource allocation

			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {
			// Ressource freeing

			base.ReleaseManagedResource();
		}

		public override int Draw() {
			if (OpenTaiko.stageSongSelect.rNowSelectedSong != null && OpenTaiko.stageSongSelect.rNowSelectedSong.eノード種別 == CSongListNode.ENodeType.SCORE) {
				int[] bpms = new int[3] {
						(int)OpenTaiko.stageSongSelect.rNowSelectedSong.arスコア[OpenTaiko.stageSongSelect.actSongList.tFetchDifficulty(OpenTaiko.stageSongSelect.rNowSelectedSong)].譜面情報.BaseBpm,
						(int)OpenTaiko.stageSongSelect.rNowSelectedSong.arスコア[OpenTaiko.stageSongSelect.actSongList.tFetchDifficulty(OpenTaiko.stageSongSelect.rNowSelectedSong)].譜面情報.MinBpm,
						(int)OpenTaiko.stageSongSelect.rNowSelectedSong.arスコア[OpenTaiko.stageSongSelect.actSongList.tFetchDifficulty(OpenTaiko.stageSongSelect.rNowSelectedSong)].譜面情報.MaxBpm
					};
				for (int i = 0; i < 3; i++) {
					tBPMNumberDraw(OpenTaiko.Skin.SongSelect_Bpm_X[i], OpenTaiko.Skin.SongSelect_Bpm_Y[i], bpms[i]);
				}

				if (OpenTaiko.stageSongSelect.actSongList.ttkSelectedSongMaker != null && OpenTaiko.Skin.SongSelect_Maker_Show) {
					TitleTextureKey.ResolveTitleTexture(OpenTaiko.stageSongSelect.actSongList.ttkSelectedSongMaker).t2D拡大率考慮描画(CTexture.RefPnt.Left, OpenTaiko.Skin.SongSelect_Maker[0], OpenTaiko.Skin.SongSelect_Maker[1]);
				}
				if (OpenTaiko.stageSongSelect.actSongList.ttkSelectedSongBPM != null && OpenTaiko.Skin.SongSelect_BPM_Text_Show) {
					TitleTextureKey.ResolveTitleTexture(OpenTaiko.stageSongSelect.actSongList.ttkSelectedSongBPM).t2D拡大率考慮描画(CTexture.RefPnt.Left, OpenTaiko.Skin.SongSelect_BPM_Text[0], OpenTaiko.Skin.SongSelect_BPM_Text[1]);
				}
				if (OpenTaiko.stageSongSelect.rNowSelectedSong.bExplicit)
					OpenTaiko.Tx.SongSelect_Explicit?.t2D描画(OpenTaiko.Skin.SongSelect_Explicit[0], OpenTaiko.Skin.SongSelect_Explicit[1]);
				if (OpenTaiko.stageSongSelect.rNowSelectedSong.bMovie)
					OpenTaiko.Tx.SongSelect_Movie?.t2D描画(OpenTaiko.Skin.SongSelect_Movie[0], OpenTaiko.Skin.SongSelect_Movie[1]);
			}


			return 0;
		}

		#region [Private]

		private void tBPMNumberDraw(float originx, float originy, int num) {
			int[] nums = CConversion.SeparateDigits(num);

			for (int j = 0; j < nums.Length; j++) {
				if (OpenTaiko.Skin.SongSelect_Bpm_Show && OpenTaiko.Tx.SongSelect_Bpm_Number != null) {
					float offset = j;
					float x = originx - (OpenTaiko.Skin.SongSelect_Bpm_Interval[0] * offset);
					float y = originy - (OpenTaiko.Skin.SongSelect_Bpm_Interval[1] * offset);

					float width = OpenTaiko.Tx.SongSelect_Bpm_Number.sz画像サイズ.Width / 10.0f;
					float height = OpenTaiko.Tx.SongSelect_Bpm_Number.sz画像サイズ.Height;

					OpenTaiko.Tx.SongSelect_Bpm_Number.t2D描画(x, y, new RectangleF(width * nums[j], 0, width, height));
				}
			}
		}

		#endregion
	}
}
