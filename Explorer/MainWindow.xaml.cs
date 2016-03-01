using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
using Myriad.Client;

namespace Myriad.Explorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MyriadReader _reader;

        public MainWindow()
        {
            InitializeComponent();

            NavigationControl.Subscribe( Observer.Create<Uri>(OnRefresh) );
            ContextControl.IsEnabled = false;
        }

        private void OnRefresh(Uri uri)
        {
            ResetClient(uri);
        }

        private void ResetClient(Uri uri)
        {
            _reader = new MyriadReader(uri);

            var dimensionValues = _reader.GetMetadata();
            ContextControl.Reset(dimensionValues);

            ContextControl.IsEnabled = true;

            // Clear result view
        }


    }
}
