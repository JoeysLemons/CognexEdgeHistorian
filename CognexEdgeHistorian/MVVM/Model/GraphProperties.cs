using CognexEdgeHistorian.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CognexEdgeHistorian.MVVM.Model
{
    public class GraphProperties : INotifyPropertyChanged
    {
        private string _name;
        private string _xAxisTitle;
        private string _yAxisTitle;
        private string _axisColor;

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public string XAxisTitle
        {
            get { return _xAxisTitle; }
            set
            {
                if (_xAxisTitle != value)
                {
                    _xAxisTitle = value;
                    OnPropertyChanged(nameof(XAxisTitle));
                }
            }
        }

        public string YAxisTitle
        {
            get { return _yAxisTitle; }
            set
            {
                if (_yAxisTitle != value)
                {
                    _yAxisTitle = value;
                    OnPropertyChanged(nameof(YAxisTitle));
                }
            }
        }

        public string AxisColor {
            get { return _axisColor; }
            set
            {if(_axisColor != value)
                {
                    _axisColor = value;
                    OnPropertyChanged(nameof(AxisColor));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
