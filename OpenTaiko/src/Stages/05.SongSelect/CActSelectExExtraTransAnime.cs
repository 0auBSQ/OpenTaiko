using FDK;

namespace TJAPlayer3 {
	internal class CActSelectExExtraTransAnime : CActivity {
		enum AnimeState {
			NotRunning = 0,
			ExToExtra = 1,
			ExtraToEx = 2,
		}

		// Timer & script for each anime
		// Activate when swapping between ex/extra
		// Do not let player move until timer is complete
		// Stop drawing script when timer is finished
		public CActSelectExExtraTransAnime() {

		}
		// because i can't read japanese very well :
		public override void CreateManagedResource() //On Managed Create Resource
		{
			base.CreateManagedResource();

			CurrentState = AnimeState.NotRunning;

			ExToExtraCounter = new CCounter(0, 1, TJAPlayer3.Skin.SongSelect_Difficulty_Bar_ExExtra_AnimeDuration[0], TJAPlayer3.Timer);
			ExtraToExCounter = new CCounter(0, 1, TJAPlayer3.Skin.SongSelect_Difficulty_Bar_ExExtra_AnimeDuration[1], TJAPlayer3.Timer);

			ExToExtraScript = new AnimeBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.SONGSELECT}Difficulty_Select{Path.DirectorySeparatorChar}ExToExtra{Path.DirectorySeparatorChar}0{Path.DirectorySeparatorChar}Script.lua"));
			ExtraToExScript = new AnimeBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.SONGSELECT}Difficulty_Select{Path.DirectorySeparatorChar}ExtraToEx{Path.DirectorySeparatorChar}0{Path.DirectorySeparatorChar}Script.lua"));

			ExToExtraScript.Init();
			ExtraToExScript.Init();
		}

		public override void ReleaseManagedResource() //On Managed Release Resource
		{
			base.ReleaseManagedResource();

			ExToExtraCounter = null;
			ExtraToExCounter = null;

			if (ExToExtraScript != null) {
				ExToExtraScript.Dispose();
				ExToExtraScript = null;
			}
			if (ExtraToExScript != null) {
				ExtraToExScript.Dispose();
				ExtraToExScript = null;
			}
		}

		public override void Activate() //On Activate
		{
			base.Activate();
		}

		public override int Draw() //On Progress Draw
		{
			switch (CurrentState) {
				case AnimeState.ExToExtra:
					ExToExtraCounter.Tick();
					if (ExToExtraCounter.IsEnded) {
						CurrentState = AnimeState.NotRunning;
						ExToExtraCounter.Stop();
						return 0;
					}

					ExToExtraScript.Update();
					ExToExtraScript.Draw();
					return 1;

				case AnimeState.ExtraToEx:
					ExtraToExCounter.Tick();
					if (ExtraToExCounter.IsEnded) {
						CurrentState = AnimeState.NotRunning;
						ExtraToExCounter.Stop();
						return 0;
					}

					ExtraToExScript.Update();
					ExtraToExScript.Draw();
					return 1;

				case AnimeState.NotRunning:
				default:
					return 0;
			}
		}

		public override void DeActivate() //On Deactivate
		{
			base.DeActivate();
		}

		public void BeginAnime(bool toExtra) {
			if (!TJAPlayer3.ConfigIni.ShowExExtraAnime) return;
			else if (toExtra && !ExToExtraScript.Exists()) return;
			else if (!toExtra && !ExtraToExScript.Exists()) return;

			CurrentState = toExtra ? AnimeState.ExToExtra : AnimeState.ExtraToEx;
			if (toExtra) {
				ExToExtraCounter = new CCounter(0, 1, TJAPlayer3.Skin.SongSelect_Difficulty_Bar_ExExtra_AnimeDuration[0], TJAPlayer3.Timer);
				ExToExtraScript.PlayAnimation();
				TJAPlayer3.Skin.soundExToExtra[0]?.tPlay(); // Placeholder code
			} else {
				ExtraToExCounter = new CCounter(0, 1, TJAPlayer3.Skin.SongSelect_Difficulty_Bar_ExExtra_AnimeDuration[1], TJAPlayer3.Timer);
				ExtraToExScript.PlayAnimation();
				TJAPlayer3.Skin.soundExtraToEx[0]?.tPlay(); // Placeholder code
			}
		}

		#region Private
		CCounter ExToExtraCounter, ExtraToExCounter;
		AnimeBG ExToExtraScript, ExtraToExScript;

		AnimeState CurrentState;
		#endregion
	}
}
