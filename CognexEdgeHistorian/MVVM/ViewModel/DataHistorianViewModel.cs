using CognexEdgeHistorian.Commands;
using CognexEdgeHistorian.MVVM.Model;
using CognexEdgeHistorian.Services;
using CognexEdgeHistorian.Stores;
using OxyPlot;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CognexEdgeHistorian.MVVM.ViewModel
{
    public class DataHistorianViewModel : ViewModelBase
    {
        public ICommand NavigateConnectionsCommand { get; }
        public PlotModel ActivePlotModel { get; set; }
        private ObservableCollection<Series> _activeSeries;

        public ObservableCollection<Series> ActiveSeries
        {
            get { return _activeSeries; }
            set { _activeSeries = value; }
        }


        public DataHistorianViewModel(NavigationStore navigationStore)
        {
            NavigateConnectionsCommand = new NavigateCommand<ConnectionsViewModel>(new NavigationService<ConnectionsViewModel>(
                navigationStore, () => new ConnectionsViewModel(navigationStore)));
        }
    }
}
