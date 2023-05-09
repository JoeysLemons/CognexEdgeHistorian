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
        public List<string> Tags { get; set; }
        public ReferenceDescriptionCollection References { get; set; }
        public Subscription Subscription { get; set; }

        public CognexSession(Session session, string endpoint, string sessionName, int id)
        {
            SessionName = sessionName;
            Endpoint = endpoint;
            Session = session;
            ID = id;
        }
    }
}
