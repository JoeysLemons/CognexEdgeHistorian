using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EdgePcConfigurationApp.Helpers;
using EdgePcConfigurationApp.Models;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Wpf.Ui.Common.Interfaces;

namespace EdgePcConfigurationApp.ViewModels
{
    public partial class DashboardViewModel : ObservableObject, INavigationAware
    {
        public ObservableCollection<CognexCamera> CognexCameras { get; set; } = new ObservableCollection<CognexCamera>();
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConnectToCameraCommand))]
        private string? endpoint;

        private CognexCamera? selectedCamera;

        public ObservableCollection<Tag> Tags { get; set; } = new ObservableCollection<Tag>();

        public CognexCamera SelectedCamera
        {
            get { return selectedCamera; }
            set 
            { 
                selectedCamera = value;
                UpdateTagBrowser();
            }
        }

        public void OnNavigatedTo()
        {
        }

        public void OnNavigatedFrom()
        {
        }
        private bool CanConnectToCamera()
        {
            
            if (endpoint == null || endpoint == string.Empty)
                return false;
            var result = CognexCameras.FirstOrDefault(s => s.Endpoint == endpoint);
            if (result?.Endpoint == endpoint)
                return false;
            return true;
        }
        
        [RelayCommand(CanExecute = nameof(CanConnectToCamera))]
        private async Task ConnectToCamera()
        {
            Trace.WriteLine($"Endpoint: {endpoint}");
            try
            {
                var opcConfig = OPCUAUtils.CreateApplicationConfiguration();
                await OPCUAUtils.InitializeApplication();
                Session session = await OPCUAUtils.ConnectToServer(opcConfig, $"opc.tcp://{endpoint}:4840");
                CognexCameras.Add(new CognexCamera(session.SessionId.ToString(), endpoint));
            }
            catch(Exception ex)
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


        private async Task<List<Tag>> BrowseChildren(Session session, ReferenceDescriptionCollection references)
        {

            try
            {
                List<Tag> nodes = new List<Tag>();
                foreach (var reference in references)
                {
                    string displayName = reference.DisplayName.ToString();
                    string nodeId = reference.NodeId.ToString();
                    Tag tag = new Tag(displayName, nodeId, session.SessionName);
                    Tags.Add(tag);
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
            catch (Exception ex)
            {
                Trace.WriteLine($"Error while attempting to browse tags from OPC UA server. \nError Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
                return null;
            }
        }
        public async void UpdateTagBrowser()
        {
            try
            {
                if (SelectedCamera != null)
                    await BrowseChildren(SelectedCamera.Session, SelectedCamera.Session.FetchReferences(SelectedCamera.Session.SessionId));
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error while attempting to update tag browser. \nError Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }
    }
}
