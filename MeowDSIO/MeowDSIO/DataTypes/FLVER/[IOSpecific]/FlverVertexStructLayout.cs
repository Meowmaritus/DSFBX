using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverVertexStructLayout
    {
        public int Unknown1 { get; set; } = 0;
        public int Unknown2 { get; set; } = 0;
        public List<FlverVertexStructMember> Members { get; set; } = new List<FlverVertexStructMember>();

        public int GetVertexSize()
        {
            return FlverVertexStructLayout.CalculateStructSize(this);
        }

        public IEnumerable<FlverVertexStructMemberSemantic> GetIncludedSemantics()
        {
            var list = new List<FlverVertexStructMemberSemantic>();
            foreach (var member in Members)
            {
                if (!list.Contains(member.Semantic))
                    list.Add(member.Semantic);
            }
            return list;
        }

        public static int CalculateStructSize(FlverVertexStructLayout vsl)
        {
            var size = 0;
            foreach (var member in vsl.Members)
            {
                switch (member.ValueType)
                {
                    case FlverVertexStructMemberValueType.BoneIndicesStruct:
                        size += 4;
                        break;
                    case FlverVertexStructMemberValueType.BoneWeightsStruct:
                        size += sizeof(ushort) * 4;
                        break;
                    case FlverVertexStructMemberValueType.UV:
                        size += sizeof(ushort) * 2;
                        break;
                    case FlverVertexStructMemberValueType.UVPair:
                        size += (sizeof(ushort) * 2) * 2;
                        break;
                    case FlverVertexStructMemberValueType.Vector3:
                        size += sizeof(float) * 3;
                        break;
                    case FlverVertexStructMemberValueType.PackedVector4:
                        size += 4;
                        break;
                }
            }
            return size;
        }

        public static FlverVertexStructLayout Read(DSBinaryReader bin)
        {
            var val = new FlverVertexStructLayout();

            int memberCount = bin.ReadInt32();

            val.Unknown1 = bin.ReadInt32();
            val.Unknown2 = bin.ReadInt32();

            int startOffset = bin.ReadInt32();

            bin.StepIn(startOffset);
            {
                val.Members = new List<FlverVertexStructMember>();
                for (int i = 0; i < memberCount; i++)
                {
                    val.Members.Add(FlverVertexStructMember.Read(bin));
                }
            }

            return val;
        }
    }
}
