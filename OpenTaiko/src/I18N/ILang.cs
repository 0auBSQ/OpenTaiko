using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3
{
    internal interface ILang
    {
        string GetString(int idx);
    }

    static internal class CLangManager
    {
        // Cheap factory-like design pattern

        public static (string, int) DefaultLanguage = ("ja", 0);
        public static CLang LangInstance { get; private set; } = new CLang(Langcodes.FirstOrDefault("ja"));
        public static void langAttach(string lang)
        {
            LangInstance = CLang.GetCLang(lang);

            //switch (lang) {
            //    case "zh":
            //        CLangManager.LangInstance = new CLang_zh();
            //        break;
            //    case "es":
            //        CLangManager.LangInstance = new CLang_es();
            //        break;
            //    case "fr":
            //        CLangManager.LangInstance = new CLang_fr();
            //        break;
            //    case "nl":
            //        CLangManager.LangInstance = new CLang_nl();
            //        break;
            //    case "ko":
            //        CLangManager.LangInstance = new CLang_ko();
            //        break;
            //    case "en":
            //        CLangManager.LangInstance = new CLang_en();
            //        break;
            //    case "ja":
            //    default:
            //        CLangManager.LangInstance = new CLang_jp();
            //        break;
            //}
        }

        public static int langToInt(string lang)
        {
            return Array.IndexOf(Langcodes, lang);
        }

        public static string fetchLang()
        {
            //if (LangInstance is CLang_jp)
            //    return "ja";
            //else if (LangInstance is CLang_en)
            //    return "en";
            //else if (LangInstance is CLang_fr)
            //    return "fr";
            //else if (LangInstance is CLang_es)
            //    return "es";
            //else if (LangInstance is CLang_zh)
            //    return "zh";
            //else if (LangInstance is CLang_nl)
            //    return "nl";
            //else if (LangInstance is CLang_ko)
            //    return "ko";
            //return DefaultLanguage.Item1;
            return LangInstance.Id;
        }

        public static string intToLang(int idx)
        {
            return Langcodes[idx];
        }

        //public static readonly string[] Languages = new string[] { "日本語 (Japanese)", "English", "Français (French)", "Español (Spanish)", "中文 (Chinese)", "nl (WIP)", "ko (WIP)" };
        //public static readonly string[] Langcodes = new string[] { "ja", "en", "fr", "es", "zh", "nl", "ko" };
        // temporary garbage code
        public static string[] Langcodes
        { 
            get
            {
                if (_langCodes == null)
                    _langCodes = Directory.GetDirectories(Path.Combine(TJAPlayer3.strEXEのあるフォルダ, "Lang"), "*", SearchOption.TopDirectoryOnly)
                    .Select(result => Path.GetRelativePath(Path.Combine(TJAPlayer3.strEXEのあるフォルダ, "Lang"), result))
                    .ToArray();

                return _langCodes;
            }
        }
        public static string[] Languages
        {
            get
            {
                if (_languages == null)
                    _languages = Langcodes.Select(result => CLang.GetLanguage(result)).ToArray();

                return _languages;
            }
        }

        private static string[] _langCodes;
        private static string[] _languages;
        //public static ILang LangInstance { get; private set; }  = new CLang_jp();
    }
}