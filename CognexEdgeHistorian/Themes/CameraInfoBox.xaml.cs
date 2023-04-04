using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CognexEdgeHistorian.Themes
{
    /// <summary>
    /// Interaction logic for CameraInfoBox.xaml
    /// </summary>
    public partial class CameraInfoBox : UserControl
    {

        public string NameText
        {
            get { return (string)GetValue(CameraNameProperty); }
            set { SetValue(CameraNameProperty, value); }
        }

        public static readonly DependencyProperty CameraNameProperty =
            DependencyProperty.Register("NameText", typeof(string), typeof(CameraInfoBox), new PropertyMetadata(string.Empty, OnCameraNameChanged));

        private static void OnCameraNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as CameraInfoBox;
            control.NameText = e.NewValue as string;
        }

        public string DescriptionText
        {
            get { return (string)GetValue(CameraDescriptionProperty); }
            set { SetValue(CameraDescriptionProperty, value); }
        }

        public static readonly DependencyProperty CameraDescriptionProperty =
            DependencyProperty.Register("DescriptionText", typeof(string), typeof(CameraInfoBox), new PropertyMetadata(string.Empty, OnCameraDescriptionChanged));
    
        private static void OnCameraDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as CameraInfoBox;
            control.DescriptionText = e.NewValue as string;
        }
    }
}
