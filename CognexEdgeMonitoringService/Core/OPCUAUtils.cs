using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Opc.Ua;
using System.Diagnostics;
using CognexEdgeMonitoringService.Models;
using System.Threading;
using System.Data.SqlClient;

namespace CognexEdgeMonitoringService.Core
{
    public class OPCUAUtils
    {
        public static async Task<ApplicationConfiguration> InitializeApplication()
        {
            var config = CreateApplicationConfiguration();
            await config.Validate(ApplicationType.Client);

            // Check if the application certificate exists, and create a new one if it doesn't.
            bool haveAppCertificate = config.SecurityConfiguration.ApplicationCertificate != null && !string.IsNullOrEmpty(config.SecurityConfiguration.ApplicationCertificate.SubjectName);
            if (!haveAppCertificate)
            {
                throw new Exception("Application certificate must be configured!");
            }

            var cert = config.SecurityConfiguration.ApplicationCertificate.Find(true).Result;
            if (cert == null)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                cert = CertificateFactory.CreateCertificate(
                    config.SecurityConfiguration.ApplicationCertificate.StoreType,
                    config.SecurityConfiguration.ApplicationCertificate.StorePath,
                    null,
                    config.ApplicationUri,
                    config.ApplicationName,
                    config.SecurityConfiguration.ApplicationCertificate.SubjectName,
                    null,
                    2048, // Key size.
                    DateTime.UtcNow - TimeSpan.FromDays(1),
                    12, // Validity period in months.
                    0, // pathLengthConstraint (ushort)
                    false // isCA (bool)
                );
#pragma warning restore CS0618 // Type or member is obsolete
            }


            config.SecurityConfiguration.ApplicationCertificate.Certificate = cert;

            return config;
        }

