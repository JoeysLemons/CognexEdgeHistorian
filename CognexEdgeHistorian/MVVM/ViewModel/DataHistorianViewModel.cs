using CognexEdgeHistorian.Commands;
using CognexEdgeHistorian.Core;
using CognexEdgeHistorian.MVVM.Model;
using CognexEdgeHistorian.Services;
using CognexEdgeHistorian.Stores;
using Microsoft.Xaml.Behaviors.Core;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace CognexEdgeHistorian.MVVM.ViewModel
{
    public class DataHistorianViewModel : ViewModelBase
    {
        public ICommand NavigateConnectionsCommand { get; }
        public ICommand AddNewChartCommand { get; }
        public ICommand TempCommand { get; }
        private PlotModel _activePlotModel;
        public PlotModel ActivePlotModel
        {
            get { return _activePlotModel; }
            set 
            { 
                _activePlotModel = value;
                OnPropertyChanged(nameof(ActivePlotModel));
            }
        }

        private ObservableCollection<Series> _activeSeries;
        public ObservableCollection<Series> ActiveSeries
        {
            get { return _activeSeries; }
            set { _activeSeries = value; }
        }

        private GraphProperties _activeGraphProperties = new GraphProperties();
        public GraphProperties ActiveGraphProperties
        {
            get { return _activeGraphProperties; }
            set 
            { 
                _activeGraphProperties = value; 
                OnPropertyChanged(nameof(ActiveGraphProperties));
            }
        }

        public void AddNewChart(object parameter)
        {
            ActivePlotModel = new PlotModel();
            ActivePlotModel.Background = OxyColors.Black;
            LineSeries series = new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)");
            ActivePlotModel.Series.Add(series);
        }

        public void UpdateActivePlotModel()
        {
            ActivePlotModel.Title = ActiveGraphProperties.Name;
            ActivePlotModel.DefaultXAxis.Title = ActiveGraphProperties.XAxisTitle;
            ActivePlotModel.DefaultYAxis.Title = ActiveGraphProperties.YAxisTitle;
            ActivePlotModel.Axes.Clear();
            ActivePlotModel.Axes.Add(new LinearAxis { AxislineColor = OxyColor.Parse(ActiveGraphProperties.AxisColor), AxislineThickness = 2, AxislineStyle = LineStyle.Solid, Position = AxisPosition.Bottom });
            ActivePlotModel.Axes.Add(new LinearAxis { AxislineColor = OxyColor.Parse(ActiveGraphProperties.AxisColor), AxislineThickness = 2, AxislineStyle = LineStyle.Solid, Position = AxisPosition.Left });
        }

        public void Temp(object parameter)
        {
            Trace.WriteLine($"Name: { ActiveGraphProperties.Name}");
        }

        public DataHistorianViewModel(NavigationStore navigationStore)
        {
            ActivePlotModel = new PlotModel();
            ActivePlotModel.Axes.Add(new LinearAxis { AxislineColor = OxyColors.Red, AxislineThickness = 2, AxislineStyle= LineStyle.Solid, Position = AxisPosition.Bottom}) ;
            ActivePlotModel.Axes.Add(new LinearAxis { AxislineColor = OxyColors.Red, AxislineThickness = 2, AxislineStyle= LineStyle.Solid, Position = AxisPosition.Left }) ;
            ActivePlotModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
            NavigateConnectionsCommand = new NavigateCommand<ConnectionsViewModel>(new NavigationService<ConnectionsViewModel>(
                navigationStore, () => new ConnectionsViewModel(navigationStore)));
            AddNewChartCommand = new RelayCommand(AddNewChart);
            TempCommand = new RelayCommand(Temp);
        }
    }
}
