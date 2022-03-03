using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static List<C曲リストノード> tFetchFavoriteFolder(C曲リストノード parent)
        {
            List<C曲リストノード> childList = new List<C曲リストノード>();
            int increment = 0;

            
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
            return childList;
        }

        #endregion

    }
}
