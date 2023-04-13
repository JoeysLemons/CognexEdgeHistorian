using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using CognexEdgeHistorian.MVVM.ViewModel;
using System.Diagnostics;
using CognexEdgeHistorian.MVVM.Models;

namespace CognexEdgeHistorian.Core
{
    public static class ListBoxItemBehavior
    {
        public static readonly DependencyProperty HandleSelectionProperty =
            DependencyProperty.RegisterAttached("HandleSelection", typeof(bool), typeof(ListBoxItemBehavior),
                new PropertyMetadata(false, OnHandleSelectionChanged));

        public static bool GetHandleSelection(DependencyObject obj)
        {
            return (bool)obj.GetValue(HandleSelectionProperty);
        }

        public static void SetHandleSelection(DependencyObject obj, bool value)
        {
            obj.SetValue(HandleSelectionProperty, value);
        }

        private static void OnHandleSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListBoxItem item)
            {
                if ((bool)e.NewValue)
                {
                    item.Selected += OnItemSelected;
                    item.Unselected += OnItemUnselected;
                }
                else
                {
                    item.Selected -= OnItemSelected;
                    item.Unselected -= OnItemUnselected;
                }
            }
        }

        private static void OnItemSelected(object sender, RoutedEventArgs e)
        {
            try
            {
                ListBoxItem item = sender as ListBoxItem;
                if (item != null)
                {
                    Tag content = (Tag)item.Content;
                    ConnectionsViewModel.AddSelectedTag(ConnectionsViewModel.GetSelectedCamera(), content);
                    Console.WriteLine($"Item selected: Name: {content.Name}, NodeId: {content.NodeId}");
                }
            }
            catch(Exception ex)
            {
                Trace.WriteLine("Failed to cast listbox item as a string ");
                Trace.WriteLine(ex.Message);
            }
        }

        private static void OnItemUnselected(object sender, RoutedEventArgs e)
        {
            try
            {
                ListBoxItem item = sender as ListBoxItem;
                if (item != null)
                {
                    Tag content = (Tag)item.Content;
                    ConnectionsViewModel.RemoveSelectedTag(ConnectionsViewModel.GetSelectedCamera(), content);
                    Console.WriteLine($"Item unselected: Name: {content.Name}, NodeId: {content.NodeId}");
                }
            }
            catch(Exception ex)
            {
                Trace.WriteLine("Failed to csat listbox item as a string ");
                Trace.WriteLine(ex.Message);
            }
        }
    }
}
