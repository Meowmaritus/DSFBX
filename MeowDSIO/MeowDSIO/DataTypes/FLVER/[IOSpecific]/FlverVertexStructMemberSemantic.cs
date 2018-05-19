using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public enum FlverVertexStructMemberSemantic : int
    {
        INVALID = -1,

        Position = 0x00,
        BoneWeights = 0x01,
        BoneIndices = 0x02,
        Normal = 0x03,
        //UNK_0x04 = 0x04,
        UV = 0x05,
        BiTangent = 0x06,

        UnknownVector4A = 0x07,

        //UNK_0x08 = 0x08,
        //UNK_0x09 = 0x09,
        VertexColor = 0x0A,

    }
}
