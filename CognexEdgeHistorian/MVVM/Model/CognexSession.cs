using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CognexEdgeHistorian.MVVM.Model
{
    public class CognexSession
    {
        public Session Session { get; }
        public string Endpoint { get; }
        public string SessionName { get; }
        public List<string> Tags { get; } = new List<string>();
        public ReferenceDescriptionCollection References { get; set; }
        public Subscription Subscription { get; set; }

        public CognexSession(Session session, string endpoint, string sessionName, ReferenceDescriptionCollection references)
        {
            SessionName = sessionName;
            Endpoint = endpoint;
            Session = session;
            References = references;
        }
    }
}
