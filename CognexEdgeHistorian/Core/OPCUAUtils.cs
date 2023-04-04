using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua.Client;
using Opc.Ua.Server;
using Opc.Ua.Configuration;
using Opc.Ua;

namespace CognexEdgeHistorian.Core
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


    }
}
