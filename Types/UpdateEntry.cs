using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoLightning.Types
{
    public class UpdateEntry
    {
        public int Version { get; set; }
        public bool breaksConfig { get; set; }
        public bool breaksAuthentification { get; set; }
        public List<String> Changes { get; set; }

        public UpdateEntry() {
            
        }

    }
}
