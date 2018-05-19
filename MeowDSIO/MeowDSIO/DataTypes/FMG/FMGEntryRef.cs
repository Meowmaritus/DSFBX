using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FMG
{
    public class FMGEntryRef : INotifyPropertyChanged
    {
        public event EventHandler<FMGEntryRefValueModifiedEventArgs> ValueModified;
        protected virtual void OnValueModified(FMGEntryRefValueModifiedEventArgs args)
        {
            var handler = ValueModified;
            handler?.Invoke(this, args);
        }

        public event EventHandler<FMGEntryRefIsMarkedForExportChangedEventArgs> IsMarkedForExportChanged;
        protected virtual void OnIsMarkedForExportChanged(FMGEntryRefIsMarkedForExportChangedEventArgs args)
        {
            var handler = IsMarkedForExportChanged;
            handler?.Invoke(this, args);
        }

        private int _id = 0;
        public int ID
        {
            get => _id;
            set
            {
                _id = value;
                RaisePropertyChanged();
            }
        }

        private string _value = null;
        public string Value
        {
            get => _value;
            set
            {
                string oldValue = _value;

                if (oldValue != value)
                {
                    _value = value;
                    RaisePropertyChanged();
                    IsModified = true;

                    OnValueModified(new FMGEntryRefValueModifiedEventArgs(oldValue, Value));

                    RaisePropertyChanged(nameof(ValuePreview));
                }
            }
        }

        public string ValuePreview
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Value))
                    return Value;

                var lines = Value.Split('\n');

                if (lines.Length == 1)
                    return lines[0];

                return lines.Where(x => !string.IsNullOrWhiteSpace(x)).First() + "...";
            }
        }

        private bool _isModified = false;
        public bool IsModified
        {
            get => _isModified;
            set
            {
                _isModified = value;
                RaisePropertyChanged();
            }
        }

        internal bool _isMarkedForExport = false;
        public bool IsMarkedForExport
        {
            get => _isMarkedForExport;
            set
            {
                var oldValue = _isMarkedForExport;
                _isMarkedForExport = value;
                RaisePropertyChanged();

                if (value != oldValue)
                {
                    OnIsMarkedForExportChanged(new FMGEntryRefIsMarkedForExportChangedEventArgs(value));
                }
            }
        }

        public FMGEntryRef(int ID, string Value)
        {
            _id = ID;
           _value = Value;
        }



        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged([CallerMemberName] string caller = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
    }

    public class FMGEntryRefValueModifiedEventArgs : EventArgs
    {
        public string OldValue { get; set; } = null;
        public string NewValue { get; set; } = null;

        public FMGEntryRefValueModifiedEventArgs(string OldValue, string NewValue)
        {
            this.OldValue = OldValue;
            this.NewValue = NewValue;
        }
    }

    public class FMGEntryRefIsMarkedForExportChangedEventArgs : EventArgs
    {
        public bool Value { get; set; }
        public FMGEntryRefIsMarkedForExportChangedEventArgs(bool Value)
        {
            this.Value = Value;
        }
    }
}
