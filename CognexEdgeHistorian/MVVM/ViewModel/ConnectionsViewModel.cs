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
using Session = Opc.Ua.Client.Session;
using System.Data;
using CognexEdgeHistorian.Commands;
using CognexEdgeHistorian.Stores;
using CognexEdgeHistorian.Services;

namespace CognexEdgeHistorian.MVVM.ViewModel
{
    public class ConnectionsViewModel : ViewModelBase
    {
        public ICommand ConnectToCamera { get; }
        public ICommand DisconnectFromCamera { get; }
        public ICommand NavigateDataHistorianCommand { get; }

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
        private static List<Tag> _allTags;
        public List<Tag> AllTags
        {
            get { return _allTags; }
            set
            { 
                _allTags = value;
                OnPropertyChanged(nameof(AllTags));
            }
        }
        private List<string> _displayNames;

        public List<string> DisplayNames
        {
            get { return _displayNames; }
            set {
                    _displayNames = value; 
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
        public static Tag GetTagByDisplayName(string displayName)
        {
            return _allTags.FirstOrDefault(tag => tag.Name == displayName);
        }
        /// <summary>
        /// List of all the open OPC UA sessions
        /// </summary>
        public static ObservableCollection<CognexSession> SessionList { get; set; }
        public static void AddSelectedTag(CognexSession session, Tag tag)
        {
            session.Tags.Add(tag.Name);
            OPCUAUtils.AddMonitoredItem(session.Subscription, tag.NodeId, OPCUAUtils.OnTagValueChanged);
            DataTable camera = DatabaseUtils.GetCameraByEndpoint(GetSelectedCamera().Endpoint);
            DatabaseUtils.AddTag(Int32.Parse(camera.Rows[0]["id"].ToString()), tag.Name);
        }
        
        public static void RemoveSelectedTag(CognexSession session, Tag tag)
        {
            session.Tags.Remove(tag.Name);
            OPCUAUtils.RemoveMonitoredItem(session.Subscription, tag.NodeId);
        }

        public void Disconnect(object parameter)
        {
            CognexSession result = SessionList.FirstOrDefault(s => s.Endpoint == (string)parameter);
            result.Tags.Clear();
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

            CognexSession cognexSession = new CognexSession(session, endpoint, session.SessionName, references);
            cognexSession.Subscription = OPCUAUtils.CreateSubscription(session);
            SessionList.Add(cognexSession);
            DatabaseUtils.AddCamera(session.SessionName, endpoint);
        }
        private static async Task<List<Tag>> BrowseChildren(Session session, ReferenceDescriptionCollection references)
        {
            List<Tag> nodes = new List<Tag>();
            foreach (var reference in references)
            {
                string displayName = reference.DisplayName.ToString();
                string nodeId = reference.NodeId.ToString();
                nodes.Add(new Tag(displayName, nodeId, session.SessionName));
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
                    List<Tag> childNodes = await BrowseChildren(session, childReferences);
                    nodes.AddRange(childNodes);
                }
            }

            return nodes;
        }

        public async void UpdateTagBrowser()
        {
            try
            {
                if(SelectedCamera != null)
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
            
        public ConnectionsViewModel(NavigationStore navigationStore)
        {
            ConnectToCamera = new RelayCommand(Connect);
            DisconnectFromCamera = new RelayCommand(Disconnect);
            NavigateDataHistorianCommand = new NavigateCommand<DataHistorianViewModel>(new NavigationService<DataHistorianViewModel>(
                navigationStore, () => new DataHistorianViewModel(navigationStore)));
            SelectedTags = new Dictionary<string, List<string>>();
            SessionList = new ObservableCollection<CognexSession>();
        }
    }
}
