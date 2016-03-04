using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive;
using System.Windows;
using System.Windows.Input;
using Myriad.Client;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MyriadReader _reader;
        private MyriadReader _writer;
        private readonly DataSet _dataSet = new DataSet("ResultsView");
        private readonly List<DimensionValues> _dimensionValues = new List<DimensionValues>();

        public MainWindow()
        {
            InitializeComponent();

            NavigationControl.Subscribe( Observer.Create<Uri>(OnRefresh) );
            ContextControl.Subscribe(Observer.Create<List<DimensionValues>>(OnQuery));
            ContextControl.Subscribe(Observer.Create<Measure>(OnMeasure));
            ContextControl.IsEnabled = false;

            _dataSet.Tables.Add(new DataTable("Results"));
            DataContext = _dataSet.Tables[0].DefaultView;
        }

        private void OnRefresh(Uri uri)
        {
            ResetClient(uri);
        }

        private void OnQuery(List<DimensionValues> dimensionValuesList)
        {
            var result = _reader.Query(dimensionValuesList);
            
            var table = _dataSet.Tables[0];
            table.Clear();
            
            foreach (var map in result)
            {
                var row = table.NewRow();

                foreach (var pair in map)
                {
                    if( table.Columns.Contains(pair.Key) )
                        row[pair.Key] = pair.Value;                        
                }
                
                table.Rows.Add(row);
            }
        }

        private void OnMeasure(Measure measure)
        {
            //_reader.AddDimensionValue(measure.Dimension, measure.Value);
        }

        private void ResetResults()
        {
            var table = new DataTable("Results");

            var dimensions = _reader.GetDimensionList();
            dimensions.Insert(0, "Ordinal");
            dimensions.Insert(2, "Value");
            dimensions.ForEach(d => table.Columns.Add(d, d == "Ordinal" ? typeof(int) : typeof(string)));

            _dataSet.Reset();
            _dataSet.Tables.Add(table);
            DataContext = _dataSet.Tables[0].DefaultView;
        }

        private void ResetClient(Uri uri)
        {
            _reader = new MyriadReader(uri);
            _writer = new MyriadReader(uri);

            ResetResults();

            _dimensionValues.Clear();
            _dimensionValues.AddRange(_reader.GetMetadata());            
            ContextControl.Reset(_dimensionValues);
            ContextControl.IsEnabled = true;
        }

        private void OnResultsDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if( ResultView.SelectedItems.Count == 0 )
                return;

            var view = ResultView.SelectedItems[0] as DataRowView;
            if (view == null)
                return;

            var valueMap = _dimensionValues.ToDictionary(d => d.Dimension.Name, d => view[d.Dimension.Name].ToString());
            valueMap["Value"] = view["Value"].ToString();


            //var propertySet = _reader.QueryProperties( new[] { valueMap["Property"] } );


            var editor = new PropertyEditorWindow
            {
                Owner = this,
                ValueMap = valueMap,
                Property = _dimensionValues.SingleOrDefault(d => d.Dimension.Name == "Property"),
                Dimensions = _dimensionValues.Where(d => d.Dimension.Name != "Property").ToList()
            };

            editor.ShowDialog();
        }
    }
}
