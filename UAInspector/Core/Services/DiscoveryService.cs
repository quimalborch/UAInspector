using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using UAInspector.Core.Models;

namespace UAInspector.Core.Services
{
    /// <summary>
 /// Service for discovering OPC UA servers on the network
    /// </summary>
    public class DiscoveryService
    {
   /// <summary>
    /// Discover OPC UA servers on common ports
/// </summary>
   public async Task<List<OpcServerInfo>> DiscoverServersOnNetworkAsync()
        {
  var servers = new List<OpcServerInfo>();

   try
   {
     // Common OPC UA discovery URLs
var discoveryUrls = new[]
      {
  "opc.tcp://localhost:4840",
     "opc.tcp://localhost:48010",
   "opc.tcp://localhost:62541",
    "opc.tcp://localhost:53530"
     };

foreach (var url in discoveryUrls)
    {
  try
       {
        var discoveredServers = await DiscoverAsync(url);
            servers.AddRange(discoveredServers);
   }
      catch (Exception ex)
 {
System.Diagnostics.Debug.WriteLine($"Discovery failed for {url}: {ex.Message}");
          // Continue with next URL
       }
     }

   // Remove duplicates based on URL
   servers = servers
   .GroupBy(s => s.Url)
.Select(g => g.First())
      .ToList();

    return servers;
  }
    catch (Exception ex)
        {
     System.Diagnostics.Debug.WriteLine($"Discovery error: {ex.Message}");
      return servers;
       }
  }

        /// <summary>
 /// Discover servers at a specific discovery URL
   /// </summary>
private async Task<List<OpcServerInfo>> DiscoverAsync(string discoveryUrl)
        {
var servers = new List<OpcServerInfo>();

  try
   {
     var endpointConfiguration = EndpointConfiguration.Create();
   endpointConfiguration.OperationTimeout = 10000; // 10 second timeout

   using (var client = DiscoveryClient.Create(new Uri(discoveryUrl), endpointConfiguration))
    {
        // Find servers
     var applicationDescriptions = client.FindServers(null);

      foreach (var app in applicationDescriptions)
         {
    try
  {
    // Get endpoints for each server
     var endpoints = client.GetEndpoints(null);
         
    // Group endpoints by URL
    var endpointGroups = endpoints.GroupBy(e => e.EndpointUrl);

  foreach (var group in endpointGroups)
         {
       var endpoint = group.OrderByDescending(e => e.SecurityLevel).First();

      var server = new OpcServerInfo
   {
  Id = Guid.NewGuid().ToString(),
 Name = app.ApplicationName.Text,
      Url = endpoint.EndpointUrl,
         Manufacturer = GetManufacturerFromUri(app.ProductUri),
   ProductName = app.ApplicationName.Text,
      SecurityMode = endpoint.SecurityMode.ToString(),
   SecurityPolicy = GetSecurityPolicyName(endpoint.SecurityPolicyUri),
     LastConnected = DateTime.MinValue,
 IsFavorite = false,
  LastLoginType = endpoint.SecurityMode == MessageSecurityMode.None 
 ? LoginType.Anonymous 
    : LoginType.UserNamePassword
    };

      servers.Add(server);
      }
      }
     catch (Exception ex)
               {
       System.Diagnostics.Debug.WriteLine($"Error getting endpoints for {app.ApplicationName}: {ex.Message}");
      }
     }
   }

     return await Task.FromResult(servers);
 }
  catch (Exception ex)
       {
 System.Diagnostics.Debug.WriteLine($"Discovery error for {discoveryUrl}: {ex.Message}");
   return await Task.FromResult(servers);
 }
   }

 /// <summary>
        /// Discover servers using Local Discovery Server (LDS)
     /// </summary>
      public async Task<List<OpcServerInfo>> DiscoverServersWithLDSAsync()
        {
var servers = new List<OpcServerInfo>();

      try
    {
     // Default LDS URLs
    var ldsUrls = new[]
      {
      "opc.tcp://localhost:4840", // Standard LDS port
    "opc.tcp://localhost:4845"  // Alternative port
  };

       foreach (var ldsUrl in ldsUrls)
     {
    try
  {
  var endpointConfiguration = EndpointConfiguration.Create();
  endpointConfiguration.OperationTimeout = 10000;

      using (var client = DiscoveryClient.Create(new Uri(ldsUrl), endpointConfiguration))
     {
  // Use FindServers instead of FindServersOnNetwork for better compatibility
   var applicationDescriptions = client.FindServers(null);
             
     foreach (var app in applicationDescriptions)
      {
     var serverInfo = new OpcServerInfo
     {
          Id = Guid.NewGuid().ToString(),
  Name = app.ApplicationName.Text,
Url = app.DiscoveryUrls.FirstOrDefault() ?? ldsUrl,
    Manufacturer = GetManufacturerFromUri(app.ProductUri),
       ProductName = app.ApplicationName.Text,
SecurityMode = "Unknown",
    SecurityPolicy = "Unknown",
  LastConnected = DateTime.MinValue,
       IsFavorite = false,
LastLoginType = LoginType.Anonymous
     };

   servers.Add(serverInfo);
   }
  }
    }
      catch (Exception ex)
       {
     System.Diagnostics.Debug.WriteLine($"LDS discovery failed for {ldsUrl}: {ex.Message}");
  }
     }

     return await Task.FromResult(servers);
    }
  catch (Exception ex)
            {
     System.Diagnostics.Debug.WriteLine($"LDS discovery error: {ex.Message}");
         return servers;
    }
    }

   /// <summary>
  /// Get endpoints for a specific server URL
  /// </summary>
  public async Task<List<EndpointDescription>> GetEndpointsAsync(string serverUrl)
    {
            try
       {
 var endpointConfiguration = EndpointConfiguration.Create();
     endpointConfiguration.OperationTimeout = 10000;

   using (var client = DiscoveryClient.Create(new Uri(serverUrl), endpointConfiguration))
       {
   var endpoints = client.GetEndpoints(null);
        return await Task.FromResult(endpoints.ToList());
       }
        }
       catch (Exception ex)
     {
    System.Diagnostics.Debug.WriteLine($"Get endpoints error: {ex.Message}");
     return new List<EndpointDescription>();
      }
        }

   private string GetManufacturerFromUri(string productUri)
   {
   if (string.IsNullOrEmpty(productUri))
    return "Unknown";

   try
     {
       var uri = new Uri(productUri);
    return uri.Host;
  }
       catch
       {
      return "Unknown";
  }
    }

   private string GetSecurityPolicyName(string securityPolicyUri)
   {
         if (string.IsNullOrEmpty(securityPolicyUri))
   return "None";

          try
       {
     var parts = securityPolicyUri.Split('#');
      return parts.Length > 1 ? parts[1] : "None";
 }
catch
      {
   return "Unknown";
  }
     }
    }
}
