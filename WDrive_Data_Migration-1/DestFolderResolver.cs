using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDrive_Data_Migration_1
{
    internal class DestFolderResolver
    {
        public static string? GetLocalPartOfEmail(string email, string userId, string trimToken)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userId))
                return null;

            email = email.Trim();
            userId = userId.Trim();

            bool containsAtSym = email.Contains("atsym", StringComparison.OrdinalIgnoreCase);
            bool containsDotCom = email.Contains(".com", StringComparison.OrdinalIgnoreCase);

            if (containsAtSym && containsDotCom)
            {
                int tokenIndex = userId.IndexOf(trimToken, StringComparison.OrdinalIgnoreCase);
                return userId.Substring(0, tokenIndex);
            }

            return email;
        }
    }
}
