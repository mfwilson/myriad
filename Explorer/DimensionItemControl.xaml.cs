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
    /// Interaction logic for DimensionItemControl.xaml
    /// </summary>
    public partial class DimensionItemControl : UserControl
    {
        public DimensionItemControl()
        {
            InitializeComponent();
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

        public object DimensionName
        {
            get { return lblName.Content; }
            set { lblName.Content = value; }
        }
    }
}
