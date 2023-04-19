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
            try
            {
                ActivePlotModel = new PlotModel();
                ActivePlotModel.Background = OxyColors.Black;
                LineSeries series = new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)");
                ActivePlotModel.Series.Add(series);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error while attempting to add new chart to Data Historian View. \nError Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

        public void OnGraphPropertyChanged(object sender, EventArgs e)
        {
            try
            {
                ActivePlotModel.Title = ActiveGraphProperties.Name;
                ActivePlotModel.Axes.Clear();
                ActivePlotModel.Axes.Add(new LinearAxis { AxislineColor = OxyColor.Parse(ActiveGraphProperties.AxisColor), AxislineThickness = 2, AxislineStyle = LineStyle.Solid, Position = AxisPosition.Bottom });
                ActivePlotModel.Axes.Add(new LinearAxis { AxislineColor = OxyColor.Parse(ActiveGraphProperties.AxisColor), AxislineThickness = 2, AxislineStyle = LineStyle.Solid, Position = AxisPosition.Left });
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error while attempting to update Active Plot Model. \nError Message: {ex.Message}\nStack Trace: {ex.StackTrace}");
            }
        }

        public void Temp(object parameter)
        {
            Trace.WriteLine($"Name: { ActiveGraphProperties.Name}");
        }

        public DataHistorianViewModel(NavigationStore navigationStore)
        {
            ActivePlotModel = new PlotModel();
            ActivePlotModel.Axes.Add(new LinearAxis { AxislineColor=OxyColors.White, AxislineThickness=2, AxislineStyle=LineStyle.Solid, Position=AxisPosition.Bottom, TitleColor=OxyColors.White, Title="X Axis" }) ;
            ActivePlotModel.Axes.Add(new LinearAxis { AxislineColor=OxyColors.White, AxislineThickness=2, AxislineStyle=LineStyle.Solid, Position=AxisPosition.Left, TitleColor=OxyColors.White, Title="Y Axis" }) ;
            ActivePlotModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
            NavigateConnectionsCommand = new NavigateCommand<ConnectionsViewModel>(new NavigationService<ConnectionsViewModel>(
                navigationStore, () => new ConnectionsViewModel(navigationStore)));
            AddNewChartCommand = new RelayCommand(AddNewChart);
            TempCommand = new RelayCommand(Temp);
            ActiveGraphProperties.GraphPropertyChanged += OnGraphPropertyChanged;
        }
    }
}
