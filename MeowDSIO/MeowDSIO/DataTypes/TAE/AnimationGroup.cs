using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.TAE
{
    public class AnimationGroup : Data
    {
        [JsonIgnore]
        public int DisplayIndex { get; set; }


        public int FirstID { get; set; }
        public int LastID { get; set; }

        public AnimationGroup(int dispIndex)
        {
            DisplayIndex = dispIndex;
        }
    }
}
