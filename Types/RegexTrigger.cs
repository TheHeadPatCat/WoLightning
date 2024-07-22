using System;
using System.Text.RegularExpressions;

namespace WoLightning.Types
{
    
    public class RegexTrigger
    {
        public Guid GUID = Guid.NewGuid();
        public bool Enabled = false;
        public string Name = "";
        public string RegexString = "(?!)";
        public Regex? Regex = new Regex("(?!)");
        public int Mode = 0;
        public int Intensity = 1;
        public int Duration = 1;
    }
}
