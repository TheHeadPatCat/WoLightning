using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoLightning.Classes
{
    public class StringSanitizer
    {
        public static String LetterOrDigit(string s)
        {
            string output = String.Empty;
            foreach(char c in s.ToCharArray())
            {
                if (c != ' ' && c != '\'' && !Char.IsLetterOrDigit(c)) break;
                output += c;
            }
            return output;
        }

        public static String PlayerName(string s) //todo implement
        {
            return String.Empty;
        }
    }
}
