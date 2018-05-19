using MeowDSIO.DataTypes.FLVER;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.Exceptions.DSWrite
{
    public class FlverVertexStructDataNullException : DSWriteException
    {
        public FlverVertexStructDataNullException(DSBinaryWriter bin, FlverVertexStructMember member, int meshIndex, int vertexIndex)
            : base(bin, $"FLVER Vertex Struct Layout Includes Member with " +
                  $"[Value Type: {member.ValueType}] and" +
                  $"[Semantic: {member.Semantic}] but that member's value was [NULL] on Submeshes[{meshIndex}].Vertices[{vertexIndex}]")
        {

        }
    }
}
