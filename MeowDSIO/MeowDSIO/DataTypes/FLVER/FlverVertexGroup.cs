using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverVertexGroup
    {
        [Newtonsoft.Json.JsonIgnore]
        public FlverSubmesh ContainingSubmesh { get; set; } = null;

        public FlverVertexGroup(FlverSubmesh ContainingSubmesh)
        {
            this.ContainingSubmesh = ContainingSubmesh;
        }

        //public FlverVertexStructLayout VertexStructLayout { get; set; }
        public int VertexStructLayoutIndex { get; set; }

        public int VertexCount { get; set; }
        public int VertexSize { get; set; } = 0;

        public int UnknownInt1 { get; set; }
        public int UnknownInt2 { get; set; }
        public int UnknownInt3 { get; set; }
    }
}
