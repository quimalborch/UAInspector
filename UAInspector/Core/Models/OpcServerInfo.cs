using System;

namespace UAInspector.Core.Models
{
    /// <summary>
    /// Represents an OPC UA server information
    /// </summary>
    public class OpcServerInfo
 {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Manufacturer { get; set; }
        public string ProductName { get; set; }
        public string SecurityMode { get; set; }
     public string SecurityPolicy { get; set; }
        public DateTime LastConnected { get; set; }
 public bool IsFavorite { get; set; }
        public LoginType LastLoginType { get; set; }
   public string Username { get; set; }
        public bool RememberCredentials { get; set; }
    }

    public enum LoginType
    {
        Anonymous,
   UserNamePassword,
        Certificate
    }
}
