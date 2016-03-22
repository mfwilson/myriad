using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Myriad;
using Myriad.Client;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MyriadReader _reader;
        private MyriadWriter _writer;
        private readonly DataSet _dataSet = new DataSet("ResultsView");
        private readonly List<DimensionValues> _dimensionValues = new List<DimensionValues>();

        public MainWindow()
        {
            InitializeComponent();

            navigationControl.Subscribe(Observer.Create<Uri>(OnRefresh));
            contextControl.Subscribe(Observer.Create<List<DimensionValues>>(OnQuery));
            contextControl.Subscribe(Observer.Create<HashSet<Measure>>(OnGet));
            contextControl.Subscribe(Observer.Create<Dimension>(OnDimension));            
            contextControl.IsEnabled = false;

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
                    if (table.Columns.Contains(pair.Key) == false)
                        continue;

                    var column = table.Columns[pair.Key];

                    if (column.DataType == typeof(DateTimeOffset))
                    {
                        var timestamp = Int64.Parse(pair.Value);
                        row[pair.Key] = Epoch.ToDateTimeOffset(timestamp);
                    }
                    else if( column.DataType == typeof(bool))
                        row[pair.Key] = bool.Parse(pair.Value);
                    else
                        row[pair.Key] = pair.Value;
                }

                table.Rows.Add(row);
            }
        }

        private void OnGet(HashSet<Measure> measures)
        {
            var result = _reader.Get(measures);

            var table = _dataSet.Tables[0];
            table.Clear();

            foreach (var property in result.Properties)
            {
                var row = table.NewRow();
                row["Property"] = property.Name;
                row["Value"] = property.Value;
                row["Deprecated"] = property.Deprecated;

                foreach (var measure in result.Context.Measures)
                {
                    if (table.Columns.Contains(measure.Dimension.Name) == false)
                        continue;
                    row[measure.Dimension.Name] = measure.Value;
                }

                table.Rows.Add(row);
            }
        }

        private void OnDimension(Dimension dimension)
        {
            if (dimension == null)
                return;

            if (dimension.Name == "Property")
            {
                var observer = Observer.Create<PropertyOperation>(
                    o =>
                    {
                        var propertyResponse = _writer.PutProperty(o);
                        ResetProperty(propertyResponse.Property);
                        ResetDimensions();
                    }
                );

                var editor = CreateEditor();
                editor.Subscribe(observer);
                editor.ShowDialog();
            }
            else
            {
                // Adding the measure only
                var dialog = new NewMeasureWindow
                {
                    Owner = Application.Current.MainWindow,
                    Title = string.Concat("New ", dimension.Name, "...")
                };

                var result = dialog.ShowDialog();
                if (result.HasValue && result.Value && string.IsNullOrEmpty(dialog.MeasureValue) == false)
                {
                    var measure = new Measure(dimension, dialog.MeasureValue);
                    var dimensionValues = _writer.AddMeasure(measure);
                    contextControl.Update(dimensionValues);
                }
            }            
        }

        private void ResetResults()
        {
            var table = new DataTable("Results");

            var dimensions = _reader.GetDimensionList();
            dimensions.Insert(0, "Ordinal");
            dimensions.Insert(2, "Value");
            dimensions.Add("UserName");
            dimensions.Add("Timestamp");

            Func<string, Type> getColumnType =
                d =>
                {
                    switch (d)
                    {
                        case "Ordinal":
                            return typeof(int);
                        case "Timestamp":
                            return typeof(DateTimeOffset);
                        default:
                            return typeof(string);
                    }
                };

            dimensions.ForEach(d => table.Columns.Add(d, getColumnType(d)));
            table.Columns.Add("Deprecated", typeof(bool));

            _dataSet.Reset();
            _dataSet.Tables.Add(table);
            DataContext = _dataSet.Tables[0].DefaultView;
        }

        private void ResetClient(Uri uri)
        {
            try
            {
                _reader = new MyriadReader(uri);
                _writer = new MyriadWriter(uri);

                ResetResults();
                ResetDimensions();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Myriad Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetDimensions()
        {
            _dimensionValues.Clear();
            _dimensionValues.AddRange(_reader.GetMetadata());
            contextControl.Reset(_dimensionValues);
            contextControl.IsEnabled = true;
        }

        private void ResetProperty(Property property)
        {
            if (property == null)
                return;

            var table = _dataSet.Tables[0];

            // Remove current
            var rows = table.Select("Property = '" + property.Key + "'");
            foreach(var row in rows)
                table.Rows.Remove(row);

            if (property.Clusters == null)
                return;

            foreach(var cluster in property.Clusters)
            {
                var row = table.NewRow();

                row["Property"] = property.Key;
                row["Deprecated"] = property.Deprecated;
                row["Value"] = cluster.Value;
                row["UserName"] = cluster.UserName;
                row["Timestamp"] = Epoch.ToDateTimeOffset(cluster.Timestamp);

                foreach (var measure in cluster.Measures)
                {
                    if (table.Columns.Contains(measure.Dimension.Name) == false)
                        continue;
                    row[measure.Dimension.Name] = measure.Value;
                }

                table.Rows.Add(row);
            }
        }

        private static Int64 GetTimestamp(DataRowView view)
        {
            object value;            
            if( view == null || (value = view["Timestamp"]) == DBNull.Value )
                return Int64.MinValue;

            var ticks = ((DateTimeOffset) value).Ticks;
            return Epoch.GetOffset(ticks);
        }

        /// <summary>Convert a data row view to a cluster</summary>
        private static Cluster ToCluster(DataRowView view, List<DimensionValues> dimensionValues)
        {
            var measures = new HashSet<Measure>(
                dimensionValues
                    .Where(d => d.Dimension.Name != "Property" && string.IsNullOrEmpty(view[d.Dimension.Name].ToString()) == false)
                    .Select(d => new Measure(d.Dimension, view[d.Dimension.Name].ToString()))
            );

            var timestamp = GetTimestamp(view);
            return Cluster.Create(view["Value"].ToString(), measures, view["UserName"].ToString(), timestamp);
        }

        private PropertyEditorWindow CreateEditor()
        {
            return new PropertyEditorWindow
            {
                Owner = this,                
                Dimensions = _dimensionValues
            };
        }

        private static DataRowView GetSelectedDataRow(DataGrid grid)
        {
            if (grid.SelectedItems.Count == 0)
                return null;
            return grid.SelectedItems[0] as DataRowView;
        }

        private static Property GetSelectedProperty(MyriadReader reader, DataRowView view)
        {
            if (view == null)
                return null;

            // Hack, we don't currently support edits on rows returned via "Get"
            if (GetTimestamp(view) == Int64.MinValue)
                return null;
            
            // Get Property
            var response = reader.GetProperties(new[] { view["Property"].ToString() });
            return response.Properties.FirstOrDefault();
        }

        private void ResultView_OnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var view = GetSelectedDataRow(resultView);
            if (view == null)
                return;

            var valueMap = _dimensionValues.ToDictionary(d => d.Dimension.Name, d => view[d.Dimension.Name].ToString());
            var property = GetSelectedProperty(_reader, view);

            var observer = Observer.Create<PropertyOperation>(
                o =>
                {
                    var propertyResponse = _writer.PutProperty(o);
                    ResetProperty(propertyResponse.Property);
                    ResetDimensions();
                }
            );

            var editor = CreateEditor();
            editor.Property = property;
            editor.Cluster = ToCluster(view, _dimensionValues);
            editor.ValueMap = valueMap;
            editor.Subscribe(observer);

            editor.ShowDialog();
        }

        private void ResultView_OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyType == typeof(DateTimeOffset))
            {
                var dataGridTextColumn = e.Column as DataGridTextColumn;
                if( dataGridTextColumn != null )
                    dataGridTextColumn.Binding.StringFormat = "{0:yyyy-MM-dd HH:mm:ss.fff}";
            }

            if( e.PropertyName == "Deprecated" )
            {
                var column = e.Column as DataGridCheckBoxColumn;
                if( column != null )
                    column.Visibility = Visibility.Collapsed;
            }
        }

        private void ResultView_OnKeyDown(object sender, KeyEventArgs e)
        {
        }

        private void ResultView_OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete)
                return;

            // Delete current value    
            var view = GetSelectedDataRow(resultView);
            if (view == null)
                return;

            var property = GetSelectedProperty(_reader, view);
            if (property == null)
                return;

            var measures = Extensions.GetMeasures(_dimensionValues, view);
            var operation = property.GetDeleteOperation(measures);
            if (operation == null)
                return;

            var propertyResponse = _writer.PutProperty(operation);
            ResetProperty(propertyResponse.Property);
            ResetDimensions();
        }
    }
}
