using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class SkinConfigJson
    {
        [JsonInclude]
        [JsonPropertyName("name")]
        public string Name { get; private set; } = "";

        [JsonInclude]
        [JsonPropertyName("description")]
        public string Description { get; private set; } = "";

        [JsonInclude]
        [JsonPropertyName("version")]
        public string Version { get; private set; } = "";

        [JsonInclude]
        [JsonPropertyName("creator")]
        public string Creators { get; private set; } = "Unknown";

        [JsonInclude]
        [JsonPropertyName("width")]
        public int Width { get; private set; } = 1280;

        [JsonInclude]
        [JsonPropertyName("height")]
        public int Height { get; private set; } = 720;

        [JsonInclude]
        [JsonPropertyName("nameplate_titletypes")]
        public string[] NamePlate_TitleTypes { get; private set; } = new string[1] { "0" };
    }
}
