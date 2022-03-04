using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace TJAPlayer3
{
    internal class CSongDict
    {
        private static Dictionary<string, C曲リストノード> nodes = new Dictionary<string, C曲リストノード>();

        public static C曲リストノード tGetNodeFromID(string id)
        {
            if (nodes.ContainsKey(id))
                return nodes[id].Clone();
            return null;
        }

        public static void tAddSongNode(string id, C曲リストノード node)
        {
            if (!nodes.ContainsKey(id))
                nodes.Add(id, node.Clone());
        }

        #region [Extra methods]

        // Generate a back button
        public static C曲リストノード tGenerateBackButton(C曲リストノード parent, string path = "/", List<string> listStrBoxDef = null)
        {
            C曲リストノード itemBack = new C曲リストノード();
            itemBack.eノード種別 = C曲リストノード.Eノード種別.BACKBOX;


            // とじる
            itemBack.strタイトル = CLangManager.LangInstance.GetString(200) + " (" + path + ")";

            itemBack.BackColor = ColorTranslator.FromHtml("#513009");
            itemBack.BoxColor = Color.White;

            itemBack.BgColor = parent.BgColor;
            itemBack.isChangedBgColor = parent.isChangedBgColor;
            itemBack.BgType = parent.BgType;
            itemBack.isChangedBgType = parent.isChangedBgType;

            itemBack.strジャンル = parent.strジャンル;
            itemBack.nスコア数 = 1;
            itemBack.r親ノード = parent;
            itemBack.strSkinPath = (parent.r親ノード == null) ?
                "" : parent.r親ノード.strSkinPath;

            // I guess this is used to count the number of box.def instances and only at startup, which makes using it here pretty weird
            if (listStrBoxDef != null && itemBack.strSkinPath != "" && !listStrBoxDef.Contains(itemBack.strSkinPath))
            {
                listStrBoxDef.Add(itemBack.strSkinPath);
            }

            itemBack.strBreadcrumbs = (itemBack.r親ノード == null) ?
                itemBack.strタイトル : itemBack.r親ノード.strBreadcrumbs + " > " + itemBack.strタイトル;

            itemBack.arスコア[0] = new Cスコア();
            itemBack.arスコア[0].ファイル情報.フォルダの絶対パス = "";
            itemBack.arスコア[0].譜面情報.タイトル = itemBack.strタイトル;
            itemBack.arスコア[0].譜面情報.コメント = "";

            return (itemBack);
        }

        public static C曲リストノード tGenerateRandomButton(C曲リストノード parent, string path = "/")
        {
            C曲リストノード itemRandom = new C曲リストノード();
            itemRandom.eノード種別 = C曲リストノード.Eノード種別.RANDOM;

            itemRandom.strタイトル = CLangManager.LangInstance.GetString(203) + " (" + path + ")"; ;

            itemRandom.nスコア数 = (int)Difficulty.Total;
            itemRandom.r親ノード = parent;

            itemRandom.strBreadcrumbs = (itemRandom.r親ノード == null) ?
                itemRandom.strタイトル : itemRandom.r親ノード.strBreadcrumbs + " > " + itemRandom.strタイトル;

            itemRandom.arスコア[0] = new Cスコア();

            return itemRandom;
        }

        // Reset the position of all back buttons, also adds a random button at the end
        public static List<C曲リストノード> tReinsertBackButtons(C曲リストノード parent, List<C曲リストノード> songList, string path = "/", List<string> listStrBoxDef = null)
        {
            // Remove all the existing back boxes currently existing
            songList.RemoveAll(e => e.eノード種別 == C曲リストノード.Eノード種別.BACKBOX);

            int songCount = songList.Count;

            for (int index = 0; index < (songCount / 7) + 1; index++)
            {
                var backBox = tGenerateBackButton(parent, path, listStrBoxDef);
                songList.Insert(Math.Min(index * (7 + 1), songList.Count), backBox);
            }

            if (songCount > 0)
                songList.Add(tGenerateRandomButton(parent, path));

            // Return the reference in case of
            return songList;
        }

        // Generate the favorite folder content
        public static List<C曲リストノード> tFetchFavoriteFolder(C曲リストノード parent)
        {
            List<C曲リストノード> childList = new List<C曲リストノード>();

            foreach (string id in TJAPlayer3.Favorites.data.favorites[TJAPlayer3.SaveFile])
            {
                var node = tGetNodeFromID(id);
                if (node != null)
                {
                    node.r親ノード = parent;
                    node.isChangedBgType = parent.isChangedBgType;
                    node.isChangedBgColor = parent.isChangedBgColor;
                    node.isChangedBoxType = parent.isChangedBoxType;
                    node.isChangedBoxColor = parent.isChangedBoxColor;

                    node.ForeColor = parent.ForeColor;
                    node.BackColor = parent.BackColor;
                    node.BoxColor = parent.BoxColor;
                    node.BgColor = parent.BgColor;

                    childList.Add(node);
                }
                    
            }

            // Generate back buttons
            
            string favPath = "./" + parent.strタイトル + "/";

            tReinsertBackButtons(parent, childList, favPath);

            return childList;
        }

        #endregion

    }
}
