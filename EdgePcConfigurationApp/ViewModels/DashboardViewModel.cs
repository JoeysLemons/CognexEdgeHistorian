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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using EdgePcConfigurationApp.Views;
using EdgePcConfigurationApp.Views.Pages;
using EdgePcConfigurationApp.Views.Windows;
using Wpf.Ui.Common;
using Wpf.Ui.Common.Interfaces;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using Session = Opc.Ua.Client.Session;

namespace EdgePcConfigurationApp.ViewModels
{
    public partial class DashboardViewModel : ObservableObject, INavigationAware
    {
        // Used to keep track of all the cameras currently connected
        public static ObservableCollection<CognexCamera> CognexCameras { get; set; } = new ObservableCollection<CognexCamera>();
        [ObservableProperty]
        private string? endpoint;   //Holds text that is in the IP address box

        //Holds the currently selected camera
        private CognexCamera? selectedCamera;
        public CognexCamera SelectedCamera
        {
            get { return selectedCamera; }
            set
            {
                selectedCamera = value;
                UpdateTagBrowser();
            }
        }

        [ObservableProperty] public string jobName = "No Job Selected";
        
        [ObservableProperty]
        public bool isCameraSettingsOpen;

        //Holds the collection of tags that should be currently displayed in the tag browser
        private ObservableCollection<Tag> _tags = new ObservableCollection<Tag>();
        public ObservableCollection<Tag> Tags
        {
            get { return _tags; }
            set
            {
                _tags = value;
                OnPropertyChanged(nameof(Tags));
            }
        }
        //! I think this could be obsolete but im too scared to remove it
        private static bool changesSaved { get; set; } = true;

        //Holds the current error message that should be displayed in the error log. If this string is empty the error log is not visible
        private string errorMessage = string.Empty;
        public string ErrorMessage
        {
            get { return errorMessage; }
            set 
            {
                errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
                OnPropertyChanged(nameof(IsStringNotEmpty));
            }
        }

        //Used for the BooleanToVisibilityConverter in the XAML Code. It just returns a true if the string is not empty and false if it is empty.
        public bool IsStringNotEmpty
        {
            get { return !string.IsNullOrEmpty(errorMessage); }
        }

        public void AddCamera(CameraInfo cameraInfo)
        {
            ConnectToCamera(cameraInfo);
        }
        
        //Looks for cameras assigned to this PC and if detected will autoconnect to them.
        public async Task SearchForPreviousCameras()
        {
            string computerGuid = AppConfigUtils.GetComputerGUID();
            int pcID = DatabaseUtils.GetPCIdFromGUID(computerGuid);
            List<CameraInfo> previousCameras = DatabaseUtils.GetpreviouslyConnectedCameras(pcID);
            foreach (CameraInfo cameraInfo in previousCameras)
            {
                if (CognexCameras.Any(c => c.Endpoint == cameraInfo.IpAddress))
                    continue;
                await ConnectToCamera(cameraInfo);
            }
        }
        //Leaving these here to implement the INavigationAware Interface. May possibly use these in the future 
        public void OnNavigatedTo()
        {
            Task.Run(SearchForPreviousCameras);
        }
        public void OnNavigatedFrom() { }

        


        #region RelayCommands
        //Connect to camera command
        
        private async Task ConnectToCamera(CameraInfo cameraInfo)
        {
            string ipAddress = cameraInfo.IpAddress;
            Trace.WriteLine($"Endpoint: {ipAddress}");
            try
            {
                bool connected = false;
                CognexCamera camera = new CognexCamera(cameraInfo.Name, ipAddress); //Create a new instance of a CognexCamera
                App.Current.Dispatcher.Invoke(() =>
                {
                    CognexCameras.Add(camera);  //Add camera to list of connected cameras
                });
                ReferenceDescriptionCollection references;
                var opcConfig = OPCUAUtils.CreateApplicationConfiguration();    //Create OPC UA App Config
                await OPCUAUtils.InitializeApplication();                       //Initialize OPC UA Client using app config
                Session session = null;
                try
                {
                    session = await OPCUAUtils.ConnectToServer(opcConfig, $"opc.tcp://{ipAddress}:4840");    //Connect to Cognex OPC UA Server via the endpoint which is retrieved from the endpoint textbox
                    connected = true;
                }
                catch (Opc.Ua.ServiceResultException ex)
                {
                    Console.WriteLine(ex);
                }

                if (connected && session != null)
                {
                    //Browse through the top level tags on the server
                    
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
                    string pcGUID = AppConfigUtils.GetComputerGUID();
                    int pcID = DatabaseUtils.GetPCIdFromGUID(pcGUID);
                    int cameraId = DatabaseUtils.AddCamera(cameraInfo.Name, ipAddress, pcID);      //Adds the camera to the database if not already there. This is so that if the camera is connected to again config data can be loaded
                    camera.Session = session;
                    camera.CameraID = cameraId;
                    camera.References = references;
                    camera.HostName = session.SessionName;
                }
                
                
                
            }
            catch(InvalidOperationException ex)
            {
                ErrorMessage = "An error was encounted while attempting to communicate to the database. Please double check connection string.";
                Trace.WriteLine($"An error was encountered while attempting to communicate with the database. \nError Message: {ex.Message}");
            }
            catch (Exception ex)
            {
                ErrorMessage = "We encountered an error while attempting to connect to the camera. Please double check connections and IP Address then try again.";
                Trace.WriteLine($"Error while attempting to connect to camera. Error Message: {ex.Message}");
            }
        }

