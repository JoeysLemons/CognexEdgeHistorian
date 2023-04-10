using CognexEdgeHistorian.MVVM.Model;
using CognexEdgeHistorian.MVVM.ViewModel;
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
                foreach (var session in ConnectionsViewModel.SessionList)
                {
                    session?.Session.Close();
                }
            }
            catch(Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            
        }
    }

}
