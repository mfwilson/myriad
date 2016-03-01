using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for ContextControl.xaml
    /// </summary>
    public partial class ContextControl : UserControl
    {
        public ContextControl()
        {
            InitializeComponent();
        }

        public void Reset(List<DimensionValues> dimensionValuesList)
        {
            RemoveDimensionControls();

            foreach(var dimensionValues in dimensionValuesList)
            {
                var control = CreateDimensionControl(dimensionValues);
                stackPanel.Children.Add(control);

            }
        }

        private DimensionControl CreateDimensionControl(DimensionValues dimensionValues)
        {
            return new DimensionControl
            {
                lblName = { Content = dimensionValues.Dimension.Name },
                Name = string.Concat("dim", dimensionValues.Dimension.Name),
                cmbItems = { ItemsSource = dimensionValues.Values.OrderBy(d => d).ToList() }                
            };
        }

        private void RemoveDimensionControls()
        {
            var count = stackPanel.Children.Count;
            stackPanel.Children.RemoveRange(1, count - 1);
        }

    }
}
