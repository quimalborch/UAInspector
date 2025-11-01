using System;
using System.Collections.ObjectModel;

namespace UAInspector.Core.Models
{
    /// <summary>
    /// Represents an OPC UA node in the address space
    /// </summary>
    public class OpcNodeInfo
    {
        public string NodeId { get; set; }
    public string DisplayName { get; set; }
  public string BrowseName { get; set; }
    public OpcNodeClass NodeClass { get; set; }
        public string DataType { get; set; }
     public object Value { get; set; }
 public DateTime Timestamp { get; set; }
     public string Quality { get; set; }
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
