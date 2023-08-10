using System.Windows;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace EdgePcConfigurationApp.Views.Windows;

public partial class ErrorPopup : MessageBox
{
    public ErrorPopup()
    {
        InitializeComponent();
    }
}