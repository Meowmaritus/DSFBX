using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSFBX
{
    public class DSFBXGenericEventArgs<T> : EventArgs
    {
        public T Parameter;
        public DSFBXGenericEventArgs(T Parameter)
        {
            this.Parameter = Parameter;
        }
    }
}
