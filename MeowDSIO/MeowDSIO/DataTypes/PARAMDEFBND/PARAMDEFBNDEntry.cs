using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.PARAMDEFBND
{
    public class PARAMDEFBNDEntry
    {
        private string name;
        public string Name
        {
            get => name;
            set
            {
                var oldName = name;
                name = value;
                var handler = NameChanged;
                handler?.Invoke(this, oldName, name);
            }
        }
        public delegate void NameChangedDelegate(object sender, string oldName, string newName);
        public event NameChangedDelegate NameChanged;

        public DataFiles.PARAMDEF ParamDef { get; set; }

        public PARAMDEFBNDEntry(string Name, DataFiles.PARAMDEF ParamDef)
        {
            this.Name = Name;
            this.ParamDef = ParamDef;
        }
    }
}
