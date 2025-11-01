using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using UAInspector.Core.Models;

namespace UAInspector.Core.Services
{
    /// <summary>
    /// OPC UA Client service for connecting and interacting with OPC UA servers
 /// </summary>
 public class OpcClientService : IDisposable
    {
      private ApplicationInstance _application;
private Session _session;
     private ApplicationConfiguration _configuration;
 private Subscription _subscription;

 public bool IsConnected => _session != null && _session.Connected;
 public Session Session => _session;

      public event EventHandler<DataChangedEventArgs> DataChanged;
    public event EventHandler<EventArgs> ConnectionStatusChanged;

        /// <summary>
     /// Initialize the OPC UA client application
        /// </summary>
public async Task InitializeAsync()
 {
 try
      {
_application = new ApplicationInstance
   {
 ApplicationName = "UAInspector",
         ApplicationType = ApplicationType.Client
   };

// Build application configuration
     _configuration = await BuildApplicationConfiguration();

        System.Diagnostics.Debug.WriteLine("OPC UA client configuration initialized");
    }
catch (Exception ex)
    {
    System.Diagnostics.Debug.WriteLine($"Initialization error: {ex.Message}");
    throw;
 }
     }

        /// <summary>
   /// Build application configuration programmatically
      /// </summary>
    private async Task<ApplicationConfiguration> BuildApplicationConfiguration()
    {
var config = new ApplicationConfiguration
  {
     ApplicationName = "UAInspector",
    ApplicationType = ApplicationType.Client,
        ApplicationUri = Utils.Format(@"urn:{0}:UAInspector", System.Net.Dns.GetHostName()),
    ProductUri = "http://opcfoundation.org/UAInspector",

      ServerConfiguration = new ServerConfiguration
  {
         MaxSubscriptionCount = 100,
      MaxMessageQueueSize = 100,
 MaxNotificationQueueSize = 100,
    MaxPublishRequestCount = 20
  },

 SecurityConfiguration = new SecurityConfiguration
       {
   ApplicationCertificate = new CertificateIdentifier
     {
 StoreType = @"Directory",
      StorePath = @"%LocalApplicationData%/UAInspector/Certificates/own",
 SubjectName = "CN=UAInspector, O=OPC Foundation, OU=Client"
 },
       TrustedPeerCertificates = new CertificateTrustList
 {
    StoreType = @"Directory",
 StorePath = @"%LocalApplicationData%/UAInspector/Certificates/trusted",
   },
     TrustedIssuerCertificates = new CertificateTrustList
       {
   StoreType = @"Directory",
      StorePath = @"%LocalApplicationData%/UAInspector/Certificates/issuers",
   },
   RejectedCertificateStore = new CertificateTrustList
   {
     StoreType = @"Directory",
     StorePath = @"%LocalApplicationData%/UAInspector/Certificates/rejected",
    },
   AutoAcceptUntrustedCertificates = true,
      AddAppCertToTrustedStore = true
   },

 TransportConfigurations = new TransportConfigurationCollection(),
       TransportQuotas = new TransportQuotas
{
 OperationTimeout = 120000,
      MaxStringLength = 1048576,
  MaxByteStringLength = 1048576,
 MaxArrayLength = 65535,
  MaxMessageSize = 4194304,
   MaxBufferSize = 65535,
      ChannelLifetime = 300000,
      SecurityTokenLifetime = 3600000
    },

        ClientConfiguration = new ClientConfiguration
   {
   DefaultSessionTimeout = 600000,
  MinSubscriptionLifetime = 10000
 },

  TraceConfiguration = new TraceConfiguration()
  };

    await config.Validate(ApplicationType.Client);
 return config;
     }

      /// <summary>
  /// Connect to an OPC UA server
   /// </summary>
 public async Task<bool> ConnectAsync(OpcServerInfo serverInfo, LoginType loginType, 
   string username = null, string password = null)
  {
     try
  {
    if (_session != null && _session.Connected)
    {
       await DisconnectAsync();
  }

 // Select endpoint using simple approach
               EndpointDescription endpointDescription = null;
    
    try
 {
using (var discoveryClient = DiscoveryClient.Create(new Uri(serverInfo.Url)))
    {
   var endpoints = discoveryClient.GetEndpoints(null);
        
  // Select endpoint based on security preference
    if (loginType == LoginType.Anonymous)
  {
     // Prefer no security for anonymous
  endpointDescription = endpoints
.OrderBy(e => e.SecurityLevel)
 .FirstOrDefault();
        }
 else
     {
        // Prefer security for authenticated
  endpointDescription = endpoints
.OrderByDescending(e => e.SecurityLevel)
       .FirstOrDefault();
     }
       
   if (endpointDescription == null)
  {
     endpointDescription = endpoints.FirstOrDefault();
    }
     }
 }
   catch (Exception ex)
      {
           System.Diagnostics.Debug.WriteLine($"Endpoint discovery error: {ex.Message}. Using direct URL.");
      // Fallback: create a basic endpoint
endpointDescription = new EndpointDescription
  {
   EndpointUrl = serverInfo.Url,
     SecurityMode = MessageSecurityMode.None,
  SecurityPolicyUri = SecurityPolicies.None,
    ServerCertificate = null,
     SecurityLevel = 0
     };
     }

   var endpointConfig = EndpointConfiguration.Create(_configuration);
       var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfig);

   // Create user identity
      UserIdentity userIdentity;
  switch (loginType)
  {
  case LoginType.UserNamePassword:
     userIdentity = new UserIdentity(username, password);
    break;
     case LoginType.Certificate:
 // TODO: Load user certificate
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

_session.KeepAlive += Session_KeepAliveHandler;

 ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);

    return true;
 }
   catch (Exception ex)
   {
     System.Diagnostics.Debug.WriteLine($"Connection error: {ex.Message}");
     return false;
 }
 }

