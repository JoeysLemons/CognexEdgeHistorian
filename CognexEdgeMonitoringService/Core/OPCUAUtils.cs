using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
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
using System.IO;
using System.Text.RegularExpressions;
using Org.BouncyCastle.Crypto.Modes;

namespace CognexEdgeMonitoringService.Core
{
    public class OPCUAUtils
    {
        private static string ftpDirectory = @"C:\Users\jverstraete\Desktop\JunkChest\Cognex\FTP";
        private static bool pushingData = false;
        private static readonly object lockObject = new object();
        private static Timer timer;
        private static Stopwatch stopwatch = new Stopwatch();

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
            TimeZoneInfo estZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime estTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, estZone);
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
                    estTime - TimeSpan.FromDays(1),
                    12, // Validity period in months.
                    0, // pathLengthConstraint (ushort)
                    false // isCA (bool)
                );
#pragma warning restore CS0618 // Type or member is obsolete
            }


            config.SecurityConfiguration.ApplicationCertificate.Certificate = cert;

            return config;
        }

        private static ApplicationConfiguration CreateApplicationConfiguration()
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

        public static void StartMonitoring(CognexSession cognexSession)
        {
            try
            {
                MonitorData(cognexSession);
                PushDataFromQueue(cognexSession.TagWriteQueue);
            }
            catch (Exception e)
            {
                CognexMonitoringService.eventLog.WriteEntry($"Error while starting timer and push data. error: {e.Message} stack trace: {e.StackTrace}");
                throw;
            }
            
        }

        private static void ReadTags(object state)
        {
            CognexSession cognexSession = state as CognexSession;
            ReadValueIdCollection nodesToRead = new ReadValueIdCollection();
            if (cognexSession == null)
                return;
            double elapsedMilliseconds;
            try
            {
                stopwatch.Restart();
                
                //stopwatch.Stop();
                //double lockTime = stopwatch.ElapsedMilliseconds;
                //stopwatch.Restart();
                CognexMonitoringService.eventLog.WriteEntry($"Tags to read: {cognexSession.Tags.Count}",
                    EventLogEntryType.Error);
                foreach (Tag tag in cognexSession.Tags)
                {
                    nodesToRead.Add(new ReadValueId
                        { NodeId = new NodeId(tag.NodeId), AttributeId = Attributes.Value });
                }
                
                DataValueCollection results;
                DiagnosticInfoCollection diagnosticInfos;
                cognexSession.Session.Read(
                    null,
                    0,
                    TimestampsToReturn.Both,
                    nodesToRead,
                    out results,
                    out diagnosticInfos
                );
                string imageFileName = null;
                DataValue imageFileNameResult = new DataValue();
                foreach (DataValue result in results)
                { 
                    imageFileName = FindAssociatedImagePattern(result.ToString());
                    imageFileNameResult = result;
                    if (imageFileName != null)
                        break;    
                }
                CognexMonitoringService.eventLog.WriteEntry($"Image File Name: {imageFileName}");
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                results.Remove(imageFileNameResult);
                // Process the results
                for (int i = 0; i < results.Count; i++)
                {
                    DataValue value = results[i];
                    string nodeId = nodesToRead[i].NodeId.ToString();

                    lock (lockObject)
                    {
                        // Find the corresponding tag in the session
                        Tag originalTag = cognexSession.Tags.Find(t => t.NodeId == nodeId);
                        if (originalTag != null)
                        {
                            // Create a new Tag object with updated values
                            Tag newTag = new Tag(originalTag.Name, originalTag.NodeId);

                            // Set properties individually
                            newTag.ID = originalTag.ID;
                            newTag.Value = value.Value; // Updated value from the results
                            newTag.NodeId = nodeId;
                            newTag.Name = originalTag.Name;
                            newTag.Timestamp = timestamp; // Updated timestamp
                            newTag.AssociatedImageName = imageFileName ?? "ImageNotFound"; // Updated associated image name    
                            cognexSession.TagWriteQueue.Enqueue(newTag);
                            CognexMonitoringService.eventLog.WriteEntry($"Queue Count: {cognexSession.TagWriteQueue.Count}",
                                EventLogEntryType.Warning);
                        }
                        else
                        {
                            CognexMonitoringService.eventLog.WriteEntry($"Failed to find Tag", EventLogEntryType.Error);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CognexMonitoringService.eventLog.WriteEntry($"Error in the timer method. Error code: {e.Message} StackTrace: {e.StackTrace}", EventLogEntryType.Error);
                throw;
            }
        }
        
        public static string FindAssociatedImagePattern(string input)
        {
            string pattern = "\\d+_\\d{2}-\\d{2}-\\d{4} \\d{2}_\\d{2}_\\d{2}";
            if (Regex.IsMatch(input, pattern))
            {
                return input; // Returns the first matching item
            }
            return null; // Or some other indication that no match was found
        }
        
        public static void MonitorData(CognexSession cognexSession)
        {
            try
            {
                MonitoredItem monitoredItem = new MonitoredItem(cognexSession.Subscription.DefaultItem)
                {
                    DisplayName = "Acquisition Complete",
                    StartNodeId = new NodeId(CognexMonitoringService.countNodeId),
                    AttributeId = Attributes.Value,
                    MonitoringMode = MonitoringMode.Reporting,
                    SamplingInterval = -1, // Set the desired sampling interval (in milliseconds)
                    QueueSize = 1,
                    DiscardOldest = true
                };

                // Set the callback for value changes
                monitoredItem.Notification += async (item, args) => ReadTags(cognexSession);
            

                // Add the monitored item to the subscription
                cognexSession.Subscription.AddItem(monitoredItem);

                // Apply the changes on the server
                cognexSession.Subscription.ApplyChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // private static void PushData(CognexSession cognexSession)
        // {
        //     try
        //     {
        //         MonitoredItem monitoredItem = new MonitoredItem(cognexSession.Subscription.DefaultItem)
        //         {
        //             DisplayName = "Acquisition Complete",
        //             StartNodeId = new NodeId(CognexMonitoringService.countNodeId),
        //             AttributeId = Attributes.Value,
        //             MonitoringMode = MonitoringMode.Reporting,
        //             SamplingInterval = -1, // Set the desired sampling interval (in milliseconds)
        //             QueueSize = 1,
        //             DiscardOldest = true
        //         };
        //
        //         // Set the callback for value changes
        //         monitoredItem.Notification += async (item, args) => PushDataCallback(cognexSession.TagWriteQueue);
        //     
        //
        //         // Add the monitored item to the subscription
        //         cognexSession.Subscription.AddItem(monitoredItem);
        //
        //         // Apply the changes on the server
        //         cognexSession.Subscription.ApplyChanges();
        //     }
        //     catch (Exception e)
        //     {
        //         Console.WriteLine(e);
        //         throw;
        //     }
        // }

        // public static void PushDataCallback(List<Tag> tags)
        // {
        //     try
        //     {
        //         stopwatch.Restart();
        //         Tag imageFileName = tags.Find(t => t.Name == "ImageFileName");
        //         foreach (Tag tag in tags)
        //         {
        //             if (tag.Name == "ImageFileName")
        //                 continue;
        //             tag.AssociatedImageName = imageFileName.Value?.ToString() ?? "NoImageRecieved";
        //             string data = $"{tag.ID},{tag.Value},{tag.AssociatedImageName},{tag.Timestamp}";
        //             //CognexMonitoringService.eventLog.WriteEntry(data);
        //             CognexMonitoringService.fileWriterQueue.Enqueue(data);
        //             DatabaseUtils.StoreTagValue(tag.ID, tag.Value?.ToString() ?? "", tag.AssociatedImageName, tag.Timestamp);
        //             tags.Remove(tag);
        //         } 
        //         stopwatch.Stop();
        //         double pushTime = stopwatch.ElapsedMilliseconds;
        //         if (pushTime > 25)
        //             CognexMonitoringService.eventLog.WriteEntry($"Tag Value: {tags[0].Value}, Database Push execution time: {pushTime}", EventLogEntryType.Error);
        //         else
        //             CognexMonitoringService.eventLog.WriteEntry($"Database Push execution time: {pushTime}");
        //     }
        //     catch (Exception e)
        //     {
        //         CognexMonitoringService.eventLog.WriteEntry(
        //             $"Error while writing data. Error: {e.Message}, Stack Trace: {e.StackTrace}", EventLogEntryType.Error);
        //     }
        //     
        // }
        
        private static void PushDataFromQueue(ConcurrentQueue<Tag> tagsQueue)
        {
            try
            {
                while (true)
                {
                    if (tagsQueue.IsEmpty)
                    {
                        Thread.Sleep(10000);
                        continue;
                    }

                    // Dequeue and process items from the queue
                    while (tagsQueue.TryDequeue(out Tag tag))
                    {
                        
                        if (tag == null)
                        {
                            CognexMonitoringService.eventLog.WriteEntry($"Tag is null somehow", EventLogEntryType.Error);
                        }
                        DatabaseUtils.StoreTagValue(tag.ID, tag.Value?.ToString() ?? "", tag.AssociatedImageName, tag.Timestamp);
                    }
                    CognexMonitoringService.eventLog.WriteEntry($"Queue emptied", EventLogEntryType.SuccessAudit);
                }
                
            }
            catch (Exception e)
            {
                // Handle any exceptions that might occur during the dequeue or processing
                // For example, you might log the exception to a file or the Event Log
                CognexMonitoringService.eventLog.WriteEntry($"Error while writing to DB, Error Message: {e.Message}, Stack Trace: {e.StackTrace}, Source: {e.Source}, Data: {e.Data}, Inner Exception{e.InnerException}", EventLogEntryType.Error);
                // Optionally, rethrow the exception or handle it as needed
            }
        }


        public static void AddEventDrivenMonitoredItem(Subscription subscription, Tag tag)
        {
            try
            {
                MonitoredItem monitoredItem = new MonitoredItem(subscription.DefaultItem)
                {
                    DisplayName = tag.Name,
                    StartNodeId = new NodeId(tag.NodeId),
                    AttributeId = Attributes.Value,
                    MonitoringMode = MonitoringMode.Reporting,
                    SamplingInterval = -1, // Set the desired sampling interval (in milliseconds)
                    QueueSize = 1,
                    DiscardOldest = true
                };

                // Set the callback for value changes
                monitoredItem.Notification += async (item, args) => TagMonitorCallback(tag, subscription);
            

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

        public static async void TagMonitorCallback(Tag tag, Subscription subscription)
        {
            if (pushingData)
                return;
            
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

            tag.Value = response.Results[0].Value.ToString();
            tag.Timestamp = response.Results[0].ServerTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            ReadResponse imageResponse = await subscription.Session.ReadAsync(
                null,
                0,
                TimestampsToReturn.Both,
                new ReadValueIdCollection
                {
                    new ReadValueId()
                    {
                        NodeId = CognexMonitoringService.imageFileNameNodeID,
                        AttributeId = Attributes.Value,
                    }
                },
                CancellationToken.None);
            tag.AssociatedImageName = imageResponse.Results[0].Value.ToString();
            CognexMonitoringService.eventLog.WriteEntry($"Tag ID: {tag.ID}, Tag Value: {tag.Value}, Associated Image: {tag.AssociatedImageName}, Timestamp: {tag.Timestamp}");
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
                    
        
                    // if (tag.NodeId == CognexMonitoringService.imageFileNameNodeID)
                    // {
                    //     tag.AssociatedImageName = response.Results[0].Value.ToString();
                    //     GetAssociatedImage(tag);
                    // }
                    try
                    {
                        Trace.WriteLine(response.Results[0].Value.ToString());
                        Trace.WriteLine($"OPC Trigger Server Time: {response.Results[0].ServerTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
                        //read image name
                        ReadResponse imageResponse = await subscription.Session.ReadAsync(
                            null,
                            0,
                            TimestampsToReturn.Both,
                            new ReadValueIdCollection
                            {
                                new ReadValueId()
                                {
                                    NodeId = CognexMonitoringService.imageFileNameNodeID,
                                    AttributeId = Attributes.Value,
                                }
                            },
                            CancellationToken.None);
                        CognexMonitoringService.eventLog.WriteEntry($"Tag ID: {tag.ID}");
                        CognexMonitoringService.eventLog.WriteEntry($"Tag ID: {tag.ID}");
                        CognexMonitoringService.eventLog.WriteEntry($"Tag ID: {tag.ID}");
                        DatabaseUtils.StoreTagValue(tag.ID, response.Results[0].Value.ToString(), imageResponse.Results[0].Value.ToString(), response.Results[0].SourceTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        //Dump packet to .csv file for debug purposes. Should be removed in final release
                        string data = $"{tag.ID},{response.Results[0].Value},{imageResponse.Results[0].Value},{response.Results[0].SourceTimestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")}";
                        CognexMonitoringService.fileWriterQueue.Enqueue(data);
                    }
                    catch (SqlException ex)
                    {
                        Trace.WriteLine($"Error while writing values to the database. Error Message: {ex.Message}");
                        CognexMonitoringService.eventLog.WriteEntry($"There was an error while attempting to write Tag: {tag.Name} to the Database. Error Message: {ex.Message}, Stack Trace: {ex.StackTrace}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Something went wrong while monitoring tag values. Error Message: {ex.Message}");
                    CognexMonitoringService.eventLog.WriteEntry($"There was an error while trying to read a tag value. Tag Name: {tag.Name} Error Message: {ex.Message}, Stack Trace: {ex.StackTrace}");
                    return;
                }
            }
        }


        // public static async void EventCallback(List<Tag> tags)
        // {
        //     try
        //     {
        //         pushingData = true;
        //         foreach (Tag tag in tags)
        //         {
        //             DatabaseUtils.StoreTagValue(tag.ID, tag.Value.ToString(), tag.AssociatedImageName, tag.Timestamp);
        //             string data = $"{tag.ID},{tag.Value},{tag.AssociatedImageName},{tag.Timestamp}";
        //             CognexMonitoringService.fileWriterQueue.Enqueue(data);
        //         }
        //
        //         pushingData = false;
        //     }
        //     catch (SqlException ex)
        //     {
        //         pushingData = false;
        //     }
        //     catch (Exception ex)
        //     {
        //         pushingData = false;
        //     }
        // }

        public static Task<string> GetAssociatedImage(Tag tag)
        {
            return Task.Run(() =>
            {
                //get files from directory
                var files = Directory.GetFiles(ftpDirectory);
                string associatedImage = string.Empty;

                //Check for matching filename
                foreach (var file in files)
                {
                    if (Path.GetFileName(file) == tag.AssociatedImageName)
                    {
                        return file;
                    }
                }

                return null;
            });
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
                DatabaseUtilsOLD.StoreTagValue(ConvertNodeIdToInteger(monitoredItem.DisplayName), value.Value.ToString(), value.SourceTimestamp.ToString());
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
