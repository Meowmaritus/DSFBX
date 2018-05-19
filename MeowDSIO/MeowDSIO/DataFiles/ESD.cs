using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataFiles
{
    public class ESD : DataFile
    {

        protected override void Read(DSBinaryReader bin, IProgress<(int, int)> prog)
        {
            throw new NotImplementedException();
        }

        protected override void Write(DSBinaryWriter bin, IProgress<(int, int)> prog)
        {
            throw new NotImplementedException();
        }
    }
}
