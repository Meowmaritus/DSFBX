using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverVertexStructMember
    {
        public int Unknown1 { get; set; } = 0;
        public int StructOffset { get; set; } = 0;
        public FlverVertexStructMemberValueType ValueType { get; set; } = FlverVertexStructMemberValueType.INVALID;
        public FlverVertexStructMemberSemantic Semantic { get; set; } = FlverVertexStructMemberSemantic.INVALID;
        public int Index { get; set; } = 0;

        public static FlverVertexStructMember Read(DSBinaryReader bin)
        {
            var val = new FlverVertexStructMember();

            val.Unknown1 = bin.ReadInt32();
            val.StructOffset = bin.ReadInt32();
            val.ValueType = (FlverVertexStructMemberValueType)bin.ReadInt32();
            val.Semantic = (FlverVertexStructMemberSemantic)bin.ReadInt32();
            val.Index = bin.ReadInt32();

            return val;
        }

        public static void Write(DSBinaryWriter bin, FlverVertexStructMember val)
        {
            bin.Write(val.Unknown1);
            bin.Write(val.StructOffset);
            bin.Write((int)val.ValueType);
            bin.Write((int)val.Semantic);
            bin.Write(val.Index);
        }
    }
}
