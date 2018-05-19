using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverMaterialParameter
    {
        public string Value { get; set; } = null;
        public string Name { get; set; } = null;

        public float UnknownFloat1 { get; set; } = 1;
        public float UnknownFloat2 { get; set; } = 1;

        public byte UnknownByte1 { get; set; } = 1;
        public byte UnknownByte2 { get; set; } = 1;
        public byte UnknownByte3 { get; set; } = 0;
        public byte UnknownByte4 { get; set; } = 0;

        public int UnknownInt1 { get; set; } = 0;
        public int UnknownInt2 { get; set; } = 0;
        public int UnknownInt3 { get; set; } = 0;

        public override string ToString()
        {
            return $"{Name} = \"{Value}\"";
        }
    }
}
