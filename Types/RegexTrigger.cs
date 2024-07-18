﻿using System;
using System.Text.RegularExpressions;

namespace WoLightning.Types
{
    public enum OpType
    {
        Shock = 0,
        Vibrate = 1,
        Beep = 2
    }
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
