using CognexEdgeHistorian.Commands;
using CognexEdgeHistorian.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CognexEdgeHistorian.MVVM.ViewModel
{
    public class DataHistorianViewModel : ViewModelBase
    {
        public ICommand NavigateConnectionsCommand { get; }
        public DataHistorianViewModel(NavigationStore navigationStore)
        {
            NavigateConnectionsCommand = new NavigateCommand<ConnectionsViewModel>(navigationStore, () => new ConnectionsViewModel(navigationStore));
        }
    }
}
