using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverFaceSet
    {
        public FlverFaceSetFlags Flags { get; set; } = FlverFaceSetFlags.None;

        public bool IsTriangleStrip { get; set; } = false;

        public bool CullBackfaces { get; set; } = true;

        public List<ushort> VertexIndices { get; set; } = new List<ushort>();

        public byte UnknownByte1 { get; set; } = 0;
        public byte UnknownByte2 { get; set; } = 0;

        public int UnknownInt1 { get; set; } = 0;
        public int UnknownInt2 { get; set; } = 0;
        public int UnknownInt3 { get; set; } = 0;

    }
}
