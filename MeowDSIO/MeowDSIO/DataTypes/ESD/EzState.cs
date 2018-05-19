using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.ESD
{
    public class EzState
    {
        public List<EzFunc> OnEnableFunctions = new List<EzFunc>();
        public List<EzFunc> OnDisableFunctions = new List<EzFunc>();

        public List<EzCondition> Conditions = new List<EzCondition>();
    }
}
