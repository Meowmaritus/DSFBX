using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverHeader
    {
        public bool IsBigEndian { get; set; } = false;
        public double Version { get; set; } = 12.2;

        public Vector3 BoundingBoxMin { get; set; } = Vector3.Zero;
        public Vector3 BoundingBoxMax { get; set; } = Vector3.Zero;

        public int Unknown0x40 { get; set; } = 2;
        public int Unknown0x44 { get; set; } = 2;
        public int Unknown0x48 { get; set; } = 272;
        public int Unknown0x4C { get; set; } = 0;

        public int Unknown0x5C { get; set; } = 0;
        public int Unknown0x60 { get; set; } = 0;
        public int Unknown0x64 { get; set; } = 0;
        public int Unknown0x68 { get; set; } = 0;
        public int Unknown0x6C { get; set; } = 0;
        public int Unknown0x70 { get; set; } = 0;
        public int Unknown0x74 { get; set; } = 0;
        public int Unknown0x78 { get; set; } = 0;
        public int Unknown0x7C { get; set; } = 0;

    }
}
