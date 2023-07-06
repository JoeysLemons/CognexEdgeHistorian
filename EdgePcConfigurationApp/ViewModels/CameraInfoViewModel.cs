using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EdgePcConfigurationApp.Models;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Wpf.Ui.Common.Interfaces;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace EdgePcConfigurationApp.ViewModels;

public partial class CameraInfoViewModel : ObservableObject, INavigationAware
{
    private string _ipAddress;
    private string _region;
    private string _location;
    private string _productionLine;
    private string _name;
    private DashboardViewModel _dashboardViewModel;
    public UiWindow PopupWindow { get; set; }

    public string ipAddress
    {
        get => _ipAddress;
        set
        {
            if (value == _ipAddress) return;
            _ipAddress = value;
            OnPropertyChanged();
        }
    }

    public string region
    {
        get => _region;
        set
        {
            if (value == _region) return;
            _region = value;
            OnPropertyChanged();
        }
    }

    public string location
    {
        get => _location;
        set
        {
            if (value == _location) return;
            _location = value;
            OnPropertyChanged();
        }
    }

    public string productionLine
    {
        get => _productionLine;
        set
        {
            if (value == _productionLine) return;
            _productionLine = value;
            OnPropertyChanged();
        }
    }

    public string name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private void Close()
    {
        PopupWindow.Close();
    }

    [RelayCommand]
    public void SaveInfo()
    {
        CameraInfo cameraInfo = new CameraInfo(ipAddress, name, region, location, productionLine);
        _dashboardViewModel.AddCamera(cameraInfo);
        PopupWindow.Close();
    }
    
    public void OnNavigatedTo()
    {
    }

    public void OnNavigatedFrom()
    {
    }
    public CameraInfoViewModel(UiWindow page, DashboardViewModel dashboardViewModel)
    {
        PopupWindow = page;
        _dashboardViewModel = dashboardViewModel;
    }
}