        [RelayCommand]
        public void DisconnectFromCamera()
        {
            if (CognexCameras.Count == 0)
                return;
            try
            {
                var result = CognexCameras.FirstOrDefault(s => s.Endpoint == SelectedCamera.Endpoint);
                result.Tags.Clear();
                result.Session?.Dispose();
                Tags.Clear();
                CognexCameras.Remove(result);
            }
            catch (NullReferenceException)
            {
                ErrorMessage = "Selected Camera was null or the list of cameras is empty. Please make sure you are connected to a device before attempting to disconnect";
                Trace.WriteLine("Selected Camera was null or the list CognexCameras is empty");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"We encountered an error while attempting to disconnect from the camera. \nError Message: {ex.Message}";
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
                selectedItem.Synced = false;
                if (selectedItem.IsChecked)
                {
                    SelectedCamera.SubscribedTags.Add(selectedItem);
                }
                else
                {
                    SelectedCamera.SubscribedTags.Remove(selectedItem);
                }
            }
            catch (NullReferenceException ex)
            {
                ErrorMessage = $"Something went wrong while attempting to subscribe to the selected OPC UA tag. \nError Message: {ex.Message}";
                Trace.WriteLine($"Could not convert parameter to ListBoxItem. \nError Message: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                ErrorMessage = "An unknown error occured while attemping to subscribe to the selected OPC UA tag. If this error persists please contact your system administrator";
                Trace.WriteLine($"Error while attempting to add tag to subscribtion list. \nError Message: {ex.Message}");
                return;
            }


        }

        [RelayCommand]
        public void ApplyChanges()
        {
            if (SelectedCamera is null)
                return;
            try
            {
                DatabaseUtils.ResetTagMonitoredStatus(SelectedCamera.CameraID);
                foreach (Tag tag in SelectedCamera.SubscribedTags)
                {
                    tag.TagId = DatabaseUtils.AddTag(SelectedCamera.CameraID, tag.Name, tag.NodeId);
                    DatabaseUtils.UpdateTagMonitoredStatus(tag.Name, SelectedCamera.CameraID, 1);
                }
                ResetSyncIcons(Tags);
            }
            catch (Exception)
            {
                
                throw;
            }
        }

        [RelayCommand]
        public void ResetTagBrowser()
        {
            if (SelectedCamera is null)
                return;
            if (DatabaseUtils.CameraExists(SelectedCamera.Endpoint))
            {
                List<string> tagsNames = DatabaseUtils.GetSavedTagConfiguration(SelectedCamera.Endpoint);
                if (tagsNames.Count != 0)
                {
                    SetTagBrowserConfiguration(SelectedCamera.Tags, tagsNames);
                }
            }
            SearchTag(SelectedCamera.Tags, "Spreadsheet");
            ResetSyncIcons(Tags);
        }

        [RelayCommand]
        public void ClearErrors()
        {
            ErrorMessage = string.Empty;
        }
        [RelayCommand]
        public void SetCameraSettings(object parameter)
        {
            CognexCamera camera = parameter as CognexCamera;
            CameraSettingsWindow cameraSettingsWindow = new CameraSettingsWindow();
            cameraSettingsWindow.DataContext = new CameraInfoViewModel(cameraSettingsWindow, this, camera);
            cameraSettingsWindow.ShowDialog();
        }
        [RelayCommand]
        public void Debug()
        {
            
        }
        
        [RelayCommand]
        public void OpenAddCameraDialog()
        {
            CameraSettingsWindow cameraSettingsWindow = new CameraSettingsWindow();
            cameraSettingsWindow.DataContext = new CameraInfoViewModel(cameraSettingsWindow, this);
            cameraSettingsWindow.ShowDialog();
        }
        
        #endregion RelayCommands

        public void ResetSyncIcons(ObservableCollection<Tag> tagList)
        {
            foreach(Tag tag in tagList)
            {
                tag.Synced = true;
                ObservableCollection<Tag> children = new ObservableCollection<Tag>(tag.Children);
                ResetSyncIcons(children);
            }
        }

        /// <summary>
        /// This method will recursively browse all available tags on the OPC UA server.
        /// </summary>
        /// <param name="session">The active OPC UA Session you would like to browse</param>
        /// <param name="references"></param>
        /// <returns>Returns an ObservableCollection of the Tag object</returns>
        public async Task<ObservableCollection<Tag>> BrowseChildren(Session session, ReferenceDescriptionCollection references)
        {
            try
            {
                ObservableCollection<Tag> nodes = new ObservableCollection<Tag>();
                if (references is null)
                    throw new NullReferenceException();
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
            catch (NullReferenceException ex)
            {
                ErrorMessage =
                    "The device you selected is either disconnected or does not support OPC UA. Please ensure that the camera is connected properly and that it supports OPC UA.";
                Trace.WriteLine(ErrorMessage);
                return null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error attempting to browse Server Tag list. Error Message {ex.Message}");
                return null;
            }
        }
        /// <summary>
        /// Used to update the tag browser with the correct tags and the correct tag monitored state
        /// </summary>
        /// <param name="tagList">A list of tags to display in the tag browser</param>
        /// <param name="searchParams">The list of tags names that are currently selected</param>
        public void SetTagBrowserConfiguration(ObservableCollection<Tag> tagList, List<string> searchParams)
        {
            foreach(Tag tag in tagList)
            {
                if(searchParams.Contains(tag.Name))
                {
                    tag.IsChecked = true;
                    SelectedCamera.SubscribedTags.Add(tag);
                }
                else
                {
                    tag.IsChecked = false;
                    SelectedCamera.SubscribedTags.Remove(tag);
                }
                if(tag.Children.Count > 0)
                {
                    ObservableCollection<Tag> children = new ObservableCollection<Tag>(tag.Children);
                    SetTagBrowserConfiguration(children, searchParams); //mmmm recursion 
                }
            }
        }

        /// <summary>
        /// Searches through a list of tags and 
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="searchParam"></param>
        private void SearchTag(ObservableCollection<Tag> tags, string searchParam)
        {
            foreach (Tag tag in tags)
            {
                if (tag.Name == searchParam)
                {
                    Tags = new ObservableCollection<Tag>(tag.Children);
                    return;
                }
                else if(tag.Children.Count > 0)
                {
                    ObservableCollection<Tag> children = new ObservableCollection<Tag>(tag.Children);
                    SearchTag(children, searchParam);
                }
            }
        }

        private string GetJobName(ObservableCollection<Tag> tags)
        {
            List<Tag> results = new List<Tag>();
            string searchParam = "System";
            foreach (Tag tag in tags)
            {
                if (tag.Name == searchParam)
                {
                    results = new List<Tag>(tag.Children);
                    break;
                }
                else if(tag.Children.Count > 0)
                {
                    ObservableCollection<Tag> children = new ObservableCollection<Tag>(tag.Children);
                    GetJobName(children);
                }
            }

            if (results.Count == 0) return "Job Not Found";
                
            foreach (Tag tag in results)
            {
                if (tag.Name == "JobName")
                {
                    return tag.Value.ToString();
                }
            }

            return "Job Not Found";
        }

        public static void DisconnectFromAllDevices(ObservableCollection<CognexCamera> deviceList)
        {
            foreach(CognexCamera camera in deviceList)
            {
                camera.Tags?.Clear();
                camera.Session?.Dispose();
            }
        }
        public async void UpdateTagBrowser()
        {
            try
            {
                if (SelectedCamera != null)
                {
                    SelectedCamera.Tags = await BrowseChildren(SelectedCamera.Session, SelectedCamera.References);
                    if (selectedCamera.Tags == null)
                        return;
                    //Check to see if camera exists in database
                    if (DatabaseUtils.CameraExists(SelectedCamera.Endpoint))
                    {
                        //Get a list of all the tags that are currently saved in the database
                        List<string> tagsNames = DatabaseUtils.GetSavedTagConfiguration(SelectedCamera.Endpoint);
                        if(tagsNames.Count != 0)
                        {
                            SetTagBrowserConfiguration(SelectedCamera.Tags, tagsNames);
                        }
                    }
                    SearchTag(SelectedCamera.Tags, "Spreadsheet");
                    //This is broken need to actually dive into the OPC node itself and pull the value from the server not just from the tag list
                    //jobName = GetJobName(selectedCamera.Tags);
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
