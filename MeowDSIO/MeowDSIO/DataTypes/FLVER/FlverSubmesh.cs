using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverSubmesh
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        [Newtonsoft.Json.JsonIgnore]
        public DataFiles.FLVER ContainingFlver { get; set; } = null;

        public FlverSubmesh(DataFiles.FLVER ContainingFlver)
        {
            this.ContainingFlver = ContainingFlver;
        }

        public sbyte FindOrAddBoneIndex(string boneName)
        {
            for (int i = 0; i < BoneIndices.Count; i++)
            {
                if (ContainingFlver.Bones[BoneIndices[i]].Name == boneName)
                {
                    return (sbyte)i;
                }
            }

            for (int i = 0; i < ContainingFlver.Bones.Count; i++)
            {
                if (ContainingFlver.Bones[i].Name == boneName)
                {
                    BoneIndices.Add(i);
                    return (sbyte)(BoneIndices.Count - 1);
                }
            }

            return -1;
        }

        public FlverBone GetBoneFromLocalIndex(int index, bool suppressExceptionForInvalidIndex = false)
        {
            var bones = GetBones().ToList();
            if (index == -1)
                return null;
            else if (index < -1)
            {
                if (!suppressExceptionForInvalidIndex)
                {
                    throw new InvalidOperationException($"Tried to retrieve FLVER bone " +
                        $"with invalid local index: {index}");
                }
                else
                {
                    return null;
                }
            }
            else if (index > bones.Count - 1)
            {
                if (!suppressExceptionForInvalidIndex)
                {
                    throw new InvalidOperationException($"Tried to retrieve FLVER bone " +
                    $"from index which was outside the range of the FLVER submesh's bone array. " +
                    $"Index: {index} / " +
                    $"FLVER bone array max index: {bones.Count - 1}");
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return bones[index];
            }

        }

        public bool IsDynamic { get; set; }

        public FlverMaterial Material { get; set; }

        public byte UnknownByte1 { get; set; } = 0;
        public byte UnknownByte2 { get; set; } = 0;
        public byte UnknownByte3 { get; set; } = 0;
        public byte UnknownByte4 { get; set; } = 0;
        public byte UnknownByte5 { get; set; } = 0;
        public byte UnknownByte6 { get; set; } = 0;
        public byte UnknownByte7 { get; set; } = 0;
        public byte UnknownByte8 { get; set; } = 0;

        public int DefaultBoneIndex { get; set; } = -1;

        public List<int> BoneIndices { get; set; } = new List<int>();

        public IEnumerable<FlverBone> GetBones()
        {
            return BoneIndices.Select(x =>
            {
                if (ContainingFlver == null)
                {
                    return null;
                }
                else if (x >= 0 && x < ContainingFlver.Bones.Count)
                {
                    return ContainingFlver.Bones[x];
                }
                else
                {
                    return null;
                }
            })
            .Where(x => x != null);
        }

        public int UnknownInt1 { get; set; } = 0;

        public List<FlverFaceSet> FaceSets { get; set; } = new List<FlverFaceSet>();

        public List<FlverVertexGroup> VertexGroups { get; set; } = new List<FlverVertexGroup>();

        public List<FlverVertex> Vertices { get; set; } = new List<FlverVertex>();

        public List<ushort> GetAllVertexIndices()
        {
            List<ushort> list = new List<ushort>();
            foreach (var faceSet in FaceSets)
            {
                list.AddRange(faceSet.VertexIndices);
            }
            return list;
        }
    }
}
