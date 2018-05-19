using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverWriteFunc_MaterialInfo
    {
        public List<int> MaterialParameterIndices { get; set; } = new List<int>();

        public FlverWriteFunc_MaterialInfo(FlverMaterial material,
            List<FlverMaterialParameter> materialParameterList)
        {
            MiscUtil.IterateIndexList(MaterialParameterIndices, materialParameterList, material.Parameters);
        }
    }
}
