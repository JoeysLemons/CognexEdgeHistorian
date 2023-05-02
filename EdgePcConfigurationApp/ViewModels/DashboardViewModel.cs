using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EdgePcConfigurationApp.Helpers;
using EdgePcConfigurationApp.Models;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Wpf.Ui.Common.Interfaces;
using Session = Opc.Ua.Client.Session;

namespace EdgePcConfigurationApp.ViewModels
{
    public partial class DashboardViewModel : ObservableObject, INavigationAware
    {
        public ObservableCollection<CognexCamera> CognexCameras { get; set; } = new ObservableCollection<CognexCamera>();
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConnectToCameraCommand))]
        private string? endpoint;

        private CognexCamera? selectedCamera;

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

        public CognexCamera SelectedCamera
        {
            get { return selectedCamera; }
            set 
            { 
                selectedCamera = value;
                UpdateTagBrowser();
            }
        }
        public void OnNavigatedTo() { }
        public void OnNavigatedFrom() { }

        private bool CanConnectToCamera()
        {
            
            if (endpoint == null || endpoint == string.Empty)
                return false;
            var result = CognexCameras.FirstOrDefault(s => s.Endpoint == endpoint);
            if (result?.Endpoint == endpoint)
                return false;
            return true;
        }

        #region RelayCommands
        [RelayCommand(CanExecute = nameof(CanConnectToCamera))]
        private async Task ConnectToCamera()
        {
            Trace.WriteLine($"Endpoint: {endpoint}");
            try
            {
                var opcConfig = OPCUAUtils.CreateApplicationConfiguration();
                await OPCUAUtils.InitializeApplication();
                Session session = await OPCUAUtils.ConnectToServer(opcConfig, $"opc.tcp://{endpoint}:4840");

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
                int cameraId = DatabaseUtils.AddCamera(session.SessionName, endpoint);

                CognexCamera camera = new CognexCamera(session, session.SessionName, endpoint, cameraId, references);
                CognexCameras.Add(camera);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error while attempting to connect to camera. Error Message: {ex.Message}");
            }
        }

        [RelayCommand]
        private void DisconnectFromCamera()
        {
            try
            {
                var result = CognexCameras.FirstOrDefault(s => s.Endpoint == endpoint);
                result.Tags.Clear();
                result.Session?.Dispose();
                CognexCameras.Remove(result);
            }
            catch (NullReferenceException)
            {
                Trace.WriteLine("Selected Camera was null or the list CognexCameras is empty");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error while attempting to disconnect from OPC UA server. \nError Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

        [RelayCommand]
        public void SubscribeTag(object parameter)
        {
            try
            {
                Tag selectedItem = parameter as Tag;
                if (selectedItem == null)
                {
                    throw new NullReferenceException();
                }
                if (selectedItem.IsChecked)
                {
                    selectedCamera.SubscribedTags.Add(selectedItem);
                }
                else
                {
                    selectedCamera.SubscribedTags.Remove(selectedItem);
                }
                
            }
            catch (NullReferenceException ex)
            {
                Trace.WriteLine($"Could not convert parameter to ListBoxItem. \nError Message: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error while attempting to add tag to subscribtion list. \nError Message: {ex.Message}");
                return;
            }


        }

        [RelayCommand]
        public void ApplyChanges()
        {
            foreach (Tag tag in selectedCamera.SubscribedTags)
            {
                tag.TagId = DatabaseUtils.AddTag(selectedCamera.CameraID, tag.Name, tag.NodeId);
            }
        }
        #endregion RelayCommands




        public async Task<ObservableCollection<Tag>> BrowseChildren(Session session, ReferenceDescriptionCollection references)
        {
            try
            {
                ObservableCollection<Tag> nodes = new ObservableCollection<Tag>();

                foreach (var reference in references)
                {
                    string displayName = reference.DisplayName.ToString();
                    string nodeId = reference.NodeId.ToString();

                    Console.WriteLine($"DisplayName: {displayName}, NodeId: {reference.NodeId}");

                    // Create a new Tag object for this node
                    Tag node = new Tag(displayName, nodeId, session.SessionName);

                    ReferenceDescriptionCollection childReferences;
                    Byte[] continuationPoint;

                    // Browse the children of this node recursively
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
                        // Recursively browse the children of this node
                        ObservableCollection<Tag> childNodes = await BrowseChildren(session, childReferences);

                        // Add the child nodes to the current node
                        node.Children.AddRange(childNodes);
                    }

                    // Add the current node to the list of nodes
                    nodes.Add(node);
                }

                return nodes;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error attempting to browse Server Tag list. Error Message {ex.Message}");
                return null;
            }
        }
        
        public void SetTagBrowserConfiguration(ObservableCollection<Tag> tagList, List<string> searchParams)
        {
            foreach(Tag tag in tagList)
            {
                if(searchParams.Contains(tag.Name))
                {
                    tag.IsChecked = true;
                    SelectedCamera.SubscribedTags.Add(tag);
                }
                if(tag.Children.Count > 0)
                {
                    ObservableCollection<Tag> children = new ObservableCollection<Tag>(tag.Children);
                    SetTagBrowserConfiguration(children, searchParams); //mmmm recursion 
                }
            }
        }
        public async void UpdateTagBrowser()
        {
            try
            {
                if (SelectedCamera != null)
                {
                    SelectedCamera.Tags = await BrowseChildren(SelectedCamera.Session, SelectedCamera.References);
                    //Check to see if camera exists in database
                    if (DatabaseUtils.CameraExists(SelectedCamera.Endpoint))
                    {
                        List<string> tagsNames = DatabaseUtils.GetSavedTagConfiguration(SelectedCamera.Endpoint);
                        if(tagsNames.Count != 0)
                        {
                            SetTagBrowserConfiguration(SelectedCamera.Tags, tagsNames);
                        }
                    }
                    Tags = SelectedCamera.Tags;
                }
                    
            }
            catch(NullReferenceException)
            {
                Trace.WriteLine($"SelectedCamera.Session was null or returned a null value while attempting to update the tag browser.");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error while attempting to update tag browser. \nError Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }
    }
}
