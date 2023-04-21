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
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CognexEdgeHistorian.MVVM.ViewModel
{
    public class DataHistorianViewModel : ViewModelBase
    {
        public ICommand NavigateConnectionsCommand { get; }
        public ICommand AddNewChartCommand { get; }
        public ICommand TempCommand { get; }
        public ICommand OpenDataBindingModal { get; }
        public ICommand CloseDataBindingModal { get; }

        public CognexSession CurrentCamera
        {
            get { return ConnectionsViewModel.GetSelectedCamera(); }
        }

        private bool _modalIsOpen;

        public bool ModalIsOpen
        {
            get { return _modalIsOpen; }
            set 
            {
                _modalIsOpen = value; 
                OnPropertyChanged(nameof(ModalIsOpen));
            }
        }

        private PlotModel _activePlotModel = new PlotModel();
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
                var xAxis = ActivePlotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Bottom);
                if (xAxis != null)
                {
                    xAxis.Title = ActiveGraphProperties.XAxisTitle;
                    try
                    {
                        xAxis.AxislineColor = OxyColor.Parse(ActiveGraphProperties.AxisColor);
                        xAxis.TicklineColor = OxyColor.Parse(ActiveGraphProperties.AxisColor);
                    }
                    catch (Exception)
                    {
                        Trace.WriteLine($"Incorrect format for Axis Color. User input: {ActiveGraphProperties.AxisColor}");
                    }
                    
                }
                var yAxis = ActivePlotModel.Axes.FirstOrDefault(a => a.Position == AxisPosition.Left);
                if (yAxis != null)
                {
                    yAxis.Title = ActiveGraphProperties.YAxisTitle;
                    try
                    {
                        yAxis.AxislineColor = OxyColor.Parse(ActiveGraphProperties.AxisColor);
                    }
                    catch (Exception)
                    {
                        Trace.WriteLine($"Incorrect format for Axis Color. User input: {ActiveGraphProperties.AxisColor}");
                    }

                }

                ActivePlotModel.Axes.Add(new LinearAxis { AxislineColor = OxyColor.Parse(ActiveGraphProperties.AxisColor), AxislineThickness = 2, AxislineStyle = LineStyle.Solid, Position = AxisPosition.Left });
                ActivePlotModel.InvalidatePlot(true);
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

        public void OpenModal(object parameter) { ModalIsOpen = true; }
        public void CloseModal(object parameter) { ModalIsOpen = false; }

        public DataHistorianViewModel(NavigationStore navigationStore)
        {
            ActivePlotModel.Title = ActiveGraphProperties.Name;
            ActivePlotModel.Axes.Add(new LinearAxis { AxislineColor= OxyColor.Parse(ActiveGraphProperties.AxisColor), AxislineThickness=2, AxislineStyle=LineStyle.Solid, Position=AxisPosition.Bottom, TitleColor=OxyColors.White, Title=ActiveGraphProperties.XAxisTitle }) ;
            ActivePlotModel.Axes.Add(new LinearAxis { AxislineColor= OxyColor.Parse(ActiveGraphProperties.AxisColor), AxislineThickness=2, AxislineStyle=LineStyle.Solid, Position=AxisPosition.Left, TitleColor=OxyColors.White, Title=ActiveGraphProperties.YAxisTitle }) ;
            ActivePlotModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
            NavigateConnectionsCommand = new NavigateCommand<ConnectionsViewModel>(new NavigationService<ConnectionsViewModel>(
                navigationStore, () => new ConnectionsViewModel(navigationStore)));
            AddNewChartCommand = new RelayCommand(AddNewChart);
            OpenDataBindingModal = new RelayCommand(OpenModal);
            CloseDataBindingModal= new RelayCommand(CloseModal);
            TempCommand= new RelayCommand(Temp);
            ActiveGraphProperties.GraphPropertyChanged += OnGraphPropertyChanged;
        }
    }
}
