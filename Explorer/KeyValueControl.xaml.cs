using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for DimensionItemControl.xaml
    /// </summary>
    public partial class KeyValueControl : UserControl  
    {
        private bool _isSingle;

        public KeyValueControl()
        {
            InitializeComponent();            
            cmbItems.Visibility = Visibility.Visible;
        }

        public static KeyValueControl Create(DimensionValues dimensionValues)
        {
            return new KeyValueControl
            {
                Name = string.Concat("control", dimensionValues.Dimension.Name),
                Tag = dimensionValues.Dimension,
                lblKey = { Content = dimensionValues.Dimension.Name },
                cmbItems = { ItemsSource = dimensionValues.Values.OrderBy(d => d).ToList() }
            };
        }

        public Measure GetMeasure()
        {
            var value = (cmbItems.SelectedValue != null) ? cmbItems.SelectedValue.ToString() : cmbItems.Text;
            if (string.IsNullOrEmpty(value))
                return null;
            return new Measure(Tag as Dimension, value);
        }

        private void UpdateView()
        {
            cmbItems.Visibility = IsSingle ? Visibility.Hidden : Visibility.Visible;
            txtValue.Visibility = IsSingle ? Visibility.Visible : Visibility.Hidden;

            var focused = IsSingle ? txtValue as IInputElement: cmbItems;
            Keyboard.Focus(focused);
        }

        public object Key
        {
            get { return lblKey.Content; }
            set { lblKey.Content = value; }
        }

        public string Value
        {
            get { return IsSingle ? txtValue.Text : cmbItems.SelectedValue.ToString(); }
            set
            {
                if (IsSingle)
                    txtValue.Text = value;
                else
                    cmbItems.SelectedValue = value;    
            }
        }

        public bool IsSingle
        {
            get { return _isSingle; }
            set { _isSingle = value; UpdateView(); }
        }
    }
}
