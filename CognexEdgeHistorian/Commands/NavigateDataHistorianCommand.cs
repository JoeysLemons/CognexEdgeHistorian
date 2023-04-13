using CognexEdgeHistorian.MVVM.ViewModel;
using CognexEdgeHistorian.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CognexEdgeHistorian.Commands
{
    public class NavigateDataHistorianCommand : CommandBase
    {
        private readonly NavigationStore _navigationStore;
        public NavigateDataHistorianCommand(NavigationStore navigationStore)
        {
            _navigationStore = navigationStore;
        }

        public override void Execute(object parameter)
        {
            _navigationStore.CurrentViewModel = new DataHistorianViewModel();
        }
    }
}
