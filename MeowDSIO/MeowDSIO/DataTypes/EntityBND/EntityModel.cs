using MeowDSIO.DataTypes.BND;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.EntityBND
{
    public class EntityModel
    {
        public Dictionary<string, byte[]> Textures { get; set; } = new Dictionary<string, byte[]>();
        public Dictionary<string, uint> TextureFlags { get; set; } = new Dictionary<string, uint>();
        public DataFiles.FLVER Mesh { get; set; } = null;
        public byte[] BodyHKX { get; set; } = null;
        //public ANIBND AnimContainer { get; set; } = null;
        public DataFiles.BND AnimContainer { get; set; } = null;
        public bool IncludesAnimDummy { get; set; } = false;
        public byte[] HKXPWV { get; set; } = null;
        public byte[] BSIPWV { get; set; } = null;
        public byte[] ClothHKX { get; set; } = null;
        public byte[] CHRTPFBHD { get; set; } = null;
    }
}
