using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    internal class CLocalizationData
    {
        [JsonProperty("strings")]
        private Dictionary<string, string> Strings = new Dictionary<string, string>();

        public CLocalizationData()
        {
            Strings = new Dictionary<string, string>();
        }

        public string GetString(string defaultsDefault)
        {
            string _lang = CLangManager.fetchLang();
            if (Strings.ContainsKey(_lang))
                return Strings[_lang];
            else if (Strings.ContainsKey("default"))
                return Strings["default"];
            return defaultsDefault;
        }
    }
}
