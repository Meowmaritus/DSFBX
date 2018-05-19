using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSFBX
{
    public class MtdChannelInfo
    {
        public string Letter { get; set; } = null;
        public string MaterialParameter { get; set; } = null;
        public MtdChannelInfo(string Letter, string MaterialParameter)
        {
            this.Letter = Letter;
            this.MaterialParameter = MaterialParameter;
        }
    }
}
