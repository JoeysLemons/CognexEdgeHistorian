using System.ServiceProcess;
using System.Xml;
using CognexEdgeMonitoringService.Core;
using CognexEdgeMonitoringService.Models;
using System.IO;
using System.Threading;
using Opc.Ua.Client;
using System.Collections.Generic;
using Opc.Ua;
using System.Net;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CognexEdgeMonitoringService
{
    public partial class CognexMonitoringService : ServiceBase
    {
        string ConfigFilePath = Path.Combine(Directory.GetCurrentDirectory(), "ServiceConfig.xml");
        private bool isRunning = false;
        private Thread edgeMonitoringThread = null;
        private string countNodeId = "";
        public CognexMonitoringService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            isRunning= true;
            edgeMonitoringThread = new Thread(EdgeMonitoringWorker);
            edgeMonitoringThread.Start();
        }
        public void OnDebug()
        {
            OnStart(null);
        }
        protected override void OnStop()
        {
        }
        private XmlDocument LoadConfigFile()
        {
            XmlDocument config = new XmlDocument();
            config.Load(ConfigFilePath);
            return config;
        }

        public void TagNotFoundErrorHandler(string name, string nodeId, string errMsg)
        {
            // write to database name of the tag that was not able to be subscribed to
            Trace.WriteLine($"Error while attempting to subscribe to tag.\nError Message: {errMsg} \nTag Name: {name}\nNode ID: {nodeId}");
        }

        private async void EdgeMonitoringWorker()
        {
            XmlDocument serviceConfig = LoadConfigFile();
            string location = string.Empty;
            try
            {
                string connectionString = GetXmlNode(serviceConfig, "//ConnectionString").InnerText;
                location = GetXmlNode(serviceConfig, "//Location").InnerText;

                DatabaseUtils.ConnectionString = connectionString;
            }
            catch (NullReferenceException ex)
            {
                Trace.WriteLine($"Failed to find connection string or location, node path may be incorrect. Error Message: {ex.Message}");
            }

            var locationId = GetLocationId(location);
            List<int> cameraIds = GetCameraId(locationId);
            List<CognexSession> sessions = new List<CognexSession>();

            foreach(int id in cameraIds)
            {
                string endpoint = DatabaseUtils.GetCameraEndpointFromId(id);

                var opcConfig = OPCUAUtils.CreateApplicationConfiguration();
                await OPCUAUtils.InitializeApplication();
                Session session = await OPCUAUtils.ConnectToServer(opcConfig, $"opc.tcp://{endpoint}:4840");
                CognexSession cognexSession = new CognexSession(session, endpoint, session.SessionName, id);
                cognexSession.Tags = DatabaseUtils.GetTags(cognexSession.ID);
                cognexSession.Subscription = OPCUAUtils.CreateEventSubscription(cognexSession.Session);
                OPCUAUtils.AddEventDrivenMonitoredItem(cognexSession.Subscription, countNodeId, cognexSession.Tags);
                sessions.Add(cognexSession);
            }
        }

        public int GetLocationId(string location)
        {
            int locationId = DatabaseUtils.GetLocationId(location);
            if (locationId == -1)
            {
                //!Do some error handling here
            }
            return locationId;
        }

        public List<int> GetCameraId(int locationId)
        {
            List<int> cameraIds = DatabaseUtils.GetCameraIdFromLocationId(locationId);
            if (cameraIds == null)
            {
                //!Do some error handling here
            }
            return cameraIds;
        }

        private XmlNode GetXmlNode(XmlDocument doc, string nodePath)
        {
            return doc.SelectSingleNode(nodePath);
        }
        private XmlNodeList GetXmlNodeList(XmlDocument doc, string nodePath)
        {
            return doc.SelectNodes(nodePath);
        }
    }
}
