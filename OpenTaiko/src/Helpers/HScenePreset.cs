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

            return preset;
        }
    }
}