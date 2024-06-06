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
        public static void langAttach(string lang)
        {
            switch (lang) {
                case "zh":
                    CLangManager.LangInstance = new CLang_zh();
                    break;
                case "es":
                    CLangManager.LangInstance = new CLang_es();
                    break;
                case "fr":
                    CLangManager.LangInstance = new CLang_fr();
                    break;
                case "nl":
                    CLangManager.LangInstance = new CLang_nl();
                    break;
                case "ko":
                    CLangManager.LangInstance = new CLang_ko();
                    break;
                case "en":
                    CLangManager.LangInstance = new CLang_en();
                    break;
                case "ja":
                default:
                    CLangManager.LangInstance = new CLang_jp();
                    break;
            }
        }

        public static int langToInt(string lang)
        {
            switch (lang)
            {
                case "ko":
                    return 6;
                case "nl":
                    return 5;
                case "zh":
                    return 4;
                case "es":
                    return 3;
                case "fr":
                    return 2;
                case "en":
                    return 1;
                case "ja":
                default:
                    return DefaultLanguage.Item2;
            }
        }

        public static string fetchLang()
        {
            if (LangInstance is CLang_jp)
                return "ja";
            else if (LangInstance is CLang_en)
                return "en";
            else if (LangInstance is CLang_fr)
                return "fr";
            else if (LangInstance is CLang_es)
                return "es";
            else if (LangInstance is CLang_zh)
                return "zh";
            else if (LangInstance is CLang_nl)
                return "nl";
            else if (LangInstance is CLang_ko)
                return "ko";
            return DefaultLanguage.Item1;
        }

        public static string intToLang(int idx)
        {
            switch (idx)
            {
                case 6:
                    return "ko";
                case 5:
                    return "nl";
                case 4:
                    return "zh";
                case 3:
                    return "es";
                case 2:
                    return "fr";
                case 1:
                    return "en";
                case 0:
                default:
                    return DefaultLanguage.Item1;
            }
        }

        public static readonly string[] Languages = new string[] { "日本語 (Japanese)", "English", "Français (French)", "Español (Spanish)", "中文 (Chinese)", "nl (WIP)", "ko (WIP)" };
        public static readonly string[] Langcodes = new string[] { "ja", "en", "fr", "es", "zh", "nl", "ko" };
        public static ILang LangInstance { get; private set; }  = new CLang_jp();
    }
}