using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;

namespace TJAPlayer3
{
    public static class HDatabaseHelpers
    {
        public static List<string> GetAvailableLanguage(SqliteConnection connection, string prefix)
        {
            List<string> _translations = new List<string>();
            foreach (string cd in CLangManager.Langcodes)
            {
                SqliteCommand chk = connection.CreateCommand();
                chk.CommandText =
                @$"
                        SELECT count(*) FROM sqlite_master WHERE type='table' AND name='{prefix}_{cd}';
                    ";
                SqliteDataReader chkreader = chk.ExecuteReader();
                while (chkreader.Read())
                {
                    if (chkreader.GetInt32(0) > 0)
                        _translations.Add(cd);
                }
            }
            return _translations;
        }
    }
}
