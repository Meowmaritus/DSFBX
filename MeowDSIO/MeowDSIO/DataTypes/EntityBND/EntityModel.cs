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
        public Dictionary<string, int> TextureFlags { get; set; } = new Dictionary<string, int>();
        public DataFiles.FLVER Mesh { get; set; } = new DataFiles.FLVER();
        public byte[] BodyHKX { get; set; } = null;
        //public ANIBND AnimContainer { get; set; } = null;
        public bool IncludesAnimContainerDummy { get; set; } = false;
        public DataFiles.BND AnimContainer { get; set; } = null;
        public byte[] HKXPWV { get; set; } = null;
        public byte[] BSIPWV { get; set; } = null;
        public byte[] ClothHKX { get; set; } = null;
        public byte[] CHRTPFBHD { get; set; } = null;
    }
}
