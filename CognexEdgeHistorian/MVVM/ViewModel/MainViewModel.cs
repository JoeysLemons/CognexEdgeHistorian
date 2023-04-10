﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CognexEdgeHistorian.MVVM.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public DebugViewModel DebugVM { get; set; }
        public ConnectionsViewModel ConnectionsVM { get; set; }

        public DataHistorianViewModel DataHistorianVM { get; set; }
        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel()
        {
            DataHistorianVM = new DataHistorianViewModel();
            CurrentView = DataHistorianVM;
        }
    }
}
