using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.TAE
{
    public class Animation : Data
    {
        public string FileName { get; set; } = "a00_0000.hkxwin";
        public List<AnimationEvent> Events { get; set; } = new List<AnimationEvent>();
        //These are the unknown 1/2 in the anim file struct:
        public int Unk1 { get; set; } = 0;
        public int Unk2 { get; set; } = -2;

        public void UpdateEventIndices()
        {
            for (int i = 0; i < Events.Count; i++)
            {
                Events[i].DisplayIndex = i + 1;
            }
        }
    }
}
