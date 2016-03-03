using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Subjects;
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
        private readonly Subject<Measure> _measureSubject = new Subject<Measure>();

        public DimensionControl()
        {
            InitializeComponent();
            listItems.ItemsSource = _selectedSet;            
        }

        public IDisposable Subscribe(IObserver<Measure> observer)
        {
            return _measureSubject.Subscribe(observer);
        }

        private void OnClickNew(object sender, RoutedEventArgs e)
        {
            var dimension = Tag as Dimension;
            if (dimension == null)
                return;

            var dialog = new NewDimensionWindow
            {
                Owner = Application.Current.MainWindow,
                Title = string.Concat("New ", dimension.Name, "...")
            };

            var result = dialog.ShowDialog();
            if ( result.HasValue && result.Value )
            {
                // raise new dimension value
                var measure = new Measure(dimension, dialog.dimensionItemControl.txtDimension.Text);
                _measureSubject.OnNext(measure);
            }
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

        public static DimensionControl Create(DimensionValues dimensionValues)
        {
            return new DimensionControl
            {
                Name = string.Concat("dim", dimensionValues.Dimension.Name),
                Tag = dimensionValues.Dimension,
                lblName = { Content = dimensionValues.Dimension.Name },
                cmbItems = { ItemsSource = dimensionValues.Values.OrderBy(d => d).ToList() }
            };
        }

        public DimensionValues GetDimensionValues()
        {
            var dimension = Tag as Dimension;
            return new DimensionValues(dimension, _selectedSet.ToArray());
        }
    }
}
