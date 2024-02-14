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
using System.Linq;
using CognexEdgeMonitoringService;
using Opc.Ua.Server;
using Session = Opc.Ua.Client.Session;
using System.IO;

namespace CognexEdgeMonitoringService
{
    public partial class CognexMonitoringService : ServiceBase
    {
        private bool isRunning = false;
        private Thread edgeMonitoringThread = null;
        public static string countNodeId;
        public string connectionString = string.Empty;
        public ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
        public Configuration serviceConfig;
        private List<CognexSession> ConnectedCameras = new List<CognexSession>();
        public static EventLog eventLog = new EventLog("Application");
        public static FileWriterQueue fileWriterQueue;
        public static string imageFileNameNodeID;
        public string pcGUID;
        public int pcID;
        private Timer cameraMonitorTimer;
        public CognexMonitoringService()
        {
            InitializeComponent();
            eventLog.Source = "DataPulseMonitor";
            fileWriterQueue = new FileWriterQueue(@"C:\Users\jverstraete\Desktop\DataDumps\Run1.csv");
        }

        protected override void OnStart(string[] args)
        {
            isRunning= true;
            
            //Setup Thread Pool
            ThreadPool.GetMinThreads(out var workerThreads, out var completionPortThreads);
            ThreadPool.SetMinThreads(50, completionPortThreads);
            ThreadPool.SetMaxThreads(1000, completionPortThreads);
            
            string filePath = @"C:\Users\jverstraete\Desktop\JunkChest\Cognex\FTP";

            try
            {
                eventLog.WriteEntry("1");
                SetDBConnectionString();
                eventLog.WriteEntry("2");
                GetPcId();
                eventLog.WriteEntry("3");
                cameraMonitorTimer = new Timer(SpawnCameraMonitors, null, 0, 30000);

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
                fileWriterQueue.CompleteAdding();
            }
        }

        protected override void OnShutdown()
        {
            foreach (CognexSession cognexSession in ConnectedCameras)
            {
                cognexSession.Session.Close();
                cognexSession.Session.Dispose();
                fileWriterQueue.CompleteAdding();
            }
        }

        public void TagNotFoundErrorHandler(string name, string nodeId, string errMsg)
        {
            // write to database name of the tag that was not able to be subscribed to
            Trace.WriteLine($"Error while attempting to subscribe to tag.\nError Message: {errMsg} \nTag Name: {name}\nNode ID: {nodeId}");
        }

        private void GetPcId()
        {
            pcID = DatabaseUtils.GetPCIdFromGUID(pcGUID);
        }
        private List<string> GetCameras()
        {
            
            List<string> cameraAddresses = DatabaseUtils.GetCamerasOnPC(pcID);
            return cameraAddresses;
        }

        private void SetDBConnectionString()
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppSettings.xml");
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            XmlNode rootNode = doc.DocumentElement;
            XmlNode pcSettings = rootNode.SelectSingleNode("PCSettings");
            XmlNode DatabaseSettings = rootNode.SelectSingleNode("Database");
            pcGUID = pcSettings.SelectSingleNode("ComputerGUID").InnerText;
            string connectionString = DatabaseSettings.SelectSingleNode("ConnectionString").InnerText;
            eventLog.WriteEntry($"Connection String {connectionString}");
            DatabaseUtils.ConnectionString = connectionString;
        }

        private void SpawnCameraMonitors(object state)
        {
            List<string> cameraAddresses = DatabaseUtils.GetCamerasOnPC(pcID);
    
            foreach (string address in cameraAddresses)
            {
                if (ConnectedCameras.Any(session => session.Endpoint == address))
                    continue;
                Task.Run(() => CameraMonitor(address));
            }

            eventLog.WriteEntry("Connected to cameras. Monitoring will continue.");
        }

        private async Task CameraMonitor(string ipAddress)
        {
            try
            {
                CognexSession cognexSession = null;
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
                bool insightExplorer = CheckInsightExplorer(tags);
                countNodeId = insightExplorer ? "ns=2;s=InspectionComplete" : "ns=2;s=Tasks.InspectionTask.Spreadsheet.AcquisitionCount";
                imageFileNameNodeID = insightExplorer ? "ns=2;s=ImageFileName" : "ns=2;s=Tasks.InspectionTask.Spreadsheet.ImageFileName";
                string jobName = GetJobNameNew(cognexSession.Session, tags);
                CognexMonitoringService.eventLog.WriteEntry($"Job Name: {jobName}", EventLogEntryType.Error);
                int jobId = DatabaseUtils.GetJobIdFromName(jobName);
                cognexSession.Tags = DatabaseUtils.GetMonitoredTags(jobId);
                Tag imageFileName = new Tag("ImageFileName", imageFileNameNodeID);
                cognexSession.Tags.Add(imageFileName);
                cognexSession.Subscription = OPCUAUtils.CreateEventSubscription(cognexSession.Session);
                OPCUAUtils.StartMonitoring(cognexSession);
            
            }
            catch (Exception e)
            {
                eventLog.WriteEntry($"Failed to connect to camera IP Address: {ipAddress}, Error Message: {e.Message}, Stack Trace: {e.StackTrace}");
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
        private bool CheckInsightExplorer(List<Tag> tags)
        {
            if (tags == null)
                return false;
            foreach (Tag tag in tags)
            {
                if (tag.Name == "SystemTags")
                    return true;
                else if(tag.Children.Count > 0)
                {
                    List<Tag> children = new List<Tag>(tag.Children);
                    if (CheckInsightExplorer(children))
                        return true; // Stop and return true if found in children
                }
            }

            return false;

        }

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
