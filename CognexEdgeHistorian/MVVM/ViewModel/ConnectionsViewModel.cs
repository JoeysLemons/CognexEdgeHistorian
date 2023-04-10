using CognexEdgeHistorian.Core;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Opc.Ua.Client;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.ObjectModel;
using CognexEdgeHistorian.MVVM.Model;
using Opc.Ua.Server;
using System.Net;
using Org.BouncyCastle.Crypto.Digests;
using Session = Opc.Ua.Client.Session;

namespace CognexEdgeHistorian.MVVM.ViewModel
{
    public class ConnectionsViewModel : ViewModelBase
    {
        public ICommand ConnectToCamera { get; }
        public ICommand DisconnectFromCamera { get; }


        /// <summary>
        /// Holds the value of the currently selected camera in the connection pane. The ItemSelected property of the listbox
        /// is bound to this property in the view
        /// </summary>
        private static CognexSession _selectedCamera;
        public CognexSession SelectedCamera
        {
            get { return _selectedCamera; }
            set 
            {
                _selectedCamera = value;
                UpdateTagBrowser();
            }
        }
        public static CognexSession GetSelectedCamera()
        {
            return _selectedCamera;
        }

        /// <summary>
        /// Contians a list of all the tags available in the currently selected camera
        /// </summary>
        private List<string> _allTags;
        public List<string> AllTags
        {
            get { return _allTags; }
            set
            { 
                _allTags = value;
                OnPropertyChanged(nameof(AllTags));
            }
        }

        private static Dictionary<string, List<string>> _selectedTags;
        public static Dictionary<string, List<string>> SelectedTags
        {
            get { return _selectedTags; }
            set 
            {
                _selectedTags = value; 
            }
        }

        /// <summary>
        /// List of all the open OPC UA sessions
        /// </summary>
        public static ObservableCollection<CognexSession> SessionList { get; set; }
        public static void AddSelectedTag(string deviceName, string tagName)
        {
            SelectedTags.TryGetValue(deviceName, out List<string> tags);
            tags?.Add(tagName);
            if(tags == null)
            {
                SelectedTags.Add(deviceName, new List<string>());
                SelectedTags.TryGetValue(deviceName, out tags);
                tags.Add(tagName);
            }
        }
        public static void RemoveSelectedTag(string deviceName, string tagName)
        {
            SelectedTags.TryGetValue(deviceName, out List<string> tags);
            tags.Remove(tagName);
        }

        public void Disconnect(object parameter)
        {
            CognexSession result = SessionList.FirstOrDefault(s => s.Endpoint == (string)parameter);
            SelectedTags.Remove(SelectedCamera.Endpoint);
            SessionList.Remove(result);
            ClearTagBrowser();
            result.Session?.Dispose();
        }
        public async void Connect(object parameter)
        {
            string endpoint = (string)parameter;
            Session session;
            var config = OPCUAUtils.CreateApplicationConfiguration();
            await OPCUAUtils.InitializeApplication();
            session = await OPCUAUtils.ConnectToServer(config, $"opc.tcp://{endpoint}");

            ReferenceDescriptionCollection references;
            Byte[] continuationPoint;

            session.Browse(
                null,
                null,
                ObjectIds.ObjectsFolder,
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HierarchicalReferences,
                true,
                uint.MaxValue,
                out continuationPoint,
                out references);

                SessionList.Add(new CognexSession(session, endpoint, session.SessionName, references));
        }
        private static async Task<List<string>> BrowseChildren(Session session, ReferenceDescriptionCollection references)
        {
            List<string> displayNames = new List<string>();
            foreach (var reference in references)
            {
                string displayName = reference.DisplayName.ToString();
                displayNames.Add(displayName);
                Console.WriteLine($"DisplayName: {displayName}, NodeId: {reference.NodeId}");

                ReferenceDescriptionCollection childReferences;
                Byte[] continuationPoint;

                session.Browse(
                    null,
                    null,
                    ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris),
                    0u,
                    BrowseDirection.Forward,
                    ReferenceTypeIds.HierarchicalReferences,
                    true,
                    uint.MaxValue,
                    out continuationPoint,
                    out childReferences);

                if (childReferences.Count > 0)
                {
                    List<string> childDisplayNames = await BrowseChildren(session, childReferences);
                    displayNames.AddRange(childDisplayNames);
                }
            }

            return displayNames;
        }

        public async void UpdateTagBrowser()
        {
            try
            {
                AllTags = await BrowseChildren(SelectedCamera.Session, SelectedCamera.References);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to browse tags: {ex.Message}");
            }
        }
        public void ClearTagBrowser()
        {
            AllTags.Clear();
            OnPropertyChanged(nameof(AllTags));
            var allTagsViewSource = CollectionViewSource.GetDefaultView(AllTags);
            allTagsViewSource.Refresh();
        }
            
        public ConnectionsViewModel()
        {
            ConnectToCamera = new RelayCommand(Connect);
            DisconnectFromCamera = new RelayCommand(Disconnect);
            SelectedTags = new Dictionary<string, List<string>>();
            SessionList = new ObservableCollection<CognexSession>();
        }
    }
}
