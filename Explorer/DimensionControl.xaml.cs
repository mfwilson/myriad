using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for DimensionControl.xaml
    /// </summary>
    public partial class DimensionControl : UserControl
    {
        private readonly ObservableCollection<string> _selectedSet = new ObservableCollection<string>();

        public DimensionControl()
        {
            InitializeComponent();
            listItems.ItemsSource = _selectedSet;            
        }

        private void OnClickClear(object sender, RoutedEventArgs e)
        {
            _selectedSet.Clear();            
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach(var item in e.AddedItems)
            {
                var selectedItem = item.ToString();
                if( _selectedSet.Contains(selectedItem) == false )
                    _selectedSet.Add(selectedItem);
            }
        }

        private void OnListKeyUp(object sender, KeyEventArgs e)
        {
            if( e.Key == Key.Delete )
            {
                for(var i = listItems.SelectedItems.Count - 1; i >= 0; i--)
                {
                    var item = listItems.SelectedItems[i] as string;
                    _selectedSet.Remove(item);
                }
            }
        }
    }
}
