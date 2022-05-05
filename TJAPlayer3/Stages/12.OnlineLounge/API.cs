using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TJAPlayer3
{
    internal class API
    {

        public class APISongData
        {
            public string SongTitle;
        }

        #region [ContractResolver override for properties]

        public class SongContractResolver : DefaultContractResolver
        {
            private Dictionary<string, string> PropertyMappings { get; set; }

            public SongContractResolver(DBCDN.CDNData cdnData)
            {
                this.PropertyMappings = new Dictionary<string, string>
                {
                    {"SongTitle", cdnData.Hooks.title["default"]},
                };
            }

            protected override string ResolvePropertyName(string propertyName)
            {
                string resolvedName = null;
                var resolved = this.PropertyMappings.TryGetValue(propertyName, out resolvedName);
                return (resolved) ? resolvedName : base.ResolvePropertyName(propertyName);
            }
        }

        #endregion

        public API(DBCDN.CDNData selectedCDN)
        {
            cdnData = selectedCDN;
            FetchedSongsList = new APISongData[0];
        }

        public APISongData[] FetchedSongsList;

        public void tLoadSongsFromInternalCDN()
        {
            string url = cdnData.BaseUrl + cdnData.SongList;


            var _fetched = GetCallAPI(url);

            _fetched.Wait();

            if (_fetched.Result != null)
                FetchedSongsList = _fetched.Result;


        }

        #region [private]

        private DBCDN.CDNData cdnData;

        private async Task<APISongData[]> GetCallAPI(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.GetAsync(url).ConfigureAwait(false);

                    if (response != null)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                        var settings = new JsonSerializerSettings();
                        settings.ContractResolver = new SongContractResolver(cdnData);
                        return JsonConvert.DeserializeObject<APISongData[]>(jsonString, settings);
                    }
                }
            }
            catch (Exception e)
            {

            }
            return null;
        }


        #endregion

    }
}
