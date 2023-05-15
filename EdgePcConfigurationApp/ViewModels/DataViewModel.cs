using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EdgePcConfigurationApp.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media;
using Wpf.Ui.Common.Interfaces;

namespace EdgePcConfigurationApp.ViewModels
{
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
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
            Trace.WriteLine($"Database Connection String: {connectionString}\nLocation: {location}\nAcquisition Node ID: {acquisitionCountNodeID}");
        }
    }
}
