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
        public static void langAttach(string lang)
        {
            switch (lang) {
                case "fr":
                    CLangManager.LangInstance = new CLang_fr();
                    break;
                case "en":
                    CLangManager.LangInstance = new CLang_en();
                    break;
                case "jp":
                default:
                    CLangManager.LangInstance = new CLang_jp();
                    break;
            }
        }

        public static int langToInt(string lang)
        {
            switch (lang)
            {
                case "fr":
                    return 2;
                case "en":
                    return 1;
                case "jp":
                default:
                    return 0;
            }
        }

        public static string intToLang(int idx)
        {
            switch (idx)
            {
                case 2:
                    return "fr";
                case 1:
                    return "en";
                case 0:
                default:
                    return "jp";
            }
        }

        public static string[] Languages = new string[] { "日本語", "English", "Français" };
        public static ILang LangInstance = new CLang_jp();
    }
}