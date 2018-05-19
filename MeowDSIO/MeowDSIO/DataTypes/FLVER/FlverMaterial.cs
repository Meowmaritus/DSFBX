using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverMaterial
    {
        public string Name { get; set; } = null;
        public string MTDName { get; set; } = null;

        public List<FlverMaterialParameter> Parameters { get; set; } 
            = new List<FlverMaterialParameter>();

        public int Flags { get; set; } = 0;

        public int UnknownInt1 { get; set; } = 0;
        public int UnknownInt2 { get; set; } = 0;
        public int UnknownInt3 { get; set; } = 0;

        public override string ToString()
        {
            return $"{Name} [MTD:\"{MTDName}\"]";
        }
    }
}
