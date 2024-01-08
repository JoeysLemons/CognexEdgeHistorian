using EdgePcConfigurationApp.Helpers;
using EdgePcConfigurationApp.Models;
using EdgePcConfigurationApp.ViewModels;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Microsoft.Extensions.Configuration;
using Wpf.Ui.Controls.Interfaces;
using Wpf.Ui.Mvvm.Contracts;
using ConfigurationManager = System.Configuration.ConfigurationManager;

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

            XmlDocument doc = new XmlDocument();
            doc.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppSettings.xml"));
            XmlNode root = doc.DocumentElement;
            XmlNode dbSettings = root.SelectSingleNode("Database");
            
            string connectionString = dbSettings.SelectSingleNode("ConnectionString").InnerText;
            DatabaseUtils.ConnectionString = SanitizeConnectionString(connectionString);
            Trace.WriteLine(DatabaseUtils.ConnectionString);
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
        static string SanitizeConnectionString(string connectionString)
        {
            // Remove leading and trailing whitespace
            string sanitizedConnectionString = connectionString.Trim();

            // Properly escape special characters
            sanitizedConnectionString = System.Security.SecurityElement.Escape(sanitizedConnectionString);

            return sanitizedConnectionString;
        }
    }
}