    /// <summary>
  /// Disconnect from the OPC UA server
   /// </summary>
   public async Task DisconnectAsync()
{
    try
 {
if (_subscription != null)
  {
   _subscription.Delete(true);
    _subscription.Dispose();
    _subscription = null;
   }

if (_session != null)
     {
_session.KeepAlive -= Session_KeepAliveHandler;
       _session.Close();
   _session.Dispose();
    _session = null;
  }

    ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
    }
        catch (Exception ex)
 {
      System.Diagnostics.Debug.WriteLine($"Disconnect error: {ex.Message}");
     }

      await Task.CompletedTask;
   }

      /// <summary>
 /// Browse nodes from a parent node
   /// </summary>
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
     NodeClassMask = (int)(NodeClass.Object | NodeClass.Variable | NodeClass.Method)
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
 HasChildren = reference.TypeDefinition != null && !NodeId.IsNull((NodeId)reference.TypeDefinition)
  };

   // If it's a variable, read its value
if (reference.NodeClass == NodeClass.Variable)
 {
      try
  {
  var nodeToRead = ExpandedNodeId.ToNodeId(reference.NodeId, _session.NamespaceUris);
 var value = _session.ReadValue(nodeToRead);
 node.Value = value.Value;
   node.Timestamp = value.SourceTimestamp;
   node.Quality = value.StatusCode.ToString();

  // Read additional attributes
    var readNode = _session.ReadNode(nodeToRead);
if (readNode is VariableNode varNode)
   {
node.DataType = varNode.DataType.ToString();
node.IsWritable = (varNode.AccessLevel & AccessLevels.CurrentWrite) != 0;
   }
     }
  catch
    {
   // Failed to read value, continue
}
    }

    nodes.Add(node);
 }

     return await Task.FromResult(nodes);
   }
  catch (Exception ex)
   {
       System.Diagnostics.Debug.WriteLine($"Browse error: {ex.Message}");
 return new List<OpcNodeInfo>();
 }
  }

     /// <summary>
/// Read a value from a node
 /// </summary>
   public async Task<DataValue> ReadValueAsync(string nodeId)
        {
  if (!IsConnected) return null;

      try
{
  var node = NodeId.Parse(nodeId);
   var value = _session.ReadValue(node);
     return await Task.FromResult(value);
         }
   catch (Exception ex)
    {
 System.Diagnostics.Debug.WriteLine($"Read error: {ex.Message}");
     return null;
 }
    }

  /// <summary>
  /// Write a value to a node
  /// </summary>
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

        return await Task.FromResult(StatusCode.IsGood(results[0]));
 }
   catch (Exception ex)
       {
         System.Diagnostics.Debug.WriteLine($"Write error: {ex.Message}");
     return false;
 }
 }

  /// <summary>
  /// Create a subscription for monitoring data changes
 /// </summary>
    public async Task<bool> CreateSubscriptionAsync(int publishingInterval = 1000)
    {
    if (!IsConnected) return false;

   try
{
if (_subscription != null)
  {
     _subscription.Delete(true);
_subscription.Dispose();
    }

 _subscription = new Subscription(_session.DefaultSubscription)
  {
      PublishingInterval = publishingInterval,
     PublishingEnabled = true,
      KeepAliveCount = 10,
 LifetimeCount = 100,
MaxNotificationsPerPublish = 1000,
      Priority = 100
 };

   _session.AddSubscription(_subscription);
     _subscription.Create();

   return await Task.FromResult(true);
}
  catch (Exception ex)
   {
    System.Diagnostics.Debug.WriteLine($"Create subscription error: {ex.Message}");
    return false;
 }
  }

  /// <summary>
     /// Add a monitored item to the subscription
  /// </summary>
        public void AddMonitoredItem(string nodeId, Action<MonitoredItem, MonitoredItemNotificationEventArgs> callback)
 {
     if (_subscription == null) return;

  try
     {
  var monitoredItem = new MonitoredItem(_subscription.DefaultItem)
  {
    StartNodeId = NodeId.Parse(nodeId),
            AttributeId = Attributes.Value,
  SamplingInterval = 1000,
   QueueSize = 10,
   DiscardOldest = true
  };

    monitoredItem.Notification += (item, e) =>
           {
   callback?.Invoke(item, e);
 };

    _subscription.AddItem(monitoredItem);
  _subscription.ApplyChanges();
 }
    catch (Exception ex)
       {
   System.Diagnostics.Debug.WriteLine($"Add monitored item error: {ex.Message}");
    }
  }

    /// <summary>
        /// Remove all monitored items from the subscription
      /// </summary>
  public void RemoveAllMonitoredItems()
 {
   if (_subscription != null)
 {
  _subscription.RemoveItems(_subscription.MonitoredItems);
   _subscription.ApplyChanges();
 }
     }

private void Session_KeepAliveHandler(ISession session, KeepAliveEventArgs e)
        {
     if (e.Status != null && ServiceResult.IsNotGood(e.Status))
     {
      System.Diagnostics.Debug.WriteLine($"KeepAlive error: {e.Status}");
ConnectionStatusChanged?.Invoke(this, EventArgs.Empty);
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

     public void Dispose()
{
 DisconnectAsync().Wait();
 }
    }

    public class DataChangedEventArgs : EventArgs
    {
     public string NodeId { get; set; }
        public object Value { get; set; }
 public DateTime Timestamp { get; set; }
        public string StatusCode { get; set; }
}
}
