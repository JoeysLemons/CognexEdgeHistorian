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
using System.Data;
using System.Data.Entity.Infrastructure.Design;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using EdgePcConfigurationApp.Exceptions;
using EdgePcConfigurationApp.Views;
using EdgePcConfigurationApp.Views.Pages;
using EdgePcConfigurationApp.Views.Windows;
using Microsoft.AspNetCore.Http;
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
        public static ObservableCollection<CognexCamera> CognexCameras { get; set; } =
            new ObservableCollection<CognexCamera>();

        [ObservableProperty] private string? endpoint; //Holds text that is in the IP address box

        //Holds the currently selected camera
        private CognexCamera? selectedCamera;

        public CognexCamera SelectedCamera
        {
            get { return selectedCamera; }
            set
            {
                selectedCamera = value;
                Tags.Clear();
                DisplayTags.Clear();
                Task.Run(() => CheckCameraOnline(selectedCamera));
                Task.Run(UpdateTagBrowser);
            }
        }
        
        
        [ObservableProperty] private string selectedJob = "No Job Selected";

        [ObservableProperty] public bool isCameraSettingsOpen;

        private void UpdateDisplayTags()
        {
            ObservableCollection<Tag> temp  = new ObservableCollection<Tag>(Tags);
            var acqCountTag = temp.FirstOrDefault(tag => tag.Name == "AcquisitionCount");
            var imgFileNameTag = temp.FirstOrDefault(tag => tag.Name == "ImageFileName");
            temp.Remove(acqCountTag);
            temp.Remove(imgFileNameTag);
            DisplayTags = temp;
        }
        private ObservableCollection<Tag> displayTags = new ObservableCollection<Tag>();
        public ObservableCollection<Tag> DisplayTags
        {
            get { return displayTags; }
            private set
            {
                displayTags = value;
                OnPropertyChanged(); // Notify that the DisplayTags property has changed
            }
        }

        //Holds the collection of tags that should be currently displayed in the tag browser
        private ObservableCollection<Tag> _tags = new ObservableCollection<Tag>();

        public ObservableCollection<Tag> Tags
        {
            get { return _tags; }
            set
            {
                _tags = value;
                OnPropertyChanged(nameof(Tags));
                UpdateDisplayTags();
            }
        }

        private List<Tag> UnsycnedTags = new List<Tag>();
        private bool _showSaveTagsButton;

        public bool ShowSaveTagsButton
        {
            get { return _showSaveTagsButton; }
            set
            {
                _showSaveTagsButton = value;
                OnPropertyChanged();
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

        public bool DefaultTagError { get; set; } = false;

        public void AddCamera(CameraInfo cameraInfo)
        {
            CognexCamera camera = new CognexCamera(cameraInfo.Name, cameraInfo.IpAddress);
            CognexCameras.Add(camera);
            Task.Run(() => ConnectToCamera(camera));
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
                CognexCamera camera = new CognexCamera(cameraInfo.Name, cameraInfo.IpAddress);

                App.Current.Dispatcher.Invoke(() =>
                {
                    CognexCameras.Add(camera); //Add camera to list of connected cameras
                });
            }

            foreach (CognexCamera camera in CognexCameras)
            {
                if (!camera.Connected)
                    Task.Run(() => ConnectToCamera(camera));
            }
        }

        public async Task CheckCameraOnline(CognexCamera camera)
        {
            App.Current.Dispatcher.Invoke(() => { camera.Connecting = true; });
            bool success = NetworkUtils.PingHost(camera.Endpoint);
            if (success && camera.Disconnected)
            {
                await Task.Run(() => ConnectToCamera(camera));
                if (camera.Connected)
                    Task.Run(UpdateTagBrowser);
                return;
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                camera.Connected = success;
                camera.Connecting = false;
            });
            
        }

        //Leaving these here to implement the INavigationAware Interface. May possibly use these in the future 
        public void OnNavigatedTo()
        {
            Task.Run(SearchForPreviousCameras);
        }

        public void OnNavigatedFrom()
        {
        }


        public async Task Refresh()
        {
            foreach (CognexCamera camera in CognexCameras)
            {
                if (!camera.Connected && !camera.Connecting)
                    Task.Run(() => ConnectToCamera(camera));
                else if (camera.Connected && !camera.Connecting)
                    Task.Run(() => CheckCameraOnline(camera));
            }
            UpdateTagBrowser();
        }

        #region RelayCommands

        //Connect to camera command

        public async Task ConnectToCamera(CognexCamera camera)
        {
            Trace.WriteLine($"Endpoint: {camera.Endpoint}");
            try
            {
                
                //Check to see if this camera is already connected
                bool hasDuplicates = CognexCameras
                    .GroupBy(cgCam => cgCam.Endpoint)
                    .Any(group => group.Count() > 1);
                if (hasDuplicates)
                    throw new DuplicateConnectionException(
                        "The camera that you are trying to add is already connected to the system.");

                bool connected = false;
                App.Current.Dispatcher.Invoke(() => { camera.Connecting = true; });

                ReferenceDescriptionCollection references;
                var opcConfig = OPCUAUtils.CreateApplicationConfiguration(); //Create OPC UA App Config
                await OPCUAUtils.InitializeApplication(); //Initialize OPC UA Client using app config
                Session session = null;
                try
                {
                    session = await OPCUAUtils.ConnectToServer(opcConfig,
                        $"opc.tcp://{camera.Endpoint}:4840"); //Connect to Cognex OPC UA Server via the endpoint which is retrieved from the endpoint textbox
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
                    camera.MacAddress = NetworkUtils.GetMacAddress(camera.Endpoint);
                    //Adds the camera to the database if not already there. This is so that if the camera is connected to again config data can be loaded
                    int cameraId = DatabaseUtils.AddCamera(camera.Name, camera.Endpoint, camera.MacAddress, pcID);
                    camera.Session = session;
                    camera.CameraID = cameraId;
                    camera.References = references;
                    camera.HostName = session.SessionName;
                    
                }


                App.Current.Dispatcher.Invoke(() =>
                {
                    camera.Connected = connected;
                    camera.Connecting = false;
                });
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage =
                    "An error was encounted while attempting to communicate to the database. Please double check connection string.";
                Trace.WriteLine(
                    $"An error was encountered while attempting to communicate with the database. \nError Message: {ex.Message}");
                camera.Connecting = false;
            }
            catch (DuplicateConnectionException ex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    CognexCameras.Remove(camera);
                });
                ErrorMessage = ex.Message;
                Trace.WriteLine(ex.Message);
                return;
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    "We encountered an error while attempting to connect to the camera. Please double check connections and IP Address then try again.";
                Trace.WriteLine($"Error while attempting to connect to camera. Error Message: {ex.Message}");
                camera.Connecting = false;
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
                result.Tags?.Clear();
                result.Session?.Close();
                result.Session?.Dispose();
                result.Connected = false;
                Tags.Clear();
            }
            catch (NullReferenceException)
            {
                ErrorMessage =
                    "Selected Camera was null or the list of cameras is empty. Please make sure you are connected to a device before attempting to disconnect";
                Trace.WriteLine("Selected Camera was null or the list CognexCameras is empty");
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    $"We encountered an error while attempting to disconnect from the camera. \nError Message: {ex.Message}";
                Trace.WriteLine(
                    $"Error while attempting to disconnect from OPC UA server. \nError Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }
        
        [RelayCommand]
        public void RefreshCameras()
        {
            Task.Run(Refresh);
            UnsycnedTags.Clear();
            ShowSaveTagsButton = false;
        }

        [RelayCommand]
        public void SubscribeTag(object parameter)
        {
            try
            {
                Tag selectedItem = parameter as Tag;
                if (selectedItem == null)
                    throw new NullReferenceException();
                if (selectedItem.IsChecked)
                    SelectedCamera.SubscribedTags.Add(selectedItem);
                else
                    SelectedCamera.SubscribedTags.Remove(selectedItem); 
                //Get Job ID
                int jobID = DatabaseUtils.StoreJob(SelectedJob, SelectedCamera.CameraID);
                List<string> tagConfiguration = DatabaseUtils.GetSavedTagConfiguration(jobID);
                bool found = tagConfiguration.Contains(selectedItem.Name);
                if (found && selectedItem.IsChecked)
                {
                    selectedItem.Synced = true;
                    if (UnsycnedTags.Contains(selectedItem))
                        UnsycnedTags.Remove(selectedItem);
                }
                else if (!found && !selectedItem.IsChecked)
                {
                    selectedItem.Synced = true;
                    if (UnsycnedTags.Contains(selectedItem))
                        UnsycnedTags.Remove(selectedItem);
                }
                else
                {
                    selectedItem.Synced = false;
                    UnsycnedTags.Add(selectedItem);
                }

                ShowSaveTagsButton = UnsycnedTags.Count != 0;
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
                int jobID = DatabaseUtils.GetJobIdFromName(SelectedJob);
                DatabaseUtils.ResetTagMonitoredStatus(jobID);
                foreach (Tag tag in SelectedCamera.SubscribedTags)
                {
                    tag.TagId = DatabaseUtils.AddTag(jobID, tag.Name, tag.NodeId);
                    DatabaseUtils.UpdateTagMonitoredStatus(tag.Name, jobID, 1);
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
            if (SelectedCamera.Tags == null || SelectedCamera.Tags.Count == 0)
                return;

            if (DatabaseUtils.CameraExists(SelectedCamera.Endpoint))
            {
                //Even though we are not storing a job into the database here this should return the id of the job as duplicate jobs are not allowed in the DB
                int jobID = DatabaseUtils.StoreJob(SelectedJob, SelectedCamera.CameraID);
                List<string> tagsNames = DatabaseUtils.GetSavedTagConfiguration(jobID);
                if (tagsNames.Count != 0)
                {
                    SetTagBrowserConfiguration(SelectedCamera.Tags, tagsNames);
                }
            }

            bool insightExplorer = CheckInsightExplorer(selectedCamera.Tags);
            SearchTag(SelectedCamera.Tags, insightExplorer ? "Jobtags" : "Spreadsheet");
            ResetSyncIcons(Tags);
            UnsycnedTags.Clear();
            ShowSaveTagsButton = false;
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
            CameraModifyWindow cameraSettingsWindow = new CameraModifyWindow();
            CameraModifyViewModel viewModel = new CameraModifyViewModel(cameraSettingsWindow, this, camera);
            cameraSettingsWindow.DataContext = viewModel;
            cameraSettingsWindow.ShowDialog();
            camera = viewModel.Camera;
        }
        [RelayCommand]
        public void Debug()
        {
            
        }
        
        [RelayCommand]
        public void OpenAddCameraDialog()
        {
            CameraAddWindow cameraSettingsWindow = new CameraAddWindow();
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
        /// Browses the children of a given session and reference collection.
        /// </summary>
        /// <param name="session">The OPC UA session.</param>
        /// <param name="references">The reference descriptions to browse.</param>
        /// <returns>An ObservableCollection of Tag objects representing the children.</returns>
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
        
        /// <summary>
        /// Given a list of all the OPC tags on a camera this method will search through that list and find the currently loaded
        /// job name.
        /// </summary>
        /// <param name="tags">A list of all the tags in a cameras OPC server</param>
        /// <returns>A string containing the currently loaded job name</returns>
        private string GetJobName(ObservableCollection<Tag> tags, string searchParam)
        {
            List<Tag> results = new List<Tag>();
            string result = "Job Not Found";
            //search for the JobName tag
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
                    result = GetJobName(children, searchParam);
                    if (result != "Job Not Found") return result;
                }
            }

            if (results.Count == 0) return result;
                
            foreach (Tag tag in results)
            {
                if (tag.Name == "JobName")
                {
                    result = OPCUAUtils.ReadTagValue(SelectedCamera.Session, tag.NodeId).Value.ToString();
                    Trace.WriteLine(result);
                    return result;
                }
            }

            return result;
        }
        public static void DisconnectFromAllDevices(ObservableCollection<CognexCamera> deviceList)
        {
            foreach(CognexCamera camera in deviceList)
            {
                camera.Tags?.Clear();
                camera.Session?.Close();
                camera.Session?.Dispose();
            }
        }
        
        /// <summary>
        /// This method checks to see if the default tags required for the monitoring system to work are present in the
        /// currently loaded job file.
        /// </summary>
        /// <returns>false if no issues were found. True if there were issues</returns>
        public bool CheckDefaultTagError()
        {
            bool found = false;
            string acquisitionCount = "AcquisitionCount";
            string imageFileName = "ImageFileName";
            var acqCountTag = Tags.FirstOrDefault(tag => tag.Name == acquisitionCount);
            var imgFileNameTag = Tags.FirstOrDefault(tag => tag.Name == imageFileName);

            if (acqCountTag is null || imgFileNameTag is null)
                found = true;
            
            return found;
        }

        private bool CheckInsightExplorer(ObservableCollection<Tag> tags)
        {
            if (tags == null)
                return false;
            foreach (Tag tag in tags)
            {
                if (tag.Name == "SystemTags")
                    return true;
                else if(tag.Children.Count > 0)
                {
                    ObservableCollection<Tag> children = new ObservableCollection<Tag>(tag.Children);
                    if (CheckInsightExplorer(children))
                        return true; // Stop and return true if found in children
                }
            }

            return false;

        }
        private bool CheckCameraStatus()
        {
            if (SelectedCamera != null && SelectedCamera.Connected)
            {
                return true;
            }
            else
            {
                App.Current.Dispatcher.Invoke(() => { Tags.Clear(); });
                return false;
            }
        }
        public async Task UpdateTagBrowser()
        {
            try
            {
                //Makes sure that the selected camera is not null and is connected
                if (!CheckCameraStatus()) return;
                //browse all the tags on the camera
                SelectedCamera.Tags = await BrowseChildren(SelectedCamera.Session, SelectedCamera.References);
                //if tags are null something is wrong and the method should return. Possible that the camera OPC is not enabled
                if (selectedCamera.Tags == null) return;

                //Gets all tags inside the spreadsheet directory in the OPC UA server
                bool insightExplorer = CheckInsightExplorer(selectedCamera.Tags);
                SearchTag(SelectedCamera.Tags, insightExplorer ? "JobTags" : "Spreadsheet");
                SelectedCamera.DefaultTagError = CheckDefaultTagError();
                //Reads the loaded job from the OPC UA server and sets that as the currently selected job
                SelectedJob = GetJobName(selectedCamera.Tags, insightExplorer ? "SystemTags" : "System");
                int jobID = DatabaseUtils.StoreJob(SelectedJob, SelectedCamera.CameraID);
                //adds the job to the list of jobs in the camera Dont know if we really need this here but here it is anyways
                SelectedCamera.jobs.Add(SelectedJob);

                //Check to see if camera exists in database
                if (DatabaseUtils.CameraExists(SelectedCamera.Endpoint))
                {
                    //Get a list of all the tags that are currently saved in the database
                    List<string> tagsNames = DatabaseUtils.GetSavedTagConfiguration(jobID);
                    if (tagsNames.Count != 0)
                    {
                        SetTagBrowserConfiguration(SelectedCamera.Tags, tagsNames);
                    }
                    else
                    {
                        List<string> tagNames = DisplayTags.Select(tag => tag.Name).ToList();
                        SetTagBrowserConfiguration(SelectedCamera.Tags, tagNames);
                        ApplyChanges();
                    }
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
