using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DSFBX_GUI
{
    public class DSFBXConfig : INotifyPropertyChanged
    {
        private string _importSkeletonPath = "";
        public string ImportSkeletonPath
        {
            get => _importSkeletonPath;
            set
            {
                _importSkeletonPath = value;
                RaisePropertyChanged();
            }
        }

        private bool _importSkeletonEnable = false;
        public bool ImportSkeletonEnable
        {
            get => _importSkeletonEnable;
            set
            {
                _importSkeletonEnable = value;
                RaisePropertyChanged();
            }
        }

        private bool _launchModelViewerAfterImport = false;
        public bool LaunchModelViewerAfterImport
        {
            get => _launchModelViewerAfterImport;
            set
            {
                _launchModelViewerAfterImport = value;
                RaisePropertyChanged();
            }
        }

        private bool _generateBackup = true;
        public bool GenerateBackup
        {
            get => _generateBackup;
            set
            {
                _generateBackup = value;
                RaisePropertyChanged();
            }
        }

        private bool _importDoubleSided = true;
        public bool ImportDoubleSided
        {
            get => _importDoubleSided;
            set
            {
                _importDoubleSided = value;
                RaisePropertyChanged();
            }
        }

        private string _inputFBX = "";
        public string InputFBX
        {
            get => _inputFBX;
            set
            {
                _inputFBX = value;
                RaisePropertyChanged();
            }
        }

        private string _outputBND = "";
        public string OutputBND
        {
            get => _outputBND;
            set
            {
                _outputBND = value;
                RaisePropertyChanged();
            }
        }

        private int _modelIndex = 0;
        public int ModelIndex
        {
            get => _modelIndex;
            set
            {
                _modelIndex = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ModelIndexRadioButtonChecked0));
                RaisePropertyChanged(nameof(ModelIndexRadioButtonChecked1));
                RaisePropertyChanged(nameof(ModelIndexRadioButtonChecked2));
                RaisePropertyChanged(nameof(ModelIndexRadioButtonChecked3));
            }
        }

        private double _scalePercent = 100.0;
        public double ScalePercent
        {
            get => _scalePercent;
            set
            {
                _scalePercent = value;
                RaisePropertyChanged();
            }
        }

        private double _ImportedSkeletonScalePercent = 100.0;
        public double ImportedSkeletonScalePercent
        {
            get => _ImportedSkeletonScalePercent;
            set
            {
                _ImportedSkeletonScalePercent = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public bool ModelIndexRadioButtonChecked0 
            => ModelIndex == 0;

        [JsonIgnore]
        public bool ModelIndexRadioButtonChecked1 
            => ModelIndex == 1;

        [JsonIgnore]
        public bool ModelIndexRadioButtonChecked2 
            => ModelIndex == 2;

        [JsonIgnore]
        public bool ModelIndexRadioButtonChecked3 
            => ModelIndex == 3;


        private bool _autoClearOutput = true;
        public bool AutoClearOutput
        {
            get => _autoClearOutput;
            set
            {
                _autoClearOutput = value;
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
