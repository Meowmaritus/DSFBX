using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DSFBX_GUI
{
    public class DSFBXContext : INotifyPropertyChanged
    {
        private DSFBXConfig _config = new DSFBXConfig();
        public DSFBXConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
