using CommunityToolkit.Mvvm.ComponentModel;
using EdgePcConfigurationApp.Helpers;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace EdgePcConfigurationApp.Models
{
    public class CognexCamera : ObservableObject
    {
        public Session? Session { get; set; }
        public string Endpoint { get; init; } = string.Empty;
        public string SessionName { get; init; } = string.Empty;
        public ObservableCollection<Tag>? Tags { get; set; }

        public CognexCamera(string sessionName, string endpoint)
        {
            SessionName = sessionName;
            Endpoint = endpoint;
        }
    }
}
