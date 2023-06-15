using System.Runtime.Intrinsics.Arm;
using System.Windows;
using System.Windows.Controls;

namespace EdgePcConfigurationApp.Controls;

public class Modal : ContentControl
{
    public static readonly DependencyProperty IsOpenProperty = 
        DependencyProperty.Register(nameof(IsOpenProperty), 
            typeof(bool),
            typeof(Modal),
            new PropertyMetadata(false));

    public bool IsOpen
    {
        get { return (bool)GetValue(IsOpenProperty); }
        set { SetValue(IsOpenProperty, value); }
    }

    static Modal()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Modal), new FrameworkPropertyMetadata(typeof(Modal)));
    }
    
}