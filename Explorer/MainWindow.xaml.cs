using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reactive;
using System.Windows;
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

            NavigationControl.Subscribe(Observer.Create<Uri>(OnRefresh));
            ContextControl.Subscribe(Observer.Create<List<DimensionValues>>(OnQuery));
            ContextControl.Subscribe(Observer.Create<HashSet<Measure>>(OnGet));
            ContextControl.Subscribe(Observer.Create<Cluster>(OnCluster));
            ContextControl.Subscribe(Observer.Create<int>(OnCreate));
            ContextControl.IsEnabled = false;

            _dataSet.Tables.Add(new DataTable("Results"));
            DataContext = _dataSet.Tables[0].DefaultView;
        }

        private void OnRefresh(Uri uri)
        {
            ResetClient(uri);
        }

        private void OnCreate(int command)
        {

            var editor = CreateEditor();




            
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

                foreach(var measure in result.Context.Measures)
                {
                    if (table.Columns.Contains(measure.Dimension.Name) == false)
                        continue;
                    row[measure.Dimension.Name] = measure.Value;
                }

                table.Rows.Add(row);
            }
        }

        private void OnCluster(Cluster cluster)
        {
            var measure = cluster.Measures.FirstOrDefault();
            if (measure == null)
                return;

            if( measure.Dimension.Name == "Property" && string.IsNullOrEmpty(cluster.Value) == false )
            {
                var operations = new List<Operation<Cluster>> { Operation<Cluster>.NewAdd(cluster) };
                var propertyOperation = PropertyOperation.Create(measure.Value, "", false, Epoch.UtcNow, operations);
                var response = _writer.PutProperty(propertyOperation);
                ResetProperty(response.Property);
                ContextControl.Update(response.Property);
            }
            else
            {
                // Adding the measure only
                var dimensionValues = _writer.AddMeasure(measure);
                ContextControl.Update(dimensionValues);
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

                _dimensionValues.Clear();
                _dimensionValues.AddRange(_reader.GetMetadata());
                ContextControl.Reset(_dimensionValues);
                ContextControl.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Myriad Explorer", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void OnResultsDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if( ResultView.SelectedItems.Count == 0 )
                return;

            var view = ResultView.SelectedItems[0] as DataRowView;
            if (view == null)
                return;

            // Hack, we don't currently support edits on rows returned via "Get"
            if (GetTimestamp(view) == Int64.MinValue)
                return;

            var valueMap = _dimensionValues.ToDictionary(d => d.Dimension.Name, d => view[d.Dimension.Name].ToString());

            // Get Property
            var response = _reader.GetProperties(new[] {valueMap["Property"]});
            var property = response.Properties.FirstOrDefault();

            var editor = CreateEditor();
            editor.Property = property;
            editor.Cluster = ToCluster(view, _dimensionValues);
            editor.ValueMap = valueMap;

            var result = editor.ShowDialog();
            if (result.HasValue == false || result.Value == false)
                return;

            var propertyResponse = _writer.PutProperty(editor.GetPropertyOperation());
            ResetProperty(propertyResponse.Property);
        }
    }
}
