using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Windows;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for PropertyEditorWindow.xaml
    /// </summary>
    public partial class PropertyEditorWindow : Window
    {
        private readonly ISubject<PropertyOperation> _operationSubject = new Subject<PropertyOperation>();
        private readonly double _windowHeight;

        public PropertyEditorWindow()
        {
            _windowHeight = SystemParameters.WindowCaptionHeight +
                            SystemParameters.WindowResizeBorderThickness.Top +
                            SystemParameters.WindowResizeBorderThickness.Bottom * 2.0;
            InitializeComponent();            
        }

        public IDisposable Subscribe(IObserver<PropertyOperation> observer)
        {
            return _operationSubject.Subscribe(observer);
        }

        private HashSet<Measure> GetMeasures()
        {
            var measures = new HashSet<Measure>();

            foreach(var child in panelDimensions.Children)
            {
                var control = child as KeyValueControl;
                var measure = control?.GetMeasure();
                if( measure != null )
                    measures.Add(measure);
            }

            return measures;
        }
        
        private PropertyOperation GetPropertyOperation()
        {
            var measures = GetMeasures();
            var updated = Cluster.Create(txtPropertyValue.Text, measures);
            var cluster = Cluster == null ? Operation<Cluster>.NewAdd(updated) : Operation<Cluster>.NewUpdate(Cluster, updated);
            var clusters = new List<Operation<Cluster>> { cluster };                
            return PropertyOperation.Create(txtPropertyName.Text, txtPropertyDescription.Text, chkDeprecated.IsChecked.GetValueOrDefault(), Epoch.UtcNow, clusters);
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            if( Property != null )
            {
                var propertyName = Property.Key;

                Title = "Edit [" + propertyName + "]";

                txtPropertyName.Text = propertyName;
                txtPropertyValue.Text = Cluster.Value;

                chkDeprecated.IsChecked = Property.Deprecated;
                txtPropertyDescription.Text = Property.Description;
            }
            else
            {
                Title = "Create Property...";

                txtPropertyName.Text = "";
                txtPropertyName.BorderThickness = new Thickness(1.0);
                txtPropertyName.IsReadOnly = false;
            }

            foreach (var dimension in Dimensions)
            {
                if (dimension.Dimension.Name == "Property")
                    continue;

                var control = KeyValueControl.Create(dimension);
                panelDimensions.Children.Add(control);

                if ( ValueMap != null && ValueMap.ContainsKey(dimension.Dimension.Name) )
                    control.cmbItems.SelectedItem = ValueMap[dimension.Dimension.Name];
            }
            
            UpdateLayout();

            var height = groupProperties.ActualHeight + groupDimensions.ActualHeight + _windowHeight;
            if (height > ActualHeight)
                Height = height;
        }

        private void OnClickApply(object sender, RoutedEventArgs e)
        {
            _operationSubject.OnNext(GetPropertyOperation());
        }

        private void OnClickClose(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public Property Property { get; set; }
        
        public Cluster Cluster { get; set; }

        public Dictionary<string, string> ValueMap { get; set; }

        public List<DimensionValues> Dimensions { get; set; }
    }
}
