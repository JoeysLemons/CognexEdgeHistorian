using CommunityToolkit.Mvvm.ComponentModel;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdgePcConfigurationApp.Models
{
    public class CognexCamera : ObservableObject
    {
        public Session? Session { get; set; }
        public string Endpoint { get; init; }
        public string SessionName { get; init; }
        public List<string> Tags { get; } = new List<string>();

        public CognexCamera(string sessionName, string endpoint)
        {
            SessionName = sessionName;
            Endpoint = endpoint;
        }
    }
}
