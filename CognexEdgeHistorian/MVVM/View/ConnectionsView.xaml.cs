using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CognexEdgeHistorian.MVVM.View
{
    /// <summary>
    /// Interaction logic for ConnectionsView.xaml
    /// </summary>
    public partial class ConnectionsView : UserControl
    {
        public ConnectionsView()
        {
            InitializeComponent();
        }


        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var listBox = sender as ListBox;
            var item = listBox.ContainerFromElement((DependencyObject)e.OriginalSource) as ListBoxItem;

            if (item != null)
            {
                item.IsSelected = !item.IsSelected;
                e.Handled = true;
            }
        }

        private void TagBrowser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
