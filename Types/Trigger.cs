using System;
using System.Text.RegularExpressions;

namespace WoLightning.Types
{
    public enum OpType
    {
        Shock,
        Vibrate,
        Beep
    }
    public class Trigger
    {
        public Guid GUID = Guid.NewGuid();
        public bool Enabled = false;
        public string Name = "";
        public string RegexString = "(?!)";
        public Regex? Regex = null;
        public int Intensity = 1;
        public int Duration = 1;
    }
}
