using CognexEdgeHistorian.Core;
using CognexEdgeHistorian.MVVM.Model;
using CognexEdgeHistorian.MVVM.ViewModel;
using CognexEdgeHistorian.Stores;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CognexEdgeHistorian
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnExit(object sender, EventArgs e)
        {
            try
            {
                //DatabaseUtils.CloseSQLConnection();
                foreach (var session in ConnectionsViewModel.SessionList)
                {
                    session?.Session.Close();
                }
            }
            catch(Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }

            App.Current.Shutdown();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            DatabaseUtils.ConnectionString = "Data Source=C:\\Programming\\CognexEdgeDatabase\\CognexEdgeHistorianTestDB.db;Version=3;";
            //DatabaseUtils.OpenSQLConnection();

            NavigationStore navigationStore = new NavigationStore();
            navigationStore.CurrentViewModel = new ConnectionsViewModel(navigationStore);

            MainWindow = new MainWindow()
            {
                DataContext = new MainViewModel(navigationStore)
            };
            MainWindow.Show();
            base.OnStartup(e);            
        }
    }

}
