using System.Windows;

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

        public object MeasureName
        {
            get { return measureControl.Key; }
            set { measureControl.Key = value; }
        }
        public string MeasureValue
        {
            get { return measureControl.Value; }
            set { measureControl.Value = value; }
        }

        private void OnClickOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
