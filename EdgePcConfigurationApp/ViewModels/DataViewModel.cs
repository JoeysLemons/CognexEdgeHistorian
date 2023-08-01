using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EdgePcConfigurationApp.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Windows.Media;
using System.Xml;
using Wpf.Ui.Common.Interfaces;

namespace EdgePcConfigurationApp.ViewModels
{
    public partial class DataViewModel : ObservableObject, INavigationAware
    {
        public void OnNavigatedTo(){}
        public void OnNavigatedFrom(){}
        private string xmlFilePath = "../../../AppSettings.xml";

        [ObservableProperty]
        private string? connectionString;
        [ObservableProperty]
        private string? geoLocation;
        [ObservableProperty]
        private string? manufacturingArea;


        [RelayCommand]
        public void SaveServiceConfig()
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlFilePath);
                XmlNode root = doc.DocumentElement;
                XmlNode dbSettings = root.SelectSingleNode("Database");
                XmlNode pcSettings = root.SelectSingleNode("PCSettings");
                dbSettings.SelectSingleNode("ConnectionString").InnerText =
                    string.IsNullOrWhiteSpace(connectionString)
                        ? dbSettings.SelectSingleNode("ConnectionString").InnerText
                        : connectionString;

                pcSettings.SelectSingleNode("GeographicLocation").InnerText =
                    string.IsNullOrWhiteSpace(geoLocation)
                        ? pcSettings.SelectSingleNode("GeographicLocation").InnerText
                        : geoLocation;

                pcSettings.SelectSingleNode("ManufacturingArea").InnerText =
                    string.IsNullOrWhiteSpace(manufacturingArea)
                        ? pcSettings.SelectSingleNode("ManufacturingArea").InnerText
                        : manufacturingArea;

                doc.Save(xmlFilePath);
            }
            catch (System.IO.FileNotFoundException e)
            {
                ErrorMessage = "Could not open the AppSettings.xml configuration file.";
                Console.WriteLine(e);
                return;
            }
            catch (NullReferenceException e)
            {
                ErrorMessage = "Could not find one or more XML Nodes. Changes were not saved.";
                Trace.WriteLine(ErrorMessage);
                return;
            }
            catch (Exception e)
            {
                ErrorMessage =
                    $"Something unexpected occurred while attempting to save changes to configuration file. Error Message: {ErrorMessage}";
                return;
            }
            
            Trace.WriteLine($"Database Connection String: {connectionString}\nLocation: {geoLocation}\nAcquisition Node ID: {manufacturingArea}");
        }
        
        //Holds the current error message that should be displayed in the error log. If this string is empty the error log is not visible
        private string errorMessage = string.Empty;
        public string ErrorMessage
        {
            get { return errorMessage; }
            set 
            {
                errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
                OnPropertyChanged(nameof(IsStringNotEmpty));
            }
        }

        //Used for the BooleanToVisibilityConverter in the XAML Code. It just returns a true if the string is not empty and false if it is empty.
        public bool IsStringNotEmpty
        {
            get { return !string.IsNullOrEmpty(errorMessage); }
        }
        
        [RelayCommand]
        public void ClearErrors()
        {
            ErrorMessage = string.Empty;
        }

        public DataViewModel()
        {
            
        }
    }
}
