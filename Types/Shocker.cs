using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WoLightning.Types
{
    public enum ShockerStatus
    {
        Unchecked = 0,

        Online = 1,
        Paused = 2,
        Offline = 3,
        
        NotAuthorized = 100,
        DoesntExist = 101,
        AlreadyUsed = 102,
    }

    [Serializable]
    public class Shocker
    {
        public String Name { get; set; }
        public String Code { get; set; }

        [NonSerialized]
        public ShockerStatus Status = ShockerStatus.Unchecked;

        public Shocker(string name, string code)
        {
            Name = name;
            Code = code;
        }

    }
}
