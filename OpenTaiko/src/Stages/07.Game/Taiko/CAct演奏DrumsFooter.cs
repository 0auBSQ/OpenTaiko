using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace TJAPlayer3
{
    internal class CAct演奏DrumsFooter : CActivity
    {
        /// <summary>
        /// フッター
        /// </summary>
        public CAct演奏DrumsFooter()
        {
            base.IsDeActivated = true;
        }

        public override void Activate()
        {
            var footerDir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.FOOTER}");

            string presetSection = "";
            if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Tower)
            {
                presetSection = "Tower";
            }
            else if (TJAPlayer3.stage選曲.n確定された曲の難易度[0] == (int)Difficulty.Dan)
            {
                presetSection = "Dan";
            }
            else if (TJAPlayer3.ConfigIni.bAIBattleMode)
            {
                presetSection = "AI";
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

            if (System.IO.Directory.Exists(footerDir))
            {
                Random random = new Random();

                var upDirs = System.IO.Directory.GetFiles(footerDir);
                if (upDirs.Length > 0)
                {
                    var _presetPath = (preset != null) ? $@"{footerDir}" + preset.FooterSet[random.Next(0, preset.FooterSet.Length)] + ".png" : "";
                    var path = (preset != null && System.IO.File.Exists(_presetPath)) 
                        ?  _presetPath
                        : upDirs[random.Next(0, upDirs.Length)];

                    Mob_Footer = TJAPlayer3.tテクスチャの生成(path);
                }
            }

            base.Activate();
        }

        public override void DeActivate()
        {
            TJAPlayer3.t安全にDisposeする(ref Mob_Footer);

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
            if (this.Mob_Footer != null)
            {
                this.Mob_Footer.t2D描画(0, TJAPlayer3.Skin.Resolution[1] - this.Mob_Footer.szテクスチャサイズ.Height);
            }
            return base.Draw();
        }

        #region[ private ]
        //-----------------
        public CTexture Mob_Footer;
        //-----------------
        #endregion
    }
}
