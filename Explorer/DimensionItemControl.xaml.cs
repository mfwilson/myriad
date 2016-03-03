using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for DimensionItemControl.xaml
    /// </summary>
    public partial class DimensionItemControl : UserControl  
    {
        private bool _isSingle;

        public DimensionItemControl()
        {
            InitializeComponent();            
            cmbItems.Visibility = Visibility.Visible;
        }

        public static DimensionItemControl Create(DimensionValues dimensionValues)
        {
            return new DimensionItemControl
            {
                Name = string.Concat("dim", dimensionValues.Dimension.Name),
                Tag = dimensionValues.Dimension,
                lblName = { Content = dimensionValues.Dimension.Name },
                cmbItems = { ItemsSource = dimensionValues.Values.OrderBy(d => d).ToList() }
            };
        }

        private void UpdateView()
        {
            cmbItems.Visibility = IsSingle ? Visibility.Hidden : Visibility.Visible;
            txtDimension.Visibility = IsSingle ? Visibility.Visible : Visibility.Hidden;
        }

        public object DimensionName
        {
            get { return lblName.Content; }
            set { lblName.Content = value; }
        }

        public string DimensionValue
        {
            get { return IsSingle ? txtDimension.Text : cmbItems.SelectedValue.ToString(); }
            set
            {
                if (IsSingle)
                    txtDimension.Text = value;
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
