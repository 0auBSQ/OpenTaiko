using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace TJAPlayer3
{
    internal class CAct演奏DrumsMob : CActivity
    {
        /// <summary>
        /// 踊り子
        /// </summary>
        public CAct演奏DrumsMob()
        {
            base.IsDeActivated = true;
        }

        public override void Activate()
        {
            var mobDir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.MOB}");

            string presetSection = "";
            if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
            {
            }
            else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
            {
                presetSection = "Dan";
            }
            else if (TJAPlayer3.ConfigIni.bAIBattleMode)
            {
            }
            else
            {
                presetSection = "Regular";
            }

            object _ps = null;

            switch (presetSection)
            {
                case "Regular":
                    _ps = TJAPlayer3.Skin.Game_SkinScenes.Regular;
                    break;
                default:
                    break;
            };
            
            var preset = (_ps != null 
                    && TJAPlayer3.stage選曲.r確定された曲.strScenePreset != null 
                    && ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).ContainsKey(TJAPlayer3.stage選曲.r確定された曲.strScenePreset)) 
                ? ((Dictionary<string,DBSkinPreset.SkinScene>)_ps)[TJAPlayer3.stage選曲.r確定された曲.strScenePreset] 
                : null;

            if (_ps != null
                    && TJAPlayer3.DTX.scenePreset != null
                    && ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).ContainsKey(TJAPlayer3.DTX.scenePreset)) // If currently selected song has valid SCENEPRESET metadata within TJA
            {
                preset = ((Dictionary<string, DBSkinPreset.SkinScene>)_ps)[TJAPlayer3.DTX.scenePreset];
            }

            if (System.IO.Directory.Exists(mobDir))
            {
                Random random = new Random();

                var upDirs = System.IO.Directory.GetFiles(mobDir);
                if (upDirs.Length > 0)
                {
                    var _presetPath = (preset != null) ? $@"{mobDir}" + preset.MobSet[random.Next(0, preset.MobSet.Length)] + ".png" : "";
                    var path = (preset != null && System.IO.File.Exists(_presetPath)) 
                        ?  _presetPath
                        : upDirs[random.Next(0, upDirs.Length)];

                    Mob = TJAPlayer3.tテクスチャの生成(path);
                }
            }
            
            nMobBeat = TJAPlayer3.Skin.Game_Mob_Beat;

            base.Activate();
        }

        public override void DeActivate()
        {
            base.DeActivate();
        }

        public override void CreateManagedResource()
        {
            base.CreateManagedResource();
        }

        public override void ReleaseManagedResource()
        {
            base.ReleaseManagedResource();
        }

        public override int Draw()
        {
            if(!TJAPlayer3.stage演奏ドラム画面.bDoublePlay)
            {
                if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Tower && TJAPlayer3.stage選曲.n確定された曲の難易度[0] != (int)Difficulty.Dan)
                {
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

                }
            }
            return base.Draw();
        }
        #region[ private ]
        //-----------------
        public CTexture Mob;
        private float nNowMobCounter;
        private float nMobBeat;
        //-----------------
        #endregion
    }
}
