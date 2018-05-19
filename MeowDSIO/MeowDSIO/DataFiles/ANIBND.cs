using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataFiles
{
    public class ANIBND : DataFile
    {
        public Dictionary<int, byte[]> AnimationHKXs { get; set; } = new Dictionary<int, byte[]>();
        public DataFiles.TAE TimeAct { get; set; } = null;

        public const int ID_SKELETON_START = 1000000;
        public const int ID_SKELETON_END = 1099999;

        public const int ID_TAE_START = 3000000;
        public const int ID_TAE_END = 3099999;

        public const int ID_PLAYER_SKELETON_START = 4000000;
        public const int ID_PLAYER_SKELETON_END = 4099999;

        public const int ID_PLAYER_TAE_START = 5000000;
        public const int ID_PLAYER_TAE_END = 5099999;

        public const int ID_PLAYER_TXT_START = 6000000;
        public const int ID_PLAYER_TXT_END = 6199999;

        public const int ID_PLAYER_TXT_DELAYLOAD_START = 6200000;
        public const int ID_PLAYER_TXT_DELAYLOAD_END = 6299999;

        protected override void Read(DSBinaryReader bin, IProgress<(int, int)> prog)
        {
            throw new NotImplementedException();
        }

        protected override void Write(DSBinaryWriter bin, IProgress<(int, int)> prog)
        {
            throw new NotImplementedException();
        }
    }
}
