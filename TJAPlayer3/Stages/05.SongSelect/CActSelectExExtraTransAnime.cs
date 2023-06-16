using FDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CActSelectExExtraTransAnime : CActivity
    {
        enum AnimeState
        {
            NotRunning = 0,
            ExToExtra = 1,
            ExtraToEx = 2,
        }

        // Timer & script for each anime
        // Activate when swapping between ex/extra
        // Do not let player move until timer is complete
        // Stop drawing script when timer is finished
        public CActSelectExExtraTransAnime()
        {

        }
        // because i can't read japanese very well :
        public override void OnManagedリソースの作成() //On Managed Create Resource
        {
            base.OnManagedリソースの作成();

            CurrentState = AnimeState.NotRunning;
            
            ExToExtraCounter = new CCounter(0, 1, TJAPlayer3.Skin.SongSelect_Difficulty_Bar_ExExtra_AnimeDuration[0], TJAPlayer3.Timer);
            ExtraToExCounter = new CCounter(0, 1, TJAPlayer3.Skin.SongSelect_Difficulty_Bar_ExExtra_AnimeDuration[1], TJAPlayer3.Timer);
            
            ExToExtraScript = new AnimeBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.SONGSELECT}Difficulty_Select\\ExToExtra\\0\\Script.lua"));
            ExtraToExScript = new AnimeBG(CSkin.Path($"{TextureLoader.BASE}{TextureLoader.SONGSELECT}Difficulty_Select\\ExtraToEx\\0\\Script.lua"));

            ExToExtraScript.Init();
            ExtraToExScript.Init();
        }

        public override void OnManagedリソースの解放() //On Managed Release Resource
        {
            base.OnManagedリソースの解放();

            ExToExtraCounter = null;
            ExtraToExCounter = null;

            if (ExToExtraScript != null)
            {
            ExToExtraScript.Dispose();
            ExToExtraScript = null;
            }
            if (ExtraToExScript != null)
            {
            ExtraToExScript.Dispose();
            ExtraToExScript = null;
            }
        }

        public override void On活性化() //On Activate
        {
            base.On活性化();
        }

        public override int On進行描画() //On Progress Draw
        {
            switch (CurrentState)
            {
                case AnimeState.ExToExtra:
                    ExToExtraCounter.t進行();
                    if (ExToExtraCounter.b終了値に達した)
                    {
                        CurrentState = AnimeState.NotRunning;
                        ExToExtraCounter.t停止();
                        return 0;
                    }

                    ExToExtraScript.Update();
                    ExToExtraScript.Draw();
                    return 1;

                case AnimeState.ExtraToEx:
                    ExtraToExCounter.t進行();
                    if (ExtraToExCounter.b終了値に達した)
                    {
                        CurrentState = AnimeState.NotRunning;
                        ExtraToExCounter.t停止();
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

        public override void On非活性化() //On Deactivate
        {
            base.On非活性化();
        }

        public void BeginAnime(bool toExtra)
        {
            if (!TJAPlayer3.ConfigIni.ShowExExtraAnime) return;
            else if (toExtra && !ExToExtraScript.Exists()) return;
            else if (!toExtra && !ExtraToExScript.Exists()) return;

            CurrentState = toExtra ? AnimeState.ExToExtra : AnimeState.ExtraToEx;
            if (toExtra)
            {
                ExToExtraCounter = new CCounter(0, 1, TJAPlayer3.Skin.SongSelect_Difficulty_Bar_ExExtra_AnimeDuration[0], TJAPlayer3.Timer);
                ExToExtraScript.PlayAnimation();
                TJAPlayer3.Skin.soundExToExtra[0]?.t再生する(); // Placeholder code
            }
            else
            {
                ExtraToExCounter = new CCounter(0, 1, TJAPlayer3.Skin.SongSelect_Difficulty_Bar_ExExtra_AnimeDuration[1], TJAPlayer3.Timer);
                ExtraToExScript.PlayAnimation();
                TJAPlayer3.Skin.soundExtraToEx[0]?.t再生する(); // Placeholder code
            }
        }

        #region Private
        CCounter ExToExtraCounter, ExtraToExCounter;
        AnimeBG ExToExtraScript, ExtraToExScript;

        AnimeState CurrentState;
        #endregion
    }
}
