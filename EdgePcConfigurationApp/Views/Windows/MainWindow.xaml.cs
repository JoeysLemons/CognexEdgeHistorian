using EdgePcConfigurationApp.Helpers;
using EdgePcConfigurationApp.Models;
using EdgePcConfigurationApp.ViewModels;
using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;

namespace EdgePcConfigurationApp.Views.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INavigationWindow
    {
        public ViewModels.MainWindowViewModel ViewModel
        {
            get;
        }

        public MainWindow(ViewModels.MainWindowViewModel viewModel, IPageService pageService, INavigationService navigationService)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            SetPageService(pageService);

            navigationService.SetNavigationControl(RootNavigation);
            WindowStyle = WindowStyle.None;

            DatabaseUtils.ConnectionString = "Data Source=(localdb)\\EdgeHistorian;Initial Catalog=EdgeHistorian;Integrated Security=True";
        }

        #region INavigationWindow methods

        public Frame GetFrame()
            => RootFrame;

        public INavigation GetNavigation()
            => RootNavigation;

        public bool Navigate(Type pageType)
            => RootNavigation.Navigate(pageType);

        public void SetPageService(IPageService pageService)
            => RootNavigation.PageService = pageService;

        public void ShowWindow()
            => Show();

        public void CloseWindow()
            => Close();

        #endregion INavigationWindow methods

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if(DashboardViewModel.CognexCameras.Count > 0)
            {
                DashboardViewModel.DisconnectFromAllDevices(DashboardViewModel.CognexCameras);
            }
            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }
    }
}