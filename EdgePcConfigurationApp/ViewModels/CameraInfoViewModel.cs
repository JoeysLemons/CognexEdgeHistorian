using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Common.Interfaces;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace EdgePcConfigurationApp.ViewModels;

public partial class CameraInfoViewModel : ObservableObject, INavigationAware
{
    public UiWindow PopupWindow { get; set; }
    [RelayCommand]
    private void Close()
    {
        PopupWindow.Close();
    }

    [RelayCommand]
    public void SaveInfo()
    {
        MessageBox.Show("Success");
    }

    public void OnNavigatedTo()
    {
    }

    public void OnNavigatedFrom()
    {
    }

    public CameraInfoViewModel(UiWindow page)
    {
        PopupWindow = page;
    }
}