using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverVertexStructLayoutCheck
    {
        public List<FlverVertexStructMemberSemantic> IncludedSemantics { get; set; } = new List<FlverVertexStructMemberSemantic>();

        public FlverVertexStructLayoutCheck()
        {

        }

        public FlverVertexStructLayoutCheck(FlverVertex vert)
        {
            ApplyCheck(vert);
        }

        public static int AddToListOnlyIfUniqueAndReturnIndex(List<FlverVertexStructLayoutCheck> list, FlverVertexStructLayoutCheck item)
        {
            var identicalItems = list.Where(x => x.IncludedSemantics.SequenceEqual(item.IncludedSemantics));
            if (identicalItems.Any())
            {
                return list.IndexOf(identicalItems.First());
            }
            else
            {
                list.Add(item);
                return list.Count - 1;
            }
        }

        public void ApplyCheck(FlverVertex vert)
        {
            if (vert.Position != null)
                IncludedSemantics.Add(FlverVertexStructMemberSemantic.Position);

            if (vert.BoneIndices != null)
                IncludedSemantics.Add(FlverVertexStructMemberSemantic.BoneIndices);

            if (vert.BoneWeights != null)
                IncludedSemantics.Add(FlverVertexStructMemberSemantic.BoneWeights);

            if (vert.Normal != null)
                IncludedSemantics.Add(FlverVertexStructMemberSemantic.Normal);

            if (vert.BiTangent != null)
                IncludedSemantics.Add(FlverVertexStructMemberSemantic.BiTangent);

            if (vert.UnknownVector4A != null)
                IncludedSemantics.Add(FlverVertexStructMemberSemantic.UnknownVector4A);

            if (vert.VertexColor != null)
                IncludedSemantics.Add(FlverVertexStructMemberSemantic.VertexColor);

            foreach (var uv in vert.UVs)
            {
                IncludedSemantics.Add(FlverVertexStructMemberSemantic.UV);
            }
        }

        public FlverVertexStructLayout BuildVertexStructLayout()
        {
            var structLayout = new FlverVertexStructLayout();
            var currentOffset = 0;

            void _newMember(FlverVertexStructMemberSemantic s, FlverVertexStructMemberValueType v)
            {
                var newMember = new FlverVertexStructMember();
                newMember.Semantic = s;
                newMember.ValueType = v;
                newMember.StructOffset = currentOffset;

                switch (v)
                {
                    case FlverVertexStructMemberValueType.BoneIndicesStruct:
                        currentOffset += 4;
                        break;
                    case FlverVertexStructMemberValueType.BoneWeightsStruct:
                        currentOffset += sizeof(ushort) * 4;
                        break;
                    case FlverVertexStructMemberValueType.UV:
                        currentOffset += sizeof(ushort) * 2;
                        break;
                    case FlverVertexStructMemberValueType.UVPair:
                        currentOffset += (sizeof(ushort) * 2) * 2;
                        break;
                    case FlverVertexStructMemberValueType.Vector3:
                        currentOffset += sizeof(float) * 3;
                        break;
                    case FlverVertexStructMemberValueType.PackedVector4:
                        currentOffset += 4;
                        break;
                }

                structLayout.Members.Add(newMember);
            }

            var semanticsQueue = new Queue<FlverVertexStructMemberSemantic>(IncludedSemantics);

            while (semanticsQueue.Count > 0)
            {
                var semantic = semanticsQueue.Dequeue();
                switch (semantic)
                {
                    case FlverVertexStructMemberSemantic.Position:
                        _newMember(FlverVertexStructMemberSemantic.Position,
                            FlverVertexStructMemberValueType.Vector3);
                        break;
                    case FlverVertexStructMemberSemantic.BoneIndices:
                        _newMember(FlverVertexStructMemberSemantic.BoneIndices,
                            FlverVertexStructMemberValueType.BoneIndicesStruct);
                        break;
                    case FlverVertexStructMemberSemantic.BoneWeights:
                        _newMember(FlverVertexStructMemberSemantic.BoneWeights,
                            FlverVertexStructMemberValueType.BoneWeightsStruct);
                        break;
                    case FlverVertexStructMemberSemantic.Normal:
                        _newMember(FlverVertexStructMemberSemantic.Normal,
                            FlverVertexStructMemberValueType.PackedVector4);
                        break;
                    case FlverVertexStructMemberSemantic.BiTangent:
                        _newMember(FlverVertexStructMemberSemantic.BiTangent,
                            FlverVertexStructMemberValueType.PackedVector4);
                        break;
                    case FlverVertexStructMemberSemantic.VertexColor:
                        _newMember(FlverVertexStructMemberSemantic.VertexColor,
                            FlverVertexStructMemberValueType.PackedVector4);
                        break;
                    case FlverVertexStructMemberSemantic.UV:
                        //If followed by another UV:
                        if (semanticsQueue.Count > 0 && semanticsQueue.Peek() == FlverVertexStructMemberSemantic.UV)
                        {
                            _newMember(FlverVertexStructMemberSemantic.UV,
                                FlverVertexStructMemberValueType.UVPair);

                            //Consume the next UV along with the current one:
                            semanticsQueue.Dequeue();
                        }
                        else
                        {
                            _newMember(FlverVertexStructMemberSemantic.UV,
                                FlverVertexStructMemberValueType.UV);
                        }
                        break;
                    case FlverVertexStructMemberSemantic.UnknownVector4A:
                        _newMember(FlverVertexStructMemberSemantic.UnknownVector4A,
                            FlverVertexStructMemberValueType.PackedVector4);
                        break;
                }
            }

            return structLayout;
        }
    }
}
