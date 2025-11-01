using System;

namespace UAInspector.Core.Models
{
    /// <summary>
    /// Application settings and preferences
    /// </summary>
    public class AppSettings
    {
        public string Id { get; set; } = "settings";
        public bool DarkMode { get; set; } = true;
        public string Language { get; set; } = "en-US";
        public int ConnectionTimeout { get; set; } = 30000;
        public int SessionTimeout { get; set; } = 600000;
        public bool AutoReconnect { get; set; } = true;
        public bool EnableSubscriptions { get; set; } = true;
        public int SubscriptionPublishingInterval { get; set; } = 1000;
        public string CertificateStorePath { get; set; } = "Certificates";
        public string ApplicationName { get; set; } = "UAInspector";
        public string ApplicationUri { get; set; } = "urn:UAInspector";
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime Modified { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// Start UAInspector automatically when Windows starts
        /// </summary>
        public bool StartWithWindows { get; set; } = false;
        
        /// <summary>
        /// Last updated timestamp
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
