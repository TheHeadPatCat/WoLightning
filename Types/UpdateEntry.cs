using System;
using System.Collections.Generic;

namespace WoLightning.Types
{
    public class UpdateEntry
    {
        public int Version { get; set; }
        public bool breaksConfig { get; set; }
        public bool breaksAuthentification { get; set; }
        public List<String> Changes { get; set; }

        public UpdateEntry()
        {

        }

    }
}
