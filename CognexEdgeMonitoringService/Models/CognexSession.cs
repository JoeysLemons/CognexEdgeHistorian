using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CognexEdgeMonitoringService.Models
{
    public class CognexSession
    {
        public Session Session { get; }
        public int ID { get; }
        public string Endpoint { get; }
        public string SessionName { get; }
        public List<Tag> Tags { get; set; }
        public List<string> NodeIds { get; set; }
        public ReferenceDescriptionCollection References { get; set; }
        public Subscription Subscription { get; set; }

        /// <summary>
        /// Create a new instance of a Cognex OPC UA Session
        /// </summary>
        /// <param name="session">The OPC UA session associated with the camera</param>
        /// <param name="endpoint">The IP Address of the camera</param>
        /// <param name="sessionName">The name of the camera</param>
        /// <param name="id">The ID of the camera should match with the camera ID in the database</param>
        public CognexSession(Session session, string endpoint, string sessionName, int id)
        {
            SessionName = sessionName;
            Endpoint = endpoint;
            Session = session;
            ID = id;
        }
    }
}
