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
        private async Task<bool> CheckTagConfiguration(CognexSession session)
        {
            List<Tag> tags = await OPCUAUtils.BrowseChildren(session.Session, session.References);
            foreach (Tag sessionTag in session.Tags)
            {
                bool foundMatch = false;
                foreach (Tag tag in tags)
                {
                    if (tag.Name == sessionTag.Name)
                    {
                        foundMatch = true;
                        break;
                    }
                }
                if (!foundMatch)
                {
                    return false;
                }
            }
            return true;

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
                    Session session;
                    var config = OPCUAUtils.CreateApplicationConfiguration();
                    await OPCUAUtils.InitializeApplication();
                    session = await OPCUAUtils.ConnectToServer(config, $"opc.tcp://{cam.Attributes["endpoint"].Value}");
                    CognexSession cameraSession = new CognexSession(session, cam.Attributes["endpoint"].Value, cam.Attributes["name"].Value);
                    XmlNodeList MonitoredTags = cam.SelectNodes("/MonitoredTags/*");
                    cameraSession.Tags = MonitoredTagsConverter(MonitoredTags);
                    OPCUAUtils.CreateSubscription(session);
                    foreach(Tag tag in cameraSession.Tags)
                        OPCUAUtils.AddMonitoredItem(cameraSession.Subscription, tag.NodeId, OPCUAUtils.OnTagValueChanged);
                    cognexSessions.Add(cameraSession);
                }
                
            }
        }
    }
}
