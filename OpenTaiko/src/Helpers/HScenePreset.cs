using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using FDK;


namespace TJAPlayer3
{
    class HScenePreset
    {
        public static DBSkinPreset.SkinScene GetBGPreset()
        {
            string presetSection = "";
            if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Tower)
            {
                presetSection = "Tower";
            }
            else if (TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] == (int)Difficulty.Dan)
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
                case "Dan":
                    _ps = TJAPlayer3.Skin.Game_SkinScenes.Dan;
                    break;
                case "Tower":
                    _ps = TJAPlayer3.Skin.Game_SkinScenes.Tower;
                    break;
                case "AI":
                    _ps = TJAPlayer3.Skin.Game_SkinScenes.AI;
                    break;
                default:
                    break;
            };

            bool sectionIsValid = _ps != null ? ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).Count > 0 : false;

            var preset = (sectionIsValid 
                    && TJAPlayer3.stageSongSelect.rChoosenSong.strScenePreset != null 
                    && ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).ContainsKey(TJAPlayer3.stageSongSelect.rChoosenSong.strScenePreset)) 
                ? ((Dictionary<string,DBSkinPreset.SkinScene>)_ps)[TJAPlayer3.stageSongSelect.rChoosenSong.strScenePreset] 
                : null;

            if (sectionIsValid
                    && TJAPlayer3.DTX.scenePreset != null
                    && ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).ContainsKey(TJAPlayer3.DTX.scenePreset)) // If currently selected song has valid SCENEPRESET metadata within TJA
            {
                preset = ((Dictionary<string, DBSkinPreset.SkinScene>)_ps)[TJAPlayer3.DTX.scenePreset];
            }
            else if (sectionIsValid
                    && TJAPlayer3.stageSongSelect.rChoosenSong.strScenePreset != null
                    && ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).ContainsKey(TJAPlayer3.stageSongSelect.rChoosenSong.strScenePreset))
            {
                preset = ((Dictionary<string, DBSkinPreset.SkinScene>)_ps)[TJAPlayer3.stageSongSelect.rChoosenSong.strScenePreset];
            }
            else if (sectionIsValid 
                    && ((Dictionary<string, DBSkinPreset.SkinScene>)_ps).ContainsKey(""))
            {
                preset = ((Dictionary<string, DBSkinPreset.SkinScene>)_ps)[""];
            }
            else if (sectionIsValid)
            {
                var cstps = (Dictionary<string, DBSkinPreset.SkinScene>)_ps;
                Random rand = new Random();
                preset = cstps.ElementAt(rand.Next(0, cstps.Count)).Value;
            }

            return preset;
        }
    }
}