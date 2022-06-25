using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    class DBCDN : CSavableT<Dictionary<string, DBCDN.CDNData>>
    {
        public DBCDN()
        {
            _fn = @".\Databases\CDN.json";
            base.tDBInitSavable();
        }

        #region [Auxiliary classes]

        public class CDNHooks
        {
            public string id = "id";
            public Dictionary<string, string> title = new Dictionary<string, string>()
            {
                ["default"] = "title",
            };
            public Dictionary<string, string> subtitle = new Dictionary<string, string>()
            {
                ["default"] = "subtitle",
            };
            public string[] difficulties = { "easy", "normal", "hard", "extreme", "extra", "tower", "dan" };
            public string life = "life";
            public string updateDate = "updateDate";
            public string creationDate = "creationDate";
            public string uploadDate = "uploadDate";
            public Dictionary<string, string> md5 = new Dictionary<string, string>()
            {
                ["default"] = "md5",
            };
            public string genre = "genre";
            public Dictionary<string, string> genreSub = new Dictionary<string, string>()
            {
                ["default"] = "name",
            };

            public string charter = "charter";
        }

        public class CDNData
        {

            [JsonProperty("baseUrl")]
            public string BaseUrl;

            [JsonProperty("download")]
            public Dictionary<string, string> Download = new Dictionary<string, string>()
            {
                ["default"] = "download/",
            };

            [JsonProperty("songList")]
            public string SongList;

            [JsonProperty("hooks")]
            public CDNHooks Hooks;
        }

        #endregion

    }
}