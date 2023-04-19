using CognexEdgeHistorian.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CognexEdgeHistorian.MVVM.Model
{
    public class GraphProperties : ViewModelBase
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
                    OnGraphPropertyChanged();
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
                    OnGraphPropertyChanged();
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
                    OnGraphPropertyChanged();
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
                    OnGraphPropertyChanged();
                    OnPropertyChanged(nameof(AxisColor));
                }
            }
        }
        private void OnGraphPropertyChanged()
        {
            GraphPropertyChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler GraphPropertyChanged;
    }
}
