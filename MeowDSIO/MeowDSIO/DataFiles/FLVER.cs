using MeowDSIO.DataTypes.FLVER;
using MeowDSIO.Exceptions.DSWrite;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataFiles
{
    public class FLVER : DataFile
    {
        public FlverHeader Header { get; set; } = new FlverHeader();
        public List<FlverDummy> Dummies { get; set; } = new List<FlverDummy>();
        public List<FlverBone> Bones { get; set; } = new List<FlverBone>();
        public List<FlverSubmesh> Submeshes { get; set; } = new List<FlverSubmesh>();
        public List<FlverVertexStructLayout> VertexStructLayouts { get; set; } = new List<FlverVertexStructLayout>();

        //public List<FlverVertexStructLayout> DEBUG_LIST_VertexStructLayout { get; set; } = new List<FlverVertexStructLayout>();
        //public List<FlverMaterial> DEBUG_LIST_Materials { get; set; } = new List<FlverMaterial>();
        //public List<FlverVertexGroup> DEBUG_LIST_VertexGroups { get; set; } = new List<FlverVertexGroup>();
        //public List<FlverFaceSet> DEBUG_LIST_FaceSets { get; set; } = new List<FlverFaceSet>();

        public static readonly byte[] FlverNullableVector3_NullBytes_A = new byte[]
        {
            0xFF, 0xFF, 0x7F, 0x7F,
            0xFF, 0xFF, 0x7F, 0x7F,
            0xFF, 0xFF, 0x7F, 0x7F,
        };

        public static readonly byte[] FlverNullableVector3_NullBytes_B = new byte[]
        {
            0xFF, 0xFF, 0x7F, 0xFF,
            0xFF, 0xFF, 0x7F, 0xFF,
            0xFF, 0xFF, 0x7F, 0xFF,
        };

        public FlverBone FindBone(string boneName, bool ignoreErrors = false)
        {
            var results = Bones.Where(x => x.Name == boneName).ToList();
            
            if (results.Count > 1)
            {
                if (!ignoreErrors)
                {
                    throw new Exception($"Multiple bones found with the name '{boneName}'.");
                }

                return null;
            }
            else if (results.Count == 0)
            {
                if (!ignoreErrors)
                {
                    throw new Exception($"No bones found with the name '{boneName}'");
                }
                return null;
            }
            else
            {
                return results.First();
            }
            
            
        }

        public FlverBone GetBoneFromIndex(int index, bool suppressExceptionForInvalidIndex = false)
        {
            if (index == -1)
                return null;
            else if (index < -1)
            {
                if (!suppressExceptionForInvalidIndex)
                {
                    throw new InvalidOperationException($"Tried to retrieve FLVER bone " +
                        $"with invalid index: {index}");
                }
                else
                {
                    return null;
                }
            }
            else if (index > Bones.Count - 1)
            {
                if (!suppressExceptionForInvalidIndex)
                {
                    throw new InvalidOperationException($"Tried to retrieve FLVER bone " +
                    $"from index which was outside the range of the FLVER's bone array. " +
                    $"Index: {index} / " +
                    $"FLVER bone array max index: {Bones.Count - 1}");
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return Bones[index];
            }

        }

        protected override void Read(DSBinaryReader bin, IProgress<(int, int)> prog)
        {
            string signature = bin.ReadStringShiftJIS();

            if (signature != "FLVER")
                throw new Exceptions.DSReadException(bin, $"Unexpected signature '{signature ?? "<NULL>"}' in FLVER file header.");

            string endiannessString = bin.ReadStringAscii(1);

            if (endiannessString == "L")
                Header.IsBigEndian = false;
            else if (endiannessString == "B")
                Header.IsBigEndian = true;
            else
                throw new Exceptions.DSReadException(bin, $"Unexpected endianness ASCII char '{endiannessString}' found in FLVER file header.");

            bin.BigEndian = Header.IsBigEndian;

            bin.Position++; //Padding

            Header.Version = bin.ReadFlverVersion();

            int dataOffset = bin.ReadInt32();
            int dataSize = bin.ReadInt32();
            int dummyCount = bin.ReadInt32();
            int materialCount = bin.ReadInt32();
            int boneCount = bin.ReadInt32();
            int meshCount = bin.ReadInt32();
            int vertexGroupCount = bin.ReadInt32();

            if (vertexGroupCount != meshCount)
                throw new NotSupportedException("This FLVER utilizes the ability to have a " +
                    "different number of vertex groups than submeshes. This FLVER loading code " +
                    "makes assumptions due to a lack of knowledge and attempting to load it " +
                    "would destroy the entire universe.");

            Header.BoundingBoxMin = bin.ReadVector3();
            Header.BoundingBoxMax = bin.ReadVector3();

            Header.Unknown0x40 = bin.ReadInt32();
            Header.Unknown0x44 = bin.ReadInt32();
            Header.Unknown0x48 = bin.ReadInt32();
            Header.Unknown0x4C = bin.ReadInt32();

            int faceSetCount = bin.ReadInt32();
            int vertexStructLayoutCount = bin.ReadInt32();
            int materialParameterCount = bin.ReadInt32();

            Header.Unknown0x5C = bin.ReadInt32();
            Header.Unknown0x60 = bin.ReadInt32();
            Header.Unknown0x64 = bin.ReadInt32();
            Header.Unknown0x68 = bin.ReadInt32();
            Header.Unknown0x6C = bin.ReadInt32();
            Header.Unknown0x70 = bin.ReadInt32();
            Header.Unknown0x74 = bin.ReadInt32();
            Header.Unknown0x78 = bin.ReadInt32();
            Header.Unknown0x7C = bin.ReadInt32();

            Dummies = new List<FlverDummy>();
            for (int i = 0; i < dummyCount; i++)
            {
                Dummies.Add(FlverDummy.Read(bin, this));
            }

            var INFO_material = new List<(int ParamCount, int ParamStartIndex)>();
            var LIST_material = new List<FlverMaterial>();

            for (int i = 0; i < materialCount; i++)
            {
                var mat = new FlverMaterial();

                var nameOffset = bin.ReadInt32();
                var mtdNameOffset = bin.ReadInt32();

                bin.StepIn(nameOffset);
                {
                    mat.Name = bin.ReadStringUnicode();
                }
                bin.StepOut();

                bin.StepIn(mtdNameOffset);
                {
                    mat.MTDName = bin.ReadStringUnicode();
                }
                bin.StepOut();

                int paramCount = bin.ReadInt32();
                int paramStartIndex = bin.ReadInt32();

                INFO_material.Add((paramCount, paramStartIndex));

                mat.Flags = bin.ReadInt32();
                mat.UnknownInt1 = bin.ReadInt32();
                mat.UnknownInt2 = bin.ReadInt32();
                mat.UnknownInt3 = bin.ReadInt32();

                LIST_material.Add(mat);
            }

            Bones = new List<FlverBone>();
            for (int i = 0; i < boneCount; i++)
            {
                var bone = new FlverBone(this);
                bone.Translation = bin.ReadVector3();
                int nameOffset = bin.ReadInt32();

                bin.StepIn(nameOffset);
                {
                    bone.Name = bin.ReadStringUnicode();
                }
                bin.StepOut();

                bone.EulerRadian = bin.ReadVector3();

                bone.ParentIndex = bin.ReadInt16();
                bone.FirstChildIndex = bin.ReadInt16();

                bone.Scale = bin.ReadVector3();

                bone.NextSiblingIndex = bin.ReadInt16();
                bone.PreviousSiblingIndex = bin.ReadInt16();

                bone.BoundingBoxMin = bin.ReadFlverNullableVector3(FlverNullableVector3_NullBytes_A);

                var isNubValue = bin.ReadInt16();

                if (isNubValue < 0 || isNubValue > 1)
                {
                    throw new Exceptions.DSReadException(bin, $"Invalid FLVER Bone 'IsNub' value " +
                        $"found (should be 0 or 1): '{isNubValue}'");
                }

                bone.IsNub = isNubValue != 0;

                bone.UnknownUShort1 = bin.ReadUInt16();

                bone.BoundingBoxMax = bin.ReadFlverNullableVector3(FlverNullableVector3_NullBytes_B);

                bone.UnknownUShort2 = bin.ReadUInt16();
                bone.UnknownUShort3 = bin.ReadUInt16();

                bone.UnknownInt1  = bin.ReadInt32();
                bone.UnknownInt2  = bin.ReadInt32();
                bone.UnknownInt3  = bin.ReadInt32();
                bone.UnknownInt4  = bin.ReadInt32();
                bone.UnknownInt5  = bin.ReadInt32();
                bone.UnknownInt6  = bin.ReadInt32();
                bone.UnknownInt7  = bin.ReadInt32();
                bone.UnknownInt8  = bin.ReadInt32();
                bone.UnknownInt9  = bin.ReadInt32();
                bone.UnknownInt10 = bin.ReadInt32();
                bone.UnknownInt11 = bin.ReadInt32();
                bone.UnknownInt12 = bin.ReadInt32();

                Bones.Add(bone);
            }

            var LOC_mesh = new List<(int Start, int BoneIndicesStartOffset)>();

            var INFO_mesh = new List<(int MaterialIndex, List<int> FaceSetIndices, List<int> VertexGroupIndices)>();

            Submeshes = new List<FlverSubmesh>();
            for (int i = 0; i < meshCount; i++)
            {
                LOC_mesh.Add(((int)bin.Position, -1));

                var mesh = new FlverSubmesh(this);


                int isDynamicValue = bin.ReadInt32();

                if (isDynamicValue < 0 || isDynamicValue > 1)
                {
                    throw new Exceptions.DSReadException(bin, $"Invalid FLVER Mesh 'IsDynamic' value " +
                        $"found (should be 0 or 1): '{isDynamicValue}'");
                }

                mesh.IsDynamic = isDynamicValue != 0;

                int materialIndex = bin.ReadInt32();

                mesh.UnknownByte1 = bin.ReadByte();
                mesh.UnknownByte2 = bin.ReadByte();
                mesh.UnknownByte3 = bin.ReadByte();
                mesh.UnknownByte4 = bin.ReadByte();
                mesh.UnknownByte5 = bin.ReadByte();
                mesh.UnknownByte6 = bin.ReadByte();
                mesh.UnknownByte7 = bin.ReadByte();
                mesh.UnknownByte8 = bin.ReadByte();

                mesh.DefaultBoneIndex = bin.ReadInt32();

                int boneIndexCount = bin.ReadInt32();

                mesh.UnknownInt1 = bin.ReadInt32();

                int boneIndicesOffset = bin.ReadInt32();
                bin.StepIn(boneIndicesOffset);
                {
                    for (int j = 0; j < boneIndexCount; j++)
                    {
                        mesh.BoneIndices.Add(bin.ReadInt32());
                    }
                }
                bin.StepOut();

                var faceSetIndices = new List<int>();
                int faceSetIndicesCount = bin.ReadInt32();
                int faceSetIndicesOffset = bin.ReadInt32();
                bin.StepIn(faceSetIndicesOffset);
                {
                    for (int j = 0; j < faceSetIndicesCount; j++)
                    {
                        faceSetIndices.Add(bin.ReadInt32());
                    }
                }
                bin.StepOut();

                var vertexGroupIndices = new List<int>();
                int vertexGroupIndicesCount = bin.ReadInt32();
                int vertexGroupIndicesOffset = bin.ReadInt32();
                bin.StepIn(vertexGroupIndicesOffset);
                {
                    for (int j = 0; j < vertexGroupIndicesCount; j++)
                    {
                        vertexGroupIndices.Add(bin.ReadInt32());
                    }
                }
                bin.StepOut();

                INFO_mesh.Add((materialIndex, faceSetIndices, vertexGroupIndices));

                Submeshes.Add(mesh);
            }

            var LIST_faceSets = new List<FlverFaceSet>();

            for (int i = 0; i < faceSetCount; i++)
            {
                var faceset = new FlverFaceSet();

                faceset.Flags = (FlverFaceSetFlags)bin.ReadUInt32();
                faceset.IsTriangleStrip = bin.ReadBoolean();
                faceset.CullBackfaces = bin.ReadBoolean();
                faceset.UnknownByte1 = bin.ReadByte();
                faceset.UnknownByte2 = bin.ReadByte();
                int indexCount = bin.ReadInt32();
                int indexBufferOffset = bin.ReadInt32();
                int indexBufferSize = bin.ReadInt32();

                bin.StepIn(indexBufferOffset + dataOffset);
                {
                    for (int j = 0; j < indexCount; j++)
                    {
                        faceset.VertexIndices.Add(bin.ReadUInt16());
                    }
                }
                bin.StepOut();

                faceset.UnknownInt1 = bin.ReadInt32();
                faceset.UnknownInt2 = bin.ReadInt32();
                faceset.UnknownInt3 = bin.ReadInt32();

                LIST_faceSets.Add(faceset);
            }

            var LIST_vertexGroups = new List<FlverVertexGroup>();

            var INFO_vertexGroup = new List<(int VertexBufferSize, int VertexBufferOffset)>();

            for (int i = 0; i < vertexGroupCount; i++)
            {
                var vertexGroup = new FlverVertexGroup(null);

                vertexGroup.UnknownInt1 = bin.ReadInt32();

                vertexGroup.VertexStructLayoutIndex = bin.ReadInt32();
                vertexGroup.VertexSize = bin.ReadInt32();

                vertexGroup.VertexCount = bin.ReadInt32();

                vertexGroup.UnknownInt2 = bin.ReadInt32();
                vertexGroup.UnknownInt3 = bin.ReadInt32();

                int vertexBufferSize = bin.ReadInt32();
                int vertexBufferOffset = bin.ReadInt32();

                INFO_vertexGroup.Add((vertexBufferSize, vertexBufferOffset));

                LIST_vertexGroups.Add(vertexGroup);
            }

            VertexStructLayouts = new List<FlverVertexStructLayout>();

            for (int i = 0; i < vertexStructLayoutCount; i++)
            {
                var vsl = new FlverVertexStructLayout();

                int memberCount = bin.ReadInt32();
                vsl.Unknown1 = bin.ReadInt32();
                vsl.Unknown2 = bin.ReadInt32();

                int memberOffset = bin.ReadInt32();

                bin.StepIn(memberOffset);
                {
                    for (int j = 0; j < memberCount; j++)
                    {
                        var vslMember = new FlverVertexStructMember();
                        vslMember.Unknown1 = bin.ReadInt32();
                        vslMember.StructOffset = bin.ReadInt32();
                        vslMember.ValueType = (FlverVertexStructMemberValueType)bin.ReadInt32();
                        vslMember.Semantic = (FlverVertexStructMemberSemantic)bin.ReadInt32();
                        vslMember.Index = bin.ReadInt32();
                        vsl.Members.Add(vslMember);
                    }
                }
                bin.StepOut();

                VertexStructLayouts.Add(vsl);
            }

            for (int i = 0; i < meshCount; i++)
            {
                foreach (var vertexGroupIndex in INFO_mesh[i].VertexGroupIndices)
                {
                    LIST_vertexGroups[vertexGroupIndex].ContainingSubmesh = Submeshes[i];
                    Submeshes[i].VertexGroups.Add(LIST_vertexGroups[vertexGroupIndex]);
                }
            }

            var LIST_materialParams = new List<FlverMaterialParameter>();

            for (int i = 0; i < materialParameterCount; i++)
            {
                var mp = new FlverMaterialParameter();

                int valueOffset = bin.ReadInt32();
                bin.StepIn(valueOffset);
                {
                    mp.Value = bin.ReadStringUnicode();
                }
                bin.StepOut();

                int nameOffset = bin.ReadInt32();
                bin.StepIn(nameOffset);
                {
                    mp.Name = bin.ReadStringUnicode();
                }
                bin.StepOut();

                mp.UnknownFloat1 = bin.ReadSingle();
                mp.UnknownFloat2 = bin.ReadSingle();

                mp.UnknownByte1  = bin.ReadByte();
                mp.UnknownByte2  = bin.ReadByte();
                mp.UnknownByte3  = bin.ReadByte();
                mp.UnknownByte4  = bin.ReadByte();

                mp.UnknownInt1  = bin.ReadInt32();
                mp.UnknownInt2  = bin.ReadInt32();
                mp.UnknownInt3  = bin.ReadInt32();

                LIST_materialParams.Add(mp);
            }

            for (int i = 0; i < materialCount; i++)
            {
                for (int j = INFO_material[i].ParamStartIndex; 
                    j < (INFO_material[i].ParamStartIndex + INFO_material[i].ParamCount); j++)
                {
                    LIST_material[i].Parameters.Add(LIST_materialParams[j]);
                }
            }

            foreach (var mesh in Submeshes)
            {
                foreach (var vertexGroup in mesh.VertexGroups)
                {
                    var vertexGroupInfo = INFO_vertexGroup[LIST_vertexGroups.IndexOf(vertexGroup)];

                   var vertexStructLayout =
                        VertexStructLayouts[vertexGroup.VertexStructLayoutIndex];

                    var verticesStartOffset = vertexGroupInfo.VertexBufferOffset + dataOffset;

                    bin.StepIn(verticesStartOffset);
                    {
                        for (int j = 0; j < vertexGroup.VertexCount; j++)
                        {
                            int currentStuctStartOffset = verticesStartOffset + j * vertexGroup.VertexSize;
                            bin.StepIn(currentStuctStartOffset);
                            {
                                var vert = new FlverVertex();

                                foreach (var member in vertexStructLayout.Members)
                                {
                                    bin.StepIn(currentStuctStartOffset + member.StructOffset);
                                    {
                                        switch (member.ValueType)
                                        {
                                            case FlverVertexStructMemberValueType.BoneIndicesStruct:
                                                switch (member.Semantic)
                                                {
                                                    case FlverVertexStructMemberSemantic.BoneIndices:
                                                        vert.BoneIndices = new FlverBoneIndices(
                                                            vertexGroup.ContainingSubmesh,
                                                            bin.ReadSByte(),
                                                            bin.ReadSByte(),
                                                            bin.ReadSByte(),
                                                            bin.ReadSByte());
                                                        break;
                                                    default:
                                                        throw new Exception($"Invalid FLVER Vertex Struct Member " +
                                                            $"Semantic ({member.Semantic}) " +
                                                            $"for Value Type {member.ValueType}.");
                                                }
                                                break;
                                            case FlverVertexStructMemberValueType.BoneWeightsStruct:
                                                switch (member.Semantic)
                                                {
                                                    case FlverVertexStructMemberSemantic.BoneWeights:
                                                        vert.BoneWeights = new FlverBoneWeights(
                                                            bin.ReadInt16(),
                                                            bin.ReadInt16(),
                                                            bin.ReadInt16(),
                                                            bin.ReadInt16());
                                                        break;
                                                    default:
                                                        throw new Exception($"Invalid FLVER Vertex Struct Member " +
                                                            $"Semantic ({member.Semantic}) " +
                                                            $"for Value Type {member.ValueType}.");
                                                }
                                                break;
                                            case FlverVertexStructMemberValueType.UV:
                                                switch (member.Semantic)
                                                {
                                                    case FlverVertexStructMemberSemantic.UV:
                                                        vert.UVs.Add(bin.ReadFlverUV());
                                                        break;
                                                    default:
                                                        throw new Exception($"Invalid FLVER Vertex Struct Member " +
                                                            $"Semantic ({member.Semantic}) " +
                                                            $"for Value Type {member.ValueType}.");
                                                }
                                                break;

                                            case FlverVertexStructMemberValueType.UVPair:
                                                switch (member.Semantic)
                                                {
                                                    case FlverVertexStructMemberSemantic.UV:
                                                        vert.UVs.Add(bin.ReadFlverUV());
                                                        vert.UVs.Add(bin.ReadFlverUV());
                                                        break;
                                                    default:
                                                        throw new Exception($"Invalid FLVER Vertex Struct Member " +
                                                            $"Semantic ({member.Semantic}) " +
                                                            $"for Value Type {member.ValueType}.");
                                                }
                                                break;

                                            case FlverVertexStructMemberValueType.Vector3:
                                                var value_Vector3 = bin.ReadVector3();

                                                switch (member.Semantic)
                                                {
                                                    case FlverVertexStructMemberSemantic.Position:
                                                        vert.Position = value_Vector3;
                                                        break;
                                                    default:
                                                        throw new Exception($"Invalid FLVER Vertex Struct Member " +
                                                            $"Semantic ({member.Semantic}) " +
                                                            $"for Value Type {member.ValueType}.");
                                                }
                                                break;
                                            case FlverVertexStructMemberValueType.PackedVector4:
                                                switch (member.Semantic)
                                                {
                                                    case FlverVertexStructMemberSemantic.Normal:
                                                        vert.Normal = bin.ReadFlverPackedVector4();
                                                        break;
                                                    case FlverVertexStructMemberSemantic.BiTangent:
                                                        vert.BiTangent = bin.ReadFlverPackedVector4();
                                                        break;
                                                    case FlverVertexStructMemberSemantic.VertexColor:
                                                        vert.VertexColor = bin.ReadFlverVertexColor();
                                                        break;
                                                    case FlverVertexStructMemberSemantic.UnknownVector4A:
                                                        vert.UnknownVector4A = bin.ReadFlverPackedVector4();
                                                        break;
                                                    default:
                                                        throw new Exception($"Invalid FLVER Vertex Struct Member " +
                                                            $"Semantic ({member.Semantic}) " +
                                                            $"for Value Type {member.ValueType}.");
                                                }
                                                break;
                                            default:
                                                throw new Exception($"Invalid FLVER Value Type: {member.ValueType}");

                                        }
                                    }
                                    bin.StepOut();
                                }

                                vertexGroup.ContainingSubmesh.Vertices.Add(vert);
                            }
                            bin.StepOut();


                        }
                    }
                    bin.StepOut();
                }

                

                
            }

            for (int i = 0; i < meshCount; i++)
            {
                Submeshes[i].Material = LIST_material[INFO_mesh[i].MaterialIndex];

                foreach (var faceSetIndex in INFO_mesh[i].FaceSetIndices)
                {
                    Submeshes[i].FaceSets.Add(LIST_faceSets[faceSetIndex]);
                }
            }

            //DEBUG_LIST_VertexStructLayout = LIST_vertexStructLayouts;
            //DEBUG_LIST_Materials = LIST_material;
            //DEBUG_LIST_VertexGroups = LIST_vertexGroups;
            //DEBUG_LIST_FaceSets = LIST_faceSets;
        }

        //private (List<FlverVertexStructLayout> VertexStructLayouts, 
        //    Dictionary<FlverVertexGroup, int> VertexGroupStructLayoutIndices) 
        //    WRITE_CalculateVertexStructLayouts()
        //{
        //    var vertStructCheckList = new List<FlverVertexStructLayoutCheck>();

        //    var vertexGroupStructLayoutIndices = new Dictionary<FlverVertexGroup, int>();

        //    foreach (var mesh in Submeshes)
        //    {
        //        int currentIndex = 0;

        //        foreach (var vertexGroup in mesh.VertexGroups)
        //        {
        //            var vertStructCheck = new FlverVertexStructLayoutCheck();

        //            for (int i = 0; i <= vertexGroup.VertexCount; i++)
        //            {
        //                vertStructCheck.ApplyCheck(mesh.Vertices[currentIndex++]);
        //            }

        //            if (vertStructCheckList.Contains(vertStructCheck))
        //            {
        //                vertexGroupStructLayoutIndices.Add(vertexGroup, vertStructCheckList.IndexOf(vertStructCheck));
        //            }
        //            else
        //            {
        //                vertexGroupStructLayoutIndices.Add(vertexGroup, vertStructCheckList.Count);
        //                vertStructCheckList.Add(vertStructCheck);
        //            }
        //        }
        //    }

        //    var structLayoutList = new List<FlverVertexStructLayout>();

        //    foreach (var check in vertStructCheckList)
        //    {
        //        var structLayout = new FlverVertexStructLayout();
        //        var currentOffset = 0;

        //        void _newMember(FlverVertexStructMemberSemantic s, FlverVertexStructMemberValueType v)
        //        {
        //            var newMember = new FlverVertexStructMember();
        //            newMember.Semantic = s;
        //            newMember.ValueType = v;
        //            newMember.StructOffset = currentOffset;
                    
        //            switch (v)
        //            {
        //                case FlverVertexStructMemberValueType.BoneIndicesStruct:
        //                    currentOffset += 4;
        //                    break;
        //                case FlverVertexStructMemberValueType.BoneWeightsStruct:
        //                    currentOffset += sizeof(ushort) * 4;
        //                    break;
        //                case FlverVertexStructMemberValueType.UV:
        //                    currentOffset += sizeof(ushort) * 2;
        //                    break;
        //                case FlverVertexStructMemberValueType.UVPair:
        //                    currentOffset += (sizeof(ushort) * 2) * 2;
        //                    break;
        //                case FlverVertexStructMemberValueType.Vector3:
        //                    currentOffset += sizeof(float) * 3;
        //                    break;
        //                case FlverVertexStructMemberValueType.PackedVector4:
        //                    currentOffset += 4;
        //                    break;
        //            }

        //            structLayout.Members.Add(newMember);
        //        }

        //        if (check.HasBiTangent)
        //            _newMember(FlverVertexStructMemberSemantic.BiTangent,
        //                FlverVertexStructMemberValueType.PackedVector4);

        //        if (check.HasBoneIndices)
        //            _newMember(FlverVertexStructMemberSemantic.BoneIndices,
        //                FlverVertexStructMemberValueType.BoneIndicesStruct);

        //        if (check.HasBoneWeights)
        //            _newMember(FlverVertexStructMemberSemantic.BoneWeights,
        //                FlverVertexStructMemberValueType.BoneWeightsStruct);

        //        if (check.HasDiffuseUV)
        //        {
        //            if (check.HasLightmapUV)
        //            {
        //                _newMember(FlverVertexStructMemberSemantic.UV,
        //                    FlverVertexStructMemberValueType.UVPair);
        //            }
        //            else
        //            {
        //                _newMember(FlverVertexStructMemberSemantic.UV,
        //                    FlverVertexStructMemberValueType.UV);
        //            }
        //        }

        //        if (check.HasNormal)
        //            _newMember(FlverVertexStructMemberSemantic.Normal,
        //                FlverVertexStructMemberValueType.PackedVector4);

        //        if (check.HasPosition)
        //            _newMember(FlverVertexStructMemberSemantic.Position,
        //                FlverVertexStructMemberValueType.Vector3);

        //        if (check.HasUnknownVector4A)
        //            _newMember(FlverVertexStructMemberSemantic.UnknownVector4A,
        //                FlverVertexStructMemberValueType.PackedVector4);

        //        if (check.HasVertexColor)
        //            _newMember(FlverVertexStructMemberSemantic.VertexColor,
        //                FlverVertexStructMemberValueType.PackedVector4);

        //        structLayoutList.Add(structLayout);
        //    }

        //    return (structLayoutList, vertexGroupStructLayoutIndices);
        //}

        protected override void Write(DSBinaryWriter bin, IProgress<(int, int)> prog)
        {
            List<FlverMaterial> LIST_materials = new List<FlverMaterial>();
            List<FlverVertexGroup> LIST_vertexGroups = new List<FlverVertexGroup>();
            List<FlverFaceSet> LIST_faceSets = new List<FlverFaceSet>();
            List<FlverMaterialParameter> LIST_materialParameter = new List<FlverMaterialParameter>();

            //LIST_vertexStructLayout = DEBUG_LIST_VertexStructLayout;
            //LIST_materials = DEBUG_LIST_Materials;
            //LIST_vertexGroups = DEBUG_LIST_VertexGroups;
            //LIST_faceSets = DEBUG_LIST_FaceSets;

            List<FlverWriteFunc_SubmeshInfo> INFO_submeshes 
                = new List<FlverWriteFunc_SubmeshInfo>();

            List<FlverWriteFunc_MaterialInfo> INFO_materials 
                = new List<FlverWriteFunc_MaterialInfo>();

            foreach (var mesh in Submeshes)
            {
                INFO_submeshes.Add(new FlverWriteFunc_SubmeshInfo
                    (mesh, LIST_materials, LIST_vertexGroups, LIST_faceSets));
            }

            foreach (var mat in LIST_materials)
            {
                INFO_materials.Add(new FlverWriteFunc_MaterialInfo(mat, LIST_materialParameter));
            }

            bin.WriteStringShiftJIS("FLVER", true);
            if (Header.IsBigEndian)
                bin.WriteStringShiftJIS("B", false);
            else
                bin.WriteStringShiftJIS("L", false);
            bin.BigEndian = Header.IsBigEndian;

            bin.Write((byte)0); //pad

            bin.WriteFlverVersion(Header.Version);

            bin.Placeholder("DataOffset");
            bin.Placeholder("DataSize");
            bin.Write(Dummies.Count);
            bin.Write(LIST_materials.Count);
            bin.Write(Bones.Count);
            bin.Write(Submeshes.Count);
            bin.Write(LIST_vertexGroups.Count);

            bin.Write(Header.BoundingBoxMin);
            bin.Write(Header.BoundingBoxMax);

            bin.Write(Header.Unknown0x40);
            bin.Write(Header.Unknown0x44);
            bin.Write(Header.Unknown0x48);
            bin.Write(Header.Unknown0x4C);

            bin.Write(LIST_faceSets.Count);
            bin.Write(VertexStructLayouts.Count);
            bin.Write(LIST_materialParameter.Count);


            bin.Write(Header.Unknown0x5C);
            bin.Write(Header.Unknown0x60);
            bin.Write(Header.Unknown0x64);
            bin.Write(Header.Unknown0x68);
            bin.Write(Header.Unknown0x6C);
            bin.Write(Header.Unknown0x70);
            bin.Write(Header.Unknown0x74);
            bin.Write(Header.Unknown0x78);
            bin.Write(Header.Unknown0x7C);

            foreach (var dmy in Dummies)
            {
                FlverDummy.Write(bin, dmy);
            }

            bin.Pad(0x10);

            for (int i = 0; i < LIST_materials.Count; i++)
            {
                var mat = LIST_materials[i];

                bin.Placeholder($"Materials[{i}].Name");
                bin.Placeholder($"Materials[{i}].MTDName");

                bin.Write(mat.Parameters.Count);
                if (INFO_materials[i].MaterialParameterIndices.Count > 0)
                    bin.Write(INFO_materials[i].MaterialParameterIndices[0]);
                else
                    bin.Write(-1);

                bin.Write(mat.Flags);
                bin.Write(mat.UnknownInt1);
                bin.Write(mat.UnknownInt2);
                bin.Write(mat.UnknownInt3);
            }

            bin.Pad(0x10);

            for (int i = 0; i < Bones.Count; i++)
            {
                var bone = Bones[i];

                bin.Write(bone.Translation);
                bin.Placeholder($"Bones[{i}].Name");
                bin.Write(bone.EulerRadian);
                bin.Write(bone.ParentIndex);
                bin.Write(bone.FirstChildIndex);
                bin.Write(bone.Scale);
                bin.Write(bone.NextSiblingIndex);
                bin.Write(bone.PreviousSiblingIndex);

                if (bone.BoundingBoxMin != null)
                {
                    bin.Write(bone.BoundingBoxMin);
                }
                else
                {
                    bin.Write((byte)0xFF);
                    bin.Write((byte)0xFF);
                    bin.Write((byte)0x7F);
                    bin.Write((byte)0x7F);

                    bin.Write((byte)0xFF);
                    bin.Write((byte)0xFF);
                    bin.Write((byte)0x7F);
                    bin.Write((byte)0x7F);

                    bin.Write((byte)0xFF);
                    bin.Write((byte)0xFF);
                    bin.Write((byte)0x7F);
                    bin.Write((byte)0x7F);
                }

                if (bone.IsNub)
                    bin.Write((ushort)1);
                else
                    bin.Write((ushort)0);

                bin.Write(bone.UnknownUShort1);

                if (bone.BoundingBoxMax != null)
                {
                    bin.Write(bone.BoundingBoxMax);
                }
                else
                {
                    bin.Write((byte)0xFF);
                    bin.Write((byte)0xFF);
                    bin.Write((byte)0x7F);
                    bin.Write((byte)0xFF);

                    bin.Write((byte)0xFF);
                    bin.Write((byte)0xFF);
                    bin.Write((byte)0x7F);
                    bin.Write((byte)0xFF);

                    bin.Write((byte)0xFF);
                    bin.Write((byte)0xFF);
                    bin.Write((byte)0x7F);
                    bin.Write((byte)0xFF);
                }

                bin.Write(bone.UnknownUShort2);
                bin.Write(bone.UnknownUShort3);

                bin.Write(bone.UnknownInt1);
                bin.Write(bone.UnknownInt2);
                bin.Write(bone.UnknownInt3);
                bin.Write(bone.UnknownInt4);
                bin.Write(bone.UnknownInt5);
                bin.Write(bone.UnknownInt6);
                bin.Write(bone.UnknownInt7);
                bin.Write(bone.UnknownInt8);
                bin.Write(bone.UnknownInt9);
                bin.Write(bone.UnknownInt10);
                bin.Write(bone.UnknownInt11);
                bin.Write(bone.UnknownInt12);

            }

            bin.Pad(0x10);

            for (int i = 0; i < Submeshes.Count; i++)
            {
                var mesh = Submeshes[i];
                var meshInfo = INFO_submeshes[i];

                if (mesh.IsDynamic)
                    bin.Write(1u);
                else
                    bin.Write(0u);

                bin.Write(meshInfo.MaterialIndex);

                bin.Write(mesh.UnknownByte1);
                bin.Write(mesh.UnknownByte2);
                bin.Write(mesh.UnknownByte3);
                bin.Write(mesh.UnknownByte4);
                bin.Write(mesh.UnknownByte5);
                bin.Write(mesh.UnknownByte6);
                bin.Write(mesh.UnknownByte7);
                bin.Write(mesh.UnknownByte8);

                bin.Write(mesh.DefaultBoneIndex);
                bin.Write(mesh.BoneIndices.Count);
                bin.Write(mesh.UnknownInt1);
                bin.Placeholder($"Submeshes[{i}].BoneIndicesOffset");

                bin.Write(mesh.FaceSets.Count);
                bin.Placeholder($"Submeshes[{i}].FaceSetIndicesOffset");

                bin.Write(mesh.VertexGroups.Count);
                bin.Placeholder($"Submeshes[{i}].VertexGroupIndicesOffset");


            }

            bin.Pad(0x10);

            for (int i = 0; i < LIST_faceSets.Count; i++)
            {
                var faceSet = LIST_faceSets[i];

                bin.Write((uint)faceSet.Flags);
                bin.Write(faceSet.IsTriangleStrip);
                bin.Write(faceSet.CullBackfaces);
                bin.Write(faceSet.UnknownByte1);
                bin.Write(faceSet.UnknownByte2);

                bin.Write(faceSet.VertexIndices.Count);
                bin.Placeholder($"FaceSets[{i}].VertexIndicesOffset");
                bin.Write(faceSet.VertexIndices.Count * sizeof(ushort));

                bin.Write(faceSet.UnknownInt1);
                bin.Write(faceSet.UnknownInt2);
                bin.Write(faceSet.UnknownInt3);
            }

            bin.Pad(0x10);

            for (int i = 0; i < LIST_vertexGroups.Count; i++)
            {
                var vertexGroup = LIST_vertexGroups[i];

                bin.Write(vertexGroup.UnknownInt1);

                bin.Write(vertexGroup.VertexStructLayoutIndex);
                bin.Write(vertexGroup.VertexSize);
                bin.Write(vertexGroup.VertexCount);

                bin.Write(vertexGroup.UnknownInt2);
                bin.Write(vertexGroup.UnknownInt3);

                bin.Write(vertexGroup.VertexSize * vertexGroup.VertexCount /*VertexBufferSize*/);
                bin.Placeholder($"VertexGroups[{i}].VertexBufferOffset");
            }

            bin.Pad(0x10);

            for (int i = 0; i < VertexStructLayouts.Count; i++)
            {
                var vsl = VertexStructLayouts[i];

                bin.Write(vsl.Members.Count);
                bin.Write(vsl.Unknown1);
                bin.Write(vsl.Unknown2);

                bin.Placeholder($"VertexStructLayouts[{i}].MembersOffset");
            }

            bin.Pad(0x10);

            for (int i = 0; i < LIST_materialParameter.Count; i++)
            {
                var mp = LIST_materialParameter[i];

                bin.Placeholder($"MaterialParameters[{i}].Value");
                bin.Placeholder($"MaterialParameters[{i}].Name");

                bin.Write(mp.UnknownFloat1);
                bin.Write(mp.UnknownFloat2);

                bin.Write(mp.UnknownByte1);
                bin.Write(mp.UnknownByte2);
                bin.Write(mp.UnknownByte3);
                bin.Write(mp.UnknownByte4);

                bin.Write(mp.UnknownInt1);
                bin.Write(mp.UnknownInt2);
                bin.Write(mp.UnknownInt3);
            }

            bin.Pad(0x10);

            for (int i = 0; i < VertexStructLayouts.Count; i++)
            {
                var vsl = VertexStructLayouts[i];

                bin.Replace($"VertexStructLayouts[{i}].MembersOffset", (int)bin.Position);

                foreach (var member in vsl.Members)
                {
                    bin.Write(member.Unknown1);
                    bin.Write(member.StructOffset);
                    bin.Write((uint)member.ValueType);
                    bin.Write((uint)member.Semantic);
                    bin.Write(member.Index);
                }
            }

            bin.Pad(0x10);

            for (int i = 0; i < Submeshes.Count; i++)
            {
                var mesh = Submeshes[i];
                var meshInfo = INFO_submeshes[i];

                bin.Replace($"Submeshes[{i}].BoneIndicesOffset", (int)bin.Position);

                foreach (var index in meshInfo.BoneIndices)
                {
                    bin.Write(index);
                }
            }

            bin.Pad(0x10);

            for (int i = 0; i < Submeshes.Count; i++)
            {
                var mesh = Submeshes[i];
                var meshInfo = INFO_submeshes[i];

                bin.Replace($"Submeshes[{i}].FaceSetIndicesOffset", (int)bin.Position);

                foreach (var index in meshInfo.FaceSetIndices)
                {
                    bin.Write(index);
                }
            }

            bin.Pad(0x10);

            for (int i = 0; i < Submeshes.Count; i++)
            {
                var mesh = Submeshes[i];
                var meshInfo = INFO_submeshes[i];

                bin.Replace($"Submeshes[{i}].VertexGroupIndicesOffset", (int)bin.Position);

                foreach (var index in meshInfo.VertexGroupIndices)
                {
                    bin.Write(index);
                }
            }

            bin.Pad(0x10);

            for (int i = 0; i < LIST_materials.Count; i++)
            {
                var mat = LIST_materials[i];
                var matInfo = INFO_materials[i];

                bin.Replace($"Materials[{i}].Name", (int)bin.Position);
                bin.WriteStringUnicode(mat.Name, terminate: true);

                bin.Replace($"Materials[{i}].MTDName", (int)bin.Position);
                bin.WriteStringUnicode(mat.MTDName, terminate: true);

                foreach (var index in matInfo.MaterialParameterIndices)
                {
                    var mp = LIST_materialParameter[index];

                    bin.Replace($"MaterialParameters[{index}].Value", (int)bin.Position);
                    bin.WriteStringUnicode(mp.Value, terminate: true);

                    bin.Replace($"MaterialParameters[{index}].Name", (int)bin.Position);
                    bin.WriteStringUnicode(mp.Name, terminate: true);
                }
            }

            bin.Pad(0x10);
            //bin.Pad(0x20);

            for (int i = 0; i < Bones.Count; i++)
            {
                var bone = Bones[i];

                bin.Replace($"Bones[{i}].Name", (int)bin.Position);
                bin.WriteStringUnicode(bone.Name, terminate: true);
            }

            //bin.Pad(0x10);

            ////16 bytes of padding...?
            //bin.Position += 0x16;

            bin.Pad(0x20);

            ////////////////////////////
            //////// DATA BEGIN ////////
            ////////////////////////////

            var LOC_Data = bin.Position;

            bin.Replace("DataOffset", (int)LOC_Data);

            for (int i = 0; i < Submeshes.Count; i++)
            {
                var mesh = Submeshes[i];
                var meshInfo = INFO_submeshes[i];

                foreach (var faceSetIndex in meshInfo.FaceSetIndices)
                {
                    var faceSet = LIST_faceSets[faceSetIndex];

                    bin.Replace($"FaceSets[{faceSetIndex}].VertexIndicesOffset", (int)(bin.Position - LOC_Data));
                    foreach (var index in faceSet.VertexIndices)
                    {
                        bin.Write(index);
                    }

                    bin.Pad(0x20);
                }

                int meshVertexIndex = 0;
                
                foreach (var vertexGroupIndex in meshInfo.VertexGroupIndices)
                {
                    var vertexGroup = LIST_vertexGroups[vertexGroupIndex];

                    var vertexStructLayout = VertexStructLayouts[vertexGroup.VertexStructLayoutIndex];

                    var LOC_vertexGroupVerticesStart = bin.Position;

                    bin.Replace($"VertexGroups[{vertexGroupIndex}].VertexBufferOffset", (int)(bin.Position - LOC_Data));
                    for (int j = 0; j < vertexGroup.VertexCount; j++)
                    {
                        //bin.StepIn(LOC_vertexGroupVerticesStart + (j * vertexGroup.VertexStructLayout.Size));
                        bin.Position = LOC_vertexGroupVerticesStart + (j * vertexGroup.VertexSize);
                        {
                            var vert = mesh.Vertices[meshVertexIndex++];

                            var vertUvQueue = new Queue<FlverUV>(vert.UVs);

                            var LOC_currentVertex = bin.Position;

                            foreach (var member in vertexStructLayout.Members)
                            {
                                //bin.StepIn(LOC_currentVertex + member.StructOffset);
                                bin.Position = LOC_currentVertex + member.StructOffset;
                                {


                                    switch (member.ValueType)
                                    {
                                        case FlverVertexStructMemberValueType.BoneIndicesStruct:
                                            switch (member.Semantic)
                                            {
                                                case FlverVertexStructMemberSemantic.BoneIndices:
                                                    if (vert.BoneIndices == null)
                                                        throw new FlverVertexStructDataNullException(bin, member, i, j);

                                                    var packedBoneIndices = vert.BoneIndices.GetPacked();

                                                    bin.Write(packedBoneIndices.A);
                                                    bin.Write(packedBoneIndices.B);
                                                    bin.Write(packedBoneIndices.C);
                                                    bin.Write(packedBoneIndices.D);

                                                    break;
                                                default:
                                                    throw new Exception($"Invalid FLVER Vertex Struct Member " +
                                                        $"Semantic ({member.Semantic}) " +
                                                        $"for Value Type {member.ValueType}.");

                                            }
                                            break;

                                        case FlverVertexStructMemberValueType.BoneWeightsStruct:
                                            switch (member.Semantic)
                                            {
                                                case FlverVertexStructMemberSemantic.BoneWeights:

                                                    if (vert.BoneWeights == null)
                                                        throw new FlverVertexStructDataNullException(bin, member, i, j);

                                                    var packedBoneWeights = vert.BoneWeights.GetPacked();

                                                    bin.Write(packedBoneWeights.A);
                                                    bin.Write(packedBoneWeights.B);
                                                    bin.Write(packedBoneWeights.C);
                                                    bin.Write(packedBoneWeights.D);

                                                    break;
                                                default:
                                                    throw new Exception($"Invalid FLVER Vertex Struct Member " +
                                                        $"Semantic ({member.Semantic}) " +
                                                        $"for Value Type {member.ValueType}.");

                                            }

                                            break;

                                        case FlverVertexStructMemberValueType.PackedVector4:
                                            switch (member.Semantic)
                                            {
                                                case FlverVertexStructMemberSemantic.BiTangent:
                                                    if (vert.BiTangent == null)
                                                        throw new FlverVertexStructDataNullException(bin, member, i, j);
                                                    bin.WriteFlverPackedVector4(vert.BiTangent);
                                                    break;
                                                case FlverVertexStructMemberSemantic.Normal:
                                                    if (vert.Normal == null)
                                                        throw new FlverVertexStructDataNullException(bin, member, i, j);
                                                    bin.WriteFlverPackedVector4(vert.Normal);
                                                    break;
                                                case FlverVertexStructMemberSemantic.VertexColor:
                                                    if (vert.VertexColor == null)
                                                        throw new FlverVertexStructDataNullException(bin, member, i, j);
                                                    bin.WriteFlverVertexColor(vert.VertexColor);
                                                    break;
                                                case FlverVertexStructMemberSemantic.UnknownVector4A:
                                                    if (vert.UnknownVector4A == null)
                                                        throw new FlverVertexStructDataNullException(bin, member, i, j);
                                                    bin.WriteFlverPackedVector4(vert.UnknownVector4A);
                                                    break;
                                                default:
                                                    throw new Exception($"Invalid FLVER Vertex Struct Member " +
                                                        $"Semantic ({member.Semantic}) " +
                                                        $"for Value Type {member.ValueType}.");

                                            }
                                            break;

                                        case FlverVertexStructMemberValueType.UV:
                                            switch (member.Semantic)
                                            {
                                                case FlverVertexStructMemberSemantic.UV:
                                                    if (vertUvQueue.Count < 1)
                                                        throw new FlverVertexStructDataNullException(bin, member, i, j);
                                                    bin.WriteFlverUV(vertUvQueue.Dequeue());
                                                    break;
                                                default:
                                                    throw new Exception($"Invalid FLVER Vertex Struct Member " +
                                                        $"Semantic ({member.Semantic}) " +
                                                        $"for Value Type {member.ValueType}.");

                                            }

                                            break;

                                        case FlverVertexStructMemberValueType.UVPair:
                                            switch (member.Semantic)
                                            {
                                                case FlverVertexStructMemberSemantic.UV:
                                                    if (vertUvQueue.Count < 2)
                                                        throw new FlverVertexStructDataNullException(bin, member, i, j);
                                                    bin.WriteFlverUV(vertUvQueue.Dequeue());
                                                    bin.WriteFlverUV(vertUvQueue.Dequeue());
                                                    break;
                                                default:
                                                    throw new Exception($"Invalid FLVER Vertex Struct Member " +
                                                        $"Semantic ({member.Semantic}) " +
                                                        $"for Value Type {member.ValueType}.");

                                            }
                                            break;

                                        case FlverVertexStructMemberValueType.Vector3:
                                            switch (member.Semantic)
                                            {
                                                case FlverVertexStructMemberSemantic.Position:
                                                    if (vert.Position == null)
                                                        throw new FlverVertexStructDataNullException(bin, member, i, j);
                                                    bin.Write(vert.Position);
                                                    break;
                                                default:
                                                    throw new Exception($"Invalid FLVER Vertex Struct Member " +
                                                        $"Semantic ({member.Semantic}) " +
                                                        $"for Value Type {member.ValueType}.");

                                            }
                                            break;

                                        default:
                                            throw new Exception($"Invalid FLVER Value Type: {member.ValueType}");
                                    }

                                }
                                //bin.StepOut();
                            }




                        }
                        //bin.StepOut();

                        
                    }
                }

                bin.Pad(0x20, ensureBytesAreEmpty: true);
            }

            var LOC_DataEnd = bin.Position;

            int dataSize = (int)(LOC_DataEnd - LOC_Data);

            bin.Replace("DataSize", dataSize);
        }



    }
}
