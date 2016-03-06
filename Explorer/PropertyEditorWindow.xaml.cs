using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for PropertyEditorWindow.xaml
    /// </summary>
    public partial class PropertyEditorWindow : Window
    {
        private readonly double _windowHeight;

        public PropertyEditorWindow()
        {
            _windowHeight = SystemParameters.WindowCaptionHeight +
                            SystemParameters.WindowResizeBorderThickness.Top +
                            SystemParameters.WindowResizeBorderThickness.Bottom * 2.0;
            InitializeComponent();            
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var propertyName = Property.Key;

            Title = "Edit [" + propertyName + "]";
            lblPropertyName.Content = propertyName;
            txtPropertyValue.Text = ValueMap["Value"];

            chkDeprecated.IsChecked = Property.Deprecated;
            txtPropertyDescription.Text = Property.Description;

            foreach (var dimension in Dimensions)
            {
                var control = DimensionItemControl.Create(dimension);
                control.cmbItems.SelectedItem = ValueMap[dimension.Dimension.Name];
                panelDimensions.Children.Add(control);
            }
            
            UpdateLayout();

            var height = groupProperties.ActualHeight + groupDimensions.ActualHeight + _windowHeight;
            if (height > ActualHeight)
                Height = height;
        }

        private void OnClickOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void OnClickCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public Property Property { get; set; }

        public Dictionary<string, string> ValueMap { get; set; }

        public List<DimensionValues> Dimensions { get; set; }
    }
}
