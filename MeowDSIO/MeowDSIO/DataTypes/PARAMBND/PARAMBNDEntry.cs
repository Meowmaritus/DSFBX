using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.PARAMBND
{
    public class PARAMBNDEntry
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

        public DataFiles.PARAM Param { get; set; }

        public PARAMBNDEntry(string Name, DataFiles.PARAM Param)
        {
            this.Name = Name;
            this.Param = Param;
        }
    }
}
