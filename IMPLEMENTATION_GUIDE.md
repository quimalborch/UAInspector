# OPC UA Implementation Guide

This guide explains how to add full OPC UA functionality to UAInspector.

## Phase 1: Install OPC Foundation Libraries

### Option A: Using NuGet Package Manager (Recommended)

1. Right-click on the `UAInspector` project in Solution Explorer
2. Select "Manage NuGet Packages..."
3. Click "Browse" tab
4. Search for and install:
   - `OPCFoundation.NetStandard.Opc.Ua` (v1.5.x or later)
   - `OPCFoundation.NetStandard.Opc.Ua.Client`

### Option B: Package Manager Console

```powershell
Install-Package OPCFoundation.NetStandard.Opc.Ua
Install-Package OPCFoundation.NetStandard.Opc.Ua.Client
```

## Phase 2: Create OPC UA Client Service

Create `Core/Services/OpcClientService.cs`:

```csharp
using System;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using UAInspector.Core.Models;

namespace UAInspector.Core.Services
{
    public class OpcClientService
    {
 private ApplicationInstance _application;
      private Session _session;
        private ApplicationConfiguration _configuration;

   public bool IsConnected => _session != null && _session.Connected;
        public event EventHandler<DataChangedEventArgs> DataChanged;

        public async Task InitializeAsync()
     {
   _application = new ApplicationInstance
    {
     ApplicationName = "UAInspector",
                ApplicationType = ApplicationType.Client,
        ConfigSectionName = "UAInspector"
       };

    // Load application configuration
       _configuration = await _application.LoadApplicationConfiguration(false);

            // Check application certificate
   bool certOk = await _application.CheckApplicationInstanceCertificate(false, 0);
            if (!certOk)
     {
         throw new Exception("Application instance certificate invalid!");
      }
        }

        public async Task<bool> ConnectAsync(OpcServerInfo serverInfo, 
LoginType loginType, string username = null, string password = null)
      {
     try
            {
   var endpointDescription = CoreClientUtils.SelectEndpoint(
   serverInfo.Url, 
               useSecurity: loginType != LoginType.Anonymous
       );

            var endpointConfiguration = EndpointConfiguration.Create(_configuration);
           var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

        // Create user identity
  UserIdentity userIdentity;
    switch (loginType)
        {
  case LoginType.UserNamePassword:
      userIdentity = new UserIdentity(username, password);
          break;
  case LoginType.Certificate:
        // Load certificate here
         userIdentity = new UserIdentity();
  break;
        default:
         userIdentity = new UserIdentity();
   break;
        }

        // Create session
                _session = await Session.Create(
    _configuration,
         endpoint,
          false,
              "UAInspector",
 60000,
         userIdentity,
      null
                );

   _session.KeepAlive += Session_KeepAlive;

          return true;
        }
         catch (Exception ex)
      {
             System.Diagnostics.Debug.WriteLine($"Connection error: {ex.Message}");
  return false;
            }
      }

        public async Task DisconnectAsync()
    {
            if (_session != null)
  {
   _session.KeepAlive -= Session_KeepAlive;
             _session.Close();
       _session.Dispose();
                _session = null;
            }
        }

        public async Task<List<OpcNodeInfo>> BrowseAsync(string nodeId = null)
        {
          if (!IsConnected) return new List<OpcNodeInfo>();

            try
        {
      var browseNodeId = string.IsNullOrEmpty(nodeId) 
            ? ObjectIds.ObjectsFolder 
         : NodeId.Parse(nodeId);

          var browser = new Browser(_session)
  {
               BrowseDirection = BrowseDirection.Forward,
    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
         IncludeSubtypes = true,
            NodeClassMask = (int)NodeClass.Object | (int)NodeClass.Variable
        };

       var references = browser.Browse(browseNodeId);
        var nodes = new List<OpcNodeInfo>();

      foreach (var reference in references)
       {
        var node = new OpcNodeInfo
         {
     NodeId = reference.NodeId.ToString(),
            DisplayName = reference.DisplayName.Text,
       BrowseName = reference.BrowseName.Name,
NodeClass = ConvertNodeClass(reference.NodeClass),
          HasChildren = reference.ReferenceTypeId != null
            };

         nodes.Add(node);
    }

            return nodes;
 }
   catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Browse error: {ex.Message}");
 return new List<OpcNodeInfo>();
      }
    }

 public async Task<object> ReadValueAsync(string nodeId)
        {
     if (!IsConnected) return null;

            try
            {
          var node = NodeId.Parse(nodeId);
    var value = _session.ReadValue(node);
    return value.Value;
    }
            catch (Exception ex)
            {
        System.Diagnostics.Debug.WriteLine($"Read error: {ex.Message}");
        return null;
  }
        }

     public async Task<bool> WriteValueAsync(string nodeId, object value)
        {
            if (!IsConnected) return false;

       try
            {
   var node = NodeId.Parse(nodeId);
    var writeValue = new WriteValue
          {
   NodeId = node,
           AttributeId = Attributes.Value,
          Value = new DataValue(new Variant(value))
      };

  var writeValueCollection = new WriteValueCollection { writeValue };
                
_session.Write(
     null,
          writeValueCollection,
   out StatusCodeCollection results,
      out DiagnosticInfoCollection diagnosticInfos
   );

         return StatusCode.IsGood(results[0]);
     }
   catch (Exception ex)
            {
              System.Diagnostics.Debug.WriteLine($"Write error: {ex.Message}");
 return false;
 }
        }

        public async Task<Subscription> CreateSubscriptionAsync(int publishingInterval = 1000)
      {
            if (!IsConnected) return null;

       var subscription = new Subscription(_session.DefaultSubscription)
     {
         PublishingInterval = publishingInterval,
        PublishingEnabled = true
       };

            _session.AddSubscription(subscription);
          subscription.Create();

       return subscription;
        }

        public void AddMonitoredItem(Subscription subscription, string nodeId, 
      MonitoredItemNotificationEventHandler callback)
        {
    var monitoredItem = new MonitoredItem(subscription.DefaultItem)
            {
         StartNodeId = NodeId.Parse(nodeId),
    AttributeId = Attributes.Value,
   SamplingInterval = 1000,
         QueueSize = 10,
          DiscardOldest = true
         };

         monitoredItem.Notification += callback;
     subscription.AddItem(monitoredItem);
       subscription.ApplyChanges();
 }

   private void Session_KeepAlive(Session session, KeepAliveEventArgs e)
        {
            if (e.Status != null && ServiceResult.IsNotGood(e.Status))
  {
  System.Diagnostics.Debug.WriteLine($"KeepAlive error: {e.Status}");
       }
        }

        private OpcNodeClass ConvertNodeClass(NodeClass nodeClass)
        {
      switch (nodeClass)
 {
     case NodeClass.Object: return OpcNodeClass.Object;
          case NodeClass.Variable: return OpcNodeClass.Variable;
   case NodeClass.Method: return OpcNodeClass.Method;
 case NodeClass.ObjectType: return OpcNodeClass.ObjectType;
     case NodeClass.VariableType: return OpcNodeClass.VariableType;
      case NodeClass.ReferenceType: return OpcNodeClass.ReferenceType;
           case NodeClass.DataType: return OpcNodeClass.DataType;
      case NodeClass.View: return OpcNodeClass.View;
                default: return OpcNodeClass.Object;
            }
  }
    }

    public class DataChangedEventArgs : EventArgs
    {
        public string NodeId { get; set; }
     public object Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
```