        public static ApplicationConfiguration CreateApplicationConfiguration()
        {
            var applicationName = "CognexEdgeHistorianOpcUaClient";
            var applicationUri = Utils.Format(@"urn:{0}:CognexEdgeHistorianOpcUaClient", System.Net.Dns.GetHostName());
            var applicationType = ApplicationType.Client;

            var config = new ApplicationConfiguration()
            {
                ApplicationName = applicationName,
                ApplicationUri = applicationUri,
                ApplicationType = applicationType,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "X509Store",
                        StorePath = @"CurrentUser\My",
                        SubjectName = applicationName
                    },
                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities"
                    },
                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications"
                    },
                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = "Directory",
                        StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates"
                    },
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                TraceConfiguration = new TraceConfiguration()
            };

            return config;
        }
        public static async Task<Opc.Ua.Client.Session> ConnectToServer(ApplicationConfiguration config, string serverUrl)
        {
            var endpointDescription = CoreClientUtils.SelectEndpoint(serverUrl, false, 15000);
            var endpointConfiguration = EndpointConfiguration.Create(config);
            var endpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

            // Assign a User Identity Token (Anonymous, Username/Password, or Certificate).
            // For this example, we are using Anonymous identity.
            var identity = new UserIdentity();

            // Create and connect the session.
            var session = await Opc.Ua.Client.Session.Create(
                config,
                endpoint,
                false,
                config.ApplicationName,
                60000,
                identity,
                null
            );

            return session;
        }

        public static Subscription CreateSubscription(Session session)
        {
            // Create a subscription
            Subscription subscription = new Subscription(session.DefaultSubscription)
            {
                PublishingInterval = 1000, // Set the desired publishing interval (in milliseconds)
                PublishingEnabled = true
            };

            // Add the subscription to the session and apply the changes
            session.AddSubscription(subscription);
            subscription.Create();

            return subscription;
        }

        public static Subscription CreateEventSubscription(Session session)
        {
            Subscription subscription = new Subscription(session.DefaultSubscription)
            {
                PublishingInterval = 0,
                LifetimeCount = 1000,
                MaxNotificationsPerPublish = 1000,
                Priority = 0
            };

            session.AddSubscription(subscription);
            subscription.Create();
            return subscription;
        }

        public static void AddMonitoredItem(Subscription subscription, string nodeId, MonitoredItemNotificationEventHandler callback)
        {
            MonitoredItem monitoredItem = new MonitoredItem(subscription.DefaultItem)
            {
                DisplayName = nodeId,
                StartNodeId = new NodeId(nodeId),
                AttributeId = Attributes.Value,
                MonitoringMode = MonitoringMode.Reporting,
                SamplingInterval = 1000, // Set the desired sampling interval (in milliseconds)
                QueueSize = 1,
                DiscardOldest = true
            };

            // Set the callback for value changes
            monitoredItem.Notification += callback;

            // Add the monitored item to the subscription
            subscription.AddItem(monitoredItem);

            // Apply the changes on the server
            subscription.ApplyChanges();
        }
        
        public static DataValue ReadTagValue(Session session, NodeId nodeId)
        {
            ReadValueId nodeToRead = new ReadValueId
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value
            };

            ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
            nodesToRead.Add(nodeToRead);

            DataValueCollection results;
            DiagnosticInfoCollection diagnosticInfos;

            session.Read(
                null,
                0,
                TimestampsToReturn.Both,
                nodesToRead,
                out results,
                out diagnosticInfos);

            if (StatusCode.IsGood(results[0].StatusCode))
            {
                return results[0];
            }
            else
            {
                throw new Exception($"Failed to read tag value. Status: {results[0].StatusCode}");
            }
        }

        public static void AddEventDrivenMonitoredItem(Subscription subscription, string nodeId, List<Tag> tags)
        {
            try
            {
                MonitoredItem monitoredItem = new MonitoredItem(subscription.DefaultItem)
                {
                    DisplayName = nodeId,
                    StartNodeId = new NodeId(nodeId),
                    AttributeId = Attributes.Value,
                    MonitoringMode = MonitoringMode.Reporting,
                    SamplingInterval = -1, // Set the desired sampling interval (in milliseconds)
                    QueueSize = 1,
                    DiscardOldest = true
                };

                // Set the callback for value changes
                monitoredItem.Notification += async (item, args) => EventCallback(tags, subscription);
            

                // Add the monitored item to the subscription
                subscription.AddItem(monitoredItem);

                // Apply the changes on the server
                subscription.ApplyChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static async void EventCallback(List<Tag> tags, Subscription subscription)
        {
            // Read the values of the other nodes
            ReadValueIdCollection nodesToReadCollection = new ReadValueIdCollection();
            foreach (Tag tag in tags)
            {
                try
                {
                    ReadResponse response = await subscription.Session.ReadAsync(
                        null,
                        0,
                        TimestampsToReturn.Both,
                        new ReadValueIdCollection
                        {
                            new ReadValueId()
                            {
                                NodeId = tag.NodeId,
                                AttributeId = Attributes.Value,
                            }
                        },
                        CancellationToken.None);

                    try
                    {
                        Trace.WriteLine(response.Results[0].Value.ToString());
                        Trace.WriteLine($"OPC Trigger Server Time: {response.Results[0].ServerTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
                        DatabaseUtils.StoreTagValue(tag.ID, response.Results[0].Value.ToString(), response.Results[0].SourceTimestamp.ToString());
                    }
                    catch (SqlException ex)
                    {
                        Trace.WriteLine($"Error while writing values to the database. Error Message: {ex.Message}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Something went wrong while monitoring tag values. Error Message: {ex.Message}");
                    return;
                }
            }
        }

        public static void RemoveMonitoredItem(Subscription subscription, string nodeId)
        {
            MonitoredItem monitoredItem = subscription.MonitoredItems.FirstOrDefault(item => item.StartNodeId.ToString() == nodeId);

            if (monitoredItem != null)
            {
                // Remove the callback
                monitoredItem.Notification -= OnTagValueChanged;

                // Remove the monitored item from the subscription
                subscription.RemoveItem(monitoredItem);

                // Apply the changes on the server
                subscription.ApplyChanges();
            }
            else
            {
                Console.WriteLine($"Node with ID '{nodeId}' not found in the subscription.");
            }
        }

        public static void OnTagValueChanged(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
        {
            MonitoredItemNotification notification = e.NotificationValue as MonitoredItemNotification;
            if (notification == null) return;

            DataValue value = notification.Value;
            Console.WriteLine($"Tag: {monitoredItem.DisplayName}, Value: {value.Value}, Timestamp: {value.SourceTimestamp.ToString()}");

            try
            {
                DatabaseUtils.StoreTagValue(ConvertNodeIdToInteger(monitoredItem.DisplayName), value.Value.ToString(), value.SourceTimestamp.ToString());
            }
            catch (SqlException ex)
            {
                Trace.WriteLine($"Error while writing values to the database. Error Message: {ex.Message}");
                return;
            }
        }

        private static int ConvertNodeIdToInteger(string nodeId)
        {
            int nodeIdInteger = Int32.Parse(nodeId.Substring(2));
            Console.WriteLine(nodeIdInteger);
            return nodeIdInteger;
        }

        public static async Task<List<Tag>> BrowseChildren(Session session, ReferenceDescriptionCollection references)
        {

            try
            {
                List<Tag> nodes = new List<Tag>();
                foreach (var reference in references)
                {
                    string displayName = reference.DisplayName.ToString();
                    string nodeId = reference.NodeId.ToString();
                    nodes.Add(new Tag(displayName, nodeId));
                    Console.WriteLine($"DisplayName: {displayName}, NodeId: {reference.NodeId}");

                    ReferenceDescriptionCollection childReferences;
                    Byte[] continuationPoint;

                    session.Browse(
                        null,
                        null,
                        ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris),
                        0u,
                        BrowseDirection.Forward,
                        ReferenceTypeIds.HierarchicalReferences,
                        true,
                        uint.MaxValue,
                        out continuationPoint,
                        out childReferences);

                    if (childReferences.Count <= 0) continue;
                    List<Tag> childNodes = await BrowseChildren(session, childReferences);
                    nodes.AddRange(childNodes);
                }
                return nodes;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error while attempting to browse tags from OPC UA server. \nError Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
                return null;
            }

        }
    }
}
