using FDK;

namespace OpenTaiko {
	internal class CActImplMob : CActivity {
		/// <summary>
		/// 踊り子
		/// </summary>
		public CActImplMob() {
			base.IsDeActivated = true;
		}

		public override void Activate() {
			var mobDir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.MOB}");
			var preset = HScenePreset.GetBGPreset();

			if (System.IO.Directory.Exists(mobDir)) {
				Random random = new Random();

				var upDirs = System.IO.Directory.GetDirectories(mobDir);
				if (upDirs.Length > 0) {
					var _presetPath = (preset != null && preset.MobSet != null) ? $@"{mobDir}" + preset.MobSet[random.Next(0, preset.MobSet.Length)] : "";
					var path = (preset != null && System.IO.Directory.Exists(_presetPath))
						? _presetPath
						: upDirs[random.Next(0, upDirs.Length)];

					MobScript = new ScriptBG($@"{path}{Path.DirectorySeparatorChar}Script.lua");
					MobScript.Init();
				}
			}

			base.Activate();
		}

		public override void DeActivate() {
			MobScript?.Dispose();

			base.DeActivate();
		}

		public override void CreateManagedResource() {
			base.CreateManagedResource();
		}

		public override void ReleaseManagedResource() {
			base.ReleaseManagedResource();
		}

		public override int Draw() {
			if (!OpenTaiko.stage演奏ドラム画面.bDoublePlay) {
				if (OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Tower && OpenTaiko.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan) {
					if (!OpenTaiko.stage演奏ドラム画面.bPAUSE) MobScript?.Update();
					MobScript?.Draw();

					/*
                    if (HGaugeMethods.UNSAFE_IsRainbow(0))
                    {

                        if (!TJAPlayer3.stage演奏ドラム画面.bPAUSE) nNowMobCounter += (Math.Abs((float)TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM[0] / 60.0f) * (float)TJAPlayer3.FPS.DeltaTime) * 180 / nMobBeat;
                        bool endAnime = nNowMobCounter >= 180;

                        if (endAnime)
                        {
                            nNowMobCounter = 0;
                        }

                        int moveHeight = (int)(70 * (TJAPlayer3.Skin.Resolution[1] / 720.0));

                        if (Mob != null)
                            Mob.t2D描画(0, (TJAPlayer3.Skin.Resolution[1] - (Mob.szテクスチャサイズ.Height - moveHeight)) + -((float)Math.Sin(nNowMobCounter * (Math.PI / 180)) * moveHeight));
                        
                    }
                    */
				}
			}
			return base.Draw();
		}
		#region[ private ]
		//-----------------
		private ScriptBG MobScript;
		//-----------------
		#endregion
	}
}
