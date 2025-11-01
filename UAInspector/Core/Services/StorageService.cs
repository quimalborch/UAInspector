using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UAInspector.Core.Models;

namespace UAInspector.Core.Services
{
    /// <summary>
    /// Simple file-based storage service using XML serialization
    /// </summary>
    public class StorageService
    {
        private readonly string _dataPath;
        private const string ServersFileName = "servers.xml";
        private const string SettingsFileName = "settings.xml";
        private const string FavoritesFileName = "favorites.xml";

        public StorageService()
        {
            _dataPath = Path.Combine(
       Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
     "UAInspector",
            "Data"
        );

       if (!Directory.Exists(_dataPath))
    {
        Directory.CreateDirectory(_dataPath);
   }
     }

        public List<OpcServerInfo> LoadServers()
  {
  try
        {
       var filePath = Path.Combine(_dataPath, ServersFileName);
         if (!File.Exists(filePath))
     return new List<OpcServerInfo>();

  var serializer = new XmlSerializer(typeof(List<OpcServerInfo>));
         using (var stream = File.OpenRead(filePath))
     {
      return (List<OpcServerInfo>)serializer.Deserialize(stream);
   }
      }
     catch
    {
        return new List<OpcServerInfo>();
        }
  }

        public void SaveServers(List<OpcServerInfo> servers)
  {
     try
     {
            var filePath = Path.Combine(_dataPath, ServersFileName);
          var serializer = new XmlSerializer(typeof(List<OpcServerInfo>));
      using (var stream = File.Create(filePath))
    {
        serializer.Serialize(stream, servers);
         }
   }
 catch (Exception ex)
  {
    System.Diagnostics.Debug.WriteLine($"Error saving servers: {ex.Message}");
  }
        }

        public AppSettings LoadSettings()
        {
      try
      {
     var filePath = Path.Combine(_dataPath, SettingsFileName);
  if (!File.Exists(filePath))
     return new AppSettings();

       var serializer = new XmlSerializer(typeof(AppSettings));
  using (var stream = File.OpenRead(filePath))
      {
        return (AppSettings)serializer.Deserialize(stream);
      }
   }
    catch
        {
   return new AppSettings();
   }
  }

   public void SaveSettings(AppSettings settings)
    {
            try
    {
        settings.Modified = DateTime.UtcNow;
            var filePath = Path.Combine(_dataPath, SettingsFileName);
    var serializer = new XmlSerializer(typeof(AppSettings));
   using (var stream = File.Create(filePath))
     {
       serializer.Serialize(stream, settings);
            }
      }
   catch (Exception ex)
       {
       System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
   }
  }

        public List<string> LoadFavoriteNodes()
  {
      try
       {
    var filePath = Path.Combine(_dataPath, FavoritesFileName);
     if (!File.Exists(filePath))
    return new List<string>();

     var serializer = new XmlSerializer(typeof(List<string>));
      using (var stream = File.OpenRead(filePath))
     {
           return (List<string>)serializer.Deserialize(stream);
  }
 }
        catch
       {
   return new List<string>();
      }
  }

 public void SaveFavoriteNodes(List<string> nodeIds)
     {
   try
            {
            var filePath = Path.Combine(_dataPath, FavoritesFileName);
   var serializer = new XmlSerializer(typeof(List<string>));
   using (var stream = File.Create(filePath))
{
      serializer.Serialize(stream, nodeIds);
 }
 }
       catch (Exception ex)
    {
  System.Diagnostics.Debug.WriteLine($"Error saving favorites: {ex.Message}");
       }
        }

        public void AddOrUpdateServer(OpcServerInfo server)
        {
   var servers = LoadServers();
          var existing = servers.FirstOrDefault(s => s.Id == server.Id);
            
      if (existing != null)
      {
    servers.Remove(existing);
    }
    
  server.LastConnected = DateTime.UtcNow;
      servers.Insert(0, server);
            
      // Keep only last 50 servers
   if (servers.Count > 50)
          {
     servers = servers.Take(50).ToList();
   }
  
       SaveServers(servers);
        }
    }
}
