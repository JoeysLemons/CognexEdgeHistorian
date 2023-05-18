using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EdgePcConfigurationApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Media;
using Wpf.Ui.Common.Interfaces;

namespace EdgePcConfigurationApp.ViewModels
{
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        private ExeConfigurationFileMap fileMap;
        private Configuration serviceConfig;
        public void OnNavigatedTo(){}
        public void OnNavigatedFrom(){}

        [ObservableProperty]
        private string? connectionString;
        [ObservableProperty]
        private string? location;
        [ObservableProperty]
        private string? acquisitionCountNodeID;


        [RelayCommand]
        public void SaveServiceConfig()
        {
            //Write textbox values to EdgeMonitoringService app.config
            //serviceConfig.ConnectionStrings.ConnectionStrings["MainConnectionString"].ConnectionString = connectionString;
            var settings = serviceConfig.AppSettings.Settings;

            settings["Location"].Value = location;
            settings["AcquisitionCountNodeID"].Value = acquisitionCountNodeID;
            serviceConfig.ConnectionStrings.ConnectionStrings["MainConnectionString"].ConnectionString = connectionString;
            
            serviceConfig.Save();
            ConfigurationManager.RefreshSection(serviceConfig.AppSettings.SectionInformation.Name);
            ConfigurationManager.RefreshSection(serviceConfig.ConnectionStrings.SectionInformation.Name);
            
            Trace.WriteLine($"Database Connection String: {connectionString}\nLocation: {location}\nAcquisition Node ID: {acquisitionCountNodeID}");
        }

        public DataViewModel()
        {
            fileMap = new ExeConfigurationFileMap();
            fileMap.ExeConfigFilename =
                @"C:\Users\jverstraete\source\repos\CognexEdgeHistorian\CognexEdgeMonitoringService\ServiceConfig.config";
            serviceConfig = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
        }
    }
}
