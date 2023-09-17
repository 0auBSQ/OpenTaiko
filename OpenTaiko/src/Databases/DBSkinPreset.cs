using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    internal class DBSkinPreset
    {
        public class SkinScene
        {
            public SkinScene()
            {
                UpperBackground = null;
                LowerBackground = null;
                DancerSet = null;
            }

            [JsonProperty("UP")]
            public string UpperBackground;

            [JsonProperty("DOWN")]
            public string LowerBackground;

            [JsonProperty("DANCER")]
            public string DancerSet;
        }
        public class SkinPreset
        {
            public SkinPreset()
            {
                Regular = new Dictionary<string, SkinScene>();
                Dan = new Dictionary<string, SkinScene>();
            }


            [JsonProperty("Regular")]
            public Dictionary<string, SkinScene> Regular;

            [JsonProperty("Dan")]
            public Dictionary<string, SkinScene> Dan;

        }
    }
}
