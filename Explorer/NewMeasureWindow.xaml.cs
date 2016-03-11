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

        public object DefaultName
        {
            get { return defaultControl.Key; }
            set { defaultControl.Key = value; }
        }

        public string DefaultValue
        {
            get { return defaultControl.Value; }
            set { defaultControl.Value = value; }
        }

        private void OnClickOk(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public bool ShowDefault
        {
            get { return defaultControl.Visibility == Visibility.Visible; }
            set { defaultControl.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }
    }
}
