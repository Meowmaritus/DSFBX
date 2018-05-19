using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.TAE
{
    public class TAEHeader
    {
        //"TAE "
        [JsonConverter(typeof(Json.ByteArrayConverter))]
        public byte[] Signature { get; set; } = { 0x54, 0x41, 0x45, 0x20 };

        public bool IsBigEndian { get; set; } = false;

        //3 Null bytes

        public ushort VersionMajor { get; set; } = 11;
        public ushort VersionMinor { get; set; } = 1;

        //(uint FileLength)

        public uint UnknownB00 { get; set; } = 64; //0x00000040
        public uint UnknownB01 { get; set; } = 1; //0x00000001
        public uint UnknownB02 { get; set; } = 80; //0x00000050
        public uint UnknownB03 { get; set; } = 112; //0x00000070

        public const int UnknownFlagsLength = 0x30;

        public byte[] UnknownFlags { get; set; } = new byte[UnknownFlagsLength]
        {
            0x00, 0x01, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
            0x02, 0x01, 0x00, 0x01,
            0x02, 0x01, 0x00, 0x02,
            0x00, 0x00, 0x00, 0x01,
            0x00, 0x00, 0x00, 0x00,
        };

        public int FileID { get; set; } = 204100;
        public int UnknownC { get; set; } = 0x00000090;

        public uint UnknownE00 = 0;
        public uint UnknownE01 = 1;
        public uint UnknownE02 = 128;
        public uint UnknownE03 = 0;
        public uint UnknownE04 = 0;

        public int FileID2 = 204100;
        public int FileID3 = 204100;

        public uint UnknownE07 = 0x00000050u;
        public uint UnknownE08 = 0x00000000u;
        public uint UnknownE09 = 0x00000000u;

        
    }
}
