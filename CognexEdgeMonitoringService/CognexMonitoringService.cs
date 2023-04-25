using System.ServiceProcess;
using System.Xml;
using CognexEdgeMonitoringService.Core;
using CognexEdgeMonitoringService.Models;
using System.IO;
using System.Threading;
using Opc.Ua.Client;
using System.Collections.Generic;
using Opc.Ua.Server;
using Opc.Ua;
using System.Net;
using System;
using Session = Opc.Ua.Client.Session;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CognexEdgeMonitoringService
{
    public partial class CognexMonitoringService : ServiceBase
    {
        XmlDocument ServiceConfig = new XmlDocument();
        string ConfigFilePath = Path.Combine(Directory.GetCurrentDirectory(), "ServiceConfig.xml");
        private bool isRunning = false;
        private Thread edgeMonitoringThread = null;
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

        protected override void OnStop()
        {
        }
        private void LoadConfigFile()
        {
            ServiceConfig.Load(ConfigFilePath);
        }
        private XmlNodeList GetCameraNodes()
        {
            return ServiceConfig.SelectNodes("/configuration/CognexCameras/*");
        }
        private List<Tag> MonitoredTagsConverter(XmlNodeList nodeList)
        {
            List<Tag> tags = new List<Tag>();
            foreach (XmlNode node in nodeList)
            {
                tags.Add(new Tag(node.Attributes["name"].Value, node.Attributes["nodeID"].Value));
            }
            return tags;
        }
        public void TagNotFoundErrorHandler(string name, string nodeId, string errMsg)
        {
            // write to database name of the tag that was not able to be subscribed to
            Trace.WriteLine($"Error while attempting to subscribe to tag.\nError Message: {errMsg} \nTag Name: {name}\nNode ID: {nodeId}");
        }

        private async void EdgeMonitoringWorker()
        {
            while(isRunning)
            {
                LoadConfigFile();
                XmlNodeList cameras = GetCameraNodes();

                List<CognexSession> cognexSessions = new List<CognexSession>();
                foreach (XmlNode cam in cameras)
                {
                    var config = OPCUAUtils.CreateApplicationConfiguration();   //Create OPC UA App Config
                    await OPCUAUtils.InitializeApplication();   //Init OPC App 
                    Session session = await OPCUAUtils.ConnectToServer(config, $"opc.tcp://{cam.Attributes["endpoint"].Value}"); //Connect to OPC UA server and create session instance
                    CognexSession cameraSession = new CognexSession(session, cam.Attributes["endpoint"].Value, cam.Attributes["name"].Value);   //Create an instance of a Cognex camera session and bind the session instance to it
                    XmlNodeList MonitoredTags = cam.SelectNodes("/MonitoredTags/*");    //read from the config file to see which tags this camera needs monitored
                    cameraSession.Tags = MonitoredTagsConverter(MonitoredTags);         //Convert the xml monitored tag items to a list of Tag objects on the Cognex Camera Session
                    OPCUAUtils.CreateSubscription(cameraSession.Session);               //Create a new OPC UA Subscription
                    foreach(Tag tag in cameraSession.Tags)                              //Loop over all the tags in the Cognex camera tags list and add them to the subscription
                    {
                        try
                        {
                            OPCUAUtils.AddMonitoredItem(cameraSession.Subscription, tag.NodeId, OPCUAUtils.OnTagValueChanged);  //add monitored item to subscription
                        }
                        catch (Exception ex)
                        {
                            TagNotFoundErrorHandler(tag.Name, tag.NodeId, ex.Message);
                        }
                    }
                        
                    cognexSessions.Add(cameraSession);  //add current Cognex Session to list of all open cognex sessions
                }
                
            }
        }
    }
}
