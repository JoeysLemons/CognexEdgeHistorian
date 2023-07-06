using System.Windows.Controls;
using Wpf.Ui.Common.Interfaces;
using Wpf.Ui.Controls;

namespace EdgePcConfigurationApp.Views.Pages;

public partial class ConnectionsPage : INavigableView<ViewModels.ConnectionsViewModel>
{
    public ViewModels.ConnectionsViewModel ViewModel
    {
        get;
    }

    public ConnectionsPage(ViewModels.ConnectionsViewModel viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();
    }

    //Handles mouse wheel scrolling in the tag browser
    private void ListViewScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        ScrollViewer scv = (ScrollViewer)sender;
        scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
        e.Handled = true;
    }
}