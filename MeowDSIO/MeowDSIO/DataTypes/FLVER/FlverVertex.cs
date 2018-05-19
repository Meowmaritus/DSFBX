using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverVertex
    {
        public FlverVector3 Position { get; set; } = null;
        public FlverBoneIndices BoneIndices { get; set; } = null;
        public FlverBoneWeights BoneWeights { get; set; } = null;
        public FlverPackedVector4 Normal { get; set; } = null;
        public FlverPackedVector4 BiTangent { get; set; } = null;
        public FlverPackedVector4 UnknownVector4A { get; set; } = null;
        public FlverVertexColor VertexColor { get; set; } = null;

        public List<FlverUV> UVs { get; set; } = new List<FlverUV>();

        
    }
}
