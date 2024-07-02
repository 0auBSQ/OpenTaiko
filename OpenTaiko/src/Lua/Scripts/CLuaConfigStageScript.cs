using NLua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class CLuaConfigStageScript : CLuaStageScript
    {
        public CLuaConfigStageInfo Info { get; private set; }

        private LuaFunction lfGenMenuItemLeft;

        private LuaFunction lfGenDescriptionPanel;

        private LuaFunction lfGenItembox;

        public void GenMenuItemLeft(params object[] args)
        {
            if (!Avaibale) return;

            RunLuaCode(lfGenMenuItemLeft, args);
        }

        public void GenDescriptionPanel(params object[] args)
        {
            if (!Avaibale) return;

            RunLuaCode(lfGenDescriptionPanel, args);
        }

        public void GenItembox(params object[] args)
        {
            if (!Avaibale) return;

            RunLuaCode(lfGenItembox, args);
        }

        public CItemBase GetItembox(int index)
        {
            return Info.listItemList[Info.nItembarIndex + index + 1];
        }

        public CLuaConfigStageScript(string dir, string? texturesDir = null, string? soundsDir = null, bool loadAssets = true) : base(dir, texturesDir, soundsDir, loadAssets)
        {
            lfGenMenuItemLeft = (LuaFunction)LuaScript["genMenuItemLeft"];
            lfGenDescriptionPanel = (LuaFunction)LuaScript["genDescriptionPanel"];
            lfGenItembox = (LuaFunction)LuaScript["genItembox"];
            LuaScript["getItemBox"] = GetItembox;
            LuaScript["configstageinfo"] = Info = new CLuaConfigStageInfo();
        }
    }
}
