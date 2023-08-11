using CommunityToolkit.Mvvm.ComponentModel;
using EdgePcConfigurationApp.Helpers;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace EdgePcConfigurationApp.Models
{
    public class CognexCamera : ObservableObject
    {
        public Session Session { get; set; }
        public string Endpoint { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public string Name { get; set; }
        public int CameraID { get; set; } 
        public bool Connected { get; set; }
        public string Region { get; set; }
        public string Location { get; set; }
        public string ProductionLine { get; set; }
        public List<string> jobs { get; set; } = new List<string>();
        public ReferenceDescriptionCollection References { get; set; }
        private ObservableCollection<Tag> _tags;

        public ObservableCollection<Tag> Tags
        {
            get { return _tags; }
            set 
            {
                _tags = value;
                OnPropertyChanged(nameof(Tags));
            }
        }
        private ObservableCollection<Tag> _subscribedTags = new ObservableCollection<Tag>();

        public ObservableCollection<Tag> SubscribedTags
        {
            get { return _subscribedTags; }
            set { _subscribedTags = value; }
        }
        public static string GetHostNameFromIpAddress(string ipAddress)
        {
            try
            {
                IPAddress ip = IPAddress.Parse(ipAddress);
                IPHostEntry hostEntry = Dns.GetHostEntry(ip);
                Console.WriteLine("Host name: " + hostEntry.HostName);
                return hostEntry.HostName;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                return string.Empty;
            }
        }

        public CognexCamera(string name, string endpoint)
        {
            Endpoint = endpoint;
            Name = name;
            HostName = GetHostNameFromIpAddress(Endpoint);
        }
    }
}
