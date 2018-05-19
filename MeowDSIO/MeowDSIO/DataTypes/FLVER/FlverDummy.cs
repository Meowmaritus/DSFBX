using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverDummy
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        [Newtonsoft.Json.JsonIgnore]
        public DataFiles.FLVER ContainingFlver { get; set; }

        public FlverDummy(DataFiles.FLVER ContainingFlver)
        {
            this.ContainingFlver = ContainingFlver;
        }

        public Vector3 Position { get; set; }

        public byte UnknownByte1 { get; set; } = 0xFF;
        public byte UnknownByte2 { get; set; } = 0xFF;

        public short UnknownShort1 { get; set; } = -1;

        public Vector3 Row2 { get; set; }
        public short TypeID { get; set; } = -1;
        public short ParentBoneIndex { get; set; } = -1;

        public Vector3 Row3 { get; set; }
        public short SomeSortOfParentIndex { get; set; } = -1;
        public bool UnknownFlag1 { get; set; } = false;
        public bool UnknownFlag2 { get; set; } = true;

        //public Vector3 Row4 { get; set; } = new Vector3(0, 0, 0);
        //public short Row4_ID1 { get; set; } = 0;
        //public short Row4_ID2 { get; set; } = 0;

        public override string ToString()
        {
            return ContainingFlver.GetBoneFromIndex(ParentBoneIndex, true)?.Name ?? $"Invalid Bone Index: {ParentBoneIndex}";
        }

        public static FlverDummy Read(DSBinaryReader bin, DataFiles.FLVER ContainingFlver)
        {
            var dmy = new FlverDummy(ContainingFlver);

            dmy.Position = bin.ReadVector3();
            dmy.UnknownByte1 = bin.ReadByte();
            dmy.UnknownByte2 = bin.ReadByte();
            dmy.UnknownShort1 = bin.ReadInt16();

            dmy.Row2 = bin.ReadVector3();
            dmy.TypeID = bin.ReadInt16();
            dmy.ParentBoneIndex = bin.ReadInt16();

            dmy.Row3 = bin.ReadVector3();
            dmy.SomeSortOfParentIndex = bin.ReadInt16();
            dmy.UnknownFlag1 = bin.ReadBoolean();
            dmy.UnknownFlag2 = bin.ReadBoolean();

            //hit.Row4 = bin.ReadVector3();
            //hit.Row4_ID1 = bin.ReadInt16();
            //hit.Row4_ID2 = bin.ReadInt16();

            bin.ReadBytes(16);

            return dmy;
        }

        public static void Write(DSBinaryWriter bin, FlverDummy dmy)
        {
            bin.Write(dmy.Position);
            bin.Write(dmy.UnknownByte1);
            bin.Write(dmy.UnknownByte2);
            bin.Write(dmy.UnknownShort1);

            bin.Write(dmy.Row2);
            bin.Write(dmy.TypeID);
            bin.Write(dmy.ParentBoneIndex);

            bin.Write(dmy.Row3);
            bin.Write(dmy.SomeSortOfParentIndex);
            bin.Write(dmy.UnknownFlag1);
            bin.Write(dmy.UnknownFlag2);

            //bin.Write(hit.Row4);
            //bin.Write(hit.Row4_ID1);
            //bin.Write(hit.Row4_ID2);

            bin.Write(new byte[16]);
        }

    }

}
