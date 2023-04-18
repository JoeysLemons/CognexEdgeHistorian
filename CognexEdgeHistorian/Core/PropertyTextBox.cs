using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CognexEdgeHistorian.Core
{
    public class PropertyTextBox : TextBox
    {
        public static readonly DependencyProperty PropNameProperty =
            DependencyProperty.Register("PropName", typeof(string), typeof(PropertyTextBox), new PropertyMetadata(string.Empty));

        public string PropName
        {
            get { return (string)GetValue(PropNameProperty); }
            set { SetValue(PropNameProperty, value); }
        }
    }
}
