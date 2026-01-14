using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDrive_Data_Migration_1
{
    internal class CsvUserMapping
    {
        public static Dictionary<string, string> LoadUserEmailMapping(string csvPath)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var lines = File.ReadAllLines(csvPath);

            for (int i = 1; i < lines.Length; i++) // skip header
            {
                var cols = lines[i].Split(',');
                if (cols.Length < 2) continue;

                string email = cols[0].Trim();
                string shadowAccount = cols[1].Trim();

                if (!map.ContainsKey(shadowAccount))
                    map.Add(shadowAccount, email);
            }

            return map;
        }
    }
}
