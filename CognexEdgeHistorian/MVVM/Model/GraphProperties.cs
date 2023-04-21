using CognexEdgeHistorian.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CognexEdgeHistorian.MVVM.Model
{
    public class GraphProperties : ViewModelBase
    {
        private string _name;
        private string _xAxisTitle;
        private string _yAxisTitle;
        private string _axisColor;
        private string _titleColor;
        
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
            {
                if(_axisColor != value)
                {
                    _axisColor = value;
                    OnGraphPropertyChanged();
                    OnPropertyChanged(nameof(AxisColor));
                }
            }
        }

        public string TitleColor
        {
            get { return _titleColor; }
            set
            {
                if(_titleColor != value)
                {
                    _titleColor = value;
                    OnGraphPropertyChanged();
                    OnPropertyChanged(nameof(TitleColor));
                }
            }
        }

        public GraphProperties()
        {
            Name = "Untitled Graph";
            XAxisTitle = "X Axis";
            YAxisTitle = "Y Axis";
            AxisColor = "#FFFFFF";
            TitleColor = "#FFFFFF";
        }
        private void OnGraphPropertyChanged()
        {
            GraphPropertyChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler GraphPropertyChanged;
    }
}
