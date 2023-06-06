using System.ServiceProcess;
using System.Xml;
using CognexEdgeMonitoringService.Core;
using CognexEdgeMonitoringService.Models;
using System.Threading;
using Opc.Ua.Client;
using System.Collections.Generic;
using System.Configuration;
using Opc.Ua;
using System.Net;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace CognexEdgeMonitoringService
{
    public partial class CognexMonitoringService : ServiceBase
    {
        private bool isRunning = false;
        private Thread edgeMonitoringThread = null;
        private string countNodeId;
        public string connectionString = string.Empty;
        public ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
        public Configuration serviceConfig;
        public CognexMonitoringService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            isRunning= true;
            fileMap.ExeConfigFilename = @"ServiceConfig.config";
            serviceConfig = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            
            //Setup Thread Pool
            int workerThreads = 0;
            int completionPortThreads = 0;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            ThreadPool.SetMinThreads(3, completionPortThreads);
            ThreadPool.SetMaxThreads(8, completionPortThreads);
            
            string filePath = @"C:\Users\jverstraete\Desktop\JunkChest\Cognex\FTP";
            
            
            Task monitoringWorker = Task.Run(EdgeMonitoringWorker);

            monitoringWorker.Wait();
            
            Thread.Sleep((Timeout.Infinite));
            Console.WriteLine("Monitoring halted.");
        }
        public void OnDebug()
        {
            OnStart(null);
        }
        protected override void OnStop()
        {
        }

        public void TagNotFoundErrorHandler(string name, string nodeId, string errMsg)
        {
            // write to database name of the tag that was not able to be subscribed to
            Trace.WriteLine($"Error while attempting to subscribe to tag.\nError Message: {errMsg} \nTag Name: {name}\nNode ID: {nodeId}");
        }

        private async void EdgeMonitoringWorker()
        {
            string location = string.Empty;
            try
            {
                location = "AdamsTesting";
                countNodeId = "ns=2;s=Tasks.InspectionTask.Spreadsheet.AcquisitionCount";


                //location = serviceConfig.AppSettings.Settings["Location"].Value; //Get Location from config
                //Console.WriteLine(location);
                //connectionString = serviceConfig.ConnectionStrings.ConnectionStrings["MainConnectionString"].ConnectionString;   //Get connection string from config
                //countNodeId = serviceConfig.AppSettings.Settings["CountNodeId"].Value;
            }
            catch (NullReferenceException ex)
            {
                Trace.WriteLine($"Failed to find connection string or location, node path may be incorrect. Error Message: {ex.Message}");
            }

            var locationId = GetLocationId(location);
            List<int> cameraIds = GetCameraId(locationId);
            List<CognexSession> sessions = new List<CognexSession>();
            //DatabaseUtils.ConnectionString = connectionString;
            try
            {
                foreach(int id in cameraIds)
                {
                    string endpoint = DatabaseUtils.GetCameraEndpointFromId(id);
                
                    var opcConfig = OPCUAUtils.CreateApplicationConfiguration();
                    await OPCUAUtils.InitializeApplication();
                    Session session = await OPCUAUtils.ConnectToServer(opcConfig, $"opc.tcp://{endpoint}:4840");
                    CognexSession cognexSession = new CognexSession(session, endpoint, session.SessionName, id);
                    cognexSession.Tags = DatabaseUtils.GetMonitoredTags(cognexSession.ID);
                    cognexSession.Subscription = OPCUAUtils.CreateEventSubscription(cognexSession.Session);
                    OPCUAUtils.AddEventDrivenMonitoredItem(cognexSession.Subscription, countNodeId, cognexSession.Tags);
                    sessions.Add(cognexSession);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
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