## Phase 3: Create Discovery Service

Create `Core/Services/DiscoveryService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using UAInspector.Core.Models;

namespace UAInspector.Core.Services
{
    public class DiscoveryService
    {
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
     "opc.tcp://localhost:62541"
      };

       foreach (var url in discoveryUrls)
                {
        try
  {
        var discoveredServers = await DiscoverAsync(url);
        servers.AddRange(discoveredServers);
    }
  catch
        {
         // Continue with next URL
       }
                }

        return servers.Distinct().ToList();
            }
            catch (Exception ex)
            {
        System.Diagnostics.Debug.WriteLine($"Discovery error: {ex.Message}");
       return servers;
 }
        }

        private async Task<List<OpcServerInfo>> DiscoverAsync(string discoveryUrl)
        {
  var servers = new List<OpcServerInfo>();

       try
          {
      var endpointConfiguration = EndpointConfiguration.Create();
    using (var client = DiscoveryClient.Create(new Uri(discoveryUrl), endpointConfiguration))
          {
         var applicationDescriptions = client.FindServers(null);

      foreach (var app in applicationDescriptions)
              {
 var endpoints = client.GetEndpoints(null);

   foreach (var endpoint in endpoints)
     {
       var server = new OpcServerInfo
        {
  Id = Guid.NewGuid().ToString(),
             Name = app.ApplicationName.Text,
           Url = endpoint.EndpointUrl,
          Manufacturer = app.ProductUri,
  ProductName = app.ApplicationName.Text,
      SecurityMode = endpoint.SecurityMode.ToString(),
     SecurityPolicy = endpoint.SecurityPolicyUri,
     LastConnected = DateTime.MinValue,
    IsFavorite = false,
         LastLoginType = LoginType.Anonymous
       };

      servers.Add(server);
    }
  }
          }
            }
         catch (Exception ex)
            {
    System.Diagnostics.Debug.WriteLine($"Discovery error for {discoveryUrl}: {ex.Message}");
 }

   return servers;
        }
  }
}
```

## Phase 4: Update ViewModels

### Update ServerListViewModel

Add these members:

```csharp
private readonly OpcClientService _opcClientService;
private readonly DiscoveryService _discoveryService;

// In constructor:
_opcClientService = new OpcClientService();
_discoveryService = new DiscoveryService();

// Initialize OPC client:
await _opcClientService.InitializeAsync();
```

