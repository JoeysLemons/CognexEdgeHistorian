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
        public Session Session { get; init; }
        public string Endpoint { get; init; } = string.Empty;
        public string SessionName { get; init; } = string.Empty;
        public ReferenceDescriptionCollection References { get; init; }
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


        public CognexCamera(Session session, string sessionName, string endpoint, ReferenceDescriptionCollection references)
        {
            Session = session;
            SessionName = sessionName;
            Endpoint = endpoint;
            References = references;
        }
    }
}
