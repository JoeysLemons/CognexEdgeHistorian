using System.ServiceProcess;
using System.Xml;
using CognexEdgeMonitoringService.Core;
using CognexEdgeMonitoringService.Models;
using System.Threading;
using System.Collections.Generic;
using System.Configuration;
using Opc.Ua;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Diagnostics;
using CognexEdgeMonitoringService;
using Opc.Ua.Server;
using Session = Opc.Ua.Client.Session;

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
        private List<CognexSession> ConnectedCameras = new List<CognexSession>();
        EventLog eventLog = new EventLog("Application");
        public CognexMonitoringService()
        {
            InitializeComponent();
            eventLog.Source = "CognexMonitoringService";

        }

        protected override void OnStart(string[] args)
        {
            isRunning= true;
            
            //Setup Thread Pool
            int workerThreads = 0;
            int completionPortThreads = 0;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            ThreadPool.SetMinThreads(3, completionPortThreads);
            ThreadPool.SetMaxThreads(8, completionPortThreads);
            
            string filePath = @"C:\Users\jverstraete\Desktop\JunkChest\Cognex\FTP";

            try
            {
                List<string> cameraAddresses = GetCameras();
                SpawnCameraMonitors(cameraAddresses);

            }
            catch (Exception e)
            {
                Trace.WriteLine("Monitoring halted.");
                throw;
            }
            finally
            {
                Trace.WriteLine("Monitoring halted.");
                foreach (CognexSession cognexSession in ConnectedCameras)
                {
                    cognexSession.Session.Close();
                    cognexSession.Session.Dispose();
                }
            }
            
        }
        public void OnDebug()
        {
            OnStart(null);
        }
        protected override void OnStop()
        {
            foreach (CognexSession cognexSession in ConnectedCameras)
            {
                cognexSession.Session.Close();
                cognexSession.Session.Dispose();
            }
        }

        protected override void OnShutdown()
        {
            foreach (CognexSession cognexSession in ConnectedCameras)
            {
                cognexSession.Session.Close();
                cognexSession.Session.Dispose();
            }
        }

        public void TagNotFoundErrorHandler(string name, string nodeId, string errMsg)
        {
            // write to database name of the tag that was not able to be subscribed to
            Trace.WriteLine($"Error while attempting to subscribe to tag.\nError Message: {errMsg} \nTag Name: {name}\nNode ID: {nodeId}");
        }

        private List<string> GetCameras()
        {
            var filePath = @"C:\Users\jverstraete\source\repos\CognexEdgeHistorian\EdgePcConfigurationApp\AppSettings.xml";
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            XmlNode rootNode = doc.DocumentElement;
            XmlNode pcSettings = rootNode.SelectSingleNode("PCSettings");
            XmlNode DatabaseSettings = rootNode.SelectSingleNode("Database");
            string pcGUID = pcSettings.SelectSingleNode("ComputerGUID").InnerText;
            string connectionString = DatabaseSettings.SelectSingleNode("ConnectionString").InnerText;
            eventLog.WriteEntry($"Connection String {connectionString}");
            DatabaseUtils.ConnectionString = connectionString;

            int pcID = DatabaseUtils.GetPCIdFromGUID(pcGUID);
            List<string> cameraAddresses = DatabaseUtils.GetCamerasOnPC(pcID);
            return cameraAddresses;
        }

        private void SpawnCameraMonitors(List<string> cameraAddresses)
        {
            foreach (string address in cameraAddresses)
            {
                Task.Run(() => CameraMonitor(address));
            }
        }

        private async Task CameraMonitor(string ipAddress)
        {
            try
            {
                CognexSession cognexSession = null;
                countNodeId = "ns=2;s=Tasks.InspectionTask.Spreadsheet.AcquisitionCount";
                ApplicationConfiguration opcConfig = await OPCUAUtils.InitializeApplication();
                Session session = await OPCUAUtils.ConnectToServer(opcConfig, $"opc.tcp://{ipAddress}:4840");
                ReferenceDescriptionCollection references;
                byte[] continuationPoint;
                session.Browse(
                    null,
                    null,
                    ObjectIds.ObjectsFolder,
                    0u,
                    BrowseDirection.Forward,
                    ReferenceTypeIds.HierarchicalReferences,
                    true,
                    uint.MaxValue,
                    out continuationPoint,
                    out references);
                string cameraName = DatabaseUtils.GetCameraName(ipAddress);
                int id = DatabaseUtils.GetCameraID(ipAddress);
                cognexSession = new CognexSession(session, ipAddress, cameraName, id);
                cognexSession.References = references;
                ConnectedCameras.Add(cognexSession);
                List<Tag> tags = await OPCUAUtils.BrowseChildren(cognexSession.Session, cognexSession.References);
                string jobName = GetJobNameNew(cognexSession.Session, tags);
                //Need to modify DB functions so that it gets the tags based on the job id and not the camera id.
                //Once monitored tags are acquired it should be almost the same as before.
                //Subscribe to tags and use event based monitoring on the acq count to know when to collect data.
                //Also need to monitor the camera online status to only monitor tags when the camera is online.
                //If the camera goes offline when it comes back online the program should recheck the job name to make sure the job hasn't changed
                //If the job has changed then load the new tag configuration and proceed with monitoring.
                //Will also need to figure out how to handle jobs that the monitoring service has never seen before
                //Would like to handle this by requiring users to load the job at least once with the configurator app but that may not be an option.
                int jobId = DatabaseUtils.GetJobIdFromName(jobName);
                cognexSession.Tags = DatabaseUtils.GetMonitoredTags(jobId);
                cognexSession.Subscription = OPCUAUtils.CreateEventSubscription(cognexSession.Session);
                OPCUAUtils.AddEventDrivenMonitoredItem(cognexSession.Subscription, countNodeId, cognexSession.Tags);
            
            }
            catch (Exception e)
            {
                
                Console.WriteLine(e);
                throw;
            }
            
            
        }
        
        private string GetJobName(Session session, List<Tag> tags)
        {
            List<Tag> results = new List<Tag>();
            string searchParam = "System";
            string result = "Job Not Found";
            //search for the JobName tag
            foreach (Tag tag in tags)
            {
                if (tag.Name == searchParam)
                {
                    results = new List<Tag>(tag.Children);
                    break;
                }
                else if(tag.Children.Count > 0)
                {
                    List<Tag> children = new List<Tag>(tag.Children);
                    result = GetJobName(session, children);
                    if (result != "Job Not Found") return result;
                }
            }

            if (results.Count == 0) return result;
                
            foreach (Tag tag in results)
            {
                if (tag.Name == "JobName")
                {
                    result = OPCUAUtils.ReadTagValue(session, tag.NodeId).Value.ToString();
                    Trace.WriteLine(result);
                    return result;
                }
            }

            return result;
        }

        private string GetJobNameNew(Session session, List<Tag> tags)
        {
            string result = "Job Not Found";
            foreach (Tag tag in tags)
            {
                if (tag.Name == "JobName")
                {
                    result = OPCUAUtils.ReadTagValue(session, tag.NodeId).Value.ToString();
                }
            }

            return result;
        }

        // #########OBSOLETE#############
        // private async void EdgeMonitoringWorker()
        // {
        //     string location = string.Empty;
        //     try
        //     {
        //         location = "AdamsTesting";
        //         countNodeId = "ns=2;s=Tasks.InspectionTask.Spreadsheet.AcquisitionCount";
        //
        //
        //         //location = serviceConfig.AppSettings.Settings["Location"].Value; //Get Location from config
        //         //Console.WriteLine(location);
        //         //connectionString = serviceConfig.ConnectionStrings.ConnectionStrings["MainConnectionString"].ConnectionString;   //Get connection string from config
        //         //countNodeId = serviceConfig.AppSettings.Settings["CountNodeId"].Value;
        //     }
        //     catch (NullReferenceException ex)
        //     {
        //         Trace.WriteLine($"Failed to find connection string or location, node path may be incorrect. Error Message: {ex.Message}");
        //     }
        //
        //     var locationId = GetLocationId(location);
        //     List<int> cameraIds = GetCameraId(locationId);
        //     List<CognexSession> sessions = new List<CognexSession>();
        //     //DatabaseUtils.ConnectionString = connectionString;
        //     try
        //     {
        //         foreach(int id in cameraIds)
        //         {
        //             string endpoint = DatabaseUtilsOLD.GetCameraEndpointFromId(id);
        //         
        //             var opcConfig = OPCUAUtils.CreateApplicationConfiguration();
        //             await OPCUAUtils.InitializeApplication();
        //             Session session = await OPCUAUtils.ConnectToServer(opcConfig, $"opc.tcp://{endpoint}:4840");
        //             CognexSession cognexSession = new CognexSession(session, endpoint, session.SessionName, id);
        //             cognexSession.Tags = DatabaseUtilsOLD.GetMonitoredTags(cognexSession.ID);
        //             cognexSession.Subscription = OPCUAUtils.CreateEventSubscription(cognexSession.Session);
        //             OPCUAUtils.AddEventDrivenMonitoredItem(cognexSession.Subscription, countNodeId, cognexSession.Tags);
        //             sessions.Add(cognexSession);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine(ex);
        //         throw;
        //     }
        // }

        
        
        public int GetLocationId(string location)
        {
            int locationId = DatabaseUtilsOLD.GetLocationId(location);
            if (locationId == -1)
            {
                //!Do some error handling here
            }
            return locationId;
        }

        public List<int> GetCameraId(int locationId)
        {
            List<int> cameraIds = DatabaseUtilsOLD.GetCameraIdFromLocationId(locationId);
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