Update `DiscoverServers` method:

```csharp
private async void DiscoverServers()
{
    IsDiscovering = true;
    DiscoveredServers.Clear();

    try
    {
        var servers = await _discoveryService.DiscoverServersOnNetworkAsync();
        
 foreach (var server in servers)
     {
    DiscoveredServers.Add(server);
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Discovery error: {ex.Message}");
    }
    finally
    {
        IsDiscovering = false;
    }
}
```

Update `Connect` method:

```csharp
private async void Connect()
{
  if (SelectedServer == null)
        return;

    // TODO: Show login dialog to get credentials
    var success = await _opcClientService.ConnectAsync(
        SelectedServer,
        LoginType.Anonymous
    );

    if (success)
    {
   _mainViewModel.OnConnected(SelectedServer);
    }
}
```

## Phase 5: Create Explorer View

This will show the OPC UA address space in a TreeView with node details and the ability to read/write values.

Create `Views/ExplorerView.xaml` and `ExplorerViewModel.cs` to browse and interact with nodes.

## Phase 6: Create Login Dialog

Create a dialog window to handle authentication:
- Anonymous
- Username/Password fields
- Certificate selector

## Phase 7: Add Certificate Management

Implement certificate store management in the Settings view.

## Phase 8: Add Real-Time Subscriptions

Implement OPC UA subscriptions to show live data updates in the Explorer view.

## Testing with a Sample OPC UA Server

To test your implementation, you can use:

1. **OPC Foundation Sample Server**
   - Download from: https://github.com/OPCFoundation/UA-.NETStandard-Samples
   - Default URL: `opc.tcp://localhost:62541`

2. **Prosys OPC UA Simulation Server**
   - Download from: https://www.prosysopc.com/products/opc-ua-simulation-server/
   - Free for testing

3. **Unified Automation UaExpert**
   - Has a built-in demo server

## Configuration File

Create an `App.Config` section for OPC UA:

```xml
<configuration>
  <configSections>
    <section name="UAInspector" type="Opc.Ua.ApplicationConfigurationSection,Opc.Ua.Core" />
  </configSections>
  
  <UAInspector>
    <ApplicationConfiguration>
    <ApplicationName>UAInspector</ApplicationName>
      <ApplicationType>Client_1</ApplicationType>
      <ApplicationUri>urn:localhost:UAInspector</ApplicationUri>
  <ProductUri>http://opcfoundation.org/UAInspector</ProductUri>
      
      <SecurityConfiguration>
        <ApplicationCertificate>
          <StoreType>Directory</StoreType>
          <StorePath>%LocalApplicationData%/UAInspector/Certificates/own</StorePath>
        <SubjectName>CN=UAInspector, O=OPC Foundation, OU=Client</SubjectName>
        </ApplicationCertificate>
      
        <TrustedPeerCertificates>
          <StoreType>Directory</StoreType>
     <StorePath>%LocalApplicationData%/UAInspector/Certificates/trusted</StorePath>
        </TrustedPeerCertificates>
        
        <RejectedCertificateStore>
          <StoreType>Directory</StoreType>
      <StorePath>%LocalApplicationData%/UAInspector/Certificates/rejected</StorePath>
     </RejectedCertificateStore>
  </SecurityConfiguration>
      
      <TransportConfigurations></TransportConfigurations>
      <TransportQuotas>
        <OperationTimeout>120000</OperationTimeout>
        <MaxStringLength>1048576</MaxStringLength>
        <MaxByteStringLength>1048576</MaxByteStringLength>
        <MaxArrayLength>65535</MaxArrayLength>
        <MaxMessageSize>4194304</MaxMessageSize>
    <MaxBufferSize>65535</MaxBufferSize>
        <ChannelLifetime>300000</ChannelLifetime>
        <SecurityTokenLifetime>3600000</SecurityTokenLifetime>
      </TransportQuotas>
      
      <ClientConfiguration>
        <DefaultSessionTimeout>600000</DefaultSessionTimeout>
        <MinSubscriptionLifetime>10000</MinSubscriptionLifetime>
      </ClientConfiguration>
    </ApplicationConfiguration>
  </UAInspector>
</configuration>
```

## Next Steps

1. Install NuGet packages
2. Implement OpcClientService
3. Update ServerListViewModel with OPC connection logic
4. Create LoginDialog for authentication
5. Create ExplorerView for browsing nodes
6. Add subscription support for real-time monitoring
7. Test with sample OPC UA servers

## Useful Resources

- [OPC Foundation .NET Standard](https://github.com/OPCFoundation/UA-.NETStandard)
- [OPC UA Specification](https://reference.opcfoundation.org/)
- [OPC UA Client Tutorial](https://documentation.unified-automation.com/uasdkcpp/1.7.0/html/_l2_ua_client_tutorial.html)
