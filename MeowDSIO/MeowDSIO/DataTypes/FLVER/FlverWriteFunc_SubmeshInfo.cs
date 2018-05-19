using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverWriteFunc_SubmeshInfo
    {
        public int MaterialIndex { get; set; }
        public List<int> VertexGroupIndices { get; set; } = new List<int>();
        public List<int> FaceSetIndices { get; set; } = new List<int>();
        public List<int> BoneIndices { get; set; } = new List<int>();



        public FlverWriteFunc_SubmeshInfo(FlverSubmesh submesh, 
            List<FlverMaterial> materialList, 
            List<FlverVertexGroup> vertexGroupList,
            List<FlverFaceSet> faceSetList)
        {
            MaterialIndex = MiscUtil.AddTolistAndReturnIndex(materialList, submesh.Material);

            MiscUtil.IterateIndexList(VertexGroupIndices, vertexGroupList, submesh.VertexGroups);
            MiscUtil.IterateIndexList(FaceSetIndices, faceSetList, submesh.FaceSets);

            BoneIndices = submesh.BoneIndices;

        }

    }
}
