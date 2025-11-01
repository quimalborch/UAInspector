using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UAInspector.Core.Models
{
    /// <summary>
    /// Represents an OPC UA node in the address space
    /// </summary>
    public class OpcNodeInfo : INotifyPropertyChanged
    {
        private object _value;
        private DateTime _timestamp;
        private string _quality;

        public string NodeId { get; set; }
        public string DisplayName { get; set; }
        public string BrowseName { get; set; }
        public OpcNodeClass NodeClass { get; set; }
        public string DataType { get; set; }

        public object Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Quality
        {
            get => _quality;
            set
            {
                if (_quality != value)
                {
                    _quality = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsWritable { get; set; }
        public bool IsFavorite { get; set; }

        public ObservableCollection<OpcNodeInfo> Children { get; set; }
        public bool HasChildren { get; set; }
        public bool IsExpanded { get; set; }
        public bool IsLoaded { get; set; }

        public OpcNodeInfo()
        {
            Children = new ObservableCollection<OpcNodeInfo>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum OpcNodeClass
    {
        Object,
        Variable,
        Method,
        ObjectType,
        VariableType,
        ReferenceType,
        DataType,
        View
    }
}
