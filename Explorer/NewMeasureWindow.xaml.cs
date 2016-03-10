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
using System.Windows.Shapes;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for NewMeasureWindow.xaml
    /// </summary>
    public partial class NewMeasureWindow : Window
    {
        public NewMeasureWindow()
        {
            InitializeComponent();
        }

        public object DimensionName
        {
            get { return dimensionItemControl.DimensionName; }
            set { dimensionItemControl.DimensionName = value; }
        }
        public string DimensionValue
        {
            get { return dimensionItemControl.DimensionValue; }
            set { dimensionItemControl.DimensionValue = value; }
        }

        private void OnClickOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
