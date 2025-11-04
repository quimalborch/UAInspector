using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
        private CertificateManager _certificateManager;

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
                
        // Initialize certificate manager
 _certificateManager = new CertificateManager(
      _configuration,
         @"%LocalApplicationData%/UAInspector/Certificates"
       );

    // Validate/create application certificate
     await _certificateManager.ValidateApplicationCertificateAsync();

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
  AutoAcceptUntrustedCertificates = false,
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
      /// Connect to an OPC UA server with security support
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

   // Select endpoint
EndpointDescription endpointDescription = null;

              try
                {
                    using (var discoveryClient = DiscoveryClient.Create(new Uri(serverInfo.Url)))
          {
  var endpoints = discoveryClient.GetEndpoints(null);

    // Select endpoint based on security preference and availability
          if (loginType == LoginType.Anonymous)
  {
      // For anonymous, prefer endpoints without security
 endpointDescription = endpoints
   .Where(e => e.SecurityPolicyUri == SecurityPolicies.None)
       .OrderBy(e => e.SecurityLevel)
  .FirstOrDefault();

        // Fallback to any endpoint if no None security available
          if (endpointDescription == null)
{
      endpointDescription = endpoints
                    .OrderBy(e => e.SecurityLevel)
               .FirstOrDefault();
       }
         }
           else
         {
      // For authenticated users, prefer secure endpoints with Basic256Sha256
     endpointDescription = endpoints
           .Where(e => e.SecurityPolicyUri.Contains("Basic256Sha256"))
              .OrderByDescending(e => e.SecurityLevel)
          .FirstOrDefault();

      // Fallback to any secure endpoint
       if (endpointDescription == null)
          {
          endpointDescription = endpoints
   .Where(e => e.SecurityPolicyUri != SecurityPolicies.None)
           .OrderByDescending(e => e.SecurityLevel)
          .FirstOrDefault();
           }

                // Final fallback
                  if (endpointDescription == null)
               {
      endpointDescription = endpoints.FirstOrDefault();
       }
        }

              if (endpointDescription != null)
   {
System.Diagnostics.Debug.WriteLine(
        $"Selected endpoint: {endpointDescription.SecurityPolicyUri} - Level: {endpointDescription.SecurityLevel}");
      }
          }
     }
         catch (Exception ex)
             {
    System.Diagnostics.Debug.WriteLine($"Endpoint discovery error: {ex.Message}. Using direct URL.");
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

                // Update server info with connection details
serverInfo.LastConnected = DateTime.Now;
   serverInfo.LastLoginType = loginType;

    return true;
   }
      catch (ServiceResultException sre) when (sre.StatusCode == StatusCodes.BadCertificateUntrusted)
       {
    System.Diagnostics.Debug.WriteLine($"Certificate untrusted: {sre.Message}");
                // Certificate validation dialog should be shown to user
         throw new InvalidOperationException("Server certificate is not trusted. Please accept the certificate first.", sre);
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
        node.DataType = ConvertDataTypeToString(varNode.DataType);
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
        public async Task<WriteResult> WriteValueAsync(string nodeId, object value)
        {
            var result = new WriteResult();

            if (!IsConnected)
            {
                result.ErrorMessage = "Not connected to OPC UA server";
                result.Success = false;
  return result;
 }

       try
      {
           var node = NodeId.Parse(nodeId);

       // Read the node to get its DataType for proper type conversion
    var readNode = _session.ReadNode(node);
     if (readNode is VariableNode varNode)
   {
            // Convert value to the correct OPC UA type
     var typedValue = ConvertValueToCorrectType(value, varNode.DataType);

       var writeValue = new WriteValue
    {
              NodeId = node,
   AttributeId = Attributes.Value,
     Value = new DataValue(new Variant(typedValue))
    };

    var writeValueCollection = new WriteValueCollection { writeValue };

  _session.Write(
     null,
writeValueCollection,
     out StatusCodeCollection results,
  out DiagnosticInfoCollection diagnosticInfos
          );

     if (!StatusCode.IsGood(results[0]))
           {
           result.ErrorMessage = $"Write failed: {results[0].ToString()}";
 if (diagnosticInfos != null && diagnosticInfos.Count > 0 && diagnosticInfos[0] != null)
   {
          result.ErrorMessage += $" - {diagnosticInfos[0].LocalizedText}";
   }
   System.Diagnostics.Debug.WriteLine($"Write error - StatusCode: {results[0]}");
        result.Success = false;
              return result;
          }

          result.Success = true;
          return await Task.FromResult(result);
      }

     // Fallback: try to write without type conversion
    var fallbackWriteValue = new WriteValue
        {
             NodeId = node,
          AttributeId = Attributes.Value,
      Value = new DataValue(new Variant(value))
    };

            var fallbackCollection = new WriteValueCollection { fallbackWriteValue };

   _session.Write(
     null,
         fallbackCollection,
          out StatusCodeCollection fallbackResults,
             out DiagnosticInfoCollection fallbackDiagnostics
        );

           if (!StatusCode.IsGood(fallbackResults[0]))
       {
 result.ErrorMessage = $"Write failed: {fallbackResults[0].ToString()}";
               if (fallbackDiagnostics != null && fallbackDiagnostics.Count > 0 && fallbackDiagnostics[0] != null)
   {
      result.ErrorMessage += $" - {fallbackDiagnostics[0].LocalizedText}";
        }
         result.Success = false;
              return result;
                }

       result.Success = true;
     return await Task.FromResult(result);
          }
      catch (Exception ex)
            {
       result.ErrorMessage = $"Exception: {ex.Message}";
 result.Success = false;
      System.Diagnostics.Debug.WriteLine($"Write error: {ex.Message}");
     return result;
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

   /// <summary>
        /// Convert a value to the correct OPC UA data type
        /// </summary>
     private object ConvertValueToCorrectType(object value, NodeId dataTypeNodeId)
        {
         if (value == null)
       return null;

          // If already correct type, return as-is
            if (dataTypeNodeId == null || NodeId.IsNull(dataTypeNodeId))
           return value;

      if (dataTypeNodeId.NamespaceIndex == 0)
            {
    try
   {
   switch ((uint)dataTypeNodeId.Identifier)
              {
     case DataTypes.Boolean:
                  return Convert.ToBoolean(value);
      case DataTypes.SByte:
    return Convert.ToSByte(value);
             case DataTypes.Byte:
        return Convert.ToByte(value);
      case DataTypes.Int16:
            return Convert.ToInt16(value);
             case DataTypes.UInt16:
                  return Convert.ToUInt16(value);
        case DataTypes.Int32:
          return Convert.ToInt32(value);
           case DataTypes.UInt32:
return Convert.ToUInt32(value);
     case DataTypes.Int64:
      return Convert.ToInt64(value);
          case DataTypes.UInt64:
             return Convert.ToUInt64(value);
            case DataTypes.Float:
       return Convert.ToSingle(value);
      case DataTypes.Double:
         return Convert.ToDouble(value);
             case DataTypes.String:
      return Convert.ToString(value);
         case DataTypes.DateTime:
return Convert.ToDateTime(value);
             case DataTypes.Guid:
           return value is Guid g ? g : Guid.Parse(value.ToString());
        default:
     return value;
  }
            }
                catch (Exception ex)
        {
 System.Diagnostics.Debug.WriteLine($"Error converting value to type {dataTypeNodeId}: {ex.Message}");
           return value;
 }
     }

     return value;
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

        /// <summary>
        /// Convert OPC UA DataType NodeId to readable type name
        /// </summary>
        private string ConvertDataTypeToString(NodeId dataTypeNodeId)
     {
    if (dataTypeNodeId == null || NodeId.IsNull(dataTypeNodeId))
                return "Unknown";

            // Handle common built-in types (namespace 0)
    if (dataTypeNodeId.NamespaceIndex == 0)
            {
            switch ((uint)dataTypeNodeId.Identifier)
    {
         case DataTypes.Boolean: return "Boolean";
         case DataTypes.SByte: return "SByte";
        case DataTypes.Byte: return "Byte";
             case DataTypes.Int16: return "Int16";
          case DataTypes.UInt16: return "UInt16";
            case DataTypes.Int32: return "Int32";
         case DataTypes.UInt32: return "UInt32";
          case DataTypes.Int64: return "Int64";
          case DataTypes.UInt64: return "UInt64";
    case DataTypes.Float: return "Float";
            case DataTypes.Double: return "Double";
   case DataTypes.String: return "String";
        case DataTypes.DateTime: return "DateTime";
   case DataTypes.Guid: return "Guid";
     case DataTypes.ByteString: return "ByteString";
          case DataTypes.XmlElement: return "XmlElement";
       case DataTypes.NodeId: return "NodeId";
    case DataTypes.ExpandedNodeId: return "ExpandedNodeId";
 case DataTypes.QualifiedName: return "QualifiedName";
      case DataTypes.LocalizedText: return "LocalizedText";
   case DataTypes.StatusCode: return "StatusCode";
 case DataTypes.BaseDataType: return "BaseDataType";
      default: return $"Type_{dataTypeNodeId.Identifier}";
            }
  }

            return dataTypeNodeId.ToString();
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

    /// <summary>
 /// Result of a write operation
    /// </summary>
    public class WriteResult
    {
        public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    }
}
