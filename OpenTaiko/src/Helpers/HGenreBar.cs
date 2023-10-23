using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using FDK;


namespace TJAPlayer3
{
    class HGenreBar
    {
        public static CTexture tGetGenreBar(string value, Dictionary<string, CTexture> textures)
        {
            if (textures.TryGetValue($"{value}", out CTexture tex))
            {
                return tex;
            }
            else
            {
                if (textures.TryGetValue("0", out CTexture tex2))
                {
                    return tex2;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}