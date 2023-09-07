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
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace EdgePcConfigurationApp.Models
{
    public class CognexCamera : ObservableObject
    {
        public Session Session { get; set; }

        public string Endpoint
        {
            get => _endpoint;
            set
            {
                //Check to make sure the endpoint is not being changed to an existing endpoint
                CameraID = DatabaseUtils.GetCameraIdByEndpoint(Endpoint);
                bool canUpdate = DatabaseUtils.UpdateCameraEndpoint(value, CameraID);
                if(!canUpdate)
                    return;
                
                if (value == _endpoint) return;
                _endpoint = value;
                OnPropertyChanged();
            }
        }

        public string MacAddress
        {
            get => _macAddress;
            set
            {
                if (value == _macAddress) return;
                _macAddress = value;
                OnPropertyChanged();
            }
        }

        public string SessionName { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;

        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }
        
        private int _cameraId;
        public int CameraID
        {
            get => _cameraId;
            set => _cameraId = value;
        }

        private bool _connected = false;
        public bool Connected
        {
            get => _connected;
            set
            {
                if (value == _connected) return;
                _connected = value;
                Disconnected = !_connected;
                OnPropertyChanged(nameof(Connected));
            }
        }
        
        private bool _disconnected = true;
        public bool Disconnected
        {
            get => _disconnected;
            set
            {
                if (value == _disconnected) return;
                _disconnected = value;
                OnPropertyChanged(nameof(Disconnected));
            } 
        }

        private bool _connecting = false;
        public bool Connecting
        {
            get => _connecting;
            set
            {
                if (value == _connecting) return;
                _connecting = value;
                OnPropertyChanged();
            }
        }
        
        private bool _defaultTagError = false;
        public bool DefaultTagError
        {
            get => _defaultTagError;
            set
            {
                if (value == _defaultTagError) return;
                _defaultTagError = value;
                OnPropertyChanged();
            }
        }

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
        private string _endpoint = string.Empty;
        private string _name;
        private string _macAddress;


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
        }
    }
}
