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
        private static Dictionary<string, CSongListNode> nodes = new Dictionary<string, CSongListNode>();
        private static HashSet<string> urls = new HashSet<string>();

        public static CActSelect曲リスト.CScorePad[][] ScorePads = new CActSelect曲リスト.CScorePad[5][]
        {
            new CActSelect曲リスト.CScorePad[(int)Difficulty.Edit + 2] { new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad() },
            new CActSelect曲リスト.CScorePad[(int)Difficulty.Edit + 2] { new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad() },
            new CActSelect曲リスト.CScorePad[(int)Difficulty.Edit + 2] { new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad() },
            new CActSelect曲リスト.CScorePad[(int)Difficulty.Edit + 2] { new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad() },
            new CActSelect曲リスト.CScorePad[(int)Difficulty.Edit + 2] { new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad(), new CActSelect曲リスト.CScorePad() }
        };

        public static int tGetNodesCount()
        {
            return nodes.Count();
        }

        public static string[] tGetNodesByGenreName(string genreName)
        {
            return nodes.Where(_nd => _nd.Value.strジャンル == genreName).Select(_nd => _nd.Key).ToArray();
        }

        #region [General song dict methods]

        public static CSongListNode tGetNodeFromID(string id)
        {
            if (nodes.ContainsKey(id))
                return nodes[id].Clone();
            return null;
        }

        public static void tAddSongNode(CSongUniqueID sid, CSongListNode node)
        {
            if (sid != null && sid.data.id != null && sid.data.id != "" && !nodes.ContainsKey(sid.data.id))
                nodes.Add(sid.data.id, node.Clone());
            tAddSongUrl(sid);
        }

        public static bool tContainsSongUrl(string url)
        {
            return urls.Contains(url);
        }

        public static void tAddSongUrl(CSongUniqueID sid)
        {
            var url = sid.data.url;

            if (url != null && url != "" && !urls.Contains(url))
                urls.Add(url);
        }

        public static void tRemoveSongUrl(CSongUniqueID sid)
        {
            var url = sid.data.url;

            if (url != null && url != "" && urls.Contains(url))
                urls.Remove(url);
        }

        public static void tRemoveSongNode(CSongUniqueID sid)
        {
            if (sid != null && nodes.ContainsKey(sid.data.id))
            {
                tRemoveSongUrl(sid);
                nodes.Remove(sid.data.id);
            }
        }

        public static void tClearSongNodes()
        {
            nodes.Clear();
            urls.Clear();
        }

        #endregion

        #region [Extra methods]

        // Generate a back button
        public static CSongListNode tGenerateBackButton(CSongListNode parent, string path = "/", List<string> listStrBoxDef = null)
        {
            CSongListNode itemBack = new CSongListNode();
            itemBack.eノード種別 = CSongListNode.ENodeType.BACKBOX;


            // とじる
            itemBack.strタイトル = CLangManager.LangInstance.GetString(200) + " (" + path + ")";

            itemBack.BackColor = ColorTranslator.FromHtml("#513009");
            itemBack.BoxColor = Color.White;

            itemBack.BgColor = parent.BgColor;
            itemBack.isChangedBgColor = parent.isChangedBgColor;
            itemBack.BgType = parent.BgType;
            itemBack.isChangedBgType = parent.isChangedBgType;

            itemBack.strジャンル = parent.strジャンル;
            itemBack.strSelectBGPath = parent.strSelectBGPath;
            itemBack.nスコア数 = 1;
            itemBack.rParentNode = parent;
            itemBack.strSkinPath = (parent.rParentNode == null) ?
                "" : parent.rParentNode.strSkinPath;

            // I guess this is used to count the number of box.def instances and only at startup, which makes using it here pretty weird
            if (listStrBoxDef != null && itemBack.strSkinPath != "" && !listStrBoxDef.Contains(itemBack.strSkinPath))
            {
                listStrBoxDef.Add(itemBack.strSkinPath);
            }

            itemBack.strBreadcrumbs = (itemBack.rParentNode == null) ?
                itemBack.strタイトル : itemBack.rParentNode.strBreadcrumbs + " > " + itemBack.strタイトル;

            itemBack.arスコア[0] = new Cスコア();
            itemBack.arスコア[0].ファイル情報.フォルダの絶対パス = "";
            itemBack.arスコア[0].譜面情報.タイトル = itemBack.strタイトル;
            itemBack.arスコア[0].譜面情報.コメント = "";

            return (itemBack);
        }

        public static CSongListNode tGenerateRandomButton(CSongListNode parent, string path = "/")
        {
            CSongListNode itemRandom = new CSongListNode();
            itemRandom.eノード種別 = CSongListNode.ENodeType.RANDOM;

            itemRandom.strタイトル = CLangManager.LangInstance.GetString(203) + " (" + path + ")"; ;

            itemRandom.nスコア数 = (int)Difficulty.Total;
            itemRandom.rParentNode = parent;

            itemRandom.strBreadcrumbs = (itemRandom.rParentNode == null) ?
                itemRandom.strタイトル : itemRandom.rParentNode.strBreadcrumbs + " > " + itemRandom.strタイトル;

            itemRandom.arスコア[0] = new Cスコア();

            return itemRandom;
        }

        // Reset the position of all back buttons, also adds a random button at the end
        public static List<CSongListNode> tReinsertBackButtons(CSongListNode parent, List<CSongListNode> songList, string path = "/", List<string> listStrBoxDef = null)
        {
            // Remove all the existing back boxes currently existing
            songList.RemoveAll(e => e.eノード種別 == CSongListNode.ENodeType.BACKBOX || e.eノード種別 == CSongListNode.ENodeType.RANDOM);

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


        private static CSongListNode tReadaptChildNote(CSongListNode parent, CSongListNode node)
        {
            if (node != null)
            {
                node.rParentNode = parent;
                node.isChangedBgType = parent.isChangedBgType;
                node.isChangedBgColor = parent.isChangedBgColor;
                node.isChangedBoxType = parent.isChangedBoxType;
                node.isChangedBoxColor = parent.isChangedBoxColor;

                node.ForeColor = parent.ForeColor;
                node.BackColor = parent.BackColor;
                node.BoxColor = parent.BoxColor;
                node.BgColor = parent.BgColor;
                node.BgType = parent.BgType;
                node.BoxType = parent.BoxType;

                return node;
            }
            return null;
        }

        // Generate the favorite folder content
        public static List<CSongListNode> tFetchFavoriteFolder(CSongListNode parent)
        {
            List<CSongListNode> childList = new List<CSongListNode>();

            foreach (string id in TJAPlayer3.Favorites.data.favorites[TJAPlayer3.SaveFile])
            {
                var node = tReadaptChildNote(parent, tGetNodeFromID(id));
                if (node != null)
                {
                    childList.Add(node);
                }
                    
            }

            // Generate back buttons
            
            string favPath = "./" + parent.strタイトル + "/";

            tReinsertBackButtons(parent, childList, favPath);

            return childList;
        }

        // Generate recently played songs folder
        public static List<CSongListNode> tFetchRecentlyPlayedSongsFolder(CSongListNode parent)
        {
            List<CSongListNode> childList = new List<CSongListNode>();

            foreach (string id in TJAPlayer3.RecentlyPlayedSongs.data.recentlyplayedsongs[TJAPlayer3.SaveFile].Reverse())
            {
                var node = tReadaptChildNote(parent, tGetNodeFromID(id));
                if (node != null)
                {
                    childList.Add(node);
                }

            }

            // Generate back buttons

            string favPath = "./" + parent.strタイトル + "/";

            tReinsertBackButtons(parent, childList, favPath);

            return childList;
        }

        // 13 includes any higher difficulty
        private static bool tLevelMatches(int check, int level)
        {
            if (level == 13)
                return check >= level;
            return check == level;
        }

        // Generate search by difficulty folder
        public static List<CSongListNode> tFetchSongsByDifficulty(CSongListNode parent, int difficulty = (int)Difficulty.Oni, int level = 8)
        {
            List<CSongListNode> childList = new List<CSongListNode>();

            foreach (CSongListNode nodeT in nodes.Values)
            {
                var score = nodeT.nLevel;
                if (tLevelMatches(score[difficulty], level)
                    || (difficulty == (int)Difficulty.Oni && tLevelMatches(score[(int)Difficulty.Edit], level))) // Oni includes Ura
                {
                    var node = tReadaptChildNote(parent, nodeT);
                    if (node != null)
                    {
                        childList.Add(node);
                    }
                }
            }

            // Generate back buttons

            string favPath = "./" + parent.strタイトル + "/";

            tReinsertBackButtons(parent, childList, favPath);

            return childList;
        }

        #endregion

        #region [Score tables methods]

        public static void tRefreshScoreTables()
        {
            #region [Reset nodes]

            for (int pl = 0; pl < 5; pl++)
            {
                CActSelect曲リスト.CScorePad[] SPArrRef = ScorePads[pl];

                for (int s = 0; s <= (int)Difficulty.Edit + 1; s++)
                {
                    CActSelect曲リスト.CScorePad SPRef = SPArrRef[s];

                    for (int i = 0; i < SPRef.ScoreRankCount.Length; i++)
                        SPRef.ScoreRankCount[i] = 0;

                    for (int i = 0; i < SPRef.CrownCount.Length; i++)
                        SPRef.CrownCount[i] = 0;
                }
            }

            #endregion

            #region [Load nodes]

            foreach (CSongListNode song in nodes.Values)
            {
                for (int pl = 0; pl < 5; pl++)
                {
                    CActSelect曲リスト.CScorePad[] SPArrRef = ScorePads[pl];

                    if (song.eノード種別 == CSongListNode.ENodeType.SCORE
                        && song.strジャンル != "最近遊んだ曲"
                        && song.strジャンル != "Favorite"
                        && song.strジャンル != "SearchD")
                    {
                        var score = song.arスコア[TJAPlayer3.stageSongSelect.actSongList.n現在のアンカ難易度レベルに最も近い難易度レベルを返す(song)];

                        if (score != null)
                        {
                            var gp = score.GPInfo[pl];

                            for (int s = 0; s <= (int)Difficulty.Edit; s++)
                            {
                                CActSelect曲リスト.CScorePad SPRef = SPArrRef[s];

                                for (int i = 0; i < SPRef.ScoreRankCount.Length; i++)
                                {
                                    int increment = (gp.nScoreRank[s] == i + 1) ? 1 : 0;

                                    SPRef.ScoreRankCount[i] += increment;
                                    if (s >= (int)Difficulty.Oni)
                                        SPArrRef[(int)Difficulty.Edit + 1].ScoreRankCount[i] += increment;
                                }
                                for (int i = 0; i < SPRef.CrownCount.Length; i++)
                                {
                                    int increment = (gp.nClear[s] == i + 1) ? 1 : 0;

                                    SPRef.CrownCount[i] += increment;
                                    if (s >= (int)Difficulty.Oni)
                                        SPArrRef[(int)Difficulty.Edit + 1].CrownCount[i] += increment;
                                }
                                
                            }

                            
                        }
                    }
                }
            }

            #endregion


        }

        #endregion
    }
}
