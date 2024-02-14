using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using Opc.Ua;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using CertificateRequest = Org.BouncyCastle.Tls.CertificateRequest;
using HashAlgorithm = Org.BouncyCastle.Tls.HashAlgorithm;

namespace EdgePcConfigurationApp.Helpers
{
    public class OPCUAUtils
    {
        static X509Certificate2 FindCertificate(string certName)
        {
            // Access the Personal store for the current user
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

            try
            {
                store.Open(OpenFlags.ReadOnly);

                // Find the certificate by name
                X509Certificate2Collection certificates = store.Certificates.Find(X509FindType.FindBySubjectName, certName, false);

                return certificates.Count > 0 ? certificates[0] : null;
            }
            finally
            {
                store.Close();
            }
        }
//         public static async Task<ApplicationConfiguration> InitializeApplication()
//         {
//             var config = CreateApplicationConfiguration();
//             await config.Validate(ApplicationType.Client);
//             X509Certificate2 x509Cert = GenerateCertificate("OPCCert");
//
//             // Check if the application certificate exists, and create a new one if it doesn't.
//             bool haveAppCertificate = config.SecurityConfiguration.ApplicationCertificate != null && !string.IsNullOrEmpty(config.SecurityConfiguration.ApplicationCertificate.SubjectName);
//             if (!haveAppCertificate)
//             {
//                 throw new Exception("Application certificate must be configured!");
//             }
//
//             var cert = config.SecurityConfiguration.ApplicationCertificate.Find(true).Result;
//             if (cert == null)
//             {
// #pragma warning disable CS0618 // Type or member is obsolete
//                 x509Cert = CertificateFactory.CreateCertificate(
//                     config.SecurityConfiguration.ApplicationCertificate.StoreType,
//                     config.SecurityConfiguration.ApplicationCertificate.StorePath,
//                     null,
//                     config.ApplicationUri,
//                     config.ApplicationName,
//                     config.SecurityConfiguration.ApplicationCertificate.SubjectName,
//                     null,
//                     2048, // Key size.
//                     DateTime.UtcNow - TimeSpan.FromDays(1),
//                     12, // Validity period in months.
//                     0, // pathLengthConstraint (ushort)
//                     false // isCA (bool)
//                 );
// #pragma warning restore CS0618 // Type or member is obsolete
//             }
//             config.SecurityConfiguration.RejectSHA1SignedCertificates = false;
//             config.SecurityConfiguration.ApplicationCertificate.Certificate = cert;
//
//             return config;
//         }
        
        
        public static async Task<ApplicationConfiguration> InitializeApplication()
        {
            var config = CreateApplicationConfiguration();
            await config.Validate(ApplicationType.Client);
            var cert = FindCertificate("CognexEdgeHistorianOpcUaClient");
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
                    AutoAcceptUntrustedCertificates = true,
                    RejectSHA1SignedCertificates = false
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                TraceConfiguration = new TraceConfiguration()
            };

            return config;
        }
        
        static X509Certificate2 GenerateCertificate(string certName)
        {
            var keypairgen = new RsaKeyPairGenerator();
            keypairgen.Init(new KeyGenerationParameters(new SecureRandom(new CryptoApiRandomGenerator()), 1024));

            var keypair = keypairgen.GenerateKeyPair();

            var gen = new X509V3CertificateGenerator();

            var CN = new X509Name("CN=" + certName);
            var SN = BigInteger.ProbablePrime(120, new Random());

            gen.SetSerialNumber(SN);
            gen.SetSubjectDN(CN);
            gen.SetIssuerDN(CN);
            gen.SetNotAfter(DateTime.MaxValue);
            gen.SetNotBefore(DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0)));
            gen.SetSignatureAlgorithm("SHA256WithRSA");
            gen.SetPublicKey(keypair.Public);           

            var newCert = gen.Generate(keypair.Private);

            return new X509Certificate2(DotNetUtilities.ToX509Certificate((Org.BouncyCastle.X509.X509Certificate)newCert));
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
            Console.WriteLine($"Tag: {monitoredItem.DisplayName}, Value: {value.Value}, Timestamp: {value.SourceTimestamp}");

            DatabaseUtils.StoreTagValue(ConvertNodeIdToInteger(monitoredItem.DisplayName), value.Value.ToString(), value.SourceTimestamp.ToString());
        }

        private static int ConvertNodeIdToInteger(string nodeId)
        {
            int nodeIdInteger = Int32.Parse(nodeId.Substring(2));
            Console.WriteLine(nodeIdInteger);
            return nodeIdInteger;
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

    }
}
